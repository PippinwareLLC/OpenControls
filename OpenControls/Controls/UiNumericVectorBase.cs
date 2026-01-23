namespace OpenControls.Controls;

public abstract class UiNumericVectorBase : UiElement
{
    private readonly UiNumericField[] _fields;

    protected UiNumericVectorBase(int components, string[] defaultLabels)
    {
        if (components <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(components));
        }

        _fields = new UiNumericField[components];
        ComponentLabels = defaultLabels ?? Array.Empty<string>();

        for (int i = 0; i < components; i++)
        {
            UiNumericField field = new UiNumericField();
            field.ValueChanged += _ => HandleFieldChanged();
            AddChild(field);
            _fields[i] = field;
        }
    }

    public IReadOnlyList<UiNumericField> Fields => _fields;

    public int Padding { get; set; } = 4;
    public int Gap { get; set; } = 6;
    public int FieldPadding { get; set; } = 4;
    public int FieldTextScale { get; set; } = 1;
    public string[] ComponentLabels { get; set; }

    public double Min { get; set; } = double.MinValue;
    public double Max { get; set; } = double.MaxValue;
    public bool Clamp { get; set; }
    public bool WholeNumbers { get; set; }
    public double Step { get; set; }
    public double StepFast { get; set; }
    public string ValueFormat { get; set; } = "0.##";

    protected double GetValue(int index)
    {
        return _fields[index].Value;
    }

    protected void SetValue(int index, double value)
    {
        _fields[index].Value = value;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        LayoutFields();
        ApplyFieldOptions();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        LayoutFields();
        base.Render(context);
    }

    protected virtual void OnValueChanged()
    {
    }

    private void HandleFieldChanged()
    {
        OnValueChanged();
    }

    private void ApplyFieldOptions()
    {
        for (int i = 0; i < _fields.Length; i++)
        {
            UiNumericField field = _fields[i];
            field.Min = Min;
            field.Max = Max;
            field.Clamp = Clamp;
            field.WholeNumbers = WholeNumbers;
            field.Step = Step;
            field.StepFast = StepFast;
            field.ValueFormat = ValueFormat;
            field.TextScale = FieldTextScale;
            field.Padding = FieldPadding;

            if (ComponentLabels != null && i < ComponentLabels.Length)
            {
                field.Placeholder = ComponentLabels[i] ?? string.Empty;
            }
        }
    }

    private void LayoutFields()
    {
        int count = _fields.Length;
        if (count == 0)
        {
            return;
        }

        int padding = Math.Max(0, Padding);
        int gap = Math.Max(0, Gap);
        int height = Math.Max(0, Bounds.Height - padding * 2);
        int width = Math.Max(0, Bounds.Width - padding * 2 - gap * (count - 1));
        int partWidth = width / count;
        int remainder = width - partWidth * count;

        int x = Bounds.X + padding;
        int y = Bounds.Y + padding;

        for (int i = 0; i < count; i++)
        {
            int actualWidth = partWidth + (i == count - 1 ? remainder : 0);
            UiNumericField field = _fields[i];
            field.Bounds = new UiRect(x, y, Math.Max(0, actualWidth), height);
            x += actualWidth + gap;
        }
    }
}
