namespace OpenControls;

/// <summary>
/// Optional texture-renderer capability for choosing the sampling policy of
/// an uploaded texture without changing its pixels or draw geometry.
/// </summary>
public interface IUiTextureSamplingRenderer
{
    void SetTextureSampling(
        uint textureId,
        UiTextureSampling sampling);
}
