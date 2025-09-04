using Godot;

namespace VerletRope.Demo;

public partial class MovingBodyDemo : RigidBody3D
{
    private float _periodTime;
    private Vector3 _currentDirection;
    private RandomNumberGenerator _randomNumberGenerator = new();

    [Export] public float MovementForce { get; set; } = 20f;
    [Export] public float DirectionChangePeriod { get; set; } = 0.5f;
    
    private Vector3 GenerateRandomDirection()
    {
        var x = _randomNumberGenerator.RandfRange(-1, 1);
        var z = _randomNumberGenerator.RandfRange(-1, 1);
        return new Vector3(x, 0, z).Normalized();
    }

    public override void _Ready()
    {
        _currentDirection = GenerateRandomDirection();
    }

    public override void _PhysicsProcess(double delta)
    {
        _periodTime += (float)delta;
        
        if (_periodTime >= DirectionChangePeriod)
        {
            _currentDirection = GenerateRandomDirection();
            _periodTime = 0f;
        }
        
        ApplyCentralForce(_currentDirection * MovementForce);
    }
}