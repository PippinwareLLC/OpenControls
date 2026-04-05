namespace OpenControls;

internal enum UiRenderPassKind
{
    Main,
    Overlay
}

internal delegate void UiRenderChildDelegate(UiElement child, UiRenderContext context, UiRenderPassKind passKind);

public readonly struct UiRenderContext
{
    private readonly UiRenderChildDelegate? _renderChild;

    public UiRenderContext(IUiRenderer renderer, UiFont defaultFont)
        : this(renderer, defaultFont, null, UiRenderPassKind.Main)
    {
    }

    internal UiRenderContext(IUiRenderer renderer, UiFont defaultFont, UiRenderChildDelegate? renderChild, UiRenderPassKind passKind)
    {
        Renderer = renderer;
        DefaultFont = defaultFont;
        _renderChild = renderChild;
        PassKind = passKind;
    }

    public IUiRenderer Renderer { get; }
    public UiFont DefaultFont { get; }
    internal UiRenderPassKind PassKind { get; }

    public void RenderChild(UiElement child)
    {
        if (_renderChild != null)
        {
            _renderChild(child, this, UiRenderPassKind.Main);
            return;
        }

        child.Render(this);
    }

    public void RenderChildOverlay(UiElement child)
    {
        if (_renderChild != null)
        {
            _renderChild(child, this, UiRenderPassKind.Overlay);
            return;
        }

        child.RenderOverlay(this);
    }

    internal UiRenderContext WithRenderer(IUiRenderer renderer)
    {
        return new UiRenderContext(renderer, DefaultFont, _renderChild, PassKind);
    }
}
