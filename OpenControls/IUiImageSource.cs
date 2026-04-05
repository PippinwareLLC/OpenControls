namespace OpenControls;

public interface IUiImageSource
{
    void Draw(IUiRenderer renderer, UiRect bounds);

    string? DebugName => null;

    bool IsRenderCacheVolatile => false;

    bool TryGetIntrinsicSize(out UiPoint size)
    {
        size = default;
        return false;
    }
}
