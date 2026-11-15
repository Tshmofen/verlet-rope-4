using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `demo/test/rope_slide_collision`, the purpose-built scene for the stretch ratio a slide-stretch collision is
/// decided by: a rope whose segments are already past <see cref="VerletRopeSimulated.SlideCollisionStretch"/> keeps the part
/// of its movement that runs along the surface it hit, while a rope that never reaches the ratio has that movement discarded
/// with the rest of the step. Both hang from a pin beside a wall under a gravity that presses them into it, and the sliding
/// one has to work its way down the wall and reach the floor rather than being held up by it.
/// <para>
/// What the case does not assert is where the stuck rope comes to rest: stopping a particle on a surface is a difference in
/// how fast it descends, not a hold, because the response places it on the ray hit point, whose own height still follows the
/// step. Measured over runs of this scene, the stuck rope stalled at 1.34 m to 1.63 m but reached the floor at 1.00 m to
/// 1.04 m in others, which is the layout its particles landed in and not the flag - so a goal on that height flips.
/// </para>
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class RopeSlideCollisionTest
{
    [TestCase]
    public async Task TheSlidingRopeKeepsDescendingTheWallAndReachesTheFloor()
    {
        await SceneHarness.RunAsync("res://demo/test/rope_slide_collision.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var slideRope = root.GetNode<VerletRopeSimulated>("SlideRope");
        var stickRope = root.GetNode<VerletRopeSimulated>("StickRope");
        var floor = root.GetNode<StaticBody3D>("Floor");
        var floorBox = (BoxShape3D)floor.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
        // The floor top is the top of the box the scene gives its floor, so moving or resizing that box moves the goal.
        var floorTop = floor.GlobalPosition.Y + (floorBox.Size.Y / 2.0f);
        // The rope is stretched by its own weight while it hangs against the wall, so its drawn length is the configured
        // one to within the few percent the constraint solve gives up under load.
        var lengthTolerance = slideRope.RopeLength * 0.05f;

        checks.Check(slideRope.SlideCollisionStretch < stickRope.SlideCollisionStretch, "the two ropes are configured for different slide ratios");
        checks.Check(slideRope.IsRopeCreated && stickRope.IsRopeCreated, "both ropes are created on ready");

        await SceneHarness.WaitPhysicsFrames(root, 180);

        var slideLowest = GetLowestParticleY(slideRope);

        // The wall presses the rope in and the particles that slide keep their movement along it, so the rope descends it
        // instead of being held where it touched it. Measured 0.446 m to 1.015 m over thirteen runs of this scene against a
        // floor top of 1.000 m, and 1.244 m to 1.607 m with the slide term forced off - a rope that stops on the wall keeps
        // more than a rope width of it between itself and the floor.
        checks.Check(slideLowest < floorTop + slideRope.RopeWidth, $"the sliding rope works its way down the wall to the floor, lowest particle {slideLowest:F3} for a floor top of {floorTop:F3}");
        // It did not gain that by being stretched: measured 2.52 m and 2.50 m for a configured 2.5 m.
        checks.CheckRange(slideRope.GetCurrentRopeLength(), slideRope.RopeLength - lengthTolerance, slideRope.RopeLength + lengthTolerance, "the sliding rope keeps its rest length");
        checks.CheckRange(stickRope.GetCurrentRopeLength(), stickRope.RopeLength - lengthTolerance, stickRope.RopeLength + lengthTolerance, "the stuck rope keeps its rest length");
        checks.CheckParticlesAreFinite(slideRope, "the sliding rope particle positions stay finite");
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
