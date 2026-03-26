namespace OpenControls;

public readonly struct UiUpdateContext
{
    public UiUpdateContext(UiInputState input, UiFocusManager focus, UiDragDropContext dragDrop, float deltaSeconds, UiFont defaultFont, IUiClipboard clipboard)
    {
        Input = input;
        Focus = focus;
        DragDrop = dragDrop;
        DeltaSeconds = deltaSeconds;
        DefaultFont = defaultFont;
        Clipboard = clipboard;
    }

    public UiInputState Input { get; }
    public UiFocusManager Focus { get; }
    public UiDragDropContext DragDrop { get; }
    public float DeltaSeconds { get; }
    public UiFont DefaultFont { get; }
    public IUiClipboard Clipboard { get; }
}
