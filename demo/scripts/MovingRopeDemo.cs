using Godot;

namespace VerletRope4.Demo;

public partial class MovingRopeDemo : Node3D
{
    private bool _isMoveForward;

    [Export] public RigidBody3D MovingBody { get; set; }
	[Export] public float PathTime { get; set; }
	[Export] public float PathMaxSpeed { get; set; }
	[Export] public Tween.TransitionType TransitionType { get; set; }

	private void MoveNode()
	{
        _isMoveForward = !_isMoveForward;
		var sign = _isMoveForward ? 1 : -1;
		var targetSpeed = Vector3.Right * PathMaxSpeed * sign;

		var tween = MovingBody.CreateTween().SetTrans(TransitionType).SetProcessMode(Tween.TweenProcessMode.Physics);
		tween.TweenProperty(MovingBody, RigidBody3D.PropertyName.LinearVelocity.ToString(), targetSpeed, PathTime / 5f);
		tween.TweenInterval(3 / 5f * PathTime);
        tween.TweenProperty(MovingBody, RigidBody3D.PropertyName.LinearVelocity.ToString(), Vector3.Zero, PathTime / 5f);
		tween.TweenCallback(Callable.From(MoveNode));
	}

	public override void _Ready()
	{
		MoveNode();
	}
}
