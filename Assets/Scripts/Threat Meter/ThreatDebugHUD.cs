using UnityEngine;

[RequireComponent(typeof(ThreatMeter))]
public class ThreatDebugHUD : MonoBehaviour
{
    [SerializeField] ThreatMeter meter;
    [SerializeField] bool visible = true;
    float _ema;

    void Reset() { meter = GetComponent<ThreatMeter>(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) visible = !visible;
        _ema = Mathf.Lerp(_ema <= 0f ? Time.deltaTime : _ema, Time.deltaTime, 0.12f);
    }

    void OnGUI()
    {
        if (!visible || meter == null) return;
        var s = GUI.skin.label; int prev = s.fontSize; s.fontSize = 14;

        GUILayout.BeginArea(new Rect(12, 540, 420, 130), GUI.skin.box);
        GUILayout.Label($"FPS: {1f / _ema:0.0} | ms: {_ema * 1000f:0.0}");
        GUILayout.Label($"Threat: {meter.Threat01:0.00}");
        GUILayout.Label("Keys: [+]/[-] ramp | [Space] spike | [0] reset | [D] toggle decay | [F5] toggle HUD");
        GUILayout.EndArea();

        s.fontSize = prev;
    }
}
