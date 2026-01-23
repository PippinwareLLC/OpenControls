using System.Globalization;

namespace OpenControls.Controls;

public class UiNumericField : UiElement
{
    private readonly UiTextField _field;
    private string _lastText = string.Empty;
    private bool _wasFocused;
    private bool _suppressParse;
    private double _value;
    private double _min = double.MinValue;
    private double _max = double.MaxValue;
    private bool _wholeNumbers;

    public UiNumericField()
    {
        _field = new UiTextField();
        _field.CharacterFilter = IsCharacterAllowed;
        AddChild(_field);
        SyncText();
    }

    public UiTextField TextField => _field;

    public double Value
    {
        get => _value;
        set => SetValueInternal(value, updateText: !_wasFocused);
    }

    public double Min
    {
        get => _min;
        set
        {
            _min = value;
            if (Clamp)
            {
                SetValueInternal(_value, updateText: !_wasFocused, raiseEvents: false);
            }
        }
    }

    public double Max
    {
        get => _max;
        set
        {
            _max = value;
            if (Clamp)
            {
                SetValueInternal(_value, updateText: !_wasFocused, raiseEvents: false);
            }
        }
    }

    public bool Clamp { get; set; }

    public bool WholeNumbers
    {
        get => _wholeNumbers;
        set
        {
            _wholeNumbers = value;
            if (_wholeNumbers)
            {
                AllowDecimal = false;
                AllowExponent = false;
            }

            SetValueInternal(_value, updateText: !_wasFocused, raiseEvents: false);
        }
    }

    public double Step { get; set; }
    public double StepFast { get; set; }
    public string ValueFormat { get; set; } = "0.##";
    public bool AllowDecimal { get; set; } = true;
    public bool AllowExponent { get; set; } = true;
    public bool AllowSign { get; set; } = true;

    public string Placeholder
    {
        get => _field.Placeholder;
        set => _field.Placeholder = value ?? string.Empty;
    }

    public int TextScale
    {
        get => _field.TextScale;
        set => _field.TextScale = value;
    }

    public int Padding
    {
        get => _field.Padding;
        set => _field.Padding = value;
    }

    public int MaxLength
    {
        get => _field.MaxLength;
        set => _field.MaxLength = value;
    }

    public UiColor Background
    {
        get => _field.Background;
        set => _field.Background = value;
    }

    public UiColor Border
    {
        get => _field.Border;
        set => _field.Border = value;
    }

    public UiColor FocusBorder
    {
        get => _field.FocusBorder;
        set => _field.FocusBorder = value;
    }

    public UiColor TextColor
    {
        get => _field.TextColor;
        set => _field.TextColor = value;
    }

    public UiColor PlaceholderColor
    {
        get => _field.PlaceholderColor;
        set => _field.PlaceholderColor = value;
    }

    public UiColor CaretColor
    {
        get => _field.CaretColor;
        set => _field.CaretColor = value;
    }

    public int CornerRadius
    {
        get => _field.CornerRadius;
        set => _field.CornerRadius = value;
    }

    public event Action<double>? ValueChanged;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _field.Bounds = Bounds;
        base.Update(context);

        bool focused = context.Focus.Focused == _field;

        if (_field.Text != _lastText)
        {
            _lastText = _field.Text;
            if (!_suppressParse && TryParse(_field.Text, out double parsed))
            {
                SetValueInternal(parsed, updateText: false);
            }
        }

        if (_wasFocused && !focused)
        {
            SyncText();
        }

        if (focused)
        {
            HandleStepInput(context.Input);
        }

        _wasFocused = focused;
    }

    protected virtual void OnValueChanged(double value)
    {
    }

    private void HandleStepInput(UiInputState input)
    {
        double step = GetStep(input);
        if (step == 0d)
        {
            return;
        }

        if (input.Navigation.MoveUp)
        {
            SetValueInternal(_value + step, updateText: true);
        }

        if (input.Navigation.MoveDown)
        {
            SetValueInternal(_value - step, updateText: true);
        }

        if (input.Navigation.Home && Clamp && _max > _min)
        {
            SetValueInternal(_min, updateText: true);
        }

        if (input.Navigation.End && Clamp && _max > _min)
        {
            SetValueInternal(_max, updateText: true);
        }
    }

    private double GetStep(UiInputState input)
    {
        if (input.ShiftDown && StepFast > 0)
        {
            return StepFast;
        }

        if (Step > 0)
        {
            return Step;
        }

        if (WholeNumbers)
        {
            return 1d;
        }

        return 0d;
    }

    private void SetValueInternal(double value, bool updateText, bool raiseEvents = true)
    {
        double next = ApplyConstraints(value);
        if (_value.Equals(next))
        {
            if (updateText)
            {
                SyncText();
            }
            return;
        }

        _value = next;
        if (raiseEvents)
        {
            ValueChanged?.Invoke(_value);
            OnValueChanged(_value);
        }

        if (updateText)
        {
            SyncText();
        }
    }

    private double ApplyConstraints(double value)
    {
        double next = value;
        if (Clamp && _max > _min)
        {
            next = Math.Clamp(next, _min, _max);
        }

        if (WholeNumbers)
        {
            next = Math.Round(next);
        }

        return next;
    }

    private void SyncText()
    {
        string formatted = FormatValue(_value);
        _suppressParse = true;
        _field.Text = formatted;
        _field.SetCaretIndex(_field.Text.Length);
        _lastText = _field.Text;
        _suppressParse = false;
    }

    private string FormatValue(double value)
    {
        string format = string.IsNullOrWhiteSpace(ValueFormat) ? "0.##" : ValueFormat;
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    private bool TryParse(string text, out double value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = _value;
            return false;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private bool IsCharacterAllowed(char character)
    {
        if (char.IsDigit(character))
        {
            return true;
        }

        if (AllowSign && (character == '-' || character == '+'))
        {
            return true;
        }

        if (AllowDecimal && character == '.')
        {
            return true;
        }

        if (AllowExponent && (character == 'e' || character == 'E'))
        {
            return true;
        }

        return false;
    }
}
