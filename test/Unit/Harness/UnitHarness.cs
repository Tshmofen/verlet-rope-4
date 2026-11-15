using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerletRope4.Physics;

namespace VerletRope4.Tests.Unit.Harness;

/// <summary>
/// The waits and lookups every unit suite needs, so one frame contract and one body collection are declared once instead
/// of once per suite.
/// </summary>
public static class UnitHarness
{
    /// <summary> What <paramref name="action"/> threw, or `null` when it threw nothing, so a case can assert a refusal without a try block. </summary>
    public static Exception CatchException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    /// <summary>
    /// The segment bodies of a rigid rope. A rebuild queues the bodies it replaces for free, and they stay children until
    /// the frame ends, so they are left out.
    /// </summary>
    public static List<RigidBody3D> CollectBodies(VerletRopeRigid rope)
    {
        return rope.GetChildren()
            .OfType<RigidBody3D>()
            .Where(body => !body.IsQueuedForDeletion())
            .ToList();
    }

    /// <summary>
    /// A deferred apply flushes after the process signal of the frame it was queued in, so a write is only visible from
    /// the frame after that one: two awaits are the contract here, never a spare.
    /// </summary>
    public static async Task NextFrame(Node node)
    {
        await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
