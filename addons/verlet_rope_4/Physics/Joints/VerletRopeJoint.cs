using Godot;
using VerletRope4.Physics;

namespace VerletRope.Physics.Joints;

[Tool]
public partial class VerletRopeJoint : Node3D
{
    public const string ScriptPath = "res://addons/verlet_rope_4/Physics/Joints/VerletRopeJoint.cs";
    public const string IconPath = "res://addons/verlet_rope_4/icon.svg";

    [ExportToolButton("Reset Rope")] public Callable ResetRopeButton => Callable.From(CreateJoint);
    
    [ExportCategory("Attachment Settings")]
    [Export] public VerletRopePhysical VerletRope { get; set; }

    [Export] public PhysicsBody3D StartBody { get; set; }
    [Export] public Node3D StartJointCustomLocation{ get; set; }
    [Export] public bool IgnoreStartBodyCollision { get; set; }

    [Export] public PhysicsBody3D EndBody { get; set; }
    [Export] public Node3D EndJointCustomLocation{ get; set; }
    [Export] public bool IgnoreEndBodyCollision { get; set; }

    public override void _Ready() => CreateJoint();

    public void CreateJoint()
    {
        if (VerletRope == null)
        {
            VerletRope = GetParent() as VerletRopePhysical;
            if (VerletRope == null)
            {
                return;
            }
        }

        if (VerletRope is VerletRopeSimulated simulated)
        {
            simulated.EndNodeAttach = EndBody;
            simulated.StartNodeAttach = StartBody;
        }

        if (EndBody != null)
        {
            VerletRope.RegisterExceptionRid(EndBody.GetRid(), IgnoreEndBodyCollision);
        }        
        
        if (StartBody != null)
        {
            VerletRope.RegisterExceptionRid(StartBody.GetRid(), IgnoreStartBodyCollision);
        }

        VerletRope.CreateRope();
    }
}