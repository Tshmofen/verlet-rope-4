#if TOOLS

using Godot;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;

namespace VerletRope4;

[Tool]
public partial class VerletRopePlugin : EditorPlugin
{
    private VerletRopeGizmoPlugin _gizmoPlugin;

    public override void _EnterTree()
    {
        var script = GD.Load<Script>(VerletRopeSimulated.ScriptPath);
        var texture = GD.Load<Texture2D>(VerletRopeSimulated.IconPath);
        AddCustomType(nameof(VerletRopeSimulated), nameof(Node3D), script, texture);

        script = GD.Load<Script>(VerletRopeRigidBody.ScriptPath);
        texture = GD.Load<Texture2D>(VerletRopeRigidBody.IconPath);
        AddCustomType(nameof(VerletRopeRigidBody), nameof(Node3D), script, texture);

        script = GD.Load<Script>(VerletRopeSimulatedJoint.ScriptPath);
        texture = GD.Load<Texture2D>(VerletRopeSimulatedJoint.IconPath);
        AddCustomType(nameof(VerletRopeSimulatedJoint), nameof(Node3D), script, texture);
        
        script = GD.Load<Script>(VerletRopeRigidJoint.ScriptPath);
        texture = GD.Load<Texture2D>(VerletRopeRigidJoint.IconPath);
        AddCustomType(nameof(VerletRopeRigidJoint), nameof(Node3D), script, texture);

        AddNode3DGizmoPlugin(_gizmoPlugin = new VerletRopeGizmoPlugin());
    }

    public override void _ExitTree()
    {
        RemoveCustomType(nameof(VerletRopeSimulated));
        RemoveCustomType(nameof(VerletRopeRigidBody));
        RemoveCustomType(nameof(VerletRopeSimulatedJoint));
        RemoveCustomType(nameof(VerletRopeRigidJoint));
        RemoveNode3DGizmoPlugin(_gizmoPlugin);
    }
}

#endif
