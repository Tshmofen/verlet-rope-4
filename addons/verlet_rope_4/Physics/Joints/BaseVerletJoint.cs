using Godot;
using System;
using VerletRope4.Utility;

namespace VerletRope4.Physics.Joints;

public abstract partial class BaseVerletJoint : Node, ISerializationListener
{
    private BaseVerletRopePhysical _verletRope;

    private JointApplyReason _pendingApply;
    private bool _hasPendingApply;
    private BaseVerletRopePhysical _appliedRope;
    private PhysicsBody3D _appliedStartBody;
    private Node3D _appliedStartCustomLocation;
    private PhysicsBody3D _appliedEndBody;
    private Node3D _appliedEndCustomLocation;

    /// <summary> A body that will be joined to the start of the Rope - by default <see cref="Node3D.GlobalPosition"/> is used as connection point. </summary>
    public abstract PhysicsBody3D StartBody { get; set; }

    /// <summary>
    /// A custom location for the start of the Rope. If <see cref="StartBody"/> is specified,
    /// used as custom joint location for physics calculations - otherwise behaves as simple start particle <see cref="Node3D.GlobalPosition"/> constraint.
    /// </summary>
    public abstract Node3D StartCustomLocation { get; set; }

    /// <summary> A body that will be joined to the end of the Rope - by default <see cref="Node3D.GlobalPosition"/> is used as connection point. </summary>
    public abstract PhysicsBody3D EndBody { get; set; }

    /// <summary>
    /// A custom location for the end of the Rope. If <see cref="EndBody"/> is specified,
    /// used as custom joint location for physics calculations - otherwise behaves as simple start particle <see cref="Node3D.GlobalPosition"/> constraint.
    /// </summary>
    public abstract Node3D EndCustomLocation { get; set; }

    protected abstract BaseVerletRopePhysical TryFindVerletRope();

    public override void _Ready()
    {
        ResetJoint();
        this.PrintHeadlessWarnings(_GetConfigurationWarnings());
    }

    public override void _EnterTree()
    {
        ResetJoint(false);
    }

    public override void _ExitTree()
    {
        _verletRope?.SetJoint(null, false);
    }

    /// <summary>
    /// Reapplies every joint property and resets the joined rope (if requested and created) - the shape reset behind the
    /// `Reset Joint` quick button. A property change is applied on its own, so this is only needed to re-lay the rope
    /// between attachment points that moved.
    /// </summary>
    public virtual void ResetJoint(bool toResetRope = true)
    {
        UpdateConfigurationWarnings();
        ApplyJointConfiguration();
        _verletRope = TryFindVerletRope();
        _verletRope?.SetJoint(this, toResetRope);
        OnJointApplied();
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (StartBody == null && StartCustomLocation == null && EndBody == null && EndCustomLocation == null)
        {
            return ["No custom connection points specified, joint is doing nothing - assign a connection point or remove it."];
        }

        return [];
    }

    #region Live Apply

    /// <summary> Determines what a written joint property has to be applied by, layered until the next frame. </summary>
    [Flags]
    protected enum JointApplyReason
    {
        None = 0,
        /// <summary> A value the joint reads on every frame, no rope change involved. </summary>
        Configuration = 1,
        /// <summary> The bodies the joined rope has to ignore in collisions. </summary>
        Collision = 2,
        /// <summary> An attachment point the joined rope has to be re-laid between. </summary>
        Attachment = 4,
        /// <summary> The joined rope itself. </summary>
        Rope = 8,
    }

    // ReSharper disable once RedundantAssignment
    /// <summary> Stores a joint property and queues the apply that every reactive property of a joint shares. </summary>
    private protected void SetJointProperty<T>(ref T field, T value, JointApplyReason reason)
    {
        field = value;
        QueueJointApply(reason);
    }

    /// <summary>
    /// Applies the joint properties on the next frame, deferred so that a setter stays safe to call from a scene load,
    /// an undoable editor action or a loop that writes several properties. A write that arrives while the joint is
    /// outside the tree is dropped, as the initial state is what `_EnterTree` and `_Ready` produce.
    /// </summary>
    private void QueueJointApply(JointApplyReason reason)
    {
        // A deferred call would outlive a node that is never added to the tree, and would run against a rope that
        // cannot be laid out yet.
        if (!IsInsideTree())
        {
            return;
        }

        _pendingApply |= reason;

        if (_hasPendingApply)
        {
            return;
        }

        _hasPendingApply = true;
        Callable.From(ApplyJointChangesDeferred).CallDeferred();
    }

    /// <summary> Applies the values a joint type reads on every frame, called after any joint property write. </summary>
    protected virtual void ApplyJointConfiguration() { }

    /// <summary> Pushes the bodies the joined rope has to ignore, called when a collision flag of the joint changes. </summary>
    protected virtual void ApplyJointCollisionExceptions() { }

    private void ApplyJointChangesDeferred()
    {
        if (!_hasPendingApply)
        {
            return;
        }

        var reason = _pendingApply;
        _pendingApply = JointApplyReason.None;
        _hasPendingApply = false;

        // The joint can leave the tree after the write was queued, and laying out a rope from outside the tree would
        // read the transforms the nodes do not have yet.
        if (!IsInsideTree())
        {
            return;
        }

        // The joint could have been pointed at another rope, which has to stop using it before the new one starts.
        UpdateConfigurationWarnings();
        var previousRope = _verletRope;
        _verletRope = TryFindVerletRope();

        if (_verletRope != previousRope)
        {
            previousRope?.SetJoint(null, false);
            _verletRope?.SetJoint(this, false);
        }

        ApplyJointConfiguration();

        if (reason.HasFlag(JointApplyReason.Collision))
        {
            ApplyJointCollisionExceptions();
        }

        if (!reason.HasFlag(JointApplyReason.Attachment) && !reason.HasFlag(JointApplyReason.Rope))
        {
            return;
        }

        if (_verletRope == null || !_verletRope.IsRopeCreated || IsJointApplied())
        {
            return;
        }

        _verletRope.CreateRope();
        OnJointApplied();
    }

    /// <summary> Returns whether the joined rope has already been laid out with the joint values it currently has. </summary>
    private bool IsJointApplied()
    {
        return _appliedRope == _verletRope
            && _appliedStartBody == StartBody
            && _appliedStartCustomLocation == StartCustomLocation
            && _appliedEndBody == EndBody
            && _appliedEndCustomLocation == EndCustomLocation;
    }

    /// <summary> Marks the current joint values as the ones the joined rope has been laid out with. </summary>
    private void OnJointApplied()
    {
        _appliedRope = _verletRope;
        _appliedStartBody = StartBody;
        _appliedStartCustomLocation = StartCustomLocation;
        _appliedEndBody = EndBody;
        _appliedEndCustomLocation = EndCustomLocation;
    }

    #endregion

    #region Script Reload

    public virtual void OnBeforeSerialize()
    {
        // Ignore unload
    }

    public virtual void OnAfterDeserialize()
    {
        // Recreate joint after script reload to make sure all physics related data (and updated logic) is reapplied.
        ResetJoint();
    }

    #endregion
}
