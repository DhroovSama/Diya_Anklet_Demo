using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(ThreatMeter))]
public class ThreatFeedbackDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] ThreatMeter meter;
    [SerializeField] CameraAdditiveBus camBus;

    [Header("FOV Pump")]
    [Tooltip("Max extra FOV (degrees) added at high threat.")]
    [Range(0f, 15f)] public float fovMax = 3f;
    [Tooltip("FOV starts ramping at this threat.")]
    [Range(0f, 1f)] public float fovStart = 0.7f;

    [Header("Post FX (URP Volume)")]
    public Volume volume; // Should have Vignette / ChromaticAberration / FilmGrain overrides
    [Range(0f, 1f)] public float vignetteMin = 0.20f;
    [Range(0f, 1f)] public float vignetteMax = 0.55f;
    [Range(0f, 1f)] public float vignetteStart = 0.30f;
    [Range(0f, 1f)] public float vignetteEnd = 0.90f;

    [Range(0f, 1f)] public float caMax = 0.18f;
    [Range(0f, 1f)] public float caStart = 0.60f;

    [Range(0f, 1f)] public float grainMax = 0.35f;
    [Range(0f, 1f)] public float grainStart = 0.50f;

    Vignette _vig;
    ChromaticAberration _ca;
    FilmGrain _grain;

    [Header("Heartbeat")]
    [Tooltip("A single 'thump' clip (short). Plays at BPM mapped from threat.")]
    public AudioSource heartbeatSource;
    public AudioClip heartbeatClip;
    [Range(30f, 200f)] public float bpmMin = 60f;
    [Range(30f, 200f)] public float bpmMax = 140f;
    [Range(0f, 1f)] public float heartbeatVolumeAtMax = 0.9f;
    [Tooltip("Threat at which heartbeat begins to be audible.")]
    [Range(0f, 1f)] public float heartbeatAudibleStart = 0.30f;
    double _nextBeatDSP;

    [Header("Audio Mixer (Ambience LPF)")]
    public AudioMixer mixer;
    [Tooltip("Exposed parameter name for ambience lowpass cutoff (Hz).")]
    public string mixerLPFParam = "Ambience_LPF_Hz";
    [Tooltip("Exposed parameter name for ambience ducking (dB). Leave empty to ignore.")]
    public string mixerDuckParam = "Ambience_Duck_dB";
    public float lpfHiHz = 18000f;
    public float lpfLoHz = 1200f;
    [Range(0f, 1f)] public float lpfStart = 0.40f; // begins closing LPF
    [Range(0f, 1f)] public float duckStart = 0.80f; // begins ducking ambience
    public float duckMaxDb = -2.5f;

    void Reset()
    {
        meter = GetComponent<ThreatMeter>();
    }

    void Awake()
    {
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out _vig);
            volume.profile.TryGet(out _ca);
            volume.profile.TryGet(out _grain);
        }
    }

    void OnEnable()
    {
        if (heartbeatSource != null && heartbeatClip != null)
        {
            heartbeatSource.clip = heartbeatClip;
            heartbeatSource.playOnAwake = false;
            heartbeatSource.loop = false;
            _nextBeatDSP = AudioSettings.dspTime + 0.1;
        }
    }

    void Update()
    {
        float t = meter != null ? meter.Threat01 : 0f;

        // --- FOV pump via CameraAdditiveBus
        if (camBus != null)
        {
            float f = Mathf.InverseLerp(fovStart, 1f, t);
            camBus.SetExternalFovTarget(Mathf.Lerp(0f, fovMax, Mathf.Clamp01(f)));
        }

        // --- Post FX
        if (_vig != null)
        {
            float v = Mathf.Lerp(vignetteMin, vignetteMax, Mathf.InverseLerp(vignetteStart, vignetteEnd, t));
            _vig.intensity.Override(v);
        }
        if (_ca != null)
        {
            float c = Mathf.Lerp(0f, caMax, Mathf.InverseLerp(caStart, 1f, t));
            _ca.intensity.Override(c);
        }
        if (_grain != null)
        {
            float g = Mathf.Lerp(0f, grainMax, Mathf.InverseLerp(grainStart, 1f, t));
            _grain.intensity.Override(g);
        }

        // --- Heartbeat (tempo + loudness)
        if (heartbeatSource != null && heartbeatClip != null)
        {
            float hb = Mathf.Lerp(bpmMin, bpmMax, t);              // BPM
            float interval = 60f / Mathf.Max(1f, hb);              // seconds per beat

            // volume fade-in after threshold
            float v = Mathf.InverseLerp(heartbeatAudibleStart, 1f, t) * heartbeatVolumeAtMax;
            heartbeatSource.volume = v;

            // schedule beats precisely using dspTime
            double now = AudioSettings.dspTime;
            if (now + 0.05 >= _nextBeatDSP) // keep slightly ahead
            {
                heartbeatSource.pitch = Mathf.Lerp(0.95f, 1.1f, Mathf.InverseLerp(bpmMin, bpmMax, hb));
                heartbeatSource.PlayScheduled(_nextBeatDSP);
                _nextBeatDSP += interval;
            }
        }

        // --- Audio Mixer LPF + duck
        if (mixer != null)
        {
            float lpf = Mathf.Lerp(lpfHiHz, lpfLoHz, Mathf.InverseLerp(lpfStart, 1f, t));
            if (!string.IsNullOrEmpty(mixerLPFParam)) mixer.SetFloat(mixerLPFParam, lpf);

            if (!string.IsNullOrEmpty(mixerDuckParam))
            {
                float duck = Mathf.Lerp(0f, duckMaxDb, Mathf.InverseLerp(duckStart, 1f, t));
                mixer.SetFloat(mixerDuckParam, duck);
            }
        }
    }
}
