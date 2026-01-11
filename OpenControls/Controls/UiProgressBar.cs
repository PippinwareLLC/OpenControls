namespace OpenControls.Controls;

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
        int fillWidth = (int)Math.Round(Bounds.Width * normalized);
        if (fillWidth > 0)
        {
            UiRect fillRect = new UiRect(Bounds.X, Bounds.Y, fillWidth, Bounds.Height);
            int fillRadius = Math.Min(CornerRadius, Math.Min(fillRect.Width, fillRect.Height) / 2);
            UiRenderHelpers.FillRectRounded(context.Renderer, fillRect, fillRadius, Fill);
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
