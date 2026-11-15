using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Demo.Examples;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `4_grappling_hook`, a hook on a rope that the demo cycle fires on a timer: the cycle has to leave the parked
/// state on its own, the rope has to keep its particles and its end has to stay on the hook.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class GrapplingHookTest
{
    [TestCase]
    public async Task HookFiresOnItsOwnAndTheRopeFollows()
    {
        await SceneHarness.RunAsync("res://demo/examples/4_grappling_hook.tscn", RunSceneAsync);
    }

    #region Utility

    /// <summary>
    /// Distance the rope end is allowed to be behind the attachment it is written from. The end particle is written on
    /// every physics step, so the only distance left is the frame the hook moved in after that write: measured 0.0000 m on
    /// the hooked hook, and 0.3 m covers a frame of the rig recalling at its 12 m/s.
    /// </summary>
    private const float EndLagDistance = 0.3f;

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var rope = root.GetNode<VerletRopeSimulated>("GrappleRope");
        var hook = root.GetNode<GrapplingHookRig>("HookBody");
        var attachment = root.GetNode<Node3D>("HookBody/Attachment");
        var demo = root.GetNode<GrapplingHookDemo>("DemoCycle");

        checks.Check(rope.IsRopeCreated, "the grapple rope is created on ready");
        checks.Check(rope.GetParticleCount() == 20, $"the grapple rope keeps its 20 configured particles, got {rope.GetParticleCount()}");
        checks.Check(hook.Phase == GrapplingHookRig.HookPhase.Parked, $"the hook starts parked in the hand, phase is {hook.Phase}");

        var hookStart = hook.GlobalPosition;

        // The demo holds the hook for IdleTime before it fires and gives the flight FlightTimeout to hit something, so
        // outlasting both is what makes the parked state impossible to be the one this run measures.
        var cycleTicks = Mathf.RoundToInt((demo.IdleTime + demo.FlightTimeout) * DemoCadence.PhysicsTicksPerSecond) + DemoCadence.PhaseMarginTicks;

        await SceneHarness.WaitPhysicsFrames(root, cycleTicks);

        var hookTravel = hook.GlobalPosition.DistanceTo(hookStart);

        checks.Check(hook.Phase != GrapplingHookRig.HookPhase.Parked, $"the demo cycle fires the hook on its own, phase is {hook.Phase}");
        checks.Check(hookTravel > 0.5f, $"the hook leaves the hand, travelled {hookTravel:F2}");
        checks.Check(rope.GetParticleCount() == 20, $"the rope keeps its particles through the cycle, got {rope.GetParticleCount()}");
        // The rope writes its last particle from the attachment node of the hook, so the only distance left is the frame
        // the hook moved in after that write.
        checks.CheckAtMost(rope.GetParticlePosition(-1).DistanceTo(attachment.GlobalPosition), EndLagDistance, "the rope end follows the hook");
        checks.CheckParticlesAreFinite(rope, "the grapple rope particle positions stay finite");
    }

    #endregion
}
