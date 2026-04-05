namespace OpenControls;

public sealed class UiDelegateImageSource : IUiImageSource
{
    private readonly Action<IUiRenderer, UiRect> _draw;
    private readonly Func<UiPoint>? _getIntrinsicSize;

    public UiDelegateImageSource(Action<IUiRenderer, UiRect> draw, Func<UiPoint>? getIntrinsicSize = null, string? debugName = null)
    {
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _getIntrinsicSize = getIntrinsicSize;
        DebugName = debugName;
    }

    public string? DebugName { get; }

    public bool IsRenderCacheVolatile => true;

    public void Draw(IUiRenderer renderer, UiRect bounds)
    {
        _draw(renderer, bounds);
    }

    public bool TryGetIntrinsicSize(out UiPoint size)
    {
        if (_getIntrinsicSize != null)
        {
            size = _getIntrinsicSize();
            return true;
        }

        size = default;
        return false;
    }
}
