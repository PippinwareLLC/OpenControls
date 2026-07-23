namespace OpenControls;

/// <summary>
/// Optional renderer capability for uploading and drawing RGBA texture pixels.
/// </summary>
public interface IUiTextureRenderer
{
    uint CreateRgbaTexture(int width, int height, ReadOnlySpan<byte> rgbaPixels);

    void UpdateRgbaTexture(uint textureId, int width, int height, ReadOnlySpan<byte> rgbaPixels);

    void DrawTexture(
        uint textureId,
        UiRect rect,
        float sourceX,
        float sourceY,
        float sourceWidth,
        float sourceHeight,
        bool flipVertical = false,
        UiColor? tint = null);
}
