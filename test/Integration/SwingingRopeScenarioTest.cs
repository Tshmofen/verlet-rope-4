using GdUnit4;
using System.Threading.Tasks;
using VerletRope4.Tests.Integration.Scenarios;

namespace VerletRope4.Tests.Integration;

/// <summary> Drives `1_swinging_rope`: a vine hanging off a branch that sways has to stay tied to it and keep its shape. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "integration")]
public class SwingingRopeScenarioTest
{
    [TestCase]
    public async Task VineHangsFromTheSwayingBranch()
    {
        await SceneHarness.RunAsync(new SwingingRopeScenario());
    }
}
