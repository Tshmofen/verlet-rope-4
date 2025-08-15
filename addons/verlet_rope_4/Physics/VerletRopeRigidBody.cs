 using Godot;
using System.Collections.Generic;
using VerletRope4.Data;

namespace VerletRope.Physics;

[Tool]
public partial class VerletRopeRigidBody : VerletRopePhysical
{
    public const string ScriptPath = "res://addons/verlet_rope_4/Physics/VerletRopeRigidBody.cs";
    public const string IconPath = "res://addons/verlet_rope_4/icon.svg";
    
    private RopeParticleData _particleData;
    private readonly List<RigidBody3D> _segmentBodies = [];

    [ExportToolButton("Reset Rope")] public Callable ResetRopeButton => Callable.From(CreateRope);

    [ExportGroup("Simulation")]
    [Export(PropertyHint.Range, "1,100")] public int SimulationSegments { get; set; } = 10;
    [Export] public bool IsDisabledWhenInvisible { get; set; } = true;

    [ExportGroup("Collision")]
    [Export] public float CollisionWidthMargin { get; set; } = -0.01f;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayer { get; set; } = 1;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;
    [Export] public bool IsContinuousCollision { get; set; } = false;

    [ExportGroup("Debug")]
    [Export] public bool DrawDebugParticles { get; set; } = false;

    public Node3D StartNodeAttach { get; set; }
    public Node3D EndNodeAttach { get; set; }

    #region Util

    private float GetSegmentLength()
    {
        return RopeLength / SimulationSegments;
    }

    private void ClearSections()
    {
        foreach (var body in _segmentBodies)
        {
            body.QueueFree();
        }

        _segmentBodies.Clear();
    }

    #endregion

    public override void _Ready()
    {
        base._Ready();
        CreateRope();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDisabledWhenInvisible && !VerletRopeMesh.IsRopeVisible)
        {
            ProcessMode = ProcessModeEnum.Disabled;
            return;
        }

        ProcessMode = ProcessModeEnum.Inherit;
        var isEditor = Engine.IsEditorHint();
        if (isEditor && _particleData == null)
        {
            CreateRope();
        }

        var segmentLength = GetSegmentLength();
        for (var i = 0; i < _particleData!.Count; i++)
        {
            if (i == _particleData.Count - 1)
            {
                // There is one less segment, calculate from actual positions
                var segmentEndPosition = new Vector3(segmentLength / 2f, 0, 0);
                _particleData[i].PositionCurrent = _segmentBodies[i - 1].ToGlobal(segmentEndPosition);
            }
            else
            {
                _particleData[i].PositionCurrent = _segmentBodies[i].GlobalPosition;
            }
        }

        VerletRopeMesh.DrawRopeParticles(_particleData);

        if (DrawDebugParticles)
        {
            VerletRopeMesh.DrawRopeDebugParticles(_particleData);
        }
    }

    public override void CreateRope()
    {
        base.CreateRope();
        ClearSections();

        var segmentLength = GetSegmentLength();
        var segmentRotation = new Vector3(0, 0, Mathf.Pi / 2f);
        var segmentShape = new CapsuleShape3D
        {
            Height = segmentLength,
            Radius = RopeWidth + CollisionWidthMargin
        };

        for (var i = 0; i < SimulationSegments; i++)
        {
            var rigidBody = new RigidBody3D
            {
                Position = new Vector3(i * segmentLength, 0, 0),
                CollisionMask = CollisionMask,
                CollisionLayer = CollisionLayer,
                ContinuousCd = IsContinuousCollision
            };

            rigidBody.AddChild(new CollisionShape3D
            {
                Shape = segmentShape,
                Rotation = segmentRotation
            });

            _segmentBodies.Add(rigidBody);
            AddChild(rigidBody);
        }

        for (var i = 0; i < _segmentBodies.Count - 1; i++)
        {
            var currentBody = _segmentBodies[i];
            var nextBody = _segmentBodies[i + 1];

            currentBody.AddChild(new PinJoint3D
            {
                Position = new Vector3(segmentLength / 2f, 0, 0),
                NodeA = currentBody.GetPath(),
                NodeB = nextBody.GetPath()
            });
        }

        var particlePositions = _segmentBodies.ConvertAll(b => b.GlobalPosition);
        var finalPointPosition = new Vector3(segmentLength * _segmentBodies.Count, 0, 0);
        particlePositions.Add(ToGlobal(finalPointPosition));
        _particleData = RopeParticleData.GenerateParticleData(particlePositions);
    }

    public override void DestroyRope()
    {
        ClearSections();
    }
}
