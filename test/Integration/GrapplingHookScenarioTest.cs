using GdUnit4;
using System.Threading.Tasks;
using VerletRope4.Tests.Scenes.Scenarios;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives `4_grappling_hook`: the demo cycle has to fire the parked hook on its own and the rope has to follow it. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class GrapplingHookScenarioTest
{
    [TestCase]
    public async Task HookFiresOnItsOwnAndTheRopeFollows()
    {
        await SceneHarness.RunAsync(new GrapplingHookScenario());
    }
}
