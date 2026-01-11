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

    public bool IsDragging => _dragging;

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
        context.Renderer.FillRect(Bounds, color);
        base.Render(context);
    }

    private int GetAxisPosition(UiPoint point)
    {
        return Orientation == UiSplitterOrientation.Vertical ? point.X : point.Y;
    }
}
