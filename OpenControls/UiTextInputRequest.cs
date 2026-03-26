namespace OpenControls;

public readonly struct UiTextInputRequest
{
    public UiTextInputRequest(
        UiRect bounds,
        bool isMultiLine = false,
        UiRect? caretBounds = null,
        UiRect? candidateBounds = null,
        bool supportsComposition = true)
    {
        Bounds = bounds;
        IsMultiLine = isMultiLine;
        CaretBounds = caretBounds ?? bounds;
        CandidateBounds = candidateBounds ?? CaretBounds;
        SupportsComposition = supportsComposition;
    }

    public UiRect Bounds { get; }
    public bool IsMultiLine { get; }
    public UiRect CaretBounds { get; }
    public UiRect CandidateBounds { get; }
    public bool SupportsComposition { get; }
}
