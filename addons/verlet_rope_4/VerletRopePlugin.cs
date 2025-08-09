#if TOOLS

using Godot;

namespace VerletRope4;

[Tool]
public partial class VerletRopePlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>(VerletRopeSimulated.ScriptPath);
        var texture = GD.Load<Texture2D>(VerletRopeSimulated.IconPath);
        AddCustomType(nameof(VerletRopeSimulated), nameof(MeshInstance3D), script, texture);
    }

    public override void _ExitTree()
    {
        RemoveCustomType(nameof(VerletRopeSimulated));
    }
}

#endif
