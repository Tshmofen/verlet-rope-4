using Godot;
using System.Collections.Generic;
using VerletRope4.Data;

namespace VerletRope4.Rendering;

public abstract partial class VerletRopeMesh : MeshInstance3D
{
    private const string DefaultMaterialPath = "res://addons/verlet_rope_4/materials/rope_default.material";
    private const string CreationStampMeta = "verlet_rope_internal_stamp";

    private static readonly float Cos5Deg = Mathf.Cos(Mathf.DegToRad(5.0f));
    private static readonly float Cos15Deg = Mathf.Cos(Mathf.DegToRad(15.0f));
    private static readonly float Cos30Deg = Mathf.Cos(Mathf.DegToRad(30.0f));
    
    private VisibleOnScreenNotifier3D _visibleNotifier;
    private ImmediateMesh _mesh;
    private Camera3D _camera;
    private double _simulationDelta;

    protected bool IsRopeVisible { get; private set; } = true;

    [Export] public float RopeLength { get; set; } = 3.0f;
    [Export] public float RopeWidth { get; set; } = 0.07f;

    [ExportGroup("Visuals")]
    [Export] public float SubdivisionLodDistance { get; set; } = 15.0f;

    private bool _useVisibleOnScreenNotifier = true;
    [Export] public bool UseVisibleOnScreenNotifier
    {
        get => _useVisibleOnScreenNotifier; 
        set { _useVisibleOnScreenNotifier = value; UpdateConfigurationWarnings(); }
    }

    #region Util

    private (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) GetSimulationParticles(RopeParticleData particles, int index)
    {
        var p0 = (index == 0)
            ? particles[index].PositionCurrent - (particles[index].Tangent * GetAverageSegmentLength(particles.Count))
            : particles[index - 1].PositionCurrent;

        var p1 = particles[index].PositionCurrent;

        var p2 = particles[index + 1].PositionCurrent;

        var p3 = index == particles.Count - 2
            ? particles[index + 1].PositionCurrent + (particles[index + 1].Tangent * GetAverageSegmentLength(particles.Count))
            : particles[index + 2].PositionCurrent;

        return (p0, p1, p2, p3);
    }

    private float GetAverageSegmentLength(int particleCount)
    {
        return RopeLength / (particleCount - 1);
    }

    private void ResetRopeRotation()
    {
        // NOTE: rope doesn't draw from origin to attach_end_to correctly if rotated
        // calling to_local() in the drawing code is too slow
        GlobalTransform = new Transform3D(Basis.Identity, GlobalPosition);
    }

    #endregion

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

    private void DrawQuad(IReadOnlyList<Vector3> vertices, Vector3 normal, float uvx0, float uvx1)
    {
        // NOTE: still may need tangents setup for normal mapping, not tested
        // SetTangent(new Plane(-t, 0.0f));
        _mesh.SurfaceSetNormal(normal);
        _mesh.SurfaceSetUV(new Vector2(uvx0, 0.0f));
        _mesh.SurfaceAddVertex(vertices[0]);
        _mesh.SurfaceSetUV(new Vector2(uvx1, 0.0f));
        _mesh.SurfaceAddVertex(vertices[1]);
        _mesh.SurfaceSetUV(new Vector2(uvx1, 1.0f));
        _mesh.SurfaceAddVertex(vertices[2]);
        _mesh.SurfaceSetUV(new Vector2(uvx0, 0.0f));
        _mesh.SurfaceAddVertex(vertices[0]);
        _mesh.SurfaceSetUV(new Vector2(uvx1, 1.0f));
        _mesh.SurfaceAddVertex(vertices[2]);
        _mesh.SurfaceSetUV(new Vector2(uvx0, 1.0f));
        _mesh.SurfaceAddVertex(vertices[3]);
    }

    private float GetDrawSubdivisionStep(RopeParticleData particles, Vector3 cameraPosition, int particleIndex)
    {
        var camDistParticle = cameraPosition - particles[particleIndex].PositionCurrent;

        if (camDistParticle.LengthSquared() > SubdivisionLodDistance * SubdivisionLodDistance)
        {
            return 1.0f;
        }

        var tangentDots = particles[particleIndex].Tangent.Dot(particles[particleIndex + 1].Tangent);

        return
            tangentDots >= Cos5Deg ? 1.0f :
            tangentDots >= Cos15Deg ? 0.5f :
            tangentDots >= Cos30Deg ? 0.33333f :
            0.25f;
    }

    private void CalculateRopeCameraOrientation(RopeParticleData particles)
    {
        var cameraPosition = _camera?.GlobalPosition ?? Vector3.Zero;

        ref var start = ref particles[0];
        start.Tangent = (particles[1].PositionCurrent - start.PositionCurrent).Normalized();
        start.Normal = (start.PositionCurrent - cameraPosition).Normalized();
        start.Binormal = start.Normal.Cross(start.Tangent).Normalized();

        ref var end = ref particles[particles.Count - 1];
        end.Tangent = (end.PositionCurrent - particles[particles.Count - 2].PositionCurrent).Normalized();
        end.Normal = (end.PositionCurrent - cameraPosition).Normalized();
        end.Binormal = end.Normal.Cross(end.Tangent).Normalized();

        for (var i = 1; i < particles.Count - 1; i++)
        {
            ref var particle = ref particles[i];
            particle.Tangent = (particles[i + 1].PositionCurrent - particles[i - 1].PositionCurrent).Normalized();
            particle.Normal = (particles[i].PositionCurrent - cameraPosition).Normalized();
            particle.Binormal = particles[i].Normal.Cross(particles[i].Tangent).Normalized();
        }
    }

    private void DrawCurve(RopeParticleData particles)
    {
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        var cameraPosition = _camera?.GlobalPosition ?? Vector3.Zero;

        for (var i = 0; i < particles.Count - 1; i++)
        {
            var (p0, p1, p2, p3) = GetSimulationParticles(particles, i);
            var step = GetDrawSubdivisionStep(particles, cameraPosition, i);
            var t = 0.0f;

            while (t <= 1.0f)
            {
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, t, out var currentPosition, out var currentTangent);
                CatmullInterpolate(p0, p1, p2, p3, 0.0f, Mathf.Min(t + step, 1.0f), out var nextPosition, out var nextTangent);

                var currentNormal = (currentPosition - cameraPosition).Normalized();
                var currentBinormal = currentNormal.Cross(currentTangent).Normalized();
                currentPosition -= GlobalPosition;

                var nextNormal = (nextPosition - cameraPosition).Normalized();
                var nextBinormal = nextNormal.Cross(nextTangent).Normalized();
                nextPosition -= GlobalPosition;

                var vs = new[]
                {
                    currentPosition - (currentBinormal * RopeWidth),
                    nextPosition - (nextBinormal * RopeWidth),
                    nextPosition + (nextBinormal * RopeWidth),
                    currentPosition + (currentBinormal * RopeWidth)
                };

                DrawQuad(vs, -currentBinormal, t, t + step);
                t += step;
            }
        }

        _mesh.SurfaceEnd();
    }

    public void DrawRopeParticles(RopeParticleData particles)
    {
        if (!IsRopeVisible)
        {
            return;
        }

        #if TOOLS
        _camera = Engine.IsEditorHint()
            ? EditorInterface.Singleton.GetEditorViewport3D().GetCamera3D()
            : GetViewport().GetCamera3D();
        #else
        _camera = GetViewport().GetCamera3D();
        #endif

        CalculateRopeCameraOrientation(particles);
        ResetRopeRotation();
        DrawCurve(particles);

        if (_visibleNotifier != null)
        {
            _visibleNotifier.Aabb = GetAabb();
        }
    }

    public void DrawRopeDebugParticles(RopeParticleData particles)
    {        
        if (!IsRopeVisible)
        {
            return;
        }

        const float debugParticleLength = 0.3f;
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        for (var i = 0; i < particles.Count; i++)
        {
            var particle = particles[i];
            var localPosition = particle.PositionCurrent - GlobalPosition;

            _mesh.SurfaceAddVertex(localPosition);
            _mesh.SurfaceAddVertex(localPosition + (debugParticleLength * particle.Tangent));

            _mesh.SurfaceAddVertex(localPosition);
            _mesh.SurfaceAddVertex(localPosition + (debugParticleLength * particle.Normal));

            _mesh.SurfaceAddVertex(localPosition);
            _mesh.SurfaceAddVertex(localPosition + (debugParticleLength * particle.Binormal));
        }

        _mesh.SurfaceEnd();
    }

    public override string[] _GetConfigurationWarnings()
    {
        return !UseVisibleOnScreenNotifier
            ? [$"Consider checking '{nameof(UseVisibleOnScreenNotifier)}' to disable rope visuals when it's not on screen for increased performance."]
            : [];
    }

    public override void _Ready()
    {
        _mesh = Mesh as ImmediateMesh;
        if (_mesh == null || _mesh.GetMeta(CreationStampMeta, 0ul).AsUInt64() != GetInstanceId())
        {
            Mesh = _mesh = new ImmediateMesh();
            _mesh.SetMeta(CreationStampMeta, GetInstanceId());
            _mesh.ResourceLocalToScene = true;
        }

        if (UseVisibleOnScreenNotifier && !Engine.IsEditorHint())
        {
            AddChild(_visibleNotifier = new VisibleOnScreenNotifier3D());
            _visibleNotifier.ScreenEntered += () => IsRopeVisible = true;
            _visibleNotifier.ScreenExited += () => IsRopeVisible = false;
            IsRopeVisible = _visibleNotifier.IsOnScreen();
        }

        MaterialOverride ??= GD.Load<StandardMaterial3D>(DefaultMaterialPath);
    }
}
