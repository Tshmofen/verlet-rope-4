using Godot;

namespace VerletRope4.Demo.Examples;

/// <summary> Sways the rope anchor around its pivot, to show that the rope is attached to its parent node and follows it. </summary>
public partial class SwingingRopeDemo : Node3D
{
    private Vector3 _originRotation;
    private float _elapsedTime;

    /// <summary> Maximum rotation the anchor swings by, in degrees. </summary>
    [Export] public float SwayAngleDegrees { get; set; } = 8.0f;

    /// <summary> Full back and forth cycles per second. </summary>
    [Export] public float SwayFrequency { get; set; } = 0.25f;

    public override void _Ready()
    {
        _originRotation = Rotation;
    }

    public override void _PhysicsProcess(double delta)
    {
        _elapsedTime += (float)delta;

        var swayAngle = Mathf.Sin(_elapsedTime * Mathf.Tau * SwayFrequency) * Mathf.DegToRad(SwayAngleDegrees);
        Rotation = _originRotation + new Vector3(0.0f, 0.0f, swayAngle);
    }
}
