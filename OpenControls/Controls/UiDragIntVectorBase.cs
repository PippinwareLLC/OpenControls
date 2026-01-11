using System.Globalization;

namespace OpenControls.Controls;

public abstract class UiDragIntVectorBase : UiElement
{
    private readonly int[] _values;
    private bool _dragging;
    private bool _focused;
    private int _dragStartX;
    private int _dragStartValue;
    private int _activeIndex = -1;
    private int _hoverIndex = -1;
    private int _min;
    private int _max = 100;

    protected UiDragIntVectorBase(int components, string[] defaultLabels)
    {
        if (components <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(components));
        }

        _values = new int[components];
        ComponentLabels = defaultLabels ?? Array.Empty<string>();
    }

    public int Min
    {
        get => _min;
        set
        {
            _min = value;
            ClampAll();
        }
    }

    public int Max
    {
        get => _max;
        set
        {
            _max = value;
            ClampAll();
        }
    }

    public float Speed { get; set; } = 1f;
    public int Step { get; set; } = 1;
    public UiDragFlags Flags { get; set; } = UiDragFlags.Clamp;
    public float SlowSpeedMultiplier { get; set; } = 0.1f;
    public float FastSpeedMultiplier { get; set; } = 10f;
    public string ValueFormat { get; set; } = "0";
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 4;
    public int Gap { get; set; } = 6;
    public string[] ComponentLabels { get; set; }

    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor ActiveBackground { get; set; } = new UiColor(50, 58, 76);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor TextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public IReadOnlyList<int> Values => _values;

    public override bool IsFocusable => true;

    protected int GetValue(int index)
    {
        return _values[index];
    }

    protected void SetValue(int index, int value)
    {
        SetValueInternal(index, value);
    }

    protected virtual void OnValueChanged()
    {
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hoverIndex = GetIndexAt(input.MousePosition);

        if (input.LeftClicked && _hoverIndex >= 0)
        {
            _activeIndex = _hoverIndex;
            _dragging = true;
            _dragStartX = input.MousePosition.X;
            _dragStartValue = _values[_activeIndex];
            context.Focus.RequestFocus(this);
        }

        if (_dragging && input.LeftDown && _activeIndex >= 0)
        {
            float delta = input.MousePosition.X - _dragStartX;
            float speed = GetDragSpeed(input);
            int next = ApplyDrag(_dragStartValue, delta, speed);
            SetValueInternal(_activeIndex, next);
        }

        if (_dragging && input.LeftReleased)
        {
            _dragging = false;
        }

        if (_focused)
        {
            int index = _activeIndex >= 0 ? _activeIndex : 0;
            int step = GetStepValue();

            if (input.Navigation.MoveLeft)
            {
                AdjustValue(index, -step);
            }

            if (input.Navigation.MoveRight)
            {
                AdjustValue(index, step);
            }

            if (input.Navigation.Home && HasRange())
            {
                SetValueInternal(index, _min);
            }

            if (input.Navigation.End && HasRange())
            {
                SetValueInternal(index, _max);
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

        for (int i = 0; i < _values.Length; i++)
        {
            UiRect rect = GetPartRect(i);
            UiColor fill = Background;
            if (_dragging && _activeIndex == i)
            {
                fill = ActiveBackground;
            }
            else if (_hoverIndex == i)
            {
                fill = HoverBackground;
            }

            UiRenderHelpers.FillRectRounded(context.Renderer, rect, CornerRadius, fill);
            if (Border.A > 0)
            {
                UiRenderHelpers.DrawRectRounded(context.Renderer, rect, CornerRadius, Border, 1);
            }

            string text = FormatComponentText(i, _values[i]);
            int textWidth = context.Renderer.MeasureTextWidth(text, TextScale);
            int textHeight = context.Renderer.MeasureTextHeight(TextScale);
            int textX = rect.X + (rect.Width - textWidth) / 2;
            int textY = rect.Y + (rect.Height - textHeight) / 2;
            context.Renderer.DrawText(text, new UiPoint(textX, textY), TextColor, TextScale);
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

    private void ClampAll()
    {
        for (int i = 0; i < _values.Length; i++)
        {
            SetValueInternal(i, _values[i]);
        }
    }

    private void AdjustValue(int index, int delta)
    {
        if (delta == 0)
        {
            return;
        }

        SetValueInternal(index, _values[index] + delta);
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

    private UiRect GetPartRect(int index)
    {
        int count = _values.Length;
        int padding = Math.Max(0, Padding);
        int gap = Math.Max(0, Gap);
        int height = Math.Max(0, Bounds.Height - padding * 2);
        int width = Math.Max(0, Bounds.Width - padding * 2 - gap * (count - 1));
        int partWidth = count > 0 ? width / count : 0;
        int remainder = width - partWidth * count;

        int x = Bounds.X + padding + index * (partWidth + gap);
        int y = Bounds.Y + padding;
        int actualWidth = partWidth + (index == count - 1 ? remainder : 0);
        return new UiRect(x, y, Math.Max(0, actualWidth), height);
    }

    private int GetIndexAt(UiPoint point)
    {
        for (int i = 0; i < _values.Length; i++)
        {
            if (GetPartRect(i).Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    private void SetValueInternal(int index, int value)
    {
        int next = ApplyConstraints(value);
        if (_values[index] == next)
        {
            return;
        }

        _values[index] = next;
        OnValueChanged();
    }

    private int ApplyConstraints(int value)
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

        return next;
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

    private string FormatComponentText(int index, int value)
    {
        string label = string.Empty;
        if (ComponentLabels != null && index < ComponentLabels.Length)
        {
            label = ComponentLabels[index] ?? string.Empty;
        }

        string formatted = value.ToString(ValueFormat, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(label))
        {
            return formatted;
        }

        return $"{label}: {formatted}";
    }
}
