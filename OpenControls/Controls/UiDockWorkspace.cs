using OpenControls.State;

namespace OpenControls.Controls;

public sealed class UiDockWorkspace : UiElement
{
    public event Action<UiWindow, UiPoint>? TabDetached;
    public event Action<string>? TearOffTelemetry;
    public Func<UiWindow, UiDockHost, DockTarget, bool>? CanDockWindowPredicate { get; set; }

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
        public bool IsCollapsed { get; set; }
        public UiDockCollapseEdge CollapseEdge { get; set; } = UiDockCollapseEdge.Right;
        public UiRect Bounds { get; set; }
        public UiRect FirstBounds { get; set; }
        public UiRect SecondBounds { get; set; }
        public UiRect SplitterBounds { get; set; }
    }

    private sealed class ExternalGroupLeaseState
    {
        public required UiDockHost SourceHost { get; init; }
        public required UiWindow[] Windows { get; init; }
        public required ExternalGroupTabPlacement[] Placements { get; init; }
        public required UiWindow GroupActiveWindow { get; init; }
        public required UiWindow SourceActiveWindow { get; init; }
        public required int HostListIndex { get; init; }
        public required bool CompleteSourceHost { get; init; }
        public DockNode? SurvivingSibling { get; init; }
        public bool SourceWasFirst { get; init; }
        public bool SplitHorizontal { get; init; }
        public float SplitRatio { get; init; }
        public bool WasCollapsed { get; init; }
        public bool SiblingWasCollapsed { get; init; }
    }

    private readonly record struct ExternalGroupTabPlacement(
        UiWindow Window,
        int OriginalIndex,
        UiWindow? PreviousStableWindow,
        UiWindow? NextStableWindow);

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
    private bool _dragOutsideWorkspaceTelemetryEmitted;
    private UiDockHost? _hoverHost;
    private DockTarget _hoverTarget;
    private UiRect _previewBounds;
    private string? _lastTelemetryHoverHostId;
    private DockTarget _lastTelemetryHoverTarget;
    private UiWindow? _floatingDragWindow;
    private UiWindow? _externalPreviewWindow;
    private UiPoint _externalPreviewHoverPoint;
    private UiRect _externalPreviewWindowBounds;
    private DockNode? _hoverSplitNode;
    private DockNode? _dragSplitNode;
    private int _dragSplitStartAxis;
    private int _dragSplitStartPrimarySize;
    private DockNode? _hoverCollapsedNode;
    private bool _suppressHostMutationCallbacks;
    private bool _restoreValidationActive;
    private long _topologyMutationVersion;
    private readonly Dictionary<long, ExternalGroupLeaseState> _externalGroupLeases = new();
    private long _externalGroupLeaseId;

    public UiColor DragPreviewColor { get; set; } = new(70, 130, 200, 120);
    public UiColor DragPreviewOutline { get; set; } = new(120, 180, 220, 200);
    public UiColor DropTargetColor { get; set; } = new(50, 110, 180, 130);
    public UiColor DropTargetActiveColor { get; set; } = new(110, 180, 230, 180);
    public UiColor DropTargetOutline { get; set; } = new(160, 200, 240, 220);
    public int DropTargetSize { get; set; } = 48;
    public int DragThreshold { get; set; } = 6;
    public int SplitterThickness { get; set; } = 6;
    public int SplitterVisualThickness { get; set; } = 2;
    public int SplitterVisualInset { get; set; } = 2;
    public int MinPaneSize { get; set; } = 80;
    /// <summary>
    /// When enabled, splitters honor the largest minimum size requested by any
    /// window tabbed into each dock host, in addition to <see cref="MinPaneSize"/>.
    /// </summary>
    public bool RespectDockedWindowMinimums { get; set; }
    public UiColor SplitterColor { get; set; } = new(44, 52, 68);
    public UiColor SplitterHoverColor { get; set; } = new(68, 82, 106);
    public UiColor SplitterActiveColor { get; set; } = new(96, 120, 154);
    public UiColor SplitterTrackHoverColor { get; set; } = new(68, 82, 106, 32);
    public UiColor SplitterTrackActiveColor { get; set; } = new(96, 120, 154, 44);
    public UiColor CollapsedStripColor { get; set; } = new(22, 26, 36);
    public UiColor CollapsedStripHoverColor { get; set; } = new(32, 36, 48);
    public UiColor CollapsedStripBorderColor { get; set; } = new(60, 70, 90);
    public UiColor CollapsedStripGlyphColor { get; set; } = UiColor.White;
    public int CollapsedStripSize { get; set; } = 28;

    public UiDockHost RootHost { get; }
    public IReadOnlyList<UiDockHost> DockHosts => _hosts;
    public IReadOnlyList<UiWindow> FloatingWindows => _floatingWindows;
    public override bool CapturesPointerInput => _hoverSplitNode != null || _dragSplitNode != null;

    public UiDockWorkspace()
    {
        RootHost = CreateHost();
        RootHost.AllowCollapse = false;
        AssignHostId(RootHost);
        _rootNode = new DockNode(RootHost);
    }

    public bool IsCollapseRegionCollapsed(UiDockHost memberHost)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        DockNode? node = FindNode(_rootNode, memberHost);
        return node != null && FindCollapsedAncestor(_rootNode, node) != null;
    }

    public bool CanCollapseRegion(UiDockHost memberHost)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        if (ReferenceEquals(memberHost, RootHost)
            || !_hosts.Contains(memberHost)
            || IsCollapseRegionCollapsed(memberHost))
        {
            return false;
        }

        DockNode? collapseNode = ResolveCollapseNode(_rootNode, memberHost);
        return collapseNode != null && CanCollapseNode(collapseNode);
    }

    public bool SetCollapseRegionCollapsed(UiDockHost memberHost, bool collapsed)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        ThrowIfRestoreValidationMutation("collapse dock regions");
        if (!_hosts.Contains(memberHost))
        {
            throw new ArgumentException("Dock host is not part of this workspace.", nameof(memberHost));
        }

        if (ReferenceEquals(memberHost, RootHost))
        {
            return false;
        }

        DockNode? hostNode = FindNode(_rootNode, memberHost);
        if (hostNode == null)
        {
            return false;
        }

        if (!collapsed)
        {
            DockNode? collapsedNode = FindCollapsedAncestor(_rootNode, hostNode);
            if (collapsedNode == null)
            {
                return false;
            }

            CancelDockInteractionsForTopologyChange();
            collapsedNode.IsCollapsed = false;
            _topologyMutationVersion++;
            _hoverCollapsedNode = null;
            Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
            return true;
        }

        if (!CanCollapseRegion(memberHost))
        {
            return false;
        }

        DockNode? collapseNode = ResolveCollapseNode(_rootNode, memberHost);
        if (collapseNode == null || collapseNode.IsCollapsed)
        {
            return false;
        }

        CancelDockInteractionsForTopologyChange();
        collapseNode.IsCollapsed = true;
        _topologyMutationVersion++;
        _hoverCollapsedNode = null;
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
        return true;
    }

    public bool ToggleCollapseRegion(UiDockHost memberHost)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        return SetCollapseRegionCollapsed(memberHost, !IsCollapseRegionCollapsed(memberHost));
    }

    public UiRect GetCollapseRegionBounds(UiDockHost memberHost)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        DockNode? hostNode = FindNode(_rootNode, memberHost);
        DockNode? collapsedNode = hostNode == null ? null : FindCollapsedAncestor(_rootNode, hostNode);
        return collapsedNode?.Bounds ?? default;
    }

    public UiRect GetCollapseRegionRestoreBounds(UiDockHost memberHost)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        DockNode? hostNode = FindNode(_rootNode, memberHost);
        DockNode? collapsedNode = hostNode == null ? null : FindCollapsedAncestor(_rootNode, hostNode);
        return collapsedNode == null ? default : GetCollapsedRestoreBounds(collapsedNode);
    }

    public UiDockHost? GetCollapseRegionRepresentative(UiDockHost memberHost)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        DockNode? region = ResolveCollapseNode(_rootNode, memberHost);
        return region == null ? null : EnumerateHosts(region).FirstOrDefault();
    }

    public IReadOnlyList<UiDockHost> GetCollapseRegionMembers(UiDockHost memberHost)
    {
        ArgumentNullException.ThrowIfNull(memberHost);
        DockNode? region = ResolveCollapseNode(_rootNode, memberHost);
        return region == null ? Array.Empty<UiDockHost>() : EnumerateHosts(region).ToArray();
    }

    public UiDockHost SplitHost(UiDockHost host, DockTarget target)
    {
        return SplitHost(host, target, 0.5f);
    }

    public UiDockHost SplitHost(UiDockHost host, DockTarget target, float splitRatio)
    {
        ArgumentNullException.ThrowIfNull(host);
        ThrowIfRestoreValidationMutation("split dock hosts");
        if (!float.IsFinite(splitRatio))
        {
            throw new ArgumentOutOfRangeException(nameof(splitRatio), splitRatio, "Split ratio must be finite.");
        }

        if (target is DockTarget.Center or DockTarget.None)
        {
            return host;
        }

        if (IsCollapseRegionCollapsed(host))
        {
            SetCollapseRegionCollapsed(host, collapsed: false);
        }

        CancelDockInteractionsForTopologyChange();

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
        node.SplitRatio = Math.Clamp(splitRatio, 0.05f, 0.95f);
        _topologyMutationVersion++;

        TraceTearOffTelemetry(
            $"split-host sourceHost={FormatHost(host)} newHost={FormatHost(newHost)} target='{target}'");

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
        ThrowIfRestoreValidationMutation("apply dock workspace state");
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

        ValidateNodeState(state.Root);
        EnsureHostIds();

        Dictionary<string, UiDockHost> hostById = new(StringComparer.Ordinal);
        foreach (UiDockHost host in _hosts)
        {
            if (!string.IsNullOrWhiteSpace(host.Id))
            {
                hostById[host.Id] = host;
            }
        }

        HashSet<UiDockHost> originalHosts = new(_hosts);
        Dictionary<UiDockHost, string> originalHostIds = _hosts.ToDictionary(host => host, host => host.Id);
        HashSet<UiDockHost> usedHosts = new();
        DockNode proposedRoot;
        try
        {
            proposedRoot = BuildNode(state.Root, hostById, usedHosts);
            if (!usedHosts.Contains(RootHost))
            {
                throw new ArgumentException($"Dock state must include root host '{RootHost.Id}'.", nameof(state));
            }

            long validationVersion = _topologyMutationVersion;
            UiDockHost[] validationHosts = _hosts.ToArray();
            Dictionary<UiDockHost, string> validationHostIds = _hosts.ToDictionary(host => host, host => host.Id);
            DockNode liveTopology = CloneDockNode(_rootNode);
            _restoreValidationActive = true;
            try
            {
                ValidateRestoredWindowAssignments(state, windowsById, hostById, usedHosts);
                ValidateRestoredCollapsedBranches(state, proposedRoot, windowsById, hostById);
            }
            finally
            {
                _restoreValidationActive = false;
            }

            ValidateLiveRestorePreCommit(
                state,
                windowsById,
                hostById,
                usedHosts,
                proposedRoot,
                liveTopology,
                validationHosts,
                validationHostIds,
                validationVersion);
        }
        catch
        {
            _restoreValidationActive = false;
            foreach (UiDockHost host in _hosts.ToArray())
            {
                if (!originalHosts.Contains(host))
                {
                    RemoveDockHost(host);
                }
            }

            foreach ((UiDockHost host, string hostId) in originalHostIds)
            {
                host.Id = hostId;
            }

            throw;
        }

        CancelDockInteractionsForTopologyChange();
        bool previousSuppression = _suppressHostMutationCallbacks;
        _suppressHostMutationCallbacks = true;
        try
        {
            _rootNode = proposedRoot;
            _topologyMutationVersion++;

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
                        PrepareDockedWindow(window, host);
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
        finally
        {
            _suppressHostMutationCallbacks = previousSuppression;
        }
    }

    public void AddFloatingWindow(UiWindow window)
    {
        ThrowIfRestoreValidationMutation("add floating windows");
        if (_floatingWindows.Contains(window))
        {
            return;
        }

        window.ShowTitleBar = true;
        window.AllowDrag = true;
        window.AllowResize = true;
        window.ShowResizeGrip = true;
        AddChild(window);
        _floatingWindows.Add(window);
    }

    public void DockWindow(UiWindow window, UiDockHost host, int index = -1)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(host);
        ThrowIfRestoreValidationMutation("dock windows");

        if (!_hosts.Contains(host))
        {
            throw new ArgumentException("Dock host is not part of this workspace.", nameof(host));
        }

        if (!CanDockWindow(window, host, DockTarget.Center))
        {
            throw new InvalidOperationException($"Window '{window.Id}' cannot dock in host '{host.Id}'.");
        }

        if (!_hosts.Contains(host) || !CanRestoreWindow(window))
        {
            throw new InvalidOperationException($"Dock policy invalidated host '{host.Id}' or window '{window.Id}'.");
        }

        DetachWindowInternal(window);
        PrepareDockedWindow(window, host);
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
        ThrowIfRestoreValidationMutation("detach windows");

        DetachWindowInternal(window);
        CollapseEmptyHosts();
    }

    /// <summary>
    /// Atomically detaches a non-empty tab group from one dock host and returns
    /// an opaque, single-use lease that can restore the group to the same
    /// topology and tab positions.
    /// </summary>
    /// <remarks>
    /// All supplied windows must belong to one live dock host. Their source tab
    /// order, stable neighboring tabs, original indices, and active tab are
    /// captured from the host. All detach policies are evaluated before the
    /// first window is removed.
    /// </remarks>
    public UiDockExternalGroupLease BeginExternalDockGroup(IReadOnlyList<UiWindow> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);
        ThrowIfRestoreValidationMutation("begin external dock groups");

        UiWindow[] requestedWindows = ValidateExternalGroupMembership(windows, nameof(windows));
        if (requestedWindows[0].Parent is not UiDockHost sourceHost || !_hosts.Contains(sourceHost))
        {
            throw new InvalidOperationException("External dock groups must start in a live workspace dock host.");
        }

        UiWindow[] sourceWindows = sourceHost.Windows.ToArray();
        HashSet<UiWindow> requestedWindowSet = new(requestedWindows);
        if (requestedWindows.Any(window => !ReferenceEquals(window.Parent, sourceHost))
            || requestedWindows.Any(window => !sourceWindows.Contains(window)))
        {
            throw new ArgumentException(
                "External dock groups must contain windows from exactly one source host.",
                nameof(windows));
        }

        UiWindow[] leasedWindows = sourceWindows.Where(requestedWindowSet.Contains).ToArray();
        UiWindow sourceActiveWindow = sourceHost.ActiveWindow ?? sourceWindows[0];
        UiWindow groupActiveWindow = requestedWindowSet.Contains(sourceActiveWindow)
            ? sourceActiveWindow
            : leasedWindows[0];
        ExternalGroupTabPlacement[] placements = CaptureExternalGroupTabPlacements(
            sourceWindows,
            requestedWindowSet);
        bool completeSourceHost = leasedWindows.Length == sourceWindows.Length;
        DockNode sourceNode = FindNode(_rootNode, sourceHost)
            ?? throw new InvalidOperationException("The external dock group's source host is not in the dock topology.");
        TryFindDockNodeParent(_rootNode, sourceNode, out DockNode? sourceParent, out bool sourceWasFirst);
        DockNode? survivingSibling = sourceParent == null
            ? null
            : sourceWasFirst ? sourceParent.Second : sourceParent.First;

        int sourceHostIndex = _hosts.IndexOf(sourceHost);
        long validationVersion = _topologyMutationVersion;
        _restoreValidationActive = true;
        try
        {
            foreach (UiWindow window in leasedWindows)
            {
                if (!EvaluateExternalDetach(sourceHost, window).Allowed)
                {
                    throw new InvalidOperationException(
                        $"Window '{window.Id}' is not eligible for external detachment from host '{sourceHost.Id}'.");
                }
            }
        }
        finally
        {
            _restoreValidationActive = false;
        }

        if (_topologyMutationVersion != validationVersion
            || sourceHostIndex < 0
            || sourceHostIndex >= _hosts.Count
            || !ReferenceEquals(_hosts[sourceHostIndex], sourceHost)
            || !WindowSequenceMatches(sourceHost.Windows, sourceWindows)
            || !ReferenceEquals(sourceHost.ActiveWindow, sourceActiveWindow)
            || !ReferenceEquals(FindNode(_rootNode, sourceHost), sourceNode))
        {
            throw new InvalidOperationException(
                "Dock group membership or topology changed while detach policy was being validated.");
        }

        ExternalGroupLeaseState leaseState = new()
        {
            SourceHost = sourceHost,
            Windows = leasedWindows,
            Placements = placements,
            GroupActiveWindow = groupActiveWindow,
            SourceActiveWindow = sourceActiveWindow,
            HostListIndex = sourceHostIndex,
            CompleteSourceHost = completeSourceHost,
            SurvivingSibling = survivingSibling,
            SourceWasFirst = sourceWasFirst,
            SplitHorizontal = sourceParent?.SplitHorizontal ?? false,
            SplitRatio = sourceParent?.SplitRatio ?? 0.5f,
            WasCollapsed = sourceParent?.IsCollapsed ?? false,
            SiblingWasCollapsed = survivingSibling?.IsCollapsed ?? false
        };

        CancelDockInteractionsForTopologyChange();
        bool previousSuppression = _suppressHostMutationCallbacks;
        _suppressHostMutationCallbacks = true;
        try
        {
            foreach (UiWindow window in leasedWindows)
            {
                if (!RemoveWindowForWorkspaceMutation(sourceHost, window))
                {
                    throw new InvalidOperationException(
                        $"Window '{window.Id}' left its source host during external group detachment.");
                }
            }

            CollapseEmptyHosts();
        }
        finally
        {
            _suppressHostMutationCallbacks = previousSuppression;
        }

        long leaseId = ++_externalGroupLeaseId;
        _externalGroupLeases.Add(leaseId, leaseState);
        return new UiDockExternalGroupLease(this, leaseId);
    }

    /// <summary>
    /// Restores an external group in its captured tab order with its captured
    /// active tab.
    /// </summary>
    public bool RestoreExternalDockGroup(UiDockExternalGroupLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ThrowIfRestoreValidationMutation("restore external dock groups");
        ExternalGroupLeaseState state = GetExternalDockGroupLeaseState(lease, out long leaseId);
        return RestoreExternalDockGroupCore(
            leaseId,
            state,
            state.Windows,
            state.SourceActiveWindow,
            requireActiveWindowInGroup: false);
    }

    /// <summary>
    /// Restores an external group to the exact source topology and selects the
    /// supplied active tab. Complete-host groups apply the supplied current tab
    /// order; subsets return to their captured source order and slots.
    /// </summary>
    /// <remarks>
    /// Invalid membership throws before mutation. A docking-policy rejection or
    /// unavailable structural anchor returns <see langword="false"/> and leaves
    /// both the workspace and external host unchanged. Successful restoration
    /// consumes the lease.
    /// </remarks>
    public bool RestoreExternalDockGroup(
        UiDockExternalGroupLease lease,
        IReadOnlyList<UiWindow> windows,
        UiWindow? activeWindow = null)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(windows);
        ThrowIfRestoreValidationMutation("restore external dock groups");

        ExternalGroupLeaseState state = GetExternalDockGroupLeaseState(lease, out long leaseId);
        UiWindow[] orderedWindows = ValidateExternalGroupMembership(windows, nameof(windows));
        if (orderedWindows.Length != state.Windows.Length
            || !new HashSet<UiWindow>(orderedWindows).SetEquals(state.Windows))
        {
            throw new ArgumentException(
                "Restored external dock groups must contain exactly the leased windows.",
                nameof(windows));
        }

        UiWindow desiredActiveWindow = activeWindow ?? state.GroupActiveWindow;
        if (!orderedWindows.Contains(desiredActiveWindow))
        {
            throw new ArgumentException("The active window must belong to the leased external group.", nameof(activeWindow));
        }

        return RestoreExternalDockGroupCore(
            leaseId,
            state,
            orderedWindows,
            desiredActiveWindow,
            requireActiveWindowInGroup: true);
    }

    private bool RestoreExternalDockGroupCore(
        long leaseId,
        ExternalGroupLeaseState state,
        IReadOnlyList<UiWindow> requestedOrder,
        UiWindow desiredActiveWindow,
        bool requireActiveWindowInGroup)
    {
        UiWindow[] orderedWindows = requestedOrder.ToArray();

        if (orderedWindows.Any(window =>
                ReferenceEquals(window.Parent, this)
                || window.Parent is UiDockHost host && _hosts.Contains(host)))
        {
            return false;
        }

        bool sourceHostIsLive = _hosts.Contains(state.SourceHost);
        DockNode? sourceNode = sourceHostIsLive ? FindNode(_rootNode, state.SourceHost) : null;
        if (!state.CompleteSourceHost)
        {
            if (!sourceHostIsLive
                || sourceNode == null
                || state.Windows.Any(window => ReferenceEquals(window.Parent, state.SourceHost)))
            {
                return false;
            }
        }
        else if (sourceHostIsLive)
        {
            if (sourceNode == null || !state.SourceHost.IsEmpty)
            {
                return false;
            }
        }
        else
        {
            if (state.SurvivingSibling == null
                || state.SourceHost.Parent != null
                || !state.SourceHost.IsEmpty
                || _hosts.Any(host => string.Equals(host.Id, state.SourceHost.Id, StringComparison.Ordinal))
                || !ContainsDockNodeReference(_rootNode, state.SurvivingSibling))
            {
                return false;
            }
        }

        if (!requireActiveWindowInGroup
            && !state.Windows.Contains(desiredActiveWindow)
            && !ReferenceEquals(desiredActiveWindow.Parent, state.SourceHost))
        {
            return false;
        }

        Dictionary<UiWindow, UiElement?> originalParents = orderedWindows.ToDictionary(window => window, window => window.Parent);
        Dictionary<UiDockHost, UiWindow[]> externalHostWindows = orderedWindows
            .Select(window => window.Parent)
            .OfType<UiDockHost>()
            .Where(host => !_hosts.Contains(host))
            .Distinct()
            .ToDictionary(host => host, host => host.Windows.ToArray());
        Dictionary<UiDockHost, UiWindow?> externalHostActiveWindows = externalHostWindows.Keys
            .ToDictionary(host => host, host => host.ActiveWindow);
        UiWindow[] sourceHostWindows = sourceHostIsLive ? state.SourceHost.Windows.ToArray() : [];
        UiWindow? sourceHostActiveWindow = sourceHostIsLive ? state.SourceHost.ActiveWindow : null;
        long validationVersion = _topologyMutationVersion;
        _restoreValidationActive = true;
        bool canRestore = true;
        try
        {
            foreach (UiWindow window in orderedWindows)
            {
                if (!CanDockWindow(window, state.SourceHost, DockTarget.Center))
                {
                    canRestore = false;
                }
            }
        }
        finally
        {
            _restoreValidationActive = false;
        }

        bool validationStable = _topologyMutationVersion == validationVersion
            && orderedWindows.All(window => ReferenceEquals(window.Parent, originalParents[window]))
            && externalHostWindows.All(pair => WindowSequenceMatches(pair.Key.Windows, pair.Value))
            && externalHostActiveWindows.All(pair => ReferenceEquals(pair.Key.ActiveWindow, pair.Value));
        if (sourceHostIsLive)
        {
            validationStable = validationStable
                && _hosts.Contains(state.SourceHost)
                && WindowSequenceMatches(state.SourceHost.Windows, sourceHostWindows)
                && ReferenceEquals(state.SourceHost.ActiveWindow, sourceHostActiveWindow)
                && ReferenceEquals(FindNode(_rootNode, state.SourceHost), sourceNode);
        }
        else
        {
            validationStable = validationStable
                && !_hosts.Contains(state.SourceHost)
                && state.SourceHost.Parent == null
                && state.SourceHost.IsEmpty
                && state.SurvivingSibling != null
                && ContainsDockNodeReference(_rootNode, state.SurvivingSibling);
        }

        if (!validationStable)
        {
            throw new InvalidOperationException(
                "Dock group membership or topology changed while restore policy was being validated.");
        }

        if (!canRestore)
        {
            return false;
        }

        CancelDockInteractionsForTopologyChange();
        bool previousSuppression = _suppressHostMutationCallbacks;
        _suppressHostMutationCallbacks = true;
        try
        {
            if (!sourceHostIsLive)
            {
                AttachExistingDockHost(state.SourceHost, state.HostListIndex);
                DockNode sourceLeaf = new(state.SourceHost);
                DockNode sibling = state.SurvivingSibling!;
                sibling.IsCollapsed = state.SiblingWasCollapsed;
                DockNode wrapper = new(null)
                {
                    First = state.SourceWasFirst ? sourceLeaf : sibling,
                    Second = state.SourceWasFirst ? sibling : sourceLeaf,
                    SplitHorizontal = state.SplitHorizontal,
                    SplitRatio = state.SplitRatio,
                    IsCollapsed = state.WasCollapsed
                };
                if (!ReplaceDockNodeReference(sibling, wrapper))
                {
                    throw new InvalidOperationException("The external dock group's structural return anchor is no longer live.");
                }

                _topologyMutationVersion++;
            }

            if (state.CompleteSourceHost)
            {
                foreach (UiWindow window in orderedWindows)
                {
                    RemoveWindowFromCurrentParent(window);
                    PrepareDockedWindow(window, state.SourceHost);
                    state.SourceHost.DockWindow(window);
                }
            }
            else
            {
                RestoreExternalGroupSubset(state);
            }

            state.SourceHost.ActivateWindow(Array.IndexOf(state.SourceHost.Windows.ToArray(), desiredActiveWindow));
            _externalGroupLeases.Remove(leaseId);
            Arrange();
            return true;
        }
        finally
        {
            _suppressHostMutationCallbacks = previousSuppression;
        }
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
        return CommitExternalDockGroup(new[] { window }, window, window);
    }

    public bool CommitExternalDockGroup(IReadOnlyList<UiWindow> windows, UiWindow previewWindow, UiWindow? activeWindow = null)
    {
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(previewWindow);
        ThrowIfRestoreValidationMutation("commit external docking");

        if (windows.Count == 0)
        {
            throw new ArgumentException("External dock groups cannot be empty.", nameof(windows));
        }

        UiWindow[] groupWindows = new UiWindow[windows.Count];
        HashSet<UiWindow> uniqueWindows = new();
        for (int index = 0; index < windows.Count; index++)
        {
            UiWindow window = windows[index]
                ?? throw new ArgumentNullException(nameof(windows), "External dock groups cannot contain null windows.");
            if (!uniqueWindows.Add(window))
            {
                throw new ArgumentException("External dock groups cannot contain duplicate windows.", nameof(windows));
            }

            groupWindows[index] = window;
        }

        if (!uniqueWindows.Contains(previewWindow))
        {
            throw new ArgumentException("The preview window must belong to the external dock group.", nameof(previewWindow));
        }

        if (activeWindow != null && !uniqueWindows.Contains(activeWindow))
        {
            throw new ArgumentException("The active window must belong to the external dock group.", nameof(activeWindow));
        }

        if (!ReferenceEquals(_externalPreviewWindow, previewWindow))
        {
            return false;
        }

        UiDockHost? hoverHost = _hoverHost;
        DockTarget hoverTarget = _hoverTarget;
        ClearExternalDockPreview(previewWindow);
        if (hoverHost == null || hoverTarget == DockTarget.None)
        {
            return false;
        }

        for (int index = 0; index < groupWindows.Length; index++)
        {
            UiWindow window = groupWindows[index];
            if (!CanDockWindow(window, hoverHost, hoverTarget) || !_hosts.Contains(hoverHost))
            {
                return false;
            }
        }

        UiDockHost targetHost = hoverHost;
        if (hoverTarget is DockTarget.Left or DockTarget.Right or DockTarget.Top or DockTarget.Bottom)
        {
            targetHost = SplitHost(hoverHost, hoverTarget);
        }

        foreach (UiWindow window in groupWindows)
        {
            if (!CanDockWindow(window, targetHost, DockTarget.Center) || !_hosts.Contains(targetHost))
            {
                CollapseEmptyHosts();
                return false;
            }
        }

        UiWindow desiredActiveWindow = activeWindow ?? previewWindow;
        int activeIndex = -1;
        for (int index = 0; index < groupWindows.Length; index++)
        {
            UiWindow window = groupWindows[index];
            DetachWindowInternal(window);
            RemoveWindowFromCurrentParent(window);
            PrepareDockedWindow(window, targetHost);
            targetHost.DockWindow(window);
            if (ReferenceEquals(window, desiredActiveWindow))
            {
                activeIndex = targetHost.Windows.Count - 1;
            }
        }

        targetHost.ActivateWindow(activeIndex >= 0 ? activeIndex : targetHost.Windows.Count - 1);
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
        ThrowIfRestoreValidationMutation("reset dock layout");
        CancelDockInteractionsForTopologyChange();
        bool previousSuppression = _suppressHostMutationCallbacks;
        _suppressHostMutationCallbacks = true;
        try
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
            _topologyMutationVersion++;
            EnsureRootHost();
        }
        finally
        {
            _suppressHostMutationCallbacks = previousSuppression;
        }
    }

    /// <summary>
    /// Arranges dock hosts and their windows immediately without processing input.
    /// Call this after assigning <see cref="UiElement.Bounds"/> when child content
    /// must be laid out before the next UI update pass.
    /// </summary>
    public void Arrange()
    {
        LayoutNode(_rootNode, Bounds, UiDockCollapseEdge.Right);
        UiDockHost[] hosts = _hosts.ToArray();
        foreach (UiDockHost host in hosts)
        {
            if (_hosts.Contains(host))
            {
                host.ArrangeDockedWindows();
            }
        }

        ClampFloatingWindows();
        UiWindow[] floatingWindows = _floatingWindows.ToArray();
        foreach (UiWindow window in floatingWindows)
        {
            if (_floatingWindows.Contains(window))
            {
                window.ArrangeContent();
            }
        }
    }

    private void ClearFloatingWindows()
    {
        for (int i = _floatingWindows.Count - 1; i >= 0; i--)
        {
            RemoveChild(_floatingWindows[i]);
        }

        _floatingWindows.Clear();
    }

    private static void RemoveWindowFromCurrentParent(UiWindow window)
    {
        if (window.Parent is UiDockHost currentHost)
        {
            currentHost.RemoveWindow(window);
            return;
        }

        window.Parent?.RemoveChild(window);
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
            if (RemoveWindowForWorkspaceMutation(host, window))
            {
                return;
            }
        }
    }

    private bool RemoveWindowForWorkspaceMutation(UiDockHost host, UiWindow window)
    {
        bool previousSuppression = _suppressHostMutationCallbacks;
        _suppressHostMutationCallbacks = true;
        try
        {
            return host.RemoveWindow(window);
        }
        finally
        {
            _suppressHostMutationCallbacks = previousSuppression;
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        Arrange();

        UiInputState input = context.Input;
        bool collapseInputHandled = HandleCollapseInputBeforeChildren(input, context.Focus);
        if (collapseInputHandled)
        {
            Arrange();
            input = SuppressInputForFrame(input);
            context = new UiUpdateContext(
                input,
                context.Focus,
                context.DragDrop,
                context.DeltaSeconds,
                context.DefaultFont,
                context.Clipboard,
                context.ActiveInputLayer);
        }

        UpdateSplitters(input, context.Focus);
        Arrange();
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
        DrawCollapsedStrips(context, _rootNode);

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

        host.ExternalDragHandling = template?.ExternalDragHandling ?? true;
        host.AllowDetach = template?.AllowDetach ?? false;
        host.CanDetachWindowPredicate = template?.CanDetachWindowPredicate;
        host.TabCloseCompleted += HandleHostTabCloseCompleted;
        host.WindowsAdding += HandleHostWindowsAdding;
        host.WindowsMutating += HandleHostWindowsMutating;
        host.WindowsMutated += HandleHostWindowsMutated;
        AssignHostId(host);

        _hosts.Add(host);
        AddChild(host);
        _topologyMutationVersion++;
        return host;
    }

    private void AttachExistingDockHost(UiDockHost host, int preferredIndex)
    {
        if (_hosts.Contains(host) || host.Parent != null)
        {
            throw new InvalidOperationException($"Dock host '{host.Id}' is already attached.");
        }

        host.TabCloseCompleted += HandleHostTabCloseCompleted;
        host.WindowsAdding += HandleHostWindowsAdding;
        host.WindowsMutating += HandleHostWindowsMutating;
        host.WindowsMutated += HandleHostWindowsMutated;
        int index = Math.Clamp(preferredIndex, 0, _hosts.Count);
        _hosts.Insert(index, host);
        AddChild(host);
        _topologyMutationVersion++;
    }

    private static UiWindow[] ValidateExternalGroupMembership(IReadOnlyList<UiWindow> windows, string parameterName)
    {
        if (windows.Count == 0)
        {
            throw new ArgumentException("External dock groups cannot be empty.", parameterName);
        }

        UiWindow[] result = new UiWindow[windows.Count];
        HashSet<UiWindow> uniqueWindows = new();
        for (int index = 0; index < windows.Count; index++)
        {
            UiWindow window = windows[index]
                ?? throw new ArgumentNullException(parameterName, "External dock groups cannot contain null windows.");
            if (!uniqueWindows.Add(window))
            {
                throw new ArgumentException("External dock groups cannot contain duplicate windows.", parameterName);
            }

            result[index] = window;
        }

        return result;
    }

    private static ExternalGroupTabPlacement[] CaptureExternalGroupTabPlacements(
        IReadOnlyList<UiWindow> sourceWindows,
        IReadOnlySet<UiWindow> leasedWindows)
    {
        List<ExternalGroupTabPlacement> placements = new(leasedWindows.Count);
        for (int index = 0; index < sourceWindows.Count; index++)
        {
            UiWindow window = sourceWindows[index];
            if (!leasedWindows.Contains(window))
            {
                continue;
            }

            UiWindow? previousStableWindow = null;
            for (int previousIndex = index - 1; previousIndex >= 0; previousIndex--)
            {
                if (!leasedWindows.Contains(sourceWindows[previousIndex]))
                {
                    previousStableWindow = sourceWindows[previousIndex];
                    break;
                }
            }

            UiWindow? nextStableWindow = null;
            for (int nextIndex = index + 1; nextIndex < sourceWindows.Count; nextIndex++)
            {
                if (!leasedWindows.Contains(sourceWindows[nextIndex]))
                {
                    nextStableWindow = sourceWindows[nextIndex];
                    break;
                }
            }

            placements.Add(new ExternalGroupTabPlacement(
                window,
                index,
                previousStableWindow,
                nextStableWindow));
        }

        return placements.ToArray();
    }

    private static void RestoreExternalGroupSubset(ExternalGroupLeaseState state)
    {
        foreach (UiWindow window in state.Windows)
        {
            RemoveWindowFromCurrentParent(window);
        }

        int runStart = 0;
        while (runStart < state.Placements.Length)
        {
            int runEnd = runStart + 1;
            while (runEnd < state.Placements.Length
                && state.Placements[runEnd].OriginalIndex == state.Placements[runEnd - 1].OriginalIndex + 1)
            {
                runEnd++;
            }

            ExternalGroupTabPlacement first = state.Placements[runStart];
            ExternalGroupTabPlacement last = state.Placements[runEnd - 1];
            int insertionIndex = ResolveExternalGroupSubsetInsertionIndex(
                state.SourceHost,
                first.OriginalIndex,
                first.PreviousStableWindow,
                last.NextStableWindow);
            for (int placementIndex = runStart; placementIndex < runEnd; placementIndex++)
            {
                UiWindow window = state.Placements[placementIndex].Window;
                PrepareDockedWindow(window, state.SourceHost);
                state.SourceHost.DockWindow(window, insertionIndex++);
            }

            runStart = runEnd;
        }
    }

    private static int ResolveExternalGroupSubsetInsertionIndex(
        UiDockHost host,
        int originalIndex,
        UiWindow? previousStableWindow,
        UiWindow? nextStableWindow)
    {
        UiWindow[] currentWindows = host.Windows.ToArray();
        int previousIndex = previousStableWindow == null
            ? -1
            : Array.IndexOf(currentWindows, previousStableWindow);
        int nextIndex = nextStableWindow == null
            ? -1
            : Array.IndexOf(currentWindows, nextStableWindow);

        if (previousIndex >= 0 && nextIndex >= 0)
        {
            if (previousIndex < nextIndex)
            {
                return Math.Clamp(originalIndex, previousIndex + 1, nextIndex);
            }

            return previousIndex + 1;
        }

        if (previousIndex >= 0)
        {
            return previousIndex + 1;
        }

        if (nextIndex >= 0)
        {
            return Math.Clamp(originalIndex, 0, nextIndex);
        }

        return Math.Clamp(originalIndex, 0, currentWindows.Length);
    }

    private ExternalGroupLeaseState GetExternalDockGroupLeaseState(
        UiDockExternalGroupLease lease,
        out long leaseId)
    {
        if (!lease.BelongsTo(this, out leaseId))
        {
            throw new ArgumentException("The external dock group lease belongs to a different workspace.", nameof(lease));
        }

        if (!_externalGroupLeases.TryGetValue(leaseId, out ExternalGroupLeaseState? state))
        {
            throw new InvalidOperationException("The external dock group lease is no longer active.");
        }

        return state;
    }

    internal bool IsExternalDockGroupLeaseActive(UiDockExternalGroupLease lease, long leaseId)
    {
        return lease.BelongsTo(this, out long actualLeaseId)
            && actualLeaseId == leaseId
            && _externalGroupLeases.ContainsKey(leaseId);
    }

    internal void AbandonExternalDockGroup(UiDockExternalGroupLease lease, long leaseId)
    {
        if (!lease.BelongsTo(this, out long actualLeaseId) || actualLeaseId != leaseId)
        {
            throw new ArgumentException("The external dock group lease belongs to a different workspace.", nameof(lease));
        }

        _externalGroupLeases.Remove(leaseId);
    }

    private UiDockNodeState CaptureNodeState(DockNode node)
    {
        if (node.Host != null)
        {
            return new UiDockNodeState
            {
                HostId = node.Host.Id,
                IsCollapsed = node.IsCollapsed
            };
        }

        UiDockNodeState state = new()
        {
            SplitHorizontal = node.SplitHorizontal,
            SplitRatio = node.SplitRatio,
            IsCollapsed = node.IsCollapsed
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
            if (!usedHosts.Add(host))
            {
                throw new ArgumentException($"Dock layout contains duplicate host leaf '{state.HostId}'.", nameof(state));
            }

            return new DockNode(host)
            {
                IsCollapsed = state.IsCollapsed
            };
        }

        DockNode node = new(null)
        {
            SplitHorizontal = state.SplitHorizontal,
            SplitRatio = Math.Clamp(state.SplitRatio, 0.05f, 0.95f),
            IsCollapsed = state.IsCollapsed
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

    private static void ValidateNodeState(UiDockNodeState state)
    {
        if (!string.IsNullOrWhiteSpace(state.HostId))
        {
            if (state.First != null || state.Second != null)
            {
                throw new ArgumentException($"Dock host node '{state.HostId}' cannot also contain split children.", nameof(state));
            }

            return;
        }

        if (state.First == null || state.Second == null)
        {
            throw new ArgumentException("Dock split nodes require both children.", nameof(state));
        }

        if (!float.IsFinite(state.SplitRatio))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state.SplitRatio, "Dock split ratio must be finite.");
        }

        ValidateNodeState(state.First);
        ValidateNodeState(state.Second);
    }

    private void ValidateRestoredWindowAssignments(
        UiDockWorkspaceState state,
        IReadOnlyDictionary<string, UiWindow> windowsById,
        IReadOnlyDictionary<string, UiDockHost> hostById,
        IReadOnlySet<UiDockHost> usedHosts)
    {
        HashSet<string> hostStateIds = new(StringComparer.Ordinal);
        HashSet<UiWindow> assignedWindows = new();
        foreach (UiDockHostState hostState in state.Hosts)
        {
            if (!hostStateIds.Add(hostState.HostId))
            {
                throw new ArgumentException($"Dock state contains duplicate host '{hostState.HostId}'.", nameof(state));
            }

            if (!hostById.TryGetValue(hostState.HostId, out UiDockHost? host) || !usedHosts.Contains(host))
            {
                throw new ArgumentException($"Dock state assigns windows to host '{hostState.HostId}' outside its layout tree.", nameof(state));
            }

            foreach (string windowId in hostState.WindowIds)
            {
                if (!windowsById.TryGetValue(windowId, out UiWindow? window))
                {
                    continue;
                }

                if (!assignedWindows.Add(window))
                {
                    throw new ArgumentException($"Dock state assigns window '{windowId}' more than once.", nameof(state));
                }

                if (!CanRestoreWindow(window))
                {
                    throw new InvalidOperationException($"Window '{windowId}' belongs to a different UI container.");
                }

                if (!CanDockWindow(window, host, DockTarget.Center))
                {
                    throw new InvalidOperationException($"Window '{window.Id}' cannot restore into host '{host.Id}'.");
                }

                if (!_hosts.Contains(host) || !CanRestoreWindow(window))
                {
                    throw new InvalidOperationException(
                        $"Dock policy invalidated host '{host.Id}' or window '{window.Id}' during restore validation.");
                }
            }
        }

        foreach (UiFloatingWindowState floatingState in state.FloatingWindows)
        {
            if (!windowsById.TryGetValue(floatingState.WindowId, out UiWindow? window))
            {
                continue;
            }

            if (!assignedWindows.Add(window))
            {
                throw new ArgumentException(
                    $"Dock state assigns window '{floatingState.WindowId}' more than once.",
                    nameof(state));
            }

            if (!CanRestoreWindow(window))
            {
                throw new InvalidOperationException(
                    $"Window '{floatingState.WindowId}' belongs to a different UI container.");
            }
            if (window.Parent is UiDockHost sourceHost
                && _hosts.Contains(sourceHost)
                && !EvaluateExternalDetach(sourceHost, window).Allowed)
            {
                throw new InvalidOperationException(
                    $"Window '{floatingState.WindowId}' cannot restore as floating from host '{sourceHost.Id}'.");
            }
        }
    }

    private void ValidateRestoredCollapsedBranches(
        UiDockWorkspaceState state,
        DockNode proposedRoot,
        IReadOnlyDictionary<string, UiWindow> windowsById,
        IReadOnlyDictionary<string, UiDockHost> hostById)
    {
        Dictionary<string, UiDockHostState> hostStates = new(StringComparer.Ordinal);
        foreach (UiDockHostState hostState in state.Hosts)
        {
            hostStates[hostState.HostId] = hostState;
        }

        ValidateRestoredCollapsedNode(
            proposedRoot,
            proposedRoot,
            ancestorCollapsed: false,
            windowsById,
            hostById,
            hostStates);
    }

    private void ValidateRestoredCollapsedNode(
        DockNode root,
        DockNode node,
        bool ancestorCollapsed,
        IReadOnlyDictionary<string, UiWindow> windowsById,
        IReadOnlyDictionary<string, UiDockHost> hostById,
        IReadOnlyDictionary<string, UiDockHostState> hostStates)
    {
        if (node.First?.IsCollapsed == true && node.Second?.IsCollapsed == true)
        {
            throw new ArgumentException("Dock state cannot collapse both sibling branches.", nameof(node));
        }

        if (node.IsCollapsed)
        {
            if (ancestorCollapsed)
            {
                throw new ArgumentException("Dock state cannot contain nested collapsed branches.", nameof(node));
            }

            if (ContainsHost(node, RootHost))
            {
                throw new InvalidOperationException("Dock state cannot collapse the root document host.");
            }

            UiDockHost? representative = EnumerateHosts(node).FirstOrDefault();
            if (representative == null || !ReferenceEquals(ResolveCollapseNode(root, representative), node))
            {
                throw new ArgumentException(
                    "Dock state collapsed a partial branch instead of its stable root-side region.",
                    nameof(node));
            }

            foreach (UiDockHost host in EnumerateHosts(node))
            {
                if (!host.AllowCollapse)
                {
                    throw new InvalidOperationException($"Dock host '{host.Id}' does not allow collapse.");
                }

                if (!hostById.TryGetValue(host.Id, out UiDockHost? currentHost)
                    || !ReferenceEquals(currentHost, host))
                {
                    throw new InvalidOperationException($"Dock collapse policy invalidated host '{host.Id}'.");
                }

                if (!hostStates.TryGetValue(host.Id, out UiDockHostState? hostState)
                    || !hostState.WindowIds.Any(windowsById.ContainsKey))
                {
                    throw new ArgumentException(
                        $"Collapsed dock host '{host.Id}' must contain at least one restorable window.",
                        nameof(node));
                }
            }
        }

        bool childAncestorCollapsed = ancestorCollapsed || node.IsCollapsed;
        if (node.First != null)
        {
            ValidateRestoredCollapsedNode(
                root,
                node.First,
                childAncestorCollapsed,
                windowsById,
                hostById,
                hostStates);
        }

        if (node.Second != null)
        {
            ValidateRestoredCollapsedNode(
                root,
                node.Second,
                childAncestorCollapsed,
                windowsById,
                hostById,
                hostStates);
        }
    }

    private void ValidateLiveRestorePreCommit(
        UiDockWorkspaceState state,
        IReadOnlyDictionary<string, UiWindow> windowsById,
        IReadOnlyDictionary<string, UiDockHost> hostById,
        IReadOnlySet<UiDockHost> usedHosts,
        DockNode proposedRoot,
        DockNode liveTopology,
        IReadOnlyList<UiDockHost> validationHosts,
        IReadOnlyDictionary<UiDockHost, string> validationHostIds,
        long validationVersion)
    {
        if (_topologyMutationVersion != validationVersion
            || _hosts.Count != validationHosts.Count
            || !DockNodesEquivalent(_rootNode, liveTopology))
        {
            throw new InvalidOperationException(
                "Dock topology changed while workspace state policy was being validated.");
        }

        for (int index = 0; index < validationHosts.Count; index++)
        {
            UiDockHost expected = validationHosts[index];
            if (!ReferenceEquals(_hosts[index], expected)
                || !ReferenceEquals(expected.Parent, this)
                || !validationHostIds.TryGetValue(expected, out string? expectedId)
                || !string.Equals(expected.Id, expectedId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Dock host membership changed while workspace state policy was being validated.");
            }
        }

        HashSet<UiDockHost> proposedHosts = new(EnumerateHosts(proposedRoot));
        if (!proposedHosts.SetEquals(usedHosts))
        {
            throw new InvalidOperationException("Proposed dock topology diverged from its validated host set.");
        }

        foreach (UiDockHost host in proposedHosts)
        {
            if (!_hosts.Contains(host)
                || !ReferenceEquals(host.Parent, this)
                || !hostById.TryGetValue(host.Id, out UiDockHost? mappedHost)
                || !ReferenceEquals(mappedHost, host))
            {
                throw new InvalidOperationException($"Proposed dock host '{host.Id}' is no longer live.");
            }
        }

        foreach (UiDockHostState hostState in state.Hosts)
        {
            foreach (string windowId in hostState.WindowIds)
            {
                if (windowsById.TryGetValue(windowId, out UiWindow? window) && !CanRestoreWindow(window))
                {
                    throw new InvalidOperationException(
                        $"Window '{windowId}' changed container during workspace state validation.");
                }
            }
        }

        foreach (UiFloatingWindowState floatingState in state.FloatingWindows)
        {
            if (windowsById.TryGetValue(floatingState.WindowId, out UiWindow? window)
                && !CanRestoreWindow(window))
            {
                throw new InvalidOperationException(
                    $"Window '{floatingState.WindowId}' changed container during workspace state validation.");
            }
        }
    }

    private static DockNode CloneDockNode(DockNode node)
    {
        DockNode clone = new(node.Host)
        {
            SplitHorizontal = node.SplitHorizontal,
            SplitRatio = node.SplitRatio,
            IsCollapsed = node.IsCollapsed
        };
        if (node.First != null)
        {
            clone.First = CloneDockNode(node.First);
        }

        if (node.Second != null)
        {
            clone.Second = CloneDockNode(node.Second);
        }

        return clone;
    }

    private static bool DockNodesEquivalent(DockNode left, DockNode right)
    {
        if (!ReferenceEquals(left.Host, right.Host)
            || left.SplitHorizontal != right.SplitHorizontal
            || left.SplitRatio != right.SplitRatio
            || left.IsCollapsed != right.IsCollapsed
            || (left.First == null) != (right.First == null)
            || (left.Second == null) != (right.Second == null))
        {
            return false;
        }

        return (left.First == null || DockNodesEquivalent(left.First, right.First!))
            && (left.Second == null || DockNodesEquivalent(left.Second, right.Second!));
    }

    private bool CanRestoreWindow(UiWindow window)
    {
        return window.Parent == null
            || ReferenceEquals(window.Parent, this) && _floatingWindows.Contains(window)
            || window.Parent is UiDockHost host && _hosts.Contains(host);
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
                    _dragOutsideWorkspaceTelemetryEmitted = false;
                    UiRect tabRect = host.GetTabBounds(index);
                    _dragPointerOffsetX = Math.Clamp(input.MousePosition.X - tabRect.X, 0, Math.Max(0, tabRect.Width));
                    _dragPointerOffsetY = Math.Clamp(input.MousePosition.Y - tabRect.Y, 0, Math.Max(0, tabRect.Height));
                    ResetDockHoverTelemetry();
                    TraceTearOffTelemetry(
                        $"drag-start host={FormatHost(host)} window={FormatWindow(_dragWindow)} index={index} mouse={FormatPoint(input.MousePosition)} screen={FormatPoint(input.ScreenMousePosition)}");
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
            if (Bounds.Contains(_dragPosition))
            {
                _dragOutsideWorkspaceTelemetryEmitted = false;
            }

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
                && !Bounds.Contains(_dragPosition))
            {
                ExternalDetachDecision detachDecision = EvaluateExternalDetach(_dragSourceHost, _dragWindow);
                if (!_dragOutsideWorkspaceTelemetryEmitted)
                {
                    _dragOutsideWorkspaceTelemetryEmitted = true;
                    TraceTearOffTelemetry(
                        $"drag-detach-check host={FormatHost(_dragSourceHost)} window={FormatWindow(_dragWindow)} allowed={(detachDecision.Allowed ? 1 : 0)} reason='{detachDecision.Reason}' predicatePresent={(detachDecision.PredicatePresent ? 1 : 0)} predicateResult={(detachDecision.PredicateResult ? 1 : 0)} mouse={FormatPoint(_dragPosition)} screen={FormatPoint(input.ScreenMousePosition)} workspace={FormatRect(Bounds)}");
                }

                if (detachDecision.Allowed)
                {
                    UiWindow window = _dragWindow;
                    UiDockHost sourceHost = _dragSourceHost;
                    UiPoint detachPoint = GetDetachPoint(input);
                    TraceTearOffTelemetry(
                        $"drag-detach-dispatch host={FormatHost(sourceHost)} window={FormatWindow(window)} detachPoint={FormatPoint(detachPoint)}");
                    RemoveWindowForWorkspaceMutation(sourceHost, window);
                    CollapseEmptyHosts();
                    TabDetached?.Invoke(window, detachPoint);
                    ResetTabDragState();
                    return;
                }
            }
        }

        if (input.LeftReleased)
        {
            if (_dragMoved)
            {
                UpdateHover(input.MousePosition);
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
            UpdateHover(input.MousePosition);
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

        _hoverTarget = GetDockTarget(_hoverHost, mousePosition, inferEdgeTarget: true);
        if (!CanDockWindow(_dragWindow, _hoverHost, _hoverTarget) || !_hosts.Contains(_hoverHost))
        {
            _hoverTarget = DockTarget.None;
            _previewBounds = default;
            return;
        }

        _previewBounds = GetDockPreviewBounds(_hoverHost.Bounds, _hoverTarget, _dragWindow.Bounds);
        TraceDockHoverIfChanged(mousePosition);
    }

    private void HandleDrop(UiWindow window, UiPoint dropPoint, UiPoint screenDropPoint)
    {
        UiDockHost? sourceHost = _dragSourceHost;
        TraceTearOffTelemetry(
            $"drop-start sourceHost={FormatHostOrNone(sourceHost)} hoverHost={FormatHostOrNone(_hoverHost)} target='{_hoverTarget}' window={FormatWindow(window)} drop={FormatPoint(dropPoint)} screenDrop={FormatPoint(screenDropPoint)} workspace={FormatRect(Bounds)}");

        if (sourceHost != null && _hoverHost == sourceHost && _hoverTarget == DockTarget.Center)
        {
            TraceTearOffTelemetry(
                $"drop-skip reason='same-host-center' sourceHost={FormatHost(sourceHost)} window={FormatWindow(window)} drop={FormatPoint(dropPoint)}");
            return;
        }

        if (_hoverHost != null
            && (!CanDockWindow(window, _hoverHost, _hoverTarget) || !_hosts.Contains(_hoverHost)))
        {
            TraceTearOffTelemetry(
                $"drop-skip reason='dock-policy' targetHost={FormatHost(_hoverHost)} target='{_hoverTarget}' window={FormatWindow(window)}");
            return;
        }

        bool collapseEmptyHosts = sourceHost != null;
        if (_hoverHost == null)
        {
            if (sourceHost != null)
            {
                ExternalDetachDecision detachDecision = EvaluateExternalDetach(sourceHost, window);
                bool dropOutsideWorkspace = !Bounds.Contains(dropPoint);
                TraceTearOffTelemetry(
                    $"drop-detach-check host={FormatHost(sourceHost)} window={FormatWindow(window)} allowed={(detachDecision.Allowed ? 1 : 0)} reason='{detachDecision.Reason}' predicatePresent={(detachDecision.PredicatePresent ? 1 : 0)} predicateResult={(detachDecision.PredicateResult ? 1 : 0)} dropOutside={(dropOutsideWorkspace ? 1 : 0)} drop={FormatPoint(dropPoint)} screenDrop={FormatPoint(screenDropPoint)} workspace={FormatRect(Bounds)}");
                RemoveWindowForWorkspaceMutation(sourceHost, window);
                if (dropOutsideWorkspace && detachDecision.Allowed)
                {
                    UiPoint detachPoint = GetDetachPoint(screenDropPoint);
                    TraceTearOffTelemetry(
                        $"drop-detach-dispatch host={FormatHost(sourceHost)} window={FormatWindow(window)} detachPoint={FormatPoint(detachPoint)}");
                    TabDetached?.Invoke(window, detachPoint);
                }
                else
                {
                    window.Bounds = ClampToBounds(GetFloatingPreviewBounds(dropPoint, window.Bounds), Bounds);
                    TraceTearOffTelemetry(
                        $"drop-floating-fallback host={FormatHost(sourceHost)} window={FormatWindow(window)} bounds={FormatRect(window.Bounds)}");
                    AddFloatingWindow(window);
                }
            }
            if (collapseEmptyHosts)
            {
                CollapseEmptyHosts();
            }
            return;
        }

        UiDockHost targetHost = _hoverHost;
        if (_hoverTarget is DockTarget.Left or DockTarget.Right or DockTarget.Top or DockTarget.Bottom)
        {
            targetHost = SplitHost(_hoverHost, _hoverTarget);
        }

        if (!CanDockWindow(window, targetHost, DockTarget.Center) || !_hosts.Contains(targetHost))
        {
            CollapseEmptyHosts();
            return;
        }

        if (sourceHost != null)
        {
            RemoveWindowForWorkspaceMutation(sourceHost, window);
        }

        if (_floatingWindows.Contains(window))
        {
            _floatingWindows.Remove(window);
            RemoveChild(window);
        }

        PrepareDockedWindow(window, targetHost);
        targetHost.DockWindow(window);
        targetHost.ActivateWindow(targetHost.Windows.Count - 1);
        TraceTearOffTelemetry(
            $"drop-dock targetHost={FormatHost(targetHost)} target='{_hoverTarget}' window={FormatWindow(window)} split={(_hoverTarget is DockTarget.Left or DockTarget.Right or DockTarget.Top or DockTarget.Bottom ? 1 : 0)}");

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
        destination.TabInactiveColor = source.TabInactiveColor;
        destination.TabActiveColor = source.TabActiveColor;
        destination.TabHoverColor = source.TabHoverColor;
        destination.TabTextColor = source.TabTextColor;
        destination.TabActiveTextColor = source.TabActiveTextColor;
        destination.TabBorderColor = source.TabBorderColor;
        destination.TabActiveAccentColor = source.TabActiveAccentColor;
        destination.MenuBackground = source.MenuBackground;
        destination.MenuHoverColor = source.MenuHoverColor;
        destination.MenuBorderColor = source.MenuBorderColor;
        destination.MenuTextColor = source.MenuTextColor;
        destination.MenuDisabledTextColor = source.MenuDisabledTextColor;
        destination.PanelInset = source.PanelInset;
        destination.CornerRadius = source.CornerRadius;
        destination.ClipChildren = source.ClipChildren;
        destination.TabBarHeight = source.TabBarHeight;
        destination.TabWidth = source.TabWidth;
        destination.TabMaxWidth = source.TabMaxWidth;
        destination.TabPadding = source.TabPadding;
        destination.TabIconSpacing = source.TabIconSpacing;
        destination.TabInset = source.TabInset;
        destination.TabTrailingInset = source.TabTrailingInset;
        destination.TabBottomInset = source.TabBottomInset;
        destination.TabCornerRadius = source.TabCornerRadius;
        destination.TabActiveAccentHeight = source.TabActiveAccentHeight;
        destination.TabTextScale = source.TabTextScale;
        destination.TabTextBold = source.TabTextBold;
        destination.AutoSizeTabs = source.AutoSizeTabs;
        destination.TabTextOverflow = source.TabTextOverflow;
        destination.ShowCloseButtons = source.ShowCloseButtons;
        destination.CloseButtonPlacement = source.CloseButtonPlacement;
        destination.CloseButtonPadding = source.CloseButtonPadding;
        destination.CloseButtonText = source.CloseButtonText;
        destination.ScrollButtonWidth = source.ScrollButtonWidth;
        destination.OverflowButtonWidth = source.OverflowButtonWidth;
        destination.ScrollStep = source.ScrollStep;
        destination.ShowOverflowMenuButton = source.ShowOverflowMenuButton;
        destination.ShowTabContextMenu = source.ShowTabContextMenu;
        destination.ShowCollapseButton = source.ShowCollapseButton;
        destination.CollapseButtonWidth = source.CollapseButtonWidth;
        destination.HideDockedTitleBars = source.HideDockedTitleBars;
        destination.AllowReorder = source.AllowReorder;
        destination.DragThreshold = source.DragThreshold;
    }

    private static bool CanDetachWindowExternally(UiDockHost host, UiWindow window)
    {
        return EvaluateExternalDetach(host, window).Allowed;
    }

    private static ExternalDetachDecision EvaluateExternalDetach(UiDockHost host, UiWindow window)
    {
        if (!host.AllowDetach)
        {
            return new ExternalDetachDecision(
                Allowed: false,
                PredicatePresent: host.CanDetachWindowPredicate != null,
                PredicateResult: false,
                Reason: "host-detach-disabled");
        }

        if (host.CanDetachWindowPredicate == null)
        {
            return new ExternalDetachDecision(
                Allowed: true,
                PredicatePresent: false,
                PredicateResult: true,
                Reason: "allowed-no-predicate");
        }

        bool predicateResult = host.CanDetachWindowPredicate(window);
        return new ExternalDetachDecision(
            Allowed: predicateResult,
            PredicatePresent: true,
            PredicateResult: predicateResult,
            Reason: predicateResult ? "allowed-by-predicate" : "blocked-by-predicate");
    }

    private void UpdateExternalPreviewHover(UiPoint hoverPoint, UiRect previewWindowBounds)
    {
        if (_externalPreviewWindow == null)
        {
            _hoverHost = null;
            _hoverTarget = DockTarget.None;
            _previewBounds = previewWindowBounds;
            return;
        }

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
            if (!CanDockWindow(_externalPreviewWindow, _hoverHost, externalTarget) || !_hosts.Contains(_hoverHost))
            {
                _hoverTarget = DockTarget.None;
                _previewBounds = previewWindowBounds;
                return;
            }

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
        _dragOutsideWorkspaceTelemetryEmitted = false;
        _hoverHost = null;
        _hoverTarget = DockTarget.None;
        _previewBounds = default;
        ResetDockHoverTelemetry();
    }

    private void TraceTearOffTelemetry(string message)
    {
        TearOffTelemetry?.Invoke(message);
    }

    private static string FormatHost(UiDockHost host)
    {
        return $"id='{host.Id}' allowDetach={(host.AllowDetach ? 1 : 0)} externalDrag={(host.ExternalDragHandling ? 1 : 0)} allowReorder={(host.AllowReorder ? 1 : 0)} predicate={(host.CanDetachWindowPredicate != null ? 1 : 0)} windows={host.Windows.Count} bounds={FormatRect(host.Bounds)}";
    }

    private static string FormatHostOrNone(UiDockHost? host)
    {
        return host == null ? "none" : FormatHost(host);
    }

    private static string FormatWindow(UiWindow window)
    {
        return $"id='{window.Id}' title='{window.Title}' bounds={FormatRect(window.Bounds)}";
    }

    private void TraceDockHoverIfChanged(UiPoint mousePosition)
    {
        if (_dragWindow == null || !_dragMoved)
        {
            return;
        }

        string? hoverHostId = _hoverHost?.Id;
        if (string.Equals(_lastTelemetryHoverHostId, hoverHostId, StringComparison.Ordinal)
            && _lastTelemetryHoverTarget == _hoverTarget)
        {
            return;
        }

        _lastTelemetryHoverHostId = hoverHostId;
        _lastTelemetryHoverTarget = _hoverTarget;
        TraceTearOffTelemetry(
            $"dock-hover sourceHost={FormatHostOrNone(_dragSourceHost)} hoverHost={FormatHostOrNone(_hoverHost)} target='{_hoverTarget}' window={FormatWindow(_dragWindow)} mouse={FormatPoint(mousePosition)} preview={FormatRect(_previewBounds)}");
    }

    private void ResetDockHoverTelemetry()
    {
        _lastTelemetryHoverHostId = null;
        _lastTelemetryHoverTarget = DockTarget.None;
    }

    private static string FormatPoint(UiPoint point)
    {
        return $"({point.X},{point.Y})";
    }

    private static string FormatRect(UiRect rect)
    {
        return $"({rect.X},{rect.Y},{rect.Width},{rect.Height})";
    }

    private readonly record struct ExternalDetachDecision(
        bool Allowed,
        bool PredicatePresent,
        bool PredicateResult,
        string Reason);

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
        if (_floatingWindows.Count == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
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
        ThrowIfRestoreValidationMutation("replace dock topology");
        CancelDockInteractionsForTopologyChange();
        DockNode originalRoot = _rootNode;
        int originalHostCount = _hosts.Count;
        DockNode? collapsed = CollapseNode(_rootNode);
        if (collapsed == null)
        {
            EnsureRootHost();
            if (!ReferenceEquals(originalRoot, _rootNode) || originalHostCount != _hosts.Count)
            {
                _topologyMutationVersion++;
            }

            return;
        }

        _rootNode = collapsed;
        EnsureRootHost();
        if (!ReferenceEquals(originalRoot, _rootNode) || originalHostCount != _hosts.Count)
        {
            _topologyMutationVersion++;
        }
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
            if (node.IsCollapsed && node.Second != null)
            {
                node.Second.IsCollapsed = true;
            }

            return node.Second;
        }

        if (node.Second == null)
        {
            if (node.IsCollapsed)
            {
                node.First.IsCollapsed = true;
            }

            return node.First;
        }

        return node;
    }

    private void RemoveDockHost(UiDockHost host)
    {
        host.TabCloseCompleted -= HandleHostTabCloseCompleted;
        host.WindowsAdding -= HandleHostWindowsAdding;
        host.WindowsMutating -= HandleHostWindowsMutating;
        host.WindowsMutated -= HandleHostWindowsMutated;
        host.ClearWindows();
        _hosts.Remove(host);
        RemoveChild(host);
        _topologyMutationVersion++;
    }

    private void HandleHostTabCloseCompleted()
    {
        CollapseEmptyHosts();
        Arrange();
    }

    private void HandleHostWindowsMutating(UiDockHost host)
    {
        if (_restoreValidationActive)
        {
            throw new InvalidOperationException(
                "Dock windows cannot be mutated while workspace state policy is being validated.");
        }

        if (_suppressHostMutationCallbacks || !_hosts.Contains(host))
        {
            return;
        }

        if (IsCollapseRegionCollapsed(host))
        {
            SetCollapseRegionCollapsed(host, collapsed: false);
        }
    }

    private void HandleHostWindowsAdding(UiDockHost host)
    {
        if (_restoreValidationActive)
        {
            throw new InvalidOperationException(
                "Dock windows cannot be mutated while workspace state policy is being validated.");
        }
    }

    private void HandleHostWindowsMutated(UiDockHost host)
    {
        if (_suppressHostMutationCallbacks
            || !_hosts.Contains(host)
            || ReferenceEquals(host, RootHost)
            || host.IsClosingWindow
            || !host.IsEmpty)
        {
            return;
        }

        CollapseEmptyHosts();
        Arrange();
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

        UiWindow[] fallbackWindows = fallback.Windows.ToArray();
        foreach (UiWindow window in fallbackWindows)
        {
            if (!CanDockWindow(window, RootHost, DockTarget.Center)
                || !_hosts.Contains(RootHost)
                || !_hosts.Contains(fallback)
                || !WindowSequenceMatches(fallback.Windows, fallbackWindows))
            {
                return;
            }
        }

        foreach (UiWindow window in fallbackWindows)
        {
            if (!ReferenceEquals(window.Parent, fallback)
                || !RemoveWindowForWorkspaceMutation(fallback, window))
            {
                return;
            }

            PrepareDockedWindow(window, RootHost);
            RootHost.DockWindow(window);
        }

        RemoveDockHost(fallback);
        _rootNode = new DockNode(RootHost);
    }

    private static bool WindowSequenceMatches(IReadOnlyList<UiWindow> actual, IReadOnlyList<UiWindow> expected)
    {
        if (actual.Count != expected.Count)
        {
            return false;
        }

        for (int index = 0; index < actual.Count; index++)
        {
            if (!ReferenceEquals(actual[index], expected[index]))
            {
                return false;
            }
        }

        return true;
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
        if (node.IsCollapsed)
        {
            return;
        }

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
            UiColor trackColor = node == _dragSplitNode
                ? SplitterTrackActiveColor
                : node == _hoverSplitNode
                    ? SplitterTrackHoverColor
                    : UiColor.Transparent;
            RenderSplitter(context, node.SplitterBounds, node.SplitHorizontal, splitterColor, trackColor);
        }

        DrawSplitters(context, node.First);
        DrawSplitters(context, node.Second);
    }

    private void RenderSplitter(UiRenderContext context, UiRect bounds, bool horizontal, UiColor splitterColor, UiColor trackColor)
    {
        if (trackColor.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, bounds, 2, trackColor);
        }

        int inset = Math.Max(0, SplitterVisualInset);
        if (horizontal)
        {
            int thickness = Math.Max(1, Math.Min(bounds.Height, SplitterVisualThickness));
            UiRect lineRect = new(
                bounds.X + inset,
                bounds.Y + (bounds.Height - thickness) / 2,
                Math.Max(0, bounds.Width - inset * 2),
                thickness);
            if (lineRect.Width > 0 && lineRect.Height > 0)
            {
                UiRenderHelpers.FillRectRounded(context.Renderer, lineRect, Math.Min(2, thickness / 2), splitterColor);
            }

            return;
        }

        int verticalThickness = Math.Max(1, Math.Min(bounds.Width, SplitterVisualThickness));
        UiRect verticalLineRect = new(
            bounds.X + (bounds.Width - verticalThickness) / 2,
            bounds.Y + inset,
            verticalThickness,
            Math.Max(0, bounds.Height - inset * 2));
        if (verticalLineRect.Width > 0 && verticalLineRect.Height > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, verticalLineRect, Math.Min(2, verticalThickness / 2), splitterColor);
        }
    }

    private void DrawCollapsedStrips(UiRenderContext context, DockNode node)
    {
        if (node.IsCollapsed)
        {
            UiRect bounds = node.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            UiColor background = ReferenceEquals(node, _hoverCollapsedNode)
                ? CollapsedStripHoverColor
                : CollapsedStripColor;
            context.Renderer.FillRect(bounds, background);
            context.Renderer.DrawRect(bounds, CollapsedStripBorderColor, 1);
            UiRect restoreBounds = GetCollapsedRestoreBounds(node);
            UiDoubleChevron.Draw(
                context.Renderer,
                restoreBounds,
                GetRestoreArrowDirection(node.CollapseEdge),
                CollapsedStripGlyphColor);
            return;
        }

        if (node.First != null)
        {
            DrawCollapsedStrips(context, node.First);
        }

        if (node.Second != null)
        {
            DrawCollapsedStrips(context, node.Second);
        }
    }

    private bool HandleCollapseInputBeforeChildren(UiInputState input, UiFocusManager focus)
    {
        _hoverCollapsedNode = FindCollapsedRestoreNode(_rootNode, input.MousePosition);
        if (!input.LeftClicked)
        {
            return false;
        }

        UiDockHost? memberHost = _hoverCollapsedNode == null
            ? FindExpandedCollapseToggleHost(input.MousePosition)
            : EnumerateHosts(_hoverCollapsedNode).FirstOrDefault();
        if (memberHost == null)
        {
            return false;
        }

        bool collapse = _hoverCollapsedNode == null;
        focus.ClearFocus();
        bool changed = SetCollapseRegionCollapsed(memberHost, collapse);
        _hoverCollapsedNode = null;
        return changed;
    }

    private UiDockHost? FindExpandedCollapseToggleHost(UiPoint point)
    {
        foreach (UiDockHost host in _hosts)
        {
            if (host.IsCollapsed || host.CollapseToggleBounds.Width <= 0)
            {
                continue;
            }

            if (host.CollapseToggleBounds.Contains(point))
            {
                return host;
            }
        }

        return null;
    }

    private void CancelDockInteractionsForTopologyChange()
    {
        _dragSplitNode = null;
        _hoverSplitNode = null;
        _dragWindow = null;
        _dragSourceHost = null;
        _dragMoved = false;
        _floatingDragWindow = null;
        _hoverHost = null;
        _hoverTarget = DockTarget.None;
        _previewBounds = default;
        _externalPreviewWindow = null;
        _externalPreviewHoverPoint = default;
        _externalPreviewWindowBounds = default;
        _dragOutsideWorkspaceTelemetryEmitted = false;
        ResetDockHoverTelemetry();
    }

    private void ThrowIfRestoreValidationMutation(string operation)
    {
        if (_restoreValidationActive)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} while workspace state policy is being validated.");
        }
    }

    private static UiInputState SuppressInputForFrame(UiInputState input)
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

    private static DockNode? FindCollapsedRestoreNode(DockNode node, UiPoint point)
    {
        if (node.IsCollapsed)
        {
            return GetCollapsedRestoreBounds(node).Contains(point) ? node : null;
        }

        if (node.First != null)
        {
            DockNode? first = FindCollapsedRestoreNode(node.First, point);
            if (first != null)
            {
                return first;
            }
        }

        return node.Second == null ? null : FindCollapsedRestoreNode(node.Second, point);
    }

    private static UiRect GetCollapsedRestoreBounds(DockNode node)
    {
        UiRect bounds = node.Bounds;
        bool vertical = node.CollapseEdge is UiDockCollapseEdge.Left or UiDockCollapseEdge.Right;
        int extent = vertical
            ? Math.Min(bounds.Width, bounds.Height)
            : Math.Min(bounds.Height, bounds.Width);
        return vertical
            ? new UiRect(bounds.X, bounds.Y, bounds.Width, Math.Max(0, extent))
            : new UiRect(bounds.X, bounds.Y, Math.Max(0, extent), bounds.Height);
    }

    private static UiArrowDirection GetRestoreArrowDirection(UiDockCollapseEdge edge)
    {
        return edge switch
        {
            UiDockCollapseEdge.Left => UiArrowDirection.Right,
            UiDockCollapseEdge.Top => UiArrowDirection.Down,
            UiDockCollapseEdge.Bottom => UiArrowDirection.Up,
            _ => UiArrowDirection.Left
        };
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

    private void LayoutNode(DockNode node, UiRect bounds, UiDockCollapseEdge edge)
    {
        node.Bounds = bounds;
        node.CollapseEdge = edge;

        if (node.IsCollapsed)
        {
            if (ReferenceEquals(node, _rootNode))
            {
                int strip = Math.Min(GetCollapsedStripExtent(), Math.Max(0, bounds.Width));
                bounds = new UiRect(bounds.Right - strip, bounds.Y, strip, bounds.Height);
            }

            ConfigureCollapsedNode(node, bounds, edge);
            node.FirstBounds = default;
            node.SecondBounds = default;
            node.SplitterBounds = default;
            return;
        }

        if (node.Host != null)
        {
            node.FirstBounds = default;
            node.SecondBounds = default;
            node.SplitterBounds = default;
            node.Host.Bounds = bounds;
            DockNode? collapseNode = ResolveCollapseNode(_rootNode, node.Host);
            UiDockHost? representative = collapseNode == null
                ? null
                : EnumerateHosts(collapseNode).FirstOrDefault();
            node.Host.ConfigureCollapsePresentation(
                collapsed: false,
                interactionEnabled: ReferenceEquals(node.Host, representative)
                    && CanCollapseRegion(node.Host),
                edge: collapseNode?.CollapseEdge ?? edge);
            return;
        }

        if (node.First == null || node.Second == null)
        {
            node.FirstBounds = default;
            node.SecondBounds = default;
            node.SplitterBounds = default;
            return;
        }

        bool firstCollapsed = node.First.IsCollapsed;
        bool secondCollapsed = node.Second.IsCollapsed;
        int splitterThickness = firstCollapsed || secondCollapsed
            ? 0
            : Math.Max(1, SplitterThickness);
        UiPoint firstMinSize = GetMinimumNodeSize(node.First);
        UiPoint secondMinSize = GetMinimumNodeSize(node.Second);
        // Preserve the authored split ratio across transient layout passes.
        // Startup and resize frames can report zero or constrained bounds before the
        // workspace settles, and feeding those clamped sizes back into SplitRatio
        // would permanently flatten a restored layout.

        if (node.SplitHorizontal)
        {
            int availableHeight = Math.Max(0, bounds.Height - splitterThickness);
            int firstHeight;
            if (firstCollapsed)
            {
                firstHeight = Math.Min(availableHeight, GetCollapsedStripExtent());
            }
            else if (secondCollapsed)
            {
                firstHeight = Math.Max(0, availableHeight - Math.Min(availableHeight, GetCollapsedStripExtent()));
            }
            else
            {
                int desiredFirstHeight = (int)Math.Round(availableHeight * node.SplitRatio);
                firstHeight = ClampSplitSize(desiredFirstHeight, availableHeight, firstMinSize.Y, secondMinSize.Y);
            }

            int secondHeight = Math.Max(0, availableHeight - firstHeight);

            UiRect firstBounds = new(bounds.X, bounds.Y, bounds.Width, firstHeight);
            UiRect splitterBounds = new(bounds.X, firstBounds.Bottom, bounds.Width, splitterThickness);
            UiRect secondBounds = new(bounds.X, splitterBounds.Bottom, bounds.Width, secondHeight);

            node.FirstBounds = firstBounds;
            node.SecondBounds = secondBounds;
            node.SplitterBounds = splitterBounds;

            LayoutNode(node.First, firstBounds, UiDockCollapseEdge.Top);
            LayoutNode(node.Second, secondBounds, UiDockCollapseEdge.Bottom);
        }
        else
        {
            int availableWidth = Math.Max(0, bounds.Width - splitterThickness);
            int firstWidth;
            if (firstCollapsed)
            {
                firstWidth = Math.Min(availableWidth, GetCollapsedStripExtent());
            }
            else if (secondCollapsed)
            {
                firstWidth = Math.Max(0, availableWidth - Math.Min(availableWidth, GetCollapsedStripExtent()));
            }
            else
            {
                int desiredFirstWidth = (int)Math.Round(availableWidth * node.SplitRatio);
                firstWidth = ClampSplitSize(desiredFirstWidth, availableWidth, firstMinSize.X, secondMinSize.X);
            }

            int secondWidth = Math.Max(0, availableWidth - firstWidth);

            UiRect firstBounds = new(bounds.X, bounds.Y, firstWidth, bounds.Height);
            UiRect splitterBounds = new(firstBounds.Right, bounds.Y, splitterThickness, bounds.Height);
            UiRect secondBounds = new(splitterBounds.Right, bounds.Y, secondWidth, bounds.Height);

            node.FirstBounds = firstBounds;
            node.SecondBounds = secondBounds;
            node.SplitterBounds = splitterBounds;

            LayoutNode(node.First, firstBounds, UiDockCollapseEdge.Left);
            LayoutNode(node.Second, secondBounds, UiDockCollapseEdge.Right);
        }
    }

    private static void ConfigureCollapsedNode(DockNode node, UiRect bounds, UiDockCollapseEdge edge)
    {
        node.Bounds = bounds;
        node.CollapseEdge = edge;
        foreach (UiDockHost host in EnumerateHosts(node))
        {
            host.Bounds = default;
            host.ConfigureCollapsePresentation(
                collapsed: true,
                interactionEnabled: false,
                edge: edge);
        }
    }

    private int GetCollapsedStripExtent() => Math.Max(1, CollapsedStripSize);

    private UiPoint GetMinimumNodeSize(DockNode? node)
    {
        if (node == null)
        {
            return new UiPoint(0, 0);
        }

        if (node.IsCollapsed)
        {
            int strip = GetCollapsedStripExtent();
            return new UiPoint(strip, strip);
        }

        if (node.Host != null)
        {
            int paneSize = Math.Max(0, MinPaneSize);
            if (!RespectDockedWindowMinimums || node.Host.Windows.Count == 0)
            {
                return new UiPoint(paneSize, paneSize);
            }

            int minimumWidth = paneSize;
            int minimumHeight = paneSize;
            foreach (UiWindow window in node.Host.Windows)
            {
                minimumWidth = Math.Max(minimumWidth, Math.Max(0, window.MinSize.X));
                minimumHeight = Math.Max(minimumHeight, Math.Max(0, window.MinSize.Y));
            }

            return new UiPoint(minimumWidth, minimumHeight);
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

    private static bool TryFindDockNodeParent(
        DockNode current,
        DockNode target,
        out DockNode? parent,
        out bool targetWasFirst)
    {
        if (ReferenceEquals(current.First, target))
        {
            parent = current;
            targetWasFirst = true;
            return true;
        }

        if (ReferenceEquals(current.Second, target))
        {
            parent = current;
            targetWasFirst = false;
            return true;
        }

        if (current.First != null
            && TryFindDockNodeParent(current.First, target, out parent, out targetWasFirst))
        {
            return true;
        }

        if (current.Second != null
            && TryFindDockNodeParent(current.Second, target, out parent, out targetWasFirst))
        {
            return true;
        }

        parent = null;
        targetWasFirst = false;
        return false;
    }

    private static bool ContainsDockNodeReference(DockNode current, DockNode target)
    {
        return ReferenceEquals(current, target)
            || current.First != null && ContainsDockNodeReference(current.First, target)
            || current.Second != null && ContainsDockNodeReference(current.Second, target);
    }

    private bool ReplaceDockNodeReference(DockNode target, DockNode replacement)
    {
        if (ReferenceEquals(_rootNode, target))
        {
            _rootNode = replacement;
            return true;
        }

        return ReplaceDockNodeReference(_rootNode, target, replacement);
    }

    private static bool ReplaceDockNodeReference(DockNode current, DockNode target, DockNode replacement)
    {
        if (ReferenceEquals(current.First, target))
        {
            current.First = replacement;
            return true;
        }

        if (ReferenceEquals(current.Second, target))
        {
            current.Second = replacement;
            return true;
        }

        return current.First != null && ReplaceDockNodeReference(current.First, target, replacement)
            || current.Second != null && ReplaceDockNodeReference(current.Second, target, replacement);
    }

    private DockNode? ResolveCollapseNode(DockNode root, UiDockHost host)
    {
        List<DockNode> path = new();
        if (!TryBuildHostPath(root, host, path))
        {
            return null;
        }

        DockNode candidate = path[^1];
        if (ReferenceEquals(host, RootHost))
        {
            return candidate;
        }

        for (int index = path.Count - 2; index >= 0; index--)
        {
            DockNode ancestor = path[index];
            if (ContainsHost(ancestor, RootHost))
            {
                break;
            }

            candidate = ancestor;
        }

        return candidate;
    }

    private static DockNode? FindCollapsedAncestor(DockNode root, DockNode target)
    {
        List<DockNode> path = new();
        if (!TryBuildNodePath(root, target, path))
        {
            return null;
        }

        for (int index = 0; index < path.Count; index++)
        {
            if (path[index].IsCollapsed)
            {
                return path[index];
            }
        }

        return null;
    }

    private static bool TryBuildHostPath(DockNode node, UiDockHost host, List<DockNode> path)
    {
        path.Add(node);
        if (ReferenceEquals(node.Host, host))
        {
            return true;
        }

        if (node.First != null && TryBuildHostPath(node.First, host, path))
        {
            return true;
        }

        if (node.Second != null && TryBuildHostPath(node.Second, host, path))
        {
            return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    private static bool TryBuildNodePath(DockNode node, DockNode target, List<DockNode> path)
    {
        path.Add(node);
        if (ReferenceEquals(node, target))
        {
            return true;
        }

        if (node.First != null && TryBuildNodePath(node.First, target, path))
        {
            return true;
        }

        if (node.Second != null && TryBuildNodePath(node.Second, target, path))
        {
            return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    private bool CanCollapseNode(DockNode node)
    {
        foreach (UiDockHost host in EnumerateHosts(node))
        {
            if (!host.AllowCollapse || host.IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<UiDockHost> EnumerateHosts(DockNode node)
    {
        if (node.Host != null)
        {
            yield return node.Host;
            yield break;
        }

        if (node.First != null)
        {
            foreach (UiDockHost host in EnumerateHosts(node.First))
            {
                yield return host;
            }
        }

        if (node.Second != null)
        {
            foreach (UiDockHost host in EnumerateHosts(node.Second))
            {
                yield return host;
            }
        }
    }

    private DockNode? FindSplitterNode(DockNode node, UiPoint point)
    {
        if (node.IsCollapsed)
        {
            return null;
        }

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

        return node.First?.IsCollapsed == true || node.Second?.IsCollapsed == true
            ? null
            : node.SplitterBounds.Contains(point) ? node : null;
    }

    private DockTarget GetDockTarget(UiDockHost host, UiPoint point, bool inferEdgeTarget)
    {
        if (TryGetDockTargetRect(host.Bounds, point, out DockTarget target, out _))
        {
            return target;
        }

        if (inferEdgeTarget
            && !host.IsPointInTabBar(point)
            && TryGetEdgeDockTarget(host.Bounds, point, out DockTarget edgeTarget))
        {
            return edgeTarget;
        }

        return DockTarget.Center;
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
        int width = Math.Min(Math.Max(0, bounds.Width), Math.Max(0, container.Width));
        int height = Math.Min(Math.Max(0, bounds.Height), Math.Max(0, container.Height));
        bounds = new UiRect(bounds.X, bounds.Y, width, height);
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

    private bool CanDockWindow(UiWindow window, UiDockHost host, DockTarget target)
    {
        return target != DockTarget.None
            && (CanDockWindowPredicate?.Invoke(window, host, target) ?? true);
    }

    private static void PrepareDockedWindow(UiWindow window, UiDockHost host)
    {
        window.AllowDrag = false;
        window.AllowResize = false;
        window.ShowResizeGrip = false;
        if (host.HideDockedTitleBars)
        {
            window.ShowTitleBar = false;
        }
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

    private bool TryGetEdgeDockTarget(UiRect bounds, UiPoint point, out DockTarget target)
    {
        int horizontalBand = Math.Clamp(bounds.Width / 5, DropTargetSize, DropTargetSize * 3);
        int verticalBand = Math.Clamp(bounds.Height / 5, DropTargetSize, DropTargetSize * 3);

        int leftDistance = point.X - bounds.X;
        int rightDistance = bounds.Right - point.X;
        int topDistance = point.Y - bounds.Y;
        int bottomDistance = bounds.Bottom - point.Y;

        if (topDistance >= 0 && topDistance <= verticalBand)
        {
            target = DockTarget.Top;
            return true;
        }

        if (bottomDistance >= 0 && bottomDistance <= verticalBand)
        {
            target = DockTarget.Bottom;
            return true;
        }

        if (leftDistance >= 0 && leftDistance <= horizontalBand)
        {
            target = DockTarget.Left;
            return true;
        }

        if (rightDistance >= 0 && rightDistance <= horizontalBand)
        {
            target = DockTarget.Right;
            return true;
        }

        target = DockTarget.None;
        return false;
    }
}
