namespace OpenControls;

public sealed class UiContext
{
    public UiContext(UiElement root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public UiElement Root { get; }
    public UiFocusManager Focus { get; } = new();

    public void Update(UiInputState input, float deltaSeconds = 0f)
    {
        Root.Update(new UiUpdateContext(input, Focus, deltaSeconds));
    }

    public void Render(IUiRenderer renderer)
    {
        UiRenderContext context = new(renderer);
        Root.Render(context);
        Root.RenderOverlay(context);
    }
}
