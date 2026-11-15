using Godot;
using System.Threading.Tasks;

namespace VerletRope4.Tests.Integration.Harness;

/// <summary> Drives one instantiated scene, recording every goal it does not meet instead of failing on the first one. </summary>
/// <param name="root"> The scene, already instantiated and parented by the runner. </param>
/// <param name="checks"> Collects the goals of the run. </param>
public delegate Task SceneRun(Node root, SceneChecks checks);
