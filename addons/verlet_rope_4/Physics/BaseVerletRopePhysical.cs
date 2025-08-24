using System;
using Godot;
using VerletRope4.Data;
using VerletRope4.Rendering;
using VerletRope4.Utility;

namespace VerletRope4.Physics;

[Tool]
public abstract partial class BaseVerletRopePhysical : Node3D, ISerializationListener
{
    #if TOOLS
    private EditorUndoRedoManager _undoRedoManager;
    #endif

    private Vector3[] _editorVertexPositions = [];
    private VerletRopeMesh _ropeMesh;

    protected VerletRopeMesh RopeMesh => _ropeMesh ??= this.FindOrCreateChild<VerletRopeMesh>();

    protected PhysicsBody3D StartBody { get; set; }
    protected Node3D StartNode { get; set; }

    protected PhysicsBody3D EndBody { get; set; }
    protected Node3D EndNode { get; set; }
    
    // Properties have the same default values as on `RopeMesh`
    /// <inheritdoc cref="VerletRopeMesh.RopeLength"/>
    [ExportGroup("Visuals")]
    [Export] public float RopeLength { get; set; } = 3.0f;
    /// <inheritdoc cref="VerletRopeMesh.RopeWidth"/>
    [Export] public float RopeWidth { get; set; } = 0.07f;
    /// <inheritdoc cref="VerletRopeMesh.SubdivisionLodDistance"/>
    [Export] public float SubdivisionLodDistance { get; set; } = 15.0f;
    /// <inheritdoc cref="VerletRopeMesh.UseVisibleOnScreenNotifier"/>
    [Export] public bool UseVisibleOnScreenNotifier { get; set; } = true;
    /// <inheritdoc cref="VerletRopeMesh.UseDebugParticles"/>
    [Export] public bool UseDebugParticles { get; set; } = false;
    /// <inheritdoc cref="VerletRopeMesh.MaterialOverride"/>
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

    public abstract void CreateJoint(int actionId = 0, bool toCreate = true);

    public void SetAttachments(PhysicsBody3D startBody, Node3D startLocation, PhysicsBody3D endBody, Node3D endLocation)
    {
        StartBody = startBody;
        StartNode = startLocation ?? startBody;
        EndBody = endBody;
        EndNode = endLocation ?? endBody;
    }

    protected static StringName GetActionMeta(string action)
    {
        return $"verlet_rope_physical_{action}";
    }
    
    #if TOOLS
    #region Editor

    protected void CommitEditorAction(string actionName, Action<EditorUndoRedoManager, int> undoRedoAction)
    {
        if (_undoRedoManager == null)
        {
            GD.PushWarning($"`{nameof(VerletRopeRigidBody)}` has tried to use `{nameof(_undoRedoManager)}`, but it was not associated with the plugin.");
            return;
        }

        var actionId = Random.Shared.Next();
        _undoRedoManager.CreateAction(actionName);
        undoRedoAction.Invoke(_undoRedoManager, actionId);
        _undoRedoManager.CommitAction();
    }
    
    protected void UpdateEditorCollision(RopeParticleData particleData)
    {
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
    }

    public void AssociateUndoRedoManager(EditorUndoRedoManager manager)
    {
        _undoRedoManager = manager;
    }

    public Vector3[] GetEditorSegments()
    {
        return _editorVertexPositions;
    }
    
    #endregion
    #endif

    #region Script Reload

    public void OnBeforeSerialize()
    {
        // Ignore unload
    }

    public void OnAfterDeserialize()
    {
        CallDeferred(MethodName.CreateRope);
    }

    #endregion
}