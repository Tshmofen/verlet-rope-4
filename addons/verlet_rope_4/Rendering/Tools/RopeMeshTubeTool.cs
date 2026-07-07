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
        var cameraPos = context.CurrentCamera?.GlobalPosition ?? Vector3.Zero;
        var globalPos = context.GlobalPosition;

        var hasPrevFrame = false;
        var prevTangent = Vector3.Zero;
        var prevNormal = Vector3.Zero;
        Vector3[] prevRing = null;
        Vector3[] prevNormals = null;

        var startCenter = Vector3.Zero;
        var startTangent = Vector3.Zero;
        Vector3[] firstRing = null;

        var endCenter = Vector3.Zero;
        var endTangent = Vector3.Zero;
        Vector3[] lastRing = null;

        // Generate tubes
        for (var i = 0; i < particles.Count - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(context, i);
            var step = GetDrawSubdivisionStep(context, cameraPos, i);
            var t = 0.0f;

            while (t <= 1.0f)
            {
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, t, out var center, out var tangent);
                var localCenter = center - globalPos;
                Vector3 prevBinormal;

                if (hasPrevFrame)
                {
                    var rotation = new Quaternion(prevTangent, tangent);
                    var newNormal = rotation * prevNormal;
                    newNormal = (newNormal - tangent * tangent.Dot(newNormal)).Normalized();
                    var newBinormal = tangent.Cross(newNormal).Normalized();
                    prevTangent = tangent;
                    prevNormal = newNormal;
                    prevBinormal = newBinormal;
                }
                else
                {
                    var up = Mathf.Abs(tangent.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
                    var normal = (up - tangent * tangent.Dot(up)).Normalized();
                    var binormal = tangent.Cross(normal).Normalized();
                    prevTangent = tangent;
                    prevNormal = normal;
                    prevBinormal = binormal;
                    hasPrevFrame = true;
                }

                // Build rings
                var ring = new Vector3[TubeSegments];
                var normals = new Vector3[TubeSegments];
                for (var j = 0; j < TubeSegments; j++)
                {
                    var angle = j * Mathf.Tau / TubeSegments;
                    var radial = Mathf.Cos(angle) * prevNormal + Mathf.Sin(angle) * prevBinormal;
                    ring[j] = localCenter + context.RopeWidth * radial;
                    normals[j] = radial; // outward normal
                }

                // Render triangles
                if (prevRing != null)
                {
                    for (var j = 0; j < TubeSegments; j++)
                    {
                        var next = (j + 1) % TubeSegments;
                        var u0 = t - step;
                        var v0 = j / (float)TubeSegments;
                        var v1 = next / (float)TubeSegments;

                        // --- First triangle (prev[j], curr[j], curr[next]) ---
                        surfaceTool.SetNormal(prevNormals[j]);
                        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
                        surfaceTool.SetUV(new Vector2(u0, v0));
                        surfaceTool.AddVertex(prevRing[j]);

                        surfaceTool.SetNormal(normals[j]);
                        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
                        surfaceTool.SetUV(new Vector2(t, v0));
                        surfaceTool.AddVertex(ring[j]);

                        surfaceTool.SetNormal(normals[next]);
                        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
                        surfaceTool.SetUV(new Vector2(t, v1));
                        surfaceTool.AddVertex(ring[next]);

                        // --- Second triangle (prev[j], curr[next], prev[next]) ---
                        surfaceTool.SetNormal(prevNormals[j]);
                        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
                        surfaceTool.SetUV(new Vector2(u0, v0));
                        surfaceTool.AddVertex(prevRing[j]);

                        surfaceTool.SetNormal(normals[next]);
                        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
                        surfaceTool.SetUV(new Vector2(t, v1));
                        surfaceTool.AddVertex(ring[next]);

                        surfaceTool.SetNormal(prevNormals[next]);
                        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
                        surfaceTool.SetUV(new Vector2(u0, v1));
                        surfaceTool.AddVertex(prevRing[next]);
                    }
                }
                
                // Preserve ends for further render
                if (i == 0 && t == 0f)
                {
                    startCenter = localCenter;
                    startTangent = tangent;
                    firstRing = ring;
                }
                else
                {
                    endCenter = localCenter;
                    endTangent = tangent;
                    lastRing = ring;
                }

                prevRing = ring;
                prevNormals = normals;
                t += step;
            }
        }

        // Generate closing caps
        if (firstRing != null && lastRing != null && TubeSegments >= 3)
        {
            for (var j = 0; j < TubeSegments; j++)
            {
                var next = (j + 1) % TubeSegments;
                
                surfaceTool.SetNormal(-startTangent);
                surfaceTool.SetTangent(new Plane(Vector3.Zero, 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f, 0.5f));
                surfaceTool.AddVertex(startCenter);
                
                var radialJ = (firstRing[j] - startCenter).Normalized();
                surfaceTool.SetNormal(-startTangent);
                surfaceTool.SetTangent(new Plane(radialJ, 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * Mathf.Cos(j * Mathf.Tau / TubeSegments), 0.5f + 0.5f * Mathf.Sin(j * Mathf.Tau / TubeSegments)));
                surfaceTool.AddVertex(firstRing[j]);
                
                var radialNext = (firstRing[next] - startCenter).Normalized();
                surfaceTool.SetNormal(-startTangent);
                surfaceTool.SetTangent(new Plane(radialNext, 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * Mathf.Cos(next * Mathf.Tau / TubeSegments), 0.5f + 0.5f * Mathf.Sin(next * Mathf.Tau / TubeSegments)));
                surfaceTool.AddVertex(firstRing[next]);
            }
            
            for (var j = 0; j < TubeSegments; j++)
            {
                var next = (j + 1) % TubeSegments;
                
                surfaceTool.SetNormal(endTangent);
                surfaceTool.SetTangent(new Plane(Vector3.Zero, 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f, 0.5f));
                surfaceTool.AddVertex(endCenter);
                
                var radialJ = (lastRing[j] - endCenter).Normalized();
                surfaceTool.SetNormal(endTangent);
                surfaceTool.SetTangent(new Plane(radialJ, 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * Mathf.Cos(j * Mathf.Tau / TubeSegments), 0.5f + 0.5f * Mathf.Sin(j * Mathf.Tau / TubeSegments)));
                surfaceTool.AddVertex(lastRing[j]);
                
                var radialNext = (lastRing[next] - endCenter).Normalized();
                surfaceTool.SetNormal(endTangent);
                surfaceTool.SetTangent(new Plane(radialNext, 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * Mathf.Cos(next * Mathf.Tau / TubeSegments), 0.5f + 0.5f * Mathf.Sin(next * Mathf.Tau / TubeSegments)));
                surfaceTool.AddVertex(lastRing[next]);
            }
        }

        context.SurfaceTool.Commit(context.ArrayMesh);
    }
}