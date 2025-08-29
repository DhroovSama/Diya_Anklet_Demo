using UnityEngine;

[CreateAssetMenu(fileName = "DiyaConfig", menuName = "Feel/Diya Config")]
public class DiyaConfig : ScriptableObject
{
    [Header("Intensity Levels (Logical)")]
    [Range(0f, 2f)] public float litIntensity = 1.0f;
    [Range(0f, 2f)] public float coveredIntensity = 0.10f;
    [Range(0f, 2f)] public float extinguishedIntensity = 0f;

    [Header("Timings (seconds)")]
    [Range(0.05f, 1.0f)] public float coverLerpTime = 0.25f;
    [Range(0.5f, 6f)] public float snuffTimeTarget = 2.5f;
    [Range(0.2f, 5f)] public float coolTime = 2.0f;

    [Header("Simple Flicker (legacy)")]
    [Range(0f, 0.5f)] public float flickerAmplitude = 0.05f;
    [Range(0.1f, 20f)] public float flickerHz = 7.5f;

    [Header("Advanced Flicker")]
    public bool useAdvancedFlicker = true;
    [Tooltip("Multiplies all advanced flicker effects.")]
    [Range(0f, 1.5f)] public float flickerStrength = 1.0f;

    [Tooltip("Three-band fBM base rates (Hz) and weights (sum ~= 1).")]
    [Range(0.1f, 10f)] public float baseFlickerHz = 2.5f;
    [Range(0.1f, 40f)] public float midFlickerHz = 9f;
    [Range(0.1f, 80f)] public float highFlickerHz = 25f;
    [Range(0f, 1f)] public float lowWeight = 0.6f;
    [Range(0f, 1f)] public float midWeight = 0.3f;
    [Range(0f, 1f)] public float highWeight = 0.1f;

    [Tooltip("Skew distribution of noise: -1 = more dips (darker), +1 = more bright spikes.")]
    [Range(-1f, 1f)] public float skew = -0.3f;

    [Header("Optional Micro-Spikes")]
    public bool enableSpikes = true;
    [Tooltip("Expected spikes per second at steady flame.")]
    [Range(0f, 6f)] public float spikeChancePerSecond = 0.6f;
    [Tooltip("Spike height as a fraction of current base intensity.")]
    [Range(0f, 0.6f)] public float spikeMagnitude = 0.25f;
    [Range(0.01f, 0.25f)] public float spikeMinDuration = 0.03f;
    [Range(0.02f, 0.35f)] public float spikeMaxDuration = 0.10f;

    [Header("Light (Unity Light)")]
    public bool autoCreateLight = true;
    public LightType lightType = LightType.Point;
    [ColorUsage(false, true)] public Color lightColor = new Color(1.0f, 0.86f, 0.55f, 1f);
    [Range(0.5f, 20f)] public float lightRange = 6f;
    [Range(0f, 20f)] public float lightIntensityScale = 4.0f;
    public LightShadows shadowType = LightShadows.Soft;
    [Range(0f, 1f)] public float shadowStrength = 0.8f;

    [Header("Range/Color Jitter (subtle)")]
    [Tooltip("Extra range wobble fraction (0.0–0.1 safe).")]
    [Range(0f, 0.15f)] public float rangeJitter = 0.05f;
    [Tooltip("Warm color shift intensity (0–0.1 is subtle).")]
    [Range(0f, 0.15f)] public float colorJitter = 0.02f;

    [Header("Light / Emissive Targets (optional)")]
    public Light targetLight;
    public Renderer emissiveRenderer;
    [ColorUsage(true, true)] public Color emissiveColor = Color.white;
    [Range(0f, 10f)] public float emissiveScale = 1.5f;

    [Header("Gizmos")]
    public bool drawRangeGizmo = true;
    public Color rangeGizmoColor = new Color(1f, 0.75f, 0.3f, 0.25f);
}
