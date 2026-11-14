using GdUnit4;
using Godot;
using System.Collections.Generic;
using VerletRope4.Data;
using VerletRope4.Rendering;
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
}
