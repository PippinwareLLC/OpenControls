using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiListBoxScrollbarTests
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
    public void ListBox_RenderWithAlwaysVisibleScrollbar_DrawsScrollbarTrack()
    {
        UiListBox listBox = new()
        {
            Bounds = new UiRect(0, 0, 120, 60),
            VerticalScrollbar = UiScrollbarVisibility.Always,
            ScrollbarThickness = 12,
            Items = new[] { "One", "Two", "Three", "Four" }
        };

        TestRenderer renderer = new();
        listBox.Render(new UiRenderContext(renderer, renderer.DefaultFont));

        Assert.Contains(new UiRect(108, 0, 12, 60), renderer.FilledRects);
    }

    [Fact]
    public void ListBox_ScrollOffsetChanged_RaisesWhenSelectionScrollsIntoView()
    {
        UiListBox listBox = new()
        {
            Bounds = new UiRect(0, 0, 180, 60),
            ItemHeight = 20,
            Items = Enumerable.Range(0, 20).Select(static i => $"Item {i:D2}").ToArray()
        };

        int eventCount = 0;
        int lastOffset = -1;
        listBox.ScrollOffsetChanged += offset =>
        {
            eventCount++;
            lastOffset = offset;
        };

        listBox.SelectedIndex = 12;

        Assert.True(eventCount > 0);
        Assert.Equal(listBox.ScrollOffset, lastOffset);
        Assert.True(listBox.ScrollOffset > 0);
    }
}
