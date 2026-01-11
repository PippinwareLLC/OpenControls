namespace OpenControls;

public sealed class UiFocusManager
{
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

        Focused?.OnFocusLost();
        Focused = element;
        Focused?.OnFocusGained();
    }

    public void ClearFocus()
    {
        RequestFocus(null);
    }
}
