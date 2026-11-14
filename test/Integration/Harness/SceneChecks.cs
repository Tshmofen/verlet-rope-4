using Godot;
using System.Collections.Generic;
using VerletRope4.Physics;

namespace VerletRope4.Tests.Integration.Harness;

/// <summary>
/// Collects the goals of one scenario run instead of throwing on the first miss, so a failing scene reports everything
/// that went wrong in it and the run still finishes with a single exit code.
/// </summary>
public sealed class SceneChecks
{
    /// <summary>
    /// Largest ratio between a segment and the rest length it was built for before the rope counts as torn. A rope
    /// settles near its rest length, so a segment several times too long is not a stretched rope but a constraint solve
    /// that stopped pulling.
    /// </summary>
    private const float IntegrityLimit = 5.0f;

    private readonly List<string> _failures = [];

    /// <summary> Amount of goals the run did not meet. </summary>
    public int FailureCount => _failures.Count;

    /// <summary> What every goal that was not met reported, in the order they were checked. </summary>
    public IReadOnlyList<string> Failures => _failures;

    /// <summary> Records a goal that is not met, and keeps the run going so one scene reports every miss at once. </summary>
    public void Check(bool condition, string description)
    {
        if (!condition)
        {
            _failures.Add(description);
        }
    }

    /// <summary> Records a goal whose measured value has to stay inside a range, quoting both in the report. </summary>
    public void CheckRange(float value, float min, float max, string description)
    {
        Check(value >= min && value <= max, $"{description} (got {value:F2}, expected {min:F2}..{max:F2})");
    }

    /// <summary> Records a goal whose measured value has to stay at or below a limit, quoting both in the report. </summary>
    public void CheckAtMost(float value, float max, string description)
    {
        Check(value <= max, $"{description} (got {value:F2}, expected at most {max:F2})");
    }

    /// <summary> Every axis of every particle has to stay a real number - an exploded constraint shows up as a NaN first. </summary>
    public void CheckParticlesAreFinite(BaseVerletRopePhysical rope, string description)
    {
        if (rope.GetParticleCount() == 0)
        {
            Check(false, $"{description} (the rope has no particles)");
            return;
        }

        for (var i = 0; i < rope.GetParticleCount(); i++)
        {
            var position = rope.GetParticlePosition(i);

            if (!position.IsFinite())
            {
                Check(false, $"{description} (particle {i} is {position})");
                return;
            }
        }
    }

    /// <summary>
    /// Records whether a rope that stays in one piece keeps every segment within a few rest lengths of its neighbours.
    /// The rest length is taken from the rope as it is configured now, so this belongs after the rope has settled at the
    /// length its scene keeps it at - not while a winch is reeling it in or a hook is paying it out.
    /// </summary>
    public void CheckRopeIntegrity(BaseVerletRopePhysical rope, string description)
    {
        var particleCount = rope.GetParticleCount();

        if (particleCount < 2)
        {
            Check(false, $"{description} (the rope has {particleCount} particle(s))");
            return;
        }

        var segmentCount = particleCount - 1;
        var limit = IntegrityLimit * (rope.RopeLength / segmentCount);
        var longestSegment = 0.0f;

        for (var i = 0; i < segmentCount; i++)
        {
            longestSegment = Mathf.Max(longestSegment, rope.GetParticlePosition(i).DistanceTo(rope.GetParticlePosition(i + 1)));
        }

        Check(longestSegment <= limit, $"{description} (longest segment {longestSegment:F2}, limit {limit:F2})");
    }
}
