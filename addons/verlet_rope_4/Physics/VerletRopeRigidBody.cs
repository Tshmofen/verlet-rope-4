 using Godot;
using System.Collections.Generic;
using System.Linq;
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
    private RopeParticleData _particleData;
    private readonly List<RigidBody3D> _segmentBodies = [];

    [ExportToolButton("Reset Rope")] public Callable ResetRopeButton => Callable.From(CreateRope);
    [ExportToolButton("Add Joint")] public Callable AddJointButton => Callable.From(CreateJoint);

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

    private void ClearRopeData()
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
                var segmentEndPosition = new Vector3(segmentLength, 0, 0);
                _particleData[i].PositionCurrent = _segmentBodies[i - 1].ToGlobal(segmentEndPosition);
            }
            else
            {
                _particleData[i].PositionCurrent = _segmentBodies[i].GlobalPosition;
            }
        }

        RopeMesh.DrawRopeParticles(_particleData);
        UpdateEditorCollision(_particleData);
        UpdateGizmos();
    }

    public override void CreateJoint()
    {
        this.FindOrCreateChild<VerletRopeRigidJoint>(true);
    }

    public override void CreateRope()
    {
        base.CreateRope();
        ClearRopeData();

        var segmentLength = GetSegmentLength();
        var segmentRotation = new Vector3(0, 0, Mathf.Pi / 2f);
        var segmentShape = new CapsuleShape3D { Height = segmentLength, Radius = RopeWidth + CollisionWidthMargin };
        var segmentMesh = ShowCollisionShapeDebug ? new CapsuleMesh { Height = segmentLength, Radius = RopeWidth + CollisionWidthMargin } : null;

        var testA = Vector3.Zero + Vector3.Up * 0.5f;
        var testB = testA + Vector3.Right * 1;
        var segmentSolution = SegmentPlacerUtility
            .ConnectPoints(testA.ToPlaneArcVector(), testB.ToPlaneArcVector(), segmentLength, SimulationSegments)
            .Select(a => a.ToSpaceArcVector())
            .ToList();
        DebugDraw3D.DrawPointPath(segmentSolution.Select(ToGlobal).ToArray(), size: 0.05f, duration: 10);
        DebugDraw3D.DrawLine(ToGlobal(testA), ToGlobal(testB), duration: 10, color: Colors.Blue);

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
                Position = new Vector3(segmentLength / 2.0f, 0, 0),
                Shape = segmentShape,
                Rotation = segmentRotation
            });

            if (segmentMesh != null)
            {
                rigidBody.AddChild(new MeshInstance3D
                {
                    Position = new Vector3(segmentLength / 2.0f, 0, 0),
                    Mesh = segmentMesh,
                    Rotation = segmentRotation
                });
            }

            rigidBody.SetMeta(InternalMetaStamp, true);
            _segmentBodies.Add(rigidBody);
            AddChild(rigidBody);
        }

        if (IsStartSegmentPinned)
        {
            _segmentBodies[0].AddChild(new PinJoint3D
            {
                Position = new Vector3(-segmentLength / 2f, 0, 0),
                NodeA = _segmentBodies[0].GetPath()
            });
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
        ClearRopeData();
    }
}
