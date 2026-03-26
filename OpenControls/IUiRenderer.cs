namespace OpenControls;

public interface IUiRenderer
{
    UiFont DefaultFont { get; set; }
    void FillRect(UiRect rect, UiColor color);
    void DrawRect(UiRect rect, UiColor color, int thickness = 1);
    void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight);
    void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB);
    void DrawText(string text, UiPoint position, UiColor color, int scale = 1);
    void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font);
    int MeasureTextWidth(string text, int scale = 1);
    int MeasureTextWidth(string text, int scale, UiFont? font);
    int MeasureTextHeight(int scale = 1);
    int MeasureTextHeight(int scale, UiFont? font);
    void PushClip(UiRect rect);
    void PopClip();
}
