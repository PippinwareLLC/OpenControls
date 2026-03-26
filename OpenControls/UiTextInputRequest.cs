namespace OpenControls;

public readonly struct UiTextInputRequest
{
    public UiTextInputRequest(UiRect bounds, bool isMultiLine = false)
    {
        Bounds = bounds;
        IsMultiLine = isMultiLine;
    }

    public UiRect Bounds { get; }
    public bool IsMultiLine { get; }
}
