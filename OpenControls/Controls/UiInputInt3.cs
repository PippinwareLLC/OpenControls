namespace OpenControls.Controls;

public sealed class UiInputInt3 : UiNumericVectorBase
{
    public UiInputInt3()
        : base(3, new[] { "X", "Y", "Z" })
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

    public event Action<int, int, int>? ValueChanged;

    protected override void OnValueChanged()
    {
        ValueChanged?.Invoke(ValueX, ValueY, ValueZ);
    }
}
