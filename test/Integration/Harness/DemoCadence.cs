namespace VerletRope4.Tests.Integration.Harness;

/// <summary>
/// The clock a demo driver counts its phases in, so an integration case derives its waits from the scene's own durations
/// and keeps one margin clear of the boundary it measures against.
/// </summary>
public static class DemoCadence
{
    /// <summary> Physics ticks per second the demo counts its exported durations in. </summary>
    public const int PhysicsTicksPerSecond = 60;

    /// <summary>
    /// Frames kept clear of the phase boundary a wait is aimed at. The demo counts its own clock in seconds while a wait
    /// counts frames, so a budget derived from a duration alone would land on the very frame a phase flips.
    /// </summary>
    public const int PhaseMarginTicks = 12;
}
