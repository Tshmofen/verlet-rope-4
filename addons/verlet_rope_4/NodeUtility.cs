using Godot;
using System.Linq;

namespace VerletRope4;

public static class NodeUtility
{
    public static TNode FindOrCreateChild<TNode>(this Node node) where TNode : Node, new()
    {
        foreach (var child in node.GetChildren())
        {
            if (child is TNode targetChild)
            {
                return targetChild;
            }
        }

        var newTargetChild = new TNode();
        node.CallDeferred(Node.MethodName.AddChild, newTargetChild);
        return newTargetChild;
    }

    public static bool IsEditorSelected(this Node node)
    {
        if (!Engine.IsEditorHint())
        {
            return false;
        }

        var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
        return selectedNodes.Any(n => n == node);
    }
}