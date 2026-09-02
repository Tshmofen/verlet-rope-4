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
                builder.BuildRing(localCenter, binormal, context.RopeWidth);

                if (builder.CanGenerateTriangles)
                {
                    builder.AddTubeTriangles(tangent, t - step, t);
                }

                if (i == 0 && t == 0f)
                {
                    builder.CaptureFirstRing(localCenter, tangent);
                }

                builder.CaptureEndRing(localCenter, tangent);
                builder.SwapRingBuffers();
                t += step;
            }
        }

        builder.AddTubeCaps();
        surfaceTool.Commit(context.ArrayMesh);
    }
}

file struct RopeMeshBuilder(SurfaceTool surfaceTool, int tubeSegments, float[] ringCos, float[] ringSin)
{
    // State
    private bool _hasPrevRing = false;
    private Vector3 _prevTangent = Vector3.Zero;
    private Vector3 _prevNormal = Vector3.Zero;

    // Ring references
    private Vector3[] _currentRing = new Vector3[tubeSegments];
    private Vector3[] _currentNormals = new Vector3[tubeSegments];
    private readonly Vector3[] _firstRing  = new Vector3[tubeSegments];
    private readonly Vector3[] _firstNormals = new Vector3[tubeSegments];
    private Vector3[] _prevRing = new Vector3[tubeSegments];
    private Vector3[] _prevNormals = new Vector3[tubeSegments];

    // Cap data
    private Vector3 _startCenter = Vector3.Zero;
    private Vector3 _startTangent = Vector3.Zero;
    private Vector3 _endCenter = Vector3.Zero;
    private Vector3 _endTangent = Vector3.Zero;

    public bool CanGenerateTriangles => _hasPrevRing;

    public void UpdateFrame(Vector3 tangent, out Vector3 binormal)
    {
        if (_hasPrevRing)
        {
            var rotation = new Quaternion(_prevTangent, tangent);
            var newNormal = rotation * _prevNormal;
            newNormal = (newNormal - tangent * tangent.Dot(newNormal)).Normalized();
            binormal = tangent.Cross(newNormal).Normalized();

            _prevTangent = tangent;
            _prevNormal = newNormal;
        }
        else
        {
            var up = Mathf.Abs(tangent.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
            _prevNormal = (up - tangent * tangent.Dot(up)).Normalized();
            binormal = tangent.Cross(_prevNormal).Normalized();
            _prevTangent = tangent;
        }
    }

    public void BuildRing(Vector3 center, Vector3 binormal, float ropeWidth)
    {
        for (var j = 0; j < tubeSegments; j++)
        {
            var radial = ringCos[j] * _prevNormal + ringSin[j] * binormal;
            _currentRing[j] = center + ropeWidth * radial;
            _currentNormals[j] = radial;
        }
    }

    public void AddTubeTriangles(Vector3 tangent, float u0, float u1)
    {
        if (!_hasPrevRing)
        {
            return;
        }

        surfaceTool.SetTangent(new Plane(tangent, 1.0f));

        for (var j = 0; j < tubeSegments; j++)
        {
            var next = (j + 1) % tubeSegments;
            var v0 = j / (float)tubeSegments;
            var v1 = (j + 1) / (float)tubeSegments;

            // First triangle
            surfaceTool.SetNormal(_prevNormals[j]);
            surfaceTool.SetUV(new Vector2(u0, v0));
            surfaceTool.AddVertex(_prevRing[j]);

            surfaceTool.SetNormal(_currentNormals[j]);
            surfaceTool.SetUV(new Vector2(u1, v0));
            surfaceTool.AddVertex(_currentRing[j]);

            surfaceTool.SetNormal(_currentNormals[next]);
            surfaceTool.SetUV(new Vector2(u1, v1));
            surfaceTool.AddVertex(_currentRing[next]);

            // Second triangle
            surfaceTool.SetNormal(_prevNormals[j]);
            surfaceTool.SetUV(new Vector2(u0, v0));
            surfaceTool.AddVertex(_prevRing[j]);

            surfaceTool.SetNormal(_currentNormals[next]);
            surfaceTool.SetUV(new Vector2(u1, v1));
            surfaceTool.AddVertex(_currentRing[next]);

            surfaceTool.SetNormal(_prevNormals[next]);
            surfaceTool.SetUV(new Vector2(u0, v1));
            surfaceTool.AddVertex(_prevRing[next]);
        }
    }

    public void AddTubeCaps()
    {
        if (tubeSegments < 3 || !_hasPrevRing)
        {
            return;
        }

        AddTubeCap(_startCenter, _startTangent, _firstRing, _firstNormals, true);
        AddTubeCap(_endCenter, _endTangent, _prevRing, _prevNormals, false);
    }

    #region Utils

    private void AddTubeCap(Vector3 center, Vector3 tangent, Vector3[] ring, Vector3[] normals, bool isStartCap)
    {
        var normal = isStartCap ? -tangent : tangent;
        var firstNormal = normals[0];

        for (var j = 0; j < tubeSegments; j++)
        {
            var next = (j + 1) % tubeSegments;
            var uvCenter = new Vector2(0.5f, 0.5f);
            var uvJ = new Vector2(0.5f + 0.5f * ringCos[j], 0.5f + 0.5f * ringSin[j]);
            var uvNext = new Vector2(0.5f + 0.5f * ringCos[next], 0.5f + 0.5f * ringSin[next]);

            AddTubeCapVertex(normal, firstNormal, uvCenter, center);

            if (isStartCap)
            {
                AddTubeCapVertex(normal, normals[j], uvJ, ring[j]);
                AddTubeCapVertex(normal, normals[next], uvNext, ring[next]);
            }
            else
            {
                AddTubeCapVertex(normal, normals[next], uvNext, ring[next]);
                AddTubeCapVertex(normal, normals[j], uvJ, ring[j]);
            }
        }
    }
    
    private void AddTubeCapVertex(Vector3 normal, Vector3 tangent, Vector2 uv, Vector3 position)
    {
        surfaceTool.SetNormal(normal);
        surfaceTool.SetTangent(new Plane(tangent, 1.0f));
        surfaceTool.SetUV(uv);
        surfaceTool.AddVertex(position);
    }

    public void SwapRingBuffers()
    {
        var tempRing = _prevRing;
        var tempNormals = _prevNormals;

        _prevRing = _currentRing;
        _prevNormals = _currentNormals;

        _currentRing = tempRing;
        _currentNormals = tempNormals;

        _hasPrevRing = true;
    }

    public void CaptureFirstRing(Vector3 center, Vector3 tangent)
    {
        for (var j = 0; j < tubeSegments; j++)
        {
            _firstRing[j] = _currentRing[j];
            _firstNormals[j] = _currentNormals[j];
        }

        _startCenter = center;
        _startTangent = tangent;
    }

    public void CaptureEndRing(Vector3 center, Vector3 tangent)
    {
        _endCenter = center;
        _endTangent = tangent;
    }

    #endregion
}
