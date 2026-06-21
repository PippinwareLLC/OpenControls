namespace OpenControls;

public sealed class UiScaledRenderer : IUiRenderer, IUiVectorRenderer, IUiVectorPassRenderer, IUiTransformedVectorRenderer, IUiShapeRenderer
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

    public void DrawPolyline(IReadOnlyList<UiPoint> points, int thickness, UiColor color)
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        UiPoint[] scaled = new UiPoint[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            scaled[i] = ScalePoint(points[i]);
        }

        int scaledThickness = ScaleExtent(thickness);
        if (_inner is IUiVectorRenderer vectorRenderer)
        {
            vectorRenderer.DrawPolyline(scaled, scaledThickness, color);
            return;
        }

        UiRenderHelpers.DrawPolylineFallback(_inner, scaled, scaledThickness, color);
    }

    public void BeginVectorPass()
    {
        if (_inner is IUiVectorPassRenderer vectorPassRenderer)
        {
            vectorPassRenderer.BeginVectorPass();
        }
    }

    public void EndVectorPass()
    {
        if (_inner is IUiVectorPassRenderer vectorPassRenderer)
        {
            vectorPassRenderer.EndVectorPass();
        }
    }

    public void DrawPolylineTransformed(
        IReadOnlyList<UiPoint> points,
        int thickness,
        UiColor color,
        UiPoint origin,
        float zoom,
        float panX,
        float panY)
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        int scaledThickness = ScaleExtent(thickness);
        if (_inner is IUiTransformedVectorRenderer transformedRenderer)
        {
            transformedRenderer.DrawPolylineTransformed(points, scaledThickness, color, ScalePoint(origin), zoom * _dpi.ScaleFactor, panX, panY);
            return;
        }

        UiPoint[] scaled = new UiPoint[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            int x = origin.X + (int)Math.Round((points[i].X - panX) * zoom);
            int y = origin.Y + (int)Math.Round((points[i].Y - panY) * zoom);
            scaled[i] = ScalePoint(new UiPoint(x, y));
        }

        if (_inner is IUiVectorRenderer vectorRenderer)
        {
            vectorRenderer.DrawPolyline(scaled, scaledThickness, color);
            return;
        }

        UiRenderHelpers.DrawPolylineFallback(_inner, scaled, scaledThickness, color);
    }

    public void FillRoundedRect(UiRect rect, int radius, UiColor color)
    {
        UiRect scaled = ScaleRect(rect);
        int scaledRadius = ScaleExtent(radius);
        if (_inner is IUiShapeRenderer shapeRenderer)
        {
            shapeRenderer.FillRoundedRect(scaled, scaledRadius, color);
            return;
        }

        UiRenderHelpers.FillRectRoundedFallback(_inner, scaled, scaledRadius, color);
    }

    public void FillTopRoundedRect(UiRect rect, int radius, UiColor color)
    {
        UiRect scaled = ScaleRect(rect);
        int scaledRadius = ScaleExtent(radius);
        if (_inner is IUiShapeRenderer shapeRenderer)
        {
            shapeRenderer.FillTopRoundedRect(scaled, scaledRadius, color);
            return;
        }

        UiRenderHelpers.FillRectTopRoundedFallback(_inner, scaled, scaledRadius, color);
    }

    public void DrawRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
    {
        UiRect scaled = ScaleRect(rect);
        int scaledRadius = ScaleExtent(radius);
        int scaledThickness = ScaleExtent(thickness);
        if (_inner is IUiShapeRenderer shapeRenderer)
        {
            shapeRenderer.DrawRoundedRect(scaled, scaledRadius, color, scaledThickness);
            return;
        }

        UiRenderHelpers.DrawRectRoundedFallback(_inner, scaled, scaledRadius, color, scaledThickness);
    }

    public void DrawTopRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
    {
        UiRect scaled = ScaleRect(rect);
        int scaledRadius = ScaleExtent(radius);
        int scaledThickness = ScaleExtent(thickness);
        if (_inner is IUiShapeRenderer shapeRenderer)
        {
            shapeRenderer.DrawTopRoundedRect(scaled, scaledRadius, color, scaledThickness);
            return;
        }

        UiRenderHelpers.DrawRectTopRoundedFallback(_inner, scaled, scaledRadius, color, scaledThickness);
    }

    public void FillCircle(UiPoint center, int radius, UiColor color)
    {
        UiPoint scaledCenter = ScalePoint(center);
        int scaledRadius = ScaleExtent(radius);
        if (_inner is IUiShapeRenderer shapeRenderer)
        {
            shapeRenderer.FillCircle(scaledCenter, scaledRadius, color);
            return;
        }

        UiRenderHelpers.FillCircleFallback(_inner, scaledCenter, scaledRadius, color);
    }

    public void DrawCircle(UiPoint center, int radius, UiColor color, int thickness = 1)
    {
        UiPoint scaledCenter = ScalePoint(center);
        int scaledRadius = ScaleExtent(radius);
        int scaledThickness = ScaleExtent(thickness);
        if (_inner is IUiShapeRenderer shapeRenderer)
        {
            shapeRenderer.DrawCircle(scaledCenter, scaledRadius, color, scaledThickness);
            return;
        }

        UiRenderHelpers.DrawCircleFallback(_inner, scaledCenter, scaledRadius, color, scaledThickness);
    }

    public void FillTriangleRight(UiRect rect, UiColor color)
    {
        UiRect scaled = ScaleRect(rect);
        if (_inner is IUiShapeRenderer shapeRenderer)
        {
            shapeRenderer.FillTriangleRight(scaled, color);
            return;
        }

        UiRenderHelpers.FillTriangleRightFallback(_inner, scaled, color);
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
