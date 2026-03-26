namespace OpenControls.Controls;

[Flags]
public enum UiSliderFlags
{
    None = 0,
    AlwaysClamp = 1 << 0,
    NoInput = 1 << 1,
    NoRoundToFormat = 1 << 2,
    WrapAround = 1 << 3
}
