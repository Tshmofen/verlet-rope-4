using GdUnit4;
using Godot;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary>
/// Covers the simulated rope node itself: how it lays its particles out, which of them stay pinned, how a written
/// property is applied and what it reports when it is asked for something it does not have.
/// <para>
/// The physics step is driven by hand here. Frames pass in the gdUnit4 host and physics ticks with them, but by an
/// uncontrolled amount per await (measured on 4.7.2: 20 idle-frame awaits advanced the physics counter by 9), so stepping
/// the callback is what gives a test the exact number of steps it asserts on.
/// </para>
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "unit")]
public class SimulatedRopeTest
{
    private const double FrameDelta = 1.0 / 60.0;

    [TestCase]
    public async Task RopeIsCreatedOnReadyWithTheConfiguredParticles()
    {
        var rope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);

        AssertThat(rope.IsRopeCreated).IsFalse();

        await LoadRope(rope);

        AssertThat(rope.IsRopeCreated).IsTrue();
        AssertThat(rope.GetParticleCount()).IsEqual(8);
    }

    [TestCase]
    public void RopeExposesItsDocumentedDefaults()
    {
        var rope = AutoFree(new VerletRopeSimulated());

        AssertThat(rope.IsCreatedOnReady).IsTrue();
        AssertThat(rope.SimulationParticles).IsEqual(10);
        AssertThat(rope.RopeLength).IsEqualApprox(3.0f, 0.0001f);
        AssertThat(rope.RopeWidth).IsEqualApprox(0.07f, 0.0001f);
        AssertThat(rope.ApplyGravity).IsTrue();
        AssertThat(rope.GetParticleCount()).IsEqual(0);
    }

    [TestCase]
    public void SimulationParticlesIsClampedToTheSupportedRange()
    {
        var rope = AutoFree(new VerletRopeSimulated());

        rope.SimulationParticles = 1;
        AssertThat(rope.SimulationParticles).IsEqual(3);

        rope.SimulationParticles = 500;
        AssertThat(rope.SimulationParticles).IsEqual(100);
    }

    /// <summary> A rope that starts and ends on the same point is jittered apart on creation, so the pin is judged after the first step. </summary>
    [TestCase]
    public async Task StartParticleStaysPinnedToTheRopeNodePosition()
    {
        var rope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);
        rope.Position = new Vector3(0.0f, 5.0f, 0.0f);

        await LoadRope(rope);
        StepFrames(rope, 10);

        var pinnedPosition = rope.GetParticlePosition(0);
        StepFrames(rope, 30);

        AssertThat(rope.GetParticle(0).HasValue).IsTrue();
        AssertThat(rope.GetParticle(0)!.Value.IsAttached).IsTrue();
        AssertThat(pinnedPosition.DistanceTo(rope.GlobalPosition)).IsLess(0.001f);
        AssertThat(rope.GetParticlePosition(0).DistanceTo(pinnedPosition)).IsLess(0.001f);
    }

    [TestCase]
    public async Task ParticleLookupSupportsNegativeAndMissingIndexes()
    {
        var rope = CreateRope(simulationParticles: 6, ropeLength: 1.5f);

        await LoadRope(rope);

        AssertThat(rope.GetParticle(-1)!.Value.PositionCurrent).IsEqual(rope.GetParticle(5)!.Value.PositionCurrent);
        AssertThat(rope.GetParticle(-6).HasValue).IsTrue();
        AssertThat(rope.GetParticle(6) == null).IsTrue();
        AssertThat(rope.GetParticle(-7) == null).IsTrue();
        AssertThat(rope.GetParticlePosition(42)).IsEqual(Vector3.Zero);
    }

    /// <summary> A simulated rope solves towards its new length, so the constraint pull reels it in without any particle being re-laid. </summary>
    [TestCase]
    public async Task RopeLengthWriteReelsTheRopeIn()
    {
        var rope = CreateRope(simulationParticles: 12, ropeLength: 3.0f);

        await LoadRope(rope);

        StepFrames(rope, 30);
        var hangingLength = rope.GetCurrentRopeLength();

        rope.RopeLength = 0.6f;
        StepFrames(rope, 30);
        var reeledLength = rope.GetCurrentRopeLength();

        AssertThat(hangingLength).IsGreater(2.0f);
        AssertThat(reeledLength).IsLess(1.0f);
        AssertThat(reeledLength).IsGreater(0.3f);
        AssertThat(rope.GetParticleCount()).IsEqual(12);
    }

    [TestCase]
    public async Task DestroyRopeRemovesTheParticleData()
    {
        var rope = CreateRope();

        await LoadRope(rope);
        rope.DestroyRope();

        AssertThat(rope.IsRopeCreated).IsFalse();
        AssertThat(rope.GetParticleCount()).IsEqual(0);
        AssertThat(rope.GetParticle(0) == null).IsTrue();
    }

    /// <summary>
    /// The amount of particles is structural, so writing it re-lays the rope on a following frame instead of immediately.
    /// The apply is deferred to the end of the frame, which in the test host is a frame boundary away.
    /// </summary>
    [TestCase]
    public async Task SimulationParticlesWriteRelaysTheRopeOnTheNextFrame()
    {
        var rope = CreateRope(simulationParticles: 10, ropeLength: 3.0f);

        await LoadRope(rope);

        rope.SimulationParticles = 20;

        await NextFrame(rope);
        await NextFrame(rope);

        AssertThat(rope.GetParticleCount()).IsEqual(20);
        AssertThat(rope.IsRopeCreated).IsTrue();
    }

    /// <summary> A frame that took longer than <see cref="VerletRopeSimulated.DeltaSkipMs"/> is a freeze or a stall, so the rope is not stepped with it. </summary>
    [TestCase]
    public async Task FrameLongerThanDeltaSkipDoesNotStepTheRope()
    {
        var rope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);

        await LoadRope(rope);

        var before = rope.GetParticlePosition(-1);
        rope._PhysicsProcess(0.5);
        var afterSkipped = rope.GetParticlePosition(-1);

        rope._PhysicsProcess(FrameDelta);
        var afterStep = rope.GetParticlePosition(-1);

        AssertThat(rope.DeltaSkipMs).IsEqual(300);
        AssertThat(afterSkipped.DistanceTo(before)).IsLess(0.0001f);
        AssertThat(afterStep.DistanceTo(afterSkipped)).IsGreater(0.0001f);
    }

    /// <summary>
    /// Loads a rope into the test runner and lets one frame pass. The rope creates its mesh child deferred, so a test that
    /// asserts and ends in the same frame leaves that child unparented and the runner reports it as an orphan.
    /// </summary>
    private static async Task LoadRope(VerletRopeSimulated rope)
    {
        ISceneRunner.Load(rope);
        await NextFrame(rope);
    }

    private static async Task NextFrame(Node node)
    {
        await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary> A rope that is configured for a headless run: created on ready and simulated regardless of the host's editor hint. </summary>
    private static VerletRopeSimulated CreateRope(int simulationParticles = 10, float ropeLength = 3.0f)
    {
        return new VerletRopeSimulated
        {
            SimulationParticles = simulationParticles,
            RopeLength = ropeLength,
            UseVisibleOnScreenNotifier = false,
            SimulationBehavior = RopeSimulationBehavior.Editor
        };
    }

    private static void StepFrames(VerletRopeSimulated rope, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            rope._PhysicsProcess(FrameDelta);
        }
    }
}
