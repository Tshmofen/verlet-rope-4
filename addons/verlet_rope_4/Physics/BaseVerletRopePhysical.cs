using System.Collections.Generic;
using System.Linq;
using Godot;
using VerletRope4.Data;
using VerletRope4.Rendering;
using VerletRope4.Utility;

namespace VerletRope4.Physics;

[Tool]
public abstract partial class BaseVerletRopePhysical : Node3D, ISerializationListener
{
    private Vector3[] _editorVertexPositions = [];
    private VerletRopeMesh _ropeMesh;

    protected VerletRopeMesh RopeMesh => _ropeMesh ??= this.FindOrCreateChild<VerletRopeMesh>();

    protected PhysicsBody3D StartBody { get; set; }
    protected Node3D StartNode { get; set; }

    protected PhysicsBody3D EndBody { get; set; }
    protected Node3D EndNode { get; set; }
    
    // Properties have the same default values as on `RopeMesh`
    [ExportGroup("Visuals")]
    [Export] public float RopeLength { get; set; } = 3.0f;
    [Export] public float RopeWidth { get; set; } = 0.07f;
    [Export] public float SubdivisionLodDistance { get; set; } = 15.0f;
    [Export] public bool UseVisibleOnScreenNotifier { get; set; } = true;
    [Export] public bool UseDebugParticles { get; set; } = false;
    [Export] public Material MaterialOverride { get; set; }

    public virtual void CreateRope()
    {
        RopeMesh.RopeLength = RopeLength;
        RopeMesh.RopeWidth = RopeWidth;
        RopeMesh.SubdivisionLodDistance = SubdivisionLodDistance;
        RopeMesh.UseVisibleOnScreenNotifier = UseVisibleOnScreenNotifier;
        RopeMesh.UseDebugParticles = UseDebugParticles;
        RopeMesh.MaterialOverride = MaterialOverride;
    }

    public virtual void DestroyRope() { }

    public abstract void CreateJoint();

    public void SetAttachments(PhysicsBody3D startBody, Node3D startLocation, PhysicsBody3D endBody, Node3D endLocation)
    {
        StartBody = startBody;
        StartNode = startLocation ?? startBody;
        EndBody = endBody;
        EndNode = endLocation ?? endBody;
    }

    #region Editor

    public void UpdateEditorCollision(RopeParticleData particleData)
    {
        #if TOOLS
        if (particleData.Count != _editorVertexPositions?.Length)
        {
            _editorVertexPositions = new Vector3[particleData.Count * 2 - 2];
        }

        if (_editorVertexPositions.Length == 0)
        {
            return;
        }

        for (var i = 0; i < _editorVertexPositions.Length; i++)
        {
            _editorVertexPositions[i] = ToLocal(particleData[(i + 1) / 2].PositionCurrent);
        }
        #endif
    }

    public Vector3[] GetEditorSegments()
    {
        return _editorVertexPositions;
    }

    #endregion

    #region Script Reload

    public void OnBeforeSerialize()
    {
        // Ignore unload
    }

    public void OnAfterDeserialize()
    {
        CreateRope();
    }

    #endregion
}