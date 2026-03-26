namespace OpenControls.SilkNet;

public sealed class SilkTextInputBridge
{
    public Func<UiTextCompositionState>? GetComposition { get; set; }
    public Action<bool>? SetActive { get; set; }
    public Action<UiTextInputRequest?>? ApplyRequest { get; set; }

    public UiTextCompositionState ResolveComposition()
    {
        return GetComposition?.Invoke() ?? UiTextCompositionState.Empty;
    }

    public void ApplyActive(bool active)
    {
        SetActive?.Invoke(active);
    }

    public void Apply(UiTextInputRequest? request)
    {
        ApplyRequest?.Invoke(request);
    }
}
