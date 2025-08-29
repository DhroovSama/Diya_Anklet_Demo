using UnityEngine;

[RequireComponent(typeof(CameraAdditiveBus))]
public class IdleBreathAdditive : MonoBehaviour, ICameraAdditive
{
    [SerializeField] CameraFeelConfig config;
    [Tooltip("If true, always active (useful before locomotion exists).")]
    [SerializeField] bool forceActive = false;

    [Tooltip("Optional: if assigned and implements IPlayerVelocityProvider, breath will auto-disable while moving.")]
    [SerializeField] MonoBehaviour velocityProviderOverride;

    float _t;
    CameraAdditiveBus _bus;
    IPlayerVelocityProvider _velProv;

    void Awake() { _bus = GetComponent<CameraAdditiveBus>(); }

    void OnEnable()
    {
        _bus.Register(this);
        if (velocityProviderOverride != null)
            _velProv = velocityProviderOverride as IPlayerVelocityProvider;
        if (_velProv == null)
            _velProv = GetComponentInParent<IPlayerVelocityProvider>();
    }

    void OnDisable() { _bus.Unregister(this); }

    public void Sample(float dt, out Vector3 posOffset, out Vector3 eulerOffset, out float fovOffset)
    {
        posOffset = Vector3.zero; eulerOffset = Vector3.zero; fovOffset = 0f;
        if (config == null) return;

        bool moving = false;
        if (_velProv != null)
        {
            var v = _velProv.Velocity;
            moving = new Vector2(v.x, v.z).magnitude > 0.2f; // yield if moving
        }

        if (!forceActive && moving) return;

        _t += dt * Mathf.Max(0.01f, config.idleBreathHz);
        float y = Mathf.Sin(_t * Mathf.PI * 2f) * config.idleBreathAmplitude;
        posOffset = new Vector3(0f, y, 0f);
    }
}
