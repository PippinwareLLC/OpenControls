namespace OpenControls.Controls;

public sealed class UiDragDropSource : UiElement
{
    private bool _pressed;
    private UiPoint _pressPosition;
    private bool _dragStarted;

    public string PayloadType { get; set; } = string.Empty;
    public object? PayloadData { get; set; }
    public Func<UiDragDropPayload?>? PayloadBuilder { get; set; }
    public int DragThreshold { get; set; } = 6;
    public bool AllowWhenDisabled { get; set; }
    public bool StartOnClick { get; set; }

    public bool IsDragging => _dragStarted;

    public event Action<UiDragDropPayload>? DragStarted;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || (!Enabled && !AllowWhenDisabled))
        {
            Reset();
            return;
        }

        UiInputState input = context.Input;
        if (input.LeftClicked && Bounds.Contains(input.MousePosition))
        {
            _pressed = true;
            _pressPosition = input.MousePosition;
        }

        if (_pressed && input.LeftDown && !_dragStarted)
        {
            if (StartOnClick || HasExceededThreshold(input.MousePosition))
            {
                UiDragDropPayload? payload = BuildPayload();
                if (payload != null && context.DragDrop.BeginDrag(this, payload, _pressPosition))
                {
                    _dragStarted = true;
                    DragStarted?.Invoke(payload);
                }
            }
        }

        if (input.LeftReleased)
        {
            _pressed = false;
        }

        if (_dragStarted && context.DragDrop.Source != this)
        {
            _dragStarted = false;
        }

        base.Update(context);
    }

    private UiDragDropPayload? BuildPayload()
    {
        if (PayloadBuilder != null)
        {
            return PayloadBuilder();
        }

        if (PayloadData == null && string.IsNullOrEmpty(PayloadType))
        {
            return null;
        }

        return new UiDragDropPayload(PayloadType, PayloadData);
    }

    private bool HasExceededThreshold(UiPoint current)
    {
        int threshold = Math.Max(0, DragThreshold);
        int dx = Math.Abs(current.X - _pressPosition.X);
        int dy = Math.Abs(current.Y - _pressPosition.Y);
        return dx >= threshold || dy >= threshold;
    }

    private void Reset()
    {
        _pressed = false;
        _dragStarted = false;
    }
}
