using GdUnit4;
using System.Threading.Tasks;
using VerletRope4.Tests.Integration.Scenarios;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives the purpose-built `rope_collision` scene: a rope falling onto a static floor has to rest on it, not pass through. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class RopeCollisionScenarioTest
{
    [TestCase]
    public async Task RopeFallsOntoTheFloorAndStaysOnIt()
    {
        await SceneHarness.RunAsync(new RopeCollisionScenario());
    }
}
