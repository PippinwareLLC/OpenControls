namespace OpenControls.Controls;

public sealed class UiNumericVector : UiNumericVectorBase
{
    public UiNumericVector(int components, string[]? labels = null)
        : base(components, labels ?? Array.Empty<string>())
    {
    }

    public double[] Values
    {
        get
        {
            int count = Fields.Count;
            double[] values = new double[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = Fields[i].Value;
            }

            return values;
        }
        set
        {
            if (value == null)
            {
                return;
            }

            int count = Math.Min(value.Length, Fields.Count);
            for (int i = 0; i < count; i++)
            {
                Fields[i].Value = value[i];
            }
        }
    }

    public event Action<IReadOnlyList<double>>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(Values);
    }
}
