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

    [ExportToolButton("Reset Rope (Apply Changes)")] public Callable ResetRopeButton => Callable.From(CreateRope);
    [ExportToolButton("Clone Rigid Bodies")] public Callable CloneBodiesButton => Callable.From(CloneRigidBodiesAction);
    [ExportToolButton("Add Rigid Joint")] public Callable AddJointButton => Callable.From(CreateJointAction);
    
    /// <summary> Determines amount of separate <see cref="RigidBody3D"/> segments that will constitute the rope. </summary>
    [ExportGroup("Simulation")]
    [Export(PropertyHint.Range, "1,100")] public int SimulationSegments { get; set; } = 10;
    /// <summary> Determines if <see cref="PinJoint3D"/> is created at the start of the first segment. </summary>
    [Export] public bool IsStartSegmentPinned { get; set; } = true;
    
    /// <summary> Adjusts the radius of rope segment collision. Final collision width equals to <see cref="BaseVerletRopePhysical.RopeWidth"/> with added <see cref="CollisionWidthMargin"/>. </summary>
    [ExportGroup("Collision")]
    [Export] public float CollisionWidthMargin { get; set; } = -0.01f;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayer { get; set; } = 1;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;
    [Export] public bool IsContinuousCollision { get; set; } = false;
    /// <summary> Renders meshes with the same shape and size as <see cref="CollisionShape3D"/> used in segment generation. </summary>
    [Export] public bool ShowCollisionShapeDebug { get; set; } = false;

    /// <summary> Determines overall <see cref="RigidBody3D.Mass"/> of the rope, each segment will have weight equal to <see cref="RigidBody3D.Mass"/> divided by <see cref="SimulationSegments"/> count. </summary>
    [ExportGroup("Physics")]
    [ExportSubgroup("Segments")]
    [Export(PropertyHint.Range, "0.001,10000")] public float TotalRopeMass { get; set; } = 10.0f;
    [Export(PropertyHint.Range, "-8.000,8")] public float GravityScale { get; set; } = 1.0f;
    [Export] public PhysicsMaterial PhysicsMaterialOverride { get; set; }
    [Export] public RigidBody3D.DampMode LinearDampMode { get; set; } = RigidBody3D.DampMode.Combine;
    [Export(PropertyHint.Range, "0.000,100")] public float LinearDamp { get; set; } = 0.0f;
    [Export] public RigidBody3D.DampMode AngularDampMode { get; set; } = RigidBody3D.DampMode.Combine;
    [Export(PropertyHint.Range, "0.000,100")] public float AngularDamp { get; set; } = 0.0f;
    /// <summary> Determines <see cref="PinJoint3D.Param.Bias"/> for each separate joint <see cref="PinJoint3D"/>. Only works with default Physics engine. </summary>
    [ExportSubgroup("Joints")]
    [Export(PropertyHint.Range,"0.01,0.99,0.01")] public float PinBias { get; set; } = 0.3f;
    /// <summary> Determines <see cref="PinJoint3D.Param.Damping"/> for each separate joint <see cref="PinJoint3D"/>. Only works with default Physics engine. </summary>
    [Export(PropertyHint.Range,"0.01,8,0.01")] public float PinDamping { get; set; } = 1.0f;
    /// <summary> Determines <see cref="PinJoint3D.Param.ImpulseClamp"/> for each separate joint <see cref="PinJoint3D"/>. Only works with default Physics engine. </summary>
    [Export(PropertyHint.Range,"0.0,64,0.05")] public float PinImpulseClamp { get; set; } = 0.0f;

    #region Util

    private float GetSegmentLength()
    {
        return RopeLength / SimulationSegments;
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

        var startPosition = StartNode != null
            ? ToLocal(StartNode.GlobalPosition)
            : Vector3.Zero;
        var endPosition = EndNode != null
            ? ToLocal(EndNode.GlobalPosition)
            : startPosition + Vector3.Right * segmentLength * (SimulationSegments + 1);

        var positions = SegmentPlaceUtility.ConnectPoints(startPosition, endPosition, Vector3.Forward, segmentLength, SimulationSegments);
        for (var i = 0; i < SimulationSegments; i++)
        {
            var body = new RigidBody3D
            {
                Position = positions[i],
                CollisionMask = CollisionMask,
                CollisionLayer = CollisionLayer,
                ContinuousCd = IsContinuousCollision,
                Mass = TotalRopeMass / SimulationSegments,
                PhysicsMaterialOverride = PhysicsMaterialOverride,
                LinearDamp = LinearDamp,
                LinearDampMode = LinearDampMode,
                AngularDamp = AngularDamp,
                AngularDampMode = AngularDampMode
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
        PinJoint3D SetPinParameters(PinJoint3D pin)
        {
            pin.SetParam(PinJoint3D.Param.Bias, PinBias);
            pin.SetParam(PinJoint3D.Param.Damping, PinDamping);
            pin.SetParam(PinJoint3D.Param.ImpulseClamp, PinImpulseClamp);
            return pin;
        }

        if (StartNode != null || IsStartSegmentPinned)
        {
            segmentBodies[0].AddChild(SetPinParameters(new PinJoint3D
            {
                Position = Vector3.Zero,
                NodeA = StartBody?.GetPath(),
                NodeB = segmentBodies[0].GetPath()
            }));
        }
        
        var jointPosition = new Vector3(0, GetSegmentLength(), 0);
        for (var i = 0; i < segmentBodies.Count - 1; i++)
        {
            var currentBody = segmentBodies[i];
            var nextBody = segmentBodies[i + 1];

            currentBody.AddChild(SetPinParameters(new PinJoint3D
            {
                Position = jointPosition,
                NodeA = currentBody.GetPath(),
                NodeB = nextBody.GetPath()
            }));
        }

        if (EndNode != null)
        {
            segmentBodies[^1].AddChild(SetPinParameters(new PinJoint3D
            {
                Position = jointPosition,
                NodeA = segmentBodies[^1].GetPath(),
                NodeB = EndBody?.GetPath()
            }));
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

    #region Editor

    private void CloneRigidBodiesAction()
    {
        CommitEditorAction("Verlet Rope - Clone Rigid Bodies", (undoRedo, actionId) =>
        {
            undoRedo.AddDoMethod(this, MethodName.CloneRigidBodies, actionId, true);
            undoRedo.AddUndoMethod(this, MethodName.CloneRigidBodies, actionId, false);
        });
    }

    private void CreateJointAction()
    {
        CommitEditorAction("Verlet Rope - Create Rigid Joint", (undoRedo, actionId) =>
        {
            undoRedo.AddDoMethod(this, MethodName.CreateJoint, actionId, true);
            undoRedo.AddUndoMethod(this, MethodName.CreateJoint, actionId, false);
        });
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

    public void CloneRigidBodies(int actionId = 0, bool toCreate = true)
    {
        var metaName = GetActionMeta("clone_rigid_bodies");

        if (!toCreate)
        {
            GetParent().RemoveChildByMeta(metaName, actionId);
            return;
        }

        var groupNode = new Node3D
        {
            Name = $"Bodies_{Name}",
            Position = Position,
            Rotation = Rotation
        };
        groupNode.SetMeta(metaName, actionId);
        AddSibling(groupNode);

        var cloneBodies = SpawnSegmentBodies(groupNode);
        PinSegmentBodies(cloneBodies);

        groupNode.SetSubtreeOwner(GetTree().EditedSceneRoot);
    }

    public override void CreateJoint(int actionId = 0, bool toCreate = true)
    {
        var metaName = GetActionMeta("create_rigid_joint");

        if (!toCreate)
        {
            this.RemoveChildByMeta(metaName, actionId);
            return;
        }

        var joint = this.CreateChild<VerletRopeRigidJoint>("JointRigid");
        joint.SetMeta(metaName, actionId);
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
