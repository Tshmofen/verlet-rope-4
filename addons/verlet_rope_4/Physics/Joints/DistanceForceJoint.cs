using Godot;

namespace VerletRope4.Physics.Joints;

public partial class DistanceForceJoint : Node, IVerletExported
{
    public static string ScriptPath => "res://addons/verlet_rope_4/Physics/Joints/DistanceForceJoint.cs";
    public static string IconPath => "res://addons/verlet_rope_4/icons/icon_joint.svg";
    public static string ExportedBase => nameof(Node);
    public static string ExportedType => nameof(DistanceForceJoint);

    /// <summary> Physical body used in joint calculations, by default <see cref="Node3D.GlobalPosition"/> is used as connection point. If is instance of <see cref="RigidBody3D"/>, the joint force will be applied to it. </summary>
    [ExportCategory("Connection Settings")]
    [Export] public PhysicsBody3D BodyA { get; set; }
    /// <summary> A custom location for <see cref="BodyA"/> joint, used in distance calculations. </summary>
    [Export] public Node3D CustomLocationA { get; set; }
    
    /// <summary> Physical body used in joint calculations, by default <see cref="Node3D.GlobalPosition"/> is used as connection point. If is instance of <see cref="RigidBody3D"/>, the joint force will be applied to it. </summary>
    [Export] public PhysicsBody3D BodyB { get; set; }
    /// <summary> A custom location for <see cref="BodyB"/> joint, used in distance calculations. </summary>
    [Export] public Node3D CustomLocationB { get; set; }
    
    /// <summary> The distance before joint force is start being applied. When is set to zero - constraint is not applied. </summary>
    [ExportCategory("Movement Settings")]
    [Export] public float MaxDistance { get; set; } = 1f;
    /// <summary> Max physical force that can be applied between connected bodies to reduce the distance between them below specified max value. </summary>
    [Export] public float MaxForce { get; set; } = 100;
    /// <summary> Determines force easing once it's applied, is only relevant while force is less than <see cref="MaxForce"/> and determines how fast it's rising depending on the distance. </summary>
    [Export(PropertyHint.ExpEasing)] public float ForceEasing { get; set; } = 1.0f;

    private static void ApplyPullForce(PhysicsBody3D body, Node3D customLocation, Vector3 pullForce)
    {
        if (body is not RigidBody3D rigidBody)
        {
            return;
        }

        if (customLocation != null)
        {
            rigidBody.ApplyForce(pullForce, customLocation.GlobalPosition - body.GlobalPosition);
        }
        else
        {
            rigidBody.ApplyCentralForce(pullForce);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (MaxDistance == 0)
        {
            return;
        } 

        var a = CustomLocationA?.GlobalPosition ?? BodyA?.GlobalPosition;
        var b = CustomLocationB?.GlobalPosition ?? BodyB?.GlobalPosition;

        if (a == null || b == null)
        {
            return;
        }

        var connectionDirection = (b - a).Value;
        var connectionDistance = connectionDirection.Length();

        if (connectionDistance < MaxDistance)
        {
            return;
        }
        
        var currentScale = Mathf.Ease(connectionDistance / MaxDistance - 1.0f, ForceEasing);
        var pullForce = connectionDirection.Normalized() * Mathf.Clamp(currentScale * MaxForce, 0.0f, MaxForce);

        ApplyPullForce(BodyA, CustomLocationA, pullForce);
        ApplyPullForce(BodyB, CustomLocationB, -pullForce);
    }
} 