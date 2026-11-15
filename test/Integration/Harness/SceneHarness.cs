using GdUnit4;
using Godot;
using System;
using System.IO;
using System.Threading;
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
            checks.Check(false, $"the scene threw: {exception}");
        }

        AssertThat(checks.FailureCount).OverrideFailureMessage(DescribeFailures(scenePath, checks)).IsEqual(0);
        AssertThat(checks.GoalCount > 0).OverrideFailureMessage(DescribeMissingGoals(scenePath)).IsTrue();
    }

    /// <summary> Waits out real physics ticks, which is what the scene's own driver and ropes are stepped by. </summary>
    /// <remarks> A cancelled run leaves this wait at its next frame instead of running out the frames it was asked for. </remarks>
    public static async Task WaitPhysicsFrames(Node node, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            _sceneCancellation?.Token.ThrowIfCancellationRequested();
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    #region Utility

    /// <summary> Wall clock limit for one scene, so a goal that waits on a frame that never comes fails instead of stalling the session. </summary>
    private const double SceneTimeoutSeconds = 60.0;

    /// <summary> How long a scene that has just been cancelled gets to leave the frame it is waiting in before the harness stops waiting for it. </summary>
    private const int SceneStopMilliseconds = 5000;

    /// <summary>
    /// The run in flight, so a body that waits for a frame can be stopped instead of running on after its case has ended.
    /// </summary>
    /// <remarks>
    /// Static because a scene body reaches <see cref="WaitPhysicsFrames"/> without a token - the delegate is the scene, not
    /// the harness - and this host drives every scene from its main thread, one run at a time, so only the run in flight
    /// ever reads it.
    /// </remarks>
    private static CancellationTokenSource _sceneCancellation;

    /// <summary> Drives the scene under a wall clock limit, and stops the body on a timeout instead of abandoning the task it returned. </summary>
    private static async Task RunScene(Node root, SceneChecks checks, SceneRun run)
    {
        var timer = root.GetTree().CreateTimer(SceneTimeoutSeconds, true, false, true);
        using var cancellation = new CancellationTokenSource();

        _sceneCancellation = cancellation;

        try
        {
            var sceneRun = run(root, checks);

            if (await Task.WhenAny(sceneRun, WaitTimeout(root, timer)) == sceneRun)
            {
                await sceneRun;
                return;
            }

            checks.Check(false, $"the scene did not finish within {SceneTimeoutSeconds} s");
            await StopSceneRun(sceneRun, checks, cancellation);
        }
        finally
        {
            _sceneCancellation = null;
        }
    }

    /// <summary>
    /// Cancels a body that overran the timeout and waits for it to leave, so it stops at its next frame instead of awaiting a
    /// scene the runner frees at case end - where it would fault into a task nobody observes and take the reason it hung with it.
    /// </summary>
    private static async Task StopSceneRun(Task sceneRun, SceneChecks checks, CancellationTokenSource cancellation)
    {
        cancellation.Cancel();

        if (await Task.WhenAny(sceneRun, Task.Delay(SceneStopMilliseconds)) != sceneRun)
        {
            // Stuck somewhere the token cannot reach: its own failure has nothing left to report to, but it must not stay unobserved.
            _ = sceneRun.ContinueWith(task => _ = task.Exception, TaskScheduler.Default);
            return;
        }

        try
        {
            await sceneRun;
        }
        catch (OperationCanceledException)
        {
            // The stop this method asked for, so it is not a second failure.
        }
        catch (Exception exception)
        {
            checks.Check(false, $"the scene threw while it was stopped: {exception}");
        }
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

    /// <summary> What a scene reports when it recorded nothing at all, since a body whose checks were skipped would otherwise pass. </summary>
    private static string DescribeMissingGoals(string scenePath)
    {
        var sceneName = Path.GetFileNameWithoutExtension(scenePath);

        return $"`{sceneName}` recorded no goal, so it cannot pass: the run never reached a `{nameof(SceneChecks)}` check.";
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
