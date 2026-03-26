namespace OpenControls.Controls;

[Flags]
public enum UiDragFlags
{
    None = 0,
    AlwaysClamp = 1 << 0,
    Clamp = AlwaysClamp,
    NoSlowFast = 1 << 1,
    Logarithmic = 1 << 2,
    NoInput = 1 << 3,
    NoRoundToFormat = 1 << 4,
    WrapAround = 1 << 5
}
