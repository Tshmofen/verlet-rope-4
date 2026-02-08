using System;
using Godot;
using VerletRope4.Data;
using VerletRope4.Physics.Joints;
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

    protected RopeParticleData ParticleData { get; set; }
    protected BaseVerletJoint ConnectedJoint { get; private set; }
    protected VerletRopeMesh RopeMesh => _ropeMesh ??= this.FindOrCreateChild<VerletRopeMesh>();
    
    protected Node3D PreviousStart { get; private set; }
    protected PhysicsBody3D StartBody { get; private set; }
    protected Node3D StartNode { get; private set; }
    
    protected Node3D PreviousEnd { get; private set; }
    protected PhysicsBody3D EndBody { get; private set; }
    protected Node3D EndNode { get; private set; }
    
    // Note: Is not using [Export] to be properly grouped in actual inherited properties.
    /// <summary> Determines whether rope is immediately created on <see cref="_Ready"/> call or have to be manually created via <see cref="CreateRope"/>. </summary>
    public abstract bool IsCreatedOnReady { get; set; }

    /// <summary> Returns whether rope is created at the moment, managed via <see cref="CreateRope"/> and <see cref="DestroyRope"/>. </summary>
    public abstract bool IsRopeCreated { get; }
    
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
    
    /// <summary> Resets the rope and all corresponding properties, have to be called after any property changes. It is being called when you press `Reset Rope` quick button. </summary>
    public virtual void CreateRope(bool forceReset = true)
    {
        if (ConnectedJoint != null)
        {
            ConnectedJoint.ResetJoint(false);
            SetAttachmentPointsInternal(
                ConnectedJoint.StartBody,
                ConnectedJoint.StartCustomLocation,
                ConnectedJoint.EndBody,
                ConnectedJoint.EndCustomLocation
            );
        }

        RopeMesh.RopeLength = RopeLength;
        RopeMesh.RopeWidth = RopeWidth;
        RopeMesh.SubdivisionLodDistance = SubdivisionLodDistance;
        RopeMesh.UseVisibleOnScreenNotifier = UseVisibleOnScreenNotifier;
        RopeMesh.UseDebugParticles = UseDebugParticles;
        RopeMesh.MaterialOverride = MaterialOverride;
    }

    /// <summary> Removes underlying particles data and disables rendering. Rope should be created using `CreateRope` to start working again. </summary>
    public virtual void DestroyRope() { }

    /// <summary>Creates corresponding joint child node and adds it to the tree. Is being created via `Deferred`, so one frame have to be awaited to get the joint instance.</summary>
    public abstract void CreateJoint(int actionId = 0, bool toCreate = true);

    protected static StringName GetActionMeta(string action)
    {
        return $"verlet_rope_physical_{action}";
    }

    #region Joint / Attachment

    public override void _Ready()
    {
        if (IsCreatedOnReady || Engine.IsEditorHint())
        {
            CreateRope();
            RopeMesh.UpdateRopeVisibility(ParticleData);
        }
    }

    private void SetAttachmentPointsInternal(PhysicsBody3D startBody, Node3D startLocation, PhysicsBody3D endBody, Node3D endLocation)
    {
        PreviousStart = StartNode ?? StartBody;
        StartBody = startBody;
        StartNode = startLocation ?? startBody;

        PreviousEnd = EndNode ?? EndBody;
        EndBody = endBody;
        EndNode = endLocation ?? endBody;
    }

    /// <summary>
    /// Manually sets attachment points of the Rope without using corresponding <see cref="BaseVerletJoint"/> instance.
    /// Throws an exception if used when <see cref="BaseVerletJoint"/> is already set.
    /// </summary>
    /// <exception cref="ApplicationException"/>
    public void SetAttachmentPoints(PhysicsBody3D startBody, Node3D startLocation, PhysicsBody3D endBody, Node3D endLocation)
    {
        if (ConnectedJoint != null)
        {
            throw new ApplicationException("Attachment points cannot be manually set while joint is connected.");
        }

        SetAttachmentPointsInternal(startBody, startLocation, endBody, endLocation);
    }

    /// <summary> Configures current joint of the rope to determine which points are used as rope connections, and recreates the rope if requested and was already created. </summary>
    public void SetJoint(BaseVerletJoint joint, bool toResetRope = true)
    {
        ConnectedJoint = joint;

        if (IsRopeCreated && toResetRope)
        {
            CreateRope();
        }
    }

    #endregion

    #region Particle Data

    /// <summary> Returns particle struct if it exists or null, supports negative indexes. </summary>
    public RopeParticle? GetParticle(int index)
    {
        if (ParticleData == null || index < -ParticleData.Count || index > ParticleData.Count)
        {
            return null;
        }

        if (index < 0)
        {
            index = ParticleData.Count + index;
        }

        return ParticleData[index];
    }

    /// <summary> Returns currently simulated particles amount. </summary>
    public int GetParticleCount()
    {
        return ParticleData?.Count ?? 0;
    }

    #endregion

    #if TOOLS
    #region Editor

    protected void CommitEditorAction(string actionName, Action<EditorUndoRedoManager, int> undoRedoAction)
    {
        if (_undoRedoManager == null)
        {
            GD.PushWarning($"`{nameof(VerletRopeRigid)}` has tried to use `{nameof(_undoRedoManager)}`, but it was not associated with the plugin.");
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
        CallDeferred(MethodName.CreateRope, true);
    }

    #endregion
}