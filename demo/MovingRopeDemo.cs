using Godot;

namespace VerletRope4;

public partial class MovingRopeDemo : Node3D
{
	private bool _moveForward;
	private Vector3 _targetPosition;
    private bool _handleMovingManually;

[Export] public Node3D MovingNode { get; set; }
	[Export] public Vector3 MovingPath { get; set; }
	[Export] public float PathTime{ get; set; }
	[Export] public Tween.TransitionType TransitionType { get; set; }

	private void SynchronizeNodePosition(double _)
	{
		MovingNode.GlobalPosition = _targetPosition;
	}

	private void UpdateTargetRopePosition(Vector3 position)
	{
		_targetPosition = position;
	}

	private void MoveNode()
	{
		_moveForward = !_moveForward;
		var sign = _moveForward ? 1 : -1;
		var fromPosition = MovingNode.GlobalPosition;
		var toPosition = MovingNode.GlobalPosition + sign * MovingPath;

		var tween = MovingNode.CreateTween().SetTrans(TransitionType).SetProcessMode(Tween.TweenProcessMode.Physics);
		tween.TweenMethod(Callable.From<Vector3>(UpdateTargetRopePosition), fromPosition, toPosition, PathTime);
		tween.TweenCallback(Callable.From(MoveNode));
	}

	public override void _Ready()
	{
        if (MovingNode is VerletRopeSimulated rope)
        {
            rope.SimulationStep += SynchronizeNodePosition;
        }
		else
        {
            _handleMovingManually = true;
        }

		MoveNode();
	}

    public override void _Process(double delta)
    {
        if (_handleMovingManually)
        {
            SynchronizeNodePosition(delta);
        }
    }
}
