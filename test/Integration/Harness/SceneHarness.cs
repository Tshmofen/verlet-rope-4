using GdUnit4;
using Godot;
using System;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Runs one <see cref="SceneScenario"/> against its real scene inside the gdUnit4 host, which is what makes a scene run an
/// ordinary test case instead of a separate engine invocation with its own exit code.
/// <para>
/// A scenario waits for physics frames instead of counting steps: frames and physics ticks do not pair up one to one in
/// this host, so a scenario that counted them would assert on a scene it never really stepped.
/// </para>
/// </summary>
public static class SceneHarness
{
    /// <summary> Wall clock limit for one scenario, so a goal that waits on a frame that never comes fails instead of stalling the session. </summary>
    private const double ScenarioTimeoutSeconds = 60.0;

    /// <summary> Loads the scenario's scene, drives it, and reports every goal it did not meet as a single test failure. </summary>
    public static async Task RunAsync(SceneScenario scenario)
    {
        var packedScene = ResourceLoader.Load<PackedScene>(scenario.ScenePath);

        AssertObject(packedScene).OverrideFailureMessage($"`{scenario.ScenePath}` could not be loaded").IsNotNull();

        var root = packedScene.Instantiate();
        var checks = new SceneChecks();

        try
        {
            ApplyHeadlessRopeSettings(root);
            ISceneRunner.Load(root);
            await RunScenario(scenario, root, checks);
        }
        catch (Exception exception)
        {
            checks.Check(false, $"the scenario threw `{exception.GetType().Name}`: {exception.Message}");
        }

        AssertThat(checks.FailureCount).OverrideFailureMessage(DescribeFailures(scenario, checks)).IsEqual(0);
    }

    /// <summary> Drives the scenario and turns a run that never returns into a failed goal, because the wait it is stuck in cannot report one. </summary>
    private static async Task RunScenario(SceneScenario scenario, Node root, SceneChecks checks)
    {
        var timer = root.GetTree().CreateTimer(ScenarioTimeoutSeconds, true, false, true);
        var run = scenario.RunAsync(root, checks);

        if (await Task.WhenAny(run, WaitTimeout(root, timer)) != run)
        {
            checks.Check(false, $"the scenario did not finish within {ScenarioTimeoutSeconds} s");
            return;
        }

        await run;
    }

    private static async Task WaitTimeout(Node root, SceneTreeTimer timer)
    {
        await root.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    private static string DescribeFailures(SceneScenario scenario, SceneChecks checks)
    {
        var failurePrefix = System.Environment.NewLine + "  ";

        return $"`{scenario.Name}` did not meet {checks.FailureCount} goal(s):{failurePrefix}{string.Join(failurePrefix, checks.Failures)}";
    }

    /// <summary>
    /// Disables the visibility based skipping of every rope in the scene, before it enters the tree. Headless, a screen
    /// notifier is never on screen, so a rope that only draws - or only simulates - while visible would stay frozen for
    /// the whole scenario and every goal would be measured against a dead rope.
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
