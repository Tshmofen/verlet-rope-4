using Godot;

namespace VerletRope4.Physics.Joints;

[Tool]
public partial class VerletJointRigid : BaseVerletJoint, IVerletExported
{
    public static string ScriptPath => "res://addons/verlet_rope_4/Physics/Joints/VerletJointRigid.cs";
    public static string IconPath => "res://addons/verlet_rope_4/icons/icon_joint.svg";
    public static string ExportedBase => nameof(Node);
    public static string ExportedType => nameof(VerletJointRigid);

    #if TOOLS
    [ExportToolButton("Reset Joint (Apply Changes)")] public Callable ResetJointButton => Callable.From(() => ResetJoint());
    #endif

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

    protected override BaseVerletRopePhysical TryFindVerletRope()
    {
        return VerletRope ??= GetParent() as VerletRopeRigid;
    }
}