namespace OpenControls.Controls;

public sealed class UiPickerItem
{
    public string Text { get; set; } = string.Empty;
    public string SecondaryText { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public IUiImageSource? ImageSource { get; set; }
    public object? Tag { get; set; }
}
