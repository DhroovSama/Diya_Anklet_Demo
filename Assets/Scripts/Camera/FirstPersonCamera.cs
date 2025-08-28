using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    public enum LookDevice { Unknown, Mouse, Gamepad }

    [Header("Rig")]
    [SerializeField] Transform yawPivot;
    [SerializeField] Transform pitchPivot;
    [SerializeField] Camera fpsCamera;
    [SerializeField] CameraFeelConfig config;
    [SerializeField] CameraAdditiveBus additiveBus;

    [Header("Input (New Input System)")]
    [SerializeField] InputActionReference lookAction;          // Vector2 (Mouse delta / Gamepad rightStick)
    [SerializeField] InputActionReference toggleCursorAction;  // Button (Escape recommended)

    // Fallback actions if refs not assigned.
    InputAction _look, _toggle;
    LookDevice _device = LookDevice.Unknown;

    // Angles & smoothing
    float _targetYaw, _targetPitch;
    float _smYaw, _smPitch;
    float _yawVel, _pitchVel;

    // Bases
    Vector3 _camBaseLocalPos;
    float _baseFov;

    // Debug last frame values
    Vector2 _lastLookDeltaDeg; // degrees this frame (pre-smooth)

    void Reset()
    {
        yawPivot = transform;
        if (transform.childCount > 0) pitchPivot = transform.GetChild(0);
        if (pitchPivot != null && pitchPivot.GetComponentInChildren<Camera>() != null)
            fpsCamera = pitchPivot.GetComponentInChildren<Camera>();
    }

    void Awake()
    {
        if (fpsCamera == null) fpsCamera = GetComponentInChildren<Camera>();
        if (additiveBus == null) additiveBus = GetComponent<CameraAdditiveBus>();
        _camBaseLocalPos = fpsCamera.transform.localPosition;
        _baseFov = config != null ? config.baseFOV : 65f;
        if (fpsCamera != null) fpsCamera.fieldOfView = _baseFov;

        // Configure bus FOV params
        if (config != null && additiveBus != null)
            additiveBus.ConfigureExternalFov(config.externalFOVMax, config.externalFOVEase);

        // Create fallback actions if not wired in inspector.
        if (lookAction == null)
        {
            _look = new InputAction("Look", InputActionType.PassThrough, binding: "");
            _look.AddBinding("<Mouse>/delta");
            _look.AddBinding("<Gamepad>/rightStick");
        }
        else _look = lookAction.action;

        if (toggleCursorAction == null)
        {
            _toggle = new InputAction("ToggleCursor", InputActionType.Button, "<Keyboard>/escape");
        }
        else _toggle = toggleCursorAction.action;

        // Track last active device for scaling rules.
        _look.performed += ctx =>
        {
            var dev = ctx.control?.device;
            if (dev is Mouse) _device = LookDevice.Mouse;
            else if (dev is Gamepad) _device = LookDevice.Gamepad;
        };
    }

    void OnEnable()
    {
        _look?.Enable();
        _toggle?.Enable();
        _toggle.performed += OnToggleCursor;
        if (config != null && config.cursorLockOnStart) LockCursor(true);
    }

    void OnDisable()
    {
        _toggle.performed -= OnToggleCursor;
        _look?.Disable();
        _toggle?.Disable();
        LockCursor(false);
    }

    void OnToggleCursor(InputAction.CallbackContext ctx)
    {
        LockCursor(Cursor.lockState != CursorLockMode.Locked);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        if (_look == null) return;

        Vector2 look = _look.ReadValue<Vector2>();

        // Apply deadzone for sticks.
        if (_device == LookDevice.Gamepad && look.magnitude < (config?.stickDeadzone ?? 0.15f)) look = Vector2.zero;

        // Convert to degrees (per frame for mouse; per second for stick).
        float dxDeg = 0f, dyDeg = 0f;

        if (_device == LookDevice.Gamepad)
        {
            float sx = (config?.stickSensitivityX ?? 220f);
            float sy = (config?.stickSensitivityY ?? 220f);
            dxDeg = look.x * sx * Time.deltaTime;
            dyDeg = look.y * sy * Time.deltaTime;
        }
        else // Mouse or Unknown -> treat as mouse counts
        {
            float mx = (config?.mouseSensitivityX ?? 0.12f);
            float my = (config?.mouseSensitivityY ?? 0.12f);
            dxDeg = look.x * mx;
            dyDeg = look.y * my;
        }

        if (config != null && config.invertY) dyDeg = -dyDeg;

        _lastLookDeltaDeg = new Vector2(dxDeg, dyDeg);

        _targetYaw = WrapAngle(_targetYaw + dxDeg);
        _targetPitch = Mathf.Clamp(_targetPitch - dyDeg, config?.minPitch ?? -88f, config?.maxPitch ?? 88f);

        // Smoothing
        float smooth = (_device == LookDevice.Gamepad) ? (config?.stickSmoothingTime ?? 0.08f)
                                                       : (config?.mouseSmoothingTime ?? 0f);

        _smYaw = Mathf.SmoothDampAngle(_smYaw, _targetYaw, ref _yawVel, smooth);
        _smPitch = Mathf.SmoothDampAngle(_smPitch, _targetPitch, ref _pitchVel, smooth);

        // Calculate look velocity (deg/sec) for additives.
        float yawVel = Mathf.DeltaAngle(_prevYaw, _smYaw) / Mathf.Max(1e-4f, Time.deltaTime);
        float pitchVel = Mathf.DeltaAngle(_prevPitch, _smPitch) / Mathf.Max(1e-4f, Time.deltaTime);
        if (additiveBus != null) additiveBus.LookVelocityDegPerSec = new Vector2(yawVel, pitchVel);

        _prevYaw = _smYaw; _prevPitch = _smPitch;
    }

    float _prevYaw, _prevPitch;

    void LateUpdate()
    {
        // Apply base rotations
        if (yawPivot != null) yawPivot.localRotation = Quaternion.Euler(0f, _smYaw, 0f);
        if (pitchPivot != null) pitchPivot.localRotation = Quaternion.Euler(_smPitch, 0f, 0f);

        // Additives (pos/euler/FOV)
        if (additiveBus != null && fpsCamera != null)
        {
            additiveBus.SampleAdditives(Time.deltaTime, out var p, out var e, out var fovOff);

            // Position offset
            fpsCamera.transform.localPosition = _camBaseLocalPos + p;

            // Rotation offset (apply as additional local rotation on PitchPivot's child)
            fpsCamera.transform.localRotation = Quaternion.Euler(e) * Quaternion.identity;

            // FOV offset
            fpsCamera.fieldOfView = (_baseFov + fovOff);
        }
    }

    static float WrapAngle(float a)
    {
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

    // Public bridge for external systems (optional sugar)
    public void SetExternalFovTarget(float offset) => additiveBus?.SetExternalFovTarget(offset);
    public void PushImpulse(Vector3 posMag, Vector3 rotMag, float duration) => additiveBus?.PushImpulse(posMag, rotMag, duration);

    // Debug getters
    public Vector2 LastLookDeltaDegrees => _lastLookDeltaDeg;
    public LookDevice Device => _device;
    public float Yaw => _smYaw;
    public float Pitch => _smPitch;
}
