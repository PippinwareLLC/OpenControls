namespace OpenControls.State;

public interface IUiStatefulElement
{
    void CaptureState(UiElementState state);
    void ApplyState(UiElementState state);
}
