using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiMenuLayoutTests
{
    [Fact]
    public void PopupWidth_UsesResolvedFontAndExplicitShortcutTrailingPadding()
    {
        const string label = "Export PNG...";
        const string shortcut = "Primary+Shift+E";
        UiMenuBar menu = CreatePopupMenu(label, shortcut);
        menu.FallbackCharWidth = 1;
        menu.ItemPadding = 5;
        menu.CheckmarkAreaWidth = 9;
        menu.ShortcutPadding = 7;
        menu.ShortcutTrailingPadding = 11;

        Update(menu);

        UiRect layout = Assert.Single(menu.GetDebugOpenLayoutBounds());
        int expectedWidth = menu.ItemPadding
            + menu.CheckmarkAreaWidth
            + UiFont.Default.MeasureTextWidth(label)
            + menu.ItemPadding
            + menu.ShortcutPadding
            + UiFont.Default.MeasureTextWidth(shortcut)
            + menu.ShortcutTrailingPadding;

        Assert.Equal(expectedWidth, layout.Width);
    }

    [Fact]
    public void PopupWidthAndRendering_ReserveMeasuredTrailingText()
    {
        const string label = "Subdivision";
        const string shortcut = "Ctrl+2";
        const string trailingText = "#";
        UiMenuBar menu = CreatePopupMenu(label, shortcut);
        menu.Items[0].TrailingText = trailingText;
        menu.ItemPadding = 5;
        menu.CheckmarkAreaWidth = 9;
        menu.ShortcutPadding = 7;
        menu.ShortcutTrailingPadding = 6;
        menu.TrailingTextPadding = 10;
        menu.TrailingTextTrailingPadding = 11;

        Update(menu);

        UiRect layout = Assert.Single(menu.GetDebugOpenLayoutBounds());
        int expectedWidth = menu.ItemPadding
            + menu.CheckmarkAreaWidth
            + UiFont.Default.MeasureTextWidth(label)
            + menu.ItemPadding
            + menu.ShortcutPadding
            + UiFont.Default.MeasureTextWidth(shortcut)
            + menu.ShortcutTrailingPadding
            + menu.TrailingTextPadding
            + UiFont.Default.MeasureTextWidth(trailingText)
            + menu.TrailingTextTrailingPadding;
        Assert.Equal(expectedWidth, layout.Width);

        CaptureRenderer renderer = new();
        menu.RenderOverlay(new UiRenderContext(renderer, renderer.DefaultFont));
        TextDraw shortcutDraw = Assert.Single(renderer.TextDraws, draw => draw.Text == shortcut);
        TextDraw trailingDraw = Assert.Single(renderer.TextDraws, draw => draw.Text == trailingText);
        UiRect shortcutInk = shortcutDraw.Font.MeasureTextInkBounds(shortcut, shortcutDraw.Scale);
        UiRect trailingInk = trailingDraw.Font.MeasureTextInkBounds(trailingText, trailingDraw.Scale);
        int shortcutInkRight = shortcutDraw.Position.X + shortcutInk.Right;
        int trailingInkLeft = trailingDraw.Position.X + trailingInk.X;
        int trailingInkRight = trailingDraw.Position.X + trailingInk.Right;

        Assert.True(
            trailingInkLeft - shortcutInkRight >= menu.ShortcutTrailingPadding + menu.TrailingTextPadding,
            "Shortcut and trailing text must retain both configured gaps.");
        Assert.True(layout.Right - trailingInkRight >= menu.TrailingTextTrailingPadding);
    }

    [Fact]
    public void LongShortcut_RenderingLeavesConfiguredTrailingGapBeforePopupBorder()
    {
        const string shortcut = "Primary+Shift+Alt+Super+F12";
        UiMenuBar menu = CreatePopupMenu("Export for review", shortcut);
        menu.DropdownMinWidth = 40;
        menu.ShortcutTrailingPadding = 13;

        Update(menu);

        UiRect layout = Assert.Single(menu.GetDebugOpenLayoutBounds());
        CaptureRenderer renderer = new();
        menu.RenderOverlay(new UiRenderContext(renderer, renderer.DefaultFont));

        TextDraw shortcutDraw = Assert.Single(renderer.TextDraws, draw => draw.Text == shortcut);
        UiRect shortcutInk = shortcutDraw.Font.MeasureTextInkBounds(shortcut, shortcutDraw.Scale);
        int shortcutInkRight = shortcutDraw.Position.X + shortcutInk.Right;

        Assert.True(layout.Width > menu.DropdownMinWidth);
        Assert.True(
            layout.Right - shortcutInkRight >= menu.ShortcutTrailingPadding,
            $"Expected at least {menu.ShortcutTrailingPadding}px after shortcut ink, got {layout.Right - shortcutInkRight}px.");
    }

    [Fact]
    public void PopupVerticalPadding_ExpandsContainerWithoutChangingCompactRowBounds()
    {
        UiMenuBar menu = CreatePopupMenu("Open", string.Empty);
        menu.Items.Add(new UiMenuBar.MenuItem { Text = "Save" });
        menu.DropdownItemHeight = 20;
        menu.DropdownVerticalPadding = 4;

        Update(menu);

        UiRect layout = Assert.Single(menu.GetDebugOpenLayoutBounds());
        IReadOnlyList<UiRect> items = menu.GetDebugOpenItemBounds();

        Assert.Equal(new UiRect(20, 30, layout.Width, 48), layout);
        Assert.Equal(2, items.Count);
        Assert.Equal(new UiRect(20, 34, layout.Width, 20), items[0]);
        Assert.Equal(new UiRect(20, 54, layout.Width, 20), items[1]);
        Assert.Equal(menu.DropdownVerticalPadding, items[0].Top - layout.Top);
        Assert.Equal(menu.DropdownVerticalPadding, layout.Bottom - items[1].Bottom);
    }

    [Fact]
    public void PopupVerticalPadding_DoesNotHoverOrActivateAdjacentRows()
    {
        int activations = 0;
        UiMenuBar menu = CreatePopupMenu("Open", string.Empty);
        menu.DropdownItemHeight = 20;
        menu.DropdownVerticalPadding = 4;
        menu.ClosePopupOnItemClick = false;
        menu.Items[0].Clicked = _ => activations++;
        UiFocusManager focus = new();

        Update(menu, focus, new UiInputState());
        UiRect layout = Assert.Single(menu.GetDebugOpenLayoutBounds());
        UiRect row = Assert.Single(menu.GetDebugOpenItemBounds());

        Update(menu, focus, Click(new UiPoint(row.X + 2, layout.Top + 1)));
        Assert.Equal(0, activations);
        Assert.True(menu.IsPopupOpen);
        Assert.False(menu.TryGetDebugHighlightedItemBounds(out _));

        Update(menu, focus, Click(new UiPoint(row.X + 2, layout.Bottom - 1)));
        Assert.Equal(0, activations);
        Assert.True(menu.IsPopupOpen);
        Assert.False(menu.TryGetDebugHighlightedItemBounds(out _));

        Update(menu, focus, Click(new UiPoint(row.X + 2, row.Y + row.Height / 2)));
        Assert.Equal(1, activations);
    }

    private static UiMenuBar CreatePopupMenu(string label, string shortcut)
    {
        UiMenuBar menu = new()
        {
            DisplayMode = UiMenuDisplayMode.Popup,
            DropdownMinWidth = 1,
            ClampToParent = false
        };
        menu.Items.Add(new UiMenuBar.MenuItem
        {
            Text = label,
            Shortcut = shortcut
        });
        menu.OpenContext(new UiPoint(20, 30));
        return menu;
    }

    private static void Update(UiMenuBar menu)
    {
        Update(menu, new UiFocusManager(), new UiInputState());
    }

    private static void Update(UiMenuBar menu, UiFocusManager focus, UiInputState input)
    {
        menu.Update(new UiUpdateContext(
            input,
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }

    private static UiInputState Click(UiPoint point)
    {
        return new UiInputState
        {
            MousePosition = point,
            ScreenMousePosition = point,
            LeftClicked = true,
            LeftDown = true
        };
    }

    private readonly record struct TextDraw(string Text, UiPoint Position, int Scale, UiFont Font);

    private sealed class CaptureRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<TextDraw> TextDraws { get; } = new();

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
            DrawText(text, position, color, scale, DefaultFont);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
            TextDraws.Add(new TextDraw(text, position, scale, font ?? DefaultFont));
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, DefaultFont);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            return (font ?? DefaultFont).MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, DefaultFont);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            return (font ?? DefaultFont).MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }
}
