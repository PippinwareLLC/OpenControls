namespace OpenControls.Controls;

[Flags]
public enum UiDragFlags
{
    None = 0,
    Clamp = 1 << 0,
    NoSlowFast = 1 << 1,
    Logarithmic = 1 << 2
}
