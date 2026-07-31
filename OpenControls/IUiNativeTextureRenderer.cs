namespace OpenControls;

/// <summary>
/// Optional texture-renderer capability for drawing native textures that are
/// already owned by the renderer's graphics context. Wrappers may expose the
/// stable capability through <see cref="IUiTextureRendererResourceOwner"/>
/// while continuing to transform draw geometry themselves.
/// </summary>
public interface IUiNativeTextureRenderer
    : IUiTextureRenderer
{
    object NativeTextureContext { get; }

    bool IsNativeTexture(uint textureId);
}
