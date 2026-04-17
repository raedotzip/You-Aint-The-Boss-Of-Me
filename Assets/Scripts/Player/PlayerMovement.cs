using UnityEngine;
using Valve.VR;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ===============================
    // MOVEMENT
    // ===============================
    [Header("Movement")]
    public SteamVR_Action_Vector2 moveAction;
    public SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.Any; // Changed to Any for better compatibility
    public float moveSpeed = 2f;
    public Transform head;

    // ===============================
    // DASH
    // ===============================
    [Header("Dash")]
    [Tooltip("Recommended: /actions/default/in/InteractUI or GrabPinch")]
    public SteamVR_Action_Boolean triggerAction;

    public float dashDistance = 4f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    private bool _isDashing = false;
    private float _dashTimer = 0f;
    private float _cooldownTimer = 0f;
    private Vector3 _dashDir = Vector3.zero;

    // ===============================
    // PHYSICS
    // ===============================
    private CharacterController cc;
    private float verticalVelocity = 0f;
    private float gravity = -9.81f;
    private float stickToGroundForce = -2f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (head == null) head = Camera.main.transform;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Handle Cooldown
        if (_cooldownTimer > 0f)
            _cooldownTimer -= dt;

        // Handle Gravity
        ApplyGravity();

        // 1. Check for Dash Input
        // Note: Using GetStateDown. Ensure your SteamVR Input Binding is set to "Boolean" for this action.
        if (triggerAction != null && triggerAction.GetStateDown(inputSource) && _cooldownTimer <= 0f && !_isDashing)
        {
            StartDash();
        }

        // 2. Execute Movement Logic
        if (_isDashing)
        {
            UpdateDash(dt);
        }
        else
        {
            UpdateNormalMovement(dt);
        }
    }

    private void ApplyGravity()
    {
        if (cc.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = stickToGroundForce;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        verticalVelocity = Mathf.Max(verticalVelocity, -20f);
    }

    private void StartDash()
    {
        Vector2 stick = moveAction.GetAxis(inputSource);
        
        // Get Forward/Right based on Head Orientation
        Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 rgt = head.right;   rgt.y = 0f; rgt.Normalize();

        // If stick is moved, dash in stick direction. Otherwise, dash where you are looking.
        Vector3 dir = stick.sqrMagnitude > 0.05f
            ? (fwd * stick.y + rgt * stick.x).normalized
            : fwd;

        if (dir.sqrMagnitude > 0.001f)
        {
            _dashDir = dir;
            _dashTimer = dashDuration;
            _cooldownTimer = dashCooldown;
            _isDashing = true;
        }
    }

    private void UpdateDash(float dt)
    {
        _dashTimer -= dt;

        // Calculate velocity: Distance / Time
        float speed = dashDistance / dashDuration;
        Vector3 move = (_dashDir * speed) + (Vector3.up * verticalVelocity);
        
        cc.Move(move * dt);

        if (_dashTimer <= 0f)
        {
            _isDashing = false;
        }
    }

    private void UpdateNormalMovement(float dt)
    {
        Vector2 input = moveAction.GetAxis(inputSource);
        
        Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 rgt = head.right;   rgt.y = 0f; rgt.Normalize();

        Vector3 horizontal = (fwd * input.y + rgt * input.x) * moveSpeed;
        Vector3 vertical = Vector3.up * verticalVelocity;

        cc.Move((horizontal + vertical) * dt);
    }
}