using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThreatMeter : MonoBehaviour
{
    [Header("Threat [0..1]")]
    [Range(0f, 1f)][SerializeField] float threat01 = 0f;
    public float Threat01
    {
        get => threat01;
        set
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(value, threat01)) return;
            threat01 = value;
            OnThreatChanged?.Invoke(threat01);
        }
    }

    [Header("Manual Control (for testing)")]
    public bool manualControl = true;
    [Tooltip("Hold Increase/Decrease to ramp threat at this rate.")]
    [Range(0f, 2f)] public float rampPerSecond = 0.6f;
    [Tooltip("Optional passive decay toward 0 when manualControl is true.")]
    public bool enableDecay = false;
    [Range(0f, 2f)] public float decayPerSecond = 0.15f;
    [Tooltip("Spike adds this amount instantly (clamped).")]
    [Range(0f, 1f)] public float spikeAmount = 0.35f;

    [Header("Input (New Input System)")]
    [SerializeField] InputActionReference increaseAction;  // e.g., "<Keyboard>/equals" & "<Keyboard>/numpadPlus"
    [SerializeField] InputActionReference decreaseAction;  // e.g., "<Keyboard>/minus"  & "<Keyboard>/numpadMinus"
    [SerializeField] InputActionReference spikeAction;     // e.g., "<Keyboard>/space"
    [SerializeField] InputActionReference resetAction;     // e.g., "<Keyboard>/digit0"
    [SerializeField] InputActionReference toggleDecayAction; // e.g., "<Keyboard>/d"

    InputAction _inc, _dec, _spike, _reset, _toggleDecay;

    public event Action<float> OnThreatChanged;

    void Awake()
    {
        // Fallback bindings if not wired
        _inc = increaseAction ? increaseAction.action : new InputAction("ThreatInc", InputActionType.Button);
        if (!increaseAction)
        {
            _inc.AddBinding("<Keyboard>/equals");
            _inc.AddBinding("<Keyboard>/numpadPlus");
        }

        _dec = decreaseAction ? decreaseAction.action : new InputAction("ThreatDec", InputActionType.Button);
        if (!decreaseAction)
        {
            _dec.AddBinding("<Keyboard>/minus");
            _dec.AddBinding("<Keyboard>/numpadMinus");
        }

        _spike = spikeAction ? spikeAction.action : new InputAction("ThreatSpike", InputActionType.Button, "<Keyboard>/space");
        _reset = resetAction ? resetAction.action : new InputAction("ThreatReset", InputActionType.Button, "<Keyboard>/digit0");
        _toggleDecay = toggleDecayAction ? toggleDecayAction.action : new InputAction("ThreatToggleDecay", InputActionType.Button, "<Keyboard>/d");
    }

    void OnEnable()
    {
        _inc.Enable(); _dec.Enable(); _spike.Enable(); _reset.Enable(); _toggleDecay.Enable();
        _spike.performed += Spike;
        _reset.performed += _ => Threat01 = 0f;
        _toggleDecay.performed += _ => enableDecay = !enableDecay;
    }

    void OnDisable()
    {
        _spike.performed -= Spike;
        _reset.performed -= _ => Threat01 = 0f; // safe even if not subscribed
        _toggleDecay.performed -= _ => enableDecay = !enableDecay;
        _inc.Disable(); _dec.Disable(); _spike.Disable(); _reset.Disable(); _toggleDecay.Disable();
    }

    void Spike(InputAction.CallbackContext _)
    {
        Threat01 = Mathf.Clamp01(Threat01 + spikeAmount);
    }

    void Update()
    {
        if (!manualControl) return;
        float dt = Time.deltaTime;

        if (_inc.IsPressed()) Threat01 += rampPerSecond * dt;
        if (_dec.IsPressed()) Threat01 -= rampPerSecond * dt;
        if (enableDecay && !_inc.IsPressed()) Threat01 -= decayPerSecond * dt;
    }
}
