namespace OpenControls.Controls;

public sealed class UiNodeGraph : UiElement, IUiDebugBoundsResolver
{
    private sealed class UiNodeWireLayer : UiElement
    {
        private readonly UiNodeGraph _graph;

        public UiNodeWireLayer(UiNodeGraph graph)
        {
            _graph = graph;
        }

        public override UiElement? HitTest(UiPoint point)
        {
            return null;
        }

        public override void Render(UiRenderContext context)
        {
            if (!Visible)
            {
                return;
            }

            _graph.EnsureWireRoutes();
            if (context.Renderer is IUiVectorPassRenderer vectorPassRenderer)
            {
                vectorPassRenderer.BeginVectorPass();
            }

            try
            {
                if (_graph.EnableWireShadows && _graph.WireShadowColor.A > 0)
                {
                    for (int i = 0; i < _graph._wires.Count; i++)
                    {
                        UiNodeWire wire = _graph._wires[i];
                        int thickness = _graph.ResolveWireThickness(wire);
                        int shadowThickness = _graph.ResolveWireShadowThickness(thickness);
                        DrawRoute(
                            context.Renderer,
                            wire.GetOffsetRenderRoute(thickness, _graph.WireShadowOffsetX, _graph.WireShadowOffsetY),
                            shadowThickness,
                            _graph.WireShadowColor);
                    }
                }

                if (_graph.EnableWireGlow)
                {
                    for (int i = 0; i < _graph._wires.Count; i++)
                    {
                        UiNodeWire wire = _graph._wires[i];
                        if (!_graph.ShouldShowWireGlow(wire))
                        {
                            continue;
                        }

                        int thickness = _graph.ResolveWireThickness(wire);
                        DrawGlowRoute(context.Renderer, wire.GetRenderRoute(thickness), thickness, _graph.ResolveWireGlowColor(wire), _graph);
                    }
                }

                for (int i = 0; i < _graph._wires.Count; i++)
                {
                    UiNodeWire wire = _graph._wires[i];
                    int thickness = _graph.ResolveWireThickness(wire);
                    UiColor color = _graph.ResolveWireColor(wire);
                    DrawRoute(context.Renderer, wire.GetRenderRoute(thickness), thickness, color);
                }

                if (_graph.PreviewWire.Active)
                {
                    DrawPreviewRoute(context.Renderer, _graph.PreviewWire.Route, Math.Max(1, _graph.PreviewWireThickness), _graph.PreviewWireColor);
                }

                for (int i = 0; i < _graph._wires.Count; i++)
                {
                    UiNodeWire wire = _graph._wires[i];
                    if (!_graph.ShouldShowRerouteHandles(wire))
                    {
                        continue;
                    }

                    int thickness = _graph.ResolveWireThickness(wire);
                    IReadOnlyList<UiPoint> centers = wire.GetRerouteHandleCenters(thickness);
                    for (int handleIndex = 0; handleIndex < centers.Count; handleIndex++)
                    {
                        UiPoint center = centers[handleIndex];
                        UiRenderHelpers.FillCircle(context.Renderer, center, _graph.RerouteHandleRadius, _graph.RerouteHandleFillColor);
                        UiRenderHelpers.DrawCircle(context.Renderer, center, _graph.RerouteHandleRadius, _graph.ResolveRerouteHandleBorderColor(wire), _graph.RerouteHandleBorderThickness);
                    }
                }
            }
            finally
            {
                if (context.Renderer is IUiVectorPassRenderer endVectorPassRenderer)
                {
                    endVectorPassRenderer.EndVectorPass();
                }
            }
        }

        private static void DrawRoute(IUiRenderer renderer, IReadOnlyList<UiPoint> route, int thickness, UiColor color)
        {
            if (route == null || route.Count < 2 || color.A == 0)
            {
                return;
            }

            UiRenderHelpers.DrawPolyline(renderer, route, Math.Max(1, thickness), color);
        }

        private static void DrawPreviewRoute(IUiRenderer renderer, IReadOnlyList<UiPoint> route, int thickness, UiColor color)
        {
            if (route == null || route.Count < 2 || color.A == 0)
            {
                return;
            }

            if (route.Count >= 5)
            {
                UiRenderHelpers.DrawPolyline(renderer, UiNodeWire.TessellateOpenGlBezierWire(route[0], route[^1]), Math.Max(1, thickness), color);
                return;
            }

            UiRenderHelpers.DrawPolyline(renderer, route, Math.Max(1, thickness), color);
        }

        private static void DrawGlowRoute(IUiRenderer renderer, IReadOnlyList<UiPoint> route, int thickness, UiColor color, UiNodeGraph graph)
        {
            if (route == null || route.Count < 2 || color.A == 0)
            {
                return;
            }

            int safePasses = Math.Max(1, graph.WireGlowPasses);
            int extraThickness = Math.Max(1, graph.WireGlowExtraThickness);
            for (int pass = safePasses; pass >= 1; pass--)
            {
                int glowThickness = Math.Max(1, thickness + extraThickness + (pass - 1) * graph.WireGlowSpreadStep);
                int intensity = safePasses - pass + 1;
                UiColor passColor = WithAlpha(color, color.A * intensity / (safePasses + 1));
                UiRenderHelpers.DrawPolyline(renderer, route, glowThickness, passColor);
            }
        }

        private static UiColor WithAlpha(UiColor color, int alpha)
        {
            return new UiColor(color.R, color.G, color.B, (byte)Math.Clamp(alpha, 0, 255));
        }

    }

    private sealed class UiNodeCommentLayer : UiElement
    {
        public override UiElement? HitTest(UiPoint point)
        {
            return null;
        }
    }

    private sealed class UiNodeOverlayLayer : UiElement
    {
        public UiNodeOverlayLayer()
        {
            ClipChildren = true;
        }

        public override UiElement? HitTest(UiPoint point)
        {
            return null;
        }
    }

    private readonly UiCanvas _canvas = new();
    private readonly UiNodeWireLayer _wireLayer;
    private readonly UiNodeCommentLayer _commentLayer = new();
    private readonly UiNodeOverlayLayer _overlayLayer = new();
    private readonly List<UiNodeControl> _nodes = new();
    private readonly Dictionary<string, UiNodeControl> _nodesById = new(StringComparer.Ordinal);
    private readonly List<UiNodeCommentBox> _comments = new();
    private readonly Dictionary<string, UiNodeCommentBox> _commentsById = new(StringComparer.Ordinal);
    private readonly List<UiNodeWire> _wires = new();
    private readonly Dictionary<UiNodeControl, UiRect> _nodeDragStartBounds = new();
    private readonly UiSelectionMarquee _selectionMarquee = new()
    {
        Id = "node-graph-selection-marquee",
        AutomationId = "node-graph-selection-marquee",
        AutomationName = "Selection Marquee",
        AutomationRole = "selection",
        Visible = false
    };
    private UiRect? _selectionMarqueeBounds;
    private bool _wireRoutesDirty = true;
    private UiNodeControl? _previewStartNode;
    private UiNodePin? _previewStartPin;

    public UiNodeGraph()
    {
        _wireLayer = new UiNodeWireLayer(this);
        _canvas.AddChild(_wireLayer);
        _canvas.AddChild(_commentLayer);
        AddChild(_canvas);
        _overlayLayer.AddChild(_selectionMarquee);
        AddChild(_overlayLayer);
    }

    public UiCanvas Canvas => _canvas;
    public IReadOnlyList<UiNodeControl> Nodes => _nodes;
    public IReadOnlyList<UiNodeCommentBox> Comments => _comments;
    public IReadOnlyList<UiNodeWire> Wires => _wires;
    public UiNodeControl? HoveredNode { get; private set; }
    public UiNodePin? HoveredPin { get; private set; }
    public UiNodeWire? HoveredWire { get; private set; }
    public UiNodeWirePreviewState PreviewWire { get; private set; } = UiNodeWirePreviewState.Inactive;
    public bool EnableWirePreview { get; set; } = true;
    public bool EnableWireSelection { get; set; } = true;
    public int WireHitSlop { get; set; } = 5;
    public int PreviewWireThickness { get; set; } = 3;
    public int DataWireThickness { get; set; } = 2;
    public int ExecWireThickness { get; set; } = 4;
    public UiColor DataWireColor { get; set; } = new(95, 170, 230);
    public UiColor ExecWireColor { get; set; } = UiColor.White;
    public UiColor HoverWireColor { get; set; } = new(230, 235, 245);
    public UiColor SelectedWireColor { get; set; } = new(245, 205, 110);
    public UiColor PreviewWireColor { get; set; } = new(220, 225, 235, 180);
    public bool EnableWireShadows { get; set; } = true;
    public UiColor WireShadowColor { get; set; } = new(0, 0, 0, 118);
    public int WireShadowOffsetX { get; set; } = 2;
    public int WireShadowOffsetY { get; set; } = 2;
    public int WireShadowExtraThickness { get; set; } = 2;
    public bool EnableWireRerouteHandles { get; set; } = true;
    public int RerouteHandleRadius { get; set; } = 5;
    public int RerouteHandleBorderThickness { get; set; } = 1;
    public UiColor RerouteHandleFillColor { get; set; } = new(16, 20, 27, 238);
    public UiColor RerouteHandleBorderColor { get; set; } = new(210, 220, 235, 235);
    public UiColor SelectedRerouteHandleBorderColor { get; set; } = new(255, 226, 126, 255);
    public bool EnableWireGlow { get; set; } = true;
    public UiColor SelectedWireGlowColor { get; set; } = new(255, 206, 92, 96);
    public UiColor HoverWireGlowColor { get; set; } = new(92, 184, 255, 72);
    public int WireGlowExtraThickness { get; set; } = 8;
    public int WireGlowSpreadStep { get; set; } = 3;
    public int WireGlowPasses { get; set; } = 3;

    public UiRect? SelectionMarqueeBounds
    {
        get => _selectionMarqueeBounds;
        set
        {
            if (value is { } bounds && bounds.Width > 0 && bounds.Height > 0)
            {
                _selectionMarqueeBounds = bounds;
                _selectionMarquee.Visible = true;
                UpdateSelectionMarqueeLayout();
            }
            else
            {
                _selectionMarqueeBounds = null;
                _selectionMarquee.Visible = false;
            }
        }
    }

    public float PanX
    {
        get => _canvas.PanX;
        set => _canvas.PanX = value;
    }

    public float PanY
    {
        get => _canvas.PanY;
        set => _canvas.PanY = value;
    }

    public float Zoom
    {
        get => _canvas.Zoom;
        set => _canvas.Zoom = value;
    }

    public event Action<UiNodeWirePreviewState>? WirePreviewStarted;
    public event Action<UiNodeWirePreviewState>? WirePreviewUpdated;
    public event Action<UiNodeWirePreviewState, UiNodePin?>? WirePreviewEnded;
    public event Action<UiNodeSelectionRequestedEvent>? NodeSelectionRequested;
    public event Action<UiNodeDragEvent>? NodeDragStarted;
    public event Action<UiNodeDragEvent>? NodeDragged;
    public event Action<UiNodeDragEvent>? NodeDragEnded;

    public void AddNode(UiNodeControl node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_nodes.Contains(node))
        {
            return;
        }

        _nodes.Add(node);
        if (!string.IsNullOrEmpty(node.Id))
        {
            _nodesById[node.Id] = node;
        }

        SubscribeNodeEvents(node);
        _canvas.AddChild(node);
        MarkWireRoutesDirty();
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public void AddCommentBox(UiNodeCommentBox comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        if (_comments.Contains(comment))
        {
            return;
        }

        _comments.Add(comment);
        if (!string.IsNullOrEmpty(comment.Id))
        {
            _commentsById[comment.Id] = comment;
        }

        _commentLayer.AddChild(comment);
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public bool TryGetNode(string id, out UiNodeControl? node)
    {
        if (string.IsNullOrEmpty(id))
        {
            node = null;
            return false;
        }

        return _nodesById.TryGetValue(id, out node);
    }

    public bool TryGetCommentBox(string id, out UiNodeCommentBox? comment)
    {
        if (string.IsNullOrEmpty(id))
        {
            comment = null;
            return false;
        }

        return _commentsById.TryGetValue(id, out comment);
    }

    public bool RemoveNode(UiNodeControl node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodes.Remove(node))
        {
            return false;
        }

        for (int i = _wires.Count - 1; i >= 0; i--)
        {
            UiNodeWire wire = _wires[i];
            if (wire.FromNode == node || wire.ToNode == node)
            {
                _wires.RemoveAt(i);
            }
        }

        UnsubscribeNodeEvents(node);
        _nodeDragStartBounds.Remove(node);
        if (!string.IsNullOrEmpty(node.Id) && ReferenceEquals(_nodesById.GetValueOrDefault(node.Id), node))
        {
            _nodesById.Remove(node.Id);
        }

        _canvas.RemoveChild(node);
        MarkWireRoutesDirty();
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
        return true;
    }

    private void SubscribeNodeEvents(UiNodeControl node)
    {
        node.DragStarted += HandleNodeDragStarted;
        node.Dragged += HandleNodeDragged;
        node.DragEnded += HandleNodeDragEnded;
    }

    private void UnsubscribeNodeEvents(UiNodeControl node)
    {
        node.DragStarted -= HandleNodeDragStarted;
        node.Dragged -= HandleNodeDragged;
        node.DragEnded -= HandleNodeDragEnded;
    }

    private void HandleNodeDragStarted(UiNodeControl node)
    {
        _nodeDragStartBounds[node] = node.Bounds;
        NodeDragStarted?.Invoke(CreateNodeDragEvent(node));
    }

    private void HandleNodeDragged(UiNodeControl node)
    {
        MarkWireRoutesDirty();
        NodeDragged?.Invoke(CreateNodeDragEvent(node));
    }

    private void HandleNodeDragEnded(UiNodeControl node)
    {
        MarkWireRoutesDirty();
        NodeDragEnded?.Invoke(CreateNodeDragEvent(node));
        _nodeDragStartBounds.Remove(node);
    }

    private UiNodeDragEvent CreateNodeDragEvent(UiNodeControl node)
    {
        UiRect startBounds = _nodeDragStartBounds.TryGetValue(node, out var storedStart)
            ? storedStart
            : node.Bounds;
        UiRect currentBounds = node.Bounds;
        UiPoint delta = new(currentBounds.X - startBounds.X, currentBounds.Y - startBounds.Y);
        return new UiNodeDragEvent(this, node, startBounds, currentBounds, delta);
    }

    public bool RemoveCommentBox(UiNodeCommentBox comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        if (!_comments.Remove(comment))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(comment.Id) && ReferenceEquals(_commentsById.GetValueOrDefault(comment.Id), comment))
        {
            _commentsById.Remove(comment.Id);
        }

        _commentLayer.RemoveChild(comment);
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
        return true;
    }

    public UiNodeWire Connect(UiNodeControl fromNode, UiNodePin fromPin, UiNodeControl toNode, UiNodePin toPin)
    {
        if (fromPin.Direction != UiNodePinDirection.Output)
        {
            throw new ArgumentException("The source pin must be an output pin.", nameof(fromPin));
        }

        if (toPin.Direction != UiNodePinDirection.Input)
        {
            throw new ArgumentException("The target pin must be an input pin.", nameof(toPin));
        }

        UiNodeWire wire = new(fromNode, fromPin, toNode, toPin);
        AddWire(wire);
        return wire;
    }

    public void AddWire(UiNodeWire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);

        if (_wires.Contains(wire))
        {
            return;
        }

        _wires.Add(wire);
        MarkWireRoutesDirty();
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public bool RemoveWire(UiNodeWire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);

        bool removed = _wires.Remove(wire);
        if (removed)
        {
            MarkWireRoutesDirty();
            Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Paint | UiInvalidationReason.State);
        }

        return removed;
    }

    public IReadOnlyList<UiNodeWireDebugLayout> GetWireDebugLayouts()
    {
        EnsureWireRoutes();
        UiNodeWireDebugLayout[] layouts = new UiNodeWireDebugLayout[_wires.Count];
        for (int i = 0; i < _wires.Count; i++)
        {
            UiNodeWire wire = _wires[i];
            int thickness = ResolveWireThickness(wire);
            int shadowThickness = ResolveWireShadowThickness(thickness);
            IReadOnlyList<UiPoint> shadowRoute = EnableWireShadows
                ? wire.GetOffsetRenderRoute(thickness, WireShadowOffsetX, WireShadowOffsetY)
                : Array.Empty<UiPoint>();
            IReadOnlyList<UiPoint> handleCenters = ShouldShowRerouteHandles(wire)
                ? wire.GetRerouteHandleCenters(thickness)
                : Array.Empty<UiPoint>();
            IReadOnlyList<UiRect> handleBounds = BuildRerouteHandleBounds(handleCenters, RerouteHandleRadius);
            UiColor glowColor = ShouldShowWireGlow(wire) ? ResolveWireGlowColor(wire) : default;
            int glowThickness = ShouldShowWireGlow(wire) ? ResolveWireGlowThickness(thickness) : 0;
            UiRect hitBounds = ExpandRect(wire.Bounds, Math.Max(WireHitSlop, thickness));
            layouts[i] = new UiNodeWireDebugLayout(
                wire,
                wire.Route,
                wire.Bounds,
                hitBounds,
                wire.Kind,
                thickness,
                ResolveWireColor(wire),
                wire.Selected,
                wire.Hovered,
                EnableWireShadows ? UiNodeWire.CalculateBounds(shadowRoute, shadowThickness) : default,
                EnableWireShadows ? WireShadowColor : default,
                EnableWireShadows ? shadowThickness : 0,
                handleCenters,
                handleBounds,
                ShouldShowWireGlow(wire) ? UiNodeWire.CalculateBounds(wire.GetRenderRoute(thickness), glowThickness) : default,
                glowColor,
                glowThickness);
        }

        return layouts;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UpdateCanvasLayout();
        base.Update(context);
        RefreshGraphState(context.GetSelfInput(this));
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UpdateCanvasLayout();
        EnsureWireRoutes();
        base.Render(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UpdateCanvasLayout();
        base.RenderOverlay(context);
    }

    bool IUiDebugBoundsResolver.TryResolveDebugBounds(UiElement element, out UiRect bounds, out UiRect clipBounds)
    {
        return ((IUiDebugBoundsResolver)_canvas).TryResolveDebugBounds(element, out bounds, out clipBounds);
    }

    private void UpdateCanvasLayout()
    {
        _canvas.Bounds = Bounds;
        _wireLayer.Bounds = default;
        _commentLayer.Bounds = default;
        _overlayLayer.Bounds = Bounds;
        UpdateSelectionMarqueeLayout();
    }

    private void UpdateSelectionMarqueeLayout()
    {
        if (_selectionMarqueeBounds is not { } bounds)
        {
            return;
        }

        _selectionMarquee.Bounds = new UiRect(
            Bounds.X + bounds.X,
            Bounds.Y + bounds.Y,
            bounds.Width,
            bounds.Height);
    }

    private void RefreshGraphState(UiInputState input)
    {
        EnsureWireRoutes();

        HoveredNode = null;
        HoveredPin = null;
        HoveredWire = null;
        for (int i = 0; i < _wires.Count; i++)
        {
            _wires[i].Hovered = false;
        }

        bool mouseInViewport = _canvas.ViewportBounds.Contains(input.MousePosition);
        UiPoint worldMouse = mouseInViewport ? _canvas.ScreenToWorld(input.MousePosition) : default;
        if (mouseInViewport)
        {
            RefreshHoverState(worldMouse);
        }

        RefreshPreviewState(input, mouseInViewport, worldMouse);
        if (input.LeftClicked && HoveredNode != null && HoveredPin == null)
        {
            NodeSelectionRequested?.Invoke(new UiNodeSelectionRequestedEvent(this, HoveredNode, input.Modifiers));
        }

        if (EnableWireSelection && input.LeftClicked && HoveredWire != null && HoveredPin == null)
        {
            for (int i = 0; i < _wires.Count; i++)
            {
                _wires[i].Selected = false;
            }

            HoveredWire.Selected = true;
        }
    }

    private void MarkWireRoutesDirty()
    {
        _wireRoutesDirty = true;
    }

    private void EnsureWireRoutes()
    {
        if (!_wireRoutesDirty && !AnyWireNeedsRouteRefresh())
        {
            return;
        }

        RefreshWireRoutes();
    }

    private bool AnyWireNeedsRouteRefresh()
    {
        for (int i = 0; i < _wires.Count; i++)
        {
            UiNodeWire wire = _wires[i];
            if (wire.NeedsRouteRefresh(ResolveWireThickness(wire)))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshWireRoutes()
    {
        bool anyChanged = false;
        for (int i = 0; i < _wires.Count; i++)
        {
            anyChanged |= _wires[i].RefreshRoute(ResolveWireThickness(_wires[i]));
        }

        _wireRoutesDirty = false;
        if (anyChanged)
        {
            _wireLayer.Invalidate(UiInvalidationReason.Paint | UiInvalidationReason.State);
        }
    }

    private void RefreshHoverState(UiPoint worldMouse)
    {
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            UiNodeControl node = _nodes[i];
            if (!node.Visible || !node.Enabled)
            {
                continue;
            }

            if (HoveredPin == null && node.TryGetPinAt(worldMouse, out UiNodePin? pin))
            {
                HoveredPin = pin;
                HoveredNode = node;
                break;
            }

            if (HoveredNode == null && node.HitTest(worldMouse) == node)
            {
                HoveredNode = node;
            }
        }

        if (HoveredPin != null)
        {
            return;
        }

        for (int i = _wires.Count - 1; i >= 0; i--)
        {
            UiNodeWire wire = _wires[i];
            int thickness = ResolveWireThickness(wire);
            if (IsPointNearRoute(worldMouse, wire.GetRenderRoute(thickness), Math.Max(WireHitSlop, thickness)))
            {
                HoveredWire = wire;
                wire.Hovered = true;
                return;
            }
        }
    }

    private void RefreshPreviewState(UiInputState input, bool mouseInViewport, UiPoint worldMouse)
    {
        if (!EnableWirePreview)
        {
            ClearPreview();
            return;
        }

        if (_previewStartPin == null && input.LeftClicked && HoveredPin != null && HoveredNode != null)
        {
            _previewStartNode = HoveredNode;
            _previewStartPin = HoveredPin;
            PreviewWire = BuildPreviewState(mouseInViewport ? worldMouse : HoveredPin.Layout.Center);
            WirePreviewStarted?.Invoke(PreviewWire);
        }

        if (_previewStartPin == null)
        {
            PreviewWire = UiNodeWirePreviewState.Inactive;
            return;
        }

        UiPoint previewEnd = mouseInViewport ? worldMouse : _previewStartPin.Layout.Center;
        PreviewWire = BuildPreviewState(previewEnd);
        WirePreviewUpdated?.Invoke(PreviewWire);

        if (input.LeftReleased)
        {
            UiNodePin? targetPin = HoveredPin != _previewStartPin ? HoveredPin : null;
            UiNodeWirePreviewState endedState = PreviewWire;
            ClearPreview();
            WirePreviewEnded?.Invoke(endedState, targetPin);
        }
    }

    private UiNodeWirePreviewState BuildPreviewState(UiPoint end)
    {
        if (_previewStartNode == null || _previewStartPin == null)
        {
            return UiNodeWirePreviewState.Inactive;
        }

        UiPoint[] route = UiNodeWire.BuildPreviewRoute(_previewStartPin, end);
        return new UiNodeWirePreviewState(true, _previewStartNode, _previewStartPin, end, route);
    }

    private void ClearPreview()
    {
        _previewStartNode = null;
        _previewStartPin = null;
        PreviewWire = UiNodeWirePreviewState.Inactive;
    }

    private UiColor ResolveWireColor(UiNodeWire wire)
    {
        if (wire.Selected)
        {
            return SelectedWireColor;
        }

        if (wire.Hovered)
        {
            return HoverWireColor;
        }

        if (wire.Kind == UiNodePinKind.Exec)
        {
            return ExecWireColor;
        }

        return wire.Color ?? wire.FromPin.Color ?? wire.ToPin.Color ?? DataWireColor;
    }

    private int ResolveWireThickness(UiNodeWire wire)
    {
        return Math.Max(1, wire.Kind == UiNodePinKind.Exec ? ExecWireThickness : DataWireThickness);
    }

    private int ResolveWireShadowThickness(int thickness)
    {
        if (!EnableWireShadows || WireShadowColor.A == 0)
        {
            return 0;
        }

        return Math.Max(1, thickness + Math.Max(0, WireShadowExtraThickness));
    }

    private bool ShouldShowWireGlow(UiNodeWire wire)
    {
        return EnableWireGlow
            && WireGlowPasses > 0
            && WireGlowExtraThickness > 0
            && (wire.Selected || wire.Hovered)
            && ResolveWireGlowColor(wire).A > 0;
    }

    private UiColor ResolveWireGlowColor(UiNodeWire wire)
    {
        return wire.Selected ? SelectedWireGlowColor : (wire.Hovered ? HoverWireGlowColor : default);
    }

    private int ResolveWireGlowThickness(int thickness)
    {
        int passes = Math.Max(1, WireGlowPasses);
        return Math.Max(1, thickness + Math.Max(1, WireGlowExtraThickness) + (passes - 1) * Math.Max(0, WireGlowSpreadStep));
    }

    private bool ShouldShowRerouteHandles(UiNodeWire wire)
    {
        return EnableWireRerouteHandles
            && RerouteHandleRadius > 0
            && (wire.Selected || wire.Hovered);
    }

    private UiColor ResolveRerouteHandleBorderColor(UiNodeWire wire)
    {
        return wire.Selected ? SelectedRerouteHandleBorderColor : RerouteHandleBorderColor;
    }

    private static IReadOnlyList<UiRect> BuildRerouteHandleBounds(IReadOnlyList<UiPoint> centers, int radius)
    {
        if (centers.Count == 0 || radius <= 0)
        {
            return Array.Empty<UiRect>();
        }

        UiRect[] bounds = new UiRect[centers.Count];
        for (int i = 0; i < centers.Count; i++)
        {
            UiPoint center = centers[i];
            bounds[i] = new UiRect(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        }

        return bounds;
    }

    private static bool IsPointNearRoute(UiPoint point, IReadOnlyList<UiPoint> route, int slop)
    {
        if (route == null || route.Count < 2)
        {
            return false;
        }

        int safeSlop = Math.Max(0, slop);
        for (int i = 1; i < route.Count; i++)
        {
            if (IsPointNearSegment(point, route[i - 1], route[i], safeSlop))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointNearSegment(UiPoint point, UiPoint a, UiPoint b, int slop)
    {
        int left = Math.Min(a.X, b.X) - slop;
        int right = Math.Max(a.X, b.X) + slop;
        int top = Math.Min(a.Y, b.Y) - slop;
        int bottom = Math.Max(a.Y, b.Y) + slop;
        if (point.X < left || point.X > right || point.Y < top || point.Y > bottom)
        {
            return false;
        }

        if (a.X == b.X)
        {
            return Math.Abs(point.X - a.X) <= slop;
        }

        if (a.Y == b.Y)
        {
            return Math.Abs(point.Y - a.Y) <= slop;
        }

        int dx = b.X - a.X;
        int dy = b.Y - a.Y;
        int numerator = Math.Abs(dy * point.X - dx * point.Y + b.X * a.Y - b.Y * a.X);
        double distance = numerator / Math.Sqrt(dx * dx + dy * dy);
        return distance <= slop;
    }

    private static UiRect ExpandRect(UiRect rect, int padding)
    {
        int safePadding = Math.Max(0, padding);
        return new UiRect(
            rect.X - safePadding,
            rect.Y - safePadding,
            rect.Width + safePadding * 2,
            rect.Height + safePadding * 2);
    }
}

public sealed record UiNodeSelectionRequestedEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiModifierKeys Modifiers);

public sealed record UiNodeDragEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiRect StartBounds,
    UiRect CurrentBounds,
    UiPoint Delta);
