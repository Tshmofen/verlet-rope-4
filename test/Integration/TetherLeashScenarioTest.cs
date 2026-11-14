using GdUnit4;
using System.Threading.Tasks;
using VerletRope4.Tests.Integration.Scenarios;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives `2_tether_leash`: a rigid chain between a walking body and a crate has to hold them apart and drag the crate. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class TetherLeashScenarioTest
{
    [TestCase]
    public async Task RigidLeashDragsTheCrate()
    {
        await SceneHarness.RunAsync(new TetherLeashScenario());
    }
}
