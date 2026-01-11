namespace OpenControls;

public interface IUiRenderer
{
    void FillRect(UiRect rect, UiColor color);
    void DrawRect(UiRect rect, UiColor color, int thickness = 1);
    void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight);
    void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB);
    void DrawText(string text, UiPoint position, UiColor color, int scale = 1);
    int MeasureTextWidth(string text, int scale = 1);
    int MeasureTextHeight(int scale = 1);
    void PushClip(UiRect rect);
    void PopClip();
}
