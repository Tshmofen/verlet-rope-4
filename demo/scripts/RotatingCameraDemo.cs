using Godot;

namespace VerletRope.demo.scripts;

public partial class RotatingCameraDemo : Camera3D
{
    [Export] public bool IsRotating { get; set; } = true;
    [Export] public Vector3 Axis { get; set; } = Vector3.Up;
    [Export] public float Speed { get; set; } = -0.5f;

    public override void _Process(double delta)
    {
        if (!IsRotating)
        {
            return;
        }

        Rotate(Axis, Speed * (float) delta);
    }
}
