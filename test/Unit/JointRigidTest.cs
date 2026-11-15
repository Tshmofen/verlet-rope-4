using GdUnit4;
using Godot;
using System.Linq;
using System.Threading.Tasks;
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

    #region Utility

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
