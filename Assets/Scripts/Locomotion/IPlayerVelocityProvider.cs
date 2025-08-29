using UnityEngine;

public interface IPlayerVelocityProvider
{
    Vector3 Velocity { get; }
    bool IsGrounded { get; }
}
