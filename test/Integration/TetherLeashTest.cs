using GdUnit4;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives `2_tether_leash`, a rigid chain between a walking body and a crate. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class TetherLeashTest
{
    [TestCase]
    public async Task RigidLeashDragsTheCrate()
    {
        await SceneHarness.RunAsync("res://demo/examples/2_tether_leash.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var leash = root.GetNode<VerletRopeRigid>("LeashRope");
        var walker = root.GetNode<RigidBody3D>("Walker");
        var crate = root.GetNode<RigidBody3D>("Crate");
        var walkerPoint = root.GetNode<Node3D>("Walker/LeashPoint");
        var cratePoint = root.GetNode<Node3D>("Crate/LeashPoint");

        var segmentBodies = leash.GetChildren()
            .OfType<RigidBody3D>()
            // A rebuild queues the bodies it replaces for free, and they stay children until the frame ends.
            .Where(body => !body.IsQueuedForDeletion())
            .ToList();
        var segmentShapes = CollectSegmentShapes(segmentBodies);
        var distinctSegmentShapes = segmentShapes.Distinct().ToList();
        var segmentLength = leash.RopeLength / leash.SimulationSegments;

        checks.Check(leash.IsRopeCreated, "the leash chain is created on ready");
        checks.Check(segmentBodies.Count == leash.SimulationSegments, $"the leash is built from {leash.SimulationSegments} segment bodies, got {segmentBodies.Count}");
        // The rope assigns one capsule instance to every segment, so its length is checked once instead of once per body;
        // the shape count still has to match the segment count, which is what a segment of another shape type breaks.
        checks.Check(
            segmentShapes.Count == leash.SimulationSegments && distinctSegmentShapes.Count == 1 && Mathf.IsEqualApprox(distinctSegmentShapes[0].Height, segmentLength),
            $"every segment carries a capsule shape of the {segmentLength:F3} segment length, got {segmentShapes.Count} capsule shapes, {distinctSegmentShapes.Count} distinct"
        );

        var walkerStart = walker.GlobalPosition;
        var crateStart = crate.GlobalPosition;

        await SceneHarness.WaitPhysicsFrames(root, 180);

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

    #endregion
}
