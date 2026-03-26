namespace OpenControls;

public readonly struct UiContainerStateSnapshot
{
    public UiContainerStateSnapshot(
        UiElement? element,
        UiContainerKind kind,
        UiRect bounds,
        UiRect clipBounds,
        bool visible,
        bool enabled,
        bool hovered,
        bool focused,
        bool open,
        bool activeTab,
        bool activePopup,
        bool activeInputLayer)
    {
        Element = element;
        Kind = kind;
        Bounds = bounds;
        ClipBounds = clipBounds;
        Visible = visible;
        Enabled = enabled;
        Hovered = hovered;
        Focused = focused;
        Open = open;
        ActiveTab = activeTab;
        ActivePopup = activePopup;
        ActiveInputLayer = activeInputLayer;
    }

    public UiElement? Element { get; }
    public UiContainerKind Kind { get; }
    public UiRect Bounds { get; }
    public UiRect ClipBounds { get; }
    public bool Visible { get; }
    public bool Enabled { get; }
    public bool Hovered { get; }
    public bool Focused { get; }
    public bool Open { get; }
    public bool ActiveTab { get; }
    public bool ActivePopup { get; }
    public bool ActiveInputLayer { get; }
    public bool IsValid => Element != null;
    public bool IsClipped => !Bounds.Equals(ClipBounds);
    public bool IsWindow => Kind == UiContainerKind.Window;
    public bool IsPopup => Kind == UiContainerKind.Popup || Kind == UiContainerKind.Modal;
    public bool IsModal => Kind == UiContainerKind.Modal;
    public bool IsMenuBar => Kind == UiContainerKind.MenuBar;

    public static UiContainerStateSnapshot Empty => default;
}
