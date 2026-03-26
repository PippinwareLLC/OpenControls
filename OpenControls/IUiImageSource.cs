namespace OpenControls;

public interface IUiImageSource
{
    void Draw(IUiRenderer renderer, UiRect bounds);

    string? DebugName => null;

    bool TryGetIntrinsicSize(out UiPoint size)
    {
        size = default;
        return false;
    }
}
