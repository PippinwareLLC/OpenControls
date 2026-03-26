namespace OpenControls;

public readonly struct UiTextCompositionState
{
    public UiTextCompositionState(
        string text,
        int selectionStart = 0,
        int selectionLength = 0,
        int caretIndex = -1)
    {
        Text = text ?? string.Empty;
        SelectionStart = Math.Clamp(selectionStart, 0, Text.Length);
        SelectionLength = Math.Clamp(selectionLength, 0, Text.Length - SelectionStart);
        CaretIndex = caretIndex >= 0
            ? Math.Clamp(caretIndex, 0, Text.Length)
            : SelectionStart + SelectionLength;
    }

    public string Text { get; }
    public int SelectionStart { get; }
    public int SelectionLength { get; }
    public int SelectionEnd => SelectionStart + SelectionLength;
    public int CaretIndex { get; }
    public bool IsActive => !string.IsNullOrEmpty(Text);

    public static UiTextCompositionState Empty => default;
}
