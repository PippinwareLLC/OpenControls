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

    }

    private sealed class UiNodeCommentLayer : UiElement
    {
        public override UiElement? HitTest(UiPoint point)
        {
            return null;
        }
    }

    private readonly UiCanvas _canvas = new();
    private readonly UiNodeWireLayer _wireLayer;
    private readonly UiNodeCommentLayer _commentLayer = new();
    private readonly List<UiNodeControl> _nodes = new();
    private readonly Dictionary<string, UiNodeControl> _nodesById = new(StringComparer.Ordinal);
    private readonly List<UiNodeCommentBox> _comments = new();
    private readonly Dictionary<string, UiNodeCommentBox> _commentsById = new(StringComparer.Ordinal);
    private readonly List<UiNodeWire> _wires = new();
    private readonly UiSelectionMarquee _selectionMarquee = new()
    {
        Id = "node-graph-selection-marquee",
        AutomationId = "node-graph-selection-marquee",
        AutomationName = "Selection Marquee",
        AutomationRole = "selection",
        Visible = false
    };
    private bool _wireRoutesDirty = true;
    private UiNodeControl? _previewStartNode;
    private UiNodePin? _previewStartPin;

    public UiNodeGraph()
    {
        _wireLayer = new UiNodeWireLayer(this);
        _canvas.AddChild(_wireLayer);
        _canvas.AddChild(_commentLayer);
        AddChild(_canvas);
        AddChild(_selectionMarquee);
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

    public UiRect? SelectionMarqueeBounds
    {
        get => _selectionMarquee.Visible ? _selectionMarquee.Bounds : null;
        set
        {
            if (value is { } bounds && bounds.Width > 0 && bounds.Height > 0)
            {
                _selectionMarquee.Bounds = bounds;
                _selectionMarquee.Visible = true;
            }
            else
            {
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

        if (!string.IsNullOrEmpty(node.Id) && ReferenceEquals(_nodesById.GetValueOrDefault(node.Id), node))
        {
            _nodesById.Remove(node.Id);
        }

        _canvas.RemoveChild(node);
        MarkWireRoutesDirty();
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
        return true;
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
                wire.Hovered);
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

        return wire.FromPin.Color ?? wire.ToPin.Color ?? DataWireColor;
    }

    private int ResolveWireThickness(UiNodeWire wire)
    {
        return Math.Max(1, wire.Kind == UiNodePinKind.Exec ? ExecWireThickness : DataWireThickness);
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
