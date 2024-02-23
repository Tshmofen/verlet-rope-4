using Godot;

namespace VerletRope4;

public partial class MovingRopeDemo : Node3D
{
    private bool _moveForward;
    private Vector3 _targetPosition;

    [Export] public VerletRope MovingRope { get; set; }
    [Export] public Vector3 MovingPath { get; set; }
    [Export] public float PathTime{ get; set; }
    [Export] public Tween.TransitionType TransitionType { get; set; }

    private void SynchronizeRopePosition(double _)
    {
        MovingRope.GlobalPosition = _targetPosition;
    }

    private void UpdateTargetRopePosition(Vector3 position)
    {
        _targetPosition = position;
    }

    private void MoveRope()
    {
        _moveForward = !_moveForward;
        var sign = _moveForward ? 1 : -1;
        var fromPosition = MovingRope.GlobalPosition;
        var toPosition = MovingRope.GlobalPosition + sign * MovingPath;

        var tween = MovingRope.CreateTween().SetTrans(TransitionType).SetProcessMode(Tween.TweenProcessMode.Physics);
        tween.TweenMethod(Callable.From<Vector3>(UpdateTargetRopePosition), fromPosition, toPosition, PathTime);
        tween.TweenCallback(Callable.From(MoveRope));
    }

    public override void _Ready()
    {
        MovingRope.SimulationStep += SynchronizeRopePosition;
        MoveRope();
    }
}
