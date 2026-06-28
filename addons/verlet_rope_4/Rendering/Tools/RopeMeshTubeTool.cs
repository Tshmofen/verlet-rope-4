using Godot;

namespace VerletRope.Rendering.Tools;

public class RopeMeshTubeTool : IRopeMeshTool
{
    private const int TubeSegments = 6;

    private static readonly float Cos5Deg = Mathf.Cos(Mathf.DegToRad(5.0f));
    private static readonly float Cos15Deg = Mathf.Cos(Mathf.DegToRad(15.0f));
    private static readonly float Cos30Deg = Mathf.Cos(Mathf.DegToRad(30.0f));

    #region Utility Methods

    private static float GetDrawSubdivisionStep(MeshRenderContext context, Vector3 cameraPosition, int particleIndex)
    {
        var particles = context.Particles;
        var camDistParticle = cameraPosition - particles[particleIndex].PositionCurrent;
        if (camDistParticle.LengthSquared() > context.SubdivisionLodDistance * context.SubdivisionLodDistance)
        {
            return 1.0f;
        }

        var tangentDots = particles[particleIndex].Tangent.Dot(particles[particleIndex + 1].Tangent);
        return tangentDots >= Cos5Deg ? 1.0f :
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

    private static (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) GetSimulationParticles(MeshRenderContext context, int index)
    {
        var particles = context.Particles;
        var p0 = (index == 0)
            ? particles[index].PositionCurrent - (particles[index].Tangent * context.AverageSegmentLength)
            : particles[index - 1].PositionCurrent;
        var p1 = particles[index].PositionCurrent;
        var p2 = particles[index + 1].PositionCurrent;
        var p3 = (index == particles.Count - 2)
            ? particles[index + 1].PositionCurrent + (particles[index + 1].Tangent * context.AverageSegmentLength)
            : particles[index + 2].PositionCurrent;
        return (p0, p1, p2, p3);
    }

    #endregion

    public void DrawParticles(MeshRenderContext context)
    {
        var surfaceTool = context.SurfaceTool;
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var particles = context.Particles;
        var radius = context.RopeWidth * 0.5f;
        var cameraPos = context.CurrentCamera?.GlobalPosition ?? Vector3.Zero;
        var globalPos = context.GlobalPosition;
        
        for (var i = 0; i < particles.Count - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(context, i);
            var step = GetDrawSubdivisionStep(context, cameraPos, i);
            var t = 0.0f;
            
            Vector3[] previousRing = null;
            while (t <= 1.0f)
            {
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, t, out var center, out var tangent);
                var localCenter = center -= globalPos;

                // Build a stable orthonormal basis (using world up)
                var up = Mathf.Abs(tangent.Dot(Vector3.Up)) > 0.99f
                    ? Vector3.Right
                    : Vector3.Up;
                
                var normal = (up - tangent * tangent.Dot(up)).Normalized();
                var binormal = tangent.Cross(normal).Normalized();
                
                var ring = new Vector3[TubeSegments];
                for (var j = 0; j < TubeSegments; j++)
                {
                    var angle = j * Mathf.Tau / TubeSegments;
                    var offset = radius * (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal);
                    ring[j] = localCenter + offset;
                }

                // Triangulate between previous ring and current ring
                if (previousRing != null)
                {
                    for (var j = 0; j < TubeSegments; j++)
                    {
                        var next = (j + 1) % TubeSegments;
                        // Two triangles: (prev[j], curr[j], curr[next]) and (prev[j], curr[next], prev[next])
                        // UVs: U = t (current), V = j/sides
                        var u0 = t - step; // previous t
                        var u1 = t;
                        var v0 = j / (float)TubeSegments;
                        var v1 = next / (float)TubeSegments;

                        // Triangle 1
                        surfaceTool.SetUV(new Vector2(u0, v0));
                        surfaceTool.AddVertex(previousRing[j]);
                        surfaceTool.SetUV(new Vector2(u1, v0));
                        surfaceTool.AddVertex(ring[j]);
                        surfaceTool.SetUV(new Vector2(u1, v1));
                        surfaceTool.AddVertex(ring[next]);

                        // Triangle 2
                        surfaceTool.SetUV(new Vector2(u0, v0));
                        surfaceTool.AddVertex(previousRing[j]);
                        surfaceTool.SetUV(new Vector2(u1, v1));
                        surfaceTool.AddVertex(ring[next]);
                        surfaceTool.SetUV(new Vector2(u0, v1));
                        surfaceTool.AddVertex(previousRing[next]);
                    }
                }

                previousRing = ring;
                t += step;
            }
        }
        
        surfaceTool.GenerateNormals();
        surfaceTool.GenerateTangents();
        surfaceTool.Commit(context.ArrayMesh);
    }
}