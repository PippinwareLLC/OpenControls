namespace OpenControls;

public readonly struct UiRenderContext
{
    public UiRenderContext(IUiRenderer renderer)
    {
        Renderer = renderer;
    }

    public IUiRenderer Renderer { get; }
}
