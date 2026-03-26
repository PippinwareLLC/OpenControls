using System.Globalization;

namespace OpenControls.Controls;

public sealed class UiDragFloat : UiElement
{
    private readonly UiInputFloat _inputField;
    private bool _dragging;
    private bool _hovered;
    private bool _focused;
    private bool _inputMode;
    private bool _pendingFocusSelf;
    private int _dragStartX;
    private float _inputSnapshotValue;
    private float _dragStartValue;
    private float _value;
    private float _min;
    private float _max = 1f;

    public UiDragFloat()
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

    public float Speed { get; set; } = 0.1f;
    public float Step { get; set; }
    public bool WholeNumbers { get; set; }
    public UiDragFlags Flags { get; set; } = UiDragFlags.Clamp;
    public float SlowSpeedMultiplier { get; set; } = 0.1f;
    public float FastSpeedMultiplier { get; set; } = 10f;
    public string ValueFormat { get; set; } = "0.##";
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 4;

    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor ActiveBackground { get; set; } = new UiColor(50, 58, 76);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

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

        if (!Flags.HasFlag(UiDragFlags.NoInput) &&
            input.LeftClicked &&
            _hovered &&
            (input.LeftDoubleClicked || input.PrimaryShortcutDown))
        {
            EnterInputMode(context);
            base.Update(context);
            return;
        }

        if (input.LeftClicked && _hovered)
        {
            _dragging = true;
            _dragStartX = input.MousePosition.X;
            _dragStartValue = _value;
            context.Focus.RequestFocus(this);
        }

        if (_dragging && input.LeftDown)
        {
            float delta = input.MousePosition.X - _dragStartX;
            float speed = GetDragSpeed(input);
            SetValue(ApplyDrag(_dragStartValue, delta, speed));
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

            if (input.Navigation.Home && HasRange())
            {
                SetValue(_min);
            }

            if (input.Navigation.End && HasRange())
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

        UiColor fill = _dragging ? ActiveBackground : (_hovered ? HoverBackground : Background);
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, fill);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        string text = FormatValue(_value);
        int textWidth = context.Renderer.MeasureTextWidth(text, TextScale);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textX = Bounds.X + Padding + (Bounds.Width - Padding * 2 - textWidth) / 2;
        int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
        context.Renderer.DrawText(text, new UiPoint(textX, textY), TextColor, TextScale);

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
        _inputField.Clamp = Flags.HasFlag(UiDragFlags.AlwaysClamp) && !Flags.HasFlag(UiDragFlags.WrapAround);
        _inputField.WholeNumbers = WholeNumbers;
        _inputField.Step = Step;
        _inputField.StepFast = Step > 0f ? Step * 10f : 0f;
        _inputField.ValueFormat = ValueFormat;
        _inputField.TextScale = TextScale;
        _inputField.Padding = Padding;
    }

    private bool HasRange()
    {
        return _max > _min;
    }

    private bool CanUseLogScale()
    {
        return Flags.HasFlag(UiDragFlags.Logarithmic) && HasRange() && _min > 0f && _max > 0f;
    }

    private float ApplyDrag(float startValue, float delta, float speed)
    {
        if (!CanUseLogScale())
        {
            return startValue + delta * speed;
        }

        float min = MathF.Max(_min, 0.000001f);
        float max = MathF.Max(_max, min + 0.000001f);
        float logMin = MathF.Log(min);
        float logMax = MathF.Log(max);
        float logRange = logMax - logMin;
        if (logRange <= 0f)
        {
            return startValue;
        }

        float clamped = Math.Clamp(startValue, _min, _max);
        float logValue = MathF.Log(MathF.Max(clamped, min));
        float normalized = (logValue - logMin) / logRange;
        float normalizedDelta = delta * speed / 100f;
        normalized = Math.Clamp(normalized + normalizedDelta, 0f, 1f);
        return MathF.Exp(logMin + normalized * logRange);
    }

    private float GetDragSpeed(UiInputState input)
    {
        float speed = Speed;
        if (speed <= 0f)
        {
            float range = MathF.Abs(_max - _min);
            speed = range > 0f ? range / 200f : 0.1f;
        }

        if (!Flags.HasFlag(UiDragFlags.NoSlowFast))
        {
            if (input.CtrlDown)
            {
                speed *= SlowSpeedMultiplier;
            }

            if (input.ShiftDown)
            {
                speed *= FastSpeedMultiplier;
            }
        }

        return speed;
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

        if (Speed > 0f)
        {
            return Speed;
        }

        float range = MathF.Abs(_max - _min);
        return range > 0f ? range / 200f : 0.1f;
    }

    private void AdjustValue(float delta)
    {
        if (delta == 0f)
        {
            return;
        }

        SetValue(_value + delta);
    }

    private void SetValue(float value)
    {
        float next = UiNumericValueHelpers.ApplyFloatConstraints(
            value,
            _min,
            _max,
            Step,
            WholeNumbers,
            ValueFormat,
            clampByDefault: false,
            UiNumericValueHelpers.ToModifierFlags(Flags));

        if (Math.Abs(_value - next) <= float.Epsilon)
        {
            return;
        }

        _value = next;
        ValueChanged?.Invoke(_value);
    }

    private string FormatValue(float value)
    {
        return value.ToString(ValueFormat, CultureInfo.InvariantCulture);
    }
}
