using UnityEngine;

[RequireComponent(typeof(CameraAdditiveBus))]
public class LookSwayAdditive : MonoBehaviour, ICameraAdditive
{
    [SerializeField] CameraFeelConfig config;
    CameraAdditiveBus _bus;

    // Internal velocity filter to avoid noisy roll.
    Vector2 _velFiltered;

    void Awake()
    {
        _bus = GetComponent<CameraAdditiveBus>();
    }
    void OnEnable() { _bus.Register(this); }
    void OnDisable() { _bus.Unregister(this); }

    public void Sample(float dt, out Vector3 posOffset, out Vector3 eulerOffset, out float fovOffset)
    {
        posOffset = Vector3.zero; eulerOffset = Vector3.zero; fovOffset = 0f;
        var target = _bus.LookVelocityDegPerSec;
        // Critically damp velocity a bit for stability.
        float k = Mathf.Exp(-config.lookSwayResponsiveness * dt);
        _velFiltered = Vector2.Lerp(target, _velFiltered, k);

        // Roll by yaw velocity, small pitch nudge by pitch velocity.
        float roll = Mathf.Clamp(-_velFiltered.x * 0.01f, -config.lookSwayMaxRoll, config.lookSwayMaxRoll);
        float pitch = Mathf.Clamp(_velFiltered.y * 0.004f, -config.lookSwayMaxRoll, config.lookSwayMaxRoll);

        eulerOffset = new Vector3(pitch, 0f, roll);
    }
}
