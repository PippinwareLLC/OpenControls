namespace OpenControls.Controls;

public sealed class UiTreeViewItem
{
    public UiTreeViewItem()
    {
    }

    public UiTreeViewItem(string text)
    {
        Text = text ?? string.Empty;
    }

    public string Text { get; set; } = string.Empty;
    public int ExtraTextOffset { get; set; }
    public List<UiTreeViewItem> Children { get; } = new();
    public bool IsOpen { get; set; }
    public UiColor? TextColor { get; set; }
    public object? Tag { get; set; }

    public bool HasChildren => Children.Count > 0;
}
