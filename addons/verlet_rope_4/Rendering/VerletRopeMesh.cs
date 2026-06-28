using Godot;
using System.Collections.Generic;
using VerletRope.Data;
using VerletRope.Rendering;
using VerletRope.Rendering.Tools;
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

    private static readonly RopeMeshDebugTool MeshDebugTool = new();
    private static readonly Dictionary<RopeMeshType, IRopeMeshTool> MeshTools = new()
    {
        { RopeMeshType.Ribbon, new RopeMeshRibbonTool() },
        { RopeMeshType.Tube, new RopeMeshDebugTool() } // TODO
    };
    
    private bool _useVisibleOnScreenNotifier = true;
    private VisibleOnScreenNotifier3D _visibleNotifier;
    private double _simulationDelta;
    
    private SurfaceTool _surfaceTool;
    private ArrayMesh _arrayMesh;

    #region Exported Properties
    
    /// <inheritdoc cref="RopeMeshType"/>
    [ExportGroup("Visuals")]
    [Export] public RopeMeshType MeshType { get; set; } = RopeMeshType.Ribbon;
    /// <summary> Determines total target length of the rope, it is just a base value and actual length might be different depending on physics and configured behavior. </summary>
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

    #endregion

    #region Util
    
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

    private MeshRenderContext GetMeshRenderContext(RopeParticleData particles)
    {
        return new MeshRenderContext
        {
            Particles = particles,
            RopeWidth = RopeWidth,
            ArrayMesh = _arrayMesh,
            SurfaceTool = _surfaceTool,
            GlobalPosition = GlobalPosition,
            CurrentCamera = GetCurrentCamera(),
            SubdivisionLodDistance = SubdivisionLodDistance,
            AverageSegmentLength = GetAverageSegmentLength(particles.Count)
        };
    }

    private Camera3D GetCurrentCamera()
    {
        #if TOOLS
        return Engine.IsEditorHint()
            ? EditorInterface.Singleton.GetEditorViewport3D().GetCamera3D()
            : GetViewport().GetCamera3D();
        #else
        return GetViewport().GetCamera3D();
        #endif
    }

    private static void CalculateRopeCameraOrientation(MeshRenderContext context)
    {
        var cameraPosition = context.CurrentCamera?.GlobalPosition ?? Vector3.Zero;
        var particles = context.Particles;

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

    #endregion
    
    public void DrawRopeParticles(RopeParticleData particles)
    {
        if (!IsRopeVisible || !IsInsideTree() || particles == null || particles.Count < 2)
        {
            return;
        }
        
        var renderContext = GetMeshRenderContext(particles);
        CalculateRopeCameraOrientation(renderContext);
        ResetRopeRotation();
        
        _arrayMesh.ClearSurfaces();
        MeshTools[MeshType].DrawParticles(renderContext);

        if (UseDebugParticles)
        {
            MeshDebugTool.DrawParticles(renderContext);
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
