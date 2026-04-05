using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiVirtualizationTests
{
    private sealed class TestRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<UiRect> FilledRects { get; } = new();

        public void FillRect(UiRect rect, UiColor color)
        {
            FilledRects.Add(rect);
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, DefaultFont);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, DefaultFont);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }

    [Fact]
    public void Clipper_FixedHeightRangeComputesVisibleAndMaterializedIndices()
    {
        UiClipRange range = UiClipper.FixedHeight(itemCount: 100, itemExtent: 20, viewportStart: 45, viewportExtent: 55, overscanItems: 1);

        Assert.Equal(2, range.FirstVisibleIndex);
        Assert.Equal(4, range.LastVisibleIndex);
        Assert.Equal(1, range.FirstMaterializedIndex);
        Assert.Equal(5, range.LastMaterializedIndex);
        Assert.Equal(2000, range.ContentExtent);
    }

    [Fact]
    public void Clipper_EnsureVisibleClampsLastItemWithoutOverscroll()
    {
        int scroll = UiClipper.EnsureVisible(itemCount: 100, itemExtent: 20, viewportExtent: 55, scrollOffset: 0, index: 99);

        Assert.Equal(1945, scroll);
    }

    [Fact]
    public void DefaultClipRange_DoesNotPretendToHaveMaterializedItems()
    {
        UiClipRange range = default;

        Assert.False(range.HasVisibleItems);
        Assert.False(range.HasMaterializedItems);
    }

    [Fact]
    public void ListBox_LargeSelectionSurvivesClippedRows()
    {
        UiSelectionModel selection = new();
        UiListBox listBox = new()
        {
            Bounds = new UiRect(0, 0, 180, 60),
            ItemHeight = 20,
            OverscanItems = 1,
            SelectionModel = selection,
            Items = Enumerable.Range(0, 1000).Select(static i => $"Item {i:D4}").ToArray()
        };

        Update(listBox, new UiInputState());
        listBox.SelectedIndex = 512;
        Update(listBox, new UiInputState());

        Assert.Equal(512, listBox.SelectedIndex);
        Assert.True(listBox.FirstVisibleIndex <= 512);
        Assert.True(listBox.LastVisibleIndex >= 512);

        listBox.ScrollOffset = 0;
        Update(listBox, new UiInputState());

        Assert.Equal(512, listBox.SelectedIndex);
        Assert.True(selection.IsSelected(512));
    }

    [Fact]
    public void TreeView_OpenToggleAndSelectionWorkAcrossVirtualizedRows()
    {
        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 0, 220, 66),
            ItemHeight = 22,
            OverscanItems = 1
        };

        for (int i = 0; i < 40; i++)
        {
            UiTreeViewItem parent = new($"Group {i:D2}") { IsOpen = i < 3 };
            for (int child = 0; child < 12; child++)
            {
                parent.Children.Add(new UiTreeViewItem($"Item {i:D2}-{child:D2}"));
            }

            tree.RootItems.Add(parent);
        }

        Update(tree, new UiInputState());
        Assert.True(tree.VisibleItemCount > tree.LastVisibleIndex);

        UiRect firstArrow = new UiRect(tree.Bounds.X + tree.Padding, tree.Bounds.Y + 7, tree.ArrowSize, tree.ArrowSize);
        Update(tree, new UiInputState
        {
            MousePosition = new UiPoint(firstArrow.X + 1, firstArrow.Y + 1),
            ScreenMousePosition = new UiPoint(firstArrow.X + 1, firstArrow.Y + 1),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.False(tree.RootItems[0].IsOpen);

        tree.SelectedIndex = 18;
        Update(tree, new UiInputState());
        Assert.Equal(18, tree.SelectedIndex);
        Assert.NotNull(tree.SelectedItem);
        Assert.True(tree.VisibleItemCount < 40 + 36);
    }

    [Fact]
    public void TreeView_RenderBeforeFirstUpdateDoesNotThrow()
    {
        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 0, 220, 88),
            ItemHeight = 22,
            OverscanItems = 1
        };

        UiTreeViewItem root = new("Gameplay") { IsOpen = true };
        root.Children.Add(new UiTreeViewItem("Player"));
        root.Children.Add(new UiTreeViewItem("Enemies"));
        tree.RootItems.Add(root);

        TestRenderer renderer = new();
        UiRenderContext context = new(renderer, renderer.DefaultFont);

        tree.Render(context);

        Assert.Equal(3, tree.VisibleItemCount);
    }

    [Fact]
    public void TreeView_HierarchyLinesUseAncestorContinuationForChildRows()
    {
        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 0, 220, 120),
            ItemHeight = 20,
            OverscanItems = 1,
            ShowHierarchyLines = true
        };

        UiTreeViewItem firstRoot = new("First") { IsOpen = true };
        firstRoot.Children.Add(new UiTreeViewItem("Child"));
        tree.RootItems.Add(firstRoot);
        tree.RootItems.Add(new UiTreeViewItem("Second"));

        TestRenderer renderer = new();
        UiRenderContext context = new(renderer, renderer.DefaultFont);

        tree.Render(context);

        int connectorX = tree.Bounds.X + Math.Max(0, tree.Padding) + Math.Max(4, tree.ArrowSize) / 2;
        UiRect expectedAncestorLine = new(connectorX, tree.Bounds.Y + tree.ItemHeight, Math.Max(1, tree.HierarchyLineThickness), tree.ItemHeight);

        Assert.Contains(expectedAncestorLine, renderer.FilledRects);
    }

    private static void Update(UiElement element, UiInputState input)
    {
        element.Update(new UiUpdateContext(
            input,
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }
}
