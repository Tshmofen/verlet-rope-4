using GdUnit4;
using System.Threading.Tasks;
using VerletRope4.Tests.Scenes.Scenarios;

namespace VerletRope4.Tests.Integration.Scenarios;

/// <summary> Drives `5_fishing_rod`: the line has to hang off the resting rod tip and the cast has to throw the hook away from it. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class FishingRodScenarioTest
{
    [TestCase]
    public async Task CastThrowsTheHookOffTheRodTip()
    {
        await SceneHarness.RunAsync(new FishingRodScenario());
    }
}
