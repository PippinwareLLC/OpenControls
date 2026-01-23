namespace OpenControls.Controls;

public sealed class UiArrowButton : UiElement
{
    private bool _pressed;
    private bool _hovered;
    private bool _focused;

    public UiArrowDirection Direction { get; set; } = UiArrowDirection.Right;
    public UiColor Background { get; set; } = new UiColor(52, 60, 78);
    public UiColor HoverBackground { get; set; } = new UiColor(70, 82, 108);
    public UiColor PressedBackground { get; set; } = new UiColor(40, 48, 62);
    public UiColor Border { get; set; } = new UiColor(90, 100, 120);
    public UiColor ArrowColor { get; set; } = UiColor.White;
    public int ArrowSize { get; set; } = 8;
    public int CornerRadius { get; set; }

    public event Action? Clicked;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
            context.Focus.RequestFocus(this);
        }

        if (_focused && input.Navigation.Activate)
        {
            Clicked?.Invoke();
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
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

        int size = ArrowSize > 0 ? ArrowSize : Math.Min(Bounds.Width, Bounds.Height);
        int arrowX = Bounds.X + (Bounds.Width - size) / 2;
        int arrowY = Bounds.Y + (Bounds.Height - size) / 2;
        UiArrow.DrawTriangle(context.Renderer, arrowX, arrowY, size, Direction, ArrowColor);

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
}
