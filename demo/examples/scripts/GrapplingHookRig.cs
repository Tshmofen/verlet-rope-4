using Godot;
using VerletRope4.Physics.Joints;

namespace VerletRope4.Demo.Examples;

/// <summary>
/// A grappling hook body: it is parked in the hand, fired with an impulse, sticks to whatever it hits and is pulled
/// back on recall. This is everything the hook itself needs - the loop that fires it on a timer lives in
/// <see cref="GrapplingHookDemo"/>, so the hook can be dropped into a scene on its own.
/// </summary>
public partial class GrapplingHookRig : RigidBody3D
{
    /// <summary> What the hook is currently doing. </summary>
    public enum HookPhase
    {
        Parked,
        Flying,
        Attached,
        Retracting
    }

    /// <summary> Distance within the hook counts as back in the hand. </summary>
    private const float ParkDistance = 0.05f;

    #region Hook Setup

    /// <summary> Hand the hook is held in and fired from. </summary>
    [ExportCategory("Hook")]
    [Export] public Node3D Hand { get; set; }

    /// <summary> Point the hook is aimed at when it is fired. </summary>
    [Export] public Node3D AimPoint { get; set; }

    /// <summary> Joint that leashes the player to the hook and acts as the winch. </summary>
    [Export] public VerletJointSimulated GrappleJoint { get; set; }

    /// <summary> Speed the hook leaves the hand with. </summary>
    [ExportCategory("Launch")]
    [Export] public float LaunchSpeed { get; set; } = 28.0f;

    /// <summary> Vertical aim offset that compensates the drop during the flight. </summary>
    [Export] public float LaunchLift { get; set; } = 0.4f;

    /// <summary> Leash length used while the hook is parked or in flight, effectively disabling the pull. </summary>
    [Export] public float FreeLeashLength { get; set; } = 40.0f;

    /// <summary> Speed the hook is pulled back to the hand with on recall. </summary>
    [ExportCategory("Recall")]
    [Export] public float RecallSpeed { get; set; } = 12.0f;

    /// <summary> What the hook is currently doing. </summary>
    public HookPhase Phase { get; private set; } = HookPhase.Parked;

    /// <summary> Whether the hook is stuck in a surface. </summary>
    public bool IsAttached => Phase == HookPhase.Attached;

    /// <summary> Distance between the hand and the hook at the moment it attached. </summary>
    public float AttachedDistance { get; private set; }

    #endregion

    /// <summary> Freezes the hook back in the hand and hides the rope, which is what the player holds while it is idle. </summary>
    public void Park()
    {
        FreezeMode = FreezeModeEnum.Kinematic;
        Freeze = true;
        Visible = true;
        GlobalPosition = Hand.GlobalPosition;
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        SetLeashLength(FreeLeashLength);
        SetRopeVisible(false);
        Phase = HookPhase.Parked;
    }

    /// <summary> Keeps the parked hook in the hand, for when the player keeps moving while the hook is not fired. </summary>
    public void FollowHand()
    {
        GlobalPosition = Hand.GlobalPosition;
    }

    /// <summary> Fires the hook out of the hand towards the aim point. </summary>
    public void Launch()
    {
        Freeze = false;
        Sleeping = false;
        Visible = true;
        GlobalPosition = Hand.GlobalPosition;
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        var aimDirection = (AimPoint.GlobalPosition + (Vector3.Up * LaunchLift) - Hand.GlobalPosition).Normalized();
        ApplyCentralImpulse(aimDirection * LaunchSpeed * Mass);

        SetLeashLength(FreeLeashLength);
        SetRopeVisible(true);
        Phase = HookPhase.Flying;
    }

    /// <summary> Pulls the hook back towards the hand. Returns <see langword="true"/> once it is parked there again. </summary>
    public bool Retract(float delta)
    {
        // The hook has to be a free body again before it can travel, and it drops out of the attached phase so it
        // cannot stick to anything it brushes on the way home.
        Freeze = false;
        Sleeping = false;
        Phase = HookPhase.Retracting;

        GlobalPosition = GlobalPosition.MoveToward(Hand.GlobalPosition, RecallSpeed * delta);
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        if (GlobalPosition.DistanceTo(Hand.GlobalPosition) > ParkDistance)
        {
            return false;
        }

        Park();
        return true;
    }

    /// <summary> Re-lays the rope at the hand. Has to be called whenever the hand was moved on its own. </summary>
    public void ResetRope()
    {
        GrappleJoint.VerletRope.CreateRope();
    }

    /// <summary> Sets how far the joint lets the player and the hook drift apart. </summary>
    public void SetLeashLength(float distance)
    {
        GrappleJoint.JointMaxDistance = distance;

        // Joint reconfiguration is what actually passes the new length to the underlying distance joint,
        // and the rope is left untouched to keep the winch from rebuilding the simulation every frame.
        GrappleJoint.ResetJoint(false);
    }

    #region Hook Logic

    private void SetRopeVisible(bool isVisible)
    {
        // Hiding keeps the rope simulating, so it comes back already laid out instead of piling up in the hand.
        GrappleJoint.VerletRope.Visible = isVisible;
    }

    private void Attach()
    {
        // The hook becomes a kinematic anchor, so it stays stuck in the surface it hit while the joint pulls against it.
        // Kinematic bodies take their transform from the node, so the hit position has to be re-applied after freezing,
        // otherwise the body falls back to the transform the physics server last buffered for it.
        var attachPoint = GlobalPosition;
        FreezeMode = FreezeModeEnum.Kinematic;
        Freeze = true;
        GlobalPosition = attachPoint;
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        AttachedDistance = Hand.GlobalPosition.DistanceTo(attachPoint);
        SetLeashLength(AttachedDistance);
        Phase = HookPhase.Attached;
    }

    private void OnBodyEntered(Node body)
    {
        if (Phase != HookPhase.Flying)
        {
            return;
        }

        Attach();
    }

    #endregion

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        Park();
    }
}
