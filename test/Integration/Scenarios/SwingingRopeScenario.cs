using Godot;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration.Scenarios;

/// <summary>
/// A rope hanging off a swaying branch: it has to be created, stay tied to the branch, hang at about its own length and
/// keep moving with the branch it is tied to.
/// </summary>
public sealed class SwingingRopeScenario : SceneScenario
{
    public override string ScenePath => "res://demo/examples/1_swinging_rope.tscn";

    public override async Task RunAsync(Node root, SceneChecks checks)
    {
        var vine = root.GetNode<VerletRopeSimulated>("Branch/Vine");

        checks.Check(vine.IsRopeCreated, "the vine rope is created on ready");
        checks.Check(vine.GetParticleCount() == 15, $"the vine keeps its 15 configured particles, got {vine.GetParticleCount()}");
        checks.Check(vine.GetParticle(0)?.IsAttached == true, "the start particle is pinned to the branch");

        // The rope unwraps from a jittered point on creation, so it is given three seconds to hang before it is measured.
        await WaitPhysicsFrames(root, 180);

        var settledEnd = vine.GetParticlePosition(-1);
        await WaitPhysicsFrames(root, 60);

        var movedEnd = vine.GetParticlePosition(-1);

        checks.Check(movedEnd.DistanceTo(settledEnd) > 0.02f, "the free end keeps moving once the rope has settled");
        checks.Check(movedEnd.Y < vine.GlobalPosition.Y - 0.5f, "the rope hangs below the anchor it is tied to");
        checks.CheckRange(vine.GetCurrentRopeLength(), 2.0f, 4.5f, "the rope hangs at about its rest length");
        checks.Check(vine.GetParticlePosition(0).DistanceTo(vine.GlobalPosition) < 0.05f, "the start particle stays at the rope node");
        checks.CheckRopeIntegrity(vine, "the rope stays in one piece");
        checks.CheckParticlesAreFinite(vine, "the rope particle positions stay finite");
    }
}
