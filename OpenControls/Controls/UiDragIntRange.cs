using System.Globalization;

namespace OpenControls.Controls;

public sealed class UiDragIntRange : UiElement
{
    private enum RangePart
    {
        None,
        Min,
        Max
    }

    private readonly UiInputInt _minInputField;
    private readonly UiInputInt _maxInputField;
    private bool _dragging;
    private bool _focused;
    private bool _inputMode;
    private bool _pendingFocusSelf;
    private int _dragStartX;
    private int _inputSnapshotValue;
    private int _dragStartValue;
    private int _valueMin;
    private int _valueMax;
    private int _min;
    private int _max = 100;
    private bool _hasValueMin;
    private bool _hasValueMax;
    private RangePart _activePart;
    private RangePart _hoveredPart;
    private RangePart _inputPart;

    public UiDragIntRange()
    {
        _minInputField = new UiInputInt
        {
            Visible = false
        };
        _maxInputField = new UiInputInt
        {
            Visible = false
        };
        _minInputField.ValueChanged += value => SetMin(value);
        _maxInputField.ValueChanged += value => SetMax(value);
        _minInputField.Submitted += () => HandleInputSubmitted(RangePart.Min);
        _maxInputField.Submitted += () => HandleInputSubmitted(RangePart.Max);
        _minInputField.Cancelled += () => HandleInputCancelled(RangePart.Min);
        _maxInputField.Cancelled += () => HandleInputCancelled(RangePart.Max);
        AddChild(_minInputField);
        AddChild(_maxInputField);
    }

    public int Min
    {
        get => _min;
        set
        {
            _min = value;
            if (_hasValueMin)
            {
                SetMin(_valueMin);
            }

            if (_hasValueMax)
            {
                SetMax(_valueMax);
            }
        }
    }

    public int Max
    {
        get => _max;
        set
        {
            _max = value;
            if (_hasValueMin)
            {
                SetMin(_valueMin);
            }

            if (_hasValueMax)
            {
                SetMax(_valueMax);
            }
        }
    }

    public int ValueMin
    {
        get => _valueMin;
        set => SetMin(value);
    }

    public int ValueMax
    {
        get => _valueMax;
        set => SetMax(value);
    }

    public float Speed { get; set; } = 1f;
    public int Step { get; set; } = 1;
    public UiDragFlags Flags { get; set; } = UiDragFlags.Clamp;
    public bool EnsureOrder { get; set; } = true;
    public float SlowSpeedMultiplier { get; set; } = 0.1f;
    public float FastSpeedMultiplier { get; set; } = 10f;
    public string ValueFormat { get; set; } = "0";
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 4;
    public int Gap { get; set; } = 6;

    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor ActiveBackground { get; set; } = new UiColor(50, 58, 76);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public event Action<int, int>? RangeChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        UiRect minRect = GetMinRect();
        UiRect maxRect = GetMaxRect();
        _hoveredPart = RangePart.None;
        SyncInputFieldOptions();

        if (_inputMode && _inputPart != RangePart.None)
        {
            UiNumericField field = GetInputField(_inputPart);
            field.Bounds = _inputPart == RangePart.Min ? minRect : maxRect;
            field.Visible = true;
            base.Update(context);

            if (context.Focus.Focused != field.TextField)
            {
                _inputMode = false;
                _inputPart = RangePart.None;
                field.Visible = false;
            }

            if (_pendingFocusSelf)
            {
                _pendingFocusSelf = false;
                context.Focus.RequestFocus(this);
            }

            return;
        }

        if (minRect.Contains(input.MousePosition))
        {
            _hoveredPart = RangePart.Min;
        }
        else if (maxRect.Contains(input.MousePosition))
        {
            _hoveredPart = RangePart.Max;
        }

        if (!Flags.HasFlag(UiDragFlags.NoInput) &&
            input.LeftClicked &&
            _hoveredPart != RangePart.None &&
            (input.LeftDoubleClicked || input.PrimaryShortcutDown))
        {
            EnterInputMode(context, _hoveredPart);
            base.Update(context);
            return;
        }

        if (input.LeftClicked && _hoveredPart != RangePart.None)
        {
            _activePart = _hoveredPart;
            _dragging = true;
            _dragStartX = input.MousePosition.X;
            _dragStartValue = _activePart == RangePart.Max ? _valueMax : _valueMin;
            context.Focus.RequestFocus(this);
        }

        if (_dragging && input.LeftDown && _activePart != RangePart.None)
        {
            float delta = input.MousePosition.X - _dragStartX;
            float speed = GetDragSpeed(input);
            int next = ApplyDrag(_dragStartValue, delta, speed);
            if (_activePart == RangePart.Max)
            {
                SetMax(next);
            }
            else
            {
                SetMin(next);
            }
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }

        if (_focused)
        {
            RangePart part = _activePart == RangePart.None ? RangePart.Min : _activePart;
            int step = GetStepValue();

            if (input.Navigation.MoveLeft)
            {
                AdjustValue(part, -step);
            }

            if (input.Navigation.MoveRight)
            {
                AdjustValue(part, step);
            }

            if (input.Navigation.Home && HasRange())
            {
                SetValue(part, _min);
            }

            if (input.Navigation.End && HasRange())
            {
                SetValue(part, _max);
            }
        }

        HideInputFields();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRect minRect = GetMinRect();
        UiRect maxRect = GetMaxRect();
        if (!(_inputMode && _inputPart == RangePart.Min))
        {
            DrawPart(context, minRect, RangePart.Min, FormatValue(_valueMin));
        }

        if (!(_inputMode && _inputPart == RangePart.Max))
        {
            DrawPart(context, maxRect, RangePart.Max, FormatValue(_valueMax));
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

    private void EnterInputMode(UiUpdateContext context, RangePart part)
    {
        _dragging = false;
        _inputMode = true;
        _inputPart = part;
        _activePart = part;
        _inputSnapshotValue = part == RangePart.Max ? _valueMax : _valueMin;

        UiNumericField field = GetInputField(part);
        field.Visible = true;
        field.Bounds = part == RangePart.Min ? GetMinRect() : GetMaxRect();
        if (part == RangePart.Max)
        {
            ((UiInputInt)field).Value = _valueMax;
        }
        else
        {
            ((UiInputInt)field).Value = _valueMin;
        }

        field.SelectAllText();
        context.Focus.RequestFocus(field.TextField);
    }

    private void HandleInputSubmitted(RangePart part)
    {
        if (_inputPart != part)
        {
            return;
        }

        _inputMode = false;
        _inputPart = RangePart.None;
        GetInputField(part).Visible = false;
        _pendingFocusSelf = true;
    }

    private void HandleInputCancelled(RangePart part)
    {
        if (_inputPart != part)
        {
            return;
        }

        SetValue(part, _inputSnapshotValue);
        _inputMode = false;
        _inputPart = RangePart.None;
        GetInputField(part).Visible = false;
        _pendingFocusSelf = true;
    }

    private UiNumericField GetInputField(RangePart part)
    {
        return part == RangePart.Max ? _maxInputField : _minInputField;
    }

    private void SyncInputFieldOptions()
    {
        ConfigureInputField(_minInputField);
        ConfigureInputField(_maxInputField);
    }

    private void ConfigureInputField(UiInputInt field)
    {
        field.Min = _min;
        field.Max = _max;
        field.Clamp = Flags.HasFlag(UiDragFlags.AlwaysClamp) && !Flags.HasFlag(UiDragFlags.WrapAround);
        field.Step = Step;
        field.StepFast = Step > 0 ? Math.Max(1, Step * 10) : 0;
        field.ValueFormat = ValueFormat;
        field.TextScale = TextScale;
        field.Padding = Padding;
    }

    private void HideInputFields()
    {
        _minInputField.Visible = false;
        _maxInputField.Visible = false;
    }

    private void DrawPart(UiRenderContext context, UiRect rect, RangePart part, string text)
    {
        UiColor fill = Background;
        if (_dragging && _activePart == part)
        {
            fill = ActiveBackground;
        }
        else if (_hoveredPart == part)
        {
            fill = HoverBackground;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, rect, CornerRadius, fill);
        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, rect, CornerRadius, Border, 1);
        }

        int textWidth = context.Renderer.MeasureTextWidth(text, TextScale);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textX = rect.X + (rect.Width - textWidth) / 2;
        int textY = rect.Y + (rect.Height - textHeight) / 2;
        context.Renderer.DrawText(text, new UiPoint(textX, textY), TextColor, TextScale);
    }

    private UiRect GetMinRect()
    {
        int width = Math.Max(0, Bounds.Width - Padding * 2 - Gap);
        int boxWidth = width / 2;
        int height = Math.Max(0, Bounds.Height - Padding * 2);
        int x = Bounds.X + Padding;
        int y = Bounds.Y + Padding;
        return new UiRect(x, y, boxWidth, height);
    }

    private UiRect GetMaxRect()
    {
        int width = Math.Max(0, Bounds.Width - Padding * 2 - Gap);
        int boxWidth = width / 2;
        int rightWidth = Math.Max(0, width - boxWidth);
        int height = Math.Max(0, Bounds.Height - Padding * 2);
        int x = Bounds.X + Padding + boxWidth + Gap;
        int y = Bounds.Y + Padding;
        return new UiRect(x, y, rightWidth, height);
    }

    private bool HasRange()
    {
        return _max > _min;
    }

    private bool CanUseLogScale()
    {
        return Flags.HasFlag(UiDragFlags.Logarithmic) && HasRange() && _min > 0 && _max > 0;
    }

    private int ApplyDrag(int startValue, float delta, float speed)
    {
        if (!CanUseLogScale())
        {
            return (int)MathF.Round(startValue + delta * speed);
        }

        float min = MathF.Max(_min, 1f);
        float max = MathF.Max(_max, min + 1f);
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
        float value = MathF.Exp(logMin + normalized * logRange);
        return (int)MathF.Round(value);
    }

    private float GetDragSpeed(UiInputState input)
    {
        float speed = Speed;
        if (speed <= 0f)
        {
            int range = Math.Abs(_max - _min);
            speed = range > 0 ? range / 200f : 1f;
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

    private int GetStepValue()
    {
        if (Step > 0)
        {
            return Step;
        }

        if (Speed > 0f)
        {
            return (int)MathF.Max(1f, MathF.Round(Speed));
        }

        int range = Math.Abs(_max - _min);
        return Math.Max(1, range / 200);
    }

    private void AdjustValue(RangePart part, int delta)
    {
        if (delta == 0)
        {
            return;
        }

        if (part == RangePart.Max)
        {
            SetMax(_valueMax + delta);
        }
        else
        {
            SetMin(_valueMin + delta);
        }
    }

    private void SetValue(RangePart part, int value)
    {
        if (part == RangePart.Max)
        {
            SetMax(value);
        }
        else
        {
            SetMin(value);
        }
    }

    private void SetMin(int value)
    {
        _hasValueMin = true;
        int next = ApplyConstraints(value);
        if (EnsureOrder && _hasValueMax && next > _valueMax)
        {
            next = _valueMax;
        }

        if (_valueMin == next)
        {
            return;
        }

        _valueMin = next;
        RangeChanged?.Invoke(_valueMin, _valueMax);
    }

    private void SetMax(int value)
    {
        _hasValueMax = true;
        int next = ApplyConstraints(value);
        if (EnsureOrder && _hasValueMin && next < _valueMin)
        {
            next = _valueMin;
        }

        if (_valueMax == next)
        {
            return;
        }

        _valueMax = next;
        RangeChanged?.Invoke(_valueMin, _valueMax);
    }

    private int ApplyConstraints(int value)
    {
        return UiNumericValueHelpers.ApplyIntConstraints(
            value,
            _min,
            _max,
            Step,
            clampByDefault: false,
            UiNumericValueHelpers.ToModifierFlags(Flags));
    }

    private string FormatValue(int value)
    {
        return value.ToString(ValueFormat, CultureInfo.InvariantCulture);
    }
}
