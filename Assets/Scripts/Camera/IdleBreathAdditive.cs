using UnityEngine;

[RequireComponent(typeof(CameraAdditiveBus))]
public class IdleBreathAdditive : MonoBehaviour, ICameraAdditive
{
    [SerializeField] CameraFeelConfig config;
    [Tooltip("If true, always active until locomotion wires speed.")]
    [SerializeField] bool forceActive = true;

    float _t;
    CameraAdditiveBus _bus;

    void Awake() { _bus = GetComponent<CameraAdditiveBus>(); }
    void OnEnable() { _bus.Register(this); }
    void OnDisable() { _bus.Unregister(this); }

    public void Sample(float dt, out Vector3 posOffset, out Vector3 eulerOffset, out float fovOffset)
    {
        posOffset = Vector3.zero; eulerOffset = Vector3.zero; fovOffset = 0f;
        if (!forceActive) return;

        _t += dt * Mathf.Max(0.01f, config.idleBreathHz);
        float y = Mathf.Sin(_t * Mathf.PI * 2f) * config.idleBreathAmplitude;
        posOffset = new Vector3(0f, y, 0f);
    }
}
