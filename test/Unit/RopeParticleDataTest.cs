using GdUnit4;
using Godot;
using System.Collections.Generic;
using VerletRope4.Data;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "unit")]
public class RopeParticleDataTest
{
    [TestCase]
    public void GenerateParticleDataSpacesParticlesAlongTheRopeDirection()
    {
        var start = new Vector3(0.0f, 0.0f, 0.0f);
        var end = new Vector3(0.0f, 0.0f, 1.0f);
        var acceleration = new Vector3(0.0f, -9.8f, 0.0f);

        var data = RopeParticleData.GenerateParticleData(start, end, acceleration, 5, 0.25f);

        AssertThat(data.Count).IsEqual(5);
        AssertThat(data[0].PositionCurrent).IsEqual(start);

        for (var i = 1; i < data.Count; i++)
        {
            AssertThat(data[i].PositionCurrent.DistanceTo(start)).IsEqualApprox(0.25f * i, 0.0001f);
            AssertThat(data[i].PositionCurrent).IsEqual(data[i].PositionPrevious);
            AssertThat(data[i].Acceleration).IsEqual(acceleration);
            AssertThat(data[i].IsAttached).IsFalse();
        }
    }

    /// <summary> A rope whose two ends sit on the same point would be a zero-length direction, so it is jittered apart instead. </summary>
    [TestCase]
    public void GenerateParticleDataJittersParticlesThatStartAtTheSamePoint()
    {
        var location = new Vector3(1.0f, 2.0f, 3.0f);

        var data = RopeParticleData.GenerateParticleData(location, location, Vector3.Zero, 4, 0.5f);
        var distinctPositions = new HashSet<Vector3>();

        for (var i = 0; i < data.Count; i++)
        {
            distinctPositions.Add(data[i].PositionCurrent);
            AssertThat(data[i].PositionCurrent.DistanceTo(location)).IsLessEqual(0.01f);
            AssertThat(data[i].Tangent).IsEqual(Vector3.Zero);
        }

        AssertThat(distinctPositions.Count).IsEqual(data.Count);
    }

    [TestCase]
    public void GenerateParticleDataKeepsGivenParticlePositions()
    {
        var positions = new List<Vector3> { new(0.0f, 0.0f, 0.0f), new(0.0f, 1.0f, 0.0f), new(0.0f, 2.0f, 0.0f) };

        var data = RopeParticleData.GenerateParticleData(positions);

        AssertThat(data.Count).IsEqual(3);

        for (var i = 0; i < positions.Count; i++)
        {
            AssertThat(data[i].PositionCurrent).IsEqual(positions[i]);
            AssertThat(data[i].PositionRender).IsEqual(positions[i]);
            AssertThat(data[i].Acceleration).IsEqual(Vector3.Zero);
            AssertThat(data[i].IsAttached).IsFalse();
        }
    }
}
