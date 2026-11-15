using GdUnit4;
using Godot;
using System.Collections.Generic;
using System.Linq;
using VerletRope4.Data;
using VerletRope4.Rendering;
using VerletRope4.Tests.Unit.Harness;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary>
/// Covers that the mesh tools turn particle data into geometry. The screen notifier is off on every mesh here, because
/// headless it is never "on screen" and a mesh that is not visible draws nothing.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "unit")]
public class RopeMeshTest
{
    [TestCase]
    public void RibbonMeshDrawsASurfaceForTheParticles()
    {
        var mesh = CreateMesh(RopeMeshType.Ribbon);

        ISceneRunner.Load(mesh);
        mesh.DrawRopeParticles(CreateParticles());

        AssertDrawn(mesh);
    }

    [TestCase]
    public void TubeMeshDrawsASurfaceForTheParticles()
    {
        var mesh = CreateMesh(RopeMeshType.Tube);

        ISceneRunner.Load(mesh);
        mesh.DrawRopeParticles(CreateParticles());

        AssertDrawn(mesh);
    }

    [TestCase]
    public void MeshSkipsDrawingWhileItIsNotInTheTree()
    {
        var mesh = AutoFree(CreateMesh(RopeMeshType.Ribbon));

        mesh.DrawRopeParticles(CreateParticles());

        AssertThat(mesh.Mesh == null || mesh.Mesh.GetSurfaceCount() == 0).IsTrue();
    }

    /// <summary>
    /// A tube is framed from the direction between two particles, and two particles that sit on the same point have none -
    /// handing that zero vector to the quaternion a frame is built with is what the builder guards against. The surface of
    /// such a tube has to commit and have no extent at all, and only a real direction may give it one. Caps need a ring of
    /// three segments to close, so a tube configured below that draws its rings alone.
    /// </summary>
    [TestCase]
    public void TubeMeshWithAZeroTangentDrawsNoExtentAndSkipsItsCapsBelowThreeSegments()
    {
        var collapsedMesh = CreateMesh(RopeMeshType.Tube);

        ISceneRunner.Load(collapsedMesh);

        var thrown = UnitHarness.CatchException(() => collapsedMesh.DrawRopeParticles(CreateParticlesAtSamePoint(6)));

        AssertThat(thrown == null).OverrideFailureMessage($"the zero tangent threw: {thrown}").IsTrue();
        AssertThat(collapsedMesh.Mesh.GetSurfaceCount()).IsEqual(1);
        // Every ring of a tube that has no direction to be framed by collapses onto the same point, so the committed
        // surface spans nothing at all: measured 0.000017 m, which is the float error of the interpolation itself.
        AssertThat(collapsedMesh.GetAabb().Size.Length()).IsLess(0.0001f);

        collapsedMesh.DrawRopeParticles(CreateParticles());

        // Measured 1.277693 m of extent for the same particle count once a real direction is there, so the collapsed
        // surface is the one without geometry and not a surface this tool never commits.
        AssertThat(collapsedMesh.GetAabb().Size.Length()).IsGreater(0.5f);

        var narrowMesh = CreateMesh(RopeMeshType.Tube);
        // Below the three segments a cap is built from, the tube is drawn without caps.
        narrowMesh.TubeSegments = 2;

        ISceneRunner.Load(narrowMesh);
        narrowMesh.DrawRopeParticles(CreateParticles());

        // 108 vertices for two segment rings against 360 for six: the nine rings that carry triangles are 6 vertices per
        // segment either way, and the 36 vertices the two caps add are the ones a two segment tube does not commit.
        AssertThat(GetVertexCount(narrowMesh)).IsEqual(108);
        AssertThat(GetVertexCount(collapsedMesh)).IsEqual(360);
    }

    /// <summary>
    /// The committed tube of a fixed particle list: the rings of six segments around a straight line, capped at both
    /// ends. Pinned as counts and as the box they span, so a change that only keeps "a surface exists" fails here.
    /// </summary>
    [TestCase]
    public void TubeMeshCommitsTheRingsAndCapsOfAConfiguredParticleList()
    {
        var mesh = CreateMesh(RopeMeshType.Tube);

        ISceneRunner.Load(mesh);
        mesh.DrawRopeParticles(CreateParticles());

        var bounds = mesh.GetAabb();

        // Nine of the ten rings carry triangles of 6 segments and 6 vertices each, and the two caps 3 vertices per
        // segment: measured 360 vertices, which is 120 triangles.
        AssertThat(GetVertexCount(mesh)).IsEqual(360);
        AssertThat(GetVertexCount(mesh) / 3).IsEqual(120);
        // Measured (0.2, 1.25, 0.173205) from the box's own minimum (-0.1, -1.25, -0.086603): two rope widths across, the
        // 1.25 m the particle list spans, and the 0.1732 m a hexagon with a 0.1 m radius spans across its flats.
        AssertThat(bounds.Size.X).IsEqualApprox(0.2f, 0.0001f);
        AssertThat(bounds.Size.Y).IsEqualApprox(1.25f, 0.0001f);
        AssertThat(bounds.Size.Z).IsEqualApprox(0.173205f, 0.0001f);
        AssertThat(bounds.Position.X).IsEqualApprox(-0.1f, 0.0001f);
        AssertThat(bounds.Position.Y).IsEqualApprox(-1.25f, 0.0001f);
        AssertThat(bounds.Position.Z).IsEqualApprox(-0.086603f, 0.0001f);
    }

    /// <summary>
    /// The screen notifier is what lets a rope stop drawing once it leaves the view, and headless it is never on screen -
    /// so a mesh that was readied with one draws nothing, while the flag being off is both a drawn mesh and the
    /// configuration warning that recommends turning it back on. The notifier is only created on ready, which is why the
    /// drawing half is checked on a mesh that never had one.
    /// </summary>
    [TestCase]
    public void OnScreenNotifierHidesTheMeshAndItsWarningFollowsTheFlag()
    {
        var notifiedMesh = new VerletRopeMesh { MeshType = RopeMeshType.Tube, RopeWidth = 0.1f, RopeSmoothing = 0.0f, UseVisibleOnScreenNotifier = true };

        ISceneRunner.Load(notifiedMesh);

        var notifier = notifiedMesh.GetChildren().OfType<VisibleOnScreenNotifier3D>().First();

        AssertThat(notifiedMesh.IsRopeVisible).IsFalse();
        AssertThat(notifiedMesh._GetConfigurationWarnings().Length).IsEqual(0);

        notifiedMesh.DrawRopeParticles(CreateParticles());
        AssertThat(notifiedMesh.Mesh.GetSurfaceCount()).IsEqual(0);

        // The notifier is what the rope reports its own box to, which is the only thing that can make it on screen again.
        notifiedMesh.UpdateRopeVisibility(CreateParticles());
        AssertThat(notifier.Aabb.Size).IsNotEqual(Vector3.Zero);

        notifiedMesh.UseVisibleOnScreenNotifier = false;
        var warnings = notifiedMesh._GetConfigurationWarnings();

        AssertThat(warnings.Length).IsEqual(1);
        AssertThat(warnings[0].Contains(nameof(VerletRopeMesh.UseVisibleOnScreenNotifier))).IsTrue();

        notifiedMesh.UseVisibleOnScreenNotifier = true;
        AssertThat(notifiedMesh._GetConfigurationWarnings().Length).IsEqual(0);

        var unnotifiedMesh = CreateMesh(RopeMeshType.Tube);

        ISceneRunner.Load(unnotifiedMesh);

        AssertThat(unnotifiedMesh.IsRopeVisible).IsTrue();
        unnotifiedMesh.DrawRopeParticles(CreateParticles());
        AssertThat(unnotifiedMesh.Mesh.GetSurfaceCount()).IsGreater(0);
    }

    #region Utility

    private static int GetVertexCount(VerletRopeMesh mesh)
    {
        var surfaceArrays = ((ArrayMesh)mesh.Mesh).SurfaceGetArrays(0);
        return surfaceArrays[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length;
    }

    private static void AssertDrawn(VerletRopeMesh mesh)
    {
        AssertThat(mesh.Mesh).IsNotNull();
        AssertThat(mesh.Mesh.GetSurfaceCount()).IsGreater(0);
        AssertThat(mesh.GetAabb().Size).IsNotEqual(Vector3.Zero);
    }

    private static VerletRopeMesh CreateMesh(RopeMeshType meshType)
    {
        return new VerletRopeMesh
        {
            MeshType = meshType,
            RopeWidth = 0.1f,
            RopeSmoothing = 0.0f,
            UseVisibleOnScreenNotifier = false
        };
    }

    private static RopeParticleData CreateParticles()
    {
        var positions = new List<Vector3>();

        for (var i = 0; i < 6; i++)
        {
            positions.Add(new Vector3(0.0f, -0.25f * i, 0.0f));
        }

        return RopeParticleData.GenerateParticleData(positions);
    }

    /// <summary> Particles that all sit on the same point, which is what a rope reeled in to nothing hands the mesh. </summary>
    private static RopeParticleData CreateParticlesAtSamePoint(int count)
    {
        return RopeParticleData.GenerateParticleData(Enumerable.Repeat(Vector3.Zero, count).ToList());
    }

    #endregion
}
