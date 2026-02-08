using System.Collections.Generic;
using System.Linq;
using Godot;
using VerletRope4.Utility;

namespace VerletRope4.Physics.Joints;

[Tool]
public partial class VerletJointSimulated : BaseVerletJoint, IVerletExported
{
    public static string ScriptPath => "res://addons/verlet_rope_4/Physics/Joints/VerletJointSimulated.cs";
    public static string IconPath => "res://addons/verlet_rope_4/icons/icon_joint.svg";
    public static string ExportedBase => nameof(Node);
    public static string ExportedType => nameof(VerletJointSimulated);

    private DistanceForceJoint _joint;
    
    #if TOOLS
    [ExportToolButton("Reset Joint (Apply Changes)")] public Callable ResetJointButton => Callable.From(() => ResetJoint());
    #endif

    /// <summary> A <see cref="VerletRopeSimulated"/> node instance to which join constraints will be applied to. Automatically assigns current parent if it is of needed type and the value is currently unset. </summary>
    [ExportCategory("Attachment Settings")]
    [Export] public VerletRopeSimulated VerletRope { get; set; }
    
    /// <inheritdoc/>
    [ExportSubgroup("Rope Start")]
    [Export] public override  PhysicsBody3D StartBody { get; set; }
    /// <inheritdoc/>
    [Export] public override  Node3D StartCustomLocation{ get; set; }
    /// <summary> Determines whether rope will collide with the connected <see cref="StartBody"/>. </summary>
    [Export] public bool IgnoreStartBodyCollision { get; set; } = true;
    
    /// <inheritdoc/>
    [ExportSubgroup("Rope End")] 
    [Export] public override  PhysicsBody3D EndBody { get; set; }
    /// <inheritdoc/>
    [Export] public override Node3D EndCustomLocation{ get; set; }
    /// <summary> Determines whether rope will collide with the connected  <see cref="EndBody"/>. </summary>
    [Export] public bool IgnoreEndBodyCollision { get; set; } = true;

    /// <inheritdoc cref="DistanceForceJoint.MaxDistance"/>
    [ExportSubgroup("Distance Joint")]
    [Export(PropertyHint.Range, "0, 10000")] public float JointMaxDistance { get; set; } = 0;
    /// <inheritdoc cref="DistanceForceJoint.MaxForce"/>
    [Export(PropertyHint.Range, "0, 10000")] public float JointMaxForce { get; set; } = 50f;
    /// <inheritdoc cref="DistanceForceJoint.ForceEasing"/>
    [Export(PropertyHint.ExpEasing)] public float JointForceEasing { get; set; } = 0.9f;

    private void ConfigureDistanceJoint()
    {
        if (JointMaxDistance == 0 || (StartBody == null && EndBody == null))
        {
            _joint?.Dispose();
            _joint = null;
            return;
        }

        _joint ??= this.FindOrCreateChild<DistanceForceJoint>();
        _joint.MaxDistance = JointMaxDistance;
        _joint.BodyA = StartBody;
        _joint.BodyB = EndBody;
        _joint.CustomLocationA = StartCustomLocation;
        _joint.CustomLocationB = EndCustomLocation;
        _joint.ForceEasing = JointForceEasing;
        _joint.MaxForce = JointMaxForce;
        _joint.IsAppliedCustomCondition = VerletRope != null ? () => VerletRope.IsRopeCreated : null;

        if (StartBody == null && StartCustomLocation == null)
        {
            _joint.CustomLocationA = VerletRope;
        }
    }

    protected override BaseVerletRopePhysical TryFindVerletRope()
    {
        return VerletRope ??= GetParent() as VerletRopeSimulated;
    }

    /// <inheritdoc cref="BaseVerletJoint.ResetJoint"/>
    public override void ResetJoint(bool toResetRope = true)
    {
        ConfigureDistanceJoint();
        base.ResetJoint(toResetRope);
    }

    /// <summary> Returns physics <see cref="Rid"/> of connected bodies that are to be ignored by parent <see cref="VerletRopeSimulated"/> instance. </summary>
    public List<Rid> GetPhysicsExceptionRids()
    {
        var exceptions = new List<Rid>(2);

        if (IgnoreEndBodyCollision && EndBody != null)
        {
            exceptions.Add(EndBody.GetRid());
        }

        if (IgnoreStartBodyCollision && StartBody != null)
        {
            exceptions.Add(StartBody.GetRid());
        }

        return exceptions;
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = base._GetConfigurationWarnings().ToList();

        if (VerletRope == null)
        {
            warnings.Add("Joint will do nothing without an associated rope, please assign one."); 
        }

        if (JointMaxDistance > 0 && StartBody is null && EndBody is null)
        {
            warnings.Add($"{nameof(JointMaxDistance)} is configured but both `{nameof(StartBody)}` and `{nameof(EndBody)}` are not accessible for physical connection.");
        }

        if (JointMaxDistance > 0 && VerletRope?.IsDisabledWhenInvisible == true)
        {
            warnings.Add($"Rope has `{nameof(VerletRopeSimulated.IsDisabledWhenInvisible)}` enabled, if your joint is moving it might lead to rope not being drawn when it leaves the screen - consider disabling it.");
        }

        return warnings.ToArray();
    }
}