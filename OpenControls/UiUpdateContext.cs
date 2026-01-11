namespace OpenControls;

public readonly struct UiUpdateContext
{
    public UiUpdateContext(UiInputState input, UiFocusManager focus, float deltaSeconds)
    {
        Input = input;
        Focus = focus;
        DeltaSeconds = deltaSeconds;
    }

    public UiInputState Input { get; }
    public UiFocusManager Focus { get; }
    public float DeltaSeconds { get; }
}
