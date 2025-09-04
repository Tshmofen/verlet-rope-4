using Godot;
using System.Linq;

namespace VerletRope4.Utility;

public static class NodeUtility
{
    public static TNode FindOrCreateChild<TNode>(this Node node, string editorName = null) where TNode : Node, new()
    {
        var foundChild = FindChild<TNode>(node);
        return foundChild ?? CreateChild<TNode>(node, editorName);
    }

    public static TNode FindChild<TNode>(this Node node) where TNode : Node, new()
    {
        foreach (var child in node.GetChildren())
        {
            if (child is TNode targetChild)
            {
                return targetChild;
            }
        }

        return null;
    }

    public static TNode CreateChild<TNode>(this Node node, string editorName) where TNode : Node, new()
    {
        var newTargetChild = new TNode();

        void AssignNode()
        {
            node.AddChild(newTargetChild);

            if (string.IsNullOrEmpty(editorName))
            {
                return;
            }

            newTargetChild.Owner = node.GetTree().EditedSceneRoot;
            newTargetChild.Name = editorName;
        }
        Callable.From(AssignNode).CallDeferred();

        return newTargetChild;
    }

    public static void RemoveChildByMeta(this Node node, StringName metaName, long metaValue)
    {
        var matchingNode = node
            .GetChildren()
            .Where(c => c.HasMeta(metaName))
            .FirstOrDefault(c => c.GetMeta(metaName).AsInt64() == metaValue);
        matchingNode?.QueueFree();
    }

    public static bool IsEditorSelected(this Node node)
    {
        #if TOOLS

        if (!Engine.IsEditorHint())
        {
            return false;
        }

        var selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
        return selectedNodes.Any(n => n == node);

        #else

        return false;

        #endif
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