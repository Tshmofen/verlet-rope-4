using GdUnit4;
using Godot;
using System.Linq;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
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

    /// <summary> A distance joint without bodies cannot pull anything, and a joint on an invisible-disabled rope can stop tracking it off screen. </summary>
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
}
