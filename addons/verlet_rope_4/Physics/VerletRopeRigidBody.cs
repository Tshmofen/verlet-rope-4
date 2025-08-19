using Godot;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VerletRope4.Data;
using VerletRope4.Physics.Joints;
using VerletRope4.Utility;

namespace VerletRope4.Physics;

[Tool]
public partial class VerletRopeRigidBody : BaseVerletRopePhysical
{
    public const string ScriptPath = "res://addons/verlet_rope_4/Physics/VerletRopeRigidBody.cs";
    public const string IconPath = "res://addons/verlet_rope_4/icon.svg";

    private static readonly StringName InternalMetaStamp = "verlet_rope_rigid_body";
    private readonly List<RigidBody3D> _segmentBodies = [];
    private RopeParticleData _particleData;

    [ExportToolButton("Reset Rope")] public Callable ResetRopeButton => Callable.From(CreateRope);
    [ExportToolButton("Add Joint")] public Callable AddJointButton => Callable.From(CreateJoint);
    [ExportToolButton("Clone Rigid Bodies")] public Callable CloneBodiesButton => Callable.From(CloneRigidBodies);

    [ExportGroup("Simulation")]
    [Export(PropertyHint.Range, "1,100")] public int SimulationSegments { get; set; } = 10;
    [Export] public bool IsDisabledWhenInvisible { get; set; } = true;
    [Export] public bool IsStartSegmentPinned { get; set; } = true;

    [ExportGroup("Collision")]
    [Export] public float CollisionWidthMargin { get; set; } = -0.01f;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayer { get; set; } = 1;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;
    [Export] public bool IsContinuousCollision { get; set; } = false;
    [Export] public bool ShowCollisionShapeDebug { get; set; } = false;

    #region Util

    private float GetSegmentLength()
    {
        return RopeLength / SimulationSegments;
    }

    #endregion

    #region Physics Spawn

    private void SpawnSegmentBodies()
    {
        var segmentLength = GetSegmentLength();
        var segmentRotation = new Vector3(0, 0, Mathf.Pi / 2f);
        var segmentShape = new CapsuleShape3D { Height = segmentLength, Radius = RopeWidth + CollisionWidthMargin };
        var segmentMesh = ShowCollisionShapeDebug ? new CapsuleMesh { Height = segmentLength, Radius = RopeWidth + CollisionWidthMargin } : null;

        var startPosition = StartNodeAttach != null
            ? ToLocal(StartNodeAttach.GlobalPosition)
            : Vector3.Zero;
        var endPosition = EndNodeAttach != null
            ? ToLocal(EndNodeAttach.GlobalPosition)
            : startPosition + Vector3.Right * segmentLength * (SimulationSegments + 1);

        var positions = SegmentPlaceUtility.ConnectPoints(startPosition, endPosition, Vector3.Forward, segmentLength, SimulationSegments);
        DebugDraw3D.DrawLine(ToGlobal(startPosition + Vector3.Up * 0.3f), ToGlobal(endPosition + Vector3.Up * 0.3f), duration: 10, color: Colors.Blue);

        for (var i = 0; i < SimulationSegments; i++)
        {
            var body = new RigidBody3D
            {
                Position = positions[i],
                CollisionMask = CollisionMask,
                CollisionLayer = CollisionLayer,
                ContinuousCd = IsContinuousCollision
            };

            body.AddChild(new CollisionShape3D
            {
                Position = new Vector3(segmentLength / 2.0f, 0, 0),
                Shape = segmentShape,
                Rotation = segmentRotation
            });

            if (segmentMesh != null)
            {
                body.AddChild(new MeshInstance3D
                {
                    Position = new Vector3(segmentLength / 2.0f, 0, 0),
                    Mesh = segmentMesh,
                    Rotation = segmentRotation
                });
            }

            DebugDraw3D.DrawArrow(ToGlobal(positions[i] + Vector3.Up * 0.3f), ToGlobal(positions[i + 1] + Vector3.Up * 0.3f), duration: 10f);
            //rigidBody.RotateY(Mathf.Pi / 2);
            body.SetMeta(InternalMetaStamp, true);
            _segmentBodies.Add(body);
            AddChild(body);
        }

        for (var i = 0; i < SimulationSegments; i++)
        {
            var body = _segmentBodies[i];
            var direction = positions[i + 1] - positions[i];
        }
    }

    private void PinSegmentBodies()
    {
        var segmentLength = GetSegmentLength();

        if (StartNodeAttach != null || IsStartSegmentPinned)
        {
            _segmentBodies[0].AddChild(new PinJoint3D
            {
                Position = Vector3.Zero,
                NodeA = StartNodeAttach is PhysicsBody3D ? StartNodeAttach.GetPath() : null,
                NodeB = _segmentBodies[0].GetPath()
            });
        }

        for (var i = 0; i < _segmentBodies.Count - 1; i++)
        {
            var currentBody = _segmentBodies[i];
            var nextBody = _segmentBodies[i + 1];

            currentBody.AddChild(new PinJoint3D
            {
                Position = Vector3.Right * segmentLength,
                NodeA = currentBody.GetPath(),
                NodeB = nextBody.GetPath()
            });
        }

        if (EndNodeAttach != null)
        {
            _segmentBodies[^1].AddChild(new PinJoint3D
            {
                Position = Vector3.Right * segmentLength,
                NodeA = _segmentBodies[0].GetPath(),
                NodeB = EndNodeAttach is PhysicsBody3D ? EndNodeAttach.GetPath() : null
            });
        }
    }

    private void GenerateParticleData()
    {
        var particlePositions = _segmentBodies.ConvertAll(b => b.GlobalPosition);
        var finalPointPosition = new Vector3(GetSegmentLength() * _segmentBodies.Count, 0, 0);
        particlePositions.Add(ToGlobal(finalPointPosition));
        _particleData = RopeParticleData.GenerateParticleData(particlePositions);
    }

    #endregion

    public override void _Ready()
    {
        base._Ready();
        CreateRope();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsDisabledWhenInvisible && !RopeMesh.IsRopeVisible)
        {
            ProcessMode = ProcessModeEnum.Disabled;
            return;
        }

        ProcessMode = ProcessModeEnum.Inherit;
        if (_particleData == null)
        {
            CreateRope();
        }

        if (_segmentBodies.Count > 0)
        {
            var segmentLength = GetSegmentLength();
            for (var i = 0; i < _particleData!.Count; i++)
            {
                _particleData[i].PositionCurrent = (i == _particleData.Count - 1) // One less segment than particles, handle separately
                    ? _segmentBodies[i - 1].ToGlobal(new Vector3(segmentLength, 0, 0))
                    : _segmentBodies[i].GlobalPosition;
            }
        }

        RopeMesh.DrawRopeParticles(_particleData);
        UpdateEditorCollision(_particleData);
        UpdateGizmos();
    }

    public void CloneRigidBodies()
    {
        var groupNode = new Node3D
        {
            Name = $"Bodies_{Name}",
            Position = Position
        };

        foreach (var body in _segmentBodies)
        {
            var node = body.Duplicate();
            groupNode.AddChild(node);
        }

        AddSibling(groupNode);
        groupNode.SetSubtreeOwner(GetTree().EditedSceneRoot);
    }

    public override void CreateJoint()
    {
        this.FindOrCreateChild<VerletRopeRigidJoint>("JointRigid");
    }

    public override void CreateRope()
    {
        DestroyRope();
        base.CreateRope();
        SpawnSegmentBodies();
        PinSegmentBodies();
        GenerateParticleData();
    }

    public override void DestroyRope()
    {
        _segmentBodies.Clear();

        foreach (var child in GetChildren())
        {
            if (child.HasMeta(InternalMetaStamp))
            {
                child.QueueFree();
            }
        }
    }
}
