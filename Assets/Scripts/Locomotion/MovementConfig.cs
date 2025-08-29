using UnityEngine;

[CreateAssetMenu(fileName = "MovementConfig", menuName = "Feel/Movement Config")]
public class MovementConfig : ScriptableObject
{
    [Header("Speeds (m/s)")]
    [Range(0.5f, 10f)] public float walkSpeed = 2.8f;
    [Range(0.5f, 12f)] public float runSpeed = 4.2f;
    public bool enableSprint = false;

    [Header("Accel / Decel (seconds to reach target)")]
    [Range(0.01f, 0.3f)] public float accelTime = 0.06f;
    [Range(0.01f, 0.3f)] public float decelTime = 0.10f;

    [Header("Vertical")]
    [Tooltip("Negative, stronger than gravity when grounded to prevent micro-floating on edges/ramps.")]
    [Range(-10f, 0f)] public float stickToGroundForce = -2.0f;
    [Tooltip("Gravity in m/s^2 (negative).")]
    [Range(-40f, -5f)] public float gravity = -19.6f;

    [Header("Grounding Probe")]
    [Tooltip("Cast radius used for ground probe. Default uses controller radius minus this.")]
    [Range(0f, 0.15f)] public float groundProbeRadiusPadding = 0.05f;
    [Tooltip("How far below the feet we scan for ground.")]
    [Range(0.05f, 0.5f)] public float groundCheckDistance = 0.18f;

    [Header("Input")]
    [Range(0f, 0.4f)] public float stickDeadzone = 0.15f;
}
