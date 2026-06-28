using Godot;
using System.Collections.Generic;

namespace VerletRope.Rendering.Tools;

public class RopeMeshRibbonTool : IRopeMeshTool
{
    private static readonly float Cos5Deg = Mathf.Cos(Mathf.DegToRad(5.0f));
    private static readonly float Cos15Deg = Mathf.Cos(Mathf.DegToRad(15.0f));
    private static readonly float Cos30Deg = Mathf.Cos(Mathf.DegToRad(30.0f));

    #region Utils

    private static float GetDrawSubdivisionStep(MeshRenderContext context, Vector3 cameraPosition, int particleIndex)
    {
        var camDistParticle = cameraPosition - context.Particles[particleIndex].PositionCurrent;
        if (camDistParticle.LengthSquared() > context.SubdivisionLodDistance * context.SubdivisionLodDistance)
        {
            return 1.0f;
        }

        var tangentDots = context.Particles[particleIndex].Tangent.Dot(context.Particles[particleIndex + 1].Tangent);
        return
            tangentDots >= Cos5Deg ? 1.0f :
            tangentDots >= Cos15Deg ? 0.5f :
            tangentDots >= Cos30Deg ? 0.33333f :
            0.25f;
    }

    private static void CatmullInterpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float tension, float t, out Vector3 point, out Vector3 tangent)
    {
        // Fast catmull spline
        var tSqr = t * t;
        var tCube = tSqr * t;

        var m1 = (1f - tension) / 2f * (p2 - p0);
        var m2 = (1f - tension) / 2f * (p3 - p1);

        var a = (2f * (p1 - p2)) + m1 + m2;
        var b = (-3f * (p1 - p2)) - (2f * m1) - m2;

        point = (a * tCube) + (b * tSqr) + (m1 * t) + p1;
        tangent = ((3f * a * tSqr) + (2f * b * t) + m1).Normalized();
    }

    private static void DrawQuad(MeshRenderContext context, IReadOnlyList<Vector3> vertices, float uvx0, float uvx1)
    {
        var surfaceTool = context.SurfaceTool;

        // Triangle 1
        surfaceTool.SetUV(new Vector2(uvx0, 0.0f));
        surfaceTool.AddVertex(vertices[0]);

        surfaceTool.SetUV(new Vector2(uvx1, 0.0f));
        surfaceTool.AddVertex(vertices[1]);

        surfaceTool.SetUV(new Vector2(uvx1, 1.0f));
        surfaceTool.AddVertex(vertices[2]);

        // Triangle 2
        surfaceTool.SetUV(new Vector2(uvx0, 0.0f));
        surfaceTool.AddVertex(vertices[0]);

        surfaceTool.SetUV(new Vector2(uvx1, 1.0f));
        surfaceTool.AddVertex(vertices[2]);

        surfaceTool.SetUV(new Vector2(uvx0, 1.0f));
        surfaceTool.AddVertex(vertices[3]);
    }
    
    private (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) GetSimulationParticles(MeshRenderContext context, int index)
    {
        var particles = context.Particles;

        var p0 = (index == 0)
            ? particles[index].PositionCurrent - (particles[index].Tangent * context.AverageSegmentLength)
            : particles[index - 1].PositionCurrent;

        var p1 = particles[index].PositionCurrent;

        var p2 = particles[index + 1].PositionCurrent;

        var p3 = index == particles.Count - 2
            ? particles[index + 1].PositionCurrent + (particles[index + 1].Tangent * context.AverageSegmentLength)
            : particles[index + 2].PositionCurrent;

        return (p0, p1, p2, p3);
    }

    #endregion

    public void DrawParticles(MeshRenderContext context)
    {
        var surfaceTool = context.SurfaceTool;
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var cameraPosition = context.CurrentCamera?.GlobalPosition ?? Vector3.Zero;

        for (var i = 0; i < context.Particles.Count - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(context, i);
            var step = GetDrawSubdivisionStep(context, cameraPosition, i);
            var t = 0.0f;

            while (t <= 1.0f)
            {
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, t, out var currentPosition, out var currentTangent);
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, Mathf.Min(t + step, 1.0f), out var nextPosition, out var nextTangent);

                var currentNormal = (currentPosition - cameraPosition).Normalized();
                var currentBinormal = currentNormal.Cross(currentTangent).Normalized();
                currentPosition -= context.GlobalPosition;

                var nextNormal = (nextPosition - cameraPosition).Normalized();
                var nextBinormal = nextNormal.Cross(nextTangent).Normalized();
                nextPosition -= context.GlobalPosition;

                var vs = new[]
                {
                    currentPosition - (currentBinormal * context.RopeWidth),
                    nextPosition - (nextBinormal * context.RopeWidth),
                    nextPosition + (nextBinormal * context.RopeWidth),
                    currentPosition + (currentBinormal * context.RopeWidth)
                };

                DrawQuad(context, vs, t, t + step);
                t += step;
            }
        }

        surfaceTool.GenerateNormals();
        surfaceTool.GenerateTangents();
        surfaceTool.Commit(context.ArrayMesh);
    }
}