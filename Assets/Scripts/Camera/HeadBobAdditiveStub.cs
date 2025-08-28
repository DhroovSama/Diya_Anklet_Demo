using UnityEngine;

[RequireComponent(typeof(CameraAdditiveBus))]
public class HeadBobAdditiveStub : MonoBehaviour, ICameraAdditive
{
    CameraAdditiveBus _bus;
    void Awake() { _bus = GetComponent<CameraAdditiveBus>(); }
    void OnEnable() { _bus.Register(this); }
    void OnDisable() { _bus.Unregister(this); }

    public void Sample(float dt, out Vector3 posOffset, out Vector3 eulerOffset, out float fovOffset)
    {
        // Intentionally zero until locomotion wires a velocity provider.
        posOffset = Vector3.zero; eulerOffset = Vector3.zero; fovOffset = 0f;
    }
}
