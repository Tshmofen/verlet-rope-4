using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using VerletRope.addons.verlet_rope_4.Structure;
using VerletRope4.Structure;

namespace VerletRope4;

/*
MIT License

Rewritten to be used with Godot 4.0+.
No additional license applied, feel free to use without my notice, though do not forget to still apply original license.
(c) Timofey Ivanov / tshmofen

Copyright (c) 2023 Zae Chao(zaevi)
Copyright (c) 2021 Shashank C

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

[Tool]
public partial class VerletRope : MeshInstance3D
{
    #region Signals

    [Signal] public delegate void SimulationStepEventHandler(double delta);

    #endregion

    #region Variables

    #region Vars Private

    private const string DefaultMaterialPath = "res://addons/verlet_rope_4/materials/rope_default.material";
    private const string NoNotifierWarning = $"Consider checking '{nameof(UseVisibleOnScreenNotifier)}' to disable rope visuals when it's not on screen for increased performance.";
    private const string CreationStampMeta = "creation_stamp";
    private const string ParticlesRangeHint = "3,300";
    private const string SimulationRangeHint = "30,265";
    private const string MaxSegmentStretchRangeHint = "1,20";
    private const string MaxCollisionsRangeHint = "1,256";
    private const float CollisionCheckLength = 0.001f;
    private const float DynamicCollisionCheckLength = 0.08f;

    private static readonly float Cos5Deg = Mathf.Cos(Mathf.DegToRad(5.0f));
    private static readonly float Cos15Deg = Mathf.Cos(Mathf.DegToRad(15.0f));
    private static readonly float Cos30Deg = Mathf.Cos(Mathf.DegToRad(30.0f));

    private double _time;
    private Camera3D _camera;
    private double _simulationDelta;
    private RopeParticleData _particleData;
    private VisibleOnScreenNotifier3D _visibleNotifier;

    private RayCast3D _rayCast;
    private ImmediateMesh _mesh;
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

    [Export] public float RopeLength { get; set; } = 5.0f;
    [Export] public float RopeWidth { get; set; } = 0.07f;

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

    private bool _useVisibleOnScreenNotifier = true;
    [Export] public bool UseVisibleOnScreenNotifier
    {
        get => _useVisibleOnScreenNotifier; 
        set { _useVisibleOnScreenNotifier = value; UpdateConfigurationWarnings(); }
    }

    #endregion

    #region Vars Simulation

    [ExportGroup("Simulation")]
    [Export(PropertyHint.Range, SimulationRangeHint)] public int SimulationRate { get; set; } = 60;
    [Export] public int Iterations { get; set; } = 2;
    [Export] public int PreprocessIterations { get; set; } = 5;
    [Export] public float PreprocessDelta { get; set; } = 0.016f; 
    [Export(PropertyHint.Range, "0.0, 1.5")] public float Stiffness { get; set; } = 0.9f;
    [Export] public bool StartSimulationFromStartPoint { get; set; } = true;
    [Export] public bool Simulate { get; set; } = true;
    [Export] public bool Draw { get; set; } = true;
    [Export] public bool StartDrawSimulationOnStart { get; set; } = true;
    [Export] public float SubdivisionLodDistance { get; set; } = 15.0f;

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

    private (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) GetSimulationParticles(int index)
    {
        var p0 = (index == 0)
            ? _particleData[index].PositionCurrent - (_particleData[index].Tangent * GetAverageSegmentLength())
            : _particleData[index - 1].PositionCurrent;

        var p1 = _particleData[index].PositionCurrent;

        var p2 = _particleData[index + 1].PositionCurrent;

        var p3 = index == SimulationParticles - 2
            ? _particleData[index + 1].PositionCurrent + (_particleData[index + 1].Tangent * GetAverageSegmentLength())
            : _particleData[index + 2].PositionCurrent;

        return (p0, p1, p2, p3);
    }

    private float GetAverageSegmentLength()
    {
        return RopeLength / (SimulationParticles - 1);
    }

    private void ResetRopeRotation()
    {
        // NOTE: rope doesn't draw from origin to attach_end_to correctly if rotated
        // calling to_local() in the drawing code is too slow
        GlobalTransform = new Transform3D(Basis.Identity, GlobalPosition);
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

    #region Draw

    private static void CatmullInterpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float tension, float t, out Vector3 point, out Vector3 tangent)
    {
        // this is fast catmull spline

        var tSqr = t * t;
        var tCube = tSqr * t;

        var m1 = (1f - tension) / 2f * (p2 - p0);
        var m2 = (1f - tension) / 2f * (p3 - p1);

        var a = (2f * (p1 - p2)) + m1 + m2;
        var b = (-3f * (p1 - p2)) - (2f * m1) - m2;

        point = (a * tCube) + (b * tSqr) + (m1 * t) + p1;
        tangent = ((3f * a * tSqr) + (2f * b * t) + m1).Normalized();
    }

    private void DrawQuad(IReadOnlyList<Vector3> vertices, Vector3 normal, float uvx0, float uvx1)
    {
        // NOTE: still may need tangents setup for normal mapping, not tested
        // SetTangent(new Plane(-t, 0.0f));
        _mesh.SurfaceSetNormal(normal);
        _mesh.SurfaceSetUV(new Vector2(uvx0, 0.0f));
        _mesh.SurfaceAddVertex(vertices[0]);
        _mesh.SurfaceSetUV(new Vector2(uvx1, 0.0f));
        _mesh.SurfaceAddVertex(vertices[1]);
        _mesh.SurfaceSetUV(new Vector2(uvx1, 1.0f));
        _mesh.SurfaceAddVertex(vertices[2]);
        _mesh.SurfaceSetUV(new Vector2(uvx0, 0.0f));
        _mesh.SurfaceAddVertex(vertices[0]);
        _mesh.SurfaceSetUV(new Vector2(uvx1, 1.0f));
        _mesh.SurfaceAddVertex(vertices[2]);
        _mesh.SurfaceSetUV(new Vector2(uvx0, 1.0f));
        _mesh.SurfaceAddVertex(vertices[3]);
    }

    private float GetDrawSubdivisionStep(Vector3 cameraPosition, int particleIndex)
    {
        var camDistParticle = cameraPosition - _particleData[particleIndex].PositionCurrent;

        if (camDistParticle.LengthSquared() > SubdivisionLodDistance * SubdivisionLodDistance)
        {
            return 1.0f;
        }

        var tangentDots = _particleData[particleIndex].Tangent.Dot(_particleData[particleIndex + 1].Tangent);

        return
            tangentDots >= Cos5Deg ? 1.0f :
            tangentDots >= Cos15Deg ? 0.5f :
            tangentDots >= Cos30Deg ? 0.33333f :
            0.25f;
    }

    private void CalculateRopeCameraOrientation()
    {
        var cameraPosition = _camera?.GlobalPosition ?? Vector3.Zero;

        ref var start = ref _particleData[0];
        start.Tangent = (_particleData[1].PositionCurrent - start.PositionCurrent).Normalized();
        start.Normal = (start.PositionCurrent - cameraPosition).Normalized();
        start.Binormal = start.Normal.Cross(start.Tangent).Normalized();

        ref var end = ref _particleData[SimulationParticles - 1];
        end.Tangent = (end.PositionCurrent - _particleData[SimulationParticles - 2].PositionCurrent).Normalized();
        end.Normal = (end.PositionCurrent - cameraPosition).Normalized();
        end.Binormal = end.Normal.Cross(end.Tangent).Normalized();

        for (var i = 1; i < SimulationParticles - 1; i++)
        {
            ref var particle = ref _particleData[i];
            particle.Tangent = (_particleData[i + 1].PositionCurrent - _particleData[i - 1].PositionCurrent).Normalized();
            particle.Normal = (_particleData[i].PositionCurrent - cameraPosition).Normalized();
            particle.Binormal = _particleData[i].Normal.Cross(_particleData[i].Tangent).Normalized();
        }
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
            return (false, Array.Empty<Vector3>());
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

        return (isStaticCollision, dynamicCollisions ?? Array.Empty<Vector3>());
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

    private void DrawCurve()
    {
        // Catmull curve
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        var cameraPosition = _camera?.GlobalPosition ?? Vector3.Zero;

        for (var i = 0; i < SimulationParticles - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(i);
            var step = GetDrawSubdivisionStep(cameraPosition, i);
            var t = 0.0f;

            while (t <= 1.0f)
            {
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, t, out var currentPosition, out var currentTangent);
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, Mathf.Min(t + step, 1.0f), out var nextPosition, out var nextTangent);

                var currentNormal = (currentPosition - cameraPosition).Normalized();
                var currentBinormal = currentNormal.Cross(currentTangent).Normalized();
                currentPosition -= GlobalPosition;

                var nextNormal = (nextPosition - cameraPosition).Normalized();
                var nextBinormal = nextNormal.Cross(nextTangent).Normalized();
                nextPosition -= GlobalPosition;

                var vs = new[]
                {
                    currentPosition - (currentBinormal * RopeWidth),
                    nextPosition - (nextBinormal * RopeWidth),
                    nextPosition + (nextBinormal * RopeWidth),
                    currentPosition + (currentBinormal * RopeWidth)
                };

                DrawQuad(vs, -currentBinormal, t, t + step);
                t += step;
            }
        }

        _mesh.SurfaceEnd();
    }

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

    public override string[] _GetConfigurationWarnings()
    {
        return UseVisibleOnScreenNotifier ? Array.Empty<string>() : new[] { NoNotifierWarning };
    }

    public override void _Ready()
    {
        if (!Engine.IsEditorHint() && StartDrawSimulationOnStart)
        {
            Draw = true;
            Simulate = true;
        }

        _mesh = Mesh as ImmediateMesh;
        if (_mesh == null || _mesh.GetMeta(CreationStampMeta, 0ul).AsUInt64() != GetInstanceId())
        {
            Mesh = _mesh = new ImmediateMesh();
            _mesh.SetMeta(CreationStampMeta, GetInstanceId());
            _mesh.ResourceLocalToScene = true;
        }

        AddChild(_rayCast = new RayCast3D
        {
            CollisionMask = StaticCollisionMask,
            HitFromInside = _hitFromInside,
            HitBackFaces = _hitBackFaces,
            Enabled = false
        });

        if (UseVisibleOnScreenNotifier)
        {
            AddChild(_visibleNotifier = new VisibleOnScreenNotifier3D());
            _visibleNotifier.ScreenEntered += () => Draw = true;
            _visibleNotifier.ScreenExited += () => Draw = false;
        }

        _camera = GetViewport().GetCamera3D();
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
        MaterialOverride ??= GD.Load<StandardMaterial3D>(DefaultMaterialPath);

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

        var simulationStep = 1 / (float) SimulationRate;
        if (_simulationDelta < simulationStep)
        {
            return;
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
            _camera = GetViewport().GetCamera3D();
            CalculateRopeCameraOrientation();
            _mesh.ClearSurfaces();
            ResetRopeRotation();
            DrawRopeDebugParticles();
            DrawCurve();

            if (_visibleNotifier != null)
            {
                _visibleNotifier.Aabb = GetAabb();
            }
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

        CalculateRopeCameraOrientation();
    }

    public void DestroyRope()
    {
        _particleData.Resize(0);
        SimulationParticles = 0;
    }

    public void DrawRopeDebugParticles()
    {
        const float debugParticleLength = 0.3f;

        if (!DrawDebugParticles)
        {
            return;
        }

        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        for (var i = 0; i < SimulationParticles; i++)
        {
            var particle = _particleData[i];
            var localPosition = particle.PositionCurrent - GlobalPosition;

            _mesh.SurfaceAddVertex(localPosition);
            _mesh.SurfaceAddVertex(localPosition + (debugParticleLength * particle.Tangent));

            _mesh.SurfaceAddVertex(localPosition);
            _mesh.SurfaceAddVertex(localPosition + (debugParticleLength * particle.Normal));

            _mesh.SurfaceAddVertex(localPosition);
            _mesh.SurfaceAddVertex(localPosition + (debugParticleLength * particle.Binormal));
        }

        _mesh.SurfaceEnd();
    }
}
