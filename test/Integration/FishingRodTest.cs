using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Demo.Examples;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives `5_fishing_rod`, a fishing line tied to a rod tip with a hook on its end. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class FishingRodTest
{
    [TestCase]
    public async Task CastThrowsTheHookOffTheRodTip()
    {
        await SceneHarness.RunAsync("res://demo/examples/5_fishing_rod.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var line = root.GetNode<VerletRopeSimulated>("Rod/LineRope");
        var rod = root.GetNode<FishingRodRig>("Rod");
        var hook = root.GetNode<RigidBody3D>("Rod/Hook");
        var rodTip = root.GetNode<Node3D>("Rod/Grip/RodTipAnchor");
        var demo = root.GetNode<FishingRodDemo>("DemoCycle");

        checks.Check(line.IsRopeCreated, "the fishing line is created on ready");
        checks.Check(line.GetParticleCount() == 16, $"the line keeps its 16 configured particles, got {line.GetParticleCount()}");
        // The line writes its first particle from the rod tip it is tied to on every physics step, so a tip it was never
        // tied to would show up as a distance: measured 0.0000 m on creation.
        checks.CheckAtMost(line.GetParticlePosition(0).DistanceTo(rodTip.GlobalPosition), 0.05f, "the line is tied to the rod tip");

        // The rod rests with the hook hanging for IdleTime, so the sample is taken a margin short of the end of that
        // window instead of at a frame count of its own.
        var idleTicks = Mathf.RoundToInt(demo.IdleTime * DemoCadence.PhysicsTicksPerSecond);
        var hangingTicks = idleTicks - DemoCadence.PhaseMarginTicks;

        await SceneHarness.WaitPhysicsFrames(root, hangingTicks);

        var hangingDistance = hook.GlobalPosition.DistanceTo(rodTip.GlobalPosition);

        checks.Check(rod.Phase == FishingRodRig.RodPhase.Idle, $"the rod rests with the hook hanging, phase is {rod.Phase}");
        checks.CheckRange(hangingDistance, 0.2f, 0.7f, "the hook hangs under the rod tip at about the line length");
        checks.CheckRopeIntegrity(line, "the line stays in one piece while the hook hangs");

        var tipStart = rodTip.GlobalPosition;
        var lineStart = line.GetParticlePosition(0);

        // The backswing and the swing are over BackswingTime + SwingTime after that window, and that is when the cast has
        // thrown the hook off the tip, so the rest of the cycle is waited out from the same durations.
        var castTicks = Mathf.RoundToInt((demo.IdleTime + demo.BackswingTime + demo.SwingTime) * DemoCadence.PhysicsTicksPerSecond) - hangingTicks + DemoCadence.PhaseMarginTicks;

        await SceneHarness.WaitPhysicsFrames(root, castTicks);

        var castDistance = hook.GlobalPosition.DistanceTo(rodTip.GlobalPosition);
        var tipTravel = rodTip.GlobalPosition - tipStart;
        var lineTravel = line.GetParticlePosition(0) - lineStart;

        checks.Check(rod.Phase != FishingRodRig.RodPhase.Idle, $"the demo cycle casts the line, phase is {rod.Phase}");
        checks.Check(castDistance > 0.8f, $"the cast throws the hook away from the rod tip, distance {castDistance:F2}");
        checks.Check(
            lineTravel.DistanceTo(tipTravel) < 0.3f,
            $"the line end travels with the rod tip (rod tip moved {tipTravel.Length():F2}, line end {lineTravel.Length():F2})"
        );
        checks.CheckParticlesAreFinite(line, "the line particle positions stay finite");
    }

    #endregion
}
