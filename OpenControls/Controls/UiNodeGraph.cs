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

            for (int i = 0; i < _graph._wires.Count; i++)
            {
                UiNodeWire wire = _graph._wires[i];
                UiColor color = _graph.ResolveWireColor(wire);
                DrawRoute(context.Renderer, wire.Route, _graph.ResolveWireThickness(wire), color);
            }

            if (_graph.PreviewWire.Active)
            {
                DrawRoute(context.Renderer, _graph.PreviewWire.Route, Math.Max(1, _graph.PreviewWireThickness), _graph.PreviewWireColor);
            }
        }

        private static void DrawRoute(IUiRenderer renderer, IReadOnlyList<UiPoint> route, int thickness, UiColor color)
        {
            if (route == null || route.Count < 2 || color.A == 0)
            {
                return;
            }

            int half = thickness / 2;
            if (route.Count >= 5)
            {
                DrawBezierRoute(renderer, route, thickness, color);
                return;
            }

            for (int i = 1; i < route.Count; i++)
            {
                UiPoint a = route[i - 1];
                UiPoint b = route[i];
                if (a.X == b.X)
                {
                    int top = Math.Min(a.Y, b.Y);
                    int height = Math.Max(1, Math.Abs(b.Y - a.Y));
                    renderer.FillRect(new UiRect(a.X - half, top, thickness, height), color);
                }
                else if (a.Y == b.Y)
                {
                    int left = Math.Min(a.X, b.X);
                    int width = Math.Max(1, Math.Abs(b.X - a.X));
                    renderer.FillRect(new UiRect(left, a.Y - half, width, thickness), color);
                }
                else
                {
                    DrawSteppedSegment(renderer, a, b, thickness, half, color);
                }
            }
        }

        private static void DrawSteppedSegment(IUiRenderer renderer, UiPoint a, UiPoint b, int thickness, int half, UiColor color)
        {
            UiPoint mid = new(b.X, a.Y);
            int left = Math.Min(a.X, mid.X);
            int width = Math.Max(1, Math.Abs(mid.X - a.X));
            renderer.FillRect(new UiRect(left, a.Y - half, width, thickness), color);

            int top = Math.Min(mid.Y, b.Y);
            int height = Math.Max(1, Math.Abs(b.Y - mid.Y));
            renderer.FillRect(new UiRect(b.X - half, top, thickness, height), color);
        }

        private static void DrawBezierRoute(IUiRenderer renderer, IReadOnlyList<UiPoint> route, int thickness, UiColor color)
        {
            UiPoint start = route[0];
            UiPoint startControl = route.Count > 1 ? route[1] : start;
            UiPoint endControl = route.Count > 2 ? route[^2] : route[^1];
            UiPoint end = route[^1];
            int steps = Math.Max(16, Math.Abs(end.X - start.X) / 12 + Math.Abs(end.Y - start.Y) / 16);
            UiPoint previous = start;
            for (int step = 1; step <= steps; step++)
            {
                float t = step / (float)steps;
                UiPoint current = Cubic(start, startControl, endControl, end, t);
                DrawLine(renderer, previous, current, thickness, color);
                previous = current;
            }
        }

        private static UiPoint Cubic(UiPoint a, UiPoint b, UiPoint c, UiPoint d, float t)
        {
            float inv = 1f - t;
            float x = inv * inv * inv * a.X
                + 3f * inv * inv * t * b.X
                + 3f * inv * t * t * c.X
                + t * t * t * d.X;
            float y = inv * inv * inv * a.Y
                + 3f * inv * inv * t * b.Y
                + 3f * inv * t * t * c.Y
                + t * t * t * d.Y;
            return new UiPoint((int)Math.Round(x), (int)Math.Round(y));
        }

        private static void DrawLine(IUiRenderer renderer, UiPoint a, UiPoint b, int thickness, UiColor color)
        {
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            int radius = Math.Max(1, thickness / 2);
            if (steps == 0)
            {
                UiRenderHelpers.FillCircle(renderer, a, radius, color);
                return;
            }

            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;
                int x = (int)Math.Round(a.X + dx * t);
                int y = (int)Math.Round(a.Y + dy * t);
                UiRenderHelpers.FillCircle(renderer, new UiPoint(x, y), radius, color);
            }
        }
    }

    private readonly UiCanvas _canvas = new();
    private readonly UiNodeWireLayer _wireLayer;
    private readonly List<UiNodeControl> _nodes = new();
    private readonly List<UiNodeWire> _wires = new();
    private UiNodeControl? _previewStartNode;
    private UiNodePin? _previewStartPin;

    public UiNodeGraph()
    {
        _wireLayer = new UiNodeWireLayer(this);
        _canvas.AddChild(_wireLayer);
        AddChild(_canvas);
    }

    public UiCanvas Canvas => _canvas;
    public IReadOnlyList<UiNodeControl> Nodes => _nodes;
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
        _canvas.AddChild(node);
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
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

        _canvas.RemoveChild(node);
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
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public bool RemoveWire(UiNodeWire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);

        bool removed = _wires.Remove(wire);
        if (removed)
        {
            Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Paint | UiInvalidationReason.State);
        }

        return removed;
    }

    public IReadOnlyList<UiNodeWireDebugLayout> GetWireDebugLayouts()
    {
        RefreshWireRoutes();
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
    }

    private void RefreshGraphState(UiInputState input)
    {
        RefreshWireRoutes();

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

    private void RefreshWireRoutes()
    {
        for (int i = 0; i < _wires.Count; i++)
        {
            _wires[i].RefreshRoute(ResolveWireThickness(_wires[i]));
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
            if (IsPointNearRoute(worldMouse, wire.Route, Math.Max(WireHitSlop, ResolveWireThickness(wire))))
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
