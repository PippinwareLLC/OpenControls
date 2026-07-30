namespace OpenControls;

/// <summary>
/// Optional wrapper contract that identifies the stable renderer owning
/// uploaded texture resources while the wrapper continues to transform draw
/// geometry.
/// </summary>
public interface
    IUiTextureRendererResourceOwner
{
    IUiTextureRenderer
        TextureRendererResourceOwner
    {
        get;
    }
}
