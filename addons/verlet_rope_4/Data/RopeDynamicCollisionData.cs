using Godot;

namespace VerletRope4.Data;

public readonly struct RopeDynamicCollisionData
{
    public RigidBody3D Body { get; init; }
    public Vector3 Movement { get; init; }
    public Vector3 PreviousPosition { get; init; }
    public ulong TrackingStamp { get; init; }
}
