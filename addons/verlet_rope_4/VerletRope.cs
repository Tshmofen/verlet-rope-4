using Godot;
using System.Collections.Generic;
using System.Linq;
using VerletRope4.Structure;

namespace VerletRope4;

[Tool]
public partial class VerletRope : VerletRopeMesh
{
    #region Signals

    [Signal] public delegate void SimulationStepEventHandler(double delta);

    #endregion

    #region Variables

    #region Vars Private

    private const string ParticlesRangeHint = "3,300";
    private const string SimulationRangeHint = "0,1000";
    private const string MaxSegmentStretchRangeHint = "1,20";
    private const string MaxCollisionsRangeHint = "1,256";
    private const float CollisionCheckLength = 0.001f;
    private const float DynamicCollisionCheckLength = 0.08f;

    private double _time;
    private double _simulationDelta;
    private RopeParticleData _particleData;

    private RayCast3D _rayCast;
    private BoxShape3D _collisionCheckBox;
    private PhysicsDirectSpaceState3D _spaceState;
    private PhysicsShapeQueryParameters3D _collisionShapeParameters;

    private Node3D _attachEnd;
    private bool _attachStart = true;
    private int _simulationParticles = 10;

    #endregion

    #region Vars Basics

    [ExportGroup("Basics")]
    [Export]
    public bool AttachStart
    {
        set
        {
            _attachStart = value;

            if (_particleData != null)
            {
                _particleData[0].IsAttached = value;
            }
        }
        get => _attachStart;
    }

    [Export] public Node3D AttachEnd
    {
        set
        {
            _attachEnd = value;

            if (_particleData != null)
            {
                _particleData[^1].IsAttached = _attachEnd != null;
            }
        }
        get => _attachEnd;
    }

    [Export(PropertyHint.Range, ParticlesRangeHint)]
    public int SimulationParticles
    {
        set
        {
            _simulationParticles = value;

            if (_particleData == null)
            {
                return;
            }

            _particleData.Resize(_simulationParticles);
            CreateRope();
        }
        get => _simulationParticles;
    }

    #endregion

    #region Vars Simulation

    [ExportGroup("Simulation")]
    [Export(PropertyHint.Range, SimulationRangeHint)] public int SimulationRate { get; set; } = 0;
    [Export] public int Iterations { get; set; } = 2;
    [Export] public int PreprocessIterations { get; set; } = 5;
    [Export] public float PreprocessDelta { get; set; } = 0.016f; 
    [Export(PropertyHint.Range, "0.0, 1.5")] public float Stiffness { get; set; } = 0.9f;
    [Export] public bool StartSimulationFromStartPoint { get; set; } = true;
    [Export] public bool Simulate { get; set; } = true;
    [Export] public bool Draw { get; set; } = true;
    [Export] public bool StartDrawSimulationOnStart { get; set; } = true;

    #endregion

    #region Vars Gravity

    [ExportGroup("Gravity")]
    [Export] public bool ApplyGravity { get; set; } = true;
    [Export] public Vector3 Gravity { get; set; } = Vector3.Down * 9.8f;
    [Export] public float GravityScale { get; set; } = 1.0f;

    #endregion

    #region Vars Wind

    [ExportGroup("Wind")]
    [Export] public bool ApplyWind { get; set; } = false;
    [Export] public FastNoiseLite WindNoise { get; set; } = null;
    [Export] public Vector3 Wind { get; set; } = new(1.0f, 0.0f, 0.0f);
    [Export] public float WindScale { get; set; } = 20.0f;

    #endregion

    #region Vars Damping

    [ExportGroup("Damping")]
    [Export] public bool ApplyDamping { get; set; } = true;
    [Export] public float DampingFactor { get; set; } = 100.0f;

    #endregion

    #region Vars Collision

    [ExportGroup("Collision")]
    [Export] public RopeCollisionType RopeCollisionType { get; set; } = RopeCollisionType.StaticOnly;
    [Export] public RopeCollisionBehavior RopeCollisionBehavior { get; set; } = RopeCollisionBehavior.None;
    [Export(PropertyHint.Range, MaxSegmentStretchRangeHint)] public float MaxRopeStretch { get; set; } = 1.1f;
    [Export(PropertyHint.Range, MaxSegmentStretchRangeHint)] public float SlideIgnoreCollisionStretch { get; set; } = 1.5f;
    [Export(PropertyHint.Range, MaxCollisionsRangeHint)] public int MaxDynamicCollisions { get; set; } = 32;

    private uint _staticCollisionMask = 1;
    [Export(PropertyHint.Layers3DPhysics)] public uint StaticCollisionMask 
    {
        get => _staticCollisionMask;
        set { _staticCollisionMask = value; if (_rayCast != null) _rayCast.CollisionMask = value; if (_collisionShapeParameters != null) _collisionShapeParameters.CollisionMask = value; }
    }
    
    [Export(PropertyHint.Layers3DPhysics)] public uint DynamicCollisionMask { get; set; }

    private bool _hitFromInside = true;
    [Export] public bool HitFromInside
    {
        get => _hitFromInside;
        set { _hitFromInside = value; if (_rayCast != null) _rayCast.HitFromInside = value; }
    }

    private bool _hitBackFaces = true;
    [Export] public bool HitBackFaces
    {
        get => _hitBackFaces;
        set { _hitBackFaces = value; if (_rayCast != null) _rayCast.HitBackFaces = value; }
    }

    #endregion

    #region Vars Debug

    [ExportGroup("Debug")]
    [Export] public bool DrawDebugParticles { get; set; } = false;

    #endregion

    #endregion

    #region Internal Logic

    #region Util

    private float GetAverageSegmentLength()
    {
        return RopeLength / (SimulationParticles - 1);
    }

    private float GetCurrentRopeLength()
    {
        var length = 0f;

        for (var i = 0; i < SimulationParticles - 1; i++)
        {
            length += (_particleData[i + 1].PositionCurrent - _particleData[i].PositionCurrent).Length();
        }

        return length;
    }

    private bool CollideRayCast(Vector3 from, Vector3 direction, uint collisionMask, out Vector3 collision, out Vector3 normal)
    {
        _rayCast.CollisionMask = collisionMask;
        _rayCast.GlobalPosition = from;
        _rayCast.TargetPosition = direction;
        _rayCast.ForceRaycastUpdate();

        if (!_rayCast.IsColliding())
        {
            collision = normal = Vector3.Zero;
            return false;
        }

        collision = _rayCast.GetCollisionPoint();
        normal = _rayCast.GetCollisionNormal();
        return true;
    }

    #endregion

    #region Constraints

    private void StiffRope()
    {
        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            for (var i = 0; i < SimulationParticles - 1; i++)
            {
                var segment = _particleData[i + 1].PositionCurrent - _particleData[i].PositionCurrent;
                var stretch = segment.Length() - GetAverageSegmentLength();
                var direction = segment.Normalized();

                if (_particleData[i].IsAttached)
                {
                    _particleData[i + 1].PositionCurrent -= direction * stretch * Stiffness;
                }
                else if (_particleData[i + 1].IsAttached)
                {
                    _particleData[i].PositionCurrent += direction * stretch * Stiffness;
                }
                else
                {
                    _particleData[i].PositionCurrent += direction * stretch * 0.5f * Stiffness;
                    _particleData[i + 1].PositionCurrent -= direction * stretch * 0.5f * Stiffness;
                }
            }
        }
    }

    private (bool isStaticCollision, Vector3[] dynamicCollisions) GetRopeCollisions()
    {
        var visuals = GetAabb();

        if (visuals.Size == Vector3.Zero)
        {
            return (false, []);
        }

        _collisionCheckBox.Size = visuals.Size;
        _collisionShapeParameters.Transform = new Transform3D(_collisionShapeParameters.Transform.Basis, GlobalPosition + visuals.Position + (visuals.Size / 2));

        var isStaticCollision = false;
        if (RopeCollisionType is RopeCollisionType.All or RopeCollisionType.StaticOnly)
        {
            _collisionShapeParameters.CollisionMask = StaticCollisionMask;
            isStaticCollision = _spaceState.CollideShape(_collisionShapeParameters, 1).Any();
        }

        var dynamicCollisions = (Vector3[]) null;
        if (RopeCollisionType is RopeCollisionType.All or RopeCollisionType.DynamicOnly)
        {
            _collisionShapeParameters.CollisionMask = DynamicCollisionMask;
            dynamicCollisions = _spaceState
                .IntersectShape(_collisionShapeParameters, MaxDynamicCollisions)
                .Select(c => c["collider"].As<Node3D>().GlobalPosition)
                .ToArray();
        }

        return (isStaticCollision, dynamicCollisions ?? []);
    }

    private void CollideRope(ICollection<Vector3> dynamicCollisions)
    {
        var generalCollisionMask = RopeCollisionType switch
        {
            RopeCollisionType.All => StaticCollisionMask | DynamicCollisionMask,
            RopeCollisionType.DynamicOnly => DynamicCollisionMask,
            RopeCollisionType.StaticOnly => StaticCollisionMask,
            _ => StaticCollisionMask
        };

        var segmentSlideIgnoreLength = GetAverageSegmentLength() * SlideIgnoreCollisionStretch;
        var isRopeStretched = GetCurrentRopeLength() > RopeLength * MaxRopeStretch;


        for (var i = 1; i < SimulationParticles; i++)
        {
            ref var currentPoint = ref _particleData[i];

            if (isRopeStretched)
            {
                if (RopeCollisionBehavior == RopeCollisionBehavior.StickyStretch)
                {
                    // Just ignore collision for sticky stretch
                    continue;
                }

                ref var previousPoint = ref _particleData[i - 1];
                var currentSegmentLength = (previousPoint.PositionCurrent - currentPoint.PositionCurrent).Length();
                if (currentSegmentLength > segmentSlideIgnoreLength)
                {
                    // We still need to ignore collisions when it's too stretched
                    continue;
                }
            }

            foreach (var dynamicCollision in dynamicCollisions)
            {
                var toDynamic = (dynamicCollision - currentPoint.PositionCurrent).Normalized() * DynamicCollisionCheckLength;
                if (CollideRayCast(currentPoint.PositionCurrent, toDynamic, DynamicCollisionMask, out var collision, out var normal))
                {
                    currentPoint.PositionCurrent = collision + (normal * DynamicCollisionCheckLength);
                }
            }

            var particleMove = currentPoint.PositionCurrent - currentPoint.PositionPrevious;
            if (particleMove == Vector3.Zero)
            {
                continue;
            }

            var generalTo = particleMove + (particleMove.Normalized() * CollisionCheckLength);
            if (!CollideRayCast(currentPoint.PositionPrevious, generalTo, generalCollisionMask, out var generalCollision, out var generalNormal))
            {
                continue;
            }

            currentPoint.PositionCurrent = generalCollision + (generalNormal * CollisionCheckLength);
            if (isRopeStretched)
            {
                currentPoint.PositionCurrent += particleMove.Slide(generalNormal);
            }
        }
    }

    #endregion

    private void VerletProcess(float delta)
    {
        for (var i = 0; i < SimulationParticles; i++)
        {
            ref var p = ref _particleData[i];

            if (p.IsAttached)
            {
                continue;
            }

            var positionCurrentCopy = p.PositionCurrent;
            p.PositionCurrent = (2f * p.PositionCurrent) - p.PositionPrevious + (delta * delta * p.Acceleration);
            p.PositionPrevious = positionCurrentCopy;
        }
    }

    private void ApplyForces()
    {
        for (var i = 0; i < SimulationParticles; i++)
        {
            ref var p = ref _particleData[i];
            var totalAcceleration = Vector3.Zero;

            if (ApplyGravity)
            {
                totalAcceleration += Gravity * GravityScale;
            }

            if (ApplyWind && WindNoise != null)
            {
                var timedPosition = p.PositionCurrent + (Vector3.One * (float)_time);
                var windForce = WindNoise.GetNoise3D(timedPosition.X, timedPosition.Y, timedPosition.Z);
                totalAcceleration += WindScale * Wind * windForce;
            }

            if (ApplyDamping)
            {
                var velocity = _particleData[i].PositionCurrent - _particleData[i].PositionPrevious;
                var drag = -DampingFactor * velocity.Length() * velocity;
                totalAcceleration += drag;
            }

            p.Acceleration = totalAcceleration;
        }
    }

    private void ApplyConstraints()
    {
        StiffRope();

        var isLayersAvailable = (DynamicCollisionMask != 0 || StaticCollisionMask != 0) && (
            RopeCollisionType == RopeCollisionType.All
            || (RopeCollisionType == RopeCollisionType.StaticOnly && StaticCollisionMask != 0)
            || (RopeCollisionType == RopeCollisionType.DynamicOnly && DynamicCollisionMask != 0)
        );
        if (RopeCollisionBehavior == RopeCollisionBehavior.None || !isLayersAvailable)
        {
            return;
        }

        var (isStaticCollision, dynamicCollisions) = GetRopeCollisions();
        if (!isStaticCollision && dynamicCollisions.Length == 0)
        {
            return;
        }

        CollideRope(dynamicCollisions);
    }

    #endregion

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint() && StartDrawSimulationOnStart)
        {
            Draw = true;
            Simulate = true;
        }

        AddChild(_rayCast = new RayCast3D
        {
            CollisionMask = StaticCollisionMask,
            HitFromInside = _hitFromInside,
            HitBackFaces = _hitBackFaces,
            Enabled = false
        });

        _spaceState = GetWorld3D().DirectSpaceState;
        var visuals = GetAabb();

        _collisionCheckBox = new BoxShape3D
        {
            Size = visuals.Size
        };

        _collisionShapeParameters = new PhysicsShapeQueryParameters3D
        {
            ShapeRid = _collisionCheckBox.GetRid(),
            CollisionMask = StaticCollisionMask,
            Margin = 0.1f
        };

        _collisionShapeParameters.Transform = new Transform3D(_collisionShapeParameters.Transform.Basis, GlobalPosition + visuals.Position + (visuals.Size / 2));

        CreateRope();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() && _particleData == null)
        {
            CreateRope();
        }

        _time += delta;
        _simulationDelta += delta;

        if (SimulationRate != 0)
        {
            var simulationStep = 1f / SimulationRate;
            if (_simulationDelta < simulationStep)
            {
                return;
            }
        }

        if (_attachEnd != null)
        {
            ref var end = ref _particleData![SimulationParticles - 1];
            end.PositionCurrent = _attachEnd.GlobalPosition;
        }

        if (AttachStart)
        {
            ref var start = ref _particleData![0];
            start.PositionCurrent = GlobalPosition;
        }

        if (Simulate)
        {
            ApplyForces();
            VerletProcess((float)_simulationDelta);
            ApplyConstraints();
        }

        if (Draw)
        {
            DrawRopeParticles(_particleData);
        }

        if (DrawDebugParticles)
        {
            DrawRopeDebugParticles(_particleData);
        }

        EmitSignal(SignalName.SimulationStep, _simulationDelta);
        _simulationDelta = 0;
    }

    public void CreateRope()
    {
        var endLocation = GlobalPosition + (Vector3.Down * RopeLength);

        if (_attachEnd != null)
        {
            endLocation = _attachEnd.GlobalPosition;
        }
        else if (StartSimulationFromStartPoint)
        {
            endLocation = GlobalPosition;
        }

        var acceleration = Gravity * GravityScale;
        var segment = GetAverageSegmentLength();
        _particleData = RopeParticleData.GenerateParticleData(endLocation, GlobalPosition, acceleration, _simulationParticles, segment);

        ref var start = ref _particleData[0];
        ref var end = ref _particleData[SimulationParticles - 1];

        start.IsAttached = AttachStart;
        end.IsAttached = _attachEnd != null;
        end.PositionPrevious = endLocation;
        end.PositionCurrent = endLocation;

        for (var i = 0; i < PreprocessIterations; i++)
        {
            VerletProcess(PreprocessDelta);
            ApplyConstraints();
        }
    }

    public void DestroyRope()
    {
        _particleData.Resize(0);
        SimulationParticles = 0;
    }
}
