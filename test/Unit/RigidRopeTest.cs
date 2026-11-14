using GdUnit4;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Physics;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary>
/// Covers the rigid rope node, whose structure is a chain of <see cref="RigidBody3D"/> segments pinned together rather
/// than a solved particle buffer - which is why its length and segment count are rebuilt instead of applied.
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
        await NextFrame(rope);

        var bodies = CollectBodies(rope);

        AssertThat(rope.IsRopeCreated).IsTrue();
        AssertThat(rope.GetParticleCount()).IsEqual(7);
        AssertThat(bodies.Count).IsEqual(6);
        AssertThat(bodies.All(body => body.GetChildren().OfType<CollisionShape3D>().Any())).IsTrue();
    }

    /// <summary>
    /// Every segment is pinned to the next one, and the first one is pinned to the (empty) start body slot, which is a
    /// world anchor. Nothing pins the last segment, because the rope has no end attachment.
    /// </summary>
    [TestCase]
    public async Task SegmentBodiesArePinnedTogetherAndToTheStart()
    {
        var rope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await NextFrame(rope);

        var bodies = CollectBodies(rope);
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

        await NextFrame(rope);
        await NextFrame(rope);

        var bodies = CollectBodies(rope);
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

        await NextFrame(rope);
        await NextFrame(rope);

        AssertThat(CollectBodies(rope).Count).IsEqual(8);
        AssertThat(rope.GetParticleCount()).IsEqual(9);
    }

    /// <summary> Destroying the rope drops the chain and the particle data laid out from it, so nothing stale is reported. </summary>
    [TestCase]
    public async Task DestroyRopeRemovesTheParticleData()
    {
        var rope = CreateRope(simulationSegments: 5, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await NextFrame(rope);

        AssertThat(rope.GetParticleCount()).IsEqual(6);

        rope.DestroyRope();

        AssertThat(rope.IsRopeCreated).IsFalse();
        AssertThat(rope.GetParticleCount()).IsEqual(0);
        AssertThat(rope.GetParticle(0) == null).IsTrue();
    }

    /// <summary>
    /// A rebuild after a teardown starts from nothing - the particle positions were already dropped with the old chain, so
    /// the rope has to lay out both again instead of reporting the destroyed one.
    /// </summary>
    [TestCase]
    public async Task RopeIsRebuiltAfterBeingDestroyed()
    {
        var rope = CreateRope(simulationSegments: 4, ropeLength: 1.0f);

        ISceneRunner.Load(rope);
        await NextFrame(rope);

        rope.DestroyRope();
        rope.CreateRope();
        await NextFrame(rope);

        AssertThat(rope.IsRopeCreated).IsTrue();
        AssertThat(rope.GetParticleCount()).IsEqual(5);
        AssertThat(CollectBodies(rope).Count).IsEqual(4);
    }

    #region Utility

    private static VerletRopeRigid CreateRope(int simulationSegments, float ropeLength)
    {
        return new VerletRopeRigid
        {
            SimulationSegments = simulationSegments,
            RopeLength = ropeLength,
            UseVisibleOnScreenNotifier = false
        };
    }

    private static List<RigidBody3D> CollectBodies(VerletRopeRigid rope)
    {
        return rope.GetChildren()
            .OfType<RigidBody3D>()
            // A rebuild queues the bodies it replaces for free, and they stay children until the frame ends.
            .Where(body => !body.IsQueuedForDeletion())
            .ToList();
    }

    private static async Task NextFrame(Node node)
    {
        await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    #endregion
}
