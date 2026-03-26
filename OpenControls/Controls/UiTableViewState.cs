namespace OpenControls.Controls;

public sealed class UiTableViewState
{
    public UiPoint ContentSize { get; internal set; }
    public UiPoint ViewportSize { get; internal set; }
    public UiRect HeaderViewportBounds { get; internal set; }
    public UiRect BodyViewportBounds { get; internal set; }
    public int ScrollX { get; internal set; }
    public int ScrollY { get; internal set; }
    public int FirstVisibleRowIndex { get; internal set; } = -1;
    public int LastVisibleRowIndex { get; internal set; } = -1;
}
