using OpenControls.State;

namespace OpenControls.Controls;

public sealed class UiDockWorkspace : UiElement
{
    public event Action<UiWindow, UiPoint>? TabDetached;

    public readonly record struct ExternalDockDebugState(
        bool ExternalPreviewActive,
        string? ExternalPreviewWindowId,
        string? ExternalPreviewWindowTitle,
        UiPoint HoverPoint,
        string? HoverHostId,
        DockTarget HoverTarget,
        UiRect HoverHostBounds,
        UiRect PreviewBounds,
        UiRect PreviewWindowBounds,
        UiRect WorkspaceBounds);

    public enum DockTarget
    {
        None,
        Center,
        Left,
        Right,
        Top,
        Bottom
    }

    private sealed class DockNode
    {
        public DockNode(UiDockHost? host)
        {
            Host = host;
        }

        public UiDockHost? Host { get; set; }
        public DockNode? First { get; set; }
        public DockNode? Second { get; set; }
        public bool SplitHorizontal { get; set; }
        public float SplitRatio { get; set; } = 0.5f;
        public UiRect Bounds { get; set; }
        public UiRect FirstBounds { get; set; }
        public UiRect SecondBounds { get; set; }
        public UiRect SplitterBounds { get; set; }
    }

    private readonly List<UiDockHost> _hosts = new();
    private readonly List<UiWindow> _floatingWindows = new();
    private DockNode _rootNode;
    private int _hostIdCounter;

    private UiWindow? _dragWindow;
    private UiDockHost? _dragSourceHost;
    private UiPoint _dragStart;
    private UiPoint _dragPosition;
    private bool _dragMoved;
    private int _dragPointerOffsetX;
    private int _dragPointerOffsetY;
    private UiDockHost? _hoverHost;
    private DockTarget _hoverTarget;
    private UiRect _previewBounds;
    private UiWindow? _floatingDragWindow;
    private UiWindow? _externalPreviewWindow;
    private UiPoint _externalPreviewHoverPoint;
    private UiRect _externalPreviewWindowBounds;
    private DockNode? _hoverSplitNode;
    private DockNode? _dragSplitNode;
    private int _dragSplitStartAxis;
    private int _dragSplitStartPrimarySize;

    public UiColor DragPreviewColor { get; set; } = new(70, 130, 200, 120);
    public UiColor DragPreviewOutline { get; set; } = new(120, 180, 220, 200);
    public UiColor DropTargetColor { get; set; } = new(50, 110, 180, 130);
    public UiColor DropTargetActiveColor { get; set; } = new(110, 180, 230, 180);
    public UiColor DropTargetOutline { get; set; } = new(160, 200, 240, 220);
    public int DropTargetSize { get; set; } = 48;
    public int DragThreshold { get; set; } = 6;
    public int SplitterThickness { get; set; } = 6;
    public int MinPaneSize { get; set; } = 80;
    public UiColor SplitterColor { get; set; } = new(44, 52, 68);
    public UiColor SplitterHoverColor { get; set; } = new(68, 82, 106);
    public UiColor SplitterActiveColor { get; set; } = new(96, 120, 154);

    public UiDockHost RootHost { get; }
    public IReadOnlyList<UiDockHost> DockHosts => _hosts;
    public IReadOnlyList<UiWindow> FloatingWindows => _floatingWindows;
    public override bool CapturesPointerInput => _hoverSplitNode != null || _dragSplitNode != null;

    public UiDockWorkspace()
    {
        RootHost = CreateHost();
        AssignHostId(RootHost);
        _rootNode = new DockNode(RootHost);
    }

    public UiDockHost SplitHost(UiDockHost host, DockTarget target)
    {
        if (target is DockTarget.Center or DockTarget.None)
        {
            return host;
        }

        DockNode? node = FindNode(_rootNode, host);
        if (node == null)
        {
            return host;
        }

        UiDockHost newHost = CreateHost(host);
        DockNode first;
        DockNode second;

        bool horizontal = target is DockTarget.Top or DockTarget.Bottom;
        if (target is DockTarget.Left or DockTarget.Top)
        {
            first = new DockNode(newHost);
            second = new DockNode(host);
        }
        else
        {
            first = new DockNode(host);
            second = new DockNode(newHost);
        }

        node.Host = null;
        node.First = first;
        node.Second = second;
        node.SplitHorizontal = horizontal;
        node.SplitRatio = 0.5f;

        return newHost;
    }

    public UiDockWorkspaceState CaptureState()
    {
        EnsureHostIds();

        UiDockWorkspaceState state = new()
        {
            Id = Id,
            Root = CaptureNodeState(_rootNode)
        };

        foreach (UiDockHost host in _hosts)
        {
            if (string.IsNullOrWhiteSpace(host.Id))
            {
                continue;
            }

            UiDockHostState hostState = new()
            {
                HostId = host.Id,
                ActiveIndex = host.ActiveIndex
            };

            foreach (UiWindow window in host.Windows)
            {
                if (!string.IsNullOrWhiteSpace(window.Id))
                {
                    hostState.WindowIds.Add(window.Id);
                }
            }

            state.Hosts.Add(hostState);
        }

        foreach (UiWindow window in _floatingWindows)
        {
            if (string.IsNullOrWhiteSpace(window.Id))
            {
                continue;
            }

            state.FloatingWindows.Add(new UiFloatingWindowState
            {
                WindowId = window.Id,
                Bounds = window.Bounds
            });
        }

        return state;
    }

    public void ApplyState(UiDockWorkspaceState state, IReadOnlyDictionary<string, UiWindow> windowsById)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (windowsById == null)
        {
            throw new ArgumentNullException(nameof(windowsById));
        }

        if (state.Root == null)
        {
            return;
        }

        EnsureHostIds();

        Dictionary<string, UiDockHost> hostById = new(StringComparer.Ordinal);
        foreach (UiDockHost host in _hosts)
        {
            if (!string.IsNullOrWhiteSpace(host.Id))
            {
                hostById[host.Id] = host;
            }
        }

        HashSet<UiDockHost> usedHosts = new();
        _rootNode = BuildNode(state.Root, hostById, usedHosts);

        foreach (UiDockHost host in _hosts)
        {
            host.ClearWindows();
        }

        ClearFloatingWindows();

        List<UiDockHost> existingHosts = new(_hosts);
        foreach (UiDockHost host in existingHosts)
        {
            if (host != RootHost && !usedHosts.Contains(host))
            {
                RemoveDockHost(host);
            }
        }

        foreach (UiDockHostState hostState in state.Hosts)
        {
            if (!hostById.TryGetValue(hostState.HostId, out UiDockHost? host))
            {
                continue;
            }

            foreach (string windowId in hostState.WindowIds)
            {
                if (windowsById.TryGetValue(windowId, out UiWindow? window))
                {
                    DetachWindowInternal(window);
                    host.DockWindow(window);
                }
            }

            host.ActivateWindow(hostState.ActiveIndex);
        }

        foreach (UiFloatingWindowState floatingState in state.FloatingWindows)
        {
            if (windowsById.TryGetValue(floatingState.WindowId, out UiWindow? window))
            {
                DetachWindowInternal(window);
                window.Bounds = floatingState.Bounds;
                AddFloatingWindow(window);
            }
        }

        CollapseEmptyHosts();
    }

    public void AddFloatingWindow(UiWindow window)
    {
        if (_floatingWindows.Contains(window))
        {
            return;
        }

        window.ShowTitleBar = true;
        window.AllowDrag = true;
        AddChild(window);
        _floatingWindows.Add(window);
    }

    public void DockWindow(UiWindow window, UiDockHost host, int index = -1)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(host);

        if (!_hosts.Contains(host))
        {
            throw new ArgumentException("Dock host is not part of this workspace.", nameof(host));
        }

        DetachWindowInternal(window);
        window.AllowDrag = false;
        if (index >= 0)
        {
            host.DockWindow(window, index);
            host.ActivateWindow(index);
        }
        else
        {
            host.DockWindow(window);
            host.ActivateWindow(host.Windows.Count - 1);
        }

        CollapseEmptyHosts();
    }

    public void DetachWindow(UiWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        DetachWindowInternal(window);
        CollapseEmptyHosts();
    }

    public void PreviewExternalDock(UiWindow window, UiPoint hoverPoint, UiRect previewWindowBounds)
    {
        ArgumentNullException.ThrowIfNull(window);

        _externalPreviewWindow = window;
        _externalPreviewHoverPoint = hoverPoint;
        _externalPreviewWindowBounds = previewWindowBounds;
        UpdateExternalPreviewHover(hoverPoint, previewWindowBounds);
    }

    public void ClearExternalDockPreview(UiWindow? window = null)
    {
        if (window != null && !ReferenceEquals(_externalPreviewWindow, window))
        {
            return;
        }

        _externalPreviewWindow = null;
        _externalPreviewHoverPoint = default;
        _externalPreviewWindowBounds = default;
        if (_dragWindow == null || !_dragMoved)
        {
            _hoverHost = null;
            _hoverTarget = DockTarget.None;
            _previewBounds = default;
        }
    }

    public bool CommitExternalDock(UiWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!ReferenceEquals(_externalPreviewWindow, window))
        {
            return false;
        }

        UiDockHost? hoverHost = _hoverHost;
        DockTarget hoverTarget = _hoverTarget;
        ClearExternalDockPreview(window);
        if (hoverHost == null || hoverTarget == DockTarget.None)
        {
            return false;
        }

        if (window.Parent is UiDockHost currentHost)
        {
            currentHost.RemoveWindow(window);
        }
        else
        {
            window.Parent?.RemoveChild(window);
        }

        UiDockHost targetHost = hoverHost;
        if (hoverTarget is DockTarget.Left or DockTarget.Right or DockTarget.Top or DockTarget.Bottom)
        {
            targetHost = SplitHost(hoverHost, hoverTarget);
        }

        window.AllowDrag = false;
        targetHost.DockWindow(window);
        targetHost.ActivateWindow(targetHost.Windows.Count - 1);
        CollapseEmptyHosts();
        return true;
    }

    public ExternalDockDebugState GetExternalDockDebugState()
    {
        return new ExternalDockDebugState(
            ExternalPreviewActive: _externalPreviewWindow != null,
            ExternalPreviewWindowId: _externalPreviewWindow?.Id,
            ExternalPreviewWindowTitle: _externalPreviewWindow?.Title,
            HoverPoint: _externalPreviewHoverPoint,
            HoverHostId: _hoverHost?.Id,
            HoverTarget: _hoverTarget,
            HoverHostBounds: _hoverHost?.Bounds ?? default,
            PreviewBounds: _previewBounds,
            PreviewWindowBounds: _externalPreviewWindowBounds,
            WorkspaceBounds: Bounds);
    }

    public void ResetLayout()
    {
        foreach (UiDockHost host in _hosts)
        {
            host.ClearWindows();
        }

        ClearFloatingWindows();

        for (int i = _hosts.Count - 1; i >= 0; i--)
        {
            UiDockHost host = _hosts[i];
            if (host == RootHost)
            {
                continue;
            }

            RemoveDockHost(host);
        }

        _rootNode = new DockNode(RootHost);
        EnsureRootHost();
    }

    private void ClearFloatingWindows()
    {
        for (int i = _floatingWindows.Count - 1; i >= 0; i--)
        {
            RemoveChild(_floatingWindows[i]);
        }

        _floatingWindows.Clear();
    }

    private void DetachWindowInternal(UiWindow window)
    {
        if (_floatingWindows.Contains(window))
        {
            _floatingWindows.Remove(window);
            RemoveChild(window);
            return;
        }

        foreach (UiDockHost host in _hosts)
        {
            if (host.RemoveWindow(window))
            {
                return;
            }
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        LayoutNode(_rootNode, Bounds);

        UiInputState input = context.Input;
        UpdateSplitters(input, context.Focus);
        LayoutNode(_rootNode, Bounds);
        UpdateTabDrag(input);

        base.Update(context);

        UpdateFloatingDrag(input);
        UpdateHover(input.MousePosition);
        ClampFloatingWindows();
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        base.Render(context);
        DrawSplitters(context);

        if ((_dragWindow != null && _dragMoved) || _externalPreviewWindow != null)
        {
            DrawPreview(context);
            DrawTargets(context);
        }
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        DockNode? splitNode = _dragSplitNode ?? _hoverSplitNode;
        if (splitNode != null)
        {
            cursor = splitNode.SplitHorizontal ? UiMouseCursor.ResizeNS : UiMouseCursor.ResizeEW;
            return true;
        }

        cursor = UiMouseCursor.Arrow;
        return false;
    }

    private UiDockHost CreateHost(UiDockHost? template = null, string? hostId = null)
    {
        UiDockHost host = new();
        if (template != null)
        {
            CopyHostStyle(template, host);
        }

        if (!string.IsNullOrWhiteSpace(hostId))
        {
            host.Id = hostId;
        }

        host.ExternalDragHandling = true;
        host.AllowDetach = false;
        AssignHostId(host);

        _hosts.Add(host);
        AddChild(host);
        return host;
    }

    private UiDockNodeState CaptureNodeState(DockNode node)
    {
        if (node.Host != null)
        {
            return new UiDockNodeState { HostId = node.Host.Id };
        }

        UiDockNodeState state = new()
        {
            SplitHorizontal = node.SplitHorizontal,
            SplitRatio = node.SplitRatio
        };

        if (node.First != null)
        {
            state.First = CaptureNodeState(node.First);
        }

        if (node.Second != null)
        {
            state.Second = CaptureNodeState(node.Second);
        }

        return state;
    }

    private DockNode BuildNode(UiDockNodeState state, Dictionary<string, UiDockHost> hostById, HashSet<UiDockHost> usedHosts)
    {
        if (!string.IsNullOrWhiteSpace(state.HostId))
        {
            UiDockHost host = GetOrCreateHost(state.HostId, hostById);
            usedHosts.Add(host);
            return new DockNode(host);
        }

        DockNode node = new(null)
        {
            SplitHorizontal = state.SplitHorizontal,
            SplitRatio = state.SplitRatio
        };

        if (state.First != null)
        {
            node.First = BuildNode(state.First, hostById, usedHosts);
        }

        if (state.Second != null)
        {
            node.Second = BuildNode(state.Second, hostById, usedHosts);
        }

        return node;
    }

    private UiDockHost GetOrCreateHost(string hostId, Dictionary<string, UiDockHost> hostById)
    {
        if (hostById.TryGetValue(hostId, out UiDockHost? host))
        {
            return host;
        }

        UiDockHost created = CreateHost(RootHost, hostId);
        hostById[hostId] = created;
        return created;
    }

    private void UpdateSplitters(UiInputState input, UiFocusManager focus)
    {
        _hoverSplitNode = FindSplitterNode(_rootNode, input.MousePosition);

        if (_dragSplitNode == null && input.LeftClicked && _hoverSplitNode != null)
        {
            _dragSplitNode = _hoverSplitNode;
            _dragSplitStartAxis = GetSplitterAxisPosition(_dragSplitNode, input.MousePosition);
            _dragSplitStartPrimarySize = GetNodePrimarySize(_dragSplitNode.FirstBounds, _dragSplitNode.SplitHorizontal);
            focus.RequestFocus(null);
        }

        if (_dragSplitNode == null)
        {
            return;
        }

        if (input.LeftDown)
        {
            int delta = GetSplitterAxisPosition(_dragSplitNode, input.MousePosition) - _dragSplitStartAxis;
            int available = GetNodePrimarySize(_dragSplitNode.Bounds, _dragSplitNode.SplitHorizontal) - Math.Max(1, SplitterThickness);
            int minFirst = GetMinimumNodePrimarySize(_dragSplitNode.First, _dragSplitNode.SplitHorizontal);
            int minSecond = GetMinimumNodePrimarySize(_dragSplitNode.Second, _dragSplitNode.SplitHorizontal);
            int desiredFirst = _dragSplitStartPrimarySize + delta;
            int firstSize = ClampSplitSize(desiredFirst, available, minFirst, minSecond);
            _dragSplitNode.SplitRatio = available > 0 ? firstSize / (float)available : 0.5f;
            _hoverSplitNode = _dragSplitNode;
        }

        if (input.LeftReleased)
        {
            _dragSplitNode = null;
        }
    }

    private void UpdateTabDrag(UiInputState input)
    {
        if (_dragSplitNode != null)
        {
            return;
        }

        if (_dragWindow != null && _dragSourceHost == null)
        {
            return;
        }

        if (_dragWindow == null && input.LeftClicked)
        {
            foreach (UiDockHost host in _hosts)
            {
                int index = host.GetTabIndexAt(input.MousePosition);
                if (index >= 0 && index < host.Windows.Count)
                {
                    host.ActivateWindow(index);
                    _dragWindow = host.Windows[index];
                    _dragSourceHost = host;
                    _dragStart = input.MousePosition;
                    _dragPosition = input.MousePosition;
                    _dragMoved = false;
                    UiRect tabRect = host.GetTabBounds(index);
                    _dragPointerOffsetX = Math.Clamp(input.MousePosition.X - tabRect.X, 0, Math.Max(0, tabRect.Width));
                    _dragPointerOffsetY = Math.Clamp(input.MousePosition.Y - tabRect.Y, 0, Math.Max(0, tabRect.Height));
                    break;
                }
            }
        }

        if (_dragWindow == null || _dragSourceHost == null)
        {
            return;
        }

        if (input.LeftDown)
        {
            _dragPosition = input.MousePosition;
            int deltaX = Math.Abs(_dragPosition.X - _dragStart.X);
            int deltaY = Math.Abs(_dragPosition.Y - _dragStart.Y);
            if (!_dragMoved && (deltaX >= DragThreshold || deltaY >= DragThreshold))
            {
                _dragMoved = true;
            }

            if (_dragMoved && _dragSourceHost.AllowReorder && _dragSourceHost.IsPointInTabBar(_dragPosition))
            {
                int targetIndex = _dragSourceHost.GetTabIndexAt(_dragPosition);
                int sourceIndex = -1;
                for (int i = 0; i < _dragSourceHost.Windows.Count; i++)
                {
                    if (_dragSourceHost.Windows[i] == _dragWindow)
                    {
                        sourceIndex = i;
                        break;
                    }
                }
                if (targetIndex >= 0 && targetIndex < _dragSourceHost.Windows.Count && targetIndex != sourceIndex)
                {
                    _dragSourceHost.MoveWindow(sourceIndex, targetIndex);
                }
            }
            else if (_dragMoved
                && !Bounds.Contains(_dragPosition)
                && CanDetachWindowExternally(_dragSourceHost, _dragWindow))
            {
                UiWindow window = _dragWindow;
                UiDockHost sourceHost = _dragSourceHost;
                sourceHost.RemoveWindow(window);
                CollapseEmptyHosts();
                TabDetached?.Invoke(window, GetDetachPoint(input));
                ResetTabDragState();
                return;
            }
        }

        if (input.LeftReleased)
        {
            if (_dragMoved)
            {
                HandleDrop(_dragWindow, input.MousePosition, input.ScreenMousePosition);
            }

            ResetTabDragState();
        }
    }

    private void UpdateFloatingDrag(UiInputState input)
    {
        if (_dragWindow != null && _dragSourceHost != null)
        {
            return;
        }

        UiWindow? dragging = null;
        foreach (UiWindow window in _floatingWindows)
        {
            if (window.IsDragging)
            {
                dragging = window;
                break;
            }
        }

        if (dragging != null)
        {
            if (_floatingDragWindow != dragging)
            {
                _floatingDragWindow = dragging;
                _dragWindow = dragging;
                _dragMoved = true;
            }

            _dragPosition = input.MousePosition;
            return;
        }

        if (_floatingDragWindow != null && _dragWindow == _floatingDragWindow)
        {
            HandleDrop(_floatingDragWindow, input.MousePosition, input.ScreenMousePosition);
            _dragWindow = null;
            _floatingDragWindow = null;
            _dragMoved = false;
            _hoverHost = null;
            _hoverTarget = DockTarget.None;
            _previewBounds = default;
        }
    }

    private void UpdateHover(UiPoint mousePosition)
    {
        if (_dragWindow == null || !_dragMoved)
        {
            if (_externalPreviewWindow != null)
            {
                return;
            }

            _hoverHost = null;
            _hoverTarget = DockTarget.None;
            _previewBounds = default;
            return;
        }

        _hoverHost = null;
        foreach (UiDockHost host in _hosts)
        {
            if (host.Bounds.Contains(mousePosition))
            {
                _hoverHost = host;
                break;
            }
        }

        if (_hoverHost == null)
        {
            _hoverTarget = DockTarget.None;
            _previewBounds = GetFloatingPreviewBounds(mousePosition, _dragWindow.Bounds);
            return;
        }

        _hoverTarget = GetDockTarget(_hoverHost.Bounds, mousePosition);
        _previewBounds = GetDockPreviewBounds(_hoverHost.Bounds, _hoverTarget, _dragWindow.Bounds);
    }

    private void HandleDrop(UiWindow window, UiPoint dropPoint, UiPoint screenDropPoint)
    {
        if (_dragSourceHost != null && _hoverHost == _dragSourceHost && _hoverTarget == DockTarget.Center)
        {
            return;
        }

        bool collapseEmptyHosts = _dragSourceHost != null;
        if (_hoverHost == null)
        {
            if (_dragSourceHost != null)
            {
                _dragSourceHost.RemoveWindow(window);
                if (!Bounds.Contains(dropPoint) && CanDetachWindowExternally(_dragSourceHost, window))
                {
                    TabDetached?.Invoke(window, GetDetachPoint(screenDropPoint));
                }
                else
                {
                    window.Bounds = ClampToBounds(GetFloatingPreviewBounds(dropPoint, window.Bounds), Bounds);
                    AddFloatingWindow(window);
                }
            }
            if (collapseEmptyHosts)
            {
                CollapseEmptyHosts();
            }
            return;
        }

        if (_dragSourceHost != null)
        {
            _dragSourceHost.RemoveWindow(window);
        }

        if (_floatingWindows.Contains(window))
        {
            _floatingWindows.Remove(window);
            RemoveChild(window);
        }

        UiDockHost targetHost = _hoverHost;
        if (_hoverTarget is DockTarget.Left or DockTarget.Right or DockTarget.Top or DockTarget.Bottom)
        {
            targetHost = SplitHost(_hoverHost, _hoverTarget);
        }

        window.AllowDrag = false;
        targetHost.DockWindow(window);
        targetHost.ActivateWindow(targetHost.Windows.Count - 1);

        if (collapseEmptyHosts)
        {
            CollapseEmptyHosts();
        }
    }

    private static void CopyHostStyle(UiDockHost source, UiDockHost destination)
    {
        destination.Background = source.Background;
        destination.Border = source.Border;
        destination.TabBarColor = source.TabBarColor;
        destination.TabActiveColor = source.TabActiveColor;
        destination.TabHoverColor = source.TabHoverColor;
        destination.TabTextColor = source.TabTextColor;
        destination.TabBorderColor = source.TabBorderColor;
        destination.TabBarHeight = source.TabBarHeight;
        destination.TabWidth = source.TabWidth;
        destination.TabPadding = source.TabPadding;
        destination.TabTextScale = source.TabTextScale;
        destination.ShowCloseButtons = source.ShowCloseButtons;
        destination.CloseButtonPadding = source.CloseButtonPadding;
        destination.ScrollButtonWidth = source.ScrollButtonWidth;
        destination.ScrollStep = source.ScrollStep;
        destination.HideDockedTitleBars = source.HideDockedTitleBars;
        destination.AllowReorder = source.AllowReorder;
        destination.DragThreshold = source.DragThreshold;
    }

    private static bool CanDetachWindowExternally(UiDockHost host, UiWindow window)
    {
        if (!host.AllowDetach)
        {
            return false;
        }

        return host.CanDetachWindowPredicate?.Invoke(window) ?? true;
    }

    private void UpdateExternalPreviewHover(UiPoint hoverPoint, UiRect previewWindowBounds)
    {
        _hoverHost = null;
        foreach (UiDockHost host in _hosts)
        {
            if (host.Bounds.Contains(hoverPoint))
            {
                _hoverHost = host;
                break;
            }
        }

        if (_hoverHost == null)
        {
            _hoverTarget = DockTarget.None;
            _previewBounds = GetFloatingPreviewBounds(hoverPoint, previewWindowBounds);
            return;
        }

        if (TryGetDockTargetRect(_hoverHost.Bounds, hoverPoint, out DockTarget externalTarget, out _))
        {
            _hoverTarget = externalTarget;
            _previewBounds = GetDockPreviewBounds(_hoverHost.Bounds, _hoverTarget, previewWindowBounds);
            return;
        }

        _hoverTarget = DockTarget.None;
        _previewBounds = previewWindowBounds;
    }

    private UiPoint GetDetachPoint(UiInputState input)
    {
        return GetDetachPoint(input.ScreenMousePosition);
    }

    private UiPoint GetDetachPoint(UiPoint screenPoint)
    {
        return new UiPoint(
            screenPoint.X - _dragPointerOffsetX,
            screenPoint.Y - _dragPointerOffsetY);
    }

    private void ResetTabDragState()
    {
        _dragWindow = null;
        _dragSourceHost = null;
        _dragMoved = false;
        _dragPointerOffsetX = 0;
        _dragPointerOffsetY = 0;
        _hoverHost = null;
        _hoverTarget = DockTarget.None;
        _previewBounds = default;
    }

    private void AssignHostId(UiDockHost host)
    {
        if (!string.IsNullOrWhiteSpace(host.Id))
        {
            return;
        }

        if (host == RootHost)
        {
            host.Id = "dock-root";
            return;
        }

        host.Id = $"dock-host-{_hostIdCounter++}";
    }

    private void EnsureHostIds()
    {
        AssignHostId(RootHost);
        foreach (UiDockHost host in _hosts)
        {
            AssignHostId(host);
        }
    }

    private void ClampFloatingWindows()
    {
        if (_floatingWindows.Count == 0)
        {
            return;
        }

        UiRect workspaceBounds = Bounds;
        foreach (UiWindow window in _floatingWindows)
        {
            if (!window.ClampToParent || window.IsDragging || window.IsResizing)
            {
                continue;
            }

            window.Bounds = ClampToBounds(window.Bounds, workspaceBounds);
        }
    }

    private void CollapseEmptyHosts()
    {
        DockNode? collapsed = CollapseNode(_rootNode);
        if (collapsed == null)
        {
            EnsureRootHost();
            return;
        }

        _rootNode = collapsed;
        EnsureRootHost();
    }

    private DockNode? CollapseNode(DockNode node)
    {
        if (node.Host != null)
        {
            if (node.Host != RootHost && node.Host.IsEmpty)
            {
                RemoveDockHost(node.Host);
                return null;
            }

            return node;
        }

        if (node.First != null)
        {
            node.First = CollapseNode(node.First);
        }

        if (node.Second != null)
        {
            node.Second = CollapseNode(node.Second);
        }

        if (node.First == null && node.Second == null)
        {
            return null;
        }

        if (node.First == null)
        {
            return node.Second;
        }

        if (node.Second == null)
        {
            return node.First;
        }

        return node;
    }

    private void RemoveDockHost(UiDockHost host)
    {
        host.ClearWindows();
        _hosts.Remove(host);
        RemoveChild(host);
    }

    private void EnsureRootHost()
    {
        if (!_hosts.Contains(RootHost))
        {
            _hosts.Add(RootHost);
            AddChild(RootHost);
        }

        if (!ContainsHost(_rootNode, RootHost))
        {
            _rootNode = new DockNode(RootHost);
        }

        NormalizeRootHost();
    }

    private static bool ContainsHost(DockNode node, UiDockHost host)
    {
        if (node.Host == host)
        {
            return true;
        }

        if (node.First != null && ContainsHost(node.First, host))
        {
            return true;
        }

        if (node.Second != null && ContainsHost(node.Second, host))
        {
            return true;
        }

        return false;
    }

    private void NormalizeRootHost()
    {
        if (!RootHost.IsEmpty)
        {
            return;
        }

        UiDockHost? fallback = null;
        foreach (UiDockHost host in _hosts)
        {
            if (host == RootHost || host.IsEmpty)
            {
                continue;
            }

            if (fallback != null)
            {
                return;
            }

            fallback = host;
        }

        if (fallback == null)
        {
            return;
        }

        while (fallback.Windows.Count > 0)
        {
            UiWindow window = fallback.Windows[0];
            fallback.RemoveWindow(window);
            RootHost.DockWindow(window);
        }

        RemoveDockHost(fallback);
        _rootNode = new DockNode(RootHost);
    }

    private void DrawPreview(UiRenderContext context)
    {
        if (_previewBounds.Width <= 0 || _previewBounds.Height <= 0)
        {
            return;
        }

        context.Renderer.FillRect(_previewBounds, DragPreviewColor);
        context.Renderer.DrawRect(_previewBounds, DragPreviewOutline, 1);
    }

    private void DrawSplitters(UiRenderContext context)
    {
        DrawSplitters(context, _rootNode);
    }

    private void DrawSplitters(UiRenderContext context, DockNode node)
    {
        if (node.Host != null)
        {
            return;
        }

        if (node.First == null || node.Second == null)
        {
            return;
        }

        if (node.SplitterBounds.Width > 0 && node.SplitterBounds.Height > 0)
        {
            UiColor splitterColor = node == _dragSplitNode
                ? SplitterActiveColor
                : node == _hoverSplitNode
                    ? SplitterHoverColor
                    : SplitterColor;
            context.Renderer.FillRect(node.SplitterBounds, splitterColor);
        }

        DrawSplitters(context, node.First);
        DrawSplitters(context, node.Second);
    }

    private void DrawTargets(UiRenderContext context)
    {
        if (_hoverHost == null)
        {
            return;
        }

        foreach ((DockTarget target, UiRect rect) in GetTargetRects(_hoverHost.Bounds))
        {
            UiColor color = target == _hoverTarget ? DropTargetActiveColor : DropTargetColor;
            context.Renderer.FillRect(rect, color);
            context.Renderer.DrawRect(rect, DropTargetOutline, 1);
        }
    }

    private void LayoutNode(DockNode node, UiRect bounds)
    {
        node.Bounds = bounds;

        if (node.Host != null)
        {
            node.FirstBounds = default;
            node.SecondBounds = default;
            node.SplitterBounds = default;
            node.Host.Bounds = bounds;
            return;
        }

        if (node.First == null || node.Second == null)
        {
            node.FirstBounds = default;
            node.SecondBounds = default;
            node.SplitterBounds = default;
            return;
        }

        int splitterThickness = Math.Max(1, SplitterThickness);
        UiPoint firstMinSize = GetMinimumNodeSize(node.First);
        UiPoint secondMinSize = GetMinimumNodeSize(node.Second);

        if (node.SplitHorizontal)
        {
            int availableHeight = Math.Max(0, bounds.Height - splitterThickness);
            int desiredFirstHeight = (int)Math.Round(availableHeight * node.SplitRatio);
            int firstHeight = ClampSplitSize(desiredFirstHeight, availableHeight, firstMinSize.Y, secondMinSize.Y);
            int secondHeight = Math.Max(0, availableHeight - firstHeight);

            UiRect firstBounds = new(bounds.X, bounds.Y, bounds.Width, firstHeight);
            UiRect splitterBounds = new(bounds.X, firstBounds.Bottom, bounds.Width, splitterThickness);
            UiRect secondBounds = new(bounds.X, splitterBounds.Bottom, bounds.Width, secondHeight);

            node.FirstBounds = firstBounds;
            node.SecondBounds = secondBounds;
            node.SplitterBounds = splitterBounds;
            node.SplitRatio = availableHeight > 0 ? firstHeight / (float)availableHeight : 0.5f;

            LayoutNode(node.First, firstBounds);
            LayoutNode(node.Second, secondBounds);
        }
        else
        {
            int availableWidth = Math.Max(0, bounds.Width - splitterThickness);
            int desiredFirstWidth = (int)Math.Round(availableWidth * node.SplitRatio);
            int firstWidth = ClampSplitSize(desiredFirstWidth, availableWidth, firstMinSize.X, secondMinSize.X);
            int secondWidth = Math.Max(0, availableWidth - firstWidth);

            UiRect firstBounds = new(bounds.X, bounds.Y, firstWidth, bounds.Height);
            UiRect splitterBounds = new(firstBounds.Right, bounds.Y, splitterThickness, bounds.Height);
            UiRect secondBounds = new(splitterBounds.Right, bounds.Y, secondWidth, bounds.Height);

            node.FirstBounds = firstBounds;
            node.SecondBounds = secondBounds;
            node.SplitterBounds = splitterBounds;
            node.SplitRatio = availableWidth > 0 ? firstWidth / (float)availableWidth : 0.5f;

            LayoutNode(node.First, firstBounds);
            LayoutNode(node.Second, secondBounds);
        }
    }

    private UiPoint GetMinimumNodeSize(DockNode? node)
    {
        if (node == null)
        {
            return new UiPoint(0, 0);
        }

        if (node.Host != null)
        {
            int paneSize = Math.Max(0, MinPaneSize);
            return new UiPoint(paneSize, paneSize);
        }

        UiPoint first = GetMinimumNodeSize(node.First);
        UiPoint second = GetMinimumNodeSize(node.Second);
        int splitterThickness = Math.Max(1, SplitterThickness);

        return node.SplitHorizontal
            ? new UiPoint(Math.Max(first.X, second.X), first.Y + splitterThickness + second.Y)
            : new UiPoint(first.X + splitterThickness + second.X, Math.Max(first.Y, second.Y));
    }

    private static int ClampSplitSize(int desired, int available, int minFirst, int minSecond)
    {
        if (available <= 0)
        {
            return 0;
        }

        minFirst = Math.Max(0, minFirst);
        minSecond = Math.Max(0, minSecond);
        int totalMinimum = minFirst + minSecond;
        if (totalMinimum > available)
        {
            float scale = available / (float)totalMinimum;
            minFirst = (int)Math.Floor(minFirst * scale);
            minSecond = Math.Max(0, available - minFirst);
        }

        int maxFirst = Math.Max(minFirst, available - minSecond);
        return Math.Clamp(desired, minFirst, maxFirst);
    }

    private static DockNode? FindNode(DockNode node, UiDockHost host)
    {
        if (node.Host == host)
        {
            return node;
        }

        if (node.First != null)
        {
            DockNode? found = FindNode(node.First, host);
            if (found != null)
            {
                return found;
            }
        }

        if (node.Second != null)
        {
            DockNode? found = FindNode(node.Second, host);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private DockNode? FindSplitterNode(DockNode node, UiPoint point)
    {
        if (node.Host != null)
        {
            return null;
        }

        if (node.First != null)
        {
            DockNode? first = FindSplitterNode(node.First, point);
            if (first != null)
            {
                return first;
            }
        }

        if (node.Second != null)
        {
            DockNode? second = FindSplitterNode(node.Second, point);
            if (second != null)
            {
                return second;
            }
        }

        return node.SplitterBounds.Contains(point) ? node : null;
    }

    private DockTarget GetDockTarget(UiRect bounds, UiPoint point)
    {
        return TryGetDockTargetRect(bounds, point, out DockTarget target, out _)
            ? target
            : DockTarget.Center;
    }

    private static int GetSplitterAxisPosition(DockNode node, UiPoint point)
    {
        return node.SplitHorizontal ? point.Y : point.X;
    }

    private static int GetNodePrimarySize(UiRect bounds, bool splitHorizontal)
    {
        return splitHorizontal ? bounds.Height : bounds.Width;
    }

    private int GetMinimumNodePrimarySize(DockNode? node, bool splitHorizontal)
    {
        UiPoint minimumSize = GetMinimumNodeSize(node);
        return splitHorizontal ? minimumSize.Y : minimumSize.X;
    }

    private UiRect GetDockPreviewBounds(UiRect hostBounds, DockTarget target, UiRect windowBounds)
    {
        return target switch
        {
            DockTarget.Left => new UiRect(hostBounds.X, hostBounds.Y, hostBounds.Width / 2, hostBounds.Height),
            DockTarget.Right => new UiRect(hostBounds.X + hostBounds.Width / 2, hostBounds.Y, hostBounds.Width - hostBounds.Width / 2, hostBounds.Height),
            DockTarget.Top => new UiRect(hostBounds.X, hostBounds.Y, hostBounds.Width, hostBounds.Height / 2),
            DockTarget.Bottom => new UiRect(hostBounds.X, hostBounds.Y + hostBounds.Height / 2, hostBounds.Width, hostBounds.Height - hostBounds.Height / 2),
            DockTarget.Center => hostBounds,
            _ => GetFloatingPreviewBounds(new UiPoint(hostBounds.X, hostBounds.Y), windowBounds)
        };
    }

    private UiRect GetFloatingPreviewBounds(UiPoint point, UiRect windowBounds)
    {
        int x = point.X - windowBounds.Width / 2;
        int y = point.Y - windowBounds.Height / 2;
        return new UiRect(x, y, windowBounds.Width, windowBounds.Height);
    }

    private static UiRect ClampToBounds(UiRect bounds, UiRect container)
    {
        int maxX = container.Right - bounds.Width;
        int maxY = container.Bottom - bounds.Height;
        if (maxX < container.X)
        {
            maxX = container.X;
        }

        if (maxY < container.Y)
        {
            maxY = container.Y;
        }

        int x = Math.Clamp(bounds.X, container.X, maxX);
        int y = Math.Clamp(bounds.Y, container.Y, maxY);
        return new UiRect(x, y, bounds.Width, bounds.Height);
    }

    private IEnumerable<(DockTarget target, UiRect rect)> GetTargetRects(UiRect bounds)
    {
        int size = DropTargetSize;
        int centerX = bounds.X + bounds.Width / 2;
        int centerY = bounds.Y + bounds.Height / 2;

        UiRect center = new(centerX - size / 2, centerY - size / 2, size, size);
        UiRect left = new(centerX - size * 2, centerY - size / 2, size, size);
        UiRect right = new(centerX + size, centerY - size / 2, size, size);
        UiRect top = new(centerX - size / 2, centerY - size * 2, size, size);
        UiRect bottom = new(centerX - size / 2, centerY + size, size, size);

        yield return (DockTarget.Left, left);
        yield return (DockTarget.Right, right);
        yield return (DockTarget.Top, top);
        yield return (DockTarget.Bottom, bottom);
        yield return (DockTarget.Center, center);
    }

    private bool TryGetDockTargetRect(UiRect bounds, UiPoint point, out DockTarget target, out UiRect rect)
    {
        foreach ((DockTarget candidateTarget, UiRect candidateRect) in GetTargetRects(bounds))
        {
            if (candidateRect.Contains(point))
            {
                target = candidateTarget;
                rect = candidateRect;
                return true;
            }
        }

        target = DockTarget.None;
        rect = default;
        return false;
    }
}
