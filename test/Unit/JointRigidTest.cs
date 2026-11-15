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
/// Covers what a joint does to the rope it belongs to: an attachment write that changed nothing has to leave the chain
/// alone, while a moved attachment point has to rebuild it. That decision is the snapshot every reactive joint property
/// goes through, which is why it is worth its own suite instead of one case per joint type.
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
    /// A joint decides what a written property has to apply by: the distance joint values are read on every frame, so a
    /// write of one has to reach the child joint that reads them and leave the rope where it is - only an attachment or a
    /// rope change may re-lay it. The other half of the same bookkeeping is leaving the tree, where the rope has to be left
    /// without a joint instead of keeping one that is gone from it. The configuration values only exist on
    /// <see cref="VerletJointSimulated"/>, so that half is measured on the rope type it belongs to.
    /// </summary>
    [TestCase]
    public async Task ConfigurationWritesLeaveTheRopeAloneAndLeavingTheTreeDetachesTheJoint()
    {
        var startBody = new RigidBody3D { Name = "StartBody" };
        var simulatedRope = CreateSimulatedRope();
        var simulatedJoint = new VerletJointSimulated { Name = "JointSimulated", StartBody = startBody };
        simulatedRope.AddChild(startBody);
        simulatedRope.AddChild(simulatedJoint);

        var rigidRope = CreateRope(out _, out var rigidJoint);

        rigidRope.Position = new Vector3(6.0f, 0.0f, 0.0f);

        var rig = new Node3D { Name = "JointRig" };
        rig.AddChild(simulatedRope);
        rig.AddChild(rigidRope);

        ISceneRunner.Load(rig);
        await UnitHarness.NextFrame(rig);

        for (var frame = 0; frame < 60; frame++)
        {
            simulatedRope._PhysicsProcess(1.0 / 60.0);
        }

        var hangingDistance = GetFarthestParticleDistance(simulatedRope);

        simulatedJoint.JointMaxDistance = 1.0f;
        simulatedJoint.JointMaxForce = 25.0f;
        simulatedJoint.JointForceEasing = 0.5f;

        await UnitHarness.NextFrame(rig);
        await UnitHarness.NextFrame(rig);

        // The write has to reach the node the joint type reads its distance values from, which is what a configuration
        // reason applies and an attachment or rope reason does not have to.
        var distanceJoint = simulatedJoint.GetChildren().OfType<DistanceForceJoint>().FirstOrDefault();

        AssertThat(distanceJoint == null).OverrideFailureMessage("the distance joint values were written but no distance joint was configured for them").IsFalse();
        AssertThat(distanceJoint.MaxDistance).IsEqualApprox(1.0f, 0.0001f);
        AssertThat(distanceJoint.MaxForce).IsEqualApprox(25.0f, 0.0001f);
        AssertThat(distanceJoint.ForceEasing).IsEqualApprox(0.5f, 0.0001f);

        // And it must not have re-laid the rope: a re-laid rope is back in the clump it is created as, within centimetres
        // of its start, while the hanging one it was is measured in metres.
        var distanceAfterWrites = GetFarthestParticleDistance(simulatedRope);

        AssertThat(distanceAfterWrites).OverrideFailureMessage($"the rope was re-laid: hanging {hangingDistance:F2}, now {distanceAfterWrites:F2}").IsGreater(hangingDistance * 0.5f);
        AssertThat(simulatedRope.GetParticleCount()).IsEqual(8);

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

    /// <summary> A rigid rope with a rigid joint on it, the joint attached to a marker node the chain starts from. </summary>
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
