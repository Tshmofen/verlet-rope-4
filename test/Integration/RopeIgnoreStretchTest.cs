using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `demo/test/rope_ignore_stretch`, the purpose-built scene for the stretch a segment is allowed to reach before a
/// collision gives up on it: past <see cref="VerletRopeSimulated.IgnoreCollisionStretch"/> times its rest length the segment
/// skips collision entirely, so a rope hanging more than it can hold falls through the floor instead of piling up on it. The
/// scene hangs two identical ropes one metre above the same static floor, one at the default ratio and one at the bottom of
/// the property's range, so the twin that keeps colliding is the difference the goal is measured against.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class RopeIgnoreStretchTest
{
    [TestCase]
    public async Task TheRopePastTheIgnoreRatioFallsThroughTheFloorItsTwinRestsOn()
    {
        await SceneHarness.RunAsync("res://demo/test/rope_ignore_stretch.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var collisionRope = root.GetNode<VerletRopeSimulated>("CollisionRope");
        var ignoreRope = root.GetNode<VerletRopeSimulated>("IgnoreRope");
        var floor = root.GetNode<StaticBody3D>("Floor");
        var floorBox = (BoxShape3D)floor.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
        // The floor top is the top of the box the scene gives its floor, so moving or resizing that box moves the goal.
        var floorTop = floor.GlobalPosition.Y + (floorBox.Size.Y / 2.0f);
        // Collision moves a particle out of the floor as a point, so it comes to rest on the surface itself and may sink
        // half a drawn rope width into it before the rope visibly cuts through the box.
        var restMargin = collisionRope.RopeWidth / 2.0f;

        checks.Check(ignoreRope.IgnoreCollisionStretch < collisionRope.IgnoreCollisionStretch, "the two ropes are configured for different ignore ratios");
        checks.Check(collisionRope.IsRopeCreated && ignoreRope.IsRopeCreated, "both ropes are created on ready");

        await SceneHarness.WaitPhysicsFrames(root, 240);

        var collisionLowest = GetLowestParticleY(collisionRope);
        var ignoreLowest = GetLowestParticleY(ignoreRope);

        // The ratio is a ratio of the rest segment length, so a rope hanging under its own weight is already past it at the
        // bottom of the range: measured 1.004 m and 1.005 m for the default five, which is the floor top to within the four
        // millimetres the solver leaves it above the surface.
        checks.Check(collisionLowest > floorTop - restMargin, $"the rope at the default ratio rests on the floor, lowest particle {collisionLowest:F3} against a floor top of {floorTop:F3}");
        checks.Check(collisionLowest < floorTop + restMargin, $"the rope at the default ratio really fell onto the floor, lowest particle {collisionLowest:F3}");
        // Both ropes have to be over the floor for the comparison to mean anything: a falling rope that missed the box would
        // reach the same height for a reason that has nothing to do with its ratio.
        checks.Check(Mathf.Abs(ignoreRope.GlobalPosition.X - floor.GlobalPosition.X) < floorBox.Size.X / 2.0f, "the falling rope hangs over the floor it passes through");
        // Measured -0.123 m to -0.075 m over six runs, against a floor top of 1.000 m and a rope that hangs from 2.000 m
        // with 2.000 m of length: it is through the floor and hanging below it rather than resting on it.
        checks.Check(ignoreLowest < floorTop - restMargin, $"the rope past the ignore ratio falls through the floor, lowest particle {ignoreLowest:F3} against a floor top of {floorTop:F3}");
        checks.CheckParticlesAreFinite(collisionRope, "the resting rope particle positions stay finite");
        checks.CheckParticlesAreFinite(ignoreRope, "the falling rope particle positions stay finite");
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
