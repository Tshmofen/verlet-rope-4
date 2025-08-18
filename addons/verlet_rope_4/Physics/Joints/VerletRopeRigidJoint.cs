using Godot;

namespace VerletRope4.Physics.Joints;

[Tool]
public partial class VerletRopeRigidJoint : BaseVerletRopeJoint
{
    public const string ScriptPath = "res://addons/verlet_rope_4/Physics/Joints/VerletRopeRigidJoint.cs";
    public const string IconPath = "res://addons/verlet_rope_4/icon.svg";

    [ExportToolButton("Reset Joint")] public Callable ResetJointButton => Callable.From(ResetJoint);
    
    [ExportCategory("Attachment Settings")]
    [Export] public VerletRopeRigidBody VerletRope { get; set; }

    [ExportSubgroup("Rope Start")]
    [Export] public override PhysicsBody3D StartBody { get; set; }
    [Export] public override  Node3D StartCustomLocation{ get; set; }
    
    [ExportSubgroup("Rope End")]
    [Export] public override  PhysicsBody3D EndBody { get; set; }
    [Export] public override  Node3D EndCustomLocation{ get; set; }

    public override void _Ready()
    {
        ResetJoint();
    }

    public override void _ExitTree()
    {
        VerletRope?.SetAttachments(null, null);
        VerletRope?.CreateRope();
    }

    public override void ResetJoint()
    {
        VerletRope ??= GetParent() as VerletRopeRigidBody;
        VerletRope?.SetAttachments(StartCustomLocation ?? StartBody, EndCustomLocation ?? EndBody);
        VerletRope?.CreateRope();
    }
}