using System.Globalization;

namespace OpenControls.Controls;

public sealed class UiVSlider : UiElement
{
    private readonly UiInputFloat _inputField;
    private bool _dragging;
    private bool _hovered;
    private bool _focused;
    private bool _inputMode;
    private bool _pendingFocusSelf;
    private float _inputSnapshotValue;
    private float _value;
    private float _min;
    private float _max = 1f;

    public UiVSlider()
    {
        _inputField = new UiInputFloat
        {
            Visible = false
        };
        _inputField.ValueChanged += value => SetValue(value);
        _inputField.Submitted += HandleInputSubmitted;
        _inputField.Cancelled += HandleInputCancelled;
        AddChild(_inputField);
    }

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
    public UiSliderFlags Flags { get; set; } = UiSliderFlags.AlwaysClamp;
    public bool ShowValue { get; set; } = true;
    public string ValueFormat { get; set; } = "0.##";
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 4;
    public int TrackWidth { get; set; } = 6;
    public int ThumbHeight { get; set; } = 12;
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
        SyncInputFieldOptions();

        if (_inputMode)
        {
            _inputField.Bounds = Bounds;
            _inputField.Visible = true;
            base.Update(context);

            if (context.Focus.Focused != _inputField.TextField)
            {
                _inputMode = false;
                _inputField.Visible = false;
            }

            if (_pendingFocusSelf)
            {
                _pendingFocusSelf = false;
                context.Focus.RequestFocus(this);
            }

            return;
        }

        UiRect trackRect = GetTrackRect();
        UiRect thumbRect = GetThumbRect(trackRect);
        if (!Flags.HasFlag(UiSliderFlags.NoInput) &&
            input.LeftClicked &&
            Bounds.Contains(input.MousePosition) &&
            (input.LeftDoubleClicked || input.PrimaryShortcutDown))
        {
            EnterInputMode(context);
            base.Update(context);
            return;
        }

        if (input.LeftClicked && (thumbRect.Contains(input.MousePosition) || trackRect.Contains(input.MousePosition)))
        {
            _dragging = true;
            context.Focus.RequestFocus(this);
            SetValueFromMouse(trackRect, input.MousePosition.Y);
        }

        if (_dragging && input.LeftDown)
        {
            SetValueFromMouse(trackRect, input.MousePosition.Y);
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }

        if (_focused)
        {
            if (input.Navigation.MoveUp)
            {
                AdjustValue(GetStepValue());
            }

            if (input.Navigation.MoveDown)
            {
                AdjustValue(-GetStepValue());
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

        _inputField.Visible = false;
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        if (_inputMode)
        {
            base.Render(context);
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
        int fillHeight = (int)Math.Round(trackRect.Height * normalized);
        if (fillHeight > 0)
        {
            UiRect fillRect = new UiRect(trackRect.X, trackRect.Bottom - fillHeight, trackRect.Width, fillHeight);
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

    private void EnterInputMode(UiUpdateContext context)
    {
        _dragging = false;
        _inputMode = true;
        _inputSnapshotValue = _value;
        _inputField.Visible = true;
        _inputField.Bounds = Bounds;
        _inputField.Value = _value;
        _inputField.SelectAllText();
        context.Focus.RequestFocus(_inputField.TextField);
    }

    private void HandleInputSubmitted()
    {
        _inputMode = false;
        _inputField.Visible = false;
        _pendingFocusSelf = true;
    }

    private void HandleInputCancelled()
    {
        SetValue(_inputSnapshotValue);
        _inputMode = false;
        _inputField.Visible = false;
        _pendingFocusSelf = true;
    }

    private void SyncInputFieldOptions()
    {
        _inputField.Min = _min;
        _inputField.Max = _max;
        _inputField.Clamp = Flags.HasFlag(UiSliderFlags.AlwaysClamp) && !Flags.HasFlag(UiSliderFlags.WrapAround);
        _inputField.WholeNumbers = WholeNumbers;
        _inputField.Step = Step;
        _inputField.StepFast = Step > 0f ? Step * 10f : 0f;
        _inputField.ValueFormat = ValueFormat;
        _inputField.TextScale = TextScale;
        _inputField.Padding = Padding;
    }

    private void SetValueFromMouse(UiRect trackRect, int mouseY)
    {
        if (trackRect.Height <= 0)
        {
            SetValue(_min);
            return;
        }

        float t = (trackRect.Bottom - mouseY) / (float)trackRect.Height;
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
        float clamped = UiNumericValueHelpers.ApplyFloatConstraints(
            value,
            _min,
            _max,
            Step,
            WholeNumbers,
            ValueFormat,
            clampByDefault: true,
            UiNumericValueHelpers.ToModifierFlags(Flags));

        if (Math.Abs(_value - clamped) <= float.Epsilon)
        {
            return;
        }

        _value = clamped;
        ValueChanged?.Invoke(_value);
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
        int width = Math.Max(1, TrackWidth);
        int height = Math.Max(0, Bounds.Height - Padding * 2);
        int x = Bounds.X + (Bounds.Width - width) / 2;
        int y = Bounds.Y + Padding;
        return new UiRect(x, y, width, height);
    }

    private UiRect GetThumbRect(UiRect trackRect)
    {
        int thumbHeight = Math.Max(6, ThumbHeight);
        int thumbWidth = Math.Max(trackRect.Width + 6, trackRect.Width);
        float normalized = GetNormalizedValue();
        int range = Math.Max(0, trackRect.Height - thumbHeight);
        int thumbY = trackRect.Bottom - thumbHeight - (int)Math.Round(range * normalized);
        int thumbX = trackRect.X + (trackRect.Width - thumbWidth) / 2;
        return new UiRect(thumbX, thumbY, thumbWidth, thumbHeight);
    }

    private string FormatValue(float value)
    {
        return value.ToString(ValueFormat, CultureInfo.InvariantCulture);
    }
}
