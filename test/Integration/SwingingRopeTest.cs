using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives `1_swinging_rope`, a vine hanging off a branch that sways. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class SwingingRopeTest
{
    [TestCase]
    public async Task VineHangsFromTheSwayingBranch()
    {
        await SceneHarness.RunAsync("res://demo/examples/1_swinging_rope.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var vine = root.GetNode<VerletRopeSimulated>("Branch/Vine");

        checks.Check(vine.IsRopeCreated, "the vine rope is created on ready");
        checks.Check(vine.GetParticleCount() == 15, $"the vine keeps its 15 configured particles, got {vine.GetParticleCount()}");

        // The rope unwraps from a jittered point on creation, so it is given three seconds to hang before it is measured.
        await SceneHarness.WaitPhysicsFrames(root, 180);

        var settledEnd = vine.GetParticlePosition(-1);
        await SceneHarness.WaitPhysicsFrames(root, 60);

        var movedEnd = vine.GetParticlePosition(-1);

        checks.Check(movedEnd.DistanceTo(settledEnd) > 0.02f, "the free end keeps moving once the rope has settled");
        checks.Check(movedEnd.Y < vine.GlobalPosition.Y - 0.5f, "the rope hangs below the anchor it is tied to");
        checks.CheckRange(vine.GetCurrentRopeLength(), 2.0f, 4.5f, "the rope hangs at about its rest length");
        // The first particle is written from the node the rope is tied to on every physics step: measured 0.0000 m once the
        // rope has settled.
        checks.Check(vine.GetParticlePosition(0).DistanceTo(vine.GlobalPosition) < 0.05f, "the start particle stays at the rope node");
        checks.CheckRopeIntegrity(vine, "the rope stays in one piece");
        checks.CheckParticlesAreFinite(vine, "the rope particle positions stay finite");
    }

    #endregion
}
