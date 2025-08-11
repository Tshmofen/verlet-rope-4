using Godot;
using System.Collections.Generic;
using VerletRope4.Rendering;

namespace VerletRope.Physics;

[GlobalClass]
public abstract partial class VerletRopePhysical : VerletRopeMesh
{
    protected readonly List<Rid> CollisionExceptions = [];

    public virtual void RegisterExceptionRid(Rid rid, bool toInclude)
    {
        if (toInclude)
        {
            CollisionExceptions.Add(rid);
        }
        else
        {
            CollisionExceptions.Remove(rid);
        }
    }

    public abstract void CreateRope();

    public abstract void DestroyRope();
}