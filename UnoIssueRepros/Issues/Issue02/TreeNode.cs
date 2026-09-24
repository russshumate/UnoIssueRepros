namespace UnoIssueRepros.Issues.Issue02;

public sealed record TreeNode(string Name, IReadOnlyList<TreeNode> Children)
{
    public TreeNode(string name) : this(name, []) { }

    /// <summary>Three levels deep, like an order with line items and their components.</summary>
    public static TreeNode Sample { get; } = new("Order 1001",
    [
        new TreeNode("Line: Door kit",
        [
            new TreeNode("Component: Hinge"),
            new TreeNode("Component: Handle"),
        ]),
        new TreeNode("Line: Paint",
        [
            new TreeNode("Component: Primer"),
        ]),
    ]);
}
