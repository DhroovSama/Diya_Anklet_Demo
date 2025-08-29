using UnityEngine;

public class DiyaLightDebugHUD : MonoBehaviour
{
    [SerializeField] DiyaLightController diya;
    [SerializeField] bool visible = true;
    float _ema;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4)) visible = !visible;
        _ema = Mathf.Lerp(_ema <= 0f ? Time.deltaTime : _ema, Time.deltaTime, 0.1f);
    }

    void OnGUI()
    {
        if (!visible || diya == null) return;
        var s = GUI.skin.label; int prev = s.fontSize; s.fontSize = 14;
        GUILayout.BeginArea(new Rect(12, 380, 360, 130), GUI.skin.box);
        GUILayout.Label($"FPS: {1f / _ema:0.0} | ms: {_ema * 1000f:0.0}");
        GUILayout.Label($"State: {diya.State}");
        GUILayout.Label($"Intensity01: {diya.Intensity01:0.00}  Heat01: {diya.Heat01:0.00}");
        GUILayout.Label($"Cover: RMB/Left Trigger  |  Relight (Debug): R");
        GUILayout.EndArea();
        s.fontSize = prev;
    }
}
