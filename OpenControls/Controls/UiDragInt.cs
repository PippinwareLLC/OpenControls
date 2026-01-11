using System.Globalization;

namespace OpenControls.Controls;

public sealed class UiDragInt : UiElement
{
    private bool _dragging;
    private bool _hovered;
    private bool _focused;
    private int _dragStartX;
    private int _dragStartValue;
    private int _value;
    private int _min;
    private int _max = 100;

    public int Min
    {
        get => _min;
        set
        {
            _min = value;
            SetValue(_value);
        }
    }

    public int Max
    {
        get => _max;
        set
        {
            _max = value;
            SetValue(_value);
        }
    }

    public int Value
    {
        get => _value;
        set => SetValue(value);
    }

    public float Speed { get; set; } = 1f;
    public int Step { get; set; } = 1;
    public UiDragFlags Flags { get; set; } = UiDragFlags.Clamp;
    public float SlowSpeedMultiplier { get; set; } = 0.1f;
    public float FastSpeedMultiplier { get; set; } = 10f;
    public string ValueFormat { get; set; } = "0";
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 4;

    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor ActiveBackground { get; set; } = new UiColor(50, 58, 76);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public event Action<int>? ValueChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

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
            int step = GetStepValue();
            if (input.Navigation.MoveLeft)
            {
                AdjustValue(-step);
            }

            if (input.Navigation.MoveRight)
            {
                AdjustValue(step);
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

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
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

    private void AdjustValue(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        SetValue(_value + delta);
    }

    private void SetValue(int value)
    {
        int next = value;
        if (Flags.HasFlag(UiDragFlags.Clamp) && HasRange())
        {
            next = Math.Clamp(next, _min, _max);
        }

        if (Step > 1)
        {
            next = SnapToStep(next);
        }

        if (_value == next)
        {
            return;
        }

        _value = next;
        ValueChanged?.Invoke(_value);
    }

    private int SnapToStep(int value)
    {
        if (Step <= 1)
        {
            return value;
        }

        int steps = (int)MathF.Round((value - _min) / (float)Step);
        return _min + steps * Step;
    }

    private string FormatValue(int value)
    {
        return value.ToString(ValueFormat, CultureInfo.InvariantCulture);
    }
}
