using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTreeViewAccessoryTests
{
    private sealed class TestRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<string> DrawnTexts { get; } = new();

        public void FillRect(UiRect rect, UiColor color)
        {
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
            DrawnTexts.Add(text);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
            DrawnTexts.Add(text);
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
    public void TreeView_ScrollOffsetChanged_RaisesWhenSelectionScrollsIntoView()
    {
        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 0, 180, 60),
            ItemHeight = 20
        };

        for (int index = 0; index < 20; index++)
        {
            tree.RootItems.Add(new UiTreeViewItem($"Item {index:D2}"));
        }

        tree.NotifyTreeStructureChanged();

        int eventCount = 0;
        int lastOffset = -1;
        tree.ScrollOffsetChanged += offset =>
        {
            eventCount++;
            lastOffset = offset;
        };

        tree.SelectedIndex = 12;

        Assert.True(eventCount > 0);
        Assert.Equal(tree.ScrollOffset, lastOffset);
        Assert.True(tree.ScrollOffset > 0);
    }

    [Fact]
    public void TreeView_RenderWithSecondaryText_DrawsBothPrimaryAndSecondaryLabels()
    {
        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 0, 220, 60),
            ItemHeight = 22
        };

        tree.RootItems.Add(new UiTreeViewItem("Rotation")
        {
            SecondaryText = "61.716"
        });
        tree.NotifyTreeStructureChanged();

        TestRenderer renderer = new();
        tree.Render(new UiRenderContext(renderer, renderer.DefaultFont));

        Assert.Contains("Rotation", renderer.DrawnTexts);
        Assert.Contains("61.716", renderer.DrawnTexts);
    }
}
