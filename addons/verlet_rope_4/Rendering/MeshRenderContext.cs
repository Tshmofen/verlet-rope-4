using Godot;
using VerletRope4.Data;

namespace VerletRope.Rendering;

public class MeshRenderContext
{
    public SurfaceTool SurfaceTool { get; set; }
    public ArrayMesh ArrayMesh { get; set; }
    public RopeParticleData Particles { get; set; }
    public Vector3 GlobalPosition { get; set; }
    public Camera3D CurrentCamera { get; set; }
    public float SubdivisionLodDistance { get; set; }
    public float AverageSegmentLength { get; set; }
    public int TubeSegments { get; set; }
    public float RopeWidth { get; set; }
}
