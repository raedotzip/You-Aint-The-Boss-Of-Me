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

    [Tooltip("Which layers should stop the player? (Usually Everything except Player)")]
    public LayerMask collisionLayers = ~0; // Default to everything

    // ===============================
    // DASH
    // ===============================
    [Header("Dash")]
    public SteamVR_Action_Boolean triggerAction;
    public float dashDistance = 4f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    [Tooltip("How far below the dash path to check for ground")]
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

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (head == null) head = Camera.main.transform;
        
        // Ensure actions are assigned
        if (moveAction == null) moveAction = SteamVR_Actions.default_Move;
        if (triggerAction == null) triggerAction = SteamVR_Actions.default_Dash;

        // Clean up: Auto-detect objects without colliders
        ValidateSceneColliders();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= dt;

        ApplyGravity();

        // Right Hand trigger for Dash
        if (triggerAction != null && triggerAction.GetStateDown(SteamVR_Input_Sources.RightHand) && _cooldownTimer <= 0f && !_isDashing)
            StartDash();

        if (_isDashing)
            UpdateDash(dt);
        else
            UpdateNormalMovement(dt);
            
        // Critical for VR: Ensures physics knows where the CC moved this frame
        Physics.SyncTransforms();
    }

    private void ApplyGravity()
    {
        if (cc.isGrounded && verticalVelocity < 0)
            verticalVelocity = stickToGroundForce;
        else
            verticalVelocity += gravity * Time.deltaTime;
        
        verticalVelocity = Mathf.Max(verticalVelocity, -20f);
    }

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
        
        // If the distance is too small, we are likely touching a wall already
        if (safeDistance < 0.1f) return;

        _dashDir = dir;
        _effectiveDashDistance = safeDistance;
        _dashTimer = dashDuration * (safeDistance / dashDistance);
        _cooldownTimer = dashCooldown;
        _isDashing = true;
    }

    private float GetSafeDashDistance(Vector3 dir)
    {
        Vector3 origin = transform.position + cc.center;
        
        // SphereCast checks for walls using the CharacterController's actual width
        // We use collisionLayers to ignore the player's own layer
        bool hitWall = Physics.SphereCast(origin, cc.radius, dir, out RaycastHit wallHit, dashDistance, collisionLayers);

        if (hitWall)
        {
            // Stop slightly before the wall to prevent clipping
            return Mathf.Max(0f, wallHit.distance - 0.1f);
        }

        // Ledge Detection logic
        const float stepSize = 0.5f;
        int steps = Mathf.CeilToInt(dashDistance / stepSize);
        float lastSafe = 0f;

        for (int i = 1; i <= steps; i++)
        {
            float d = Mathf.Min(i * stepSize, dashDistance);
            Vector3 checkPos = transform.position + dir * d;
            if (Physics.Raycast(checkPos + Vector3.up, Vector3.down, ledgeCheckDepth + 1f, collisionLayers))
                lastSafe = d;
            else
                break;
        }

        return lastSafe;
    }

    private void UpdateDash(float dt)
    {
        _dashTimer -= dt;
        float speed = _effectiveDashDistance / dashDuration;
        
        // CharacterController.Move automatically handles sliding against Mesh Colliders
        cc.Move((_dashDir * speed + Vector3.up * verticalVelocity) * dt);

        if (_dashTimer <= 0f)
            _isDashing = false;
    }

    private void UpdateNormalMovement(float dt)
    {
        Vector2 input = moveAction.GetAxis(inputSource);
        Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 rgt = head.right;   rgt.y = 0f; rgt.Normalize();

        Vector3 horizontal = (fwd * input.y + rgt * input.x) * moveSpeed;
        cc.Move((horizontal + Vector3.up * verticalVelocity) * dt);
    }

    private void ValidateSceneColliders()
    {
        foreach (MeshRenderer mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (mr.gameObject.isStatic && mr.GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[Physics Fix] {mr.gameObject.name} is static but has no Collider. You will walk through it!", mr.gameObject);
            }
        }
    }
}