using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `demo/test/rope_collision_all`, the purpose-built scene that turns both collision paths on at once -
/// <see cref="RopeCollisionType.StaticOnly"/> and <see cref="RopeCollisionType.DynamicOnly"/> have a scene of their own, so
/// this one is the pair of them in one run.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class RopeCollisionAllTest
{
    [TestCase]
    public async Task RopeStaysOnTheFloorWhileAMovingBodyPushesIt()
    {
        await SceneHarness.RunAsync("res://demo/test/rope_collision_all.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var rope = root.GetNode<VerletRopeSimulated>("CollisionRope");
        var floor = root.GetNode<StaticBody3D>("Floor");
        var body = root.GetNode<RigidBody3D>("MovingBody");
        var floorBox = (BoxShape3D)floor.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
        // The floor top is the top of the box the scene gives its floor, so moving or resizing that box moves the goal.
        var floorTop = floor.GlobalPosition.Y + (floorBox.Size.Y / 2.0f);
        // Collision moves a particle out of the floor as a point, so it may sink half a drawn rope width into it.
        var restMargin = rope.RopeWidth / 2.0f;

        checks.Check(rope.RopeCollisionType == RopeCollisionType.All, $"the rope tracks both collision paths, configured {rope.RopeCollisionType}");
        checks.Check(rope.IsRopeCreated, "the rope is created on ready");
        checks.Check(rope.GetParticleCount() == 14, $"the rope keeps its 14 configured particles, got {rope.GetParticleCount()}");

        await SceneHarness.WaitPhysicsFrames(root, 240);

        var settledLowest = GetLowestParticleY(rope);
        var settledPositions = Snapshot(rope);

        // The static path settles first: measured 1.005 m for a floor top of 1.000 m, inside the 0.035 m half width the
        // goal allows.
        checks.Check(settledLowest > floorTop - restMargin, $"the rope rests on the floor instead of passing through it, lowest particle {settledLowest:F3} against a floor top of {floorTop:F3}");
        checks.Check(settledLowest < floorTop + restMargin, $"the rope really fell onto the floor, lowest particle {settledLowest:F3}");

        body.Sleeping = false;
        body.LinearVelocity = new Vector3(0.0f, 0.0f, 4.0f);

        for (var frame = 0; frame < 180 && body.GlobalPosition.Z < 2.0f; frame++)
        {
            await SceneHarness.WaitPhysicsFrames(root, 1);
        }

        var pushedDistance = GetLargestDisplacement(rope, settledPositions);
        var lowestAfterPass = GetLowestParticleY(rope);

        // The dynamic path is the body sweeping through the settled rope: measured 1.993 m of movement for the particle it
        // pushed the furthest, over the 6 m the body travels at the 4 m/s it was given.
        checks.Check(body.GlobalPosition.Z > 1.5f, $"the body travels past the rope, it ends at z {body.GlobalPosition.Z:F2}");
        checks.Check(pushedDistance > 0.5f, $"the body pushes the rope out of its way, the furthest particle moved {pushedDistance:F2}");
        checks.Check(lowestAfterPass > floorTop - restMargin, $"the rope is still above the floor after the body passed, lowest particle {lowestAfterPass:F3}");
        checks.Check(rope.GetParticleCount() == 14, $"the rope keeps its particles through the pass, got {rope.GetParticleCount()}");
        checks.CheckParticlesAreFinite(rope, "the rope particle positions stay finite");
    }

    /// <summary> The positions as they are now, so the movement a pass causes can be measured against them. </summary>
    private static Vector3[] Snapshot(VerletRopeSimulated rope)
    {
        var positions = new Vector3[rope.GetParticleCount()];

        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = rope.GetParticlePosition(i);
        }

        return positions;
    }

    private static float GetLargestDisplacement(VerletRopeSimulated rope, Vector3[] snapshot)
    {
        var largestDisplacement = 0.0f;

        for (var i = 0; i < snapshot.Length; i++)
        {
            largestDisplacement = Mathf.Max(largestDisplacement, snapshot[i].DistanceTo(rope.GetParticlePosition(i)));
        }

        return largestDisplacement;
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
