using Godot;

namespace VerletRope.addons.verlet_rope_4;

public partial class RopeDistanceJoint : Node3D
{
    [Export] public PhysicsBody3D StartBody { get; set; }
    [Export] public Node3D StartJointCustomLocation{ get; set; }

    [Export] public PhysicsBody3D EndBody { get; set; }
    [Export] public Node3D EndJointCustomLocation{ get; set; }
}