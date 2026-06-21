namespace OpenControls;

public interface IUiShapeRenderer
{
    void FillRoundedRect(UiRect rect, int radius, UiColor color);
    void FillTopRoundedRect(UiRect rect, int radius, UiColor color);
    void DrawRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1);
    void DrawTopRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1);
    void FillCircle(UiPoint center, int radius, UiColor color);
    void DrawCircle(UiPoint center, int radius, UiColor color, int thickness = 1);
    void FillTriangleRight(UiRect rect, UiColor color);
}
