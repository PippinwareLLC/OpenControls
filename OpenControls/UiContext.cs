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

    private sealed class RenderCacheState
    {
        private readonly string _name;

        public RenderCacheState(string name)
        {
            _name = name;
        }

        public UiRenderCommandList? CommandList { get; private set; }
        public long RecordedInvalidationVersion { get; private set; }
        public int InteractionSignature { get; private set; }
        public UiFont? RecordedDefaultFont { get; private set; }
        public long LastSeenInvalidationVersion { get; private set; }
        public int LastInteractionSignature { get; private set; }
        public UiRenderCachePassAction LastAction { get; private set; }
        public UiRenderCacheMissReason LastMissReason { get; private set; }
        public long RecordCount { get; private set; }
        public long ReplayCount { get; private set; }
        public long BypassCount { get; private set; }
        public long DisabledBypassCount { get; private set; }
        public long VolatileBypassCount { get; private set; }
        public long EmptyMissCount { get; private set; }
        public long InvalidationMissCount { get; private set; }
        public long InteractionMissCount { get; private set; }
        public long FontMissCount { get; private set; }

        public bool CanReplay(long invalidationVersion, int interactionSignature, UiFont? defaultFont)
        {
            return CommandList != null
                && RecordedInvalidationVersion == invalidationVersion
                && InteractionSignature == interactionSignature
                && ReferenceEquals(RecordedDefaultFont, defaultFont);
        }

        public void Store(UiRenderCommandList commandList, long invalidationVersion, int interactionSignature, UiFont? defaultFont)
        {
            CommandList = commandList ?? throw new ArgumentNullException(nameof(commandList));
            RecordedInvalidationVersion = invalidationVersion;
            InteractionSignature = interactionSignature;
            RecordedDefaultFont = defaultFont;
        }

        public void Clear()
        {
            CommandList = null;
            RecordedInvalidationVersion = 0;
            InteractionSignature = 0;
            RecordedDefaultFont = null;
        }

        public UiRenderCacheMissReason ClassifyMiss(long invalidationVersion, int interactionSignature, UiFont? defaultFont)
        {
            LastSeenInvalidationVersion = invalidationVersion;
            LastInteractionSignature = interactionSignature;

            if (CommandList == null)
            {
                return UiRenderCacheMissReason.Empty;
            }

            if (RecordedInvalidationVersion != invalidationVersion)
            {
                return UiRenderCacheMissReason.Invalidation;
            }

            if (InteractionSignature != interactionSignature)
            {
                return UiRenderCacheMissReason.Interaction;
            }

            if (!ReferenceEquals(RecordedDefaultFont, defaultFont))
            {
                return UiRenderCacheMissReason.Font;
            }

            return UiRenderCacheMissReason.None;
        }

        public void MarkRecord(UiRenderCacheMissReason missReason)
        {
            RecordCount++;
            LastAction = UiRenderCachePassAction.RecordAndReplay;
            LastMissReason = missReason;
            IncrementMissCounter(missReason);
        }

        public void MarkReplay(bool recordedThisPass)
        {
            ReplayCount++;
            if (recordedThisPass)
            {
                LastAction = UiRenderCachePassAction.RecordAndReplay;
            }
            else
            {
                LastAction = UiRenderCachePassAction.Replay;
                LastMissReason = UiRenderCacheMissReason.None;
            }
        }

        public void MarkBypass(UiRenderCacheMissReason reason, long invalidationVersion, int interactionSignature)
        {
            LastSeenInvalidationVersion = invalidationVersion;
            LastInteractionSignature = interactionSignature;
            LastAction = UiRenderCachePassAction.Bypass;
            LastMissReason = reason;
            BypassCount++;
            if (reason == UiRenderCacheMissReason.Disabled)
            {
                DisabledBypassCount++;
            }
            else if (reason == UiRenderCacheMissReason.Volatile)
            {
                VolatileBypassCount++;
            }
        }

        public UiRenderCachePassStatisticsSnapshot BuildSnapshot()
        {
            return new UiRenderCachePassStatisticsSnapshot(
                _name,
                CommandList != null,
                RecordedInvalidationVersion,
                LastSeenInvalidationVersion,
                LastInteractionSignature,
                LastAction,
                LastMissReason,
                RecordCount,
                ReplayCount,
                BypassCount,
                DisabledBypassCount,
                VolatileBypassCount,
                EmptyMissCount,
                InvalidationMissCount,
                InteractionMissCount,
                FontMissCount);
        }

        private void IncrementMissCounter(UiRenderCacheMissReason missReason)
        {
            if (missReason == UiRenderCacheMissReason.Empty)
            {
                EmptyMissCount++;
            }
            else if (missReason == UiRenderCacheMissReason.Invalidation)
            {
                InvalidationMissCount++;
            }
            else if (missReason == UiRenderCacheMissReason.Interaction)
            {
                InteractionMissCount++;
            }
            else if (missReason == UiRenderCacheMissReason.Font)
            {
                FontMissCount++;
            }
        }
    }

    private sealed class CacheRootRenderCacheSet
    {
        public CacheRootRenderCacheSet(string elementLabel)
        {
            string safeLabel = string.IsNullOrWhiteSpace(elementLabel) ? "Element" : elementLabel;
            MainPass = new RenderCacheState($"CacheRoot.{safeLabel}.Main");
            OverlayPass = new RenderCacheState($"CacheRoot.{safeLabel}.Overlay");
        }

        public RenderCacheState MainPass { get; }
        public RenderCacheState OverlayPass { get; }

        public RenderCacheState Get(UiRenderPassKind passKind) => passKind == UiRenderPassKind.Overlay ? OverlayPass : MainPass;
    }

    private UiElement? _mouseCaptureTarget;
    private UiElement? _activeInputLayer;
    private FocusRequest _pendingFocusRequest;
    private Dictionary<UiElement, UiItemStateSnapshot> _itemStates = new();
    private Dictionary<UiElement, UiContainerStateSnapshot> _containerStates = new();
    private readonly UiMemoryClipboard _fallbackClipboard = new();
    private readonly RenderCacheState _rootRenderCache = new("Root");
    private readonly RenderCacheState _overlayRenderCache = new("Overlay");
    private readonly Dictionary<UiElement, CacheRootRenderCacheSet> _cacheRootRenderCaches = new();
    private bool _lastRenderHasVolatileState;
    private string _lastRenderVolatileElementLabel = string.Empty;

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
    public UiInvalidationReason RootInvalidationReasons => Root.SubtreeInvalidationReasons;
    public long RootInvalidationVersion => Root.SubtreeInvalidationVersion;
    public bool RenderCachingEnabled { get; set; }
    public UiRenderCacheStatisticsSnapshot RenderCacheStatistics => new(
        RenderCachingEnabled,
        RootInvalidationReasons,
        RootInvalidationVersion,
        _lastRenderHasVolatileState,
        _lastRenderVolatileElementLabel,
        _rootRenderCache.BuildSnapshot(),
        _overlayRenderCache.BuildSnapshot());

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

        UiElement? activeInputLayer = UiUpdateContext.ResolveActiveInputLayer(Root) ?? _activeInputLayer;
        DragDrop.BeginFrame(effectiveInput);
        Root.Update(new UiUpdateContext(effectiveInput, Focus, DragDrop, deltaSeconds, DefaultFont, Clipboard, activeInputLayer));
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
        PurgeDetachedCacheRoots();

        UiElement? volatileElement = FindFirstVolatileRenderState(Root, Root);
        _lastRenderHasVolatileState = volatileElement != null;
        _lastRenderVolatileElementLabel = BuildProfilerElementLabel(volatileElement);
        using (UiProfiling.Scope("OpenControls.Context.Root"))
        {
            UiRenderContext context = CreateRenderContext(renderer, UiRenderPassKind.Main);
            RenderPass(Root, context, _rootRenderCache, "Root", UiRenderPassKind.Main);
        }

        using (UiProfiling.Scope("OpenControls.Context.Overlay"))
        {
            UiRenderContext context = CreateRenderContext(renderer, UiRenderPassKind.Overlay);
            RenderPass(Root, context, _overlayRenderCache, "Overlay", UiRenderPassKind.Overlay);
        }
    }

    private void RenderPass(
        UiElement scopeRoot,
        UiRenderContext liveContext,
        RenderCacheState cacheState,
        string passName,
        UiRenderPassKind passKind)
    {
        long invalidationVersion = ComputeRenderCacheInvalidationVersion(scopeRoot);
        int interactionSignature = ComputeRenderCacheInteractionSignature(scopeRoot);
        UiElement? volatileElement = FindFirstVolatileRenderState(scopeRoot, scopeRoot);

        if (!RenderCachingEnabled)
        {
            cacheState.Clear();
            using (UiProfiling.Scope($"OpenControls.Context.{passName}.Bypass.Disabled"))
            {
                cacheState.MarkBypass(UiRenderCacheMissReason.Disabled, invalidationVersion, interactionSignature);
                RenderElementUncached(scopeRoot, liveContext, passKind);
            }
            return;
        }

        if (volatileElement != null)
        {
            cacheState.Clear();
            using (UiProfiling.Scope($"OpenControls.Context.{passName}.Bypass.Volatile"))
            {
                cacheState.MarkBypass(UiRenderCacheMissReason.Volatile, invalidationVersion, interactionSignature);
                RenderElementUncached(scopeRoot, liveContext, passKind);
            }
            return;
        }

        UiRenderCacheMissReason missReason = cacheState.ClassifyMiss(invalidationVersion, interactionSignature, DefaultFont);
        bool recordedThisPass = missReason != UiRenderCacheMissReason.None;
        if (recordedThisPass)
        {
            using (UiProfiling.Scope($"OpenControls.Context.{passName}.Record"))
            {
                UiRecordingRenderer recordingRenderer = new(liveContext.Renderer, DefaultFont);
                UiRenderContext recordingContext = CreateRenderContext(recordingRenderer, passKind);
                RenderElementUncached(scopeRoot, recordingContext, passKind);
                cacheState.Store(recordingRenderer.BuildCommandList(), invalidationVersion, interactionSignature, DefaultFont);
                cacheState.MarkRecord(missReason);
            }
        }

        using (UiProfiling.Scope($"OpenControls.Context.{passName}.Replay"))
        {
            cacheState.MarkReplay(recordedThisPass);
            cacheState.CommandList?.Replay(liveContext);
        }
    }

    private UiRenderContext CreateRenderContext(IUiRenderer renderer, UiRenderPassKind passKind)
    {
        return new UiRenderContext(renderer, DefaultFont, RenderChildFromContext, passKind);
    }

    private void RenderChildFromContext(UiElement child, UiRenderContext context, UiRenderPassKind passKind)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (child.IsRenderCacheRoot(this))
        {
            if (context.Renderer is UiRecordingRenderer recordingRenderer)
            {
                recordingRenderer.RecordSubtree(child, passKind);
                return;
            }

            RenderCacheState cacheState = GetOrCreateCacheRootRenderCacheSet(child).Get(passKind);
            string passName = BuildCacheRootPassName(child, passKind);
            using (UiProfiling.Scope($"OpenControls.Context.{passName}"))
            {
                RenderPass(child, context, cacheState, passName, passKind);
            }

            return;
        }

        RenderElementUncached(child, context, passKind);
    }

    private static void RenderElementUncached(UiElement element, UiRenderContext context, UiRenderPassKind passKind)
    {
        if (passKind == UiRenderPassKind.Overlay)
        {
            element.RenderOverlay(context);
        }
        else
        {
            element.Render(context);
        }
    }

    private CacheRootRenderCacheSet GetOrCreateCacheRootRenderCacheSet(UiElement element)
    {
        if (_cacheRootRenderCaches.TryGetValue(element, out CacheRootRenderCacheSet? existing))
        {
            return existing;
        }

        CacheRootRenderCacheSet created = new(BuildProfilerElementLabel(element));
        _cacheRootRenderCaches[element] = created;
        return created;
    }

    private void PurgeDetachedCacheRoots()
    {
        if (_cacheRootRenderCaches.Count == 0)
        {
            return;
        }

        List<UiElement>? stale = null;
        foreach (UiElement element in _cacheRootRenderCaches.Keys)
        {
            if (!IsElementOrAncestor(Root, element))
            {
                stale ??= new List<UiElement>();
                stale.Add(element);
            }
        }

        if (stale == null)
        {
            return;
        }

        foreach (UiElement element in stale)
        {
            _cacheRootRenderCaches.Remove(element);
        }
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
        _activeInputLayer = UiUpdateContext.ResolveActiveInputLayer(Root);
        Hovered = ResolveHoveredElement(input.MousePosition);
        if (input.LeftClicked && _activeInputLayer == null && ResolveFocusTarget(Hovered) == null)
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

    private UiElement? FindFirstVolatileRenderState(UiElement element, UiElement scopeRoot)
    {
        if (!element.Visible)
        {
            return null;
        }

        if (element != scopeRoot && element.IsRenderCacheRoot(this))
        {
            return null;
        }

        if (element.IsRenderCacheVolatile(this))
        {
            return element;
        }

        if (!ShouldTraverseChildren(element))
        {
            return null;
        }

        foreach (UiElement child in element.Children)
        {
            UiElement? volatileElement = FindFirstVolatileRenderState(child, scopeRoot);
            if (volatileElement != null)
            {
                return volatileElement;
            }
        }

        return null;
    }

    private long ComputeRenderCacheInvalidationVersion(UiElement element)
    {
        long version = element.LocalInvalidationVersion;
        if (!ShouldTraverseChildren(element))
        {
            return version;
        }

        foreach (UiElement child in element.Children)
        {
            if (child.IsRenderCacheRoot(this))
            {
                continue;
            }

            version = Math.Max(version, ComputeRenderCacheInvalidationVersion(child));
        }

        return version;
    }

    private int ComputeRenderCacheInteractionSignature(UiElement scopeRoot)
    {
        bool hoveredInScope = IsElementInRenderScope(scopeRoot, Hovered);
        bool focusedInScope = IsElementInRenderScope(scopeRoot, Focus.Focused);
        bool activeLayerInScope = IsElementInRenderScope(scopeRoot, _activeInputLayer);
        bool pointerCaptureInScope = IsElementInRenderScope(scopeRoot, PointerCaptureTarget);

        HashCode hash = new();
        hash.Add(hoveredInScope ? Hovered : null);
        hash.Add(focusedInScope ? Focus.Focused : null);
        hash.Add(activeLayerInScope ? _activeInputLayer : null);
        hash.Add(pointerCaptureInScope ? PointerCaptureTarget : null);
        hash.Add(WantCaptureMouse && pointerCaptureInScope);
        hash.Add(WantCaptureKeyboard && (focusedInScope || activeLayerInScope));
        hash.Add(WantTextInput && focusedInScope);

        if (hoveredInScope || focusedInScope || activeLayerInScope || pointerCaptureInScope)
        {
            AppendInputSignature(ref hash, LastInput);
        }
        else
        {
            hash.Add(0);
        }

        return hash.ToHashCode();
    }

    private bool IsElementInRenderScope(UiElement scopeRoot, UiElement? candidate)
    {
        UiElement? current = candidate;
        while (current != null)
        {
            if (current == scopeRoot)
            {
                return true;
            }

            if (current.IsRenderCacheRoot(this))
            {
                return false;
            }

            current = current.Parent;
        }

        return false;
    }

    private static void AppendInputSignature(ref HashCode hash, UiInputState? input)
    {
        if (input == null)
        {
            hash.Add(0);
            return;
        }

        hash.Add(input.MousePosition);
        hash.Add(input.ScreenMousePosition);
        hash.Add(input.LeftDown);
        hash.Add(input.LeftClicked);
        hash.Add(input.LeftDoubleClicked);
        hash.Add(input.LeftReleased);
        hash.Add(input.RightDown);
        hash.Add(input.RightClicked);
        hash.Add(input.RightDoubleClicked);
        hash.Add(input.RightReleased);
        hash.Add(input.MiddleDown);
        hash.Add(input.MiddleClicked);
        hash.Add(input.MiddleDoubleClicked);
        hash.Add(input.MiddleReleased);
        hash.Add(input.LeftDragOrigin);
        hash.Add(input.RightDragOrigin);
        hash.Add(input.MiddleDragOrigin);
        hash.Add(input.DragThreshold);
        hash.Add(input.ShiftDown);
        hash.Add(input.CtrlDown);
        hash.Add(input.AltDown);
        hash.Add(input.SuperDown);
        hash.Add(input.ScrollDeltaX);
        hash.Add(input.ScrollDelta);
        hash.Add(input.Composition.Text);
        hash.Add(input.Composition.CaretIndex);

        AppendCharList(ref hash, input.TextInput);
        AppendKeyList(ref hash, input.KeysDown);
        AppendKeyList(ref hash, input.KeysPressed);
        AppendKeyList(ref hash, input.KeysReleased);

        UiNavigationInput navigation = input.Navigation;
        hash.Add(navigation.MoveLeft);
        hash.Add(navigation.MoveRight);
        hash.Add(navigation.MoveUp);
        hash.Add(navigation.MoveDown);
        hash.Add(navigation.PageUp);
        hash.Add(navigation.PageDown);
        hash.Add(navigation.Home);
        hash.Add(navigation.End);
        hash.Add(navigation.Backspace);
        hash.Add(navigation.Delete);
        hash.Add(navigation.Tab);
        hash.Add(navigation.Enter);
        hash.Add(navigation.KeypadEnter);
        hash.Add(navigation.Space);
        hash.Add(navigation.Escape);
    }

    private static string BuildProfilerElementLabel(UiElement? element)
    {
        if (element == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            return $"{element.GetType().Name}#{element.Id}";
        }

        return element.GetType().Name;
    }

    private static string BuildCacheRootPassName(UiElement element, UiRenderPassKind passKind)
    {
        string label = BuildProfilerElementLabel(element);
        return $"CacheRoot.{(passKind == UiRenderPassKind.Overlay ? "Overlay" : "Root")}.{label}";
    }

    private static void AppendCharList(ref HashCode hash, IReadOnlyList<char> items)
    {
        hash.Add(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            hash.Add(items[i]);
        }
    }

    private static void AppendKeyList(ref HashCode hash, IReadOnlyList<UiKey> items)
    {
        hash.Add(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            hash.Add((int)items[i]);
        }
    }
}
