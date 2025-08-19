using Godot;
using System.Linq;

namespace VerletRope4.Utility;

public static class NodeUtility
{
    public static TNode FindOrCreateChild<TNode>(this Node node, string editorName = null) where TNode : Node, new()
    {
        foreach (var child in node.GetChildren())
        {
            if (child is TNode targetChild)
            {
                return targetChild;
            }
        }
        
        var newTargetChild = new TNode();
        Callable.From(() =>
        {
            node.AddChild(newTargetChild);
            if (!string.IsNullOrEmpty(editorName))
            {
                newTargetChild.Owner = node.GetTree().EditedSceneRoot;
                newTargetChild.Name = editorName;
            }
        }).CallDeferred();

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

    public static void SetSubtreeOwner(this Node node, Node owner)
    {
        node.Owner = owner;

        foreach (var child in node.GetChildren())
        {
            SetSubtreeOwner(child, owner);
        }
    } 
}