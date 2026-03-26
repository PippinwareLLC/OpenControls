namespace OpenControls.Controls;

public sealed class UiButton : UiElement
{
    private bool _pressed;
    private bool _hovered;
    private bool _focused;
    private bool _clickedThisFrame;

    public string Text { get; set; } = string.Empty;
    public UiColor Background { get; set; } = new UiColor(52, 60, 78);
    public UiColor HoverBackground { get; set; } = new UiColor(70, 82, 108);
    public UiColor PressedBackground { get; set; } = new UiColor(40, 48, 62);
    public UiColor Border { get; set; } = new UiColor(90, 100, 120);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int TextScale { get; set; } = 1;
    public int CornerRadius { get; set; }

    public event Action? Clicked;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _clickedThisFrame = false;
        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
            context.Focus.RequestFocus(this);
        }

        if (_focused && input.Navigation.Activate)
        {
            _clickedThisFrame = true;
            Clicked?.Invoke();
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                _clickedThisFrame = true;
                Clicked?.Invoke();
            }

            _pressed = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiColor fill = _pressed ? PressedBackground : (_hovered ? HoverBackground : Background);
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, fill);
        UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);

        int textWidth = context.Renderer.MeasureTextWidth(Text, TextScale);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textX = Bounds.X + (Bounds.Width - textWidth) / 2;
        int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
        context.Renderer.DrawText(Text, new UiPoint(textX, textY), TextColor, TextScale);

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _pressed = false;
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        if (_hovered || _pressed || focused)
        {
            cursor = UiMouseCursor.Hand;
            return true;
        }

        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (_pressed)
        {
            status |= UiItemStatusFlags.Active | UiItemStatusFlags.Pressed;
        }

        if (_clickedThisFrame)
        {
            status |= UiItemStatusFlags.Clicked;
        }

        return status;
    }
}
