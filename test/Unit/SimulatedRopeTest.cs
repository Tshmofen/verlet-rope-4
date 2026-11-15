using GdUnit4;
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
using VerletRope4.Rendering;
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
    /// Apply is deferred to the end of the frame, which in the test host is a frame boundary away.
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
    /// The mesh child is presentation only and is filled from the rope, so a rope configured in one place still draws with
    /// those values - the child is created on demand, and a guard that skips the mirror drops the whole visual setup.
    /// </summary>
    [TestCase]
    public async Task RopePropertiesAreMirroredOntoItsMeshChild()
    {
        var material = new StandardMaterial3D();
        var rope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);
        rope.MeshType = RopeMeshType.Tube;
        rope.RopeWidth = 0.25f;
        rope.RopeSmoothing = 0.1f;
        rope.IsSmoothRopeStart = false;
        rope.IsSmoothRopeEnd = false;
        rope.SubdivisionLodDistance = 7.0f;
        rope.TubeSegments = 12;
        rope.UseDebugParticles = true;
        rope.MaterialOverride = material;

        await LoadRope(rope);
        // The mesh child is parented deferred, which can land after the frame the load awaited.
        await NextFrame(rope);

        var mesh = GetMesh(rope);

        AssertThat(mesh.MeshType).IsEqual(RopeMeshType.Tube);
        AssertThat(mesh.RopeLength).IsEqualApprox(2.0f, 0.0001f);
        AssertThat(mesh.RopeWidth).IsEqualApprox(0.25f, 0.0001f);
        AssertThat(mesh.RopeSmoothing).IsEqualApprox(0.1f, 0.0001f);
        AssertThat(mesh.IsSmoothRopeStart).IsFalse();
        AssertThat(mesh.IsSmoothRopeEnd).IsFalse();
        AssertThat(mesh.SubdivisionLodDistance).IsEqualApprox(7.0f, 0.0001f);
        AssertThat(mesh.TubeSegments).IsEqual(12);
        AssertThat(mesh.UseDebugParticles).IsTrue();
        AssertThat(mesh.MaterialOverride).IsSame(material);

        rope.RopeWidth = 0.3f;

        AssertThat(mesh.RopeWidth).IsEqualApprox(0.3f, 0.0001f);
    }

    /// <summary> A joint owns the attachment points of its rope, so a manual assignment next to it would leave the two fighting over where the rope ends. </summary>
    [TestCase]
    public void SetAttachmentPointsIsRefusedWhileAJointIsConnected()
    {
        var rope = AutoFree(new VerletRopeSimulated());
        var joint = AutoFree(new VerletJointSimulated());
        rope.SetJoint(joint, false);

        var thrown = CatchException(() => rope.SetAttachmentPoints(null, null, null, null));
        var thrownWithoutJoint = CatchException(() => AutoFree(new VerletRopeSimulated()).SetAttachmentPoints(null, null, null, null));

        AssertThat(thrown is ApplicationException).IsTrue();
        AssertThat(thrown!.Message.Contains("while joint is connected")).IsTrue();
        AssertThat(thrownWithoutJoint == null).IsTrue();
    }

    /// <summary> Manual attachment points are what a rope laid out between two markers is built from, and a new end marker re-lays it without a forced reset. </summary>
    [TestCase]
    public async Task ManualAttachmentPointsLayTheRopeBetweenTheNodes()
    {
        var rope = CreateRope(simulationParticles: 6, ropeLength: 2.0f);
        // Both markers sit away from the rope node, so a particle that fell back to the node position cannot pass as attached.
        var startMarker = new Node3D { Name = "StartMarker", Position = new Vector3(0.0f, 1.0f, 0.0f) };
        var endMarker = new Node3D { Name = "EndMarker", Position = new Vector3(0.0f, 1.0f, 2.0f) };
        var otherEndMarker = new Node3D { Name = "OtherEndMarker", Position = new Vector3(0.0f, 1.0f, 4.0f) };
        rope.AddChild(startMarker);
        rope.AddChild(endMarker);
        rope.AddChild(otherEndMarker);

        await LoadRope(rope);

        rope.SetAttachmentPoints(null, startMarker, null, endMarker);
        rope.CreateRope();

        AssertThat(rope.GetParticlePosition(0).DistanceTo(startMarker.GlobalPosition)).IsLess(0.05f);
        AssertThat(rope.GetParticlePosition(-1).DistanceTo(endMarker.GlobalPosition)).IsLess(0.05f);
        AssertThat(rope.GetParticle(-1)!.Value.IsAttached).IsTrue();

        rope.SetAttachmentPoints(null, startMarker, null, otherEndMarker);
        rope.CreateRope(false);

        AssertThat(rope.GetParticlePosition(0).DistanceTo(startMarker.GlobalPosition)).IsLess(0.05f);
        AssertThat(rope.GetParticlePosition(-1).DistanceTo(otherEndMarker.GlobalPosition)).IsLess(0.05f);
    }

    /// <summary>
    /// `Reset Rope` is the shape reset behind the quick button: a rope that unwrapped into a hanging line is laid out again
    /// from its start point, which is the only way to put a simulated rope back in the shape its scene starts with.
    /// </summary>
    [TestCase]
    public async Task CreateRopeRelaysALiveRopeFromItsStartPoint()
    {
        var rope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);
        // Without preprocessed frames the rope is judged as it was laid out, before the solver starts spreading the jitter.
        rope.PreprocessIterations = 0;

        await LoadRope(rope);
        StepFrames(rope, 90);

        var unwrappedDistance = GetFarthestParticleDistance(rope, rope.GlobalPosition);

        rope.CreateRope();
        var relayedDistance = GetFarthestParticleDistance(rope, rope.GlobalPosition);

        AssertThat(unwrappedDistance).IsGreater(1.0f);
        AssertThat(relayedDistance).IsLess(0.02f);
        AssertThat(rope.GetParticleCount()).IsEqual(8);
    }

    /// <summary> A simulated rope only understands a simulated joint, and names the type it accepts instead. </summary>
    [TestCase]
    public void SimulatedRopeRejectsARigidJoint()
    {
        var rope = AutoFree(new VerletRopeSimulated());
        var joint = AutoFree(new VerletJointRigid());
        rope.SetJoint(joint, false);

        var thrown = CatchException(() => rope.CreateRope());

        AssertThat(thrown is ApplicationException).IsTrue();
        AssertThat(thrown!.Message.Contains(nameof(VerletRopeSimulated))).IsTrue();
        AssertThat(thrown.Message.Contains(nameof(VerletJointSimulated))).IsTrue();
    }

    #region Utility

    private static Exception CatchException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    /// <summary> Distance from the given point to the particle that is farthest away from it. </summary>
    private static float GetFarthestParticleDistance(VerletRopeSimulated rope, Vector3 point)
    {
        var farthestDistance = 0f;

        for (var i = 0; i < rope.GetParticleCount(); i++)
        {
            farthestDistance = Mathf.Max(farthestDistance, rope.GetParticlePosition(i).DistanceTo(point));
        }

        return farthestDistance;
    }

    private static VerletRopeMesh GetMesh(VerletRopeSimulated rope)
    {
        return rope.GetChildren().OfType<VerletRopeMesh>().First();
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

    // A deferred apply flushes after the process signal of the frame it was queued in, so a write is only visible from the
    // frame after that one: two awaits are the contract here, never a spare.
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

    #endregion
}
