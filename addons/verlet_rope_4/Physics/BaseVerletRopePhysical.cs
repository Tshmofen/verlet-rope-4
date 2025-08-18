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
    protected Node3D StartNodeAttach { get; set; }
    protected Node3D EndNodeAttach { get; set; }
    
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

    public void SetAttachments(Node3D startNode, Node3D endNode)
    {
        StartNodeAttach = startNode;
        EndNodeAttach = endNode;
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