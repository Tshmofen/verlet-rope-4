using Godot;
using System.Threading.Tasks;
using VerletRope4.Demo.Examples;
using VerletRope4.Physics;

namespace VerletRope4.Tests.Scenes.Scenarios;

/// <summary>
/// A fishing line tied to a rod tip with a hook on its end: the line has to be created tied to the tip, the hook has to
/// hang under the rod while the demo waits, and the cast has to throw it away from the tip on its own.
/// </summary>
public sealed class FishingRodScenario : SceneScenario
{
    public override string ScenePath => "res://demo/examples/5_fishing_rod.tscn";

    public override async Task RunAsync(Node root, SceneChecks checks)
    {
        var line = root.GetNode<VerletRopeSimulated>("Rod/LineRope");
        var rod = root.GetNode<FishingRodRig>("Rod");
        var hook = root.GetNode<RigidBody3D>("Rod/Hook");
        var rodTip = root.GetNode<Node3D>("Rod/Grip/RodTipAnchor");

        checks.Check(line.IsRopeCreated, "the fishing line is created on ready");
        checks.Check(line.GetParticleCount() == 16, $"the line keeps its 16 configured particles, got {line.GetParticleCount()}");
        checks.Check(line.GetParticle(0)?.IsAttached == true, "the line is tied to the rod tip");

        // The demo rests for 1.2 s before the backswing, which is the only stretch the hook is hanging.
        await WaitPhysicsFrames(root, 60);

        var hangingDistance = hook.GlobalPosition.DistanceTo(rodTip.GlobalPosition);

        checks.Check(rod.Phase == FishingRodRig.RodPhase.Idle, $"the rod rests with the hook hanging, phase is {rod.Phase}");
        checks.CheckRange(hangingDistance, 0.2f, 0.7f, "the hook hangs under the rod tip at about the line length");
        checks.CheckRopeIntegrity(line, "the line stays in one piece while the hook hangs");

        var tipStart = rodTip.GlobalPosition;
        var lineStart = line.GetParticlePosition(0);

        // 1.5 s later the backswing is over and the cast has thrown the hook off the tip.
        await WaitPhysicsFrames(root, 90);

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
}
