using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Physics;

namespace VerletRope4.Tests.Scenes.Scenarios;

/// <summary>
/// A rigid rope leash between a walking body and a crate: the chain has to be built from segment bodies of the configured
/// length, hold the two bodies about one rope length apart and drag the crate along with the body that walks away from it.
/// </summary>
public sealed class TetherLeashScenario : SceneScenario
{
    public override string ScenePath => "res://demo/examples/2_tether_leash.tscn";

    public override async Task RunAsync(Node root, SceneChecks checks)
    {
        var leash = root.GetNode<VerletRopeRigid>("LeashRope");
        var walker = root.GetNode<RigidBody3D>("Walker");
        var crate = root.GetNode<RigidBody3D>("Crate");
        var walkerPoint = root.GetNode<Node3D>("Walker/LeashPoint");
        var cratePoint = root.GetNode<Node3D>("Crate/LeashPoint");

        var segmentBodies = leash.GetChildren()
            .OfType<RigidBody3D>()
            .Where(body => !body.IsQueuedForDeletion())
            .ToList();
        var segmentShapes = CollectSegmentShapes(segmentBodies);
        var segmentLength = leash.RopeLength / leash.SimulationSegments;

        checks.Check(leash.IsRopeCreated, "the leash chain is created on ready");
        checks.Check(segmentBodies.Count == leash.SimulationSegments, $"the leash is built from {leash.SimulationSegments} segment bodies, got {segmentBodies.Count}");
        checks.Check(
            segmentShapes.Count == leash.SimulationSegments && segmentShapes.All(shape => Mathf.IsEqualApprox(shape.Height, segmentLength)),
            $"every segment carries a collision shape of the {segmentLength:F3} segment length, got {segmentShapes.Count}"
        );

        var walkerStart = walker.GlobalPosition;
        var crateStart = crate.GlobalPosition;

        await WaitPhysicsFrames(root, 180);

        var leashSpan = walkerPoint.GlobalPosition.DistanceTo(cratePoint.GlobalPosition);
        var walkerTravel = walker.GlobalPosition.DistanceTo(walkerStart);
        var crateTravel = crate.GlobalPosition.DistanceTo(crateStart);

        checks.Check(walkerTravel > 0.2f, $"the demo drives the walking body, travelled {walkerTravel:F2}");
        checks.Check(crateTravel > 0.05f, $"the crate is dragged along by the leash, travelled {crateTravel:F2}");
        checks.CheckRange(leashSpan, leash.RopeLength - 0.5f, leash.RopeLength + 0.7f, "the leash holds the two bodies about one rope length apart");
        checks.CheckParticlesAreFinite(leash, "the leash particle positions stay finite");
    }

    private static List<CapsuleShape3D> CollectSegmentShapes(List<RigidBody3D> segmentBodies)
    {
        return segmentBodies
            .SelectMany(body => body.GetChildren().OfType<CollisionShape3D>())
            .Select(shape => shape.Shape)
            .OfType<CapsuleShape3D>()
            .ToList();
    }
}
