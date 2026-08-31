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

    public void DrawParticles(MeshRenderContext context)
    {
        var surfaceTool = context.SurfaceTool;
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var tubeSegments = context.TubeSegments;
        var (ringCos, ringSin) = GetRingTrig(tubeSegments);

        var builder = new RopeMeshBuilder(surfaceTool, tubeSegments, ringCos, ringSin);
        var particles = context.Particles;
        var cameraPos = context.CurrentCamera?.GlobalPosition ?? Vector3.Zero;
        var globalPos = context.GlobalPosition;

        for (var i = 0; i < particles.Count - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(context, i);
            var step = GetDrawSubdivisionStep(context, cameraPos, i);
            var t = 0.0f;

            while (t <= 1.0f + Mathf.Epsilon)
            {
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, t, out var center, out var tangent);
                var localCenter = center - globalPos;

                builder.UpdateFrame(tangent, out var binormal);
                builder.BuildRing(localCenter, builder.PrevNormal, binormal, context.RopeWidth);

                if (builder.HasPrevRing)
                {
                    builder.AddTubeTriangles(tangent, t - step, t);
                }

                if (i == 0 && t == 0f)
                {
                    builder.CaptureFirstRing(localCenter, tangent);
                }

                builder.CaptureEnd(localCenter, tangent);
                builder.SwapBuffers();

                t += step;
            }
        }

        if (tubeSegments >= 3)
        {
            builder.AddCap(builder.StartCenter, builder.StartTangent, builder.FirstRing, builder.FirstNormals, true);
            builder.AddCap(builder.EndCenter, builder.EndTangent, builder.PrevRing, builder.PrevNormals, false);
        }

        surfaceTool.Commit(context.ArrayMesh);
    }
}

file struct RopeMeshBuilder(SurfaceTool surfaceTool, int tubeSegments, float[] ringCos, float[] ringSin)
{
    // Frame state
    private bool _hasPrevFrame = false;
    private Vector3 _prevTangent = Vector3.Zero;
    public Vector3 PrevNormal { get; private set; } = Vector3.Zero;

    // Active ring references
    private Vector3[] _currentRing = new Vector3[tubeSegments];
    private Vector3[] _currentNormals = new Vector3[tubeSegments];
    public Vector3[] FirstRing { get; } = new Vector3[tubeSegments];
    public Vector3[] FirstNormals { get; } = new Vector3[tubeSegments];
    public Vector3[] PrevRing { get; private set; } = new Vector3[tubeSegments];
    public Vector3[] PrevNormals { get; private set; } = new Vector3[tubeSegments];
    public bool HasPrevRing { get; private set; } = false;

    // Cap data
    public Vector3 StartCenter { get; private set; } = Vector3.Zero;
    public Vector3 StartTangent { get; private set; } = Vector3.Zero;
    public Vector3 EndCenter { get; private set; } = Vector3.Zero;
    public Vector3 EndTangent { get; private set; } = Vector3.Zero;

    public void UpdateFrame(Vector3 tangent, out Vector3 binormal)
    {
        if (_hasPrevFrame)
        {
            var rotation = new Quaternion(_prevTangent, tangent);
            var newNormal = rotation * PrevNormal;
            newNormal = (newNormal - tangent * tangent.Dot(newNormal)).Normalized();
            binormal = tangent.Cross(newNormal).Normalized();

            _prevTangent = tangent;
            PrevNormal = newNormal;
        }
        else
        {
            var up = Mathf.Abs(tangent.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
            PrevNormal = (up - tangent * tangent.Dot(up)).Normalized();
            binormal = tangent.Cross(PrevNormal).Normalized();
            _prevTangent = tangent;
            _hasPrevFrame = true;
        }
    }

    public void BuildRing(Vector3 center, Vector3 normal, Vector3 binormal, float ropeWidth)
    {
        for (var j = 0; j < tubeSegments; j++)
        {
            var radial = ringCos[j] * normal + ringSin[j] * binormal;
            _currentRing[j] = center + ropeWidth * radial;
            _currentNormals[j] = radial;
        }
    }

    public void AddTubeTriangles(Vector3 tangent, float u0, float u1)
    {
        surfaceTool.SetTangent(new Plane(tangent, 1.0f));

        for (var j = 0; j < tubeSegments; j++)
        {
            var next = (j + 1) % tubeSegments;
            var v0 = j / (float)tubeSegments;
            var v1 = (j + 1) / (float)tubeSegments;

            // First triangle
            surfaceTool.SetNormal(PrevNormals[j]);
            surfaceTool.SetUV(new Vector2(u0, v0));
            surfaceTool.AddVertex(PrevRing[j]);

            surfaceTool.SetNormal(_currentNormals[j]);
            surfaceTool.SetUV(new Vector2(u1, v0));
            surfaceTool.AddVertex(_currentRing[j]);

            surfaceTool.SetNormal(_currentNormals[next]);
            surfaceTool.SetUV(new Vector2(u1, v1));
            surfaceTool.AddVertex(_currentRing[next]);

            // Second triangle
            surfaceTool.SetNormal(PrevNormals[j]);
            surfaceTool.SetUV(new Vector2(u0, v0));
            surfaceTool.AddVertex(PrevRing[j]);

            surfaceTool.SetNormal(_currentNormals[next]);
            surfaceTool.SetUV(new Vector2(u1, v1));
            surfaceTool.AddVertex(_currentRing[next]);

            surfaceTool.SetNormal(PrevNormals[next]);
            surfaceTool.SetUV(new Vector2(u0, v1));
            surfaceTool.AddVertex(PrevRing[next]);
        }
    }

    public void AddCap(Vector3 center, Vector3 tangent, Vector3[] ring, Vector3[] normals, bool isStartCap)
    {
        var normal = isStartCap ? -tangent : tangent;
        var firstNormal = normals[0];

        for (var j = 0; j < tubeSegments; j++)
        {
            var next = (j + 1) % tubeSegments;
            var uvCenter = new Vector2(0.5f, 0.5f);
            var uvJ = new Vector2(0.5f + 0.5f * ringCos[j], 0.5f + 0.5f * ringSin[j]);
            var uvNext = new Vector2(0.5f + 0.5f * ringCos[next], 0.5f + 0.5f * ringSin[next]);

            AddCapVertex(normal, firstNormal, uvCenter, center);

            if (isStartCap)
            {
                AddCapVertex(normal, normals[j], uvJ, ring[j]);
                AddCapVertex(normal, normals[next], uvNext, ring[next]);
            }
            else
            {
                AddCapVertex(normal, normals[next], uvNext, ring[next]);
                AddCapVertex(normal, normals[j], uvJ, ring[j]);
            }
        }
    }

    private void AddCapVertex(Vector3 normal, Vector3 tangent, Vector2 uv, Vector3 position)
    {
        surfaceTool.SetNormal(normal);
        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
        surfaceTool.SetUV(uv);
        surfaceTool.AddVertex(position);
    }

    public void SwapBuffers()
    {
        var tempRing = PrevRing;
        var tempNormals = PrevNormals;

        PrevRing = _currentRing;
        PrevNormals = _currentNormals;

        _currentRing = tempRing;
        _currentNormals = tempNormals;

        HasPrevRing = true;
    }

    public void CaptureFirstRing(Vector3 center, Vector3 tangent)
    {
        for (var j = 0; j < tubeSegments; j++)
        {
            FirstRing[j] = _currentRing[j];
            FirstNormals[j] = _currentNormals[j];
        }

        StartCenter = center;
        StartTangent = tangent;
    }

    public void CaptureEnd(Vector3 center, Vector3 tangent)
    {
        EndCenter = center;
        EndTangent = tangent;
    }
}
