using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `demo/rope_collision`, a purpose-built scene instead of a shipped example: a rope hanging one metre above a
/// static floor with static collision enabled. The rope has to fall onto the floor and stay on top of it instead of passing
/// through, which is what the raycast collision path in <see cref="VerletRopeSimulated"/> is for and what no example scene
/// covers on its own.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class RopeCollisionTest
{
    [TestCase]
    public async Task RopeFallsOntoTheFloorAndStaysOnIt()
    {
        await SceneHarness.RunAsync("res://demo/rope_collision.tscn", RunSceneAsync);
    }

    #region Utility

    /// <summary> Fraction of its configured length the piled up rope is allowed to be shorter or longer by. </summary>
    private const float LengthTolerance = 0.05f;

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var rope = root.GetNode<VerletRopeSimulated>("CollisionRope");
        var floor = root.GetNode<StaticBody3D>("Floor");
        var floorBox = (BoxShape3D)floor.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
        // The floor top is the top of the box the scene gives its floor, so moving or resizing that box moves the goal.
        var floorTop = floor.GlobalPosition.Y + (floorBox.Size.Y / 2.0f);
        // Collision moves a particle out of the floor as a point, so it comes to rest on the surface itself and may sink
        // half a drawn rope width into it before the rope visibly cuts through the box. Measured on the piling rope:
        // 1.0037 m for a floor top of 1.0000 m, which is 0.0037 m above the surface.
        var restMargin = rope.RopeWidth / 2.0f;

        checks.Check(rope.IsRopeCreated, "the rope is created on ready");
        checks.Check(rope.GetParticleCount() == 12, $"the rope keeps its 12 configured particles, got {rope.GetParticleCount()}");

        await SceneHarness.WaitPhysicsFrames(root, 240);

        var lowestParticle = GetLowestParticleY(rope);
        // The rope is still solving towards its rest length while it lies on the floor, so its drawn length is the
        // configured one within the few percent the solver stretches under load: measured 2.0227 m against 2.0000 m.
        var lengthTolerance = rope.RopeLength * LengthTolerance;

        checks.Check(lowestParticle > floorTop - restMargin, $"the rope rests on the floor instead of passing through it, lowest particle {lowestParticle:F3} against a floor top of {floorTop:F3}");
        checks.Check(lowestParticle < floorTop + restMargin, $"the rope really fell onto the floor, lowest particle {lowestParticle:F3}");
        checks.CheckRange(rope.GetCurrentRopeLength(), rope.RopeLength - lengthTolerance, rope.RopeLength + lengthTolerance, "the rope keeps its length while it piles up on the floor");
        checks.CheckParticlesAreFinite(rope, "the rope particle positions stay finite");
    }

    private static float GetLowestParticleY(VerletRopeSimulated rope)
    {
        var lowest = float.MaxValue;

        for (var i = 0; i < rope.GetParticleCount(); i++)
        {
            lowest = Mathf.Min(lowest, rope.GetParticlePosition(i).Y);
        }

        return lowest;
    }

    #endregion
}
