namespace OpenControls.Controls;

public sealed class UiColorButton : UiElement
{
    private bool _pressed;
    private bool _hovered;
    private bool _focused;
    private bool _dragArmed;
    private UiPoint _dragStartPosition;

    public UiColor Color { get; set; } = UiColor.White;
    public bool ShowAlpha { get; set; }
    public bool AllowColorDragDrop { get; set; }
    public bool AcceptDroppedColors { get; set; }
    public string DragPayloadType { get; set; } = "color";
    public int CheckerSize { get; set; } = 6;
    public UiColor CheckerColorLight { get; set; } = new UiColor(200, 200, 200);
    public UiColor CheckerColorDark { get; set; } = new UiColor(120, 120, 120);
    public int Padding { get; set; } = 2;
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }

    public UiColor Border { get; set; } = new UiColor(90, 100, 120);
    public UiColor HoverBorder { get; set; } = new UiColor(140, 150, 170);
    public UiColor PressedBorder { get; set; } = new UiColor(110, 120, 140);

    public event Action? Clicked;
    public event Action<UiColor>? ColorDropped;

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
            _dragArmed = AllowColorDragDrop;
            _dragStartPosition = input.MousePosition;
            context.Focus.RequestFocus(this);
        }

        if (_focused && input.Navigation.Activate)
        {
            Clicked?.Invoke();
        }

        if (_dragArmed && input.LeftDown)
        {
            int dx = Math.Abs(input.MousePosition.X - _dragStartPosition.X);
            int dy = Math.Abs(input.MousePosition.Y - _dragStartPosition.Y);
            int threshold = Math.Max(0, input.DragThreshold);
            if ((dx >= threshold || dy >= threshold) && !context.DragDrop.IsDragging)
            {
                context.DragDrop.BeginDrag(this, new UiDragDropPayload(DragPayloadType, Color), _dragStartPosition);
                _dragArmed = false;
                _pressed = false;
            }
        }

        if (AcceptDroppedColors && context.DragDrop.IsDragging && _hovered)
        {
            context.DragDrop.SetHoveredTarget(this);
            if (context.DragDrop.IsDropRequested)
            {
                UiDragDropPayload? payload = context.DragDrop.AcceptPayload(this, DragPayloadType);
                if (payload?.Data is UiColor color)
                {
                    Color = color;
                    ColorDropped?.Invoke(color);
                }
            }
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                Clicked?.Invoke();
            }

            _pressed = false;
            _dragArmed = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiColor border = _pressed ? PressedBorder : (_hovered ? HoverBorder : Border);
        if (border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, border, BorderThickness);
        }

        int inset = Math.Max(0, BorderThickness) + Math.Max(0, Padding);
        UiRect inner = new UiRect(
            Bounds.X + inset,
            Bounds.Y + inset,
            Math.Max(0, Bounds.Width - inset * 2),
            Math.Max(0, Bounds.Height - inset * 2));

        if (inner.Width > 0 && inner.Height > 0)
        {
            int innerRadius = Math.Max(0, CornerRadius - inset);
            if (ShowAlpha)
            {
                context.Renderer.FillRectCheckerboard(inner, CheckerSize, CheckerColorLight, CheckerColorDark);
            }

            UiRenderHelpers.FillRectRounded(context.Renderer, inner, innerRadius, Color);
        }

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
        _dragArmed = false;
    }
}
