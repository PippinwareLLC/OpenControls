using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiSelectionDepthTests
{
    [Fact]
    public void SelectionModel_ScopesStayIsolated()
    {
        UiSelectionModel selection = new();
        selection.SetItemCount(8, "left");
        selection.SetItemCount(8, "right");

        selection.SelectRange(1, 3, "left");
        selection.SelectSingle(5, "right");

        Assert.Equal(new[] { 1, 2, 3 }, selection.GetSelectedIndices("left"));
        Assert.Equal(new[] { 5 }, selection.GetSelectedIndices("right"));

        selection.Clear("left");

        Assert.Empty(selection.GetSelectedIndices("left"));
        Assert.Equal(new[] { 5 }, selection.GetSelectedIndices("right"));
    }

    [Fact]
    public void SelectionModel_RemoveRangeAndDeleteSelectedKeepSelectionStable()
    {
        UiSelectionModel selection = new()
        {
            AnchorPersistence = UiSelectionAnchorPersistence.ClampToNearestValid
        };
        selection.SetItemCount(8, "items");
        selection.SelectRange(3, 5, "items");

        selection.RemoveRange(0, 2, "items");

        Assert.Equal(new[] { 1, 2, 3 }, selection.GetSelectedIndices("items"));
        Assert.Equal(3, selection.GetPrimaryIndex("items"));
        Assert.Equal(1, selection.GetAnchorIndex("items"));

        IReadOnlyList<int> deleted = selection.DeleteSelected("items");

        Assert.Equal(new[] { 1, 2, 3 }, deleted);
        Assert.Equal(3, selection.GetItemCount("items"));
        Assert.Equal(new[] { 1 }, selection.GetSelectedIndices("items"));
    }

    [Fact]
    public void ListAndTable_CanShareSelectionThroughScope()
    {
        UiSelectionModel selection = new();
        const string scope = "shared";

        UiListBox listBox = new()
        {
            Items = new[] { "Alpha", "Beta", "Gamma", "Delta" },
            SelectionModel = selection,
            SelectionScope = scope
        };
        selection.SetItemCount(listBox.Items.Count, scope);

        UiTable table = new()
        {
            Rows = new[]
            {
                new UiTableRow("Alpha", "Scene"),
                new UiTableRow("Beta", "Prefab"),
                new UiTableRow("Gamma", "Material"),
                new UiTableRow("Delta", "Texture")
            },
            SelectionModel = selection,
            SelectionScope = scope
        };
        table.Columns.Add(new UiTableColumn("Name"));
        table.Columns.Add(new UiTableColumn("Type"));
        selection.SetItemCount(table.Rows.Count, scope);

        listBox.SelectedIndex = 2;
        Assert.Equal(2, table.SelectedIndex);

        table.SelectedIndex = 1;
        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void ListBoxScopesRemainIndependentWithinSharedModel()
    {
        UiSelectionModel selection = new();

        UiListBox left = new()
        {
            Items = new[] { "A", "B", "C" },
            SelectionModel = selection,
            SelectionScope = "left"
        };
        UiListBox right = new()
        {
            Items = new[] { "A", "B", "C" },
            SelectionModel = selection,
            SelectionScope = "right"
        };

        selection.SetItemCount(left.Items.Count, "left");
        selection.SetItemCount(right.Items.Count, "right");

        left.SelectedIndex = 2;
        right.SelectedIndex = 0;

        Assert.Equal(2, left.SelectedIndex);
        Assert.Equal(0, right.SelectedIndex);
        Assert.Equal(new[] { 2 }, selection.GetSelectedIndices("left"));
        Assert.Equal(new[] { 0 }, selection.GetSelectedIndices("right"));
    }

    [Fact]
    public void TreeView_RevealItemExpandsParentsAndSelectsTarget()
    {
        UiSelectionModel selection = new();
        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 0, 220, 110),
            SelectionModel = selection,
            SelectionScope = "tree"
        };

        UiTreeViewItem root = new("Root");
        UiTreeViewItem branch = new("Branch");
        UiTreeViewItem leaf = new("Leaf");
        branch.Children.Add(leaf);
        root.Children.Add(branch);
        tree.RootItems.Add(root);

        Assert.True(tree.RevealItem(leaf, select: true));

        Assert.True(root.IsOpen);
        Assert.True(branch.IsOpen);
        Assert.Same(leaf, tree.SelectedItem);

        int expandedCount = tree.VisibleItemCount;
        Assert.True(expandedCount >= 3);

        tree.CollapseAll();
        Assert.Equal(1, tree.VisibleItemCount);

        tree.ExpandAll();
        Assert.Equal(expandedCount, tree.VisibleItemCount);
    }
}
