namespace OpenControls.Controls;

public sealed class UiInputInt : UiNumericField
{
    public UiInputInt()
    {
        WholeNumbers = true;
        ValueFormat = "0";
    }

    public new int Value
    {
        get => (int)Math.Round(base.Value);
        set => base.Value = value;
    }

    public new event Action<int>? ValueChanged;

    protected override void OnValueChanged(double value)
    {
        ValueChanged?.Invoke((int)Math.Round(value));
    }
}
