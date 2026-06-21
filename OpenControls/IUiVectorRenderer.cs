namespace OpenControls;

public interface IUiVectorRenderer
{
    void DrawPolyline(IReadOnlyList<UiPoint> points, int thickness, UiColor color);
}
