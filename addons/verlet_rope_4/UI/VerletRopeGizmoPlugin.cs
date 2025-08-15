﻿#if TOOLS

using Godot;
using VerletRope.Physics;

namespace VerletRope4.UI;

[Tool]
public partial class VerletRopeGizmoPlugin : EditorNode3DGizmoPlugin
{
    private static void AssociateEditorCollision(EditorNode3DGizmo gizmo)
    {
        if (gizmo.GetNode3D() is not VerletRopePhysical rope)
        {
            return;
        }

        var editorSegments = rope.GetEditorSegments();
        if (editorSegments == null)
        {
            return;
        }

        gizmo.AddCollisionSegments(editorSegments);
    }

    public override string _GetGizmoName() => nameof(VerletRopeGizmoPlugin);

    public override void _Redraw(EditorNode3DGizmo gizmo)
    {
        gizmo.Clear();
        AssociateEditorCollision(gizmo);
    }

    public override EditorNode3DGizmo _CreateGizmo(Node3D forNode3D)
    {
        if (forNode3D is not VerletRopePhysical)
        {
            return null;
        }

        var gizmo = new EditorNode3DGizmo();
        gizmo.SetNode3D(forNode3D);
        AssociateEditorCollision(gizmo);
        return gizmo;
    }

    public override bool _HasGizmo(Node3D forNode3D)
    {
        return forNode3D is VerletRopePhysical;
    }
}

#endif