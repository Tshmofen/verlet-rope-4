using Godot;

namespace VerletRope4.Demo.Examples;

/// <summary>
/// Fires the grappling hook on a timer and walks it through a cycle, so the scene shows the hook on its own.
/// Everything in here is demo only - the hook that is being cycled is <see cref="GrapplingHookRig"/>.
/// </summary>
public partial class GrapplingHookDemo : Node
{
    private enum CyclePhase
    {
        Waiting,
        Flying,
        Swinging,
        Reeling,
        Holding,
        Recalling,
        Releasing
    }

    private CyclePhase _phase = CyclePhase.Waiting;
    private float _phaseTime;
    
    /// <summary> Hook that is being cycled. </summary>
    [Export] public GrapplingHookRig Rig { get; set; }

    /// <summary> Player body that gets pulled by the hook. </summary>
    [Export] public RigidBody3D Player { get; set; }

    #region Cycle Settings

    /// <summary> Position the player is put back to at the end of every cycle. </summary>
    [ExportCategory("Demo Cycle")]
    [Export] public Node3D PlayerStart { get; set; }

    /// <summary> Distance the player is reeled in to. </summary>
    [Export] public float ReelDistance { get; set; } = 3.0f;

    /// <summary> Time the hook stays parked before it fires on its own. </summary>
    [Export] public float IdleTime { get; set; } = 1.5f;

    /// <summary> Time the hook is allowed to fly before the launch is considered missed. </summary>
    [Export] public float FlightTimeout { get; set; } = 1.0f;

    /// <summary> Time the player swings freely on the attached hook before the winch starts. </summary>
    [Export] public float SwingTime { get; set; } = 1.2f;

    /// <summary> Time the player is reeled in over. </summary>
    [Export] public float ReelTime { get; set; } = 3.0f;

    /// <summary> Time the player dangles on the reeled in hook before it is recalled. </summary>
    [Export] public float HoldTime { get; set; } = 1.5f;

    /// <summary> Time the recalled hook is held in the hand before the player is put back to the start. </summary>
    [Export] public float ReleaseTime { get; set; } = 1.5f;

    #endregion

    #region Cycle Logic

    private void ChangePhase(CyclePhase phase)
    {
        _phase = phase;
        _phaseTime = 0f;
    }

    private void Reel()
    {
        var reelProgress = Mathf.Clamp(_phaseTime / ReelTime, 0.0f, 1.0f);
        Rig.SetLeashLength(Mathf.Lerp(Rig.AttachedDistance, ReelDistance, reelProgress));
    }

    private void ResetPlayer()
    {
        Player.LinearVelocity = Vector3.Zero;
        Player.AngularVelocity = Vector3.Zero;
        Player.Sleeping = false;
        Player.GlobalPosition = PlayerStart.GlobalPosition;

        Rig.Park();
        Rig.HideRope();
    }

    #endregion

    public override void _PhysicsProcess(double delta)
    {
        _phaseTime += (float)delta;

        switch (_phase)
        {
            case CyclePhase.Waiting:
                Rig.FollowHand();
                if (_phaseTime >= IdleTime)
                {
                    Rig.Launch();
                    ChangePhase(CyclePhase.Flying);
                }
                break;

            case CyclePhase.Flying:
                if (Rig.IsAttached)
                {
                    ChangePhase(CyclePhase.Swinging);
                }
                else if (_phaseTime >= FlightTimeout)
                {
                    ChangePhase(CyclePhase.Recalling);
                }
                break;

            case CyclePhase.Swinging:
                if (_phaseTime >= SwingTime)
                {
                    ChangePhase(CyclePhase.Reeling);
                }
                break;

            case CyclePhase.Reeling:
                Reel();
                if (_phaseTime >= ReelTime)
                {
                    ChangePhase(CyclePhase.Holding);
                }
                break;

            case CyclePhase.Holding:
                if (_phaseTime >= HoldTime)
                {
                    ChangePhase(CyclePhase.Recalling);
                }
                break;

            case CyclePhase.Recalling:
                if (Rig.Retract((float)delta))
                {
                    ChangePhase(CyclePhase.Releasing);
                }
                break;

            case CyclePhase.Releasing:
                Rig.FollowHand();
                if (_phaseTime >= ReleaseTime)
                {
                    ResetPlayer();
                    ChangePhase(CyclePhase.Waiting);
                }
                break;
        }
    }
}
