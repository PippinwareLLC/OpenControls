namespace OpenControls.Controls;

public enum UiWaveformRenderMode
{
    MinMax,
    Line
}

public sealed class UiWaveform : UiElement
{
    private UiRect _plotBounds;

    public IReadOnlyList<float> Samples { get; set; } = Array.Empty<float>();
    public int StartIndex { get; set; }
    public int SampleCount { get; set; }
    public float Gain { get; set; } = 1f;
    public float MinValue { get; set; } = -1f;
    public float MaxValue { get; set; } = 1f;
    public bool AutoScale { get; set; }
    public UiWaveformRenderMode RenderMode { get; set; } = UiWaveformRenderMode.MinMax;

    public int Padding { get; set; } = 4;
    public int LineThickness { get; set; } = 1;
    public bool ShowZeroLine { get; set; } = true;
    public float ZeroLineValue { get; set; }
    public int ZeroLineThickness { get; set; } = 1;
    public int CornerRadius { get; set; }
    public bool ClipWaveform { get; set; } = true;

    public UiColor Background { get; set; } = new UiColor(18, 22, 32);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor WaveColor { get; set; } = new UiColor(120, 180, 220);
    public UiColor ZeroLineColor { get; set; } = new UiColor(70, 80, 100);

    public UiRect PlotBounds => _plotBounds;

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        UiRect plot = BuildPlotBounds();
        if (plot.Width <= 0 || plot.Height <= 0)
        {
            base.Render(context);
            return;
        }

        GetSampleRange(out int startIndex, out int count);
        GetValueRange(startIndex, count, out float minValue, out float maxValue);

        if (ClipWaveform)
        {
            context.Renderer.PushClip(plot);
        }

        if (ShowZeroLine)
        {
            DrawZeroLine(context.Renderer, plot, minValue, maxValue);
        }

        if (count > 0)
        {
            switch (RenderMode)
            {
                case UiWaveformRenderMode.Line:
                    DrawLineWaveform(context.Renderer, plot, startIndex, count, minValue, maxValue);
                    break;
                case UiWaveformRenderMode.MinMax:
                default:
                    DrawMinMaxWaveform(context.Renderer, plot, startIndex, count, minValue, maxValue);
                    break;
            }
        }

        if (ClipWaveform)
        {
            context.Renderer.PopClip();
        }

        base.Render(context);
    }

    private UiRect BuildPlotBounds()
    {
        int padding = Math.Max(0, Padding);
        int width = Math.Max(0, Bounds.Width - padding * 2);
        int height = Math.Max(0, Bounds.Height - padding * 2);
        _plotBounds = new UiRect(Bounds.X + padding, Bounds.Y + padding, width, height);
        return _plotBounds;
    }

    private void GetSampleRange(out int startIndex, out int count)
    {
        IReadOnlyList<float> samples = Samples ?? Array.Empty<float>();
        int total = samples.Count;
        int start = Math.Clamp(StartIndex, 0, total);
        int available = Math.Max(0, total - start);
        int desired = SampleCount <= 0 ? available : Math.Min(SampleCount, available);
        startIndex = start;
        count = Math.Max(0, desired);
    }

    private void GetValueRange(int startIndex, int count, out float minValue, out float maxValue)
    {
        float rangeMin = MinValue;
        float rangeMax = MaxValue;

        if (AutoScale && count > 0)
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            IReadOnlyList<float> samples = Samples ?? Array.Empty<float>();

            for (int i = 0; i < count; i++)
            {
                float value = samples[startIndex + i] * Gain;
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    continue;
                }

                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            if (min != float.PositiveInfinity && max != float.NegativeInfinity)
            {
                rangeMin = min;
                rangeMax = max;
            }
        }

        if (rangeMax <= rangeMin)
        {
            float center = rangeMin;
            rangeMin = center - 1f;
            rangeMax = center + 1f;
        }

        minValue = rangeMin;
        maxValue = rangeMax;
    }

    private void DrawZeroLine(IUiRenderer renderer, UiRect plot, float minValue, float maxValue)
    {
        int thickness = Math.Max(1, ZeroLineThickness);
        int y = MapValueToY(plot, ZeroLineValue, minValue, maxValue);
        int top = y - thickness / 2;
        if (top < plot.Y)
        {
            top = plot.Y;
        }
        if (top + thickness > plot.Bottom)
        {
            top = Math.Max(plot.Y, plot.Bottom - thickness);
        }

        renderer.FillRect(new UiRect(plot.X, top, plot.Width, thickness), ZeroLineColor);
    }

    private void DrawMinMaxWaveform(IUiRenderer renderer, UiRect plot, int startIndex, int count, float minValue, float maxValue)
    {
        if (plot.Width <= 0 || plot.Height <= 0 || count <= 0)
        {
            return;
        }

        int thickness = Math.Max(1, LineThickness);
        IReadOnlyList<float> samples = Samples ?? Array.Empty<float>();

        for (int x = 0; x < plot.Width; x++)
        {
            int sampleStart = (int)Math.Floor(x * count / (float)plot.Width);
            int sampleEnd = (int)Math.Floor((x + 1) * count / (float)plot.Width);
            if (sampleEnd <= sampleStart)
            {
                sampleEnd = sampleStart + 1;
            }

            if (sampleStart >= count)
            {
                break;
            }

            if (sampleEnd > count)
            {
                sampleEnd = count;
            }

            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;

            for (int i = sampleStart; i < sampleEnd; i++)
            {
                float value = samples[startIndex + i] * Gain;
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    continue;
                }

                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            if (min == float.PositiveInfinity || max == float.NegativeInfinity)
            {
                continue;
            }

            int yMin = MapValueToY(plot, min, minValue, maxValue);
            int yMax = MapValueToY(plot, max, minValue, maxValue);
            int top = Math.Min(yMin, yMax);
            int bottom = Math.Max(yMin, yMax);
            int height = Math.Max(1, bottom - top + 1);
            DrawVerticalLine(renderer, plot, plot.X + x, top, height, thickness, WaveColor);
        }
    }

    private void DrawLineWaveform(IUiRenderer renderer, UiRect plot, int startIndex, int count, float minValue, float maxValue)
    {
        if (plot.Width <= 0 || plot.Height <= 0 || count < 2)
        {
            return;
        }

        IReadOnlyList<float> samples = Samples ?? Array.Empty<float>();

        float prevValue = samples[startIndex] * Gain;
        int prevX = plot.X;
        int prevY = MapValueToY(plot, prevValue, minValue, maxValue);

        int lastIndex = count - 1;
        for (int i = 1; i < count; i++)
        {
            float value = samples[startIndex + i] * Gain;
            float t = lastIndex == 0 ? 0f : i / (float)lastIndex;
            int x = plot.X + (int)Math.Round(t * Math.Max(0, plot.Width - 1));
            int y = MapValueToY(plot, value, minValue, maxValue);
            DrawLine(renderer, prevX, prevY, x, y, LineThickness, WaveColor);
            prevX = x;
            prevY = y;
        }
    }

    private static int MapValueToY(UiRect plot, float value, float minValue, float maxValue)
    {
        if (maxValue <= minValue || plot.Height <= 1)
        {
            return plot.Bottom - 1;
        }

        float clamped = Math.Clamp(value, minValue, maxValue);
        float t = (clamped - minValue) / (maxValue - minValue);
        int offset = (int)Math.Round(t * Math.Max(0, plot.Height - 1));
        return plot.Bottom - 1 - offset;
    }

    private static void DrawVerticalLine(IUiRenderer renderer, UiRect plot, int x, int top, int height, int thickness, UiColor color)
    {
        int size = Math.Max(1, thickness);
        int half = size / 2;
        int left = x - half;
        int drawWidth = size;
        int drawHeight = height;

        if (left < plot.X)
        {
            int trim = plot.X - left;
            left = plot.X;
            drawWidth = Math.Max(0, drawWidth - trim);
        }

        if (left + drawWidth > plot.Right)
        {
            drawWidth = Math.Max(0, plot.Right - left);
        }

        int drawTop = top;
        if (drawTop < plot.Y)
        {
            int trim = plot.Y - drawTop;
            drawTop = plot.Y;
            drawHeight = Math.Max(0, drawHeight - trim);
        }

        if (drawTop + drawHeight > plot.Bottom)
        {
            drawHeight = Math.Max(0, plot.Bottom - drawTop);
        }

        if (drawWidth <= 0 || drawHeight <= 0)
        {
            return;
        }

        renderer.FillRect(new UiRect(left, drawTop, drawWidth, drawHeight), color);
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
