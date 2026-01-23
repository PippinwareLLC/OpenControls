namespace OpenControls.Controls;

public sealed class UiInputInt4 : UiNumericVectorBase
{
    public UiInputInt4()
        : base(4, new[] { "X", "Y", "Z", "W" })
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

    public int ValueZ
    {
        get => (int)Math.Round(GetValue(2));
        set => SetValue(2, value);
    }

    public int ValueW
    {
        get => (int)Math.Round(GetValue(3));
        set => SetValue(3, value);
    }

    public event Action<int, int, int, int>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY, ValueZ, ValueW);
    }
}
