using System.Collections.Generic;
using UnityEngine;

public interface ICameraAdditive
{
    // Return local-space offsets to apply this frame.
    void Sample(float dt, out Vector3 posOffset, out Vector3 eulerOffset, out float fovOffset);
}

public class CameraAdditiveBus : MonoBehaviour
{
    readonly List<ICameraAdditive> _additives = new List<ICameraAdditive>();

    // Exposed for additives to read (deg/sec)
    public Vector2 LookVelocityDegPerSec { get; internal set; } // x = yaw vel, y = pitch vel

    [Header("Global Motion Scaling")]
    [Range(0f, 1f)] public float motionScale = 1f; // 1 = full, 0.7 = safer, 0 = off

    [Header("Safety Clamps")]
    [Tooltip("Absolute max additive local-position offsets (meters).")]
    public Vector3 posClamp = new Vector3(0.015f, 0.015f, 0.010f);
    [Tooltip("Absolute max additive Euler offsets (degrees): (pitch,yaw,roll).")]
    public Vector3 rotClamp = new Vector3(0.40f, 0.40f, 0.35f);
    [Tooltip("Absolute max total FOV offset (degrees).")]
    [Range(0f, 10f)] public float fovClamp = 5f;

    // Optional external FOV animation (e.g., threat pump).
    float _extFovCurrent, _extFovTarget;
    float _extFovVel;
    float _extFovEase = 0.25f;
    float _extFovMax = 3f;

    // Last-sampled totals (for debug HUD).
    public Vector3 LastPosOffset { get; private set; }
    public Vector3 LastEulerOffset { get; private set; }
    public float LastFovOffset { get; private set; }

    // Simple impulse stack.
    struct Impulse { public Vector3 posMag; public Vector3 rotMag; public float t, dur; }
    readonly List<Impulse> _impulses = new List<Impulse>();

    public void ConfigureExternalFov(float max, float ease)
    {
        _extFovMax = max;
        _extFovEase = Mathf.Max(0.01f, ease);
    }

    public void SetExternalFovTarget(float target)
    {
        _extFovTarget = Mathf.Clamp(target, -_extFovMax, _extFovMax);
    }

    public void PushImpulse(Vector3 posMagnitude, Vector3 rotMagnitude, float duration)
    {
        _impulses.Add(new Impulse { posMag = posMagnitude, rotMag = rotMagnitude, dur = Mathf.Max(0.01f, duration), t = 0f });
    }

    public void Register(ICameraAdditive a) { if (!_additives.Contains(a)) _additives.Add(a); }
    public void Unregister(ICameraAdditive a) { _additives.Remove(a); }

    public void SampleAdditives(float dt, out Vector3 pos, out Vector3 euler, out float fov)
    {
        pos = Vector3.zero; euler = Vector3.zero; fov = 0f;

        // External FOV ease.
        _extFovCurrent = Mathf.SmoothDamp(_extFovCurrent, _extFovTarget, ref _extFovVel, _extFovEase);
        fov += _extFovCurrent;

        // Add impulses.
        for (int i = _impulses.Count - 1; i >= 0; --i)
        {
            var imp = _impulses[i];
            float a = 1f - (imp.t / imp.dur);
            // Cosine falloff 0..1
            float w = 0.5f - 0.5f * Mathf.Cos(a * Mathf.PI);
            pos += imp.posMag * w;
            euler += imp.rotMag * w;

            imp.t += dt;
            if (imp.t >= imp.dur) _impulses.RemoveAt(i);
            else _impulses[i] = imp;
        }

        // Add registered additives.
        foreach (var a in _additives)
        {
            a.Sample(dt, out var p, out var r, out var f);
            pos += p; euler += r; fov += f;
        }

        // Global scale
        float s = Mathf.Clamp01(motionScale);
        pos *= s;
        euler *= s;
        fov *= s;

        // Hard safety clamps
        pos.x = Mathf.Clamp(pos.x, -posClamp.x, posClamp.x);
        pos.y = Mathf.Clamp(pos.y, -posClamp.y, posClamp.y);
        pos.z = Mathf.Clamp(pos.z, -posClamp.z, posClamp.z);

        euler.x = Mathf.Clamp(euler.x, -rotClamp.x, rotClamp.x);
        euler.y = Mathf.Clamp(euler.y, -rotClamp.y, rotClamp.y);
        euler.z = Mathf.Clamp(euler.z, -rotClamp.z, rotClamp.z);

        fov = Mathf.Clamp(fov, -fovClamp, fovClamp);

        LastPosOffset = pos; LastEulerOffset = euler; LastFovOffset = fov;
    }
}
