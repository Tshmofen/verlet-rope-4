using Godot;

namespace VerletRope4.Structure;

public class RopeDynamicCollisionData
{
    public RigidBody3D Body { get; set; }
    public Vector3 Movement { get; set; }
    public Vector3 PreviousPosition { get; set; }
    public ulong TrackingStamp { get; set; }
}