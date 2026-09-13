using Godot;

namespace VerletRope4.Demo.Examples;

/// <summary> Kicks the wrecking ball on scene start, so every run swings it into the obstacles from the same direction. </summary>
public partial class WreckingBallDemo : RigidBody3D
{
    private bool _isLaunched;

    /// <summary> Impulse applied to the ball on its first physics frame. </summary>
    [Export] public Vector3 LaunchImpulse { get; set; } = new(-200.0f, 0.0f, 0.0f);

    public override void _PhysicsProcess(double delta)
    {
        if (_isLaunched)
        {
            return;
        }

        _isLaunched = true;
        ApplyCentralImpulse(LaunchImpulse);
    }
}
