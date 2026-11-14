using GdUnit4;
using Godot;
using VerletRope4.Utility;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary> Covers the segment placement both rope types lay their structure with. </summary>
[TestSuite]
[RequireGodotRuntime]
public class SegmentPlaceUtilityTest
{
    [TestCase]
    public void ConnectPointsSpacesEverySegmentAtTheTargetLengthWhenTheRopeIsStretched()
    {
        var start = new Vector3(0.0f, 0.0f, 0.0f);
        var end = new Vector3(0.0f, 0.0f, 3.0f);

        var points = SegmentPlaceUtility.ConnectPoints(start, end, Vector3.Up, 0.3f, 10);

        AssertThat(points.Length).IsEqual(11);
        AssertThat(points[0]).IsEqual(start);
        AssertThat(points[^1]).IsEqual(end);

        for (var i = 1; i < points.Length; i++)
        {
            AssertThat(points[i].DistanceTo(points[i - 1])).IsEqualApprox(0.3f, 0.0001f);
        }
    }

    [TestCase]
    public void ConnectPointsZigZagsAwayFromTheLineWhenTheRopeIsSlack()
    {
        var start = Vector3.Zero;
        var end = new Vector3(1.0f, 0.0f, 0.0f);

        var points = SegmentPlaceUtility.ConnectPoints(start, end, Vector3.Up, 0.3f, 10);

        var largestOffset = 0.0f;
        foreach (var point in points)
        {
            largestOffset = Mathf.Max(largestOffset, Mathf.Abs(point.Z));
        }

        AssertThat(points.Length).IsEqual(11);
        AssertThat(points[0]).IsEqual(start);
        AssertThat(points[^1]).IsEqual(end);
        AssertThat(largestOffset).IsGreater(0.1f);

        for (var i = 1; i < points.Length; i++)
        {
            AssertThat(points[i].DistanceTo(points[i - 1])).IsEqualApprox(0.3f, 0.0001f);
        }
    }

    /// <summary> A single segment is laid as a straight step of the target length, so it does not reach an end point further away. </summary>
    [TestCase]
    public void ConnectPointsTreatsASingleSegmentAsOneStraightStep()
    {
        var start = Vector3.Zero;
        var end = new Vector3(0.0f, 0.0f, 3.0f);

        var points = SegmentPlaceUtility.ConnectPoints(start, end, Vector3.Up, 0.5f, 1);

        AssertThat(points.Length).IsEqual(2);
        AssertThat(points[0]).IsEqual(start);
        AssertThat(points[1].DistanceTo(start)).IsEqualApprox(0.5f, 0.0001f);
        AssertThat(points[1].DistanceTo(end)).IsGreater(2.0f);
    }
}
