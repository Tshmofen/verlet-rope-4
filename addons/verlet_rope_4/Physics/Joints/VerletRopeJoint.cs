using Godot;
using VerletRope4.Physics;

namespace VerletRope.Physics.Joints;

[Tool]
public partial class VerletRopeJoint : Node3D, ISerializationListener
{
    public const string ScriptPath = "res://addons/verlet_rope_4/Physics/Joints/VerletRopeJoint.cs";
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
    [Export(PropertyHint.Range, "0.01,16")] public float DistanceDamping { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0.01,16")] public float DistanceSoftness { get; set; } = 0.7f;
    [Export(PropertyHint.Range, "0.01,16")] public float DistanceRestitution { get; set; } = 0.5f;

    public override void _Ready() => ResetJoint();

    private void ConfigureDistanceJoint()
    {
        if (JointMaxDistance == 0 || StartBody == null || EndBody == null)
        {
            _distanceJoint?.Dispose();
            _distanceJoint = null;
            return;
        }

        if (_distanceJoint == null)
        {
            AddChild(_distanceJoint = new CustomDistanceJoint());
        }

        _distanceJoint.MaxDistance = JointMaxDistance;
        _distanceJoint.UniformDamping= DistanceDamping;
        _distanceJoint.UniformSoftness = DistanceSoftness;
        _distanceJoint.UniformRestitution= DistanceRestitution;
        _distanceJoint.BodyA = StartBody;
        _distanceJoint.BodyB = EndBody;
        _distanceJoint.CustomLocationA = StartJointCustomLocation;
        _distanceJoint.CustomLocationB = EndJointCustomLocation;
        _distanceJoint.ResetJoint();
    }

    public void ResetJoint()
    {
        ConfigureDistanceJoint();

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