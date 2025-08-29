using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMover : MonoBehaviour, IPlayerVelocityProvider
{
    [Header("Refs")]
    [SerializeField] Transform yawPivot;               // MUST be the same yaw pivot the camera rotates
    [SerializeField] MovementConfig config;
    [SerializeField] CharacterController controller;

    [Header("Input (New Input System)")]
    [SerializeField] InputActionReference moveAction;  // Vector2 (WASD / leftStick)
    [SerializeField] InputActionReference sprintAction; // Button (Shift / leftStickPress) — optional

    // Fallback actions if not provided
    InputAction _move, _sprint;

    // State
    Vector3 _hVel;             // horizontal velocity (XZ)
    Vector3 _hVelSmoothRef;    // ref for SmoothDamp
    float _vVel;             // vertical velocity (Y)
    Vector3 _groundNormal = Vector3.up;
    float _groundAngleDeg;
    bool _isGrounded, _validGround;

    // Cached
    Vector3 _prevPos;
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded => _isGrounded;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
        if (yawPivot == null) yawPivot = transform; // better to assign explicitly
    }

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        // Create fallback input actions if inspector refs are empty
        if (moveAction == null)
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");
            _move.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }
        else _move = moveAction.action;

        if (sprintAction == null)
        {
            _sprint = new InputAction("Sprint", InputActionType.Button);
            _sprint.AddBinding("<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftStickPress");
        }
        else _sprint = sprintAction.action;

        _prevPos = transform.position;
    }

    void OnEnable()
    {
        _move?.Enable();
        _sprint?.Enable();
    }

    void OnDisable()
    {
        _move?.Disable();
        _sprint?.Disable();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1) INPUT
        Vector2 inMove = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

        // Apply circular deadzone for stick (keyboard unaffected)
        if (Gamepad.current != null && _move.activeControl != null && _move.activeControl.device is Gamepad)
        {
            float m = inMove.magnitude;
            if (m < (config?.stickDeadzone ?? 0.15f)) inMove = Vector2.zero;
            else inMove = inMove.normalized * Mathf.InverseLerp(config.stickDeadzone, 1f, m);
        }
        inMove = Vector2.ClampMagnitude(inMove, 1f);

        bool wantSprint = config != null && config.enableSprint && _sprint != null && _sprint.IsPressed();

        // 2) DESIRED HORIZONTAL VELOCITY (camera-relative, ground-projected)
        Vector3 camRight = yawPivot != null ? yawPivot.right : transform.right;
        Vector3 camFwd = yawPivot != null ? yawPivot.forward : transform.forward;
        camRight.y = 0f; camFwd.y = 0f;
        camRight.Normalize(); camFwd.Normalize();

        Vector3 dir = (camRight * inMove.x + camFwd * inMove.y);
        if (dir.sqrMagnitude > 0f) dir.Normalize();

        float targetSpeed = (wantSprint ? (config?.runSpeed ?? 4.2f) : (config?.walkSpeed ?? 2.8f));
        Vector3 targetHVel = dir * targetSpeed;

        // Ground project direction if valid ground detected
        if (_validGround && targetHVel.sqrMagnitude > 0f)
        {
            targetHVel = Vector3.ProjectOnPlane(targetHVel, _groundNormal).normalized * targetSpeed;
        }

        // 3) ACCEL / DECEL
        float smoothTime = (dir.sqrMagnitude > 0.0001f) ? (config?.accelTime ?? 0.06f)
                                                        : (config?.decelTime ?? 0.10f);
        _hVel = Vector3.SmoothDamp(_hVel, targetHVel, ref _hVelSmoothRef, Mathf.Max(0.0001f, smoothTime));

        // Clamp to target speed (protect against overshoot on steep planes)
        if (_hVel.sqrMagnitude > (targetSpeed * targetSpeed))
            _hVel = _hVel.normalized * targetSpeed;

        // 4) VERTICAL
        GroundProbe(); // refresh _isGrounded, _validGround, _groundNormal, _groundAngleDeg

        if (_isGrounded)
        {
            // Stick to ground to kill micro-floating on edges/ramps.
            _vVel = Mathf.Max(_vVel, (config?.stickToGroundForce ?? -2f));
        }
        else
        {
            _vVel += (config?.gravity ?? -19.6f) * dt;
        }

        // 5) MOVE
        Vector3 velocity = _hVel + Vector3.up * _vVel;
        controller.Move(velocity * dt);

        // Recompute grounded after movement; apply small downward bias if we landed
        GroundProbe();
        if (_isGrounded && _vVel < 0f)
        {
            _vVel = (config?.stickToGroundForce ?? -2f);
        }

        // 6) OUTPUT VELOCITY (for camera, analytics)
        Vector3 worldDelta = transform.position - _prevPos;
        Velocity = worldDelta / dt;
        _prevPos = transform.position;
    }

    void GroundProbe()
    {
        // Start just above the controller bottom; cast down
        float radius = Mathf.Max(0.01f, controller.radius - (config?.groundProbeRadiusPadding ?? 0.05f));
        float skin = controller.skinWidth;
        Vector3 feet = new Vector3(controller.bounds.center.x, controller.bounds.min.y + radius + skin + 0.01f, controller.bounds.center.z);

        float maxDist = (config?.groundCheckDistance ?? 0.18f) + 0.1f;
        if (Physics.SphereCast(feet, radius, Vector3.down, out var hit, maxDist, ~0, QueryTriggerInteraction.Ignore))
        {
            _groundNormal = hit.normal;
            _groundAngleDeg = Vector3.Angle(_groundNormal, Vector3.up);

            float slopeLimit = controller.slopeLimit > 0 ? controller.slopeLimit : 45f;
            _validGround = _groundAngleDeg <= slopeLimit + 0.5f;

            // "Grounded" if close enough and moving downward / resting
            bool nearFeet = hit.distance <= (config?.groundCheckDistance ?? 0.18f) + 0.02f;
            _isGrounded = nearFeet && _validGround && _vVel <= 0.5f;
        }
        else
        {
            _groundNormal = Vector3.up;
            _groundAngleDeg = 0f;
            _validGround = false;
            _isGrounded = controller.isGrounded; // fallback
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (controller == null) return;
        float radius = Mathf.Max(0.01f, controller.radius - (config != null ? config.groundProbeRadiusPadding : 0.05f));
        float skin = controller.skinWidth;
        Vector3 feet = new Vector3(controller.bounds.center.x, controller.bounds.min.y + radius + skin + 0.01f, controller.bounds.center.z);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(feet, radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(feet, feet + Vector3.down * (config != null ? config.groundCheckDistance : 0.18f));
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.05f, _groundNormal * 0.6f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f,
            $"Grounded: {_isGrounded}\nAngle: {_groundAngleDeg:0.0}°");
    }
#endif
}
