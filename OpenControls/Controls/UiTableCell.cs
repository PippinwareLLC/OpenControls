namespace OpenControls.Controls;

public sealed class UiTableCell
{
    public UiTableCell()
    {
    }

    public UiTableCell(string text)
    {
        Text = text;
    }

    public string Text { get; set; } = string.Empty;
    public UiElement? Content { get; set; }
    public UiColor? Background { get; set; }
    public UiColor? TextColor { get; set; }
    public int Padding { get; set; } = -1;
}
