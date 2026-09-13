using Godot;

namespace VerletRope4.Demo.Examples;

/// <summary> Walks the leashed body back and forth, so the leash goes taut and drags the attached body along. </summary>
public partial class TetherLeashDemo : RigidBody3D
{
    private float _elapsedTime;

    /// <summary> Maximum horizontal speed the body is driven with. </summary>
    [Export] public float WalkSpeed { get; set; } = 2.0f;

    /// <summary> Full back and forth cycles per second. </summary>
    [Export] public float WalkFrequency { get; set; } = 0.15f;

    public override void _PhysicsProcess(double delta)
    {
        _elapsedTime += (float)delta;

        // Starts towards -X, away from the attached body, so the very first leg stretches the leash instead of shoving it.
        var walkDirection = -Mathf.Sin(_elapsedTime * Mathf.Tau * WalkFrequency);
        LinearVelocity = new Vector3(walkDirection * WalkSpeed, LinearVelocity.Y, LinearVelocity.Z);
    }
}
