using Godot;
using System.Collections.Generic;
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
    private List<RigidBody3D> _segmentBodies;
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

    private Vector3 GetRotation(Vector3 direction)
    {
        if (direction == Vector3.Zero)
        {
            return Vector3.Zero;
        }

        var norm = direction.Normalized();
        var pitch = -Mathf.Asin(norm.Y);
        var yaw = Mathf.Abs(norm.X) > Mathf.Epsilon || Mathf.Abs(norm.Z) > Mathf.Epsilon
            ? -Mathf.Atan2(norm.X, norm.Z)
            : 0;

        return new Vector3(pitch, yaw, 0); // Roll (Z) cannot be derived from only direction
    }

    #endregion

    #region Physics Spawn

    private List<RigidBody3D> SpawnSegmentBodies(Node target)
    {
        var segmentBodies = new List<RigidBody3D>();
        var segmentLength = GetSegmentLength();
        var segmentPosition = new Vector3(0, segmentLength / 2.0f, 0);
        var segmentShape = new CapsuleShape3D { Height = segmentLength, Radius = RopeWidth + CollisionWidthMargin };
        var segmentMesh = ShowCollisionShapeDebug ? new CapsuleMesh { Height = segmentLength, Radius = RopeWidth + CollisionWidthMargin } : null;

        var startPosition = StartNodeAttach != null
            ? ToLocal(StartNodeAttach.GlobalPosition)
            : Vector3.Zero;
        var endPosition = EndNodeAttach != null
            ? ToLocal(EndNodeAttach.GlobalPosition)
            : startPosition + Vector3.Right * segmentLength * (SimulationSegments + 1);

        var positions = SegmentPlaceUtility.ConnectPoints(startPosition, endPosition, Vector3.Forward, segmentLength, SimulationSegments);
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
                Position = segmentPosition,
                Shape = segmentShape
            });

            if (segmentMesh != null)
            {
                body.AddChild(new MeshInstance3D
                {
                    Position = segmentPosition,
                    Mesh = segmentMesh
                });
            }

            body.SetMeta(InternalMetaStamp, true);
            segmentBodies.Add(body);
            target.AddChild(body);
        }

        for (var i = 0; i < segmentBodies.Count; i++)
        {
            var body = segmentBodies[i];
            body.LookAt(ToGlobal(positions[i + 1]));
            body.RotateObjectLocal(Vector3.Right, -Mathf.Pi / 2);
        }

        return segmentBodies;
    }

    private void PinSegmentBodies(List<RigidBody3D> segmentBodies)
    {
        var segmentLength = GetSegmentLength();
        var pinPosition = new Vector3(0, segmentLength, 0);

        if (StartNodeAttach != null || IsStartSegmentPinned)
        {
            segmentBodies[0].AddChild(new PinJoint3D
            {
                Position = Vector3.Zero,
                NodeA = segmentBodies[0].GetPath(),
                NodeB = StartNodeAttach is PhysicsBody3D ? StartNodeAttach.GetPath() : null
            });
        }

        for (var i = 0; i < segmentBodies.Count - 1; i++)
        {
            var currentBody = segmentBodies[i];
            var nextBody = segmentBodies[i + 1];

            currentBody.AddChild(new PinJoint3D
            {
                Position = pinPosition,
                NodeA = currentBody.GetPath(),
                NodeB = nextBody.GetPath()
            });
        }

        if (EndNodeAttach != null)
        {
            segmentBodies[^1].AddChild(new PinJoint3D
            {
                Position = pinPosition,
                NodeA = segmentBodies[^1].GetPath(),
                NodeB = EndNodeAttach is PhysicsBody3D ? EndNodeAttach.GetPath() : null
            });
        }
    }

    private RopeParticleData GenerateParticleData(List<RigidBody3D> segmentBodies)
    {
        var particlePositions = segmentBodies.ConvertAll(b => b.GlobalPosition);
        var finalPointPosition = new Vector3(GetSegmentLength() * _segmentBodies.Count, 0, 0);
        particlePositions.Add(ToGlobal(finalPointPosition));
        return RopeParticleData.GenerateParticleData(particlePositions);
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
        if (_particleData == null || _segmentBodies == null)
        {
            CreateRope();
        }

        if (_segmentBodies!.Count > 0)
        {
            var segmentLength = GetSegmentLength();
            var endPosition = new Vector3(0, segmentLength, 0);
            for (var i = 0; i < _particleData!.Count; i++)
            {
                _particleData[i].PositionCurrent = (i == _particleData.Count - 1) // One less segment than particles, handle separately
                    ? _segmentBodies[i - 1].ToGlobal(endPosition)
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
            Position = Position,
            Rotation = Rotation
        };
        AddSibling(groupNode);

        var cloneBodies = SpawnSegmentBodies(groupNode);
        PinSegmentBodies(cloneBodies);

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
        _segmentBodies = SpawnSegmentBodies(this);
        PinSegmentBodies(_segmentBodies);
        _particleData = GenerateParticleData(_segmentBodies);
    }

    public override void DestroyRope()
    {
        _segmentBodies = null;

        foreach (var child in GetChildren())
        {
            if (child.HasMeta(InternalMetaStamp))
            {
                child.QueueFree();
            }
        }
    }
}
