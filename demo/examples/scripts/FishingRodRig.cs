using Godot;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;

namespace VerletRope4.Demo.Examples;

/// <summary>
/// A fishing rod: a rod held by the hand that points where the hand aims, a line tied to the rod's tip and a hook
/// hanging on the end of it. This is everything the rod itself needs - the loop that casts it on a timer lives
/// in <see cref="FishingRodDemo"/>.
/// </summary>
public partial class FishingRodRig : Node3D
{
    public enum RodPhase
    {
        Idle,
        Casting,
        Reeling
    }

    private Vector3 _restGripPosition;

    #region Rod Setup

    /// <summary> Hand that holds the rod. It is what is being moved to swing the rod, and the rod itself never has to know why. </summary>
    [ExportCategory("Rod")]
    [Export] public Node3D Grip { get; set; }

    /// <summary> Node sitting at the tip of the rod, which is what the line is tied to. The rod is a rigid mesh parented to the hand, so this node follows it exactly. </summary>
    [Export] public Node3D RodTip { get; set; }

    /// <summary> Rope used as the fishing line. It keeps the length it is created at, which is what the hook hangs at, and is stretched from there while the hook is out. </summary>
    [Export] public VerletRopeSimulated LineRope { get; set; }

    /// <summary> Joint that ties the line to the rod tip and acts as the reel. </summary>
    [Export] public VerletJointSimulated LineJoint { get; set; }

    /// <summary> Hook that hangs on the end of the line. </summary>
    [Export] public RigidBody3D Hook { get; set; }

    /// <summary> Extra damping the hook is given while it hangs, so it stops swinging on the line instead of waiting for the next cast in motion. </summary>
    [Export] public float HangingDamp { get; set; } = 3.0f;

    /// <summary> Offset the hand is drawn back and up by during the backswing. </summary>
    [ExportCategory("Swing")]
    [Export] public Vector3 BackswingOffset { get; set; } = new(-0.35f, 0.5f, 0.0f);

    /// <summary> Offset the hand is thrown forward and down by as the rod whips through the cast. </summary>
    [Export] public Vector3 ForwardSwingOffset { get; set; } = new(0.55f, -0.35f, 0.0f);

    /// <summary> Direction the hook is thrown in. </summary>
    [ExportCategory("Cast")]
    [Export] public Vector3 CastDirection { get; set; } = Vector3.Right;

    /// <summary> Speed the hook leaves the rod tip with. </summary>
    [Export] public float CastSpeed { get; set; } = 5.0f;

    /// <summary> Vertical aim offset that compensates the drop during the flight. </summary>
    [Export] public float CastLift { get; set; } = 0.6f;

    /// <summary> Leash length used while the hook is out, effectively disabling the pull. </summary>
    [ExportCategory("Line")]
    [Export] public float FreeLeashLength { get; set; } = 30.0f;

    /// <summary> Furthest the hook is allowed to end up from the rod tip before it is put back under it. </summary>
    [Export] public float MaxHookDistance { get; set; } = 7.0f;

    /// <summary> Speed the hook is reeled back in with. </summary>
    [ExportCategory("Reel")]
    [Export] public float ReelSpeed { get; set; } = 4.0f;

    /// <summary> Distance within the hook counts as reeled all the way back to the rod tip. </summary>
    [Export] public float ParkDistance { get; set; } = 0.7f;

    /// <summary> State the rod is currently in. </summary>
    public RodPhase Phase { get; private set; } = RodPhase.Idle;

    #endregion

    /// <summary> Moves the hand along the cast, where -1 is the full backswing, 0 is at rest and 1 is the end of the throw. </summary>
    public void Sway(float progress)
    {
        var offset = progress < 0.0f
            ? BackswingOffset * -progress
            : ForwardSwingOffset * progress;

        Grip.Position = _restGripPosition + offset;
    }

    /// <summary> Throws the hook off the moving rod tip. </summary>
    public void Cast()
    {
        Phase = RodPhase.Casting;

        SetLeashLength(FreeLeashLength);
        var castDirection = (CastDirection.Normalized() + (Vector3.Up * CastLift)).Normalized();
        Hook.LinearDamp = 0.0f;
        Hook.Sleeping = false;
        Hook.LinearVelocity = castDirection * CastSpeed;
        Hook.AngularVelocity = Vector3.Zero;
    }

    /// <summary> Reels the line in. Returns <see langword="true"/> once the hook is back at the rod tip. </summary>
    public bool Reel(float delta)
    {
        Phase = RodPhase.Reeling;

        var hookDistance = GetHookDistance();

        // A hook that ended up further away than the rod can reach is put back
        if (hookDistance > MaxHookDistance)
        {
            ParkHook();
            return true;
        }

        SetLeashLength(Mathf.Clamp(hookDistance - (ReelSpeed * delta), LineRope.RopeLength, MaxHookDistance));
        if (hookDistance > ParkDistance)
        {
            return false;
        }

        ParkHook();
        return true;
    }

    /// <summary> Sets how far the joint lets the rod tip and the hook drift apart. </summary>
    public void SetLeashLength(float distance)
    {
        LineJoint.JointMaxDistance = distance;
        LineJoint.ResetJoint(false);
    }

    #region Rod Logic

    private float GetHookDistance()
    {
        return Hook.GlobalPosition.DistanceTo(RodTip.GlobalPosition);
    }

    private void ParkHook()
    {
        // The hook is put back under the rod tip at the length of the line, so every cast starts from the same
        // place and the line hangs slack neither way round while it waits.
        Hook.LinearVelocity = Vector3.Zero;
        Hook.AngularVelocity = Vector3.Zero;
        Hook.LinearDamp = HangingDamp;
        Hook.Sleeping = false;
        Hook.GlobalPosition = RodTip.GlobalPosition + (Vector3.Down * LineRope.RopeLength);

        SetLeashLength(LineRope.RopeLength);
        Phase = RodPhase.Idle;
    }

    #endregion

    public override void _Ready()
    {
        _restGripPosition = Grip.Position;
        ParkHook();
    }
}
