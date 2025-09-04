using VerletRope4.Physics;

namespace VerletRope4.Data;

/// <summary>
/// Determines how rope collisions behaves physically.
/// <para><see cref="None"/> - Rope collisions are disabled, most performant option.</para>
/// <para><see cref="SlideStretch"/> - When rope particle collides, they stretch up to <see cref="VerletRopeSimulated.SlideCollisionStretch"/> value,
/// then slide along the collision normal up to <see cref="VerletRopeSimulated.IgnoreCollisionStretch"/> value, afterward the collision is considered unavoidable and is ignored. </para>
/// </summary>
public enum RopeCollisionBehavior
{
    None,
    SlideStretch
}
