namespace VerletRope.Data;

/// <summary> Determines the type of Rope Mesh rendering. </summary>
public enum RopeMeshType
{
    /// <summary>
    /// Catmull-interpolated camera‑facing flat ribbon.
    /// Light performance-wise.
    /// </summary>
    Ribbon,

    /// <summary>
    /// Cylindrical 3D-mesh extruded along the rope.
    /// More performance heavy than <see cref="Ribbon"/>.
    /// </summary>
    Tube
}
