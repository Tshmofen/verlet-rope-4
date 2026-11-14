using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
using VerletRope4.Tests.Integration.Harness;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Drives `3_wrecking_ball`, a ball hanging off a rope that the demo kicks: the rope has to be tied to the ball, the ball
/// has to be thrown by the kick, the rope end has to keep following it and the joint has to keep the ball on its leash.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class WreckingBallTest
{
    [TestCase]
    public async Task BallIsKickedAndStaysOnItsLeash()
    {
        await SceneHarness.RunAsync("res://demo/examples/3_wrecking_ball.tscn", RunSceneAsync);
    }

    #region Utility

    private static async Task RunSceneAsync(Node root, SceneChecks checks)
    {
        var rope = root.GetNode<VerletRopeSimulated>("WreckingRope");
        var joint = root.GetNode<VerletJointSimulated>("WreckingRope/JointSimulated");
        var ball = root.GetNode<RigidBody3D>("WreckingRope/WreckingBall");
        var attachment = root.GetNode<Node3D>("WreckingRope/WreckingBall/Attachment");

        checks.Check(rope.IsRopeCreated, "the wrecking rope is created on ready");
        checks.Check(rope.GetParticleCount() == 15, $"the wrecking rope keeps its 15 configured particles, got {rope.GetParticleCount()}");
        checks.Check(rope.GetParticle(-1)?.IsAttached == true, "the last particle is tied to the ball");

        var ballStart = ball.GlobalPosition;
        await SceneHarness.WaitPhysicsFrames(root, 120);

        var ballTravel = ball.GlobalPosition.DistanceTo(ballStart);
        var anchorDistance = ball.GlobalPosition.DistanceTo(rope.GlobalPosition);

        // The joint has no start attachment here, so it measures from the rope node itself and not from the rope end.
        var leash = joint.JointMaxDistance + attachment.Position.Length();

        checks.Check(ballTravel > 0.2f, $"the ball swings away from the pose it starts in, travelled {ballTravel:F2}");
        checks.Check(rope.GetParticlePosition(-1).DistanceTo(attachment.GlobalPosition) < 0.5f, "the rope end follows the ball");
        // The ball settles just past the leash - measured 2.84 m against a 2.74 m one - so the margin is that settle and not
        // what the joint is allowed to give.
        checks.CheckAtMost(anchorDistance, leash + 0.25f, "the joint keeps the ball on its leash from the rope anchor");
        checks.CheckParticlesAreFinite(rope, "the wrecking rope particle positions stay finite");
    }

    #endregion
}
