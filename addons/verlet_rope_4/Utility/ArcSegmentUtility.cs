using System;
using Godot;

namespace VerletRope.addons.verlet_rope_4.Utility;

public static class ArcSegmentUtility
{
    private static (float radius, float sweepAngle) GetMinimalArcParameters(float chordLength, float arcLength)
    {
        // Solve: arcLength = radius * theta, chordLength = 2 * radius * sin(theta / 2)
        // Using Newton-Raphson to approximate theta

        var theta = 2.0f; // Initial guess
        for (var i = 0; i < 100; i++)
        {
            var thetaSin = Mathf.Sin(theta / 2.0f);
            var thetaCos = Mathf.Cos(theta / 2.0f);

            var f = 2.0f * theta * thetaSin - chordLength * theta / arcLength;
            var df = 2.0f * thetaSin + theta * thetaCos - chordLength / arcLength;
            theta -= f / df;

            if (Mathf.Abs(f) < Mathf.Epsilon)
            {
                break;
            }
        }

        var radius = arcLength / theta;
        return (radius, theta);
    }

    private static Vector2 GetArcCenter(Vector2 a, Vector2 b, float radius, float sweepAngle)
    {
        var midPoint = (a + b) / 2.0f;
        var chordLength = a.DistanceTo(b);
        var apothem = Mathf.Sqrt(radius * radius - (chordLength / 2.0f) * (chordLength / 2.0f));

        var pointsDirection = (b - a).Normalized();
        var perpendicularDirection = new Vector2(-pointsDirection.Y, pointsDirection.X);

        var sign = sweepAngle > 0 ? 1.0f : -1.0f;
        return midPoint + sign * apothem * perpendicularDirection;
    }

    private static Vector2[] SampleArcSegmentPoints(Vector2 a, Vector2 b, float pointsDistance, float segmentsLength, int segmentCount)
    {
        var (radius, sweepAngle) = GetMinimalArcParameters(pointsDistance, segmentsLength);
        var center = GetArcCenter(a, b, radius, sweepAngle);
        var startAngle = Mathf.Atan2(a.Y - center.Y, a.X - center.X);
        var angleStep = sweepAngle / segmentCount;
        
        var points = new Vector2[segmentCount + 1];
        points[0] = a;

        for (var i = 1; i < segmentCount; i++)
        {
            var angle = startAngle + i * angleStep;
            var x = center.X + radius * Mathf.Cos(angle);
            var y = center.Y + radius * Mathf.Sin(angle);
            points[i] = new Vector2(x, y);
        }

        points[^1] = b;
        return points;
    }

    private static Vector2[] GenerateStraightLineSegments(Vector2 a, Vector2 b, float segmentLength, int segmentCount)
    {
        var points = new Vector2[segmentCount + 1];
        var dir = (b - a).Normalized();

        for (var i = 0; i <= segmentCount; i++)
        {
            points[i] = a + dir * segmentLength * i;
        }

        return points;
    }

    private static Vector3 GetAxisPlaneNormal(Vector3.Axis planeAxis)
    {
        return planeAxis switch
        {
            Vector3.Axis.X => Vector3.Right,
            Vector3.Axis.Y => Vector3.Up,
            Vector3.Axis.Z => Vector3.Forward,
            _ => throw new ArgumentOutOfRangeException(nameof(planeAxis), planeAxis, null)
        };
    }

    public static Vector2[] ConnectPoints(Vector2 a, Vector2 b, float segmentLength, int segmentCount)
    {
        var segmentsLength = segmentCount * segmentLength;
        var pointsDistance = a.DistanceTo(b);

        return segmentsLength - pointsDistance <= Mathf.Epsilon
            ? GenerateStraightLineSegments(a, b, segmentLength, segmentCount)
            : SampleArcSegmentPoints(a, b, pointsDistance, segmentsLength, segmentCount);
    }

    public static Vector2[] ConnectPoints(Vector3 a, Vector3 b, Vector3.Axis planeAxis, float segmentLength, int segmentCount)
    {
        var segmentsLength = segmentCount * segmentLength;
        var pointsDistance = a.DistanceTo(b);
        var planeNormal = GetAxisPlaneNormal(planeAxis);
        
        // TODO: Project for Vector3

        return segmentsLength - pointsDistance <= Mathf.Epsilon
            ? GenerateStraightLineSegments(a, b, segmentLength, segmentCount)
            : SampleArcSegmentPoints(a, b, pointsDistance, segmentsLength, segmentCount);
    }
}