#if TOOLS

using Godot;
using VerletRope4.Physics;
using VerletRope4.Physics.Joints;
using VerletRope4.Rendering;
using VerletRope4.Utility;

namespace VerletRope4;

[Tool]
public partial class VerletRopePlugin : EditorPlugin
{
    private VerletRopeGizmoPlugin _gizmoPlugin;

    private void AddVerletType(string exportType, string exportBase, string scriptPath, string iconPath)
    {
        var script = GD.Load<Script>(scriptPath);
        var texture = GD.Load<Texture2D>(iconPath);
        AddCustomType(exportType, exportBase, script, texture);
    }

    public override void _EnterTree()
    {
        AddVerletType(VerletRopeSimulated.ExportedType, VerletRopeSimulated.ExportedBase, VerletRopeSimulated.ScriptPath, VerletRopeSimulated.IconPath);
        AddVerletType(VerletJointSimulated.ExportedType, VerletJointSimulated.ExportedBase, VerletJointSimulated.ScriptPath, VerletJointSimulated.IconPath);

        AddVerletType(VerletRopeRigid.ExportedType, VerletRopeRigid.ExportedBase, VerletRopeRigid.ScriptPath, VerletRopeRigid.IconPath);
        AddVerletType(VerletJointRigid.ExportedType, VerletJointRigid.ExportedBase, VerletJointRigid.ScriptPath, VerletJointRigid.IconPath);

        AddVerletType(DistanceForceJoint.ExportedType, DistanceForceJoint.ExportedBase, DistanceForceJoint.ScriptPath, DistanceForceJoint.IconPath);
        AddVerletType(VerletRopeMesh.ExportedType, VerletRopeMesh.ExportedBase, VerletRopeMesh.ScriptPath, VerletRopeMesh.IconPath);

        // A C# gizmo plugin sometimes crashes the engine when it is disposed at shutdown. Headless editor as well which is used for unit testing,
        // so we skip it there since gizmo is not needed, and we want all test runs to be deterministically successful.
        // See: https://github.com/godotengine/godot/issues/110790.
        if (!NodeUtility.IsHeadless)
        {
            AddNode3DGizmoPlugin(_gizmoPlugin = new VerletRopeGizmoPlugin());
        }
    }

    public override void _ExitTree()
    {
        RemoveCustomType(VerletRopeSimulated.ExportedType);
        RemoveCustomType(VerletJointSimulated.ExportedType);

        RemoveCustomType(VerletRopeRigid.ExportedType);
        RemoveCustomType(VerletJointRigid.ExportedType);

        RemoveCustomType(DistanceForceJoint.ExportedType);
        RemoveCustomType(VerletRopeMesh.ExportedType);

        if (_gizmoPlugin != null)
        {
            RemoveNode3DGizmoPlugin(_gizmoPlugin);
        }
    }

    public override bool _Handles(GodotObject @object)
    {
        if (@object is not BaseVerletRopePhysical rope)
        {
            return false;
        }

        rope.AssociateUndoRedoManager(GetUndoRedo());
        return true;
    }
}

#endif
