using Godot;
using System.Collections.Generic;
using VerletRope.Physics.Joints;
using VerletRope4;
using VerletRope4.Data;
using VerletRope4.Rendering;

namespace VerletRope.Physics;

[Tool]
public abstract partial class VerletRopePhysical : Node3D, ISerializationListener
{
    protected readonly List<Rid> CollisionExceptions = [];
    private Vector3[] _editorVertexPositions = [];
    private VerletRopeMesh _ropeMesh;

    protected VerletRopeMesh RopeMesh => _ropeMesh ??= this.FindOrCreateChild<VerletRopeMesh>();
    
    // Properties have the same default values as on `RopeMesh`
    [ExportGroup("Visuals")]
    [Export] public float RopeLength { get; set; } = 3.0f;
    [Export] public float RopeWidth { get; set; } = 0.07f;
    [Export] public float SubdivisionLodDistance { get; set; } = 15.0f;
    [Export] public bool UseVisibleOnScreenNotifier { get; set; } = true;
    [Export] public bool UseDebugParticles { get; set; } = false;
    [Export] public Material MaterialOverride { get; set; }

    public virtual void ClearExceptions()
    {
        CollisionExceptions.Clear();
    }

    public virtual void RegisterExceptionRid(Rid rid, bool toInclude)
    {
        if (toInclude)
        {
            CollisionExceptions.Add(rid);
        }
        else
        {
            CollisionExceptions.Remove(rid);
        }
    }

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

    public void CreateJoint()
    {
        this.FindOrCreateChild<VerletRopeJoint>(true);
    }

    #region Editor

    public void UpdateEditorCollision(RopeParticleData particleData)
    {
        #if TOOLS
        if (particleData.Count != _editorVertexPositions?.Length)
        {
            _editorVertexPositions = new Vector3[particleData.Count];
        }

        for (var i = 0; i < particleData.Count; i++)
        {
            _editorVertexPositions[i] = ToLocal(particleData[i].PositionCurrent);
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