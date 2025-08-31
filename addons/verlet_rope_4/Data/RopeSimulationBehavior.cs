namespace VerletRope4.Data;

/// <summary>
/// Determines how rope is being simulated.
/// <para><see cref="None"/> - Rope is disabled;</para>
/// <para><see cref="Game"/> - Only simulated in the game;</para>
/// <para><see cref="Editor"/> - Rope is simulated in both game and editor;</para>
/// <para><see cref="Selected"/> - Rope is simulated in game and only simulated in editor when selected.</para>
/// </summary>
public enum RopeSimulationBehavior
{
    None,
    Game,
    Editor,
    Selected
}