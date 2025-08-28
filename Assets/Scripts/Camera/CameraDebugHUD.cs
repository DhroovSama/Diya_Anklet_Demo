using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraDebugHUD : MonoBehaviour
{
    [SerializeField] FirstPersonCamera cam;
    [SerializeField] CameraAdditiveBus bus;
    [SerializeField] bool visible = true;

    float _deltaAvg;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            visible = !visible;

        // Simple EMA for fps
        _deltaAvg = Mathf.Lerp(_deltaAvg <= 0f ? Time.deltaTime : _deltaAvg, Time.deltaTime, 0.1f);
    }

    void OnGUI()
    {
        if (!visible || cam == null || bus == null) return;

        var s = GUI.skin.label;
        var old = s.fontSize; s.fontSize = 14;
        GUILayout.BeginArea(new Rect(12, 12, 380, 200), GUI.skin.box);
        GUILayout.Label($"FPS: {1f / _deltaAvg:0.0} | ms: {_deltaAvg * 1000f:0.0}");
        GUILayout.Label($"Device: {cam.Device}");
        GUILayout.Label($"Yaw: {cam.Yaw:0.0}°  Pitch: {cam.Pitch:0.0}°");
        GUILayout.Label($"Look Δ (deg): {cam.LastLookDeltaDegrees.x:0.00}, {cam.LastLookDeltaDegrees.y:0.00}");
        GUILayout.Space(6);
        GUILayout.Label($"Additive pos: {bus.LastPosOffset}");
        GUILayout.Label($"Additive euler: {bus.LastEulerOffset}");
        GUILayout.Label($"Additive FOV: {bus.LastFovOffset:0.00}");
        GUILayout.EndArea();
        s.fontSize = old;
    }
}
