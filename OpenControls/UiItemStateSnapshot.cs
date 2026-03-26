namespace OpenControls;

public readonly struct UiItemStateSnapshot
{
    public UiItemStateSnapshot(
        UiElement? element,
        UiRect bounds,
        UiRect clipBounds,
        UiItemStatusFlags status,
        bool visible,
        bool enabled)
    {
        Element = element;
        Bounds = bounds;
        ClipBounds = clipBounds;
        Status = status;
        Visible = visible;
        Enabled = enabled;
    }

    public UiElement? Element { get; }
    public UiRect Bounds { get; }
    public UiRect ClipBounds { get; }
    public UiItemStatusFlags Status { get; }
    public bool Visible { get; }
    public bool Enabled { get; }
    public bool IsValid => Element != null;
    public bool IsHovered => Status.HasFlag(UiItemStatusFlags.Hovered);
    public bool IsFocused => Status.HasFlag(UiItemStatusFlags.Focused);
    public bool IsActive => Status.HasFlag(UiItemStatusFlags.Active);
    public bool IsPressed => Status.HasFlag(UiItemStatusFlags.Pressed);
    public bool IsDragging => Status.HasFlag(UiItemStatusFlags.Dragging);
    public bool IsEdited => Status.HasFlag(UiItemStatusFlags.Edited);
    public bool IsActivated => Status.HasFlag(UiItemStatusFlags.Activated);
    public bool IsDeactivated => Status.HasFlag(UiItemStatusFlags.Deactivated);
    public bool IsClicked => Status.HasFlag(UiItemStatusFlags.Clicked);
    public bool IsClipped => !Bounds.Equals(ClipBounds);

    public static UiItemStateSnapshot Empty => default;
}
