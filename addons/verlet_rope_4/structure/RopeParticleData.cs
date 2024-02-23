using System;
using Godot;

namespace VerletRope4.Structure;

internal sealed class RopeParticleData
{
    public static RopeParticleData GenerateParticleData(Vector3 endLocation, Vector3 globalPosition, Vector3 initialAcceleration, int simulationParticles, float segmentLength)
    {
        var direction = (endLocation - globalPosition).Normalized();
        var data = new RopeParticle[simulationParticles];

        for (var i = 0; i < simulationParticles; i++)
        {
            data[i] = new RopeParticle();
            ref var particle = ref data[i];
            particle.Tangent = particle.Normal = particle.Binormal = Vector3.Zero;
            particle.PositionPrevious = globalPosition + (direction * segmentLength * i);
            particle.PositionCurrent = particle.PositionPrevious;
            particle.Acceleration = initialAcceleration;
            particle.IsAttached = false;
        }

        return new RopeParticleData(data);
    }

    private RopeParticle[] _particles;

    private RopeParticleData(RopeParticle[] particles)
    {
        _particles = particles;
    }

    public void Resize(int size)
    {
        Array.Resize(ref _particles, size);
    }

    public ref RopeParticle this[Index i] => ref _particles[i];
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
