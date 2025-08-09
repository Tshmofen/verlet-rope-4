using System;
using Godot;

namespace VerletRope4.Structure;

public sealed class RopeParticleData
{
    private RopeParticle[] _particles;

    public int Count => _particles.Length;
    public ref RopeParticle this[Index i] => ref _particles[i];

    private RopeParticleData(RopeParticle[] particles)
    {
        _particles = particles;
    }

    public void Resize(int size)
    {
        Array.Resize(ref _particles, size);
    }

    public static RopeParticleData GenerateParticleData(Vector3 endLocation, Vector3 startLocation, Vector3 initialAcceleration, int simulationParticles, float segmentLength)
    {
        var direction = (endLocation - startLocation).Normalized();
        var data = new RopeParticle[simulationParticles];

        for (var i = 0; i < simulationParticles; i++)
        {
            data[i] = new RopeParticle();
            ref var particle = ref data[i];
            particle.Tangent = particle.Normal = particle.Binormal = Vector3.Zero;
            particle.PositionPrevious = startLocation + (direction * segmentLength * i);
            particle.PositionCurrent = particle.PositionPrevious;
            particle.Acceleration = initialAcceleration;
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
