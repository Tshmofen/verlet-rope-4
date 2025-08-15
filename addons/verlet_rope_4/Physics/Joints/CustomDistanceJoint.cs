using Godot;
using VerletRope4;

namespace VerletRope.Physics.Joints;

[Tool]
public partial class CustomDistanceJoint : Node
{
    [ExportCategory("Connection Settings")]
    [Export] public PhysicsBody3D BodyA { get; set; }
    [Export] public Node3D CustomLocationA { get; set; }

    [Export] public PhysicsBody3D BodyB { get; set; }
    [Export] public Node3D CustomLocationB { get; set; }
    
    [ExportCategory("Movement Settings")]
    [Export] public float MaxDistance { get; set; } = 1f;
    [Export] public float MaxForce { get; set; } = 100;
    [Export(PropertyHint.ExpEasing)] public float ForceEasing { get; set; } = 1f;

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
        if (MaxDistance == 0 || BodyA == null || BodyB == null)
        {
            return;
        } 

        var a = CustomLocationA?.GlobalPosition ?? BodyA.GlobalPosition;
        var b = CustomLocationB?.GlobalPosition ?? BodyB.GlobalPosition;

        var connectionDirection = b - a;
        var connectionDistance = connectionDirection.Length();

        if (connectionDistance < MaxDistance)
        {
            return;
        }
        
        var currentScale = Mathf.Ease(connectionDistance / MaxDistance - 1f, ForceEasing);
        var pullForce = connectionDirection.Normalized() * Mathf.Clamp(currentScale * MaxForce, 0, MaxForce);

        ApplyPullForce(BodyA, CustomLocationA, pullForce);
        ApplyPullForce(BodyB, CustomLocationB, -pullForce);
    }
} 