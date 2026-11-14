using Godot;
using System;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Scenes.Scenarios;

namespace VerletRope4.Tests.Scenes;

/// <summary>
/// Runs every scene scenario in one headless engine run and reports through the process exit code, which is what
/// `run-tests.ps1` gates on.
/// <para>
/// Scenarios are driven as real scenes on the real physics server, over a fixed number of physics frames. The gdUnit4 lane
/// cannot cover that: physics does tick in its host, but by an uncontrolled amount per await, so a whole scene with its
/// drivers and collision queries cannot be run for a known number of steps there.
/// </para>
/// </summary>
public partial class SceneTestRunner : Node
{
    /// <summary> Wall clock limit for the whole run, so a scenario that waits on a frame that never comes fails instead of hanging. </summary>
    private const double RunTimeoutSeconds = 180.0;

    private static readonly SceneScenario[] Scenarios =
    [
        new SwingingRopeScenario(),
        new TetherLeashScenario(),
        new WreckingBallScenario(),
        new GrapplingHookScenario(),
        new FishingRodScenario(),
        new RopeCollisionScenario()
    ];

    public override async void _Ready()
    {
        _ = AbortWhenTimedOut();

        var failedScenarios = 0;
        var failedGoals = 0;

        foreach (var scenario in Scenarios)
        {
            var checks = await RunScenario(scenario);
            failedGoals += checks.FailureCount;
            failedScenarios += checks.FailureCount > 0 ? 1 : 0;
        }

        GD.Print($"[scenes] {Scenarios.Length} scenario(s) run, {failedScenarios} failed, {failedGoals} goal(s) not met");
        GetTree().Quit(failedGoals == 0 ? 0 : 1);
    }

    private async Task AbortWhenTimedOut()
    {
        var timer = GetTree().CreateTimer(RunTimeoutSeconds, true, false, true);
        await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);

        GD.Print($"[scenes] TIMEOUT the run did not finish within {RunTimeoutSeconds} s");
        GetTree().Quit(2);
    }

    private async Task<SceneChecks> RunScenario(SceneScenario scenario)
    {
        var checks = new SceneChecks();
        var packedScene = ResourceLoader.Load<PackedScene>(scenario.ScenePath);

        if (packedScene == null)
        {
            checks.Check(false, $"`{scenario.ScenePath}` could not be loaded");
            ReportResult(scenario, checks);
            return checks;
        }

        var root = packedScene.Instantiate();

        // A scenario that throws - a renamed node, a wrong path, a script that fails in its own `_Ready` - has to fail
        // its own scene, not leave the lane waiting for an exit code that never comes.
        try
        {
            ApplyHeadlessRopeSettings(root);
            AddChild(root);

            await scenario.RunAsync(root, checks);
        }
        catch (Exception exception)
        {
            checks.Check(false, $"the scenario threw `{exception.GetType().Name}`: {exception.Message}");
        }

        root.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        ReportResult(scenario, checks);
        return checks;
    }

    private static void ReportResult(SceneScenario scenario, SceneChecks checks)
    {
        if (checks.FailureCount == 0)
        {
            GD.Print($"[scenes] PASS {scenario.Name}");
            return;
        }

        GD.Print($"[scenes] FAIL {scenario.Name} - {checks.FailureCount} goal(s) not met");

        foreach (var failure in checks.Failures)
        {
            GD.Print($"          {failure}");
        }
    }

    /// <summary>
    /// Disables the visibility based skipping of every rope in the scene, before it enters the tree. Headless, a screen
    /// notifier is never on screen, so a rope that only draws - or only simulates - while visible would stay frozen for
    /// the whole scenario and every goal below would be measured against a dead rope.
    /// </summary>
    private static void ApplyHeadlessRopeSettings(Node root)
    {
        foreach (var node in root.FindChildren("*", "", true, false))
        {
            if (node is not BaseVerletRopePhysical rope)
            {
                continue;
            }

            rope.UseVisibleOnScreenNotifier = false;

            if (rope is VerletRopeSimulated simulatedRope)
            {
                simulatedRope.IsDisabledWhenInvisible = false;
            }
        }
    }
}
