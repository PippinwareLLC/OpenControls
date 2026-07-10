using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiNestedPopupTests
{
    private sealed class ClipRecordingRenderer : IUiRenderer
    {
        private readonly Stack<UiRect> _clips = new();

        public UiFont DefaultFont { get; set; } = UiFont.Default;
        public List<(UiRect Rect, UiColor Color, UiRect? Clip)> Fills { get; } = new();

        public void FillRect(UiRect rect, UiColor color) =>
            Fills.Add((rect, color, _clips.Count > 0 ? _clips.Peek() : null));

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1) { }
        public void FillRectGradient(
            UiRect rect,
            UiColor topLeft,
            UiColor topRight,
            UiColor bottomLeft,
            UiColor bottomRight) => FillRect(rect, topLeft);
        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB) =>
            FillRect(rect, colorA);
        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1) { }
        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font) { }
        public int MeasureTextWidth(string text, int scale = 1) =>
            DefaultFont.MeasureTextWidth(text, scale);
        public int MeasureTextWidth(string text, int scale, UiFont? font) =>
            (font ?? DefaultFont).MeasureTextWidth(text, scale);
        public int MeasureTextHeight(int scale = 1) => DefaultFont.MeasureTextHeight(scale);
        public int MeasureTextHeight(int scale, UiFont? font) =>
            (font ?? DefaultFont).MeasureTextHeight(scale);
        public void PushClip(UiRect rect) => _clips.Push(rect);
        public void PopClip() => _clips.Pop();
    }

    [Fact]
    public void EscapeClosesDeepestPopupBeforeModalAndDoesNotLeaveAnOrphanInputLayer()
    {
        (UiContext context, UiModal modal, UiComboBox combo) = CreateOpenNestedPopup();

        Assert.True(modal.IsOpen);
        Assert.True(combo.IsOpen);
        Assert.NotSame(modal, context.ActiveInputLayer);

        context.Update(EscapeInput());

        Assert.True(modal.IsOpen);
        Assert.False(combo.IsOpen);
        Assert.Same(modal, context.ActiveInputLayer);

        context.Update(EscapeInput());

        Assert.False(modal.IsOpen);
        Assert.False(combo.IsOpen);
        Assert.Null(context.ActiveInputLayer);

        context.Update(new UiInputState { KeysPressed = [UiKey.V] });
        Assert.Null(context.ActiveInputLayer);
        Assert.False(context.WantTextInput);
    }

    [Fact]
    public void NestedPopupOverlayRendersOutsideTheModalContentClip()
    {
        (UiContext context, UiModal modal, UiComboBox combo) = CreateOpenNestedPopup();
        Assert.True(combo.PopupBounds.Bottom > modal.Bounds.Bottom);
        UiColor popupColor = new(231, 17, 93);
        combo.DropdownBackground = popupColor;

        ClipRecordingRenderer renderer = new();
        context.Render(renderer);

        (UiRect Rect, UiColor Color, UiRect? Clip) popupFill = Assert.Single(
            renderer.Fills,
            fill => fill.Rect.Equals(combo.PopupBounds) && fill.Color.Equals(popupColor));
        Assert.Null(popupFill.Clip);
    }

    [Fact]
    public void PopupShadowRendersOutsideAndBeforeTheOpaquePopupBody()
    {
        UiPanel root = new() { Bounds = new UiRect(0, 0, 320, 200) };
        UiColor body = new(50, 60, 70);
        UiColor shadow = new(3, 4, 5, 100);
        UiPopup popup = new()
        {
            Bounds = new UiRect(80, 60, 120, 70),
            Background = body,
            Border = UiColor.Transparent,
            ShadowColor = shadow,
            ShadowBlur = 1,
            ShadowOffset = new UiPoint(0, 3)
        };
        root.AddChild(popup);
        popup.Open();
        UiContext context = new(root);
        context.Update(new UiInputState());
        ClipRecordingRenderer renderer = new();

        context.Render(renderer);

        int shadowIndex = renderer.Fills.FindIndex(fill => fill.Color.Equals(shadow));
        int bodyIndex = renderer.Fills.FindIndex(fill => fill.Color.Equals(body) && fill.Rect.Equals(popup.Bounds));
        Assert.True(shadowIndex >= 0);
        Assert.True(bodyIndex > shadowIndex);
        Assert.Contains(renderer.Fills, fill =>
            fill.Color.Equals(shadow)
            && (fill.Rect.Left < popup.Bounds.Left || fill.Rect.Right > popup.Bounds.Right));
    }

    private static (UiContext Context, UiModal Modal, UiComboBox Combo) CreateOpenNestedPopup()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiButton background = new()
        {
            Bounds = new UiRect(10, 10, 100, 28),
            Text = "Background"
        };
        UiModal modal = new()
        {
            Bounds = new UiRect(120, 100, 320, 150),
            ClipChildren = true
        };
        UiComboBox combo = new()
        {
            Bounds = new UiRect(160, 205, 180, 26),
            Items = ["Normal", "Dissolve", "Darken", "Multiply", "Burn", "Lighten", "Screen", "Overlay"],
            SelectedIndex = 0,
            MaxVisibleItems = 8
        };
        modal.AddChild(combo);
        host.AddChild(background);
        host.AddChild(modal);

        UiContext context = new(host);
        context.Focus.RequestFocus(background);
        modal.Open();
        context.Update(new UiInputState());
        combo.Open();
        context.Update(new UiInputState());
        return (context, modal, combo);
    }

    private static UiInputState EscapeInput() => new()
    {
        KeysPressed = [UiKey.Escape],
        Navigation = new UiNavigationInput { Escape = true }
    };
}
