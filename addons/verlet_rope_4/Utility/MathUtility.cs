using Godot;

namespace VerletRope.Utility;

public static class MathUtility
{
    public static Vector3 Lerp(Vector3 from, Vector3 to, float weight)
    {
        return new Vector3(
            Mathf.Lerp(from.X, to.X, weight),
            Mathf.Lerp(from.Y, to.Y, weight),
            Mathf.Lerp(from.Z, to.Z, weight)
        );
    }
}