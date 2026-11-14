using Godot;
using System.Threading.Tasks;
using VerletRope4.Demo.Examples;
using VerletRope4.Physics;

namespace VerletRope4.Tests.Scenes.Scenarios;

/// <summary>
/// A hook on a rope that the demo cycle fires on a timer: the cycle has to leave the parked state on its own, the rope
/// has to keep its particles and its end has to stay on the hook.
/// </summary>
public sealed class GrapplingHookScenario : SceneScenario
{
    public override string ScenePath => "res://demo/examples/4_grappling_hook.tscn";

    public override async Task RunAsync(Node root, SceneChecks checks)
    {
        var rope = root.GetNode<VerletRopeSimulated>("GrappleRope");
        var hook = root.GetNode<GrapplingHookRig>("HookBody");
        var attachment = root.GetNode<Node3D>("HookBody/Attachment");

        checks.Check(rope.IsRopeCreated, "the grapple rope is created on ready");
        checks.Check(rope.GetParticleCount() == 20, $"the grapple rope keeps its 20 configured particles, got {rope.GetParticleCount()}");
        checks.Check(hook.Phase == GrapplingHookRig.HookPhase.Parked, $"the hook starts parked in the hand, phase is {hook.Phase}");

        var hookStart = hook.GlobalPosition;

        // The demo holds the hook for 1.5 s before it fires, so the run has to outlast that to see the cycle move.
        await WaitPhysicsFrames(root, 150);

        var hookTravel = hook.GlobalPosition.DistanceTo(hookStart);

        checks.Check(hook.Phase != GrapplingHookRig.HookPhase.Parked, $"the demo cycle fires the hook on its own, phase is {hook.Phase}");
        checks.Check(hookTravel > 0.5f, $"the hook leaves the hand, travelled {hookTravel:F2}");
        checks.Check(rope.GetParticleCount() == 20, $"the rope keeps its particles through the cycle, got {rope.GetParticleCount()}");
        checks.Check(rope.GetParticlePosition(-1).DistanceTo(attachment.GlobalPosition) < 1.0f, "the rope end follows the hook");
        checks.CheckParticlesAreFinite(rope, "the grapple rope particle positions stay finite");
    }
}
