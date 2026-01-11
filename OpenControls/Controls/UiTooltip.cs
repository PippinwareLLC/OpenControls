namespace OpenControls.Controls;

public sealed class UiTooltip : UiElement
{
    private object? _owner;
    private UiPoint? _offsetOverride;
    private UiRect _lastBounds;

    public string Text { get; private set; } = string.Empty;
    public UiPoint Anchor { get; private set; }
    public UiPoint Offset { get; set; } = new UiPoint(12, 16);
    public int Padding { get; set; } = 6;
    public int TextScale { get; set; } = 1;
    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Border { get; set; } = new UiColor(70, 80, 100);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }
    public bool ClampToParent { get; set; } = true;
    public bool IsOpen { get; private set; }
    public UiRect LastBounds => _lastBounds;

    public void Show(string text, UiPoint anchor, object owner, UiPoint? offsetOverride = null)
    {
        Text = text ?? string.Empty;
        Anchor = anchor;
        _owner = owner;
        _offsetOverride = offsetOverride;
        IsOpen = !string.IsNullOrEmpty(Text);
    }

    public void Hide(object owner)
    {
        if (_owner != owner)
        {
            return;
        }

        _owner = null;
        _offsetOverride = null;
        IsOpen = false;
    }

    public override void Render(UiRenderContext context)
    {
        // Tooltips render in the overlay pass only.
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !IsOpen || string.IsNullOrEmpty(Text))
        {
            return;
        }

        UiPoint offset = _offsetOverride ?? Offset;
        int textWidth = context.Renderer.MeasureTextWidth(Text, TextScale);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int width = textWidth + Padding * 2;
        int height = textHeight + Padding * 2;
        UiRect bounds = new UiRect(Anchor.X + offset.X, Anchor.Y + offset.Y, width, height);

        if (ClampToParent && Parent != null)
        {
            bounds = ClampToBounds(bounds, Parent.Bounds);
        }

        _lastBounds = bounds;

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, bounds, CornerRadius, Background);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, bounds, CornerRadius, Border, BorderThickness);
        }

        UiPoint textPos = new UiPoint(bounds.X + Padding, bounds.Y + Padding);
        context.Renderer.DrawText(Text, textPos, TextColor, TextScale);
    }

    private static UiRect ClampToBounds(UiRect bounds, UiRect container)
    {
        int x = bounds.X;
        int y = bounds.Y;

        if (bounds.Right > container.Right)
        {
            x = container.Right - bounds.Width;
        }

        if (bounds.Bottom > container.Bottom)
        {
            y = container.Bottom - bounds.Height;
        }

        if (x < container.X)
        {
            x = container.X;
        }

        if (y < container.Y)
        {
            y = container.Y;
        }

        return new UiRect(x, y, bounds.Width, bounds.Height);
    }
}
