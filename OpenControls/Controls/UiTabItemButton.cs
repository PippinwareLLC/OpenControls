namespace OpenControls.Controls;

public enum UiTabItemButtonPlacement
{
    Leading,
    Trailing
}

public sealed class UiTabItemButton : UiElement
{
    public string Text { get; set; } = string.Empty;
    public UiTabItemButtonPlacement Placement { get; set; } = UiTabItemButtonPlacement.Leading;
    public bool AutoSize { get; set; } = true;
    public int Width { get; set; }
    public int MaxWidth { get; set; }

    internal UiRect TabBounds { get; set; }

    public event Action<UiTabItemButton>? Clicked;

    internal void RaiseClicked()
    {
        Clicked?.Invoke(this);
    }

    public override void Render(UiRenderContext context)
    {
        // Rendered by UiTabBar.
    }
}
