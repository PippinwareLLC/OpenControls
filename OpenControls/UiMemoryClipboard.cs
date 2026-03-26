namespace OpenControls;

public sealed class UiMemoryClipboard : IUiClipboard
{
    private string _text = string.Empty;

    public string GetText()
    {
        return _text;
    }

    public void SetText(string text)
    {
        _text = text ?? string.Empty;
    }
}
