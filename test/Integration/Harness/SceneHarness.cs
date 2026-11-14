using GdUnit4;
using Godot;
using System;
using System.IO;
using System.Threading.Tasks;
using VerletRope4.Physics;
using VerletRope4.Tests.Integration.Harness;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Integration;

/// <summary>
/// Runs one scene inside the gdUnit4 host, which is what makes a scene run an ordinary test case instead of a separate
/// engine invocation with its own exit code.
/// <para>
/// A scene is driven by waiting for physics frames instead of counting steps: frames and physics ticks do not pair up one
/// to one in this host, so a test that counted them would assert on a scene it never really stepped.
/// </para>
/// </summary>
public static class SceneHarness
{
    /// <summary> Loads the scene, drives it through <paramref name="run"/>, and reports every goal it did not meet as one test failure. </summary>
    public static async Task RunAsync(string scenePath, SceneRun run)
    {
        var packedScene = ResourceLoader.Load<PackedScene>(scenePath);

        AssertObject(packedScene).OverrideFailureMessage($"`{scenePath}` could not be loaded").IsNotNull();

        var root = packedScene.Instantiate();
        var checks = new SceneChecks();

        try
        {
            ApplyHeadlessRopeSettings(root);
            ISceneRunner.Load(root);
            await RunScene(root, checks, run);
        }
        catch (Exception exception)
        {
            checks.Check(false, $"the scene threw `{exception.GetType().Name}`: {exception.Message}");
        }

        AssertThat(checks.FailureCount).OverrideFailureMessage(DescribeFailures(scenePath, checks)).IsEqual(0);
    }

    /// <summary> Waits out real physics ticks, which is what the scene's own driver and ropes are stepped by. </summary>
    public static async Task WaitPhysicsFrames(Node node, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    #region Utility

    /// <summary> Wall clock limit for one scene, so a goal that waits on a frame that never comes fails instead of stalling the session. </summary>
    private const double SceneTimeoutSeconds = 60.0;

    /// <summary> Drives the scene and turns a run that never returns into a failed goal, because the wait it is stuck in cannot report one. </summary>
    private static async Task RunScene(Node root, SceneChecks checks, SceneRun run)
    {
        var timer = root.GetTree().CreateTimer(SceneTimeoutSeconds, true, false, true);
        var sceneRun = run(root, checks);

        if (await Task.WhenAny(sceneRun, WaitTimeout(root, timer)) != sceneRun)
        {
            checks.Check(false, $"the scene did not finish within {SceneTimeoutSeconds} s");
            return;
        }

        await sceneRun;
    }

    private static async Task WaitTimeout(Node root, SceneTreeTimer timer)
    {
        await root.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    private static string DescribeFailures(string scenePath, SceneChecks checks)
    {
        var sceneName = Path.GetFileNameWithoutExtension(scenePath);
        var failurePrefix = System.Environment.NewLine + "  ";

        return $"`{sceneName}` did not meet {checks.FailureCount} goal(s):{failurePrefix}{string.Join(failurePrefix, checks.Failures)}";
    }

    /// <summary>
    /// Disables the visibility based skipping of every rope in the scene, before it enters the tree. Headless, a screen
    /// notifier is never on screen, so a rope that only draws - or only simulates - while visible would stay frozen for
    /// the whole run and every goal would be measured against a dead rope.
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

    #endregion
}
