namespace OpenControls;

public sealed class UiFocusManager
{
    public static event Action<UiElement?, UiElement?, string>? DebugFocusChanged;

    public UiElement? Focused { get; private set; }

    public void RequestFocus(UiElement? element)
    {
        if (element == Focused)
        {
            return;
        }

        if (element != null && !element.IsFocusable)
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

    public void ClearFocus()
    {
        RequestFocus(null);
    }
}
