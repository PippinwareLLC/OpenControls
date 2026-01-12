namespace OpenControls.Controls;

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

    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            _value = ClampValue(_value);
        }
    }

    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            _value = ClampValue(_value);
        }
    }

    public float Value
    {
        get => _value;
        set => _value = ClampValue(value);
    }

    public UiProgressBarFillDirection FillDirection { get; set; } = UiProgressBarFillDirection.LeftToRight;
    public int SegmentCount { get; set; }
    public int SegmentGap { get; set; } = 2;
    public IReadOnlyList<UiColor>? SegmentFillColors { get; set; }
    public bool ShowText { get; set; } = true;
    public string? Text { get; set; }
    public int TextScale { get; set; } = 1;
    public UiColor Background { get; set; } = new UiColor(24, 28, 38);
    public UiColor Fill { get; set; } = new UiColor(70, 120, 180);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);

        float normalized = GetNormalizedValue();
        if (SegmentCount > 1)
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
