namespace OpenControls;

public sealed class UiInputState
{
    public UiPoint MousePosition { get; init; }
    public UiPoint ScreenMousePosition { get; init; }
    public bool LeftDown { get; init; }
    public bool LeftClicked { get; init; }
    public bool LeftReleased { get; init; }
    public bool ShiftDown { get; init; }
    public bool CtrlDown { get; init; }
    public int ScrollDelta { get; init; }
    public IReadOnlyList<char> TextInput { get; init; } = Array.Empty<char>();
    public UiNavigationInput Navigation { get; init; }
}
