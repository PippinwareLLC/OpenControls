namespace OpenControls;

[Flags]
public enum UiInvalidationReason
{
    None = 0,
    Layout = 1 << 0,
    Paint = 1 << 1,
    Children = 1 << 2,
    Visibility = 1 << 3,
    Clip = 1 << 4,
    State = 1 << 5,
    Text = 1 << 6,
    Style = 1 << 7,
    Volatility = 1 << 8,
    Parent = 1 << 9
}
