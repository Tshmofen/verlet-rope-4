using Godot;
using System.Collections.Generic;
using VerletRope4.Data;

namespace VerletRope4.Rendering;

[Tool]
public partial class VerletRopeMesh : MeshInstance3D, IVerletExported
{
    public static string ScriptPath => "res://addons/verlet_rope_4/Rendering/VerletRopeMesh.cs";
    public static string IconPath => "res://addons/verlet_rope_4/Icons/icon_rope_mesh.svg";
    public static string ExportedBase => nameof(MeshInstance3D);
    public static string ExportedType => nameof(VerletRopeMesh);

    private const string DefaultMaterialPath = "res://addons/verlet_rope_4/Materials/rope_default.material";
    private const string CreationStampMeta = "verlet_rope_internal_stamp";

    private static readonly float Cos5Deg = Mathf.Cos(Mathf.DegToRad(5.0f));
    private static readonly float Cos15Deg = Mathf.Cos(Mathf.DegToRad(15.0f));
    private static readonly float Cos30Deg = Mathf.Cos(Mathf.DegToRad(30.0f));
    
    private bool _useVisibleOnScreenNotifier = true;
    private VisibleOnScreenNotifier3D _visibleNotifier;
    private Camera3D _camera;
    private double _simulationDelta;
    
    private SurfaceTool _surfaceTool;
    private ArrayMesh _arrayMesh;

    /// <summary> Determines total target length of the rope, it is just a base value and actual length might be different depending on physics and configured behavior. </summary>
    [ExportGroup("Visuals")]
    [Export] public float RopeLength { get; set; } = 3.0f;
    /// <summary> Determines visual width of the rope, does not affect rope behavior. </summary>
    [Export] public float RopeWidth { get; set; } = 0.07f;
    /// <summary> If distance to particle is greater than <see cref="SubdivisionLodDistance"/>, the corresponding segment is not subdivided for rendering. </summary>
    [Export] public float SubdivisionLodDistance { get; set; } = 15.0f;
    /// <summary> Creates a child <see cref="VisibleOnScreenNotifier3D"/> when enabled. Is only triggered on <see cref="_Ready"/> calls. </summary>
    [Export] public bool UseVisibleOnScreenNotifier
    {
        get => _useVisibleOnScreenNotifier; 
        set { _useVisibleOnScreenNotifier = value; UpdateConfigurationWarnings(); }
    }
    /// <summary> Draws orientation axis from every actual particle position when enabled. </summary>
    [Export] public bool UseDebugParticles { get; set; } = false;
    
    /// <summary> If <see cref="VisibleOnScreenNotifier3D"/> is being used, returns if rope is actually visible - otherwise always returns <b>true</b>. </summary>
    public bool IsRopeVisible => _visibleNotifier?.IsOnScreen() ?? true;

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

    private void DrawQuad(IReadOnlyList<Vector3> vertices, float uvx0, float uvx1)
    {
        // Triangle 1
        _surfaceTool.SetUV(new Vector2(uvx0, 0.0f));
        _surfaceTool.AddVertex(vertices[0]);
        
        _surfaceTool.SetUV(new Vector2(uvx1, 0.0f));
        _surfaceTool.AddVertex(vertices[1]);
        
        _surfaceTool.SetUV(new Vector2(uvx1, 1.0f));
        _surfaceTool.AddVertex(vertices[2]);

        // Triangle 2
        _surfaceTool.SetUV(new Vector2(uvx0, 0.0f));
        _surfaceTool.AddVertex(vertices[0]);
        
        _surfaceTool.SetUV(new Vector2(uvx1, 1.0f));
        _surfaceTool.AddVertex(vertices[2]);
        
        _surfaceTool.SetUV(new Vector2(uvx0, 1.0f));
        _surfaceTool.AddVertex(vertices[3]);
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
        _surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

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
                
                DrawQuad(vs, t, t + step);
                t += step;
            }
        }

        _arrayMesh.ClearSurfaces();
        _surfaceTool.GenerateNormals();
        _surfaceTool.GenerateTangents();
        _surfaceTool.Commit(_arrayMesh);
    }

    private void DrawRopeDebugParticles(RopeParticleData particles)
    {
        if (!IsRopeVisible || !IsInsideTree() || particles == null)
        {
            return;
        }

        const float debugParticleLength = 0.3f;
        _surfaceTool.Clear();
        _surfaceTool.Begin(Mesh.PrimitiveType.Lines);

        for (var i = 0; i < particles.Count; i++)
        {
            var particle = particles[i];
            var localPosition = particle.PositionCurrent - GlobalPosition;
            
            _surfaceTool.AddVertex(localPosition);
            _surfaceTool.AddVertex(localPosition + debugParticleLength * particle.Tangent);
            
            _surfaceTool.AddVertex(localPosition);
            _surfaceTool.AddVertex(localPosition + debugParticleLength * particle.Normal);
            
            _surfaceTool.AddVertex(localPosition);
            _surfaceTool.AddVertex(localPosition + debugParticleLength * particle.Binormal);
        }

        _surfaceTool.Commit(_arrayMesh);
    }

    public void DrawRopeParticles(RopeParticleData particles)
    {
        if (!IsRopeVisible || !IsInsideTree() || particles == null || particles.Count < 2)
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

        if (UseDebugParticles)
        {
            DrawRopeDebugParticles(particles);
        }
    }

    public void UpdateRopeVisibility(RopeParticleData particles)
    {
        if (_visibleNotifier == null || particles == null || particles.Count == 0)
        {
            return;
        }

        var minPosition =  new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var maxPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (var i = 0; i < particles.Count; i++)
        {
            ref var particle = ref particles[i];
            minPosition = minPosition.Min(particle.PositionCurrent);
            maxPosition = maxPosition.Max(particle.PositionCurrent);
        }

        _visibleNotifier.Aabb = new Aabb(_visibleNotifier.ToLocal(minPosition), _visibleNotifier.ToLocal(maxPosition - minPosition)).Abs();
    }

    public override string[] _GetConfigurationWarnings()
    {
        return !UseVisibleOnScreenNotifier
            ? [$"Consider checking '{nameof(UseVisibleOnScreenNotifier)}' to disable rope visuals when it's not on screen for increased performance."]
            : [];
    }

    public override void _Ready()
    {
        _surfaceTool = new SurfaceTool();
        _arrayMesh = Mesh as ArrayMesh;

        if (_arrayMesh == null || _arrayMesh.GetMeta(CreationStampMeta, 0ul).AsUInt64() != GetInstanceId())
        {
            Mesh = _arrayMesh = new ArrayMesh();
            _arrayMesh.ResourceLocalToScene = true;
            _arrayMesh.SetMeta(CreationStampMeta, GetInstanceId());
        }

        if (UseVisibleOnScreenNotifier && !Engine.IsEditorHint())
        {
            AddChild(_visibleNotifier = new VisibleOnScreenNotifier3D());
        }

        MaterialOverride ??= GD.Load<StandardMaterial3D>(DefaultMaterialPath);
    }
}
