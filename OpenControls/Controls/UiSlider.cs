using System.Globalization;

namespace OpenControls.Controls;

public sealed class UiSlider : UiElement
{
    private bool _dragging;
    private bool _hovered;
    private bool _focused;
    private float _value;
    private float _min;
    private float _max = 1f;

    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            SetValue(_value);
        }
    }

    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            SetValue(_value);
        }
    }

    public float Value
    {
        get => _value;
        set => SetValue(value);
    }

    public float Step { get; set; }
    public bool WholeNumbers { get; set; }
    public bool ShowValue { get; set; } = true;
    public string ValueFormat { get; set; } = "0.##";
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 4;
    public int TrackHeight { get; set; } = 6;
    public int ThumbWidth { get; set; } = 12;
    public UiColor TrackColor { get; set; } = new UiColor(28, 32, 44);
    public UiColor FillColor { get; set; } = new UiColor(70, 120, 180);
    public UiColor ThumbColor { get; set; } = new UiColor(200, 210, 230);
    public UiColor ThumbHoverColor { get; set; } = new UiColor(230, 240, 255);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor ValueTextColor { get; set; } = UiColor.White;

    public event Action<float>? ValueChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        UiRect trackRect = GetTrackRect();
        UiRect thumbRect = GetThumbRect(trackRect);
        if (input.LeftClicked && (thumbRect.Contains(input.MousePosition) || trackRect.Contains(input.MousePosition)))
        {
            _dragging = true;
            context.Focus.RequestFocus(this);
            SetValueFromMouse(trackRect, input.MousePosition.X);
        }

        if (_dragging && input.LeftDown)
        {
            SetValueFromMouse(trackRect, input.MousePosition.X);
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }

        if (_focused)
        {
            if (input.Navigation.MoveLeft)
            {
                AdjustValue(-GetStepValue());
            }

            if (input.Navigation.MoveRight)
            {
                AdjustValue(GetStepValue());
            }

            if (input.Navigation.Home)
            {
                SetValue(_min);
            }

            if (input.Navigation.End)
            {
                SetValue(_max);
            }
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRect trackRect = GetTrackRect();
        UiRect thumbRect = GetThumbRect(trackRect);

        context.Renderer.FillRect(trackRect, TrackColor);
        if (Border.A > 0)
        {
            context.Renderer.DrawRect(trackRect, Border, 1);
        }

        float normalized = GetNormalizedValue();
        int fillWidth = (int)Math.Round(trackRect.Width * normalized);
        if (fillWidth > 0)
        {
            UiRect fillRect = new UiRect(trackRect.X, trackRect.Y, fillWidth, trackRect.Height);
            context.Renderer.FillRect(fillRect, FillColor);
        }

        UiColor thumbColor = (_hovered || _dragging) ? ThumbHoverColor : ThumbColor;
        context.Renderer.FillRect(thumbRect, thumbColor);
        if (Border.A > 0)
        {
            context.Renderer.DrawRect(thumbRect, Border, 1);
        }

        if (ShowValue)
        {
            string text = FormatValue(_value);
            int textWidth = context.Renderer.MeasureTextWidth(text, TextScale);
            int textHeight = context.Renderer.MeasureTextHeight(TextScale);
            int textX = Bounds.X + (Bounds.Width - textWidth) / 2;
            int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
            context.Renderer.DrawText(text, new UiPoint(textX, textY), ValueTextColor, TextScale);
        }

        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _dragging = false;
    }

    private void SetValueFromMouse(UiRect trackRect, int mouseX)
    {
        if (trackRect.Width <= 0)
        {
            SetValue(_min);
            return;
        }

        float t = (mouseX - trackRect.X) / (float)trackRect.Width;
        t = Math.Clamp(t, 0f, 1f);
        float value = _min + (_max - _min) * t;
        SetValue(value);
    }

    private void AdjustValue(float delta)
    {
        if (delta == 0f)
        {
            return;
        }

        SetValue(_value + delta);
    }

    private float GetStepValue()
    {
        if (Step > 0f)
        {
            return Step;
        }

        if (WholeNumbers)
        {
            return 1f;
        }

        float range = Math.Abs(_max - _min);
        return range <= 0f ? 0f : range / 100f;
    }

    private void SetValue(float value)
    {
        float clamped = ClampValue(value);
        if (WholeNumbers)
        {
            clamped = MathF.Round(clamped);
        }

        if (Step > 0f)
        {
            clamped = SnapToStep(clamped);
        }

        if (Math.Abs(_value - clamped) <= float.Epsilon)
        {
            return;
        }

        _value = clamped;
        ValueChanged?.Invoke(_value);
    }

    private float ClampValue(float value)
    {
        if (_max <= _min)
        {
            return _min;
        }

        return Math.Clamp(value, _min, _max);
    }

    private float SnapToStep(float value)
    {
        if (Step <= 0f)
        {
            return value;
        }

        float steps = MathF.Round((value - _min) / Step);
        return _min + steps * Step;
    }

    private float GetNormalizedValue()
    {
        if (_max <= _min)
        {
            return 0f;
        }

        return Math.Clamp((_value - _min) / (_max - _min), 0f, 1f);
    }

    private UiRect GetTrackRect()
    {
        int height = Math.Max(1, TrackHeight);
        int width = Math.Max(0, Bounds.Width - Padding * 2);
        int x = Bounds.X + Padding;
        int y = Bounds.Y + (Bounds.Height - height) / 2;
        return new UiRect(x, y, width, height);
    }

    private UiRect GetThumbRect(UiRect trackRect)
    {
        int thumbWidth = Math.Max(6, ThumbWidth);
        int thumbHeight = Math.Max(trackRect.Height + 6, trackRect.Height);
        float normalized = GetNormalizedValue();
        int range = Math.Max(0, trackRect.Width - thumbWidth);
        int thumbX = trackRect.X + (int)Math.Round(range * normalized);
        int thumbY = Bounds.Y + (Bounds.Height - thumbHeight) / 2;
        return new UiRect(thumbX, thumbY, thumbWidth, thumbHeight);
    }

    private string FormatValue(float value)
    {
        return value.ToString(ValueFormat, CultureInfo.InvariantCulture);
    }
}
