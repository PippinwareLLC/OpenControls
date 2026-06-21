namespace OpenControls;

public interface IUiTransformedVectorRenderer
{
    void DrawPolylineTransformed(
        IReadOnlyList<UiPoint> points,
        int thickness,
        UiColor color,
        UiPoint origin,
        float zoom,
        float panX,
        float panY);
}
