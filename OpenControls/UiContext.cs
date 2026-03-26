using OpenControls.Controls;

namespace OpenControls;

public sealed class UiContext
{
    private UiElement? _mouseCaptureTarget;
    private UiElement? _activeInputLayer;

    public UiContext(UiElement root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public UiElement Root { get; }
    public UiFocusManager Focus { get; } = new();
    public UiDragDropContext DragDrop { get; } = new();
    public UiElement? Hovered { get; private set; }
    public UiElement? PointerCaptureTarget { get; private set; }
    public UiMouseCursor RequestedMouseCursor { get; private set; } = UiMouseCursor.Arrow;
    public bool WantCaptureMouse { get; private set; }
    public bool WantCaptureKeyboard { get; private set; }
    public bool WantTextInput { get; private set; }
    public UiInputState? LastInput { get; private set; }
    public UiTextInputRequest? TextInputRequest { get; private set; }

    public void Update(UiInputState input, float deltaSeconds = 0f)
    {
        UiInputState effectiveInput = input;
        if (input.Navigation.Tab && !IsTabHandled())
        {
            MoveFocus(input.ShiftDown);
            effectiveInput = ConsumeTabInput(input);
        }

        if (Focus.Focused != null && (!Focus.Focused.Visible || !Focus.Focused.Enabled))
        {
            Focus.ClearFocus();
        }

        DragDrop.BeginFrame(effectiveInput);
        Root.Update(new UiUpdateContext(effectiveInput, Focus, DragDrop, deltaSeconds));
        DragDrop.EndFrame();
        RefreshOutputs(effectiveInput);
    }

    public bool IsShortcutPressed(
        UiKeyChord chord,
        UiShortcutScope scope = UiShortcutScope.Global,
        UiElement? owner = null,
        bool allowExtraModifiers = false,
        bool allowDuringTextInput = false)
    {
        if (LastInput == null || !LastInput.IsKeyChordPressed(chord, allowExtraModifiers))
        {
            return false;
        }

        return IsShortcutScopeActive(scope, owner, allowDuringTextInput);
    }

    public bool IsPrimaryShortcutPressed(
        UiKey key,
        UiShortcutScope scope = UiShortcutScope.Global,
        UiElement? owner = null,
        bool shift = false,
        bool alt = false,
        bool allowExtraModifiers = false,
        bool allowDuringTextInput = false)
    {
        if (LastInput == null || !LastInput.IsPrimaryShortcutPressed(key, shift, alt, allowExtraModifiers))
        {
            return false;
        }

        return IsShortcutScopeActive(scope, owner, allowDuringTextInput);
    }

    private bool IsTabHandled()
    {
        if (Focus.Focused is not { Visible: true, Enabled: true } focused)
        {
            return false;
        }

        return focused.HandlesTabInput;
    }

    public void Render(IUiRenderer renderer)
    {
        UiRenderContext context = new(renderer);
        Root.Render(context);
        Root.RenderOverlay(context);
    }

    private void MoveFocus(bool reverse)
    {
        List<UiElement> focusables = new();
        CollectFocusable(Root, focusables);

        if (focusables.Count == 0)
        {
            Focus.ClearFocus();
            return;
        }

        int currentIndex = Focus.Focused == null ? -1 : focusables.IndexOf(Focus.Focused);
        if (currentIndex == -1)
        {
            Focus.RequestFocus(focusables[reverse ? focusables.Count - 1 : 0]);
            return;
        }

        int nextIndex = reverse ? currentIndex - 1 : currentIndex + 1;
        if (nextIndex < 0)
        {
            nextIndex = focusables.Count - 1;
        }
        else if (nextIndex >= focusables.Count)
        {
            nextIndex = 0;
        }

        Focus.RequestFocus(focusables[nextIndex]);
    }

    private static void CollectFocusable(UiElement element, List<UiElement> focusables)
    {
        if (!element.Visible || !element.Enabled)
        {
            return;
        }

        if (element is UiTabItem tabItem && !tabItem.IsActive)
        {
            return;
        }

        if (element.IsFocusable)
        {
            focusables.Add(element);
        }

        if (!ShouldTraverseChildren(element))
        {
            return;
        }

        if (element is UiModalHost modalHost && modalHost.BlockInputWhenModalOpen)
        {
            UiModal? activeModal = FindActiveModal(modalHost);
            if (activeModal != null)
            {
                CollectFocusable(activeModal, focusables);
                return;
            }
        }

        foreach (UiElement child in element.Children)
        {
            CollectFocusable(child, focusables);
        }
    }

    private static bool ShouldTraverseChildren(UiElement element)
    {
        if (element is UiPopup popup)
        {
            return popup.IsOpen;
        }

        if (element is UiTreeNode tree)
        {
            return tree.IsOpen;
        }

        if (element is UiCollapsingHeader header)
        {
            return header.IsOpen;
        }

        return true;
    }

    private static UiModal? FindActiveModal(UiModalHost host)
    {
        for (int i = host.Children.Count - 1; i >= 0; i--)
        {
            if (host.Children[i] is UiModal modal && modal.IsOpen)
            {
                return modal;
            }
        }

        return null;
    }

    private static UiInputState ConsumeTabInput(UiInputState input)
    {
        UiNavigationInput navigation = new UiNavigationInput
        {
            MoveLeft = input.Navigation.MoveLeft,
            MoveRight = input.Navigation.MoveRight,
            MoveUp = input.Navigation.MoveUp,
            MoveDown = input.Navigation.MoveDown,
            Home = input.Navigation.Home,
            End = input.Navigation.End,
            Backspace = input.Navigation.Backspace,
            Delete = input.Navigation.Delete,
            Tab = false,
            Enter = input.Navigation.Enter,
            KeypadEnter = input.Navigation.KeypadEnter,
            Space = input.Navigation.Space,
            Escape = input.Navigation.Escape
        };

        IReadOnlyList<char> textInput = input.TextInput;
        if (textInput.Count > 0)
        {
            List<char>? filtered = null;
            for (int i = 0; i < textInput.Count; i++)
            {
                char character = textInput[i];
                if (character == '\t')
                {
                    if (filtered == null)
                    {
                        filtered = new List<char>(textInput.Count);
                        for (int j = 0; j < i; j++)
                        {
                            filtered.Add(textInput[j]);
                        }
                    }
                    continue;
                }

                filtered?.Add(character);
            }

            if (filtered != null)
            {
                textInput = filtered;
            }
        }

        return new UiInputState
        {
            MousePosition = input.MousePosition,
            ScreenMousePosition = input.ScreenMousePosition,
            LeftDown = input.LeftDown,
            LeftClicked = input.LeftClicked,
            LeftDoubleClicked = input.LeftDoubleClicked,
            LeftReleased = input.LeftReleased,
            RightDown = input.RightDown,
            RightClicked = input.RightClicked,
            RightDoubleClicked = input.RightDoubleClicked,
            RightReleased = input.RightReleased,
            MiddleDown = input.MiddleDown,
            MiddleClicked = input.MiddleClicked,
            MiddleDoubleClicked = input.MiddleDoubleClicked,
            MiddleReleased = input.MiddleReleased,
            LeftDragOrigin = input.LeftDragOrigin,
            RightDragOrigin = input.RightDragOrigin,
            MiddleDragOrigin = input.MiddleDragOrigin,
            DragThreshold = input.DragThreshold,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            AltDown = input.AltDown,
            SuperDown = input.SuperDown,
            ScrollDeltaX = input.ScrollDeltaX,
            ScrollDelta = input.ScrollDelta,
            TextInput = textInput,
            KeysDown = input.KeysDown,
            KeysPressed = FilterKey(input.KeysPressed, UiKey.Tab),
            KeysReleased = input.KeysReleased,
            Navigation = navigation
        };
    }

    private void RefreshOutputs(UiInputState input)
    {
        LastInput = input;
        Hovered = Root.HitTest(input.MousePosition);
        if (input.LeftClicked && ResolveFocusTarget(Hovered) == null)
        {
            Focus.ClearFocus();
        }

        UiElement? hoveredCaptureTarget = ResolvePointerCaptureTarget(Hovered);

        if ((input.LeftClicked || input.RightClicked || input.MiddleClicked) && hoveredCaptureTarget != null)
        {
            _mouseCaptureTarget = hoveredCaptureTarget;
        }

        if (!input.AnyMouseDown)
        {
            _mouseCaptureTarget = null;
        }

        PointerCaptureTarget = _mouseCaptureTarget ?? hoveredCaptureTarget;
        _activeInputLayer = FindActiveInputLayer(Root);

        bool blockingOverlayOpen = _activeInputLayer != null;
        WantTextInput = Focus.Focused?.WantsTextInput == true;
        WantCaptureKeyboard = WantTextInput || Focus.Focused != null || blockingOverlayOpen;
        WantCaptureMouse = PointerCaptureTarget != null || blockingOverlayOpen;
        RequestedMouseCursor = ResolveMouseCursor(input);
        TextInputRequest = ResolveTextInputRequest();
    }

    private UiMouseCursor ResolveMouseCursor(UiInputState input)
    {
        UiElement? current = PointerCaptureTarget ?? Hovered;
        while (current != null)
        {
            if (current.TryGetMouseCursor(input, current == Focus.Focused, out UiMouseCursor cursor))
            {
                return cursor;
            }

            current = current.Parent;
        }

        return UiMouseCursor.Arrow;
    }

    private static UiElement? ResolvePointerCaptureTarget(UiElement? element)
    {
        UiElement? current = element;
        while (current != null)
        {
            if (current.CapturesPointerInput)
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private static UiElement? ResolveFocusTarget(UiElement? element)
    {
        UiElement? current = element;
        while (current != null)
        {
            if (current.IsFocusable)
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    private UiTextInputRequest? ResolveTextInputRequest()
    {
        if (!WantTextInput || Focus.Focused == null)
        {
            return null;
        }

        if (Focus.Focused.TryGetTextInputRequest(out UiTextInputRequest request))
        {
            return request;
        }

        return null;
    }

    private bool IsShortcutScopeActive(UiShortcutScope scope, UiElement? owner, bool allowDuringTextInput)
    {
        if (owner != null && !IsShortcutOwnerActive(owner))
        {
            return false;
        }

        return scope switch
        {
            UiShortcutScope.Focused => owner != null && Focus.Focused != null && IsElementOrAncestor(owner, Focus.Focused),
            UiShortcutScope.Hovered => owner != null && Hovered != null && IsElementOrAncestor(owner, Hovered),
            UiShortcutScope.Global => IsGlobalShortcutActive(owner, allowDuringTextInput),
            _ => false
        };
    }

    private bool IsGlobalShortcutActive(UiElement? owner, bool allowDuringTextInput)
    {
        if (!allowDuringTextInput && WantTextInput)
        {
            return false;
        }

        if (owner == null)
        {
            return _activeInputLayer == null;
        }

        return IsShortcutOwnerActive(owner);
    }

    private bool IsShortcutOwnerActive(UiElement owner)
    {
        return _activeInputLayer == null || IsElementOrAncestor(_activeInputLayer, owner);
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

    private static UiElement? FindActiveInputLayer(UiElement element)
    {
        if (!element.Visible || !element.Enabled)
        {
            return null;
        }

        if (element is UiModalHost modalHost && modalHost.BlockInputWhenModalOpen)
        {
            UiModal? activeModal = FindActiveModal(modalHost);
            if (activeModal != null)
            {
                return FindActiveInputLayer(activeModal) ?? activeModal;
            }
        }

        for (int i = element.Children.Count - 1; i >= 0; i--)
        {
            UiElement? activeChild = FindActiveInputLayer(element.Children[i]);
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

    private static IReadOnlyList<UiKey> FilterKey(IReadOnlyList<UiKey> keys, UiKey excludedKey)
    {
        if (keys.Count == 0)
        {
            return keys;
        }

        List<UiKey>? filtered = null;
        for (int i = 0; i < keys.Count; i++)
        {
            UiKey key = keys[i];
            if (key == excludedKey)
            {
                if (filtered == null)
                {
                    filtered = new List<UiKey>(keys.Count);
                    for (int j = 0; j < i; j++)
                    {
                        filtered.Add(keys[j]);
                    }
                }

                continue;
            }

            filtered?.Add(key);
        }

        return filtered ?? keys;
    }
}
