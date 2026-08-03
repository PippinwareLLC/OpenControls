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
    public string SecondaryText { get; set; } = string.Empty;
    /// <summary>
    /// Optional leading glyph rendered with the tree's resolved font. This
    /// keeps semantic item text free of icon-font characters while allowing
    /// editor trees to present typed scene, entity, asset, and component
    /// identities consistently.
    /// </summary>
    public string LeadingIconText { get; set; } = string.Empty;
    public UiColor? LeadingIconColor { get; set; }
    public int LeadingIconGap { get; set; } = 4;
    public int ExtraTextOffset { get; set; }
    public List<UiTreeViewItem> Children { get; } = new();
    public bool IsOpen { get; set; }
    public UiColor? TextColor { get; set; }
    public UiColor? SecondaryTextColor { get; set; }
    public object? Tag { get; set; }

    public bool HasChildren => Children.Count > 0;
}
