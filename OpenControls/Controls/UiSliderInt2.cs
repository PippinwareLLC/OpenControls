namespace OpenControls.Controls;

public sealed class UiSliderInt2 : UiSliderVectorBase
{
    public UiSliderInt2()
        : base(2)
    {
        WholeNumbers = true;
        ValueFormat = "0";
    }

    public int ValueX
    {
        get => (int)Math.Round(GetValue(0));
        set => SetValue(0, value);
    }

    public int ValueY
    {
        get => (int)Math.Round(GetValue(1));
        set => SetValue(1, value);
    }

    public event Action<int, int>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY);
    }
}
