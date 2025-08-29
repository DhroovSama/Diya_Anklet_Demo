using UnityEngine;

[RequireComponent(typeof(CameraAdditiveBus))]
public class LookSwayAdditive : MonoBehaviour, ICameraAdditive
{
    [SerializeField] CameraFeelConfig config;
    [SerializeField, Range(0f, 1.5f)] float maxRollDegOverride = 0.25f;      // tiny default
    [SerializeField, Range(1f, 30f)] float responsivenessOverride = 14f;    // snappier = less mush
    [SerializeField] bool useConfigValues = true;

    CameraAdditiveBus _bus;
    Vector2 _velFiltered;

    void Awake() { _bus = GetComponent<CameraAdditiveBus>(); }
    void OnEnable() { _bus.Register(this); }
    void OnDisable() { _bus.Unregister(this); }

    public void Sample(float dt, out Vector3 posOffset, out Vector3 eulerOffset, out float fovOffset)
    {
        posOffset = Vector3.zero; eulerOffset = Vector3.zero; fovOffset = 0f;

        float maxRoll = useConfigValues && config != null ? config.lookSwayMaxRoll : maxRollDegOverride;
        float resp = useConfigValues && config != null ? config.lookSwayResponsiveness : responsivenessOverride;

        var target = _bus.LookVelocityDegPerSec;
        float k = Mathf.Exp(-resp * dt);
        _velFiltered = Vector2.Lerp(target, _velFiltered, k);

        float roll = Mathf.Clamp(-_velFiltered.x * 0.008f, -maxRoll, maxRoll);
        float pitch = Mathf.Clamp(_velFiltered.y * 0.003f, -maxRoll, maxRoll);

        eulerOffset = new Vector3(pitch, 0f, roll);
    }
}
