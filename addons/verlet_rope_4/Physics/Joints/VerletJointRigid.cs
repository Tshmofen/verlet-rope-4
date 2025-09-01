using Godot;

namespace VerletRope4.Physics.Joints;

[Tool]
public partial class VerletJointRigid : BaseVerletJoint, IVerletExported
{
    public static string ScriptPath => "res://addons/verlet_rope_4/Physics/Joints/VerletJointRigid.cs";
    public static string IconPath => "res://addons/verlet_rope_4/icons/icon_joint_rigid.svg";
    public static string ExportedBase => nameof(Node3D);
    public static string ExportedType => nameof(VerletJointRigid);

    [ExportToolButton("Reset Joint (Apply Changes)")] public Callable ResetJointButton => Callable.From(ResetJoint);
    
    /// <summary> A <see cref="VerletRopeRigid"/> node instance to which join constraints will be applied to. Automatically assigns current parent if it is of needed type and the value is currently unset. </summary>
    [ExportCategory("Attachment Settings")]
    [Export] public VerletRopeRigid VerletRope { get; set; }

    /// <inheritdoc/>
    [ExportSubgroup("Rope Start")]
    [Export] public override PhysicsBody3D StartBody { get; set; }
    /// <inheritdoc/>
    [Export] public override  Node3D StartCustomLocation{ get; set; }
    
    /// <inheritdoc/>
    [ExportSubgroup("Rope End")]
    [Export] public override  PhysicsBody3D EndBody { get; set; }
    /// <inheritdoc/>
    [Export] public override  Node3D EndCustomLocation{ get; set; }

    public override void _Ready()
    {
        ResetJoint();
    }

    public override void _ExitTree()
    {
        VerletRope?.SetAttachments(null, null, null, null);
        VerletRope?.CallDeferred(VerletRopeRigid.MethodName.CreateRope);
    }

    public override void ResetJoint()
    {
        base.ResetJoint();
        VerletRope ??= GetParent() as VerletRopeRigid;
        VerletRope?.SetAttachments(StartBody, StartCustomLocation, EndBody, EndCustomLocation);
        VerletRope?.CallDeferred(VerletRopeRigid.MethodName.CreateRope);
    }
}