using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DiyaLightController : MonoBehaviour, IDiyaLight
{
    public enum DiyaState { Lit, Covered, Extinguished }

    [Header("Refs")]
    [SerializeField] DiyaConfig config;

    [Header("Input (New Input System)")]
    [SerializeField] InputActionReference coverAction;        // hold to cover
    [SerializeField] InputActionReference relightDebugAction; // R to relight

    InputAction _cover, _relight;

    [Header("Debug")]
    [SerializeField] bool startLit = true;

    // State
    public DiyaState State { get; private set; }
    public bool IsLit => State == DiyaState.Lit;
    public bool IsCovered => State == DiyaState.Covered;
    public bool IsExtinguished => State == DiyaState.Extinguished;
    public float Intensity01 { get; private set; } // 0..1
    public float Heat01 { get; private set; }      // 0..1

    public event Action<DiyaState> OnStateChanged;
    public event Action OnExtinguished;
    public event Action OnRelit;

    float _currentIntensityAbs; // logical intensity (pre-scale)
    float _heatRatePerSec, _coolRatePerSec;

    // Light & MPB
    Light _runtimeLight;
    MaterialPropertyBlock _mpb;

    // Advanced flicker state
    float _tLow, _tMid, _tHigh;
    float _seedLow, _seedMid, _seedHigh;
    System.Random _rng;
    // Micro-spike
    float _spikeT, _spikeDur, _spikeAmp;
    bool _spikeActive;

    void Awake()
    {
        if (config == null)
        {
            Debug.LogError("DiyaLightController: Missing DiyaConfig.", this);
            enabled = false; return;
        }

        _mpb = new MaterialPropertyBlock();
        _rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
        _seedLow = (float)_rng.NextDouble() * 1000f + 13.1f;
        _seedMid = (float)_rng.NextDouble() * 1000f + 27.3f;
        _seedHigh = (float)_rng.NextDouble() * 1000f + 41.7f;

        _heatRatePerSec = 1f / Mathf.Max(0.01f, config.snuffTimeTarget);
        _coolRatePerSec = 1f / Mathf.Max(0.01f, config.coolTime);

        // Input fallbacks
        _cover = coverAction ? coverAction.action : new InputAction("Cover", InputActionType.Button, "<Mouse>/rightButton");
        if (!coverAction) { _cover.AddBinding("<Gamepad>/leftTrigger").WithInteraction("Press(pressPoint=0.4)"); }
        _relight = relightDebugAction ? relightDebugAction.action : new InputAction("DebugRelight", InputActionType.Button, "<Keyboard>/r");

        // Initial state
        SetState(startLit ? DiyaState.Lit : DiyaState.Extinguished, false);
        _currentIntensityAbs = startLit ? config.litIntensity : config.extinguishedIntensity;
        RecomputeNormalized();

        EnsureLight();
        ApplyLightStaticSettings();
        float applied = ComputeAppliedIntensity();
        ApplyLightDynamic(applied, 0f);
        ApplyEmissive(applied);
    }

    void OnEnable()
    {
        _cover?.Enable();
        _relight?.Enable();
        _relight.performed += OnDebugRelight;
    }
    void OnDisable()
    {
        _relight.performed -= OnDebugRelight;
        _cover?.Disable();
        _relight?.Disable();
    }

    void OnDebugRelight(InputAction.CallbackContext _)
    {
        if (IsExtinguished) Relight();
    }

    public void Relight()
    {
        if (!IsExtinguished) return;
        Heat01 = 0f;
        SetState(DiyaState.Lit);
        _currentIntensityAbs = config.litIntensity; // snap for acceptance tests
        RecomputeNormalized();
        float applied = ComputeAppliedIntensity();
        ApplyLightDynamic(applied, 0f);
        ApplyEmissive(applied);
        OnRelit?.Invoke();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1) Input → desired state (unless snuffed)
        bool coverHeld = _cover != null && _cover.IsPressed();
        if (!IsExtinguished)
            SetState(coverHeld ? DiyaState.Covered : DiyaState.Lit);

        // 2) Heat
        if (IsCovered)
        {
            Heat01 = Mathf.Clamp01(Heat01 + _heatRatePerSec * dt);
            if (Heat01 >= 1f)
            {
                SetState(DiyaState.Extinguished);
                _currentIntensityAbs = config.extinguishedIntensity;
                RecomputeNormalized();
                float applied0 = ComputeAppliedIntensity();
                ApplyLightDynamic(applied0, dt);
                ApplyEmissive(applied0);
                OnExtinguished?.Invoke();
                return;
            }
        }
        else Heat01 = Mathf.Clamp01(Heat01 - _coolRatePerSec * dt);

        // 3) Base intensity target and move
        float target = IsExtinguished ? config.extinguishedIntensity
                     : IsCovered ? config.coveredIntensity
                                      : config.litIntensity;

        float maxStep = Mathf.Abs(config.litIntensity - config.coveredIntensity) / Mathf.Max(0.01f, config.coverLerpTime) * dt;
        _currentIntensityAbs = Mathf.MoveTowards(_currentIntensityAbs, target, maxStep);

        // 4) Flicker
        float applied = ComputeAppliedIntensity(); // base logical intensity >= 0
        if (IsLit)
        {
            if (config.useAdvancedFlicker)
                applied = ApplyAdvancedFlicker(applied, dt);
            else
                applied = ApplySimpleFlicker(applied, dt);
        }

        // 5) Drive outputs
        ApplyLightDynamic(applied, dt);
        ApplyEmissive(applied);

        // 6) Normalize for external systems
        RecomputeNormalized();
    }

    float ComputeAppliedIntensity() => Mathf.Max(0f, _currentIntensityAbs);

    float ApplySimpleFlicker(float baseIntensity, float dt)
    {
        if (config.flickerAmplitude <= 0f || config.flickerHz <= 0f) return baseIntensity;
        _tLow += dt * config.flickerHz;
        float n = Mathf.PerlinNoise(_tLow, 0.123f) * 2f - 1f; // -1..1
        return Mathf.Max(0f, baseIntensity + n * config.flickerAmplitude * config.litIntensity);
    }

    float ApplyAdvancedFlicker(float baseIntensity, float dt)
    {
        // Advance times
        _tLow += dt * config.baseFlickerHz;
        _tMid += dt * config.midFlickerHz;
        _tHigh += dt * config.highFlickerHz;

        // 3-band signed Perlin
        float nLow = PerlinSigned(_tLow, _seedLow);
        float nMid = PerlinSigned(_tMid, _seedMid);
        float nHigh = PerlinSigned(_tHigh, _seedHigh);

        // Weighted sum
        float wsum = Mathf.Max(0.0001f, (config.lowWeight + config.midWeight + config.highWeight));
        float n = (config.lowWeight * nLow + config.midWeight * nMid + config.highWeight * nHigh) / wsum;
        n = Mathf.Clamp(n, -1f, 1f);

        // Skew (bias toward dips or peaks)
        n = SkewSigned(n, config.skew);

        // Scale by strength & original flickerAmplitude
        float amp = config.flickerStrength * config.flickerAmplitude * config.litIntensity;
        float result = Mathf.Max(0f, baseIntensity + n * amp);

        // Micro-spike (rare, short, upward)
        if (config.enableSpikes)
        {
            if (!_spikeActive && UnityEngine.Random.value < config.spikeChancePerSecond * dt)
            {
                _spikeActive = true;
                _spikeDur = UnityEngine.Random.Range(config.spikeMinDuration, config.spikeMaxDuration);
                _spikeT = 0f;
                _spikeAmp = baseIntensity * config.spikeMagnitude; // relative to base
            }

            if (_spikeActive)
            {
                _spikeT += dt;
                float a = Mathf.Clamp01(_spikeT / Mathf.Max(0.01f, _spikeDur));
                float w = Mathf.Sin(a * Mathf.PI); // 0..1..0
                result += _spikeAmp * w;
                if (_spikeT >= _spikeDur) _spikeActive = false;
            }
        }

        return result;
    }

    static float PerlinSigned(float t, float seed) => Mathf.PerlinNoise(t, seed) * 2f - 1f;

    // Map signed noise [-1,1] to skewed distribution.
    // skew<0 => more time below 0 (dips); skew>0 => more time above 0 (peaks).
    static float SkewSigned(float x, float skew)
    {
        x = Mathf.Clamp(x, -1f, 1f);
        float u = (x + 1f) * 0.5f; // 0..1
        // Map skew (-1..1) -> gamma (2.5..0.6) [>1 biases toward 0 (darker), <1 toward 1 (brighter)]
        float gamma = Mathf.Lerp(2.5f, 0.6f, (skew + 1f) * 0.5f);
        u = Mathf.Pow(u, gamma);
        return u * 2f - 1f;
    }

    void SetState(DiyaState s, bool invokeEvent = true)
    {
        if (State == s) return;
        State = s;
        if (invokeEvent) OnStateChanged?.Invoke(State);
    }

    void RecomputeNormalized()
    {
        float max = Mathf.Max(0.0001f, config.litIntensity);
        Intensity01 = Mathf.Clamp01(_currentIntensityAbs / max);
    }

    void EnsureLight()
    {
        if (config.targetLight != null) { _runtimeLight = config.targetLight; return; }
        _runtimeLight = GetComponentInChildren<Light>();
        if (_runtimeLight != null) { config.targetLight = _runtimeLight; return; }
        if (config.autoCreateLight)
        {
            var go = new GameObject("DiyaPointLight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.15f;
            _runtimeLight = go.AddComponent<Light>();
            config.targetLight = _runtimeLight;
        }
    }

    void ApplyLightStaticSettings()
    {
        if (_runtimeLight == null) return;
        _runtimeLight.type = config.lightType;
        _runtimeLight.color = config.lightColor;
        _runtimeLight.range = config.lightRange;
        _runtimeLight.shadows = config.shadowType;
        _runtimeLight.shadowStrength = config.shadowStrength;
    }

    void ApplyLightDynamic(float appliedLogicalIntensity, float dt)
    {
        if (_runtimeLight == null) return;

        // Intensity
        _runtimeLight.intensity = config.lightIntensityScale * appliedLogicalIntensity;
        _runtimeLight.enabled = appliedLogicalIntensity > 0.001f;

        // Range jitter (very subtle, tied to low band)
        if (config.rangeJitter > 0f)
        {
            float rn = Mathf.PerlinNoise(_tLow * 0.35f, 87.1f) * 2f - 1f;
            float rj = 1f + rn * config.rangeJitter; // ±fraction
            _runtimeLight.range = Mathf.Max(0.1f, config.lightRange * rj);
        }
        else
        {
            _runtimeLight.range = config.lightRange * Mathf.Lerp(0.9f, 1.0f, Intensity01);
        }

        // Warm color jitter (subtle)
        if (config.colorJitter > 0f)
        {
            float cn = Mathf.PerlinNoise(_tMid * 0.5f, 191.3f) * 2f - 1f; // -1..1
            Color warm = new Color(1f, 0.62f, 0.35f);
            float w = Mathf.Clamp01(0.5f + 0.5f * cn) * config.colorJitter;
            _runtimeLight.color = Color.Lerp(config.lightColor, warm, w);
        }
    }

    void ApplyEmissive(float appliedLogicalIntensity)
    {
        if (config.emissiveRenderer == null) return;
        var emiss = config.emissiveColor * (appliedLogicalIntensity * config.emissiveScale);
        config.emissiveRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_EmissiveColor", emiss);
        _mpb.SetColor("_EmissionColor", emiss);
        config.emissiveRenderer.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (config == null || !config.drawRangeGizmo) return;
        Gizmos.color = config.rangeGizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(Vector3.up * 0.15f, Mathf.Min(0.05f * config.lightRange, 0.25f));
        UnityEditor.Handles.color = new Color(config.rangeGizmoColor.r, config.rangeGizmoColor.g, config.rangeGizmoColor.b, 0.6f);
        UnityEditor.Handles.DrawWireDisc(transform.position + Vector3.up * 0.02f, Vector3.up, config.lightRange);
    }
#endif
}
