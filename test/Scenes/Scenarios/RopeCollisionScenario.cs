using Godot;
using System.Threading.Tasks;
using VerletRope4.Physics;

namespace VerletRope4.Tests.Scenes.Scenarios;

/// <summary>
/// A purpose-built scene instead of a shipped demo: a rope hanging one metre above a static floor with static collision
/// enabled. The rope has to fall onto the floor and stay on top of it instead of passing through, which is what the
/// raycast collision path in <see cref="VerletRopeSimulated"/> is for and what no demo scene covers on its own.
/// </summary>
public sealed class RopeCollisionScenario : SceneScenario
{
    public override string ScenePath => "res://test/Scenes/rope_collision.tscn";

    public override async Task RunAsync(Node root, SceneChecks checks)
    {
        var rope = root.GetNode<VerletRopeSimulated>("CollisionRope");
        var floorTop = root.GetNode<Node3D>("Floor").GlobalPosition.Y + 0.2f;

        checks.Check(rope.IsRopeCreated, "the rope is created on ready");
        checks.Check(rope.GetParticleCount() == 12, $"the rope keeps its 12 configured particles, got {rope.GetParticleCount()}");

        await WaitPhysicsFrames(root, 240);

        var lowestParticle = GetLowestParticleY(rope);

        checks.Check(lowestParticle > floorTop - 0.2f, $"the rope rests on the floor instead of passing through it, lowest particle {lowestParticle:F2} against a floor top of {floorTop:F2}");
        checks.Check(lowestParticle < floorTop + 0.5f, $"the rope really fell onto the floor, lowest particle {lowestParticle:F2}");
        checks.CheckRange(rope.GetCurrentRopeLength(), 0.5f, 4.5f, "the rope keeps its length while it piles up on the floor");
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
}
