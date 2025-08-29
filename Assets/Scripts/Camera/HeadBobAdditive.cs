using UnityEngine;

[RequireComponent(typeof(CameraAdditiveBus))]
public class HeadBobAdditive : MonoBehaviour, ICameraAdditive
{
    [Header("Refs")]
    [SerializeField] CameraFeelConfig config; // uses headBobAmplitudeMax
    [Tooltip("Optional. If null, will search up the hierarchy.")]
    [SerializeField] MonoBehaviour velocityProviderOverride; // must implement IPlayerVelocityProvider

    [Header("Bob Shape")]
    [Tooltip("Speed at which bob reaches full amplitude (m/s).")]
    [SerializeField, Range(0.5f, 8f)] float speedAtFullBob = 3.5f;
    [Tooltip("Minimum speed before bob starts (m/s).")]
    [SerializeField, Range(0f, 1f)] float minSpeedThreshold = 0.35f;
    [Tooltip("Bob frequency at full speed (Hz).")]
    [SerializeField, Range(0.5f, 4f)] float hzAtFullBob = 1.40f;
    [Tooltip("Roll max at full speed (degrees). Keep tiny to avoid nausea.")]
    [SerializeField, Range(0f, 1.5f)] float rollMaxDeg = 0.18f;
    [Tooltip("Lateral sway fraction of vertical amplitude.")]
    [SerializeField, Range(0f, 1f)] float lateralFrac = 0.10f;

    [Header("Smoothing")]
    [Tooltip("Upward amplitude rise time (sec).")]
    [SerializeField, Range(0.01f, 0.3f)] float riseTime = 0.08f;
    [Tooltip("Downward amplitude fall time (sec).")]
    [SerializeField, Range(0.01f, 0.4f)] float fallTime = 0.14f;

    [Header("Landing Thud")]
    [SerializeField, Range(0.5f, 8f)] float minLandingSpeedForThud = 1.5f; // m/s (vertical)
    [SerializeField, Range(1f, 12f)] float maxLandingSpeedForThud = 6.0f; // m/s (vertical)
    [SerializeField, Range(0.001f, 0.03f)] float thudPosAtMin = 0.004f;   // meters (down impulse)
    [SerializeField, Range(0.001f, 0.05f)] float thudPosAtMax = 0.010f;   // meters
    [SerializeField, Range(0.03f, 0.20f)] float thudDuration = 0.085f;    // seconds
    [SerializeField, Range(0f, 0.8f)] float thudCooldown = 0.20f;         // prevents spam on slopes

    CameraAdditiveBus _bus;
    IPlayerVelocityProvider _velocity;
    float _phase;           // 0..1
    float _amp;             // current amplitude (meters)
    float _ampVel;          // smooth damp ref
    float _sinceThud;

    // Cached last-frame states for landing detection
    bool _wasGrounded;
    float _lastVy;

    void Awake()
    {
        _bus = GetComponent<CameraAdditiveBus>();
    }

    void OnEnable()
    {
        _bus.Register(this);
        if (velocityProviderOverride != null)
            _velocity = velocityProviderOverride as IPlayerVelocityProvider;

        if (_velocity == null)
            _velocity = GetComponentInParent<IPlayerVelocityProvider>();

        _wasGrounded = _velocity != null && _velocity.IsGrounded;
        _lastVy = _velocity != null ? _velocity.Velocity.y : 0f;
        _sinceThud = thudCooldown;
    }

    void OnDisable()
    {
        _bus.Unregister(this);
    }

    public void Sample(float dt, out Vector3 posOffset, out Vector3 eulerOffset, out float fovOffset)
    {
        posOffset = Vector3.zero; eulerOffset = Vector3.zero; fovOffset = 0f;
        if (_velocity == null || config == null) return;

        // --- Speed & grounding ---
        Vector3 v = _velocity.Velocity;
        float horizontalSpeed = new Vector2(v.x, v.z).magnitude;
        bool grounded = _velocity.IsGrounded;

        // Normalized speed (0..1) across threshold → full
        float s0 = Mathf.Min(speedAtFullBob, Mathf.Max(minSpeedThreshold + 0.0001f, speedAtFullBob));
        float n = Mathf.InverseLerp(minSpeedThreshold, s0, horizontalSpeed);
        n = Mathf.Clamp01(n);

        // Target amplitude (meters) — calmer default from config
        float Amax = Mathf.Clamp(config.headBobAmplitudeMax, 0f, 0.1f); // recommend 0.006–0.010
        float targetAmp = grounded ? Amax * Smooth01(n) : 0f; // smoothstep across speed

        // Smooth amplitude with separate rise/fall times
        float tSmooth = (targetAmp > _amp) ? Mathf.Max(0.01f, riseTime) : Mathf.Max(0.01f, fallTime);
        _amp = Mathf.SmoothDamp(_amp, targetAmp, ref _ampVel, tSmooth);

        // Frequency (Hz) scales with speed
        float hz = Mathf.Lerp(0f, hzAtFullBob, n);
        _phase += hz * dt;
        if (_phase > 1f) _phase -= Mathf.Floor(_phase); // wrap

        // --- Waveform ---
        float s1 = Mathf.Sin(_phase * Mathf.PI * 2f); // 1×
        float s2 = Mathf.Sin(_phase * Mathf.PI * 4f); // 2× (alternating steps)
        float y = _amp * s1;
        float x = _amp * lateralFrac * s2;
        float roll = rollMaxDeg * n * s2;
        roll = Mathf.Clamp(roll, -0.5f, 0.5f);

        posOffset = new Vector3(x, y, 0f);
        eulerOffset = new Vector3(0f, 0f, roll);

        // --- Landing thud (impulse), on air→ground transition ---
        _sinceThud += dt;
        if (!_wasGrounded && grounded && _sinceThud >= thudCooldown)
        {
            float impactSpeed = -_lastVy; // downward velocity is negative; invert
            if (impactSpeed > minLandingSpeedForThud)
            {
                float t = Mathf.InverseLerp(minLandingSpeedForThud, maxLandingSpeedForThud, impactSpeed);
                float mag = Mathf.Lerp(thudPosAtMin, thudPosAtMax, Mathf.SmoothStep(0f, 1f, t));
                _bus.PushImpulse(new Vector3(0f, -mag, 0f), Vector3.zero, thudDuration);
                _sinceThud = 0f;
            }
        }

        _wasGrounded = grounded;
        _lastVy = v.y;
    }

    static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxLandingSpeedForThud < minLandingSpeedForThud)
            maxLandingSpeedForThud = minLandingSpeedForThud + 0.1f;
        speedAtFullBob = Mathf.Max(speedAtFullBob, minSpeedThreshold + 0.01f);
        lateralFrac = Mathf.Clamp01(lateralFrac);
        riseTime = Mathf.Max(0.01f, riseTime);
        fallTime = Mathf.Max(0.01f, fallTime);
        thudDuration = Mathf.Max(0.03f, thudDuration);
    }
#endif
}
