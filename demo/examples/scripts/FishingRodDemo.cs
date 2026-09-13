using Godot;

namespace VerletRope4.Demo.Examples;

/// <summary>
/// Casts the fishing rod on a timer and walks it through a cycle, so the scene shows the rod on its own.
/// Everything in here is demo only - the rod that is being cycled is <see cref="FishingRodRig"/>.
/// </summary>
public partial class FishingRodDemo : Node
{
    private enum CyclePhase
    {
        Idle,
        Backswing,
        Swinging,
        Settling,
        Reeling,
        Recovering
    }

    private CyclePhase _phase = CyclePhase.Idle;
    private float _phaseTime;

    #region Cycle Settings

    /// <summary> Rod that is being cycled. </summary>
    [ExportCategory("Demo Cycle")]
    [Export] public FishingRodRig Rig { get; set; }

    /// <summary> Time the rod rests with the hook hanging from it before the next cast. </summary>
    [Export] public float IdleTime { get; set; } = 1.2f;

    /// <summary> Time the hand is drawn back and up over, loading the rod. </summary>
    [Export] public float BackswingTime { get; set; } = 0.6f;

    /// <summary> Time the hand is thrown forward over, while the rod whips and the hook is released. </summary>
    [Export] public float SwingTime { get; set; } = 0.35f;

    /// <summary> Time the cast hook is given to fly and settle on the ground. </summary>
    [Export] public float SettleTime { get; set; } = 2.0f;

    /// <summary> Time the reel is given before the hook is considered stuck. </summary>
    [Export] public float ReelTimeout { get; set; } = 6.0f;

    /// <summary> Time the rod is brought back to its resting pose over. </summary>
    [Export] public float RecoverTime { get; set; } = 0.8f;

    #endregion

    #region Cycle Logic

    private void ChangePhase(CyclePhase phase)
    {
        _phase = phase;
        _phaseTime = 0f;
    }

    private float GetProgress(float duration)
    {
        return Mathf.Clamp(_phaseTime / duration, 0.0f, 1.0f);
    }

    #endregion

    public override void _PhysicsProcess(double delta)
    {
        _phaseTime += (float)delta;

        switch (_phase)
        {
            case CyclePhase.Idle:
                if (_phaseTime >= IdleTime)
                {
                    ChangePhase(CyclePhase.Backswing);
                }
                break;

            case CyclePhase.Backswing:
                Rig.Sway(-Mathf.SmoothStep(0.0f, 1.0f, GetProgress(BackswingTime)));
                if (_phaseTime >= BackswingTime)
                {
                    Rig.Cast();
                    ChangePhase(CyclePhase.Swinging);
                }
                break;

            case CyclePhase.Swinging:
                // The swing carries on from the end of the backswing, so the hand does not jump back to the middle.
                Rig.Sway(Mathf.Lerp(-1.0f, 1.0f, Mathf.SmoothStep(0.0f, 1.0f, GetProgress(SwingTime))));
                if (_phaseTime >= SwingTime)
                {
                    ChangePhase(CyclePhase.Settling);
                }
                break;

            case CyclePhase.Settling:
                if (_phaseTime >= SettleTime)
                {
                    ChangePhase(CyclePhase.Reeling);
                }
                break;

            case CyclePhase.Reeling:
                if (Rig.Reel((float)delta) || _phaseTime >= ReelTimeout)
                {
                    ChangePhase(CyclePhase.Recovering);
                }
                break;

            case CyclePhase.Recovering:
                Rig.Sway(Mathf.Lerp(1.0f, 0.0f, Mathf.SmoothStep(0.0f, 1.0f, GetProgress(RecoverTime))));
                if (_phaseTime >= RecoverTime)
                {
                    ChangePhase(CyclePhase.Idle);
                }
                break;
        }
    }
}
