namespace OpenControls;

public sealed class UiDelegateClipboard : IUiClipboard
{
    private readonly Func<string> _getText;
    private readonly Action<string> _setText;

    public UiDelegateClipboard(Func<string> getText, Action<string> setText)
    {
        _getText = getText ?? throw new ArgumentNullException(nameof(getText));
        _setText = setText ?? throw new ArgumentNullException(nameof(setText));
    }

    public string GetText()
    {
        return _getText() ?? string.Empty;
    }

    public void SetText(string text)
    {
        _setText(text ?? string.Empty);
    }
}
