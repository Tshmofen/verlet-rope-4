using GdUnit4;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Data;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
using VerletRope4.Rendering;
using VerletRope4.Tests.Unit.Harness;
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

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

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
        await UnitHarness.NextFrame(rope);

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

        var thrown = UnitHarness.CatchException(() => rope.SetAttachmentPoints(null, null, null, null));
        var thrownWithoutJoint = UnitHarness.CatchException(() => AutoFree(new VerletRopeSimulated()).SetAttachmentPoints(null, null, null, null));

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

        var thrown = UnitHarness.CatchException(() => rope.CreateRope());

        AssertThat(thrown is ApplicationException).IsTrue();
        AssertThat(thrown!.Message.Contains(nameof(VerletRopeSimulated))).IsTrue();
        AssertThat(thrown.Message.Contains(nameof(VerletJointSimulated))).IsTrue();
    }

    /// <summary>
    /// A rope whose end anchor sits behind its start has to fold back through itself to reach it. With self collision on,
    /// the parts of the rope that are not neighbours are pushed apart to the configured radius on every step; with it off
    /// they end up on top of each other. The pinned ends are left out of the measurement, because a pair whose first index
    /// is attached is skipped by the self collision pass.
    /// </summary>
    [TestCase]
    public async Task SelfCollisionKeepsTheFoldedLegsOfARopeOneRadiusApart()
    {
        var folded = CreateFoldedRope(isSelfColliding: true);
        var crossing = CreateFoldedRope(isSelfColliding: false);

        await LoadRigAsync(folded, crossing);
        StepFrames(folded, 120);
        StepFrames(crossing, 120);

        var foldedDistance = GetSmallestFreeNonNeighborDistance(folded);
        var crossingDistance = GetSmallestFreeNonNeighborDistance(crossing);
        var radius = folded.SelfCollisionRadius;

        // Measured 0.1096 m with self collision on against 0.0756 m with it off, for a 0.12 m radius: the pass removes
        // only a smoothing share of an overlap per step while the constraint pass pulls the legs back together, so they
        // settle just under the radius instead of exactly on it.
        AssertThat(foldedDistance).OverrideFailureMessage($"the folded legs are {foldedDistance:F4} m apart for a {radius:F4} m radius").IsGreater(radius * 0.85f);
        AssertThat(crossingDistance).OverrideFailureMessage($"the folded legs are {crossingDistance:F4} m apart with self collision off").IsLess(radius * 0.8f);
    }

    /// <summary>
    /// A script reload throws away everything the rope built in memory, so the node recreates it from a deferred call - the
    /// rope is only back once the frame that scheduled it has ended, which is why nothing is created in the same frame.
    /// </summary>
    [TestCase]
    public async Task ScriptReloadCreatesTheRopeAgainOnADeferredCall()
    {
        var rope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);

        await LoadRope(rope);
        rope.DestroyRope();
        rope.OnAfterDeserialize();

        AssertThat(rope.IsRopeCreated).IsFalse();

        await UnitHarness.NextFrame(rope);
        await UnitHarness.NextFrame(rope);

        AssertThat(rope.IsRopeCreated).IsTrue();
        AssertThat(rope.GetParticleCount()).IsEqual(8);
    }

    /// <summary>
    /// Wind is a force along <see cref="VerletRopeSimulated.WindDirection"/> scaled by the noise and clamped to its floor
    /// and ceiling, applied to every particle on every step - so a rope with no wind stays in the plane it was laid in,
    /// while one with wind is displaced the way the direction points. <see cref="VerletRopeSimulated.DampingFactor"/> is
    /// applied against the velocity of the particle it damps, so it decides whether that displacement comes to rest:
    /// both wind-blown ropes end up at the same offset, and only the damped one has stopped moving by then.
    /// </summary>
    [TestCase]
    public async Task WindPushesTheRopeAlongItsDirectionAndDampingBringsThatMovementToRest()
    {
        var calm = CreateWindRope(applyWind: false, dampingFactor: 1.0f);
        var wind = CreateWindRope(applyWind: true, dampingFactor: 1.0f);
        var dampedWind = CreateWindRope(applyWind: true, dampingFactor: 500.0f);

        await LoadRigAsync(calm, wind, dampedWind);

        for (var frame = 0; frame < 60; frame++)
        {
            calm._PhysicsProcess(FrameDelta);
            wind._PhysicsProcess(FrameDelta);
            dampedWind._PhysicsProcess(FrameDelta);
        }

        var calmOffset = GetMeanParticleX(calm);
        var windOffset = GetMeanParticleX(wind);
        var dampedOffset = GetMeanParticleX(dampedWind);
        var windMovement = 0.0f;
        var dampedMovement = 0.0f;

        for (var frame = 0; frame < 5; frame++)
        {
            windMovement = Mathf.Max(windMovement, StepAndMeasureMovement(wind));
            dampedMovement = Mathf.Max(dampedMovement, StepAndMeasureMovement(dampedWind));
        }

        // Measured 0.0000 m, 0.5353 m and 0.5556 m of mean offset after 60 steps: the rope with no wind never leaves X = 0,
        // and both wind-blown ropes are thrown the way WindDirection points, so the direction is what pushes them.
        AssertThat(Mathf.Abs(calmOffset)).OverrideFailureMessage($"the rope with no wind drifted to {calmOffset:F4} m").IsLess(0.01f);
        AssertThat(windOffset).OverrideFailureMessage($"the wind pushed the rope to {windOffset:F4} m").IsGreater(0.02f);
        AssertThat(dampedOffset).OverrideFailureMessage($"the damped rope settled at {dampedOffset:F4} m").IsGreater(0.02f);
        // Measured 0.02309 m against 0.00038 m of the largest movement one step makes: the damped rope has come to rest
        // while the undamped one is still swinging, which is what the damping is applied against.
        AssertThat(windMovement).OverrideFailureMessage($"the undamped rope moves {windMovement:F5} m per step").IsGreater(0.005f);
        AssertThat(dampedMovement).OverrideFailureMessage($"the damped rope moves {dampedMovement:F5} m per step against {windMovement:F5}").IsLess(windMovement / 10.0f);
    }

    /// <summary>
    /// <see cref="VerletRopeSimulated.SimulationRate"/> gates the step on the delta a rope has accumulated, never more than
    /// once per physics frame - so a rope set to half the frame rate takes fewer steps than it is given frames, and a frame
    /// long enough to be a freeze is skipped before it can reach the gate at all.
    /// </summary>
    [TestCase]
    public async Task SimulationRateStepsTheRopeFewerTimesThanTheFramesItIsGiven()
    {
        var everyFrame = CreateCountedRope(simulationRate: 0, out var everyFrameSteps);
        var halfRate = CreateCountedRope(simulationRate: 30, out var halfRateSteps);

        await LoadRigAsync(everyFrame, halfRate);

        var everyFrameBaseline = everyFrameSteps.Count;
        var halfRateBaseline = halfRateSteps.Count;

        StepFrames(everyFrame, 60);
        StepFrames(halfRate, 60);

        var everyFrameCount = everyFrameSteps.Count - everyFrameBaseline;
        var halfRateCount = halfRateSteps.Count - halfRateBaseline;

        // Measured 20 steps for 60 frames at a configured 30 per second: the loop accumulates the frame delta as a double
        // while the gate it compares against is the float `1f / SimulationRate`, so two frames of 1/60 still fall a hair
        // under 1/30 and every third frame is the one that fires. The bound leaves room for the accumulator's own margin
        // without accepting a gate that fires on every second frame, which is 30.
        AssertThat(everyFrameCount).IsEqual(60);
        AssertThat(halfRateCount).IsBetween(19, 21);

        halfRate._PhysicsProcess(0.5);
        var afterSkippedFrame = halfRateSteps.Count - halfRateBaseline;

        // The 0.5 s frame is five times the 300 ms DeltaSkipMs, so it is dropped before the rate gate ever sees it.
        AssertThat(afterSkippedFrame).IsEqual(halfRateCount);
    }

    /// <summary>
    /// <see cref="RopeRenderMode.Process"/> draws the mesh from the process callback and leaves the physics tick alone, while
    /// <see cref="RopeRenderMode.Physics"/> is the other way around - which is the whole difference between a rope that
    /// flickers while its node moves and one that redraws every frame. The surface is cleared from the outside so only the
    /// path under test can put one back.
    /// </summary>
    [TestCase]
    public async Task RenderModeDecidesWhetherTheMeshIsDrawnInProcessOrPhysics()
    {
        var physicsRope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);
        var processRope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);
        physicsRope.RenderMode = RopeRenderMode.Physics;
        processRope.RenderMode = RopeRenderMode.Process;

        // Added before the ropes enter the tree, so the case never races the rope's own deferred child.
        var physicsMesh = AddMesh(physicsRope);
        var processMesh = AddMesh(processRope);

        await LoadRigAsync(physicsRope, processRope);

        ClearSurface(physicsMesh);
        physicsRope._Process(FrameDelta);
        AssertThat(physicsMesh.Mesh.GetSurfaceCount()).OverrideFailureMessage("the physics mode drew in the process frame").IsEqual(0);
        physicsRope._PhysicsProcess(FrameDelta);
        AssertThat(physicsMesh.Mesh.GetSurfaceCount()).IsEqual(1);

        ClearSurface(processMesh);
        processRope._PhysicsProcess(FrameDelta);
        AssertThat(processMesh.Mesh.GetSurfaceCount()).OverrideFailureMessage("the process mode drew in the physics frame").IsEqual(0);
        processRope._Process(FrameDelta);
        AssertThat(processMesh.Mesh.GetSurfaceCount()).IsEqual(1);
    }

    #region Utility

    /// <summary>
    /// The presentation child of a rope, created before the rope enters the tree: a rope builds its own one on demand
    /// through a deferred `AddChild`, so a case that reads it in the frame a rope was loaded races that call.
    /// </summary>
    private static VerletRopeMesh AddMesh(VerletRopeSimulated rope)
    {
        // The mesh creates its notifier from its own flag in `_Ready`, which runs before the rope mirrors its value onto
        // it, so the flag is set here for a mesh that has to draw headless.
        var mesh = new VerletRopeMesh { UseVisibleOnScreenNotifier = false };
        rope.AddChild(mesh);

        return mesh;
    }

    private static void ClearSurface(VerletRopeMesh mesh)
    {
        ((ArrayMesh)mesh.Mesh).ClearSurfaces();
    }

    /// <summary> A rope whose end anchor sits behind its start, so the only way to reach it is to fold back through the rope. </summary>
    private static VerletRopeSimulated CreateFoldedRope(bool isSelfColliding)
    {
        var rope = CreateRope(simulationParticles: 12, ropeLength: 2.0f);
        var startAnchor = new Node3D { Name = "StartAnchor" };
        var endAnchor = new Node3D { Name = "EndAnchor", Position = new Vector3(0.0f, 0.0f, -0.05f) };
        rope.AddChild(startAnchor);
        rope.AddChild(endAnchor);
        rope.SetAttachmentPoints(null, startAnchor, null, endAnchor);
        rope.IsSelfColliding = isSelfColliding;
        // Pinned here so a default change cannot move the goal silently: 0.12 m and 0.4 are the node's own defaults.
        rope.SelfCollisionRadius = 0.12f;
        rope.SelfCollisionSmoothing = 0.4f;

        return rope;
    }

    /// <summary> A rope laid between two anchors, so every particle starts on the same line and only the forces move it off. </summary>
    private static VerletRopeSimulated CreateWindRope(bool applyWind, float dampingFactor)
    {
        var rope = CreateRope(simulationParticles: 12, ropeLength: 2.5f);
        var startAnchor = new Node3D { Name = "StartAnchor" };
        var endAnchor = new Node3D { Name = "EndAnchor", Position = new Vector3(0.0f, 0.0f, 2.0f) };
        rope.AddChild(startAnchor);
        rope.AddChild(endAnchor);
        rope.SetAttachmentPoints(null, startAnchor, null, endAnchor);
        rope.ApplyWind = applyWind;
        rope.WindDirection = new Vector3(40.0f, 0.0f, 0.0f);
        // A noise floor at its ceiling makes the wind the constant force of WindDirection, so the case measures the
        // direction the wind is applied in instead of the noise field it is scaled by.
        rope.WindNoise = new FastNoiseLite();
        rope.WindNoiseMin = 1.0f;
        rope.WindNoiseMax = 1.0f;
        rope.DampingFactor = dampingFactor;

        return rope;
    }

    /// <summary> A rope that reports every step it takes through <see cref="VerletRopeSimulated.SimulationStep"/> instead of being judged by its shape. </summary>
    private static VerletRopeSimulated CreateCountedRope(int simulationRate, out List<double> steps)
    {
        var rope = CreateRope(simulationParticles: 8, ropeLength: 2.0f);
        rope.SimulationRate = simulationRate;
        steps = [];
        rope.SimulationStep += steps.Add;

        return rope;
    }

    /// <summary>
    /// The distance between the closest two particles that are neither neighbours nor one of the pinned ends: the self
    /// collision pass skips a particle that is attached, so a pair it contains is never resolved.
    /// </summary>
    private static float GetSmallestFreeNonNeighborDistance(VerletRopeSimulated rope)
    {
        var smallestDistance = float.MaxValue;

        for (var i = 1; i < rope.GetParticleCount() - 1; i++)
        {
            if (rope.GetParticle(i)!.Value.IsAttached)
            {
                continue;
            }

            for (var j = i + 2; j < rope.GetParticleCount() - 1; j++)
            {
                smallestDistance = Mathf.Min(smallestDistance, rope.GetParticlePosition(i).DistanceTo(rope.GetParticlePosition(j)));
            }
        }

        return smallestDistance;
    }

    /// <summary> Mean X of the particles between the two pinned ends, which is what a sideways force shows up in. </summary>
    private static float GetMeanParticleX(VerletRopeSimulated rope)
    {
        var total = 0.0f;

        for (var i = 1; i < rope.GetParticleCount() - 1; i++)
        {
            total += rope.GetParticlePosition(i).X;
        }

        return total / (rope.GetParticleCount() - 2);
    }

    /// <summary> Steps the rope once and returns the largest distance any of its particles moved, which is how fast it is still going. </summary>
    private static float StepAndMeasureMovement(VerletRopeSimulated rope)
    {
        var positions = new Vector3[rope.GetParticleCount()];

        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = rope.GetParticlePosition(i);
        }

        rope._PhysicsProcess(FrameDelta);

        var largestMovement = 0.0f;

        for (var i = 0; i < positions.Length; i++)
        {
            largestMovement = Mathf.Max(largestMovement, positions[i].DistanceTo(rope.GetParticlePosition(i)));
        }

        return largestMovement;
    }

    /// <summary> Loads several ropes under one root, because a runner holds the scene it was given and a node it replaced would stop stepping. </summary>
    private static async Task LoadRigAsync(params Node3D[] ropes)
    {
        var rig = new Node3D { Name = "Rig" };

        foreach (var rope in ropes)
        {
            rig.AddChild(rope);
        }

        ISceneRunner.Load(rig);
        await UnitHarness.NextFrame(rig);
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
        await UnitHarness.NextFrame(rope);
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
