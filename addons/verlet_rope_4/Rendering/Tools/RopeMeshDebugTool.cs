using Godot;

namespace VerletRope.Rendering.Tools;

public class RopeMeshDebugTool : IRopeMeshTool
{
    private const float DebugParticleLength = 0.3f;

    public void DrawParticles(MeshRenderContext context)
    {

        var surfaceTool = context.SurfaceTool;
        surfaceTool.Clear();
        surfaceTool.Begin(Mesh.PrimitiveType.Lines);

        for (var i = 0; i < context.Particles.Count; i++)
        {
            var particle = context.Particles[i];
            var localPosition = particle.PositionCurrent - context.GlobalPosition;

            surfaceTool.AddVertex(localPosition);
            surfaceTool.AddVertex(localPosition + DebugParticleLength * particle.Tangent);

            surfaceTool.AddVertex(localPosition);
            surfaceTool.AddVertex(localPosition + DebugParticleLength * particle.Normal);

            surfaceTool.AddVertex(localPosition);
            surfaceTool.AddVertex(localPosition + DebugParticleLength * particle.Binormal);
        }

        surfaceTool.Commit(context.ArrayMesh);
    }
}
