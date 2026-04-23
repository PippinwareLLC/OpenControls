namespace OpenControls.Controls;

public sealed class UiButton : UiElement
{
    private bool _pressed;
    private bool _hovered;
    private bool _focused;
    private bool _clickedThisFrame;
    private string _text = string.Empty;
    private UiColor _background = new UiColor(52, 60, 78);
    private UiColor _hoverBackground = new UiColor(70, 82, 108);
    private UiColor _pressedBackground = new UiColor(40, 48, 62);
    private UiColor _border = new UiColor(90, 100, 120);
    private UiColor _textColor = UiColor.White;
    private int _textScale = 1;
    private int _cornerRadius;

    public string Text
    {
        get => _text;
        set => SetInvalidatingValue(ref _text, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiColor Background
    {
        get => _background;
        set => SetInvalidatingValue(ref _background, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor HoverBackground
    {
        get => _hoverBackground;
        set => SetInvalidatingValue(ref _hoverBackground, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor PressedBackground
    {
        get => _pressedBackground;
        set => SetInvalidatingValue(ref _pressedBackground, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor Border
    {
        get => _border;
        set => SetInvalidatingValue(ref _border, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor TextColor
    {
        get => _textColor;
        set => SetInvalidatingValue(ref _textColor, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int TextScale
    {
        get => _textScale;
        set => SetInvalidatingValue(ref _textScale, Math.Max(1, value), UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set => SetInvalidatingValue(ref _cornerRadius, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

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

        UiFont font = ResolveFont(context.DefaultFont);
        int availableWidth = Math.Max(0, Bounds.Width - 8);
        string drawText = UiRenderHelpers.BuildElidedText(Text, availableWidth, TextScale, font);
        int textWidth = context.Renderer.MeasureTextWidth(drawText, TextScale, font);
        int textX = Bounds.X + (Bounds.Width - textWidth) / 2;
        int textY = UiRenderHelpers.GetVerticallyCenteredTextY(Bounds, drawText, TextScale, font);
        context.Renderer.PushClip(Bounds);
        context.Renderer.DrawText(drawText, new UiPoint(textX, textY), TextColor, TextScale, font);
        context.Renderer.PopClip();

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        if (_focused)
        {
            return;
        }

        _focused = true;
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Volatility);
    }

    protected internal override void OnFocusLost()
    {
        bool changed = _focused || _pressed;
        _focused = false;
        _pressed = false;
        if (changed)
        {
            Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Volatility);
        }
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
