#if TOOLS

using Godot;
using VerletRope.Physics;
using VerletRope.Physics.Joints;
using VerletRope4.Physics;

namespace VerletRope4;

[Tool]
public partial class VerletRopePlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>(VerletRopeSimulated.ScriptPath);
        var texture = GD.Load<Texture2D>(VerletRopeSimulated.IconPath);
        AddCustomType(nameof(VerletRopeSimulated), nameof(MeshInstance3D), script, texture);

        script = GD.Load<Script>(VerletRopeRigidBody.ScriptPath);
        texture = GD.Load<Texture2D>(VerletRopeRigidBody.IconPath);
        AddCustomType(nameof(VerletRopeRigidBody), nameof(MeshInstance3D), script, texture);

        script = GD.Load<Script>(VerletRopeJoint.ScriptPath);
        texture = GD.Load<Texture2D>(VerletRopeJoint.IconPath);
        AddCustomType(nameof(VerletRopeJoint), nameof(Node3D), script, texture);
    }

    public override void _ExitTree()
    {
        RemoveCustomType(nameof(VerletRopeSimulated));
        RemoveCustomType(nameof(VerletRopeJoint));
    }
}

#endif
