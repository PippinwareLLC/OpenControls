namespace OpenControls;

public readonly struct UiUpdateContext
{
    public UiUpdateContext(UiInputState input, UiFocusManager focus, UiDragDropContext dragDrop, float deltaSeconds)
    {
        Input = input;
        Focus = focus;
        DragDrop = dragDrop;
        DeltaSeconds = deltaSeconds;
    }

    public UiInputState Input { get; }
    public UiFocusManager Focus { get; }
    public UiDragDropContext DragDrop { get; }
    public float DeltaSeconds { get; }
}
