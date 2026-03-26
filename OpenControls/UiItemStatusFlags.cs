namespace OpenControls;

[Flags]
public enum UiItemStatusFlags
{
    None = 0,
    Hovered = 1 << 0,
    Focused = 1 << 1,
    Active = 1 << 2,
    Pressed = 1 << 3,
    Dragging = 1 << 4,
    Edited = 1 << 5,
    Activated = 1 << 6,
    Deactivated = 1 << 7,
    Clicked = 1 << 8
}
