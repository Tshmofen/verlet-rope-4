using GdUnit4;
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
using VerletRope4.Tests.Unit.Harness;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary>
/// The attachment snapshot is the decision every reactive joint property goes through, which is why a joint is covered by
/// what it does to its rope instead of one case per joint type.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "unit")]
public class JointRigidTest
{
    [TestCase]
    public async Task UnchangedAttachmentWriteLeavesTheChainAlone()
    {
        var rope = CreateRope(out var anchor, out var joint);

        await LoadRope(rope);

        var bodies = UnitHarness.CollectBodies(rope);
        joint.StartCustomLocation = anchor;

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        var bodiesAfterWrite = UnitHarness.CollectBodies(rope);

        AssertThat(bodies.Count).IsEqual(4);
        AssertThat(bodiesAfterWrite.Count).IsEqual(4);
        AssertThat(bodiesAfterWrite.All(body => bodies.Contains(body))).IsTrue();
    }

    [TestCase]
    public async Task ChangedAttachmentWriteRelaysTheChain()
    {
        var rope = CreateRope(out _, out var joint);
        var otherAnchor = new Node3D { Name = "OtherAnchor", Position = new Vector3(0.0f, 0.0f, 2.0f) };
        rope.AddChild(otherAnchor);

        await LoadRope(rope);

        var bodies = UnitHarness.CollectBodies(rope);
        joint.StartCustomLocation = otherAnchor;

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        var bodiesAfterWrite = UnitHarness.CollectBodies(rope);

        AssertThat(bodiesAfterWrite.Count).IsEqual(4);
        AssertThat(bodiesAfterWrite.Any(body => bodies.Contains(body))).IsFalse();
        AssertThat(rope.GetParticlePosition(0).DistanceTo(otherAnchor.GlobalPosition)).IsLess(0.1f);
    }

    /// <summary>
    /// The distance joint values are read on every frame, so a write of one has to reach the child joint that reads them and
    /// leave the rope where it is - only an attachment or a rope change may re-lay it.
    /// </summary>
    [TestCase]
    public async Task ConfigurationWritesReachTheDistanceJointAndLeaveTheRopeWhereItIs()
    {
        var startBody = new RigidBody3D { Name = "StartBody" };
        var simulatedRope = CreateSimulatedRope();
        var simulatedJoint = new VerletJointSimulated { Name = "JointSimulated", StartBody = startBody };
        simulatedRope.AddChild(startBody);
        simulatedRope.AddChild(simulatedJoint);

        ISceneRunner.Load(simulatedRope);
        await UnitHarness.NextFrame(simulatedRope);

        for (var frame = 0; frame < 60; frame++)
        {
            simulatedRope._PhysicsProcess(1.0 / 60.0);
        }

        var hangingDistance = GetFarthestParticleDistance(simulatedRope);

        simulatedJoint.JointMaxDistance = 1.0f;
        simulatedJoint.JointMaxForce = 25.0f;
        simulatedJoint.JointForceEasing = 0.5f;

        await UnitHarness.NextFrame(simulatedRope);
        await UnitHarness.NextFrame(simulatedRope);

        // The write has to reach the node the joint type reads its distance values from.
        var distanceJoint = simulatedJoint.GetChildren().OfType<DistanceForceJoint>().FirstOrDefault();

        AssertThat(distanceJoint == null).OverrideFailureMessage("the distance joint values were written but no distance joint was configured for them").IsFalse();
        AssertThat(distanceJoint.MaxDistance).IsEqualApprox(1.0f, 0.0001f);
        AssertThat(distanceJoint.MaxForce).IsEqualApprox(25.0f, 0.0001f);
        AssertThat(distanceJoint.ForceEasing).IsEqualApprox(0.5f, 0.0001f);

        // A re-laid rope is back in the clump it is created as, within centimetres of its start, while the hanging one it
        // was is measured in metres.
        var distanceAfterWrites = GetFarthestParticleDistance(simulatedRope);

        AssertThat(distanceAfterWrites).OverrideFailureMessage($"the rope was re-laid: hanging {hangingDistance:F2}, now {distanceAfterWrites:F2}").IsGreater(hangingDistance * 0.5f);
        AssertThat(simulatedRope.GetParticleCount()).IsEqual(8);
    }

    /// <summary>
    /// While the joint is still there the rope refuses an attachment write, and once it has left the tree the same write
    /// goes through and the chain it built stays where it was.
    /// </summary>
    [TestCase]
    public async Task LeavingTheTreeDetachesTheJointFromTheRope()
    {
        var rigidRope = CreateRope(out _, out var rigidJoint);

        ISceneRunner.Load(rigidRope);
        await UnitHarness.NextFrame(rigidRope);

        var bodies = UnitHarness.CollectBodies(rigidRope);
        var thrownWhileConnected = UnitHarness.CatchException(() => rigidRope.SetAttachmentPoints(null, null, null, null));

        rigidRope.RemoveChild(rigidJoint);

        var thrownAfterLeaving = UnitHarness.CatchException(() => rigidRope.SetAttachmentPoints(null, null, null, null));
        var bodiesAfterLeaving = UnitHarness.CollectBodies(rigidRope);

        AssertThat(bodies.Count).IsEqual(4);
        AssertThat(thrownWhileConnected is ApplicationException).IsTrue();
        AssertThat(thrownAfterLeaving == null).OverrideFailureMessage($"the rope still holds the joint it left: {thrownAfterLeaving}").IsTrue();
        AssertThat(rigidRope.GetChildren().OfType<VerletJointRigid>().Any()).IsFalse();
        AssertThat(bodiesAfterLeaving.Count).IsEqual(4);
        AssertThat(bodiesAfterLeaving.All(body => bodies.Contains(body))).IsTrue();
    }

    #region Utility

    private static float GetFarthestParticleDistance(VerletRopeSimulated rope)
    {
        var farthestDistance = 0.0f;

        for (var i = 0; i < rope.GetParticleCount(); i++)
        {
            farthestDistance = Mathf.Max(farthestDistance, rope.GetParticlePosition(i).DistanceTo(rope.GlobalPosition));
        }

        return farthestDistance;
    }

    private static VerletRopeSimulated CreateSimulatedRope()
    {
        return new VerletRopeSimulated
        {
            SimulationParticles = 8,
            RopeLength = 1.5f,
            UseVisibleOnScreenNotifier = false,
            IsDisabledWhenInvisible = false,
            SimulationBehavior = RopeSimulationBehavior.Editor
        };
    }

    /// <summary> A rigid rope with a rigid joint whose start attachment is a marker node. </summary>
    private static VerletRopeRigid CreateRope(out Node3D anchor, out VerletJointRigid joint)
    {
        var rope = new VerletRopeRigid
        {
            SimulationSegments = 4,
            RopeLength = 1.0f,
            UseVisibleOnScreenNotifier = false
        };

        anchor = new Node3D { Name = "Anchor" };
        joint = new VerletJointRigid { Name = "JointRigid", StartCustomLocation = anchor };
        rope.AddChild(anchor);
        rope.AddChild(joint);

        return rope;
    }

    private static async Task LoadRope(VerletRopeRigid rope)
    {
        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);
    }

    #endregion
}
