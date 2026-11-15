using GdUnit4;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
using VerletRope4.Tests.Unit.Harness;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary> Covers the configuration warnings the simulated joint reports and the findings they are built from. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "unit")]
public class JointSimulatedTest
{
    [TestCase]
    public void UnconfiguredJointWarnsAboutItsMissingConnectionPoints()
    {
        var joint = AutoFree(new VerletJointSimulated());

        var warnings = joint._GetConfigurationWarnings();

        AssertThat(warnings.Length).IsEqual(2);
        AssertThat(warnings.Any(warning => warning.Contains("No custom connection points"))).IsTrue();
        AssertThat(warnings.Any(warning => warning.Contains("without an associated rope"))).IsTrue();
    }

    [TestCase]
    public void ConfiguredJointReportsNoWarnings()
    {
        var rope = AutoFree(new VerletRopeSimulated());
        var body = AutoFree(new RigidBody3D());
        var joint = AutoFree(new VerletJointSimulated { VerletRope = rope, StartBody = body });

        AssertThat(joint._GetConfigurationWarnings().Length).IsEqual(0);
    }

    /// <summary> A distance joint without bodies cannot pull anything, and a joint on an invisible-disabled rope can stop tracking it off-screen. </summary>
    [TestCase]
    public void DistanceJointWarnsAboutMissingBodiesAndAnInvisibilityDisabledRope()
    {
        var rope = AutoFree(new VerletRopeSimulated { IsDisabledWhenInvisible = true });
        var joint = AutoFree(new VerletJointSimulated { VerletRope = rope, JointMaxDistance = 2.0f });

        var warnings = joint._GetConfigurationWarnings();

        AssertThat(warnings.Any(warning => warning.Contains(nameof(VerletJointSimulated.JointMaxDistance)))).IsTrue();
        AssertThat(warnings.Any(warning => warning.Contains(nameof(VerletRopeSimulated.IsDisabledWhenInvisible)))).IsTrue();
    }

    [TestCase]
    public void JointFindsItsRopeAsTheParentNode()
    {
        var rope = AutoFree(new VerletRopeSimulated());
        var joint = AutoFree(new VerletJointSimulated());
        rope.AddChild(joint);

        joint.ResetJoint(false);

        AssertThat(joint.VerletRope).IsSame(rope);
    }

    /// <summary>
    /// The ignore flags are not a value the rope reads back: a joint pushes the bodies its rope has to leave out into the
    /// rope's collision exceptions, which is what keeps a rope from colliding with the body it is tied to. The list lives
    /// on the rope and is only handed to the rope's own raycast, so the case reads it where the rope keeps it.
    /// </summary>
    [TestCase]
    public async Task CollisionIgnoreFlagsReachTheRopeAsBodyRids()
    {
        var startBody = new RigidBody3D { Name = "StartBody" };
        var endBody = new RigidBody3D { Name = "EndBody" };
        var rope = CreateAnchoredRope();
        var joint = new VerletJointSimulated { Name = "JointSimulated", VerletRope = rope, StartBody = startBody, EndBody = endBody };
        rope.AddChild(startBody);
        rope.AddChild(endBody);
        rope.AddChild(joint);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        // Both bodies are ignored out of the box, which is the default of both ignore flags.
        var exceptions = GetCollisionExceptions(rope);

        AssertThat(exceptions.Count).IsEqual(2);
        AssertThat(exceptions.Contains(startBody.GetRid())).IsTrue();
        AssertThat(exceptions.Contains(endBody.GetRid())).IsTrue();

        joint.IgnoreStartBodyCollision = false;
        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        exceptions = GetCollisionExceptions(rope);

        AssertThat(exceptions.Count).IsEqual(1);
        AssertThat(exceptions.Contains(endBody.GetRid())).IsTrue();

        joint.IgnoreEndBodyCollision = false;
        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        AssertThat(GetCollisionExceptions(rope).Count).IsEqual(0);

        joint.IgnoreStartBodyCollision = true;
        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        exceptions = GetCollisionExceptions(rope);

        AssertThat(exceptions.Count).IsEqual(1);
        AssertThat(exceptions[0] == startBody.GetRid()).IsTrue();
    }

    /// <summary>
    /// A script reload on a joint re-applies it through <see cref="BaseVerletJoint.ResetJoint"/>, and an attachment point is
    /// what a rope is laid out between - so the rope is attached to where the joint points at that moment, one frame after
    /// the reload asked for it.
    /// </summary>
    [TestCase]
    public async Task ScriptReloadOnTheJointRelaysTheRopeBetweenItsAttachmentPoints()
    {
        var rope = CreateAnchoredRope();
        var endAnchor = new Node3D { Name = "EndAnchor", Position = new Vector3(0.0f, 0.0f, 1.0f) };
        var joint = new VerletJointSimulated { Name = "JointSimulated", EndCustomLocation = endAnchor };
        rope.AddChild(endAnchor);
        rope.AddChild(joint);

        ISceneRunner.Load(rope);
        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        AssertThat(rope.GetParticlePosition(-1).DistanceTo(endAnchor.GlobalPosition)).IsLess(0.05f);

        endAnchor.Position = new Vector3(0.0f, 0.0f, 2.0f);
        joint.OnAfterDeserialize();

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        AssertThat(rope.GetParticle(-1)!.Value.IsAttached).IsTrue();
        AssertThat(rope.GetParticlePosition(-1).DistanceTo(endAnchor.GlobalPosition)).IsLess(0.05f);
        AssertThat(rope.GetParticleCount()).IsEqual(6);
    }

    #region Utility

    /// <summary> A rope that lays itself out on ready and is not gated by the visibility of a screen notifier. </summary>
    private static VerletRopeSimulated CreateAnchoredRope()
    {
        return new VerletRopeSimulated
        {
            SimulationParticles = 6,
            RopeLength = 1.5f,
            UseVisibleOnScreenNotifier = false,
            IsDisabledWhenInvisible = false,
            SimulationBehavior = RopeSimulationBehavior.Editor
        };
    }

    /// <summary> The bodies the rope leaves out of its collision rays, which no public member of the rope exposes. </summary>
    private static List<Rid> GetCollisionExceptions(VerletRopeSimulated rope)
    {
        var field = typeof(VerletRopeSimulated).GetField("_collisionExceptions", BindingFlags.Instance | BindingFlags.NonPublic);
        var exceptions = field?.GetValue(rope) as List<Rid>;

        AssertThat(exceptions == null)
            .OverrideFailureMessage($"`{nameof(VerletRopeSimulated)}` no longer keeps its collision exceptions in `_collisionExceptions`.")
            .IsFalse();

        return exceptions;
    }

    #endregion
}
