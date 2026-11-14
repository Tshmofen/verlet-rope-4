using GdUnit4;
using System.Threading.Tasks;
using VerletRope4.Tests.Scenes.Scenarios;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives `3_wrecking_ball`: the kicked ball has to swing off with the rope end and stay on the joint's leash. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class WreckingBallScenarioTest
{
    [TestCase]
    public async Task BallIsKickedAndStaysOnItsLeash()
    {
        await SceneHarness.RunAsync(new WreckingBallScenario());
    }
}
