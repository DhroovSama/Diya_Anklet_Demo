using UnityEngine;

[CreateAssetMenu(fileName = "CameraFeelConfig", menuName = "Feel/Camera Feel Config")]
public class CameraFeelConfig : ScriptableObject
{
    [Header("Mouse")]
    [Range(0.01f, 2f)] public float mouseSensitivityX = 0.12f; // deg per mouse-count
    [Range(0.01f, 2f)] public float mouseSensitivityY = 0.12f;
    [Range(0f, 0.08f)] public float mouseSmoothingTime = 0.0f;

    [Header("Gamepad")]
    [Range(10f, 600f)] public float stickSensitivityX = 220f; // deg/sec @ full deflection
    [Range(10f, 600f)] public float stickSensitivityY = 220f;
    [Range(0f, 0.3f)] public float stickSmoothingTime = 0.08f;
    [Range(0f, 0.4f)] public float stickDeadzone = 0.15f;

    [Header("Clamps")]
    [Range(-89f, 0f)] public float minPitch = -88f;
    [Range(0f, 89f)] public float maxPitch = 88f;

    [Header("Additives")]
    [Range(0f, 2f)] public float lookSwayMaxRoll = 0.5f;       // degrees
    [Range(1f, 30f)] public float lookSwayResponsiveness = 8f;  // larger = snappier
    [Range(0f, 0.02f)] public float idleBreathAmplitude = 0.004f; // meters
    [Range(0.05f, 0.8f)] public float idleBreathHz = 0.28f;
    [Range(0f, 0.03f)] public float headBobAmplitudeMax = 0.010f; // calmer default

    [Header("FOV")]
    [Range(40f, 100f)] public float baseFOV = 65f;
    [Range(0f, 15f)] public float externalFOVMax = 3f;
    [Range(0.05f, 1.0f)] public float externalFOVEase = 0.25f;

    [Header("Misc")]
    public bool invertY = false;
    public bool cursorLockOnStart = true;
}
