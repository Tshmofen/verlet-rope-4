using Godot;
using System.Collections.Generic;

namespace VerletRope.Rendering.Tools;

public class RopeMeshTubeTool : IRopeMeshTool
{
    private static readonly float Cos5Deg = Mathf.Cos(Mathf.DegToRad(5.0f));
    private static readonly float Cos15Deg = Mathf.Cos(Mathf.DegToRad(15.0f));
    private static readonly float Cos30Deg = Mathf.Cos(Mathf.DegToRad(30.0f));

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

    #region Mesh Building Helpers

    private static void BuildRing(Vector3 center, Vector3 normal, Vector3 binormal, float ropeWidth, 
        int segments, float[] ringCos, float[] ringSin, Vector3[] ringBuffer, Vector3[] normalsBuffer)
    {
        for (var j = 0; j < segments; j++)
        {
            var radial = ringCos[j] * normal + ringSin[j] * binormal;
            ringBuffer[j] = center + ropeWidth * radial;
            normalsBuffer[j] = radial;
        }
    }

    private static void AddTubeTriangles(SurfaceTool surfaceTool, Vector3 tangent, Vector3[] prevRing,
        Vector3[] prevNormals, Vector3[] ring, Vector3[] normals, int segments, float u0, float u1)
    {
        surfaceTool.SetTangent(new Plane(tangent, 1.0f));

        for (var j = 0; j < segments; j++)
        {
            var next = (j + 1) % segments;
            var v0 = j / (float)segments;
            var v1 = (j + 1) / (float)segments;

            // First triangle
            surfaceTool.SetNormal(prevNormals[j]);
            surfaceTool.SetUV(new Vector2(u0, v0));
            surfaceTool.AddVertex(prevRing[j]);

            surfaceTool.SetNormal(normals[j]);
            surfaceTool.SetUV(new Vector2(u1, v0));
            surfaceTool.AddVertex(ring[j]);

            surfaceTool.SetNormal(normals[next]);
            surfaceTool.SetUV(new Vector2(u1, v1));
            surfaceTool.AddVertex(ring[next]);

            // Second triangle
            surfaceTool.SetNormal(prevNormals[j]);
            surfaceTool.SetUV(new Vector2(u0, v0));
            surfaceTool.AddVertex(prevRing[j]);

            surfaceTool.SetNormal(normals[next]);
            surfaceTool.SetUV(new Vector2(u1, v1));
            surfaceTool.AddVertex(ring[next]);

            surfaceTool.SetNormal(prevNormals[next]);
            surfaceTool.SetUV(new Vector2(u0, v1));
            surfaceTool.AddVertex(prevRing[next]);
        }
    }

    private static void AddCapVertex(SurfaceTool surfaceTool, Vector3 normal, Vector3 tangent, Vector2 uv, Vector3 position)
    {
        surfaceTool.SetNormal(normal);
        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
        surfaceTool.SetUV(uv);
        surfaceTool.AddVertex(position);
    }

    private static void AddCap(SurfaceTool surfaceTool, Vector3 center, Vector3 tangent, Vector3[] ring,
        Vector3[] normals, int segments, float[] ringCos, float[] ringSin, bool isStartCap)
    {
        var normal = isStartCap ? -tangent : tangent;
        var firstNormal = normals[0];

        for (var j = 0; j < segments; j++)
        {
            var next = (j + 1) % segments;
            var uvCenter = new Vector2(0.5f, 0.5f);
            var uvJ = new Vector2(0.5f + 0.5f * ringCos[j], 0.5f + 0.5f * ringSin[j]);
            var uvNext = new Vector2(0.5f + 0.5f * ringCos[next], 0.5f + 0.5f * ringSin[next]);
            
            AddCapVertex(surfaceTool, normal, firstNormal, uvCenter, center);
            
            if (isStartCap)
            {
                AddCapVertex(surfaceTool, normal, normals[j], uvJ, ring[j]);
                AddCapVertex(surfaceTool, normal, normals[next], uvNext, ring[next]);
            }
            else
            {
                AddCapVertex(surfaceTool, normal, normals[next], uvNext, ring[next]);
                AddCapVertex(surfaceTool, normal, normals[j], uvJ, ring[j]);
            }
        }
    }

    private static void UpdateFrame(Vector3 tangent, ref bool hasPrevFrame, ref Vector3 prevTangent,
        ref Vector3 prevNormal, out Vector3 binormal)
    {
        if (hasPrevFrame)
        {
            var rotation = new Quaternion(prevTangent, tangent);
            var newNormal = rotation * prevNormal;
            newNormal = (newNormal - tangent * tangent.Dot(newNormal)).Normalized();
            binormal = tangent.Cross(newNormal).Normalized();

            prevTangent = tangent;
            prevNormal = newNormal;
        }
        else
        {
            var up = Mathf.Abs(tangent.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
            prevNormal = (up - tangent * tangent.Dot(up)).Normalized();
            binormal = tangent.Cross(prevNormal).Normalized();
            prevTangent = tangent;
            hasPrevFrame = true;
        }
    }

    private static void SwapBuffers(ref Vector3[] prevRing, ref Vector3[] ring, ref Vector3[] prevNormals, ref Vector3[] normals)
    {
        var tempRing = prevRing;
        var tempNormals = prevNormals;
        prevRing = ring;
        prevNormals = normals;
        ring = tempRing;
        normals = tempNormals;
    }

    private static void CopyRingData(Vector3[] sourceRing, Vector3[] sourceNormals, Vector3[] destRing, Vector3[] destNormals, int segments)
    {
        for (var j = 0; j < segments; j++)
        {
            destRing[j] = sourceRing[j];
            destNormals[j] = sourceNormals[j];
        }
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

        // Buffers for preventing unnecessary GC pressure
        var ringBufferA = new Vector3[tubeSegments];
        var normalsBufferA = new Vector3[tubeSegments];
        var ringBufferB = new Vector3[tubeSegments];
        var normalsBufferB = new Vector3[tubeSegments];

        var firstRing = new Vector3[tubeSegments];
        var firstNormals = new Vector3[tubeSegments];
        
        var hasPrevFrame = false;
        var prevTangent = Vector3.Zero;
        var prevNormal = Vector3.Zero;

        var prevRing = ringBufferA;
        var prevNormals = normalsBufferA;
        var ring = ringBufferB;
        var normals = normalsBufferB;
        var hasPrevRing = false;

        var startCenter = Vector3.Zero;
        var startTangent = Vector3.Zero;
        var endCenter = Vector3.Zero;
        var endTangent = Vector3.Zero;

        for (var i = 0; i < particles.Count - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(context, i);
            var step = GetDrawSubdivisionStep(context, cameraPos, i);
            var t = 0.0f;

            while (t <= 1.0f + Mathf.Epsilon)
            {
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, t, out var center, out var tangent);
                var localCenter = center - globalPos;

                UpdateFrame(tangent, ref hasPrevFrame, ref prevTangent, ref prevNormal, out var binormal);
                BuildRing(localCenter, prevNormal, binormal, context.RopeWidth, tubeSegments, ringCos, ringSin, ring, normals);

                if (hasPrevRing)
                {
                    AddTubeTriangles(surfaceTool, tangent, prevRing, prevNormals, ring, normals, tubeSegments, t - step, t);
                }

                if (i == 0 && t == 0f)
                {
                    CopyRingData(ring, normals, firstRing, firstNormals, tubeSegments);
                    startCenter = localCenter;
                    startTangent = tangent;
                }

                endCenter = localCenter;
                endTangent = tangent;

                SwapBuffers(ref prevRing, ref ring, ref prevNormals, ref normals);
                hasPrevRing = true;
                t += step;
            }
        }

        if (tubeSegments >= 3)
        {
            AddCap(surfaceTool, startCenter, startTangent, firstRing, firstNormals, tubeSegments, ringCos, ringSin, true);
            AddCap(surfaceTool, endCenter, endTangent, prevRing, prevNormals, tubeSegments, ringCos, ringSin, false);
        }

        surfaceTool.Commit(context.ArrayMesh);
    }
}