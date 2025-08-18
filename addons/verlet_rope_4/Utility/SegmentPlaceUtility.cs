using Godot;

namespace VerletRope4.Utility;

public static class SegmentPlaceUtility
{
    public static Vector3[] GenerateAlternatingSegments(Vector3 a, Vector3 b, Vector3 planeNormal, float segmentLength, int segmentCount)
    {
        var pointsDistance = a.DistanceTo(b);
        var rightDirection = (b - a).Normalized();
        var upDirection = rightDirection.Cross(planeNormal).Normalized();

        var points = new Vector3[segmentCount + 1];
        points[0] = a;

        var isOddCount = segmentCount % 2 != 0;
        var placementDistance = isOddCount ? (pointsDistance - segmentLength) : pointsDistance;
        var placementCount = isOddCount ? (segmentCount - 1) : segmentCount;

        var cos = Mathf.Clamp(placementDistance / (placementCount * segmentLength), -1.0f, 1.0f);
        var sin = Mathf.Sqrt(1.0f - cos * cos);

        for (var i = 1; i < placementCount; i++)
        {
            var segmentChange = i % 2 == 0
                ? segmentLength * (cos * rightDirection + sin * upDirection)
                : segmentLength * (cos * rightDirection - sin * upDirection);

            points[i] = points[i - 1] + segmentChange;
        }

        if (isOddCount)
        {
            points[^2] = points[^3] * rightDirection;
        }

        points[^1] = b;
        return points;
    }

    private static Vector3[] GenerateStraightLineSegments(Vector3 a, Vector3 b, float segmentLength, int segmentCount)
    {
        var points = new Vector3[segmentCount + 1];
        var dir = (b - a).Normalized();

        for (var i = 0; i <= segmentCount; i++)
        {
            points[i] = a + dir * segmentLength * i;
        }

        return points;
    }

    public static Vector3[] ConnectPoints(Vector3 a, Vector3 b, Vector3 planeNormal, float segmentLength, int segmentCount)
    {
        var segmentsLength = segmentCount * segmentLength;
        return segmentCount == 1 || segmentsLength - a.DistanceTo(b) <= Mathf.Epsilon
            ? GenerateStraightLineSegments(a, b, segmentLength, segmentCount)
            : GenerateAlternatingSegments(a, b, planeNormal, segmentLength, segmentCount);
    }
}