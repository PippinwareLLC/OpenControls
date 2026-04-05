namespace OpenControls.Controls;

public enum UiProgressBarStyle
{
    Linear,
    Radial
}

public enum UiProgressBarFillDirection
{
    LeftToRight,
    RightToLeft,
    BottomToTop,
    TopToBottom
}

public sealed class UiProgressBar : UiElement
{
    private float _value;
    private float _min;
    private float _max = 1f;
    private UiProgressBarStyle _style = UiProgressBarStyle.Linear;
    private UiProgressBarFillDirection _fillDirection = UiProgressBarFillDirection.LeftToRight;
    private int _segmentCount;
    private int _segmentGap = 2;
    private IReadOnlyList<UiColor>? _segmentFillColors;
    private float _radialStartAngleDegrees = -90f;
    private bool _radialClockwise = true;
    private int _radialThickness;
    private bool _showText = true;
    private string? _text;
    private int _textScale = 1;
    private UiColor _background = new UiColor(24, 28, 38);
    private UiColor _fill = new UiColor(70, 120, 180);
    private UiColor _border = new UiColor(60, 70, 90);
    private UiColor _textColor = UiColor.White;
    private int _cornerRadius;

    public float Min
    {
        get => _min;
        set
        {
            if (!SetInvalidatingValue(ref _min, value, UiInvalidationReason.State | UiInvalidationReason.Text | UiInvalidationReason.Paint))
            {
                return;
            }

            _value = ClampValue(_value);
        }
    }

    public float Max
    {
        get => _max;
        set
        {
            if (!SetInvalidatingValue(ref _max, value, UiInvalidationReason.State | UiInvalidationReason.Text | UiInvalidationReason.Paint))
            {
                return;
            }

            _value = ClampValue(_value);
        }
    }

    public float Value
    {
        get => _value;
        set => SetInvalidatingValue(ref _value, ClampValue(value), UiInvalidationReason.State | UiInvalidationReason.Text | UiInvalidationReason.Paint);
    }

    public UiProgressBarStyle Style
    {
        get => _style;
        set => SetInvalidatingValue(ref _style, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiProgressBarFillDirection FillDirection
    {
        get => _fillDirection;
        set => SetInvalidatingValue(ref _fillDirection, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int SegmentCount
    {
        get => _segmentCount;
        set => SetInvalidatingValue(ref _segmentCount, Math.Max(0, value), UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int SegmentGap
    {
        get => _segmentGap;
        set => SetInvalidatingValue(ref _segmentGap, Math.Max(0, value), UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public IReadOnlyList<UiColor>? SegmentFillColors
    {
        get => _segmentFillColors;
        set => SetInvalidatingValue(ref _segmentFillColors, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public float RadialStartAngleDegrees
    {
        get => _radialStartAngleDegrees;
        set => SetInvalidatingValue(ref _radialStartAngleDegrees, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public bool RadialClockwise
    {
        get => _radialClockwise;
        set => SetInvalidatingValue(ref _radialClockwise, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int RadialThickness
    {
        get => _radialThickness;
        set => SetInvalidatingValue(ref _radialThickness, Math.Max(0, value), UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public bool ShowText
    {
        get => _showText;
        set => SetInvalidatingValue(ref _showText, value, UiInvalidationReason.Text | UiInvalidationReason.Paint);
    }

    public string? Text
    {
        get => _text;
        set => SetInvalidatingValue(ref _text, value, UiInvalidationReason.Text | UiInvalidationReason.Paint);
    }

    public int TextScale
    {
        get => _textScale;
        set => SetInvalidatingValue(ref _textScale, Math.Max(1, value), UiInvalidationReason.Text | UiInvalidationReason.Paint);
    }

    public UiColor Background
    {
        get => _background;
        set => SetInvalidatingValue(ref _background, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor Fill
    {
        get => _fill;
        set => SetInvalidatingValue(ref _fill, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor Border
    {
        get => _border;
        set => SetInvalidatingValue(ref _border, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public UiColor TextColor
    {
        get => _textColor;
        set => SetInvalidatingValue(ref _textColor, value, UiInvalidationReason.Style | UiInvalidationReason.Paint);
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set => SetInvalidatingValue(ref _cornerRadius, Math.Max(0, value), UiInvalidationReason.Style | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);

        float normalized = GetNormalizedValue();
        if (Style == UiProgressBarStyle.Radial)
        {
            RenderRadialFill(context, normalized);
        }
        else if (SegmentCount > 1)
        {
            RenderSegments(context, normalized);
        }
        else
        {
            RenderContinuousFill(context, normalized);
        }

        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        if (ShowText)
        {
            string text = !string.IsNullOrWhiteSpace(Text)
                ? Text
                : $"{Math.Round(normalized * 100f):0}%";
            int textWidth = context.Renderer.MeasureTextWidth(text, TextScale);
            int textHeight = context.Renderer.MeasureTextHeight(TextScale);
            int textX = Bounds.X + (Bounds.Width - textWidth) / 2;
            int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
            context.Renderer.DrawText(text, new UiPoint(textX, textY), TextColor, TextScale);
        }

        base.Render(context);
    }

    private void RenderContinuousFill(UiRenderContext context, float normalized)
    {
        UiRect fillRect = GetFillRect(normalized);
        if (fillRect.Width <= 0 || fillRect.Height <= 0)
        {
            return;
        }

        int fillRadius = Math.Min(CornerRadius, Math.Min(fillRect.Width, fillRect.Height) / 2);
        UiRenderHelpers.FillRectRounded(context.Renderer, fillRect, fillRadius, Fill);
    }

    private void RenderSegments(UiRenderContext context, float normalized)
    {
        int segments = Math.Max(1, SegmentCount);
        int gap = Math.Max(0, SegmentGap);
        bool horizontal = FillDirection == UiProgressBarFillDirection.LeftToRight
            || FillDirection == UiProgressBarFillDirection.RightToLeft;

        int totalLength = horizontal ? Bounds.Width : Bounds.Height;
        int crossLength = horizontal ? Bounds.Height : Bounds.Width;
        int totalGap = gap * Math.Max(0, segments - 1);
        int available = totalLength - totalGap;
        if (available <= 0 || crossLength <= 0)
        {
            return;
        }

        int baseSize = available / segments;
        int remainder = available % segments;
        int filledSegments = (int)Math.Floor(normalized * segments);
        filledSegments = Math.Clamp(filledSegments, 0, segments);

        int offset = 0;
        for (int i = 0; i < segments; i++)
        {
            int size = baseSize + (i < remainder ? 1 : 0);
            if (size > 0 && i < filledSegments)
            {
                UiRect segmentRect = GetSegmentRect(offset, size);
                if (segmentRect.Width > 0 && segmentRect.Height > 0)
                {
                    UiColor color = GetSegmentFillColor(i);
                    int fillRadius = Math.Min(CornerRadius, Math.Min(segmentRect.Width, segmentRect.Height) / 2);
                    UiRenderHelpers.FillRectRounded(context.Renderer, segmentRect, fillRadius, color);
                }
            }

            offset += size + gap;
        }
    }

    private UiRect GetFillRect(float normalized)
    {
        int width = Bounds.Width;
        int height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return new UiRect(Bounds.X, Bounds.Y, 0, 0);
        }

        switch (FillDirection)
        {
            case UiProgressBarFillDirection.RightToLeft:
                {
                    int fillWidth = Math.Max(0, (int)Math.Round(width * normalized));
                    return new UiRect(Bounds.Right - fillWidth, Bounds.Y, fillWidth, height);
                }
            case UiProgressBarFillDirection.BottomToTop:
                {
                    int fillHeight = Math.Max(0, (int)Math.Round(height * normalized));
                    return new UiRect(Bounds.X, Bounds.Bottom - fillHeight, width, fillHeight);
                }
            case UiProgressBarFillDirection.TopToBottom:
                {
                    int fillHeight = Math.Max(0, (int)Math.Round(height * normalized));
                    return new UiRect(Bounds.X, Bounds.Y, width, fillHeight);
                }
            case UiProgressBarFillDirection.LeftToRight:
            default:
                {
                    int fillWidth = Math.Max(0, (int)Math.Round(width * normalized));
                    return new UiRect(Bounds.X, Bounds.Y, fillWidth, height);
                }
        }
    }

    private UiRect GetSegmentRect(int offset, int size)
    {
        switch (FillDirection)
        {
            case UiProgressBarFillDirection.RightToLeft:
                return new UiRect(Bounds.Right - offset - size, Bounds.Y, size, Bounds.Height);
            case UiProgressBarFillDirection.BottomToTop:
                return new UiRect(Bounds.X, Bounds.Bottom - offset - size, Bounds.Width, size);
            case UiProgressBarFillDirection.TopToBottom:
                return new UiRect(Bounds.X, Bounds.Y + offset, Bounds.Width, size);
            case UiProgressBarFillDirection.LeftToRight:
            default:
                return new UiRect(Bounds.X + offset, Bounds.Y, size, Bounds.Height);
        }
    }

    private UiColor GetSegmentFillColor(int index)
    {
        if (SegmentFillColors == null || SegmentFillColors.Count == 0)
        {
            return Fill;
        }

        if (index < SegmentFillColors.Count)
        {
            return SegmentFillColors[index];
        }

        return SegmentFillColors[SegmentFillColors.Count - 1];
    }

    private void RenderRadialFill(UiRenderContext context, float normalized)
    {
        if (normalized <= 0f)
        {
            return;
        }

        int size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
        {
            return;
        }

        float radius = size / 2f;
        float centerX = Bounds.X + Bounds.Width / 2f;
        float centerY = Bounds.Y + Bounds.Height / 2f;
        float outerRadiusSquared = radius * radius;
        int thickness = Math.Max(0, RadialThickness);
        float innerRadius = thickness == 0 ? 0f : Math.Max(0f, radius - thickness);
        float innerRadiusSquared = innerRadius * innerRadius;

        float sweep = normalized >= 1f ? 360f : normalized * 360f;
        if (!RadialClockwise)
        {
            sweep = -sweep;
        }

        bool full = Math.Abs(sweep) >= 359.999f;
        int minX = (int)Math.Floor(centerX - radius);
        int maxX = (int)Math.Ceiling(centerX + radius);
        int minY = (int)Math.Floor(centerY - radius);
        int maxY = (int)Math.Ceiling(centerY + radius);

        for (int y = minY; y < maxY; y++)
        {
            float dy = (y + 0.5f) - centerY;
            for (int x = minX; x < maxX; x++)
            {
                float dx = (x + 0.5f) - centerX;
                float distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > outerRadiusSquared || distanceSquared < innerRadiusSquared)
                {
                    continue;
                }

                if (!full)
                {
                    float angle = MathF.Atan2(dy, dx) * (180f / MathF.PI);
                    if (!IsAngleWithinSweep(angle, RadialStartAngleDegrees, sweep))
                    {
                        continue;
                    }
                }

                context.Renderer.FillRect(new UiRect(x, y, 1, 1), Fill);
            }
        }
    }

    private static bool IsAngleWithinSweep(float angle, float start, float sweep)
    {
        float normalizedAngle = NormalizeAngle(angle);
        float normalizedStart = NormalizeAngle(start);
        float delta = NormalizeAngle(normalizedAngle - normalizedStart);

        if (sweep >= 0f)
        {
            return delta <= sweep;
        }

        return delta >= 360f + sweep;
    }

    private static float NormalizeAngle(float degrees)
    {
        float value = degrees % 360f;
        if (value < 0f)
        {
            value += 360f;
        }

        return value;
    }

    private float GetNormalizedValue()
    {
        if (_max <= _min)
        {
            return 0f;
        }

        return Math.Clamp((_value - _min) / (_max - _min), 0f, 1f);
    }

    private float ClampValue(float value)
    {
        if (_max <= _min)
        {
            return _min;
        }

        return Math.Clamp(value, _min, _max);
    }
}
