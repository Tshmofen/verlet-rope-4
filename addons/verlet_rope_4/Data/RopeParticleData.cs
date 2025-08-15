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

public struct RopeParticle
{
    public Vector3 PositionPrevious { get; set; }
    public Vector3 PositionCurrent { get; set; }
    public Vector3 Acceleration { get; set; }
    public bool IsAttached { get; set; }
    public Vector3 Tangent { get; set; }
    public Vector3 Normal { get; set; }
    public Vector3 Binormal { get; set; }
}
