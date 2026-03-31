namespace OpenControls;

public readonly struct UiUpdateContext
{
    public UiUpdateContext(
        UiInputState input,
        UiFocusManager focus,
        UiDragDropContext dragDrop,
        float deltaSeconds,
        UiFont defaultFont,
        IUiClipboard clipboard,
        UiElement? activeInputLayer = null)
    {
        Input = input;
        Focus = focus;
        DragDrop = dragDrop;
        DeltaSeconds = deltaSeconds;
        DefaultFont = defaultFont;
        Clipboard = clipboard;
        ActiveInputLayer = activeInputLayer;
    }

    public UiInputState Input { get; }
    public UiFocusManager Focus { get; }
    public UiDragDropContext DragDrop { get; }
    public float DeltaSeconds { get; }
    public UiFont DefaultFont { get; }
    public IUiClipboard Clipboard { get; }
    public UiElement? ActiveInputLayer { get; }

    public bool IsInputBlockedFor(UiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (ActiveInputLayer == null)
        {
            return false;
        }

        UiElement? current = element;
        while (current != null)
        {
            if (current == ActiveInputLayer)
            {
                return false;
            }

            current = current.Parent;
        }

        return true;
    }

    public UiInputState GetInputFor(UiElement element)
    {
        if (!IsInputBlockedFor(element))
        {
            return Input;
        }

        UiPoint blockedPoint = new(-1_000_000, -1_000_000);
        return new UiInputState
        {
            MousePosition = blockedPoint,
            ScreenMousePosition = blockedPoint,
            DragThreshold = Input.DragThreshold,
            Composition = UiTextCompositionState.Empty
        };
    }
}
