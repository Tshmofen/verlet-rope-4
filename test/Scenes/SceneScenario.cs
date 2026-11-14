using Godot;
using System.IO;
using System.Threading.Tasks;

namespace VerletRope4.Tests.Scenes;

/// <summary> One demo scene and the goals a run of it has to meet. </summary>
public abstract class SceneScenario
{
    /// <summary> Name the scenario is reported with, which is the scene file name without its extension. </summary>
    public string Name => Path.GetFileNameWithoutExtension(ScenePath);

    /// <summary> Scene the scenario drives, relative to the project root. </summary>
    public abstract string ScenePath { get; }

    /// <summary> Drives an instantiated and already parented scene, recording every goal it does not meet. </summary>
    public abstract Task RunAsync(Node root, SceneChecks checks);

    /// <summary> Waits out real physics ticks, which is what the scene's own driver and ropes are stepped by. </summary>
    protected static async Task WaitPhysicsFrames(Node node, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }
}
