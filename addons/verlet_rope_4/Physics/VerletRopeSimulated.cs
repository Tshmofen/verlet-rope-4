using Godot;
using System.Collections.Generic;
using System.Linq;
using VerletRope4.Data;
using VerletRope4.Physics.Joints;
using VerletRope4.Physics.Presets;
using VerletRope4.Utility;

namespace VerletRope4.Physics;

[Tool]
public partial class VerletRopeSimulated : BaseVerletRopePhysical
{
    public const string ScriptPath = "res://addons/verlet_rope_4/Physics/VerletRopeSimulated.cs";
    public const string IconPath = "res://addons/verlet_rope_4/icon.svg";
    [Signal] public delegate void SimulationStepEventHandler(double delta);

    private const float StaticCollisionCheckLength = 0.005f;
    private const float DynamicCollisionCheckLength = 0.1f;
    
    private bool _wasCreated;
    private double _simulationDelta;
    private RopeParticleData _particleData;
    private readonly List<Rid> _collisionExceptions = [];

    private RayCast3D _rayCast;
    private BoxShape3D _collisionShape;
    private PhysicsDirectSpaceState3D _spaceState;
    private PhysicsShapeQueryParameters3D _collisionShapeParameters;
    private readonly Dictionary<RigidBody3D, RopeDynamicCollisionData> _dynamicBodies = [];

    [ExportToolButton("Reset Rope (Apply Changes)")] public Callable ResetRopeButton => Callable.From(CreateRope);
    [ExportToolButton("Add Simulated Joint")] public Callable AddJointButton => Callable.From(CreateJointAction);
    
    /// <summary> Determines amount of separate particles used is simulations, total segments amount is <see cref="SimulationParticles"/> minus 1. </summary>
    [ExportGroup("Simulation")]
    [Export(PropertyHint.Range, "3,100")] public int SimulationParticles { get; set; } = 10;
    /// <summary> Determines target update rate for calculations (e.g. 30 updates per second) - but never exceeds physics tick rate. when value is set to 0 - the rope is updated every frame. </summary>
    [Export(PropertyHint.Range, "0,1000")] public int SimulationRate { get; set; } = 0;
    [Export(PropertyHint.Range, "0.2, 1.5")] public float Stiffness { get; set; } = 0.9f;
    [Export] public int StiffnessIterations { get; set; } = 2;
    /// <summary> How much frames (at 1/60 delta rate) are precalculated on rope creation to make it begin at more natural state. </summary>
    [Export] public int PreprocessIterations { get; set; } = 5;
    /// <summary> Determines if simulation is disabled when the rope is not on the screen. If <see cref="VerletRopeSimulatedJoint"/> is used to connect bodies, it might be better to disable this option to prevent de-syncs.</summary>
    [Export] public bool IsDisabledWhenInvisible { get; set; } = true;
    /// <summary>
    /// Determines how rope is being simulated.
    /// <para><see cref="RopeSimulationBehavior.None"/> - Rope is disabled;</para>
    /// <para><see cref="RopeSimulationBehavior.Game"/> - Only simulated in the game;</para>
    /// <para><see cref="RopeSimulationBehavior.Editor"/> - Rope is simulated in both game and editor;</para>
    /// <para><see cref="RopeSimulationBehavior.Selected"/> - Rope is simulated in game and only simulated in editor when selected.</para>
    /// </summary>
    [Export] public RopeSimulationBehavior SimulationBehavior { get; set; } = RopeSimulationBehavior.Selected;

    [ExportGroup("Gravity")]
    [Export] public bool ApplyGravity { get; set; } = true;
    [Export] public Vector3 Gravity { get; set; } = Vector3.Down * 9.8f;
    [Export] public float GravityScale { get; set; } = 1.0f;
    
    /// <summary> Determines if wind force simulation is enabled, for it to work <see cref="WindNoise"/> must also be assigned. </summary>
    [ExportGroup("Wind")]
    [Export] public bool ApplyWind { get; set; } = false;
    /// <summary> Determines base force and direction of the wind. </summary>
    [Export] public Vector3 WindDirection { get; set; } = new(40.0f, 0.0f, 0.0f);
    [Export] public FastNoiseLite WindNoise { get; set; } = null;
    [Export(PropertyHint.Range,"-1.00,1.00")] public float WindForceMax { get; set; } = 1.0f;
    [Export(PropertyHint.Range,"-1.00,1.00")] public float WindForceMin { get; set; } = 0.05f;

    [ExportGroup("Damping")]
    [Export] public bool ApplyDamping { get; set; } = true;
    [Export(PropertyHint.Range, "0, 10000")] public float DampingFactor { get; set; } = 1f;
    
    /// <summary>
    /// Determines how rope collisions are being tracked.
    /// <para><see cref="RopeCollisionType.StaticOnly"/> - Rope only collides with static objects specified in <see cref="StaticCollisionMask"/>, any <see cref="RigidBody3D"/> from this layer might not be handled correctly;</para>
    /// <para><see cref="RopeCollisionType.DynamicOnly"/> - Rope only collides with dynamic objects specified in <see cref="DynamicCollisionMask"/>, <see cref="RigidBody3D"/> in the rope area will be tracked
    /// and their velocity interpolated for correct dynamic collision handling, is more performance heavy compared to static tracking.</para>
    /// <para><see cref="RopeCollisionType.All"/> - Both variants of collision tracking is enabled, see their description above.</para>
    /// </summary>
    [ExportGroup("Collision")]
    [Export] public RopeCollisionType RopeCollisionType { get; set; } = RopeCollisionType.StaticOnly;    
    /// <summary>
    /// Determines how rope collisions behaves physically.
    /// <para><see cref="RopeCollisionBehavior.None"/> - Rope collisions are disabled, most performant option.</para>
    /// <para><see cref="RopeCollisionBehavior.SlideStretch"/> - When rope particle collides, they stretch up to <see cref="SlideCollisionStretch"/> value,
    /// then slide along the collision normal up to <see cref="IgnoreCollisionStretch"/> value, afterward the collision is considered unavoidable and is ignored. </para>
    /// </summary>
    [Export] public RopeCollisionBehavior RopeCollisionBehavior { get; set; } = RopeCollisionBehavior.None;
    [Export(PropertyHint.Range, "1,20")] public float SlideCollisionStretch { get; set; } = 1.05f;
    [Export(PropertyHint.Range, "1,20")] public float IgnoreCollisionStretch { get; set; } = 5f;
    [Export(PropertyHint.Range, "1,256")] public int MaxDynamicCollisions { get; set; } = 4;
    /// <summary> Determines the margin around the rope's <see cref="Aabb"/> to track incoming dynamic bodies. </summary>
    [Export(PropertyHint.Range, "0.1,100")] public float DynamicCollisionTrackingMargin { get; set; } = 1;
    [Export(PropertyHint.Layers3DPhysics)] public uint StaticCollisionMask { get; set; } = 1;
    [Export(PropertyHint.Layers3DPhysics)] public uint DynamicCollisionMask { get; set; } = 1;
    [Export] public bool RayCastHitFromInside { get; set; }
    [Export] public bool RayCastHitBackFaces { get; set; }

    [ExportGroup("Presets")]
    [ExportToolButton("Preset - Standard")] public Callable PresetStandardButton => Callable.From(
        () => CommitEditorAction("Verlet Rope Simulated - Set Standard Preset", (undoRedo, actionId) => VerletRopeSimulatedPreset.SetStandardValues(this, undoRedo, actionId))
    );

    #region Util

    private float GetAverageSegmentLength()
    {
        return RopeMesh.RopeLength / (_particleData?.Count ?? SimulationParticles - 1);
    }

    private float GetCurrentRopeLength()
    {
        var length = 0f;

        for (var i = 0; i < _particleData.Count - 1; i++)
        {
            length += (_particleData[i + 1].PositionCurrent - _particleData[i].PositionCurrent).Length();
        }

        return length;
    }

    private bool CollideRayCast(Vector3 from, Vector3 direction, uint collisionMask, out Vector3 collision, out Vector3 normal)
    {
        if (_rayCast == null || !_rayCast.IsInsideTree())
        {
            // Return for pre-ready calls from outer scripts on rope pre-initialization and tree exit
            collision = normal = Vector3.Zero;
            return false;
        }

        _rayCast.CollisionMask = collisionMask;
        _rayCast.GlobalPosition = from;
        _rayCast.TargetPosition = direction;
        _rayCast.HitBackFaces = RayCastHitBackFaces;
        _rayCast.HitFromInside = RayCastHitFromInside;

        _rayCast.ClearExceptions();
        foreach (var rid in _collisionExceptions)
        {
            _rayCast.AddExceptionRid(rid);
        }
            
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
    
    #region Internal Logic

    #region Constraints

    private void StiffRope()
    {
        for (var iteration = 0; iteration < StiffnessIterations; iteration++)
        {
            for (var i = 0; i < _particleData.Count - 1; i++)
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

    private void TrackDynamicCollisions(float delta)
    {
        if (_collisionShape == null || !RopeMesh.IsInsideTree())
        {
            // Ignore collisions pre-initialization or on remove
            return;
        }

        if (RopeCollisionType is not RopeCollisionType.All and not RopeCollisionType.DynamicOnly)
        {
            _dynamicBodies.Clear();
            return;
        }
        
        var visuals = RopeMesh.GetAabb();
        if (visuals.Size == Vector3.Zero)
        {
            _dynamicBodies.Clear();
            return;
        }

        _collisionShape.Size = visuals.Size + Vector3.One * DynamicCollisionTrackingMargin;
        _collisionShapeParameters.Transform = new Transform3D(_collisionShapeParameters.Transform.Basis, RopeMesh.GlobalPosition + visuals.GetCenter());
        _collisionShapeParameters.CollisionMask = DynamicCollisionMask;

        var trackingStamp = Time.GetTicksMsec();
        foreach (var result in _spaceState.IntersectShape(_collisionShapeParameters, MaxDynamicCollisions))
        {
            if (result["collider"].As<Node3D>() is not RigidBody3D body)
            {
                continue;
            }

            if (!_dynamicBodies.TryGetValue(body, out var data))
            {
                _dynamicBodies.Add(body, data = new RopeDynamicCollisionData
                {
                    PreviousPosition = body.GlobalPosition - body.LinearVelocity * delta,
                    Body = body
                });
            }

            data.Movement = body.GlobalPosition - data.PreviousPosition;
            data.PreviousPosition = body.GlobalPosition;
            data.TrackingStamp = trackingStamp;
        }

        foreach (var removeData in _dynamicBodies.Values.Where(data => data.TrackingStamp != trackingStamp).ToList())
        {
            _dynamicBodies.Remove(removeData.Body);
        }
    }

    private static Vector3 GetCollisionUpdatedPosition(Vector3 fromPosition, Vector3 move, Vector3 collisionPosition, Vector3 collisionNormal, float checkLength, bool isSliding)
    {
        var collisionDirection = (collisionPosition - fromPosition).Normalized();
        var newPosition = collisionPosition - collisionDirection * checkLength;
        return isSliding
            ? newPosition + move.Slide(collisionNormal)
            : newPosition;
    }

    private bool TryCollideMovementStatic(Vector3 previous, Vector3 move, bool isSliding, out Vector3 newPosition)
    {
        newPosition = previous;

        if (move == Vector3.Zero)
        {
            return false;
        }

        var checkDirection = move + (move.Normalized() * StaticCollisionCheckLength);
        if (!CollideRayCast(previous, checkDirection, StaticCollisionMask, out var collision, out var normal))
        {
            return false;
        }

        newPosition = GetCollisionUpdatedPosition(previous, move, collision, normal, StaticCollisionCheckLength, isSliding);
        return true;
    }

    private bool TryCollideMovementDynamic(Vector3 previous, Vector3 move, RopeDynamicCollisionData bodyData, bool isSliding, out Vector3 newPosition)
    {
        Vector3 adjustedPrevious;
        Vector3 checkDirection;
        float checkLength;

        if (bodyData.Movement != Vector3.Zero)
        {
            // Adjusting ray to be sent relative to interpolated body movement
            checkLength = DynamicCollisionCheckLength;
            adjustedPrevious = previous + bodyData.Movement;
            checkDirection = -bodyData.Movement.Normalized() * checkLength;
        }
        else if (move != Vector3.Zero)
        {
            adjustedPrevious = previous;
            checkLength = DynamicCollisionCheckLength;
            checkDirection = move + (move.Normalized() * checkLength);
        }
        else
        {
            newPosition = previous;
            return false;
        }

        if (!CollideRayCast(adjustedPrevious, checkDirection, DynamicCollisionMask, out var collision, out var normal))
        {
            newPosition = previous;
            return false;
        }
        
        newPosition = GetCollisionUpdatedPosition(adjustedPrevious, move, collision, normal, checkLength, isSliding);
        return true;
    }

    private void CollideRope()
    {
        var segmentLength = GetAverageSegmentLength();
        var segmentCollisionSlideLength = segmentLength * SlideCollisionStretch;
        var segmentCollisionIgnoreLength = segmentLength * IgnoreCollisionStretch;

        for (var i = 0; i < _particleData.Count; i++)
        {
            ref var currentPoint = ref _particleData[i];
            if (currentPoint.IsAttached)
            {
                continue;
            }

            var currentSegmentLength = 0f;
            if (i > 0)
            {
                ref var previousPoint = ref _particleData[i - 1];
                currentSegmentLength = (previousPoint.PositionCurrent - currentPoint.PositionCurrent).Length();
            }

            if (currentSegmentLength > segmentCollisionIgnoreLength)
            {
                // We still need to ignore collision targets when it's too stretched
                continue;
            }
            
            var particleMove = currentPoint.PositionCurrent - currentPoint.PositionPrevious;
            var isSliding = currentSegmentLength > segmentCollisionSlideLength;

            if (RopeCollisionType is RopeCollisionType.All or RopeCollisionType.StaticOnly)
            {
                if (TryCollideMovementStatic(currentPoint.PositionPrevious, particleMove, isSliding, out var updatedPosition))
                {
                    currentPoint.PositionCurrent = updatedPosition;
                    continue;
                }
            }

            if (RopeCollisionType is RopeCollisionType.All or RopeCollisionType.DynamicOnly)
            {
                foreach (var bodyData in _dynamicBodies.Values)
                {
                    if (TryCollideMovementDynamic(currentPoint.PositionPrevious, particleMove, bodyData, isSliding, out var updatedPosition))
                    {
                        currentPoint.PositionCurrent = updatedPosition;
                        break;
                    }
                }
            }
        }
    }

    #endregion

    private void VerletProcess(float delta)
    {
        for (var i = 0; i < _particleData.Count; i++)
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
        var currentTimeChange = Vector3.One * Time.GetTicksMsec() / 1000f;
        for (var i = 0; i < _particleData.Count; i++)
        {
            ref var particle = ref _particleData[i];
            var totalAcceleration = Vector3.Zero;

            if (ApplyGravity)
            {
                totalAcceleration += Gravity * GravityScale;
            }

            if (ApplyWind)
            {
                var timedPosition = particle.PositionCurrent + currentTimeChange;
                var windForce = WindNoise?.GetNoise3D(timedPosition.X, timedPosition.Y, timedPosition.Z) ?? WindForceMax;
                totalAcceleration += WindDirection * Mathf.Clamp(windForce, WindForceMin, WindForceMax);
            }

            if (ApplyDamping)
            {
                var velocity = _particleData[i].PositionCurrent - _particleData[i].PositionPrevious;
                totalAcceleration -= DampingFactor * velocity;
            }

            particle.Acceleration = totalAcceleration;
        }
    }

    private void ApplyConstraints(float delta)
    {
        StiffRope();

        if (RopeCollisionBehavior == RopeCollisionBehavior.None)
        {
            return;
        }

        if (DynamicCollisionMask == 0 && StaticCollisionMask == 0)
        {
            return;
        }

        if (RopeCollisionType == RopeCollisionType.StaticOnly && StaticCollisionMask == 0)
        {
            return;
        }

        if (RopeCollisionType == RopeCollisionType.DynamicOnly && DynamicCollisionMask == 0)
        {
            return;
        }

        TrackDynamicCollisions(delta);
        CollideRope();
    }

    private bool IsRopeSimulated()
    {
        if (_wasCreated)
        {
            _wasCreated = false;
            return true;
        }

        return SimulationBehavior switch
        {
            RopeSimulationBehavior.None => false,
            RopeSimulationBehavior.Game => !Engine.IsEditorHint(),
            RopeSimulationBehavior.Editor => true,
            RopeSimulationBehavior.Selected => !Engine.IsEditorHint() || this.IsEditorSelected(),
            _ => false
        };
    }

    #endregion

    #region Editor

    private void CreateJointAction()
    {
        CommitEditorAction("Verlet Rope - Create Simulated Joint", (undoRedo, actionId) =>
        {
            undoRedo.AddDoMethod(this, MethodName.CreateJoint, actionId, true);
            undoRedo.AddUndoMethod(this, MethodName.CreateJoint, actionId, false);
        });
    }

    #endregion

    public override void _Ready()
    {
        base._Ready();

        _rayCast = RopeMesh.FindOrCreateChild<RayCast3D>();
        _rayCast.Enabled = false;

        _spaceState = GetWorld3D().DirectSpaceState;
        _collisionShape = new BoxShape3D();
        _collisionShapeParameters = new PhysicsShapeQueryParameters3D
        {
            ShapeRid = _collisionShape.GetRid(),
            Margin = 0.1f
        };

        CreateRope();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsDisabledWhenInvisible && !RopeMesh.IsRopeVisible)
        {
            return;
        }

        if (_particleData == null)
        {
            CreateRope();
        }

        if (!IsRopeSimulated())
        {
            return;
        }

        _simulationDelta += delta;

        if (SimulationRate != 0)
        {
            var simulationStep = 1f / SimulationRate;
            if (_simulationDelta < simulationStep)
            {
                return;
            }
        }
        
        ref var start = ref _particleData![0];
        start.PositionCurrent = StartNode?.GlobalPosition ?? GlobalPosition;
        
        ref var end = ref _particleData![_particleData.Count - 1];
        if (end.IsAttached && EndNode != null)
        {
            end.PositionCurrent = EndNode.GlobalPosition;
        }

        var simulationDeltaF = (float)_simulationDelta;
        ApplyForces();
        VerletProcess(simulationDeltaF);
        ApplyConstraints(simulationDeltaF);
        RopeMesh.DrawRopeParticles(_particleData);

        EmitSignalSimulationStep(_simulationDelta);
        _simulationDelta = 0;

        UpdateEditorCollision(_particleData);
        UpdateGizmos();
    }

    public override void CreateJoint(int actionId = 0, bool toCreate = true)
    {
        var metaName = GetActionMeta("create_simulated_joint");

        if (!toCreate)
        {
            this.RemoveChildByMeta(metaName, actionId);
            return;
        }

        var joint = this.CreateChild<VerletRopeSimulatedJoint>("JointSimulated");
        joint.SetMeta(metaName, actionId);
    }

    public override void CreateRope()
    {
        base.CreateRope();

        var acceleration = Gravity * GravityScale;
        var segmentLength = GetAverageSegmentLength();
        var startLocation = StartNode?.GlobalPosition ?? GlobalPosition;
        var endLocation = EndNode?.GlobalPosition ?? startLocation;
        _particleData = RopeParticleData.GenerateParticleData(startLocation, endLocation, acceleration, SimulationParticles, segmentLength);

        ref var start = ref _particleData[0];
        ref var end = ref _particleData[_particleData.Count - 1];

        start.IsAttached = true;
        end.IsAttached = EndNode != null;
        end.PositionPrevious = endLocation;
        end.PositionCurrent = endLocation;

        for (var i = 0; i < PreprocessIterations; i++)
        {
            VerletProcess(1/60f);
            ApplyConstraints(1/60f);
        }

        _wasCreated = true;
    }

    public override void DestroyRope()
    {
        _particleData = null;
        SimulationParticles = 0;
    }
    
    /// <summary> Clears physics <see cref="Rid"/> that are currently ignored for collisions. </summary>
    public void ClearExceptions()
    {
        _collisionExceptions.Clear();
    }

    /// <summary> Registers physics <see cref="Rid"/> for collision ignore. Use to exclude joined bodies from collision simulation. </summary>
    public void RegisterExceptionRid(Rid rid, bool toInclude)
    {
        if (toInclude)
        {
            _collisionExceptions.Add(rid);
        }
        else
        {
            _collisionExceptions.Remove(rid);
        }
    }
}
