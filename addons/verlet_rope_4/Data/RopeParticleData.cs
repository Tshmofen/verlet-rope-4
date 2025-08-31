using System;
using System.Collections.Generic;
using Godot;

namespace VerletRope4.Data;

public sealed class RopeParticleData
{
    private const float UnwrappingJitter = 0.005f;
    private static readonly RandomNumberGenerator Random = new();

    private readonly RopeParticle[] _particles;

    public int Count => _particles.Length;
    public ref RopeParticle this[Index i] => ref _particles[i];

    private RopeParticleData(RopeParticle[] particles)
    {
        _particles = particles;
    }

    public static RopeParticleData GenerateParticleData(Vector3 startLocation, Vector3 endLocation, Vector3 initialAcceleration, int simulationParticles, float segmentLength)
    {
        var isUnwrapping = endLocation == startLocation;
        var direction = !isUnwrapping
            ? (endLocation - startLocation).Normalized()
            : Vector3.Zero;
        var data = new RopeParticle[simulationParticles];

        for (var i = 0; i < simulationParticles; i++)
        {
            data[i] = new RopeParticle();
            ref var particle = ref data[i];
            particle.Tangent = particle.Normal = particle.Binormal = Vector3.Zero;
            particle.PositionCurrent = particle.PositionPrevious = startLocation + (direction * segmentLength * i);
            particle.Acceleration = initialAcceleration;
            particle.IsAttached = false;

            if (isUnwrapping)
            {
                particle.PositionPrevious = particle.PositionCurrent = new Vector3(
                    particle.PositionCurrent.X + Random.RandfRange(-UnwrappingJitter, UnwrappingJitter),
                    particle.PositionCurrent.Y + Random.RandfRange(-UnwrappingJitter, UnwrappingJitter),
                    particle.PositionCurrent.Z + Random.RandfRange(-UnwrappingJitter, UnwrappingJitter)
                );
            }
        }

        return new RopeParticleData(data);
    }

    public static RopeParticleData GenerateParticleData(List<Vector3> particlePositions)
    {
        var data = new RopeParticle[particlePositions.Count];

        for (var i = 0; i < particlePositions.Count; i++)
        {
            data[i] = new RopeParticle();
            ref var particle = ref data[i];
            particle.Tangent = particle.Normal = particle.Binormal = Vector3.Zero;
            particle.PositionCurrent = particle.PositionPrevious = particlePositions[i];
            particle.Acceleration = Vector3.Zero;
            particle.IsAttached = false;
        }

        return new RopeParticleData(data);
    }
}

// TODO: Make providers implement interface/base with only PositionCurrent, so that bookmark properties were not exposed when not needed
public struct RopeParticle
{
    /// <summary> The current position of the particle for this frame - used for mesh generation. </summary>
    public Vector3 PositionPrevious { get; set; }

    /// <summary> Bookmark provider property - The position of the particle from the previous frame. used to calculate velocity. </summary>
    public Vector3 PositionCurrent { get; set; }

    /// <summary> Bookmark provider property - The acceleration applied to this particle (i.e. combined from gravity, wind or any other forces). </summary>
    public Vector3 Acceleration { get; set; }
    
    /// <summary> Bookmark provider property - Indicates whether particle's position is locked and not simulated (e.g. for attachment points). </summary>
    public bool IsAttached { get; set; }
    
    /// <summary> Internal property - Provides currently calculated visual tangent particle vector. </summary>
    public Vector3 Tangent { get; set; }
    
    /// <summary> Internal property - Provides currently calculated visual normal particle vector. </summary>
    public Vector3 Normal { get; set; }
    
    /// <summary> Internal property - Provides currently calculated visual binormal particle vector. </summary>
    public Vector3 Binormal { get; set; }
}
