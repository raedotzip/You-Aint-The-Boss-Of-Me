using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ===============================
    // MOVEMENT & COLLISION
    // ===============================
    [Header("Movement")]
    public SteamVR_Action_Vector2 moveAction;
    public SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.Any;
    public float moveSpeed = 2f;
    public Transform head;

    [Tooltip("Layers that block movement (include MapWall + Ground)")]
    public LayerMask collisionLayers = ~0;

    // ===============================
    // DASH
    // ===============================
    [Header("Dash")]
    public SteamVR_Action_Boolean triggerAction;
    public float dashDistance = 4f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    public float ledgeCheckDepth = 3f;

    private bool _isDashing = false;
    private float _dashTimer = 0f;
    private float _cooldownTimer = 0f;
    private Vector3 _dashDir = Vector3.zero;
    private float _effectiveDashDistance = 0f;

    // ===============================
    // PHYSICS
    // ===============================
    private CharacterController cc;
    private float verticalVelocity = 0f;

    private readonly float gravity = -9.81f;
    private readonly float stickToGroundForce = -2f;

    // Custom ground detection
    private bool isGrounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (head == null) head = Camera.main.transform;

        if (moveAction == null) moveAction = SteamVR_Actions.default_Move;
        if (triggerAction == null) triggerAction = SteamVR_Actions.default_Dash;

        ValidateSceneColliders();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= dt;

        ApplyGravity();

        if (triggerAction != null &&
            triggerAction.GetStateDown(SteamVR_Input_Sources.RightHand) &&
            _cooldownTimer <= 0f &&
            !_isDashing)
        {
            StartDash();
        }

        if (_isDashing)
            UpdateDash(dt);
        else
            UpdateNormalMovement(dt);

        Physics.SyncTransforms();
    }

    private void ApplyGravity()
    {
        // Use the CC's built-in flag as a secondary check
        bool ccGrounded = cc.isGrounded;

        Vector3 origin = transform.position + cc.center;
        float radius = cc.radius * 0.9f; // Slightly smaller to avoid wall friction
        float castDistance = (cc.height / 2f) + 0.1f; // Small buffer

        isGrounded = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            collisionLayers
        );

        // Combine both checks
        if ((isGrounded || ccGrounded) && verticalVelocity < 0)
        {
            verticalVelocity = stickToGroundForce;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    // ===============================
    // DASH START
    // ===============================
    private void StartDash()
    {
        Vector2 stick = moveAction.GetAxis(inputSource);

        Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 rgt = head.right;   rgt.y = 0f; rgt.Normalize();

        Vector3 dir = stick.sqrMagnitude > 0.05f
            ? (fwd * stick.y + rgt * stick.x).normalized
            : fwd;

        if (dir.sqrMagnitude <= 0.001f) return;

        float safeDistance = GetSafeDashDistance(dir);

        if (safeDistance < 0.1f) return;

        _dashDir              = dir;
        _effectiveDashDistance = safeDistance;
        _dashTimer            = dashDuration;   // always run the full duration; speed scales to cover safeDistance
        _cooldownTimer        = dashCooldown;
        _isDashing            = true;
    }

    // ===============================
    // SAFE DASH DISTANCE (WALL SAFE)
    // ===============================
    private float GetSafeDashDistance(Vector3 dir)
    {
        Vector3 origin = transform.position + cc.center;

        // Stop before any wall in the dash direction
        if (Physics.SphereCast(origin, cc.radius, dir, out RaycastHit hit, dashDistance, collisionLayers))
            return Mathf.Max(0f, hit.distance - 0.05f);

        return dashDistance;
    }

    // ===============================
    // DASH UPDATE (ANTI-TUNNEL)
    // ===============================
    private void UpdateDash(float dt)
    {
        _dashTimer -= dt;

        // Speed is calculated so we always cover exactly _effectiveDashDistance over dashDuration
        float speed = _effectiveDashDistance / dashDuration;

        // Sample the floor directly under the player to get the slope normal
        Vector3 origin   = transform.position + cc.center;
        float   castDist = (cc.height / 2f) + 0.4f;

        Vector3 moveDir;
        if (Physics.SphereCast(origin, cc.radius * 0.8f, Vector3.down, out RaycastHit groundHit, castDist, collisionLayers))
        {
            moveDir = Vector3.ProjectOnPlane(_dashDir, groundHit.normal).normalized;

            // Only keep upward Y when the slope is steep enough to actually require climbing.
            // On flat or near-flat ground (normal.y > 0.95, i.e. < ~18°) the CC handles micro
            // height changes naturally; removing spurious positive Y stops the "float up" feel.
            if (moveDir.y > 0f && groundHit.normal.y > 0.95f)
            {
                moveDir.y = 0f;
                if (moveDir.sqrMagnitude > 0.001f) moveDir.Normalize();
            }
        }
        else
        {
            // Airborne: keep horizontal direction and apply gravity
            moveDir = _dashDir;
        }

        Vector3 totalMove = moveDir * speed * dt;

        // Apply gravity separately so incline dashes don't fight it
        if (!isGrounded)
            totalMove.y += verticalVelocity * dt;

        // Sub-steps to prevent tunnelling through thin walls
        int steps = 4;
        Vector3 stepMove = totalMove / steps;

        for (int i = 0; i < steps; i++)
        {
            CollisionFlags flags = cc.Move(stepMove);

            // Stop on a solid wall; walkable slopes are handled above via ProjectOnPlane
            // so Sides here means a genuine wall or steep obstacle
            if ((flags & CollisionFlags.Sides) != 0)
            {
                _isDashing = false;
                break;
            }
        }

        if (_dashTimer <= 0f)
            _isDashing = false;
    }

    // ===============================
    // NORMAL MOVEMENT (SAFE)
    // ===============================
    private void UpdateNormalMovement(float dt)
    {
        Vector2 input = moveAction.GetAxis(inputSource);

        Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 rgt = head.right;   rgt.y = 0f; rgt.Normalize();

        Vector3 move = (fwd * input.y + rgt * input.x) * moveSpeed;
        move += Vector3.up * verticalVelocity;

        // Substep movement for stability
        int steps = 2;
        Vector3 stepMove = move * dt / steps;

        for (int i = 0; i < steps; i++)
        {
            cc.Move(stepMove);
        }
    }

    public void SyncTeleport()
    {
        verticalVelocity = 0f; // Reset gravity
        _isDashing = false;    // Stop any active dashes
    }

    // ===============================
    // DEBUG HELPER
    // ===============================
    private void ValidateSceneColliders()
    {
        foreach (MeshRenderer mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (mr.gameObject.isStatic && mr.GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[Physics Fix] {mr.gameObject.name} has no Collider!", mr.gameObject);
            }
        }
    }
}