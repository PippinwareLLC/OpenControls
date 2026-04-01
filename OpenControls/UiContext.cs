using OpenControls.Controls;

namespace OpenControls;

public sealed class UiContext
{
    private enum FocusRequestKind
    {
        None,
        Element,
        Id,
        FirstFocusableChild,
        FocusableChild
    }

    private readonly struct FocusRequest
    {
        public FocusRequest(FocusRequestKind kind, UiElement? element = null, UiElement? scope = null, string? id = null, int focusableIndex = 0)
        {
            Kind = kind;
            Element = element;
            Scope = scope;
            Id = id;
            FocusableIndex = focusableIndex;
        }

        public FocusRequestKind Kind { get; }
        public UiElement? Element { get; }
        public UiElement? Scope { get; }
        public string? Id { get; }
        public int FocusableIndex { get; }
        public bool IsPending => Kind != FocusRequestKind.None;
    }

    private UiElement? _mouseCaptureTarget;
    private UiElement? _activeInputLayer;
    private FocusRequest _pendingFocusRequest;
    private Dictionary<UiElement, UiItemStateSnapshot> _itemStates = new();
    private Dictionary<UiElement, UiContainerStateSnapshot> _containerStates = new();
    private readonly UiMemoryClipboard _fallbackClipboard = new();

    public UiContext(UiElement root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Clipboard = _fallbackClipboard;
    }

    public UiElement Root { get; }
    public UiFocusManager Focus { get; } = new();
    public UiDragDropContext DragDrop { get; } = new();
    public UiFont DefaultFont { get; set; } = UiFont.Default;
    public IUiClipboard Clipboard { get; set; }
    public UiElement? Hovered { get; private set; }
    public UiElement? ActiveInputLayer => _activeInputLayer;
    public UiElement? PointerCaptureTarget { get; private set; }
    public UiMouseCursor RequestedMouseCursor { get; private set; } = UiMouseCursor.Arrow;
    public bool WantCaptureMouse { get; private set; }
    public bool WantCaptureKeyboard { get; private set; }
    public bool WantTextInput { get; private set; }
    public UiInputState? LastInput { get; private set; }
    public UiTextInputRequest? TextInputRequest { get; private set; }
    public UiItemStateSnapshot LastItemState { get; private set; }
    public UiItemStateSnapshot HoveredItemState => GetItemState(Hovered);
    public UiItemStateSnapshot FocusedItemState => GetItemState(Focus.Focused);
    public UiContainerStateSnapshot HoveredContainerState => GetContainingContainerState(Hovered);
    public UiContainerStateSnapshot FocusedContainerState => GetContainingContainerState(Focus.Focused);
    public UiContainerStateSnapshot ActiveInputLayerState => GetContainerState(_activeInputLayer);

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
        Root.Update(new UiUpdateContext(effectiveInput, Focus, DragDrop, deltaSeconds, DefaultFont, Clipboard, _activeInputLayer));
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

    public UiItemStateSnapshot GetItemState(UiElement? element)
    {
        if (element == null)
        {
            return UiItemStateSnapshot.Empty;
        }

        if (TryResolveDebugBounds(Root, element, out UiRect resolvedBounds, out UiRect resolvedClipBounds))
        {
            if (_itemStates.TryGetValue(element, out UiItemStateSnapshot resolvedSnapshot))
            {
                return new UiItemStateSnapshot(
                    element,
                    resolvedBounds,
                    resolvedClipBounds,
                    resolvedSnapshot.Status,
                    resolvedSnapshot.Visible,
                    resolvedSnapshot.Enabled);
            }

            return new UiItemStateSnapshot(
                element,
                resolvedBounds,
                resolvedClipBounds,
                UiItemStatusFlags.None,
                element.Visible,
                element.Enabled);
        }

        if (_itemStates.TryGetValue(element, out UiItemStateSnapshot snapshot))
        {
            return snapshot;
        }

        UiRect clipBounds = element.Bounds;
        if (clipBounds.Width < 0 || clipBounds.Height < 0)
        {
            clipBounds = default;
        }

        return new UiItemStateSnapshot(
            element,
            element.Bounds,
            clipBounds,
            UiItemStatusFlags.None,
            element.Visible,
            element.Enabled);
    }

    private static bool TryResolveDebugBounds(UiElement current, UiElement target, out UiRect bounds, out UiRect clipBounds)
    {
        if (current is IUiDebugBoundsResolver resolver && resolver.TryResolveDebugBounds(target, out bounds, out clipBounds))
        {
            return true;
        }

        foreach (UiElement child in current.Children)
        {
            if (TryResolveDebugBounds(child, target, out bounds, out clipBounds))
            {
                return true;
            }
        }

        bounds = default;
        clipBounds = default;
        return false;
    }

    public UiContainerStateSnapshot GetContainerState(UiElement? element)
    {
        if (element == null)
        {
            return UiContainerStateSnapshot.Empty;
        }

        if (_containerStates.TryGetValue(element, out UiContainerStateSnapshot snapshot))
        {
            return snapshot;
        }

        UiContainerKind kind = GetContainerKind(element);
        if (kind == UiContainerKind.None)
        {
            return UiContainerStateSnapshot.Empty;
        }

        UiItemStateSnapshot itemState = GetItemState(element);
        return new UiContainerStateSnapshot(
            element,
            kind,
            itemState.Bounds,
            itemState.ClipBounds,
            itemState.Visible,
            itemState.Enabled,
            false,
            false,
            IsContainerOpen(element),
            IsContainerActiveTab(element),
            false,
            false);
    }

    public UiContainerStateSnapshot GetContainingContainerState(UiElement? element)
    {
        UiElement? current = element;
        while (current != null)
        {
            UiContainerStateSnapshot state = GetContainerState(current);
            if (state.IsValid)
            {
                return state;
            }

            current = current.Parent;
        }

        return UiContainerStateSnapshot.Empty;
    }

    public UiElement? FindElementById(string id, UiElement? scope = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return FindElementById(scope ?? Root, id);
    }

    public IReadOnlyList<UiElement> GetFocusableElements(UiElement? scope = null)
    {
        List<UiElement> focusables = new();
        CollectFocusable(scope ?? Root, focusables);
        return focusables;
    }

    public bool IsFocused(UiElement element, bool includeDescendants = true)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (Focus.Focused == null)
        {
            return false;
        }

        return includeDescendants ? IsElementOrAncestor(element, Focus.Focused) : Focus.Focused == element;
    }

    public bool IsHovered(UiElement element, bool includeDescendants = true)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (Hovered == null)
        {
            return false;
        }

        return includeDescendants ? IsElementOrAncestor(element, Hovered) : Hovered == element;
    }

    public bool IsEffectivelyVisible(UiElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        UiItemStateSnapshot itemState = GetItemState(element);
        if (!itemState.Visible)
        {
            return false;
        }

        return itemState.ClipBounds.Width > 0 && itemState.ClipBounds.Height > 0;
    }

    public bool TryGetVisibleBounds(UiElement element, out UiRect visibleBounds)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        UiItemStateSnapshot itemState = GetItemState(element);
        visibleBounds = itemState.ClipBounds;
        return itemState.Visible && visibleBounds.Width > 0 && visibleBounds.Height > 0;
    }

    public IReadOnlyList<UiElement> GetDebugChildren(UiElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        List<UiElement> children = new(element.Children.Count + 4);
        for (int i = 0; i < element.Children.Count; i++)
        {
            children.Add(element.Children[i]);
        }

        if (element is Controls.UiTable table)
        {
            table.AppendDebugChildren(children);
        }

        return children;
    }

    public void RequestFocus(UiElement? element, bool nextFrame = false)
    {
        if (nextFrame)
        {
            _pendingFocusRequest = new FocusRequest(FocusRequestKind.Element, element);
            return;
        }

        Focus.RequestFocus(ResolveFocusableTarget(element));
    }

    public bool RequestFocusById(string id, UiElement? scope = null, bool nextFrame = false)
    {
        if (nextFrame)
        {
            _pendingFocusRequest = new FocusRequest(FocusRequestKind.Id, scope: scope, id: id);
            return true;
        }

        UiElement? target = ResolveFocusableTarget(FindElementById(id, scope));
        Focus.RequestFocus(target);
        return target != null;
    }

    public bool RequestFocusFirst(UiElement? scope = null, bool nextFrame = false)
    {
        if (nextFrame)
        {
            _pendingFocusRequest = new FocusRequest(FocusRequestKind.FirstFocusableChild, scope: scope ?? Root);
            return true;
        }

        UiElement? target = FindFocusable(scope ?? Root, 0);
        Focus.RequestFocus(target);
        return target != null;
    }

    public bool RequestFocusChild(UiElement scope, int focusableIndex, bool nextFrame = false)
    {
        if (scope == null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        if (nextFrame)
        {
            _pendingFocusRequest = new FocusRequest(FocusRequestKind.FocusableChild, scope: scope, focusableIndex: focusableIndex);
            return true;
        }

        UiElement? target = FindFocusable(scope, focusableIndex);
        Focus.RequestFocus(target);
        return target != null;
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
        renderer.DefaultFont = DefaultFont;
        UiRenderContext context = new(renderer, DefaultFont);
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
            PageUp = input.Navigation.PageUp,
            PageDown = input.Navigation.PageDown,
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
            Composition = input.Composition,
            KeysDown = input.KeysDown,
            KeysPressed = FilterKey(input.KeysPressed, UiKey.Tab),
            KeysReleased = input.KeysReleased,
            Navigation = navigation
        };
    }

    private void RefreshOutputs(UiInputState input)
    {
        UiElement? previousActiveInputLayer = _activeInputLayer;
        LastInput = input;
        _activeInputLayer = FindActiveInputLayer(Root);
        Hovered = ResolveHoveredElement(input.MousePosition);
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
        ApplyPendingFocusRequest();
        ApplyDefaultFocusForActiveLayer(previousActiveInputLayer);

        bool blockingOverlayOpen = _activeInputLayer != null;
        WantTextInput = Focus.Focused?.WantsTextInput == true;
        WantCaptureKeyboard = WantTextInput || Focus.Focused != null || blockingOverlayOpen;
        WantCaptureMouse = PointerCaptureTarget != null || blockingOverlayOpen;
        RequestedMouseCursor = ResolveMouseCursor(input);
        TextInputRequest = ResolveTextInputRequest();
        RebuildRuntimeStateCaches(input);
    }

    private UiElement? ResolveHoveredElement(UiPoint mousePosition)
    {
        if (_activeInputLayer != null)
        {
            return _activeInputLayer.HitTest(mousePosition);
        }

        return Root.HitTest(mousePosition);
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

    private void ApplyPendingFocusRequest()
    {
        if (!_pendingFocusRequest.IsPending)
        {
            return;
        }

        FocusRequest request = _pendingFocusRequest;
        _pendingFocusRequest = default;

        UiElement? target = request.Kind switch
        {
            FocusRequestKind.Element => ResolveFocusableTarget(request.Element),
            FocusRequestKind.Id => ResolveFocusableTarget(FindElementById(request.Id ?? string.Empty, request.Scope)),
            FocusRequestKind.FirstFocusableChild => FindFocusable(request.Scope ?? Root, 0),
            FocusRequestKind.FocusableChild => FindFocusable(request.Scope ?? Root, request.FocusableIndex),
            _ => null
        };

        Focus.RequestFocus(target);
    }

    private void ApplyDefaultFocusForActiveLayer(UiElement? previousActiveInputLayer)
    {
        if (_activeInputLayer == null || _activeInputLayer == previousActiveInputLayer)
        {
            return;
        }

        if (_activeInputLayer is not UiPopup)
        {
            return;
        }

        if (Focus.Focused != null && IsElementOrAncestor(_activeInputLayer, Focus.Focused))
        {
            return;
        }

        UiElement? target = FindFocusable(_activeInputLayer, 0);
        if (target != null)
        {
            Focus.RequestFocus(target);
        }
    }

    private void RebuildRuntimeStateCaches(UiInputState input)
    {
        Dictionary<UiElement, UiItemStateSnapshot> previousItemStates = _itemStates;
        _itemStates = new Dictionary<UiElement, UiItemStateSnapshot>(previousItemStates.Count);
        _containerStates = new Dictionary<UiElement, UiContainerStateSnapshot>();

        UiItemStateSnapshot lastInteresting = UiItemStateSnapshot.Empty;
        RebuildRuntimeStateCaches(Root, Root.Bounds, input, previousItemStates, ref lastInteresting);

        if (lastInteresting.IsValid)
        {
            LastItemState = lastInteresting;
        }
        else if (FocusedItemState.IsValid)
        {
            LastItemState = FocusedItemState;
        }
        else
        {
            LastItemState = HoveredItemState;
        }
    }

    private void RebuildRuntimeStateCaches(
        UiElement element,
        UiRect inheritedClip,
        UiInputState input,
        IReadOnlyDictionary<UiElement, UiItemStateSnapshot> previousItemStates,
        ref UiItemStateSnapshot lastInteresting)
    {
        if (!element.Visible)
        {
            return;
        }

        UiRect clipBounds = Intersect(inheritedClip, element.ClipBounds);
        bool focused = Focus.Focused == element;
        bool hovered = Hovered == element;

        UiItemStatusFlags status = element.GetItemStatus(this, input, focused, hovered);
        if (previousItemStates.TryGetValue(element, out UiItemStateSnapshot previousState))
        {
            if (status.HasFlag(UiItemStatusFlags.Active) && !previousState.IsActive)
            {
                status |= UiItemStatusFlags.Activated;
            }

            if (!status.HasFlag(UiItemStatusFlags.Active) && previousState.IsActive)
            {
                status |= UiItemStatusFlags.Deactivated;
            }
        }

        UiItemStateSnapshot currentState = new(
            element,
            element.Bounds,
            clipBounds,
            status,
            element.Visible,
            element.Enabled);
        _itemStates[element] = currentState;

        if (IsInterestingItemState(currentState))
        {
            lastInteresting = currentState;
        }

        UiContainerKind kind = GetContainerKind(element);
        if (kind != UiContainerKind.None)
        {
            bool containerHovered = Hovered != null && IsElementOrAncestor(element, Hovered);
            bool containerFocused = Focus.Focused != null && IsElementOrAncestor(element, Focus.Focused);
            bool activeInputLayer = _activeInputLayer != null && _activeInputLayer == element;
            bool activePopup = _activeInputLayer != null
                && (kind == UiContainerKind.Popup || kind == UiContainerKind.Modal || kind == UiContainerKind.MenuBar)
                && IsElementOrAncestor(element, _activeInputLayer);

            _containerStates[element] = new UiContainerStateSnapshot(
                element,
                kind,
                element.Bounds,
                clipBounds,
                element.Visible,
                element.Enabled,
                containerHovered,
                containerFocused,
                IsContainerOpen(element),
                IsContainerActiveTab(element),
                activePopup,
                activeInputLayer);
        }

        if (!ShouldTraverseChildren(element))
        {
            return;
        }

        UiRect childClip = inheritedClip;
        if (element.ClipChildren)
        {
            childClip = Intersect(inheritedClip, element.ClipBounds);
        }

        foreach (UiElement child in element.Children)
        {
            RebuildRuntimeStateCaches(child, childClip, input, previousItemStates, ref lastInteresting);
        }
    }

    private static bool IsInterestingItemState(UiItemStateSnapshot state)
    {
        return state.IsClicked
            || state.IsActivated
            || state.IsDeactivated
            || state.IsEdited
            || state.IsPressed
            || state.IsDragging;
    }

    private static UiContainerKind GetContainerKind(UiElement element)
    {
        return element switch
        {
            UiModal => UiContainerKind.Modal,
            UiPopup => UiContainerKind.Popup,
            UiWindow => UiContainerKind.Window,
            UiMenuBar => UiContainerKind.MenuBar,
            UiTabItem => UiContainerKind.TabItem,
            UiTabBar => UiContainerKind.TabBar,
            UiDockHost => UiContainerKind.DockHost,
            UiModalHost => UiContainerKind.ModalHost,
            _ => UiContainerKind.None
        };
    }

    private static bool IsContainerOpen(UiElement element)
    {
        return element switch
        {
            UiPopup popup => popup.IsOpen,
            UiMenuBar menuBar => menuBar.HasOpenMenu || menuBar.IsPopupOpen,
            UiDockHost dockHost => !dockHost.IsEmpty,
            _ => true
        };
    }

    private static bool IsContainerActiveTab(UiElement element)
    {
        if (element is UiTabItem tabItem)
        {
            return tabItem.IsActive;
        }

        if (element is UiWindow window && window.Parent is UiDockHost dockHost)
        {
            return dockHost.ActiveWindow == window;
        }

        return false;
    }

    private UiElement? ResolveFocusableTarget(UiElement? element)
    {
        if (element == null)
        {
            return null;
        }

        if (element.Visible && element.Enabled && element.IsFocusable)
        {
            return element;
        }

        return FindFocusable(element, 0);
    }

    private static UiElement? FindElementById(UiElement scope, string id)
    {
        if (!scope.Visible)
        {
            return null;
        }

        if (string.Equals(scope.Id, id, StringComparison.Ordinal))
        {
            return scope;
        }

        if (!ShouldTraverseChildren(scope))
        {
            return null;
        }

        foreach (UiElement child in scope.Children)
        {
            UiElement? match = FindElementById(child, id);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static UiElement? FindFocusable(UiElement scope, int focusableIndex)
    {
        if (focusableIndex < 0)
        {
            return null;
        }

        List<UiElement> focusables = new();
        CollectFocusable(scope, focusables);
        return focusableIndex < focusables.Count ? focusables[focusableIndex] : null;
    }

    private static UiRect Intersect(UiRect a, UiRect b)
    {
        int left = Math.Max(a.Left, b.Left);
        int top = Math.Max(a.Top, b.Top);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);

        if (right <= left || bottom <= top)
        {
            return new UiRect(left, top, 0, 0);
        }

        return new UiRect(left, top, right - left, bottom - top);
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
