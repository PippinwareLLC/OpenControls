using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenControls.Controls;

public readonly struct UiPlotPoint
{
    public UiPlotPoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; }
    public float Y { get; }
}

public sealed class UiPlotSeries
{
    public string Label { get; set; } = string.Empty;
    public UiColor LineColor { get; set; } = new UiColor(120, 180, 220);
    public int LineThickness { get; set; } = 1;
    public bool Visible { get; set; } = true;
    public IReadOnlyList<UiPlotPoint> Points { get; set; } = Array.Empty<UiPlotPoint>();
}

public sealed class UiPlotPanel : UiElement
{
    private sealed class PlotLayout
    {
        public UiRect PlotBounds;
        public int TextHeight;
        public int MaxYLabelWidth;
        public int MaxXLabelHeight;
        public List<float> XTicks { get; } = new();
        public List<float> YTicks { get; } = new();
        public List<string> XTickLabels { get; } = new();
        public List<string> YTickLabels { get; } = new();
    }

    private bool _panning;
    private UiPoint _panStartMouse;
    private float _panStartXMin;
    private float _panStartXMax;
    private float _panStartYMin;
    private float _panStartYMax;
    private UiRect _plotBounds;

    public List<UiPlotSeries> Series { get; } = new();

    public string Title { get; set; } = string.Empty;
    public string XAxisLabel { get; set; } = string.Empty;
    public string YAxisLabel { get; set; } = string.Empty;

    public float XMin { get; set; }
    public float XMax { get; set; } = 1f;
    public float YMin { get; set; }
    public float YMax { get; set; } = 1f;

    public float MinXRange { get; set; } = 0.001f;
    public float MinYRange { get; set; } = 0.001f;
    public float MaxXRange { get; set; }
    public float MaxYRange { get; set; }
    public bool ClampViewToData { get; set; }
    public float AutoFitMargin { get; set; } = 0.05f;

    public int Padding { get; set; } = 6;
    public int TickLength { get; set; } = 4;
    public int TickLabelPadding { get; set; } = 4;
    public int AxisLabelPadding { get; set; } = 6;
    public int TitlePadding { get; set; } = 6;
    public int AxisThickness { get; set; } = 1;
    public int GridThickness { get; set; } = 1;
    public int XTickCount { get; set; } = 5;
    public int YTickCount { get; set; } = 5;
    public int TextScale { get; set; } = 1;
    public bool ShowGrid { get; set; } = true;
    public bool ShowAxes { get; set; } = true;
    public bool ShowTicks { get; set; } = true;
    public bool ShowLabels { get; set; } = true;
    public bool ShowTitle { get; set; } = true;
    public bool ShowAxisLabels { get; set; } = true;
    public bool ClipPlot { get; set; } = true;
    public int CornerRadius { get; set; }

    public UiColor Background { get; set; } = new UiColor(18, 22, 32);
    public UiColor PlotBackground { get; set; } = new UiColor(14, 18, 26);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor PlotBorder { get; set; } = new UiColor(60, 70, 90);
    public UiColor GridColor { get; set; } = new UiColor(32, 40, 56);
    public UiColor AxisColor { get; set; } = UiColor.White;
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor TickLabelColor { get; set; } = new UiColor(200, 210, 230);

    public bool EnablePan { get; set; } = true;
    public bool EnableZoom { get; set; } = true;
    public float ZoomStep { get; set; } = 0.1f;
    public bool ZoomToCursor { get; set; } = true;

    public int FallbackCharWidth { get; set; } = 6;
    public int FallbackCharHeight { get; set; } = 7;
    public Func<string, int, int>? MeasureTextWidth { get; set; }
    public Func<int, int>? MeasureTextHeight { get; set; }
    public Func<float, string>? FormatXTick { get; set; }
    public Func<float, string>? FormatYTick { get; set; }

    public UiRect PlotBounds => _plotBounds;

    public override bool IsFocusable => true;

    public void AutoFit()
    {
        if (!TryGetDataBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            return;
        }

        float rangeX = Math.Max(maxX - minX, MinXRange > 0f ? MinXRange : 1f);
        float rangeY = Math.Max(maxY - minY, MinYRange > 0f ? MinYRange : 1f);
        float marginX = rangeX * Math.Max(0f, AutoFitMargin);
        float marginY = rangeY * Math.Max(0f, AutoFitMargin);

        XMin = minX - marginX;
        XMax = maxX + marginX;
        YMin = minY - marginY;
        YMax = maxY + marginY;
        NormalizeView();
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        NormalizeView();
        PlotLayout layout = BuildLayout();
        UiRect plotBounds = layout.PlotBounds;

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;
        bool mouseInPlot = plotBounds.Contains(mouse);

        if (EnablePan && input.LeftClicked && mouseInPlot)
        {
            _panning = true;
            _panStartMouse = mouse;
            _panStartXMin = XMin;
            _panStartXMax = XMax;
            _panStartYMin = YMin;
            _panStartYMax = YMax;
            context.Focus.RequestFocus(this);
        }

        if (_panning && input.LeftDown)
        {
            ApplyPan(plotBounds, mouse);
        }

        if (_panning && input.LeftReleased)
        {
            _panning = false;
        }

        if (EnableZoom && input.ScrollDelta != 0 && mouseInPlot)
        {
            ApplyZoom(plotBounds, mouse, input.ScrollDelta);
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        PlotLayout layout = BuildLayout();
        UiRect plotBounds = layout.PlotBounds;

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        if (plotBounds.Width > 0 && plotBounds.Height > 0)
        {
            context.Renderer.FillRect(plotBounds, PlotBackground);
            if (PlotBorder.A > 0)
            {
                context.Renderer.DrawRect(plotBounds, PlotBorder, 1);
            }
        }

        DrawTitleAndAxisLabels(context, layout);

        if (plotBounds.Width <= 0 || plotBounds.Height <= 0)
        {
            base.Render(context);
            return;
        }

        if (ShowGrid)
        {
            DrawGrid(context, layout);
        }

        if (ShowAxes)
        {
            DrawAxes(context, plotBounds);
        }

        if (ShowTicks || ShowLabels)
        {
            DrawTicks(context, layout);
        }

        DrawSeries(context, plotBounds);

        base.Render(context);
    }

    protected internal override void OnFocusLost()
    {
        _panning = false;
    }

    private PlotLayout BuildLayout()
    {
        PlotLayout layout = new PlotLayout();
        layout.TextHeight = GetTextHeight(TextScale);
        BuildTicks(XMin, XMax, XTickCount, layout.XTicks, layout.XTickLabels, FormatXTick);
        BuildTicks(YMin, YMax, YTickCount, layout.YTicks, layout.YTickLabels, FormatYTick);

        layout.MaxYLabelWidth = 0;
        layout.MaxXLabelHeight = 0;

        if (ShowLabels)
        {
            foreach (string label in layout.YTickLabels)
            {
                layout.MaxYLabelWidth = Math.Max(layout.MaxYLabelWidth, GetTextWidth(label, TextScale));
            }

            layout.MaxXLabelHeight = layout.TextHeight;
        }

        int padding = Math.Max(0, Padding);
        int top = Bounds.Y + padding;
        if (ShowTitle && !string.IsNullOrWhiteSpace(Title))
        {
            top += layout.TextHeight + Math.Max(0, TitlePadding);
        }

        if (ShowAxisLabels && !string.IsNullOrWhiteSpace(YAxisLabel))
        {
            top += layout.TextHeight + Math.Max(0, AxisLabelPadding);
        }

        int left = Bounds.X + padding;
        if (ShowLabels)
        {
            int tickSpace = ShowTicks ? Math.Max(0, TickLength) : 0;
            left += layout.MaxYLabelWidth + Math.Max(0, TickLabelPadding) + tickSpace;
        }

        int bottomMargin = padding;
        if (ShowLabels)
        {
            int tickSpace = ShowTicks ? Math.Max(0, TickLength) : 0;
            bottomMargin += layout.MaxXLabelHeight + Math.Max(0, TickLabelPadding) + tickSpace;
        }

        if (ShowAxisLabels && !string.IsNullOrWhiteSpace(XAxisLabel))
        {
            bottomMargin += layout.TextHeight + Math.Max(0, AxisLabelPadding);
        }

        int right = Bounds.Right - padding;
        int bottom = Bounds.Bottom - bottomMargin;
        int width = Math.Max(0, right - left);
        int height = Math.Max(0, bottom - top);
        layout.PlotBounds = new UiRect(left, top, width, height);
        _plotBounds = layout.PlotBounds;
        return layout;
    }

    private void DrawTitleAndAxisLabels(UiRenderContext context, PlotLayout layout)
    {
        int padding = Math.Max(0, Padding);
        int textHeight = layout.TextHeight;
        int y = Bounds.Y + padding;

        if (ShowTitle && !string.IsNullOrWhiteSpace(Title))
        {
            int titleWidth = GetTextWidth(Title, TextScale);
            int titleX = Bounds.X + (Bounds.Width - titleWidth) / 2;
            context.Renderer.DrawText(Title, new UiPoint(titleX, y), TextColor, TextScale);
            y += textHeight + Math.Max(0, TitlePadding);
        }

        if (ShowAxisLabels && !string.IsNullOrWhiteSpace(YAxisLabel))
        {
            context.Renderer.DrawText(YAxisLabel, new UiPoint(Bounds.X + padding, y), TextColor, TextScale);
            y += textHeight + Math.Max(0, AxisLabelPadding);
        }

        if (ShowAxisLabels && !string.IsNullOrWhiteSpace(XAxisLabel))
        {
            int labelWidth = GetTextWidth(XAxisLabel, TextScale);
            int labelX = Bounds.X + (Bounds.Width - labelWidth) / 2;
            int labelY = Bounds.Bottom - padding - textHeight;
            context.Renderer.DrawText(XAxisLabel, new UiPoint(labelX, labelY), TextColor, TextScale);
        }
    }

    private void DrawGrid(UiRenderContext context, PlotLayout layout)
    {
        UiRect plot = layout.PlotBounds;
        if (plot.Width <= 0 || plot.Height <= 0)
        {
            return;
        }

        int thickness = Math.Max(1, GridThickness);

        if (ClipPlot)
        {
            context.Renderer.PushClip(plot);
        }

        foreach (float value in layout.XTicks)
        {
            int x = DataToScreenX(plot, value);
            context.Renderer.FillRect(new UiRect(x, plot.Y, thickness, plot.Height), GridColor);
        }

        foreach (float value in layout.YTicks)
        {
            int y = DataToScreenY(plot, value);
            context.Renderer.FillRect(new UiRect(plot.X, y, plot.Width, thickness), GridColor);
        }

        if (ClipPlot)
        {
            context.Renderer.PopClip();
        }
    }

    private void DrawAxes(UiRenderContext context, UiRect plot)
    {
        int thickness = Math.Max(1, AxisThickness);
        context.Renderer.FillRect(new UiRect(plot.X, plot.Bottom - thickness, plot.Width, thickness), AxisColor);
        context.Renderer.FillRect(new UiRect(plot.X, plot.Y, thickness, plot.Height), AxisColor);
    }

    private void DrawTicks(UiRenderContext context, PlotLayout layout)
    {
        UiRect plot = layout.PlotBounds;
        int tickLength = Math.Max(0, TickLength);
        int tickPadding = Math.Max(0, TickLabelPadding);
        int textHeight = layout.TextHeight;

        for (int i = 0; i < layout.XTicks.Count; i++)
        {
            float value = layout.XTicks[i];
            int x = DataToScreenX(plot, value);
            if (ShowTicks && tickLength > 0)
            {
                context.Renderer.FillRect(new UiRect(x, plot.Bottom, 1, tickLength), AxisColor);
            }

            if (ShowLabels && i < layout.XTickLabels.Count)
            {
                string label = layout.XTickLabels[i];
                int labelWidth = GetTextWidth(label, TextScale);
                int labelX = x - labelWidth / 2;
                int labelY = plot.Bottom + tickLength + tickPadding;
                context.Renderer.DrawText(label, new UiPoint(labelX, labelY), TickLabelColor, TextScale);
            }
        }

        for (int i = 0; i < layout.YTicks.Count; i++)
        {
            float value = layout.YTicks[i];
            int y = DataToScreenY(plot, value);
            if (ShowTicks && tickLength > 0)
            {
                context.Renderer.FillRect(new UiRect(plot.X - tickLength, y, tickLength, 1), AxisColor);
            }

            if (ShowLabels && i < layout.YTickLabels.Count)
            {
                string label = layout.YTickLabels[i];
                int labelWidth = GetTextWidth(label, TextScale);
                int labelX = plot.X - tickLength - tickPadding - labelWidth;
                int labelY = y - textHeight / 2;
                context.Renderer.DrawText(label, new UiPoint(labelX, labelY), TickLabelColor, TextScale);
            }
        }
    }

    private void DrawSeries(UiRenderContext context, UiRect plot)
    {
        if (Series.Count == 0 || plot.Width <= 0 || plot.Height <= 0)
        {
            return;
        }

        if (ClipPlot)
        {
            context.Renderer.PushClip(plot);
        }

        foreach (UiPlotSeries series in Series)
        {
            if (!series.Visible)
            {
                continue;
            }

            IReadOnlyList<UiPlotPoint> points = series.Points;
            if (points.Count < 2)
            {
                continue;
            }

            UiPlotPoint prev = points[0];
            int x0 = DataToScreenX(plot, prev.X);
            int y0 = DataToScreenY(plot, prev.Y);

            for (int i = 1; i < points.Count; i++)
            {
                UiPlotPoint next = points[i];
                int x1 = DataToScreenX(plot, next.X);
                int y1 = DataToScreenY(plot, next.Y);
                DrawLine(context.Renderer, x0, y0, x1, y1, series.LineThickness, series.LineColor);
                x0 = x1;
                y0 = y1;
            }
        }

        if (ClipPlot)
        {
            context.Renderer.PopClip();
        }
    }

    private void ApplyPan(UiRect plotBounds, UiPoint mouse)
    {
        float width = Math.Max(1f, plotBounds.Width);
        float height = Math.Max(1f, plotBounds.Height);
        float rangeX = _panStartXMax - _panStartXMin;
        float rangeY = _panStartYMax - _panStartYMin;

        float deltaX = mouse.X - _panStartMouse.X;
        float deltaY = mouse.Y - _panStartMouse.Y;

        float shiftX = -(deltaX / width) * rangeX;
        float shiftY = (deltaY / height) * rangeY;

        XMin = _panStartXMin + shiftX;
        XMax = _panStartXMax + shiftX;
        YMin = _panStartYMin + shiftY;
        YMax = _panStartYMax + shiftY;
        ClampView();
    }

    private void ApplyZoom(UiRect plotBounds, UiPoint mouse, int scrollDelta)
    {
        int steps = (int)Math.Round(scrollDelta / 120f);
        if (steps == 0)
        {
            return;
        }

        float zoomStep = Math.Clamp(ZoomStep, 0.01f, 0.95f);
        float factor = steps > 0 ? MathF.Pow(1f - zoomStep, steps) : MathF.Pow(1f + zoomStep, -steps);
        float xRange = XMax - XMin;
        float yRange = YMax - YMin;

        if (ZoomToCursor)
        {
            float anchorX = ScreenToDataX(plotBounds, mouse.X);
            float anchorY = ScreenToDataY(plotBounds, mouse.Y);

            float newRangeX = xRange * factor;
            float newRangeY = yRange * factor;
            XMin = anchorX - (anchorX - XMin) * (newRangeX / xRange);
            XMax = XMin + newRangeX;
            YMin = anchorY - (anchorY - YMin) * (newRangeY / yRange);
            YMax = YMin + newRangeY;
        }
        else
        {
            float centerX = (XMin + XMax) * 0.5f;
            float centerY = (YMin + YMax) * 0.5f;
            float newRangeX = xRange * factor;
            float newRangeY = yRange * factor;
            XMin = centerX - newRangeX * 0.5f;
            XMax = centerX + newRangeX * 0.5f;
            YMin = centerY - newRangeY * 0.5f;
            YMax = centerY + newRangeY * 0.5f;
        }

        ClampView();
    }

    private void NormalizeView()
    {
        if (XMax <= XMin)
        {
            XMax = XMin + 1f;
        }

        if (YMax <= YMin)
        {
            YMax = YMin + 1f;
        }

        ClampView();
    }

    private void ClampView()
    {
        float xRange = Math.Max(XMax - XMin, 0.0001f);
        float yRange = Math.Max(YMax - YMin, 0.0001f);
        float minXRange = Math.Max(0f, MinXRange);
        float minYRange = Math.Max(0f, MinYRange);
        float maxXRange = MaxXRange > 0f ? MaxXRange : float.PositiveInfinity;
        float maxYRange = MaxYRange > 0f ? MaxYRange : float.PositiveInfinity;

        if (xRange < minXRange)
        {
            float center = (XMin + XMax) * 0.5f;
            xRange = minXRange;
            XMin = center - xRange * 0.5f;
            XMax = center + xRange * 0.5f;
        }
        else if (xRange > maxXRange)
        {
            float center = (XMin + XMax) * 0.5f;
            xRange = maxXRange;
            XMin = center - xRange * 0.5f;
            XMax = center + xRange * 0.5f;
        }

        if (yRange < minYRange)
        {
            float center = (YMin + YMax) * 0.5f;
            yRange = minYRange;
            YMin = center - yRange * 0.5f;
            YMax = center + yRange * 0.5f;
        }
        else if (yRange > maxYRange)
        {
            float center = (YMin + YMax) * 0.5f;
            yRange = maxYRange;
            YMin = center - yRange * 0.5f;
            YMax = center + yRange * 0.5f;
        }

        if (ClampViewToData && TryGetDataBounds(out float dataMinX, out float dataMaxX, out float dataMinY, out float dataMaxY))
        {
            float clampXRange = XMax - XMin;
            if (XMin < dataMinX)
            {
                XMin = dataMinX;
                XMax = XMin + clampXRange;
            }
            if (XMax > dataMaxX)
            {
                XMax = dataMaxX;
                XMin = XMax - clampXRange;
            }

            float clampYRange = YMax - YMin;
            if (YMin < dataMinY)
            {
                YMin = dataMinY;
                YMax = YMin + clampYRange;
            }
            if (YMax > dataMaxY)
            {
                YMax = dataMaxY;
                YMin = YMax - clampYRange;
            }
        }
    }

    private bool TryGetDataBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minY = float.PositiveInfinity;
        maxY = float.NegativeInfinity;
        bool hasPoint = false;

        foreach (UiPlotSeries series in Series)
        {
            IReadOnlyList<UiPlotPoint> points = series.Points;
            for (int i = 0; i < points.Count; i++)
            {
                UiPlotPoint point = points[i];
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
                hasPoint = true;
            }
        }

        if (!hasPoint)
        {
            minX = 0f;
            maxX = 1f;
            minY = 0f;
            maxY = 1f;
            return false;
        }

        return true;
    }

    private int DataToScreenX(UiRect plot, float x)
    {
        float range = XMax - XMin;
        if (range <= 0f || plot.Width <= 1)
        {
            return plot.X;
        }

        float t = (x - XMin) / range;
        return plot.X + (int)Math.Round(t * Math.Max(0, plot.Width - 1));
    }

    private int DataToScreenY(UiRect plot, float y)
    {
        float range = YMax - YMin;
        if (range <= 0f || plot.Height <= 1)
        {
            return plot.Bottom;
        }

        float t = (y - YMin) / range;
        int offset = (int)Math.Round(t * Math.Max(0, plot.Height - 1));
        return plot.Bottom - 1 - offset;
    }

    private float ScreenToDataX(UiRect plot, int x)
    {
        float range = XMax - XMin;
        if (range <= 0f || plot.Width <= 1)
        {
            return XMin;
        }

        float t = (x - plot.X) / (float)Math.Max(1, plot.Width - 1);
        return XMin + t * range;
    }

    private float ScreenToDataY(UiRect plot, int y)
    {
        float range = YMax - YMin;
        if (range <= 0f || plot.Height <= 1)
        {
            return YMin;
        }

        float t = (plot.Bottom - 1 - y) / (float)Math.Max(1, plot.Height - 1);
        return YMin + t * range;
    }

    private int GetTextWidth(string text, int scale)
    {
        if (MeasureTextWidth != null)
        {
            return MeasureTextWidth(text, scale);
        }

        return text.Length * FallbackCharWidth * scale;
    }

    private int GetTextHeight(int scale)
    {
        if (MeasureTextHeight != null)
        {
            return MeasureTextHeight(scale);
        }

        return FallbackCharHeight * scale;
    }

    private static void BuildTicks(
        float min,
        float max,
        int targetCount,
        List<float> ticks,
        List<string> labels,
        Func<float, string>? formatter)
    {
        ticks.Clear();
        labels.Clear();

        if (targetCount < 2 || max <= min)
        {
            ticks.Add(min);
            labels.Add(FormatTick(min, formatter));
            return;
        }

        double range = NiceNumber(max - min, false);
        double step = NiceNumber(range / (targetCount - 1), true);
        if (step <= 0.0)
        {
            ticks.Add(min);
            labels.Add(FormatTick(min, formatter));
            return;
        }

        double tickMin = Math.Floor(min / step) * step;
        double tickMax = Math.Ceiling(max / step) * step;
        int guard = 0;

        for (double value = tickMin; value <= tickMax + step * 0.5; value += step)
        {
            if (guard++ > 1000)
            {
                break;
            }

            float f = (float)value;
            ticks.Add(f);
            labels.Add(FormatTick(f, formatter));
        }
    }

    private static double NiceNumber(double range, bool round)
    {
        if (range <= 0.0)
        {
            return 1.0;
        }

        double exponent = Math.Floor(Math.Log10(range));
        double fraction = range / Math.Pow(10.0, exponent);
        double niceFraction;

        if (round)
        {
            if (fraction < 1.5)
            {
                niceFraction = 1.0;
            }
            else if (fraction < 3.0)
            {
                niceFraction = 2.0;
            }
            else if (fraction < 7.0)
            {
                niceFraction = 5.0;
            }
            else
            {
                niceFraction = 10.0;
            }
        }
        else
        {
            if (fraction <= 1.0)
            {
                niceFraction = 1.0;
            }
            else if (fraction <= 2.0)
            {
                niceFraction = 2.0;
            }
            else if (fraction <= 5.0)
            {
                niceFraction = 5.0;
            }
            else
            {
                niceFraction = 10.0;
            }
        }

        return niceFraction * Math.Pow(10.0, exponent);
    }

    private static string FormatTick(float value, Func<float, string>? formatter)
    {
        if (formatter != null)
        {
            return formatter(value);
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void DrawLine(IUiRenderer renderer, int x0, int y0, int x1, int y1, int thickness, UiColor color)
    {
        int size = Math.Max(1, thickness);
        int half = size / 2;

        if (x0 == x1)
        {
            int top = Math.Min(y0, y1);
            int height = Math.Abs(y1 - y0) + 1;
            renderer.FillRect(new UiRect(x0 - half, top, size, height), color);
            return;
        }

        if (y0 == y1)
        {
            int left = Math.Min(x0, x1);
            int width = Math.Abs(x1 - x0) + 1;
            renderer.FillRect(new UiRect(left, y0 - half, width, size), color);
            return;
        }

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int x = x0;
        int y = y0;
        while (true)
        {
            renderer.FillRect(new UiRect(x - half, y - half, size, size), color);
            if (x == x1 && y == y1)
            {
                break;
            }

            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
}
