namespace OpenControls;

internal sealed class UiRecordingRenderer : IUiRenderer, IUiVectorRenderer, IUiVectorPassRenderer, IUiTransformedVectorRenderer, IUiShapeRenderer
{
    private readonly IUiRenderer _measurementRenderer;
    private readonly List<UiRenderCommandList.UiRenderCommand> _commands = new();

    public UiRecordingRenderer(IUiRenderer measurementRenderer, UiFont? defaultFont = null)
    {
        _measurementRenderer = measurementRenderer ?? throw new ArgumentNullException(nameof(measurementRenderer));
        DefaultFont = defaultFont ?? measurementRenderer.DefaultFont;
    }

    public UiFont DefaultFont { get; set; }

    public int RecordedCommandCount => _commands.Count;

    public UiRenderCommandList BuildCommandList()
    {
        return new UiRenderCommandList(_commands.ToArray());
    }

    public void RecordSubtree(UiElement element, UiRenderPassKind passKind)
    {
        _commands.Add(new UiRenderCommandList.RenderSubtreeCommand(element, passKind));
    }

    public void FillRect(UiRect rect, UiColor color)
    {
        _commands.Add(new UiRenderCommandList.FillRectCommand(rect, color));
    }

    public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
    {
        _commands.Add(new UiRenderCommandList.DrawRectCommand(rect, color, thickness));
    }

    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
    {
        _commands.Add(new UiRenderCommandList.FillRectGradientCommand(rect, topLeft, topRight, bottomLeft, bottomRight));
    }

    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
    {
        _commands.Add(new UiRenderCommandList.FillRectCheckerboardCommand(rect, cellSize, colorA, colorB));
    }

    public void DrawPolyline(IReadOnlyList<UiPoint> points, int thickness, UiColor color)
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        UiPoint[] copy = new UiPoint[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            copy[i] = points[i];
        }

        _commands.Add(new UiRenderCommandList.DrawPolylineCommand(copy, thickness, color));
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

        UiPoint[] copy = new UiPoint[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            copy[i] = points[i];
        }

        _commands.Add(new UiRenderCommandList.DrawPolylineTransformedCommand(copy, thickness, color, origin, zoom, panX, panY));
    }

    public void BeginVectorPass()
    {
        _commands.Add(UiRenderCommandList.BeginVectorPassCommand.Instance);
    }

    public void EndVectorPass()
    {
        _commands.Add(UiRenderCommandList.EndVectorPassCommand.Instance);
    }

    public void FillRoundedRect(UiRect rect, int radius, UiColor color)
    {
        _commands.Add(new UiRenderCommandList.FillRoundedRectCommand(rect, radius, color));
    }

    public void FillTopRoundedRect(UiRect rect, int radius, UiColor color)
    {
        _commands.Add(new UiRenderCommandList.FillTopRoundedRectCommand(rect, radius, color));
    }

    public void DrawRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
    {
        _commands.Add(new UiRenderCommandList.DrawRoundedRectCommand(rect, radius, color, thickness));
    }

    public void DrawTopRoundedRect(UiRect rect, int radius, UiColor color, int thickness = 1)
    {
        _commands.Add(new UiRenderCommandList.DrawTopRoundedRectCommand(rect, radius, color, thickness));
    }

    public void FillCircle(UiPoint center, int radius, UiColor color)
    {
        _commands.Add(new UiRenderCommandList.FillCircleCommand(center, radius, color));
    }

    public void DrawCircle(UiPoint center, int radius, UiColor color, int thickness = 1)
    {
        _commands.Add(new UiRenderCommandList.DrawCircleCommand(center, radius, color, thickness));
    }

    public void FillTriangleRight(UiRect rect, UiColor color)
    {
        _commands.Add(new UiRenderCommandList.FillTriangleRightCommand(rect, color));
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
    {
        DrawText(text, position, color, scale, DefaultFont);
    }

    public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
    {
        _commands.Add(new UiRenderCommandList.DrawTextCommand(text, position, color, scale, font ?? DefaultFont));
    }

    public int MeasureTextWidth(string text, int scale = 1)
    {
        return _measurementRenderer.MeasureTextWidth(text, scale, DefaultFont);
    }

    public int MeasureTextWidth(string text, int scale, UiFont? font)
    {
        return _measurementRenderer.MeasureTextWidth(text, scale, font ?? DefaultFont);
    }

    public int MeasureTextHeight(int scale = 1)
    {
        return _measurementRenderer.MeasureTextHeight(scale, DefaultFont);
    }

    public int MeasureTextHeight(int scale, UiFont? font)
    {
        return _measurementRenderer.MeasureTextHeight(scale, font ?? DefaultFont);
    }

    public void PushClip(UiRect rect)
    {
        _commands.Add(new UiRenderCommandList.PushClipCommand(rect));
    }

    public void PopClip()
    {
        _commands.Add(UiRenderCommandList.PopClipCommand.Instance);
    }
}
