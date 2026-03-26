namespace OpenControls;

public readonly struct UiRenderContext
{
    public UiRenderContext(IUiRenderer renderer, UiFont defaultFont)
    {
        Renderer = renderer;
        DefaultFont = defaultFont;
    }

    public IUiRenderer Renderer { get; }
    public UiFont DefaultFont { get; }
}
