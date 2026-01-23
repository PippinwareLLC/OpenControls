namespace OpenControls.Controls;

public sealed class UiSliderVector : UiSliderVectorBase
{
    public UiSliderVector(int components)
        : base(components)
    {
    }

    public float[] Values
    {
        get
        {
            int count = Sliders.Count;
            float[] values = new float[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = Sliders[i].Value;
            }

            return values;
        }
        set
        {
            if (value == null)
            {
                return;
            }

            int count = Math.Min(value.Length, Sliders.Count);
            for (int i = 0; i < count; i++)
            {
                Sliders[i].Value = value[i];
            }
        }
    }

    public event Action<IReadOnlyList<float>>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(Values);
    }
}
