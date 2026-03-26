namespace OpenControls;

internal sealed class UiOffsetRenderer : IUiRenderer
{
    private readonly IUiRenderer _inner;
    private readonly UiPoint _offset;

    public UiOffsetRenderer(IUiRenderer inner, UiPoint offset)
    {
        _inner = inner;
        _offset = offset;
    }

    public UiFont DefaultFont
    {
        get => _inner.DefaultFont;
        set => _inner.DefaultFont = value;
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        _inner.FillRect(Offset(rect), color);
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        _inner.DrawRect(Offset(rect), color, thickness);
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        _inner.FillRectGradient(Offset(rect), topLeft, topRight, bottomLeft, bottomRight);
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        _inner.FillRectCheckerboard(Offset(rect), cellSize, colorA, colorB);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        _inner.DrawText(text, Offset(position), color, scale);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
    {
        _inner.DrawText(text, Offset(position), color, scale, font);
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return _inner.MeasureTextWidth(text, scale);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        return _inner.MeasureTextWidth(text, scale, font);
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return _inner.MeasureTextHeight(scale);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        return _inner.MeasureTextHeight(scale, font);
    }

    public void PushClip(UiRect rect)
    {
        _inner.PushClip(Offset(rect));
    }

    public void PopClip()
    {
        _inner.PopClip();
    }

    private UiRect Offset(UiRect rect)
    {
        return new UiRect(rect.X + _offset.X, rect.Y + _offset.Y, rect.Width, rect.Height);
    }

    private UiPoint Offset(UiPoint point)
    {
        return new UiPoint(point.X + _offset.X, point.Y + _offset.Y);
    }
}
