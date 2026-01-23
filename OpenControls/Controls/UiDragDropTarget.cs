namespace OpenControls.Controls;

public sealed class UiDragDropTarget : UiElement
{
    public string PayloadType { get; set; } = string.Empty;
    public bool AllowWhenDisabled { get; set; }

    public bool IsHovered { get; private set; }

    public event Action<UiDragDropPayload>? PayloadDropped;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || (!Enabled && !AllowWhenDisabled))
        {
            IsHovered = false;
            return;
        }

        UiDragDropContext dragDrop = context.DragDrop;
        if (!dragDrop.IsDragging)
        {
            IsHovered = false;
            return;
        }

        if (!Bounds.Contains(context.Input.MousePosition))
        {
            IsHovered = false;
            return;
        }

        IsHovered = true;
        dragDrop.SetHoveredTarget(this);

        if (dragDrop.IsDropRequested)
        {
            UiDragDropPayload? payload = dragDrop.AcceptPayload(this, PayloadType);
            if (payload != null)
            {
                PayloadDropped?.Invoke(payload);
            }
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        // Drag/drop targets are invisible by default; render children only.
        base.Render(context);
    }
}
