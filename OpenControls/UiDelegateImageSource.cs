namespace OpenControls;

public sealed class UiDelegateImageSource : IUiImageSource
{
    private readonly Action<IUiRenderer, UiRect> _draw;

    public UiDelegateImageSource(Action<IUiRenderer, UiRect> draw)
    {
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
    }

    public void Draw(IUiRenderer renderer, UiRect bounds)
    {
        _draw(renderer, bounds);
    }
}
