using OpenControls.State;

namespace OpenControls.Controls;

public sealed class UiDockWorkspace : UiElement
{
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
    private UiDockHost? _hoverHost;
    private DockTarget _hoverTarget;
    private UiRect _previewBounds;
    private UiWindow? _floatingDragWindow;

    public UiColor DragPreviewColor { get; set; } = new(70, 130, 200, 120);
    public UiColor DragPreviewOutline { get; set; } = new(120, 180, 220, 200);
    public UiColor DropTargetColor { get; set; } = new(50, 110, 180, 130);
    public UiColor DropTargetActiveColor { get; set; } = new(110, 180, 230, 180);
    public UiColor DropTargetOutline { get; set; } = new(160, 200, 240, 220);
    public int DropTargetSize { get; set; } = 48;
    public int DragThreshold { get; set; } = 6;

    public UiDockHost RootHost { get; }
    public IReadOnlyList<UiDockHost> DockHosts => _hosts;
    public IReadOnlyList<UiWindow> FloatingWindows => _floatingWindows;

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

        List<UiDockHost> existingHosts = new(_hosts);
        foreach (UiDockHost host in existingHosts)
        {
            if (host != RootHost && !usedHosts.Contains(host))
            {
                RemoveDockHost(host);
            }
        }

        foreach (UiDockHost host in _hosts)
        {
            host.ClearWindows();
        }

        ClearFloatingWindows();

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
                    DetachWindow(window);
                    host.DockWindow(window);
                }
            }

            host.ActivateWindow(hostState.ActiveIndex);
        }

        foreach (UiFloatingWindowState floatingState in state.FloatingWindows)
        {
            if (windowsById.TryGetValue(floatingState.WindowId, out UiWindow? window))
            {
                DetachWindow(window);
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

    private void DetachWindow(UiWindow window)
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

        if (_dragWindow != null && _dragMoved)
        {
            DrawPreview(context);
            DrawTargets(context);
        }
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

    private void UpdateTabDrag(UiInputState input)
    {
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
        }

        if (input.LeftReleased)
        {
            if (_dragMoved)
            {
                HandleDrop(_dragWindow, input.MousePosition);
            }

            _dragWindow = null;
            _dragSourceHost = null;
            _dragMoved = false;
            _hoverHost = null;
            _hoverTarget = DockTarget.None;
            _previewBounds = default;
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
            HandleDrop(_floatingDragWindow, input.MousePosition);
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

    private void HandleDrop(UiWindow window, UiPoint dropPoint)
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
                window.Bounds = ClampToBounds(GetFloatingPreviewBounds(dropPoint, window.Bounds), Bounds);
                AddFloatingWindow(window);
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
        destination.TabTextColor = source.TabTextColor;
        destination.TabBorderColor = source.TabBorderColor;
        destination.TabBarHeight = source.TabBarHeight;
        destination.TabWidth = source.TabWidth;
        destination.TabPadding = source.TabPadding;
        destination.TabTextScale = source.TabTextScale;
        destination.HideDockedTitleBars = source.HideDockedTitleBars;
        destination.AllowReorder = source.AllowReorder;
        destination.DragThreshold = source.DragThreshold;
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
        if (node.Host != null)
        {
            node.Host.Bounds = bounds;
            return;
        }

        if (node.First == null || node.Second == null)
        {
            return;
        }

        if (node.SplitHorizontal)
        {
            int firstHeight = (int)(bounds.Height * node.SplitRatio);
            UiRect firstBounds = new(bounds.X, bounds.Y, bounds.Width, firstHeight);
            UiRect secondBounds = new(bounds.X, bounds.Y + firstHeight, bounds.Width, bounds.Height - firstHeight);
            LayoutNode(node.First, firstBounds);
            LayoutNode(node.Second, secondBounds);
        }
        else
        {
            int firstWidth = (int)(bounds.Width * node.SplitRatio);
            UiRect firstBounds = new(bounds.X, bounds.Y, firstWidth, bounds.Height);
            UiRect secondBounds = new(bounds.X + firstWidth, bounds.Y, bounds.Width - firstWidth, bounds.Height);
            LayoutNode(node.First, firstBounds);
            LayoutNode(node.Second, secondBounds);
        }
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

    private DockTarget GetDockTarget(UiRect bounds, UiPoint point)
    {
        foreach ((DockTarget target, UiRect rect) in GetTargetRects(bounds))
        {
            if (rect.Contains(point))
            {
                return target;
            }
        }

        return DockTarget.Center;
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
}
