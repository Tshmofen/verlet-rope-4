using Godot;

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
}