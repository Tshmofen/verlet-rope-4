using GdUnit4;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
using VerletRope4.Tests.Unit.Harness;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary>
/// A rigid rope is a chain of <see cref="RigidBody3D"/> segments pinned together rather than a solved particle buffer, which
/// is why its length and segment count are rebuilt instead of applied.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "unit")]
public class RigidRopeTest
{
    [TestCase]
    public async Task RopeIsCreatedAsAChainOfSegmentBodies()
    {
        var rope = CreateRope(simulationSegments: 6, ropeLength: 1.2f);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        var bodies = UnitHarness.CollectBodies(rope);

        AssertThat(rope.IsRopeCreated).IsTrue();
        AssertThat(rope.GetParticleCount()).IsEqual(7);
        AssertThat(bodies.Count).IsEqual(6);
        AssertThat(bodies.All(body => body.GetChildren().OfType<CollisionShape3D>().Any())).IsTrue();
    }

    /// <summary>
    /// The first segment is pinned to the empty start body slot, which is a world anchor, and nothing pins the last segment
    /// because the rope has no end attachment.
    /// </summary>
    [TestCase]
    public async Task SegmentBodiesArePinnedTogetherAndToTheStart()
    {
        var rope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        var bodies = UnitHarness.CollectBodies(rope);
        var pinCount = bodies.Sum(body => body.GetChildren().OfType<PinJoint3D>().Count());

        AssertThat(bodies.Count).IsEqual(4);
        AssertThat(pinCount).IsEqual(4);
        AssertThat(bodies[0].GetChildren().OfType<PinJoint3D>().Count()).IsEqual(2);
        AssertThat(bodies[^1].GetChildren().OfType<PinJoint3D>().Any()).IsFalse();
    }

    /// <summary> The length is stored in the segment bodies, so writing it rebuilds the chain on the next frame. </summary>
    [TestCase]
    public async Task RopeLengthWriteRebuildsTheChainOnTheNextFrame()
    {
        var rope = CreateRope(simulationSegments: 5, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        rope.RopeLength = 2.0f;

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        var bodies = UnitHarness.CollectBodies(rope);
        var shapes = bodies
            .SelectMany(body => body.GetChildren().OfType<CollisionShape3D>())
            .Select(shape => shape.Shape)
            .OfType<CapsuleShape3D>()
            .ToList();

        AssertThat(bodies.Count).IsEqual(5);
        AssertThat(shapes.Count).IsEqual(5);
        AssertThat(shapes[0].Height).IsEqualApprox(0.4f, 0.001f);
    }

    /// <summary> The segment count is structural the same way, so the chain is rebuilt with the new amount of bodies. </summary>
    [TestCase]
    public async Task SimulationSegmentsWriteRebuildsTheChainOnTheNextFrame()
    {
        var rope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        rope.SimulationSegments = 8;

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        AssertThat(UnitHarness.CollectBodies(rope).Count).IsEqual(8);
        AssertThat(rope.GetParticleCount()).IsEqual(9);
    }

    /// <summary> The particle data is laid out from the chain, so a destroyed rope reports neither. </summary>
    [TestCase]
    public async Task DestroyRopeRemovesTheParticleData()
    {
        var rope = CreateRope(simulationSegments: 5, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        AssertThat(rope.GetParticleCount()).IsEqual(6);

        rope.DestroyRope();

        AssertThat(rope.IsRopeCreated).IsFalse();
        AssertThat(rope.GetParticleCount()).IsEqual(0);
        AssertThat(rope.GetParticle(0) == null).IsTrue();
    }

    /// <summary>
    /// The particle positions are dropped with the old chain, so a rebuild after a teardown has to lay out both again.
    /// </summary>
    [TestCase]
    public async Task RopeIsRebuiltAfterBeingDestroyed()
    {
        var rope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        rope.DestroyRope();
        rope.CreateRope();
        await UnitHarness.NextFrame(rope);

        AssertThat(rope.IsRopeCreated).IsTrue();
        AssertThat(rope.GetParticleCount()).IsEqual(5);
        AssertThat(UnitHarness.CollectBodies(rope).Count).IsEqual(4);
    }

    /// <summary> The width is baked into the capsule of every segment, so a chain that already exists is rebuilt for it. </summary>
    [TestCase]
    public async Task RopeWidthWriteRebuildsTheRigidChain()
    {
        var rope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        rope.RopeWidth = 0.2f;
        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        var bodies = UnitHarness.CollectBodies(rope);
        var segmentShapes = CollectCapsuleShapes(rope);
        var distinctSegmentShapes = segmentShapes.Distinct().ToList();

        // The rope assigns one capsule instance to every segment, so its radius is checked once; the shape count still has
        // to match the body count, which is what a segment of another shape type breaks.
        AssertThat(bodies.Count).IsEqual(4);
        AssertThat(segmentShapes.Count)
            .OverrideFailureMessage($"every segment carries a capsule shape, got {segmentShapes.Count} capsule shapes for {bodies.Count} bodies")
            .IsEqual(bodies.Count);
        AssertThat(distinctSegmentShapes.Count)
            .OverrideFailureMessage($"the segments share one capsule shape, got {distinctSegmentShapes.Count} capsule shapes")
            .IsEqual(1);
        // The default collision margin is -0.01, so the collision width of the segment is 0.19.
        AssertThat(Mathf.IsEqualApprox(distinctSegmentShapes[0].Radius, 0.19f, 0.001f)).IsTrue();
    }

    /// <summary>
    /// A write of the length the chain already has must not rebuild it, or every repeated write from an inspector, a tween
    /// or a loop would tear the rope down and spawn it again frame after frame.
    /// </summary>
    [TestCase]
    public async Task WritingTheLengthItAlreadyHasLeavesTheChainAlone()
    {
        var rope = CreateRope(simulationSegments: 5, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        var bodies = UnitHarness.CollectBodies(rope);
        rope.RopeLength = 1.0f;

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        var bodiesAfterWrite = UnitHarness.CollectBodies(rope);

        AssertThat(bodiesAfterWrite.Count).IsEqual(5);
        AssertThat(bodiesAfterWrite.All(body => bodies.Contains(body))).IsTrue();
        // A chain that was rebuilt under the same count would have freed the bodies collected before the write.
        AssertThat(bodies.All(body => body != null && body.GetParent() == rope)).IsTrue();
    }

    /// <summary> A rebuild queued before the rope was destroyed has to be dropped, or the rope revives a frame after the user removed it, laid out from the world origin. </summary>
    [TestCase]
    public async Task AQueuedStructureApplyDoesNotReviveADestroyedRope()
    {
        var rope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        rope.SimulationSegments = 8;
        rope.DestroyRope();

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        AssertThat(rope.IsRopeCreated).IsFalse();
        AssertThat(rope.GetParticleCount()).IsEqual(0);
        AssertThat(UnitHarness.CollectBodies(rope).Count).IsEqual(0);
    }

    /// <summary> A rigid rope only understands a rigid joint, and the refusal happens before anything is built. </summary>
    [TestCase]
    public async Task RigidRopeRejectsASimulatedJointBeforeBuildingAnything()
    {
        var rope = new VerletRopeRigid
        {
            SimulationSegments = 4,
            RopeLength = 1.0f,
            IsCreatedOnReady = false,
            UseVisibleOnScreenNotifier = false
        };
        var joint = AutoFree(new VerletJointSimulated());
        rope.SetJoint(joint, false);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);

        var thrown = UnitHarness.CatchException(() => rope.CreateRope());

        AssertThat(thrown is ApplicationException).IsTrue();
        AssertThat(thrown!.Message.Contains(nameof(VerletRopeRigid))).IsTrue();
        AssertThat(thrown.Message.Contains(nameof(VerletJointRigid))).IsTrue();
        AssertThat(rope.IsRopeCreated).IsFalse();
        AssertThat(UnitHarness.CollectBodies(rope).Count).IsEqual(0);
    }

    /// <summary>
    /// An end attachment is what pins the last segment, while a rope with no start pin keeps only the joints between its
    /// segments.
    /// </summary>
    [TestCase]
    public async Task TheEndAttachmentPinsTheLastSegmentAndAnUnpinnedStartLeavesTheFirstOneFree()
    {
        var pinnedRope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);
        var endAnchor = new Node3D { Name = "EndAnchor", Position = new Vector3(0.0f, -1.0f, 0.0f) };
        pinnedRope.AddChild(endAnchor);
        pinnedRope.SetAttachmentPoints(null, null, null, endAnchor);

        var freeRope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);
        freeRope.IsStartPinned = false;
        freeRope.Position = new Vector3(6.0f, 0.0f, 0.0f);

        var rig = new Node3D { Name = "RigidRig" };
        rig.AddChild(pinnedRope);
        rig.AddChild(freeRope);

        ISceneRunner.Load(rig);
        await UnitHarness.NextFrame(rig);

        var pinnedBodies = UnitHarness.CollectBodies(pinnedRope);
        var freeBodies = UnitHarness.CollectBodies(freeRope);
        var pinnedJoints = pinnedBodies.Sum(body => body.GetChildren().OfType<PinJoint3D>().Count());
        var freeJoints = freeBodies.Sum(body => body.GetChildren().OfType<PinJoint3D>().Count());

        // The joints between the four segments, plus the start anchor and the end attachment.
        AssertThat(pinnedBodies.Count).IsEqual(4);
        AssertThat(pinnedJoints).IsEqual(5);
        AssertThat(pinnedBodies[0].GetChildren().OfType<PinJoint3D>().Count()).IsEqual(2);
        AssertThat(pinnedBodies[^1].GetChildren().OfType<PinJoint3D>().Count()).IsEqual(1);
        // Only the three joints between the segments are left when neither end of the chain is held.
        AssertThat(freeBodies.Count).IsEqual(4);
        AssertThat(freeJoints).IsEqual(3);
        AssertThat(freeBodies[0].GetChildren().OfType<PinJoint3D>().Count()).IsEqual(1);
        AssertThat(freeBodies[^1].GetChildren().OfType<PinJoint3D>().Count()).IsEqual(0);
        AssertThat(pinnedRope.GetParticleCount()).IsEqual(5);
    }

    #region Utility

    private static List<CapsuleShape3D> CollectCapsuleShapes(VerletRopeRigid rope)
    {
        return UnitHarness.CollectBodies(rope)
            .SelectMany(body => body.GetChildren().OfType<CollisionShape3D>())
            .Select(shape => shape.Shape)
            .OfType<CapsuleShape3D>()
            .ToList();
    }

    private static VerletRopeRigid CreateRope(int simulationSegments, float ropeLength)
    {
        return new VerletRopeRigid
        {
            SimulationSegments = simulationSegments,
            RopeLength = ropeLength,
            UseVisibleOnScreenNotifier = false
        };
    }

    #endregion
}
