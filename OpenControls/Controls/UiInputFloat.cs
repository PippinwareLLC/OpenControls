namespace OpenControls.Controls;

public sealed class UiInputFloat : UiNumericField
{
    public UiInputFloat()
    {
        ValueFormat = "0.###";
    }

    public new float Value
    {
        get => (float)base.Value;
        set => base.Value = value;
    }

    public new event Action<float>? ValueChanged;

    protected override void OnValueChanged(double value)
    {
        ValueChanged?.Invoke((float)value);
    }
}
