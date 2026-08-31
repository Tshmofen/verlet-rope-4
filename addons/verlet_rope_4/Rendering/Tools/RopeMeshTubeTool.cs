using Godot;
using System.Collections.Generic;

namespace VerletRope.Rendering.Tools;

public class RopeMeshTubeTool : IRopeMeshTool
{
    // Threshold cosines for adaptive subdivision
    private static readonly float Cos5Deg = Mathf.Cos(Mathf.DegToRad(5.0f));
    private static readonly float Cos15Deg = Mathf.Cos(Mathf.DegToRad(15.0f));
    private static readonly float Cos30Deg = Mathf.Cos(Mathf.DegToRad(30.0f));

    // Cache for precomputed ring angles (cos/sin)
    private static readonly Dictionary<int, (float[] Cos, float[] Sin)> RingTrigCache = new();
    private static readonly object CacheLock = new();

    #region Utility Methods

    private static (float[] Cos, float[] Sin) GetRingTrig(int segments)
    {
        if (RingTrigCache.TryGetValue(segments, out var trig))
        {
            return trig;
        }

        lock (CacheLock)
        {
            if (RingTrigCache.TryGetValue(segments, out trig))
            {
                return trig;
            }

            var cos = new float[segments];
            var sin = new float[segments];

            for (var j = 0; j < segments; j++)
            {
                var angle = j * Mathf.Tau / segments;
                cos[j] = Mathf.Cos(angle);
                sin[j] = Mathf.Sin(angle);
            }

            RingTrigCache[segments] = trig = (cos, sin);
            return trig;
        }
    }

    private static float GetDrawSubdivisionStep(MeshRenderContext context, Vector3 cameraPosition, int particleIndex)
    {
        var particles = context.Particles;
        var camDistParticle = cameraPosition - particles[particleIndex].PositionRender;
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
            ? particles[index].PositionRender - (particles[index].Tangent * context.AverageSegmentLength)
            : particles[index - 1].PositionRender;
        var p1 = particles[index].PositionRender;
        var p2 = particles[index + 1].PositionRender;
        var p3 = (index == particles.Count - 2)
            ? particles[index + 1].PositionRender + (particles[index + 1].Tangent * context.AverageSegmentLength)
            : particles[index + 2].PositionRender;
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

        var tubeSegments = context.TubeSegments;
        var (ringCos, ringSin) = GetRingTrig(tubeSegments);

        // Pre-allocate two sets of buffers for current and previous rings
        var ringBufferA = new Vector3[tubeSegments];
        var normalsBufferA = new Vector3[tubeSegments];
        var ringBufferB = new Vector3[tubeSegments];
        var normalsBufferB = new Vector3[tubeSegments];

        // Buffer for storing the first ring (separate from rotating buffers)
        var firstRing = new Vector3[tubeSegments];
        var firstNormals = new Vector3[tubeSegments];

        var hasPrevFrame = false;
        var prevTangent = Vector3.Zero;
        var prevNormal = Vector3.Zero;

        // Initialize buffers: previous points to A, current to B
        var prevRing = ringBufferA;
        var prevNormals = normalsBufferA;
        var ring = ringBufferB;
        var normals = normalsBufferB;
        var hasPrevRing = false;

        var startCenter = Vector3.Zero;
        var startTangent = Vector3.Zero;
        var endCenter = Vector3.Zero;
        var endTangent = Vector3.Zero;

        // Generate tubes
        for (var i = 0; i < particles.Count - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(context, i);
            var step = GetDrawSubdivisionStep(context, cameraPos, i);
            var t = 0.0f;

            while (t <= 1.0f + Mathf.Epsilon)
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

                // Fill current ring buffer
                for (var j = 0; j < tubeSegments; j++)
                {
                    var radial = ringCos[j] * prevNormal + ringSin[j] * prevBinormal;
                    ring[j] = localCenter + context.RopeWidth * radial;
                    normals[j] = radial;
                }

                // Render triangles if we have a valid previous ring
                if (hasPrevRing)
                {
                    surfaceTool.SetTangent(new Plane(tangent, 1.0f));
                    for (var j = 0; j < tubeSegments; j++)
                    {
                        var next = (j + 1) % tubeSegments;
                        var u0 = t - step;
                        var v0 = j / (float)tubeSegments;
                        var v1 = (j + 1) / (float)tubeSegments;

                        // First triangle
                        surfaceTool.SetNormal(prevNormals[j]);
                        surfaceTool.SetUV(new Vector2(u0, v0));
                        surfaceTool.AddVertex(prevRing[j]);

                        surfaceTool.SetNormal(normals[j]);
                        surfaceTool.SetUV(new Vector2(t, v0));
                        surfaceTool.AddVertex(ring[j]);

                        surfaceTool.SetNormal(normals[next]);
                        surfaceTool.SetUV(new Vector2(t, v1));
                        surfaceTool.AddVertex(ring[next]);

                        // Second triangle
                        surfaceTool.SetNormal(prevNormals[j]);
                        surfaceTool.SetUV(new Vector2(u0, v0));
                        surfaceTool.AddVertex(prevRing[j]);

                        surfaceTool.SetNormal(normals[next]);
                        surfaceTool.SetUV(new Vector2(t, v1));
                        surfaceTool.AddVertex(ring[next]);

                        surfaceTool.SetNormal(prevNormals[next]);
                        surfaceTool.SetUV(new Vector2(u0, v1));
                        surfaceTool.AddVertex(prevRing[next]);
                    }
                }

                // Capture the first ring data (for start cap)
                if (i == 0 && t == 0f)
                {
                    for (var j = 0; j < tubeSegments; j++)
                    {
                        firstRing[j] = ring[j];
                        firstNormals[j] = normals[j];
                    }
                    startCenter = localCenter;
                    startTangent = tangent;
                }

                // Always update the end values; after the loop they will hold the last ring's data
                endCenter = localCenter;
                endTangent = tangent;

                // Swap buffers: current becomes previous, previous becomes current
                var tempRing = prevRing;
                var tempNormals = prevNormals;
                prevRing = ring;
                prevNormals = normals;
                ring = tempRing;
                normals = tempNormals;

                hasPrevRing = true;
                t += step;
            }
        }

        // Generate closing caps
        if (tubeSegments >= 3)
        {
            // Start cap
            for (var j = 0; j < tubeSegments; j++)
            {
                var next = (j + 1) % tubeSegments;

                surfaceTool.SetNormal(-startTangent);
                surfaceTool.SetTangent(new Plane(firstNormals[0], 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f, 0.5f));
                surfaceTool.AddVertex(startCenter);

                surfaceTool.SetNormal(-startTangent);
                surfaceTool.SetTangent(new Plane(firstNormals[j], 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * ringCos[j], 0.5f + 0.5f * ringSin[j]));
                surfaceTool.AddVertex(firstRing[j]);

                surfaceTool.SetNormal(-startTangent);
                surfaceTool.SetTangent(new Plane(firstNormals[next], 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * ringCos[next], 0.5f + 0.5f * ringSin[next]));
                surfaceTool.AddVertex(firstRing[next]);
            }

            // End cap (using the final prevRing/prevNormals)
            for (var j = 0; j < tubeSegments; j++)
            {
                var next = (j + 1) % tubeSegments;

                surfaceTool.SetNormal(endTangent);
                surfaceTool.SetTangent(new Plane(prevNormals[0], 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f, 0.5f));
                surfaceTool.AddVertex(endCenter);

                surfaceTool.SetNormal(endTangent);
                surfaceTool.SetTangent(new Plane(prevNormals[j], 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * ringCos[j], 0.5f + 0.5f * ringSin[j]));
                surfaceTool.AddVertex(prevRing[j]);

                surfaceTool.SetNormal(endTangent);
                surfaceTool.SetTangent(new Plane(prevNormals[next], 1.0f));
                surfaceTool.SetUV(new Vector2(0.5f + 0.5f * ringCos[next], 0.5f + 0.5f * ringSin[next]));
                surfaceTool.AddVertex(prevRing[next]);
            }
        }

        context.SurfaceTool.Commit(context.ArrayMesh);
    }
}
