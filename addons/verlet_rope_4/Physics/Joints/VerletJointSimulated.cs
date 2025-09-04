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

    [ExportToolButton("Reset Joint (Apply Changes)")] public Callable ResetJointButton => Callable.From(ResetJoint);
    
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

    public override void _Ready()
    {
        ResetJoint();
    }

    public override void _EnterTree()
    {
        ResetJoint();
    }

    public override void _ExitTree()
    {
        VerletRope?.SetAttachments(null, null, null, null);
        VerletRope?.ClearExceptions();
    }

    private void ConfigureDistanceJoint()
    {
        if (JointMaxDistance == 0 || StartBody == null || EndBody == null)
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
    }

    /// <inheritdoc cref="BaseVerletJoint.ResetJoint"/>
    public override void ResetJoint()
    {
        base.ResetJoint();
        ConfigureDistanceJoint();
        UpdateConfigurationWarnings();

        VerletRope ??= GetParent() as VerletRopeSimulated;
        VerletRope?.SetAttachments(StartBody, StartCustomLocation, EndBody,EndCustomLocation);

        if (EndBody != null)
        {
            VerletRope?.RegisterExceptionRid(EndBody.GetRid(), IgnoreEndBodyCollision);
        }        
        
        if (StartBody != null)
        {
            VerletRope?.RegisterExceptionRid(StartBody.GetRid(), IgnoreStartBodyCollision);
        }
        
        VerletRope?.CallDeferred(VerletRopeSimulated.MethodName.CreateRope);
    }

    public override string[] _GetConfigurationWarnings()
    {
        var baseWarnings = base._GetConfigurationWarnings();

        if (JointMaxDistance > 0 && (StartBody is null || EndBody is null))
        {
            return baseWarnings
                .Union([$"{nameof(JointMaxDistance)} is configured but either `{nameof(StartBody)}` or `{nameof(EndBody)}` is not accessible for physical connection."])
                .ToArray();
        }

        return baseWarnings;
    }

    #region Script Reload

    public override void OnAfterDeserialize()
    {
        // Clear exceptions after script reload as physics server does not retain object RIDs
        VerletRope?.ClearExceptions();
        base.OnAfterDeserialize();
    }

    #endregion
}