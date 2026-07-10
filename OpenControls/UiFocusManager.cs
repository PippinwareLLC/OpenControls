namespace OpenControls;

public sealed class UiFocusManager
{
    private bool _changingFocus;
    private bool _hasPendingFocusRequest;
    private UiElement? _pendingFocus;

    public static event Action<UiElement?, UiElement?, string>? DebugFocusChanged;

    public UiElement? Focused { get; private set; }

    public void RequestFocus(UiElement? element)
    {
        if (element != null && !element.IsFocusable)
        {
            return;
        }

        if (_changingFocus)
        {
            _pendingFocus = element;
            _hasPendingFocusRequest = true;
            return;
        }

        _changingFocus = true;
        try
        {
            UiElement? requested = element;
            while (true)
            {
                _hasPendingFocusRequest = false;
                ApplyFocusChange(requested);
                if (!_hasPendingFocusRequest)
                {
                    break;
                }

                requested = _pendingFocus;
            }
        }
        finally
        {
            _changingFocus = false;
            _hasPendingFocusRequest = false;
            _pendingFocus = null;
        }
    }

    public void ClearFocus()
    {
        RequestFocus(null);
    }

    private void ApplyFocusChange(UiElement? element)
    {
        if (element == Focused)
        {
            return;
        }

        UiElement? previous = Focused;
        previous?.OnFocusLost();
        Focused = element;
        Focused?.OnFocusGained();

        Action<UiElement?, UiElement?, string>? debugFocusChanged = DebugFocusChanged;
        if (debugFocusChanged != null)
        {
            string stack = new System.Diagnostics.StackTrace(1, true).ToString();
            debugFocusChanged(previous, Focused, stack);
        }
    }
}
