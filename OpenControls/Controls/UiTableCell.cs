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
    public string? RenderText { get; set; }
    public UiElement? Content { get; set; }
    public UiColor? Background { get; set; }
    public UiColor? TextColor { get; set; }
    public int Padding { get; set; } = -1;

    internal string GetRenderText()
    {
        return RenderText ?? Text;
    }
}
