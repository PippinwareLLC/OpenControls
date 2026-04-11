using OpenControls.Controls;

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

    public UiUpdateContext CreateChildContext(UiElement parent, UiElement child)
    {
        return CreateChildContext(parent, child, Input);
    }

    public UiUpdateContext CreateChildContext(UiElement parent, UiElement child, UiInputState input)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);

        UiElement? activeInputLayer = ResolveActiveInputLayer(parent) ?? ActiveInputLayer;
        UiInputState childInput = BlockInputFor(child, input, activeInputLayer);
        return new UiUpdateContext(childInput, Focus, DragDrop, DeltaSeconds, DefaultFont, Clipboard, activeInputLayer);
    }

    public bool IsInputBlockedFor(UiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return IsInputBlockedFor(element, ActiveInputLayer);
    }

    public UiInputState GetInputFor(UiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return BlockInputFor(element, Input, ActiveInputLayer);
    }

    public UiInputState GetSelfInput(UiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return BlockSelfInputFor(element, Input, ActiveInputLayer);
    }

    internal static UiElement? ResolveActiveInputLayer(UiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!element.Visible || !element.Enabled)
        {
            return null;
        }

        if (element is UiModalHost { BlockInputWhenModalOpen: true } modalHost)
        {
            UiModal? activeModal = modalHost.ActiveModal;
            if (activeModal != null)
            {
                return ResolveActiveInputLayer(activeModal) ?? activeModal;
            }
        }

        for (int i = element.Children.Count - 1; i >= 0; i--)
        {
            UiElement? activeChild = ResolveActiveInputLayer(element.Children[i]);
            if (activeChild != null)
            {
                return activeChild;
            }
        }

        if (element is UiModal modal && modal.IsOpen)
        {
            return modal;
        }

        if (element is UiPopup popup && popup.IsOpen)
        {
            return popup;
        }

        if (element is UiMenuBar menuBar && menuBar.HasOpenMenu)
        {
            return menuBar;
        }

        return null;
    }

    private static bool IsInputBlockedFor(UiElement element, UiElement? activeInputLayer)
    {
        if (activeInputLayer == null)
        {
            return false;
        }

        return !IsElementOrAncestor(element, activeInputLayer)
            && !IsElementOrAncestor(activeInputLayer, element);
    }

    private static UiInputState BlockInputFor(UiElement element, UiInputState input, UiElement? activeInputLayer)
    {
        return IsInputBlockedFor(element, activeInputLayer)
            ? BuildBlockedInput(input)
            : input;
    }

    private static UiInputState BlockSelfInputFor(UiElement element, UiInputState input, UiElement? activeInputLayer)
    {
        if (activeInputLayer == null)
        {
            return input;
        }

        return IsElementOrAncestor(activeInputLayer, element)
            ? input
            : BuildBlockedInput(input);
    }

    private static UiInputState BuildBlockedInput(UiInputState input)
    {
        UiPoint blockedPoint = new(-1_000_000, -1_000_000);
        return new UiInputState
        {
            MousePosition = blockedPoint,
            ScreenMousePosition = blockedPoint,
            DragThreshold = input.DragThreshold,
            Composition = UiTextCompositionState.Empty
        };
    }

    private static bool IsElementOrAncestor(UiElement ancestor, UiElement element)
    {
        UiElement? current = element;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}
