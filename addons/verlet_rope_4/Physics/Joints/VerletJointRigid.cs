using Godot;

namespace VerletRope4.Physics.Joints;

[Tool]
public partial class VerletJointRigid : BaseVerletJoint, IVerletExported
{
    public static string ScriptPath => "res://addons/verlet_rope_4/Physics/Joints/VerletJointRigid.cs";
    public static string IconPath => "res://addons/verlet_rope_4/Icons/icon_joint.svg";
    public static string ExportedBase => nameof(Node);
    public static string ExportedType => nameof(VerletJointRigid);

    private VerletRopeRigid _verletRopeNode;
    private PhysicsBody3D _startBody;
    private Node3D _startCustomLocation;
    private PhysicsBody3D _endBody;
    private Node3D _endCustomLocation;

#if TOOLS
    [ExportToolButton("Reset Joint")] public Callable ResetJointButton => Callable.From(() => ResetJoint());
#endif

    /// <summary> A <see cref="VerletRopeRigid"/> node instance to which join constraints will be applied to. Automatically assigns current parent if it is of needed type and the value is currently unset. </summary>
    [ExportCategory("Attachment Settings")]
    [Export] public VerletRopeRigid VerletRope { get => _verletRopeNode; set => SetJointProperty(ref _verletRopeNode, value, JointApplyReason.Rope); }

    /// <inheritdoc/>
    [ExportSubgroup("Rope Start")]
    [Export] public override PhysicsBody3D StartBody { get => _startBody; set => SetJointProperty(ref _startBody, value, JointApplyReason.Attachment); }
    /// <inheritdoc/>
    [Export] public override Node3D StartCustomLocation { get => _startCustomLocation; set => SetJointProperty(ref _startCustomLocation, value, JointApplyReason.Attachment); }

    /// <inheritdoc/>
    [ExportSubgroup("Rope End")]
    [Export] public override PhysicsBody3D EndBody { get => _endBody; set => SetJointProperty(ref _endBody, value, JointApplyReason.Attachment); }
    /// <inheritdoc/>
    [Export] public override Node3D EndCustomLocation { get => _endCustomLocation; set => SetJointProperty(ref _endCustomLocation, value, JointApplyReason.Attachment); }

    protected override BaseVerletRopePhysical TryFindVerletRope()
    {
        return VerletRope ??= GetParent() as VerletRopeRigid;
    }
}
