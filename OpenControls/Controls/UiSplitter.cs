namespace OpenControls.Controls;

public enum UiSplitterOrientation
{
    Vertical,
    Horizontal
}

public sealed class UiSplitter : UiElement
{
    private bool _hovered;
    private bool _dragging;
    private int _lastPosition;

    public UiSplitterOrientation Orientation { get; set; } = UiSplitterOrientation.Vertical;
    public UiColor Color { get; set; } = new UiColor(48, 56, 72);
    public UiColor HoverColor { get; set; } = new UiColor(70, 82, 105);
    public UiColor ActiveColor { get; set; } = new UiColor(96, 114, 145);
    public UiColor HoverTrackColor { get; set; } = new UiColor(70, 82, 105, 34);
    public UiColor ActiveTrackColor { get; set; } = new UiColor(96, 114, 145, 48);
    public int VisualThickness { get; set; } = 2;
    public int ActiveVisualThickness { get; set; } = 3;
    public int VisualInset { get; set; } = 2;

    public bool IsDragging => _dragging;
    public override bool CapturesPointerInput => true;

    public event Action<int>? Dragged;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        if (!_dragging && input.LeftClicked && _hovered)
        {
            _dragging = true;
            _lastPosition = GetAxisPosition(input.MousePosition);
            context.Focus.RequestFocus(null);
        }

        if (_dragging && input.LeftDown)
        {
            int current = GetAxisPosition(input.MousePosition);
            int delta = current - _lastPosition;
            if (delta != 0)
            {
                _lastPosition = current;
                Dragged?.Invoke(delta);
            }
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiColor color = _dragging ? ActiveColor : (_hovered ? HoverColor : Color);
        UiColor trackColor = _dragging ? ActiveTrackColor : (_hovered ? HoverTrackColor : UiColor.Transparent);
        if (trackColor.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, 2, trackColor);
        }

        UiRect lineRect = GetVisualRect(_dragging ? ActiveVisualThickness : VisualThickness);
        if (lineRect.Width > 0 && lineRect.Height > 0)
        {
            UiRenderHelpers.FillRectRounded(
                context.Renderer,
                lineRect,
                Math.Min(2, Math.Min(lineRect.Width, lineRect.Height) / 2),
                color);
        }

        base.Render(context);
    }

    private int GetAxisPosition(UiPoint point)
    {
        return Orientation == UiSplitterOrientation.Vertical ? point.X : point.Y;
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        if (_hovered || _dragging)
        {
            cursor = Orientation == UiSplitterOrientation.Vertical
                ? UiMouseCursor.ResizeEW
                : UiMouseCursor.ResizeNS;
            return true;
        }

        cursor = UiMouseCursor.Arrow;
        return false;
    }

    private UiRect GetVisualRect(int requestedThickness)
    {
        int inset = Math.Max(0, VisualInset);
        if (Orientation == UiSplitterOrientation.Vertical)
        {
            int thickness = Math.Max(1, Math.Min(Bounds.Width, requestedThickness));
            return new UiRect(
                Bounds.X + (Bounds.Width - thickness) / 2,
                Bounds.Y + inset,
                thickness,
                Math.Max(0, Bounds.Height - inset * 2));
        }

        int horizontalThickness = Math.Max(1, Math.Min(Bounds.Height, requestedThickness));
        return new UiRect(
            Bounds.X + inset,
            Bounds.Y + (Bounds.Height - horizontalThickness) / 2,
            Math.Max(0, Bounds.Width - inset * 2),
            horizontalThickness);
    }
}
