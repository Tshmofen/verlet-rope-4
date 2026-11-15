using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `demo/test/rope_slide_collision`, the purpose-built scene for the stretch ratio a slide-stretch collision is
/// decided by: a rope whose segments are already past <see cref="VerletRopeSimulated.SlideCollisionStretch"/> keeps the part
/// of its movement that runs along the surface it hit, while a rope that never reaches the ratio has that movement discarded
/// with the rest of the step. Both hang from a pin beside a wall under a gravity that presses them into it, and the sliding
/// one has to work its way down the wall while the stuck one is held where it touched.
/// <para>
/// Neither rope is anchored at its far end, so both start as a randomly jittered clump and unfold into a layout the case
/// cannot predict: measured over thirty runs of this scene with this wait, the stuck rope came to rest anywhere between the
/// floor at 1.00 m and 1.74 m. What does repeat is the difference between the two ropes of the same run, which never fell
/// below 0.222 m. The goal is therefore that difference and not the height either rope rests at, and the run lasts long
/// enough for the slowest layouts to finish descending - one of them still read 1.128 m at frame 240 and 0.796 m by frame
/// 600.
/// </para>
/// <para>
/// The difference is the stable goal but not the one a lost ratio trips: with the slide term ignored both ropes draw their
/// layout from the same 1.00 m to 1.74 m spread, so the comparison only fails when the two layouts happen to order that way,
/// which reads as a flake rather than as a regression. What catches it is the absolute goal at the floor, and that holds
/// only because the sliding rope's own lowest particle never stopped above 1.001 m over the same runs, against the 1.070 m
/// bound the rope's drawn width sets.
/// </para>
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class RopeSlideCollisionTest
{
    [TestCase]
    public async Task TheSlidingRopeKeepsDescendingWhileTheStuckOneIsHeldAtTheWall()
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
        // The rope is stretched by its own weight while it hangs against the wall, and how much of it hangs free is the
        // layout's to decide, so its drawn length is the configured one to within the ten percent measured over runs.
        var lengthTolerance = slideRope.RopeLength * 0.1f;

        checks.Check(slideRope.SlideCollisionStretch < stickRope.SlideCollisionStretch, "the two ropes are configured for different slide ratios");
        checks.Check(slideRope.IsRopeCreated && stickRope.IsRopeCreated, "both ropes are created on ready");

        await SceneHarness.WaitPhysicsFrames(root, 600);

        var slideLowest = GetLowestParticleY(slideRope);
        var stickLowest = GetLowestParticleY(stickRope);

        // The wall presses the rope in and the particles that slide keep their movement along it, so the sliding rope
        // descends it instead of being held where it touched. Measured over thirty runs of this scene with this wait: the
        // sliding rope ended 0.222 m to 1.524 m lower than the stuck rope of the same run, while the stuck rope on its own
        // came to rest anywhere between the floor at 1.00 m and 1.74 m - which is why the goal is the difference.
        checks.Check(stickLowest - slideLowest > slideRope.RopeWidth * 2.0f, $"the sliding rope works its way down the wall while the stuck one is held, sliding {slideLowest:F3} against stuck {stickLowest:F3}");
        // It got there by descending the whole way: over those runs its lowest particle never stopped above 1.001 m, so the
        // sliding rope reaches the floor instead of stopping on the wall the way its twin does.
        checks.Check(slideLowest < floorTop + slideRope.RopeWidth, $"the sliding rope reaches the floor rather than stopping on the wall, lowest particle {slideLowest:F3} for a floor top of {floorTop:F3}");
        // It did not gain that by being stretched: measured 2.48 m to 2.66 m over the same runs for a configured 2.5 m.
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
