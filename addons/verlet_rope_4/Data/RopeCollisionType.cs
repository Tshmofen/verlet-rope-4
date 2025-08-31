using Godot;
using VerletRope4.Physics;

namespace VerletRope4.Data;

/// <summary>
/// Determines how rope collisions are being tracked.
/// <para><see cref="StaticOnly"/> - Rope only collides with static objects specified in <see cref="VerletRopeSimulated.StaticCollisionMask"/>, any <see cref="RigidBody3D"/> from this layer might not be handled correctly;</para>
/// <para><see cref="DynamicOnly"/> - Rope only collides with dynamic objects specified in <see cref="VerletRopeSimulated.DynamicCollisionMask"/>, any <see cref="RigidBody3D"/> in the rope area will be tracked
/// and their velocity interpolated for correct dynamic collision handling, is more performance heavy compared to static tracking;</para>
/// <para><see cref="All"/> - Both variants of collision tracking is enabled, see their descriptions above.</para>
/// </summary>
public enum RopeCollisionType
{
    StaticOnly,
    DynamicOnly,
    All
}
