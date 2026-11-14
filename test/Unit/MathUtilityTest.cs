using GdUnit4;
using Godot;
using VerletRope4.Utility;
using static GdUnit4.Assertions;

namespace VerletRope4.Tests.Unit;

/// <summary> Covers the vector interpolation helper the tools share. </summary>
[TestSuite]
[RequireGodotRuntime]
[Trait("lane", "unit")]
public class MathUtilityTest
{
    [TestCase]
    public void LerpReturnsTheEndpointsForTheExtremeWeights()
    {
        var from = new Vector3(1.0f, 2.0f, 3.0f);
        var to = new Vector3(-4.0f, 5.0f, -6.0f);

        AssertThat(MathUtility.Lerp(from, to, 0.0f)).IsEqual(from);
        AssertThat(MathUtility.Lerp(from, to, 1.0f)).IsEqual(to);
    }

    [TestCase]
    public void LerpInterpolatesEveryComponentAtHalfWeight()
    {
        var from = new Vector3(1.0f, 2.0f, 3.0f);
        var to = new Vector3(3.0f, 4.0f, 5.0f);

        AssertThat(MathUtility.Lerp(from, to, 0.5f)).IsEqual(new Vector3(2.0f, 3.0f, 4.0f));
    }
}
