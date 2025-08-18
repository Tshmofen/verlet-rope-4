using Godot;
using VerletRope4.Utility;

namespace VerletRope4.Physics.Joints;

[Tool]
public partial class VerletRopeRigidJoint : Node3D, ISerializationListener
{
    public const string ScriptPath = "res://addons/verlet_rope_4/Physics/Joints/VerletRopeRigidJoint.cs";
    public const string IconPath = "res://addons/verlet_rope_4/icon.svg";

    private CustomDistanceJoint _distanceJoint;

    [ExportToolButton("Reset Joint")] public Callable ResetJointButton => Callable.From(ResetJoint);
    
    [ExportCategory("Attachment Settings")]
    [Export] public VerletRopePhysical VerletRope { get; set; }

    [ExportSubgroup("Rope Start")]
    [Export] public PhysicsBody3D StartBody { get; set; }
    [Export] public Node3D StartJointCustomLocation{ get; set; }
    [Export] public bool IgnoreStartBodyCollision { get; set; } = true;
    
    [ExportSubgroup("Rope End")]
    [Export] public PhysicsBody3D EndBody { get; set; }
    [Export] public Node3D EndJointCustomLocation{ get; set; }
    [Export] public bool IgnoreEndBodyCollision { get; set; } = true;

    [ExportSubgroup("Distance Joint Settings")]
    [Export(PropertyHint.Range, "0, 10000")] public float JointMaxDistance { get; set; } = 0;
    [Export(PropertyHint.Range, "0, 10000")] public float JointMaxForce { get; set; } = 50f;
    [Export(PropertyHint.ExpEasing)] public float JointForceEasing { get; set; } = 0.9f;

    public override void _Ready()
    {
        ResetJoint();
    }

    public override void _ExitTree()
    {
        if (VerletRope == null)
        {
            return;
        }
        
        // Needed to restore rope state on joint delete or move
        if (VerletRope is VerletRopeSimulated simulatedRope)
        {
            simulatedRope.StartNodeAttach = null;
            simulatedRope.EndNodeAttach = null;
        }

        VerletRope.ClearExceptions();
        VerletRope.CreateRope();
    }

    private void ConfigureDistanceJoint()
    {
        if (JointMaxDistance == 0 || StartBody == null || EndBody == null)
        {
            _distanceJoint?.Dispose();
            _distanceJoint = null;
            return;
        }

        _distanceJoint ??= this.FindOrCreateChild<CustomDistanceJoint>();
        _distanceJoint.MaxDistance = JointMaxDistance;
        _distanceJoint.BodyA = StartBody;
        _distanceJoint.BodyB = EndBody;
        _distanceJoint.CustomLocationA = StartJointCustomLocation;
        _distanceJoint.CustomLocationB = EndJointCustomLocation;
        _distanceJoint.ForceEasing = JointForceEasing;
        _distanceJoint.MaxForce = JointMaxForce;
    }

    public void ResetJoint()
    {
        ConfigureDistanceJoint();
        UpdateConfigurationWarnings();

        if (VerletRope == null)
        {
            VerletRope = GetParent() as VerletRopePhysical;
            if (VerletRope == null)
            {
                return;
            }
        }

        if (VerletRope is VerletRopeSimulated simulatedRope)
        {
            simulatedRope.StartNodeAttach = StartJointCustomLocation ?? StartBody;
            simulatedRope.EndNodeAttach = EndJointCustomLocation ?? EndBody;
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

    public override string[] _GetConfigurationWarnings()
    {
        if (JointMaxDistance > 0 && (StartBody is null || EndBody is null))
        {
            return [$"{nameof(JointMaxDistance)} is configured but either `{nameof(StartBody)}` or `{nameof(EndBody)}` is not accessible for physical connection."];
        }

        return [];
    }

    #region Script Reload

    public void OnBeforeSerialize()
    {
        // Ignore unload
    }

    public void OnAfterDeserialize()
    {
        // Recreate joint and clear exceptions after script reload as physics server does not retain object RIDs
        VerletRope?.ClearExceptions();
        ResetJoint();
    }

    #endregion
}