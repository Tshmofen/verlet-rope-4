using Godot;
using System;
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

    private bool _skipPhysicsRender;
    private Vector3 _previousGlobalPosition;
    private Vector3[] _editorVertexPositions = [];
    private VerletRopeMesh _ropeMesh;

    private RopeMeshType _meshType = RopeMeshType.Ribbon;
    private float _ropeLength = 3.0f;
    private float _ropeWidth = 0.07f;
    private float _ropeSmoothing = 0.7f;
    private bool _isSmoothRopeStart = true;
    private bool _isSmoothRopeEnd = true;
    private float _subdivisionLodDistance = 15.0f;
    private bool _useVisibleOnScreenNotifier = true;
    private bool _useDebugParticles;
    private int _tubeSegments = 6;
    private Material _materialOverride;

    private bool _hasPendingStructure;
    private int _appliedSimulationSegments;
    private float _appliedRopeLength;
    private float _appliedRopeWidth;

    protected RopeParticleData ParticleData { get; set; }
    protected BaseVerletJoint ConnectedJoint { get; private set; }
    protected VerletRopeMesh RopeMesh => _ropeMesh ??= this.FindOrCreateChild<VerletRopeMesh>();

    /// <summary> The length a single segment of the rope is expected to have, which is what its constraints solve towards. </summary>
    protected float _restSegmentLength;

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
    /// <inheritdoc cref="RopeMeshType"/>
    [ExportGroup("Visuals")]
    [Export] public RopeMeshType MeshType { get => _meshType; set => SetMirroredProperty(ref _meshType, value); }
    /// <summary>
    /// Determines total target length of the rope, it is just a base value and actual length might be different depending on physics and configured behavior.
    /// Applied at any time while the rope exists: <see cref="VerletRopeSimulated"/> solves towards the new length, while
    /// <see cref="VerletRopeRigid"/> stores its length in its segment bodies and is rebuilt for it.
    /// </summary>
    [Export]
    public float RopeLength
    {
        get => _ropeLength;
        set
        {
            _ropeLength = value;
            ApplyRopeMeshProperties();
            ApplyRopeLengthChange();
        }
    }

    /// <inheritdoc cref="VerletRopeMesh.RopeWidth"/>
    [Export]
    public float RopeWidth
    {
        get => _ropeWidth;
        set
        {
            _ropeWidth = value;
            ApplyRopeMeshProperties();
            ApplyRopeWidthChange();
        }
    }

    /// <inheritdoc cref="RopeRenderMode"/>
    [Export] public RopeRenderMode RenderMode { get; set; } = RopeRenderMode.PhysicsAndMovement;
    /// <inheritdoc cref="VerletRopeMesh.RopeSmoothing"/>
    [Export(PropertyHint.Range, "0,0.99,0.01")]
    public float RopeSmoothing { get => _ropeSmoothing; set => SetMirroredProperty(ref _ropeSmoothing, value); }
    /// <inheritdoc cref="VerletRopeMesh.IsSmoothRopeStart"/>
    [Export] public bool IsSmoothRopeStart { get => _isSmoothRopeStart; set => SetMirroredProperty(ref _isSmoothRopeStart, value); }
    /// <inheritdoc cref="VerletRopeMesh.IsSmoothRopeEnd"/>
    [Export] public bool IsSmoothRopeEnd { get => _isSmoothRopeEnd; set => SetMirroredProperty(ref _isSmoothRopeEnd, value); }
    /// <inheritdoc cref="VerletRopeMesh.SubdivisionLodDistance"/>
    [Export] public float SubdivisionLodDistance { get => _subdivisionLodDistance; set => SetMirroredProperty(ref _subdivisionLodDistance, value); }
    /// <inheritdoc cref="VerletRopeMesh.UseVisibleOnScreenNotifier"/>
    [Export] public bool UseVisibleOnScreenNotifier { get => _useVisibleOnScreenNotifier; set => SetMirroredProperty(ref _useVisibleOnScreenNotifier, value); }
    /// <inheritdoc cref="VerletRopeMesh.UseDebugParticles"/>
    [Export] public bool UseDebugParticles { get => _useDebugParticles; set => SetMirroredProperty(ref _useDebugParticles, value); }
    /// <inheritdoc cref="VerletRopeMesh.TubeSegments"/>
    [Export(PropertyHint.Range, "3,32")]
    public int TubeSegments { get => _tubeSegments; set => SetMirroredProperty(ref _tubeSegments, value); }
    /// <inheritdoc cref="VerletRopeMesh.MaterialOverride"/>
    [Export] public Material MaterialOverride { get => _materialOverride; set => SetMirroredProperty(ref _materialOverride, value); }

    /// <summary>
    /// Resets the rope and applies all corresponding properties - creates the rope if it does not exist yet, and re-lays
    /// every particle if it does. Only needed to reset the shape: every property change is applied on its own while the
    /// rope exists. It is being called when you press `Reset Rope` quick button.
    /// </summary>
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

        ApplyRopeMeshProperties();
        _appliedRopeLength = RopeLength;
        _appliedRopeWidth = RopeWidth;

        _previousGlobalPosition = StartNode.GetSafeGlobalPosition() ?? GlobalPosition;
    }

    /// <summary> Removes underlying particles data and disables rendering. Rope should be created using `CreateRope` to start working again. </summary>
    public virtual void DestroyRope() { }

    /// <summary>Creates corresponding joint child node and adds it to the tree. Is being created via `Deferred`, so one frame have to be awaited to get the joint instance.</summary>
    public abstract void CreateJoint(int actionId = 0, bool toCreate = true);

    protected static StringName GetActionMeta(string action)
    {
        return $"verlet_rope_physical_{action}";
    }

    protected void TryDrawRope(bool isPhysics = true)
    {
        if (ParticleData == null || ParticleData.Count == 0)
        {
            return;
        }

        if (isPhysics && RenderMode == RopeRenderMode.Process)
        {
            return;
        }

        if (isPhysics && _skipPhysicsRender)
        {
            _skipPhysicsRender = false;
            return;
        }

        RopeMesh.DrawRopeParticles(ParticleData);
        RopeMesh.UpdateRopeVisibility(ParticleData);
        _skipPhysicsRender = !isPhysics;
    }

    public override void _Process(double delta)
    {
        if (RenderMode == RopeRenderMode.Physics || ParticleData == null || ParticleData.Count == 0)
        {
            return;
        }

        if (RenderMode == RopeRenderMode.PhysicsAndMovement)
        {
            var currentGlobalPosition = StartNode.GetSafeGlobalPosition() ?? GlobalPosition;
            var changeDelta = currentGlobalPosition - _previousGlobalPosition;
            _previousGlobalPosition = currentGlobalPosition;

            if (changeDelta.IsZeroApprox())
            {
                return;
            }
        }

        // Sync the mesh render to ensure no flickering when node is moved between physics frames.
        // The mesh origin updates immediately when the parent moves, but the particle positions are
        // only updated on physics ticks – redrawing in _Process prevents a one‑frame misalignment.
        TryDrawRope(false);
    }

    public override void _Ready()
    {
        if (IsCreatedOnReady || Engine.IsEditorHint())
        {
            CreateRope();
            RopeMesh.UpdateRopeVisibility(ParticleData);
        }

        _previousGlobalPosition = GlobalPosition;
    }

    #region Joint / Attachment

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

    #region Live Apply

    /// <summary>
    /// Applies a written <see cref="RopeLength"/> to a rope that is already built: a rope type that solves towards its length takes it on at once,
    /// one that stores its length in its structure is rebuilt for it. Before that, <see cref="CreateRope"/> is what turns the value into a structure.
    /// </summary>
    private void ApplyRopeLengthChange()
    {
        if (ParticleData == null || ParticleData.Count == 0)
        {
            return;
        }

        if (!IsRopeLengthStructural())
        {
            ApplyRopeLength();
            return;
        }

        if (!Mathf.IsEqualApprox(RopeLength, _appliedRopeLength))
        {
            QueueRopeStructureApply();
        }
    }

    /// <summary> Applies a written <see cref="RopeWidth"/> to the rope types whose structure is built from it - the shape and the joints of a segment chain. </summary>
    private void ApplyRopeWidthChange()
    {
        if (ParticleData == null || ParticleData.Count == 0 || !IsRopeWidthStructural())
        {
            return;
        }

        if (!Mathf.IsEqualApprox(RopeWidth, _appliedRopeWidth))
        {
            QueueRopeStructureApply();
        }
    }

    /// <summary> Returns whether the rope is configured with a different amount of particles than the one the current structure was built for. </summary>
    protected bool IsSimulationSegmentsChanged()
    {
        return GetSimulationSegments() != _appliedSimulationSegments;
    }

    /// <summary>
    /// Marks the currently configured structure as the one that is applied, called by a rope type once it has rebuilt itself.
    /// A rebuild that is still queued would produce the same structure, so it is cancelled with it.
    /// </summary>
    protected void OnStructureApplied()
    {
        _appliedSimulationSegments = GetSimulationSegments();
        _appliedRopeLength = RopeLength;
        _appliedRopeWidth = RopeWidth;
        _hasPendingStructure = false;
    }

    /// <summary> Returns whether <see cref="CreateRope"/> has to rebuild the rope, which is the case when the rope does not exist yet, its amount of particles changed or its attachment points moved. </summary>
    protected bool IsRopeStructureOutdated(bool forceReset)
    {
        return forceReset || !IsRopeCreated || IsSimulationSegmentsChanged() || PreviousStart != StartNode || PreviousEnd != EndNode;
    }

    /// <summary> Sends the rope properties down to the <see cref="VerletRopeMesh"/> child, which is a presentation node that is not meant to be configured on its own. </summary>
    private void ApplyRopeMeshProperties()
    {
        RopeMesh.MeshType = MeshType;
        RopeMesh.RopeLength = RopeLength;
        RopeMesh.RopeWidth = RopeWidth;
        RopeMesh.RopeSmoothing = RopeSmoothing;
        RopeMesh.IsSmoothRopeStart = IsSmoothRopeStart;
        RopeMesh.IsSmoothRopeEnd = IsSmoothRopeEnd;
        RopeMesh.SubdivisionLodDistance = SubdivisionLodDistance;
        RopeMesh.UseVisibleOnScreenNotifier = UseVisibleOnScreenNotifier;
        RopeMesh.UseDebugParticles = UseDebugParticles;
        RopeMesh.TubeSegments = TubeSegments;
        RopeMesh.MaterialOverride = MaterialOverride;
    }

    // ReSharper disable once RedundantAssignment
    /// <summary> Stores a property the presentation child mirrors, and applies the mirror, which is the reaction every one of them shares. </summary>
    private void SetMirroredProperty<T>(ref T field, T value)
    {
        field = value;
        ApplyRopeMeshProperties();
    }

    /// <summary>
    /// Rebuilds the underlying structure on the next frame, deferred so that a setter stays safe to call from a scene load,
    /// an undoable editor action or a loop that writes several properties. A caller queues only a change its structure is
    /// built from, and a rope that does not exist yet is built by <see cref="CreateRope"/> instead.
    /// </summary>
    protected void QueueRopeStructureApply()
    {
        if (ParticleData == null || ParticleData.Count == 0 || _hasPendingStructure)
        {
            return;
        }

        _hasPendingStructure = true;
        Callable.From(ApplyRopeStructureDeferred).CallDeferred();
    }

    /// <summary> Rebuilds the rope for the amount of particles it is currently configured with, keeping the joint and the attachment points as they are. </summary>
    protected abstract void ApplyRopeStructure();

    /// <summary> Applies <see cref="RopeLength"/> to a rope that is already created. A rope type that stores its length as a separate structure rebuilds instead, see <see cref="ApplyRopeStructure"/>. </summary>
    protected abstract void ApplyRopeLength();

    /// <summary> Returns the amount of segments the rope is currently configured to have, the value that determines the particle data shape. </summary>
    protected abstract int GetSimulationSegments();

    /// <summary> Returns whether the rope has to be rebuilt to apply <see cref="RopeLength"/>, which is the case for a rope type that stores its length as a structure rather than solving towards it. </summary>
    protected virtual bool IsRopeLengthStructural()
    {
        return false;
    }

    /// <summary> Returns whether the rope has to be rebuilt to apply <see cref="RopeWidth"/>, which is the case for a rope type that builds its collision shapes and joints from it. </summary>
    protected virtual bool IsRopeWidthStructural()
    {
        return false;
    }

    /// <summary> Returns the length every segment of the rope is supposed to have, or zero when the rope has no segments to spread it across. </summary>
    protected float GetTargetRestSegmentLength()
    {
        var simulationSegments = GetSimulationSegments();
        return simulationSegments > 0 ? RopeLength / simulationSegments : 0f;
    }

    private void ApplyRopeStructureDeferred()
    {
        if (!_hasPendingStructure)
        {
            return;
        }

        _hasPendingStructure = false;
        ApplyRopeStructure();
    }

    #endregion

    #region Particle Data

    /// <summary> Returns particle struct if it exists or null, supports negative indexes. </summary>
    public RopeParticle? GetParticle(int index)
    {
        if (ParticleData == null || index < -ParticleData.Count || index >= ParticleData.Count)
        {
            return null;
        }

        if (index < 0)
        {
            index = ParticleData.Count + index;
        }

        return ParticleData[index];
    }

    /// <summary> Returns current physics position of a particle, supports negative indexes, returns <see cref="Vector3.Zero"/> when there is no such particle. </summary>
    public Vector3 GetParticlePosition(int index)
    {
        return GetParticle(index)?.PositionCurrent ?? Vector3.Zero;
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
