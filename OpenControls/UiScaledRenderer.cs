namespace OpenControls;

public sealed class UiScaledRenderer : IUiRenderer
{
    private readonly IUiRenderer _inner;
    private readonly UiDpiCompensation _dpi;
    private readonly Dictionary<(UiFont Font, int PixelSize), UiFont> _fontCache = new();
    private UiFont _defaultFont;

    public UiScaledRenderer(IUiRenderer inner, UiDpiCompensation dpiCompensation)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _dpi = dpiCompensation ?? throw new ArgumentNullException(nameof(dpiCompensation));
        _defaultFont = inner.DefaultFont;
        _inner.DefaultFont = ResolveScaledFont(_defaultFont);
    }

    public UiDpiCompensation DpiCompensation => _dpi;

    public IUiRenderer InnerRenderer => _inner;

    public UiFont DefaultFont
    {
        get => _defaultFont;
        set
        {
            _defaultFont = value ?? UiFont.Default;
            _inner.DefaultFont = ResolveScaledFont(_defaultFont);
        }
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        _inner.FillRect(ScaleRect(rect), color);
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        _inner.DrawRect(ScaleRect(rect), color, ScaleExtent(thickness));
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        _inner.FillRectGradient(ScaleRect(rect), topLeft, topRight, bottomLeft, bottomRight);
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        _inner.FillRectCheckerboard(ScaleRect(rect), ScaleExtent(cellSize), colorA, colorB);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        DrawText(text, position, color, scale, null);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
    {
        _inner.DrawText(text, ScalePoint(position), color, scale, ResolveScaledFont(font));
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return MeasureTextWidth(text, scale, null);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        int physicalWidth = _inner.MeasureTextWidth(text, scale, ResolveScaledFont(font));
        return _dpi.ToLogicalExtent(physicalWidth);
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return MeasureTextHeight(scale, null);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        int physicalHeight = _inner.MeasureTextHeight(scale, ResolveScaledFont(font));
        return _dpi.ToLogicalExtent(physicalHeight);
    }

    public void PushClip(UiRect rect)
    {
        _inner.PushClip(ScaleRect(rect));
    }

    public void PopClip()
    {
        _inner.PopClip();
    }

    private UiFont ResolveScaledFont(UiFont? font)
    {
        UiFont resolved = font ?? _defaultFont;
        if (!_dpi.Enabled)
        {
            return resolved;
        }

        int scaledPixelSize = _dpi.ToPhysicalExtent(resolved.PixelSize);
        if (scaledPixelSize == resolved.PixelSize)
        {
            return resolved;
        }

        (UiFont Font, int PixelSize) key = (resolved, scaledPixelSize);
        if (_fontCache.TryGetValue(key, out UiFont? cached))
        {
            return cached;
        }

        UiFont scaled = resolved.WithPixelSize(scaledPixelSize);
        _fontCache[key] = scaled;
        return scaled;
    }

    private UiRect ScaleRect(UiRect rect)
    {
        return _dpi.ToPhysical(rect);
    }

    private UiPoint ScalePoint(UiPoint point)
    {
        return _dpi.ToPhysical(point);
    }

    private int ScaleExtent(int value)
    {
        return _dpi.ToPhysicalExtent(value);
    }
}
