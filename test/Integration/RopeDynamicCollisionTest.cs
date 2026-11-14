using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `demo/rope_dynamic_collision`, a purpose-built scene instead of a shipped example: a rope tied at both ends so it
/// hangs taut in a straight line, while a body on its own physics layer sweeps through it with dynamic collision tracking
/// enabled. The rope has to be pushed out of its line instead of being passed through, which is what
/// <see cref="RopeCollisionType.DynamicOnly"/> and its tracked body velocity are for, and what no example scene covers on
/// its own.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class RopeDynamicCollisionTest
{
    [TestCase]
    public async Task MovingBodyPushesTheRopeOutOfItsLine()
    {
        await SceneHarness.RunAsync("res://demo/rope_dynamic_collision.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var rope = root.GetNode<VerletRopeSimulated>("DynamicCollisionRope");
        var body = root.GetNode<RigidBody3D>("MovingBody");

        checks.Check(rope.IsRopeCreated, "the rope is created on ready");
        checks.Check(rope.GetParticleCount() == 14, $"the rope keeps its 14 configured particles, got {rope.GetParticleCount()}");

        // The rope is laid out taut between its two attachments on creation, so it is already straight; the wait only lets
        // the first physics frames settle before the body is sent at it.
        await SceneHarness.WaitPhysicsFrames(root, 120);

        var settledDeviation = GetLargestHorizontalDeviation(rope);
        body.Sleeping = false;
        body.LinearVelocity = new Vector3(0.0f, 0.0f, 4.0f);
        var peakDeviation = settledDeviation;

        for (var frame = 0; frame < 180 && body.GlobalPosition.Z < 2.0f; frame++)
        {
            await SceneHarness.WaitPhysicsFrames(root, 1);
            peakDeviation = Mathf.Max(peakDeviation, GetLargestHorizontalDeviation(rope));
        }

        checks.Check(settledDeviation < 0.02f, $"the rope is laid taut in a straight line before the body arrives, deviation {settledDeviation:F2}");
        checks.Check(body.GlobalPosition.Z > 1.5f, $"the body travels past the rope, it ends at z {body.GlobalPosition.Z:F2}");
        // A hit particle is placed off the 0.5 m sphere's surface while the body passes, which moves it about 0.6 m.
        checks.Check(peakDeviation > 0.2f, $"the body pushes the rope out of its line instead of passing through it, peak deviation {peakDeviation:F2}");
        checks.Check(rope.GetParticleCount() == 14, $"the rope keeps its particles through the pass, got {rope.GetParticleCount()}");
        checks.CheckParticlesAreFinite(rope, "the rope particle positions stay finite");
    }

    /// <summary> Largest horizontal distance any particle has from the rope node, which is the line it hangs on. </summary>
    private static float GetLargestHorizontalDeviation(VerletRopeSimulated rope)
    {
        var largestDeviation = 0f;

        for (var i = 0; i < rope.GetParticleCount(); i++)
        {
            var offset = rope.GetParticlePosition(i) - rope.GlobalPosition;
            largestDeviation = Mathf.Max(largestDeviation, new Vector2(offset.X, offset.Z).Length());
        }

        return largestDeviation;
    }

    #endregion
}
