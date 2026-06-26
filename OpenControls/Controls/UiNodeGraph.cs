namespace OpenControls.Controls;

public sealed class UiNodeGraph : UiElement, IUiDebugBoundsResolver
{
    private const int ValueEditorMaxLength = 2048;
    private const int CommentEditorMaxLength = 4096;
    private const int NodeSearchMaxLength = 128;
    private const int NodeSearchMaxResults = 256;
    private const int NodeSearchPopupWidth = 500;
    private const int NodeSearchHeaderHeight = 38;
    private const int NodeSearchResultHeight = 28;
    private const int NodeSearchCategoryHeight = 22;
    private const int NodeSearchInputHeight = 30;
    private const int NodeSearchTextScale = 1;
    private const int NodeSearchMaxRowsHeight = 390;
    private const int NodeSearchScrollbarWidth = 8;
    private const int NodeSearchScrollWheelStep = 44;

    private enum ValueEditAction
    {
        None,
        Commit,
        Cancel
    }

    private enum CommentEditField
    {
        None,
        Title,
        Body
    }

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
    private readonly UiTextEditingState _valueEditingState = new();
    private readonly UiTextEditingState _commentEditingState = new();
    private readonly UiTextEditingState _nodeSearchEditingState = new();
    private readonly List<UiNodeControl> _nodes = new();
    private readonly Dictionary<string, UiNodeControl> _nodesById = new(StringComparer.Ordinal);
    private readonly List<UiNodeCommentBox> _comments = new();
    private readonly Dictionary<string, UiNodeCommentBox> _commentsById = new(StringComparer.Ordinal);
    private readonly List<UiNodeWire> _wires = new();
    private readonly List<UiNodeSearchItem> _nodeSearchItems = new();
    private readonly List<UiNodeSearchDisplayRow> _nodeSearchDisplayRows = new();
    private readonly HashSet<string> _nodeSearchExpandedCategories = new(StringComparer.OrdinalIgnoreCase);
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
    private UiNodeControl? _editingValueNode;
    private UiNodePin? _editingValuePin;
    private UiNodeCommentBox? _editingComment;
    private CommentEditField _editingCommentField;
    private UiTextCompositionState _valueComposition;
    private UiTextCompositionState _commentComposition;
    private UiTextCompositionState _nodeSearchComposition;
    private bool _nodeSearchOpen;
    private UiPoint _nodeSearchScreenPosition;
    private UiPoint _nodeSearchWorldPosition;
    private UiModifierKeys _nodeSearchModifiers;
    private UiRect _nodeSearchBounds;
    private UiRect _nodeSearchInputBounds;
    private UiRect _nodeSearchRowsViewportBounds;
    private UiRect _debugPinValueBounds;
    private string _debugPinValueText = string.Empty;
    private UiNodePin? _debugPinValuePin;
    private UiNodeControl? _nodeSearchContextNode;
    private UiNodePin? _nodeSearchContextPin;
    private int _nodeSearchSelectedIndex;
    private int _nodeSearchScrollY;
    private bool _nodeSearchHandledInputThisFrame;
    private bool _boxSelectionArmed;
    private UiPoint _boxSelectionStartScreen;
    private UiPoint _boxSelectionStartGraphLocal;
    private UiPoint _boxSelectionStartWorld;
    private UiModifierKeys _boxSelectionModifiers;
    private bool _commentDragArmed;
    private UiNodeCommentBox? _commentDragComment;
    private UiPoint _commentDragStartScreen;
    private UiPoint _commentDragStartWorld;
    private UiRect _commentDragStartBounds;
    private UiModifierKeys _commentDragModifiers;
    private UiFont _valueEditFont = UiFont.Default;
    private UiFont _commentEditFont = UiFont.Default;
    private bool _valueCaretVisible = true;
    private bool _commentCaretVisible = true;
    private bool _valueDragSelecting;
    private float _valueCaretTimer;
    private float _commentCaretTimer;
    private int _valueDragSelectionAnchor;
    private int _valueHorizontalScrollOffset;
    private ValueEditAction _pendingValueEditAction;
    private ValueEditAction _pendingCommentEditAction;

    public UiNodeGraph()
    {
        _wireLayer = new UiNodeWireLayer(this);
        _canvas.PanButton = UiCanvas.UiCanvasPanButton.Middle;
        _canvas.PanWithSpaceLeftButton = true;
        _canvas.ViewportChanged += HandleCanvasViewportChanged;
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
    public UiNodePin? HoveredValuePin { get; private set; }
    public UiNodeCommentBox? HoveredComment { get; private set; }
    public UiNodeWire? HoveredWire { get; private set; }
    public UiNodeWirePreviewState PreviewWire { get; private set; } = UiNodeWirePreviewState.Inactive;
    public bool IsEditingValue => _editingValueNode is not null && _editingValuePin is not null;
    public bool IsEditingComment => _editingComment is not null && _editingCommentField != CommentEditField.None;
    public bool IsEditingText => IsEditingValue || IsEditingComment;
    public bool IsNodeSearchOpen => _nodeSearchOpen;
    public string NodeSearchQuery => _nodeSearchEditingState.Text;
    public IReadOnlyList<UiNodeSearchItem> NodeSearchItems => _nodeSearchItems;
    public IReadOnlyList<UiNodeSearchDisplayRow> NodeSearchDisplayRows => _nodeSearchDisplayRows;
    public int NodeSearchSelectedIndex => _nodeSearchSelectedIndex;
    public UiRect NodeSearchPopupBounds => _nodeSearchBounds;
    public UiRect NodeSearchRowsViewportBounds => _nodeSearchRowsViewportBounds;
    public UiPoint NodeSearchWorldPosition => _nodeSearchWorldPosition;
    public UiNodeControl? NodeSearchContextNode => _nodeSearchContextNode;
    public UiNodePin? NodeSearchContextPin => _nodeSearchContextPin;
    public int NodeSearchScrollY => _nodeSearchScrollY;
    public UiRect DebugPinValuePopupBounds => _debugPinValueBounds;
    public string DebugPinValuePopupText => _debugPinValueText;
    public UiNodePin? DebugPinValuePopupPin => _debugPinValuePin;
    public override bool IsFocusable => true;
    public override bool WantsTextInput => IsEditingText || IsNodeSearchOpen;
    public bool EnableBoxSelection { get; set; } = true;
    public bool IsBoxSelectionPointerActive => _boxSelectionArmed || IsBoxSelecting;
    public bool IsBoxSelecting { get; private set; }
    public UiRect? SelectionMarqueeWorldBounds { get; private set; }
    public bool EnableCommentDragging { get; set; } = true;
    public bool IsDraggingComment { get; private set; }
    public bool EnableWirePreview { get; set; } = true;
    public bool EnableWireSelection { get; set; } = true;
    public bool EnableNodeSearch { get; set; } = true;
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
    public UiColor NodeSearchBackground { get; set; } = new(18, 22, 30, 246);
    public UiColor NodeSearchBorder { get; set; } = new(82, 112, 148, 230);
    public UiColor NodeSearchInputBackground { get; set; } = new(11, 15, 22, 255);
    public UiColor NodeSearchInputBorder { get; set; } = new(56, 78, 104, 255);
    public UiColor NodeSearchTextColor { get; set; } = new(235, 245, 255, 255);
    public UiColor NodeSearchPlaceholderColor { get; set; } = new(116, 138, 164, 255);
    public UiColor NodeSearchSelectedBackground { get; set; } = new(36, 92, 142, 186);
    public UiColor NodeSearchSelectedBorder { get; set; } = new(92, 166, 255, 200);
    public UiColor NodeSearchCategoryColor { get; set; } = new(130, 172, 216, 255);
    public string NodeSearchTitle { get; set; } = "All Actions for this Blueprint";
    public string NodeSearchPlaceholder { get; set; } = "Search";
    public bool NodeSearchContextSensitive { get; set; } = true;
    public UiColor DebugPinValueBackground { get; set; } = new(7, 10, 16, 238);
    public UiColor DebugPinValueBorder { get; set; } = new(92, 166, 255, 220);
    public UiColor DebugPinValueTextColor { get; set; } = new(230, 244, 255, 255);

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
    public event Action<UiNodeWireConnectionRequestedEvent>? WireConnectionRequested;
    public event Action<UiNodeWireRerouteRequestedEvent>? WireRerouteRequested;
    public event Action<UiNodeSelectionRequestedEvent>? NodeSelectionRequested;
    public event Action<UiNodeDoubleClickedEvent>? NodeDoubleClicked;
    public event Action<UiNodeClickCompletedEvent>? NodeClickCompleted;
    public event Action<UiNodeDragEvent>? NodeDragStarted;
    public event Action<UiNodeDragEvent>? NodeDragged;
    public event Action<UiNodeDragEvent>? NodeDragEnded;
    public event Action<UiNodeValueEditStartedEvent>? ValueEditStarted;
    public event Action<UiNodeValueEditCommittedEvent>? ValueEditCommitted;
    public event Action<UiNodeValueEditCancelledEvent>? ValueEditCancelled;
    public event Action<UiNodeCommentEditStartedEvent>? CommentEditStarted;
    public event Action<UiNodeCommentEditCommittedEvent>? CommentEditCommitted;
    public event Action<UiNodeCommentEditCancelledEvent>? CommentEditCancelled;
    public event Action<UiNodeCommentDragEvent>? CommentDragStarted;
    public event Action<UiNodeCommentDragEvent>? CommentDragged;
    public event Action<UiNodeCommentDragEvent>? CommentDragEnded;
    public event Action<UiNodeCommentDragEvent>? CommentDragCancelled;
    public event Action<UiNodeBoxSelectionEvent>? BoxSelectionStarted;
    public event Action<UiNodeBoxSelectionEvent>? BoxSelectionUpdated;
    public event Action<UiNodeBoxSelectionEvent>? BoxSelectionEnded;
    public event Action<UiNodeBoxSelectionEvent>? BoxSelectionCancelled;
    public event Action<UiNodeGraphViewportChangedEvent>? ViewportChanged;
    public event Action<UiNodeGraphCommandRequestedEvent>? CommandRequested;
    public event Action<UiNodeSearchRequestedEvent>? NodeSearchRequested;
    public event Action<UiNodeSearchQueryChangedEvent>? NodeSearchQueryChanged;
    public event Action<UiNodeSearchItemInvokedEvent>? NodeSearchItemInvoked;
    public event Action<UiNodeSearchClosedEvent>? NodeSearchClosed;

    public override bool IsRenderCacheVolatile(UiContext context)
    {
        return (IsEditingText || IsNodeSearchOpen) && ReferenceEquals(context.Focus.Focused, this);
    }

    public void SetNodeSearchResults(IEnumerable<UiNodeSearchItem> items, int selectedIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(items);
        _nodeSearchItems.Clear();
        _nodeSearchItems.AddRange(items.Where(static item => !string.IsNullOrWhiteSpace(item.Id)).Take(NodeSearchMaxResults));
        RebuildNodeSearchDisplayRows();
        _nodeSearchSelectedIndex = ClampNodeSearchIndex(selectedIndex);
        EnsureNodeSearchSelectionVisible();
        UpdateNodeSearchBounds();
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public IReadOnlyList<UiNodeSearchDebugRow> GetNodeSearchDebugRows()
    {
        if (!IsNodeSearchOpen)
        {
            return [];
        }

        RebuildNodeSearchDisplayRows();
        var rows = new List<UiNodeSearchDebugRow>(_nodeSearchDisplayRows.Count);
        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            var row = _nodeSearchDisplayRows[rowIndex];
            rows.Add(new UiNodeSearchDebugRow(
                row.Kind,
                row.Text,
                row.Category,
                row.ItemIndex >= 0 && row.ItemIndex < _nodeSearchItems.Count ? _nodeSearchItems[row.ItemIndex].Id : "",
                ResolveNodeSearchDisplayRowBounds(rowIndex),
                row.ItemIndex >= 0 && row.ItemIndex == ClampNodeSearchIndex(_nodeSearchSelectedIndex)));
        }

        if (rows.Count == 0)
        {
            rows.Add(new UiNodeSearchDebugRow("empty", "No matching nodes", "", "", ResolveNodeSearchDisplayRowBounds(0), false));
        }

        return rows;
    }

    public void CloseNodeSearch()
    {
        CloseNodeSearch(raiseEvent: true);
    }

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

        if (ReferenceEquals(_editingValueNode, node))
        {
            CancelValueEdit();
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
        node.ClickCompleted += HandleNodeClickCompleted;
        node.DragStarted += HandleNodeDragStarted;
        node.Dragged += HandleNodeDragged;
        node.DragEnded += HandleNodeDragEnded;
    }

    private void UnsubscribeNodeEvents(UiNodeControl node)
    {
        node.ClickCompleted -= HandleNodeClickCompleted;
        node.DragStarted -= HandleNodeDragStarted;
        node.Dragged -= HandleNodeDragged;
        node.DragEnded -= HandleNodeDragEnded;
    }

    private void HandleNodeClickCompleted(UiNodeControl node, UiModifierKeys modifiers)
    {
        NodeClickCompleted?.Invoke(new UiNodeClickCompletedEvent(this, node, modifiers));
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

        if (ReferenceEquals(_editingComment, comment))
        {
            CancelCommentEdit();
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
            CancelCommentDrag(_commentDragModifiers);
            CancelBoxSelection(_boxSelectionModifiers);
            return;
        }

        UpdateCanvasLayout();
        _nodeSearchHandledInputThisFrame = false;
        UiInputState graphInput = context.GetSelfInput(this);
        if (!IsEditingText
            && graphInput.LeftClicked
            && _canvas.ViewportBounds.Contains(graphInput.MousePosition))
        {
            context.Focus.RequestFocus(this);
        }

        if (IsEditingValue && ReferenceEquals(context.Focus.Focused, this))
        {
            _valueEditFont = ResolveFont(context.DefaultFont);
            HandleValueEditInput(context, graphInput, _valueEditFont);
        }
        else if (IsEditingComment && ReferenceEquals(context.Focus.Focused, this))
        {
            _commentEditFont = ResolveFont(context.DefaultFont);
            HandleCommentEditInput(context, graphInput);
        }
        else if (IsNodeSearchOpen && ReferenceEquals(context.Focus.Focused, this))
        {
            HandleNodeSearchInput(context, graphInput);
        }

        base.Update(context);
        RefreshNodeLayouts(context.DefaultFont);
        RefreshGraphState(context, graphInput);
        ProcessValueEditorState(context);
        ProcessCommentEditorState(context);
        ProcessGraphCommandInput(context, graphInput);
    }

    protected internal override void OnFocusLost()
    {
        CancelCommentDrag(_commentDragModifiers);
        CancelBoxSelection(_boxSelectionModifiers);
        CloseNodeSearch(raiseEvent: true);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UpdateCanvasLayout();
        RefreshNodeLayouts(context.DefaultFont);
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
        DrawDebugPinValuePopup(context);
        DrawNodeSearchPopup(context);
    }

    bool IUiDebugBoundsResolver.TryResolveDebugBounds(UiElement element, out UiRect bounds, out UiRect clipBounds)
    {
        return ((IUiDebugBoundsResolver)_canvas).TryResolveDebugBounds(element, out bounds, out clipBounds);
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        if (IsEditingValue
            && _editingValuePin is { } pin
            && pin.Layout.ValueBounds.Width > 0
            && pin.Layout.ValueBounds.Height > 0
            && pin.Layout.ValueBounds.Contains(_canvas.ScreenToWorld(input.MousePosition)))
        {
            cursor = UiMouseCursor.TextInput;
            return true;
        }

        if (IsEditingComment
            && _editingComment is { } comment
            && ResolveCommentEditBounds(comment, _editingCommentField) is { Width: > 0, Height: > 0 } commentBounds
            && commentBounds.Contains(_canvas.ScreenToWorld(input.MousePosition)))
        {
            cursor = UiMouseCursor.TextInput;
            return true;
        }

        if (IsNodeSearchOpen && _nodeSearchInputBounds.Contains(input.MousePosition))
        {
            cursor = UiMouseCursor.TextInput;
            return true;
        }

        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal override bool TryGetTextInputRequest(out UiTextInputRequest request)
    {
        request = default;
        if (IsEditingValue && _editingValueNode is not null && _editingValuePin is not null)
        {
            UiRect valueBounds = _editingValuePin.Layout.ValueBounds;
            if (valueBounds.Width <= 0 || valueBounds.Height <= 0)
            {
                return false;
            }

            UiRect screenBounds = _canvas.WorldToScreen(valueBounds);
            UiRect caretBounds = _canvas.WorldToScreen(GetValueEditCaretBounds(_editingValueNode, _editingValuePin, _valueEditFont));
            UiRect candidateBounds = caretBounds;
            if (_valueComposition.IsActive)
            {
                int compositionWidth = Math.Max(
                    caretBounds.Width,
                    (int)MathF.Round(_valueEditFont.MeasureTextWidth(_valueComposition.Text, _editingValueNode.TextScale) * Math.Max(_canvas.Zoom, 0.0001f)));
                candidateBounds = new UiRect(caretBounds.X, caretBounds.Y, Math.Max(1, compositionWidth), caretBounds.Height);
            }

            request = new UiTextInputRequest(screenBounds, isMultiLine: false, caretBounds: caretBounds, candidateBounds: candidateBounds);
            return true;
        }

        if (IsEditingComment && _editingComment is { } comment)
        {
            UiRect fieldBounds = ResolveCommentEditBounds(comment, _editingCommentField);
            if (fieldBounds.Width <= 0 || fieldBounds.Height <= 0)
            {
                return false;
            }

            UiRect screenBounds = _canvas.WorldToScreen(fieldBounds);
            UiRect caretBounds = screenBounds;
            if (_commentComposition.IsActive)
            {
                caretBounds = new UiRect(screenBounds.X, screenBounds.Y, Math.Max(1, screenBounds.Width), screenBounds.Height);
            }

            request = new UiTextInputRequest(screenBounds, _editingCommentField == CommentEditField.Body, caretBounds, caretBounds);
            return true;
        }

        if (IsNodeSearchOpen)
        {
            UpdateNodeSearchBounds();
            if (_nodeSearchInputBounds.Width <= 0 || _nodeSearchInputBounds.Height <= 0)
            {
                return false;
            }

            int textX = _nodeSearchInputBounds.X + 8 + ResolveNodeSearchCaretX(ResolveFont(UiFont.Default));
            var caretBounds = new UiRect(textX, _nodeSearchInputBounds.Y + 5, 1, Math.Max(1, _nodeSearchInputBounds.Height - 10));
            var candidateBounds = caretBounds;
            if (_nodeSearchComposition.IsActive)
            {
                candidateBounds = new UiRect(caretBounds.X, caretBounds.Y, Math.Max(1, caretBounds.Width + _nodeSearchComposition.Text.Length * 8), caretBounds.Height);
            }

            request = new UiTextInputRequest(_nodeSearchInputBounds, isMultiLine: false, caretBounds: caretBounds, candidateBounds: candidateBounds);
            return true;
        }

        return false;
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

    private void ProcessValueEditorState(UiUpdateContext context)
    {
        if (_pendingValueEditAction == ValueEditAction.Commit)
        {
            CommitValueEdit();
            return;
        }

        if (_pendingValueEditAction == ValueEditAction.Cancel)
        {
            CancelValueEdit();
            return;
        }

        if (IsEditingValue && !ReferenceEquals(context.Focus.Focused, this))
        {
            CommitValueEdit();
        }
    }

    private void ProcessCommentEditorState(UiUpdateContext context)
    {
        if (_pendingCommentEditAction == ValueEditAction.Commit)
        {
            CommitCommentEdit();
            return;
        }

        if (_pendingCommentEditAction == ValueEditAction.Cancel)
        {
            CancelCommentEdit();
            return;
        }

        if (IsEditingComment && !ReferenceEquals(context.Focus.Focused, this))
        {
            CommitCommentEdit();
        }
    }

    private bool TryBeginValueEdit(UiUpdateContext context, UiNodeControl node, UiNodePin pin, UiPoint worldMouse, UiInputState input)
    {
        if (!pin.ValueFieldEditable)
        {
            return false;
        }

        if (ReferenceEquals(_editingValueNode, node) && ReferenceEquals(_editingValuePin, pin))
        {
            context.Focus.RequestFocus(this);
            MoveValueEditCaretFromPoint(node, pin, worldMouse, input.LeftDoubleClicked, input.ShiftDown);
            return true;
        }

        if (IsEditingValue)
        {
            CommitValueEdit();
        }

        if (IsEditingComment)
        {
            CommitCommentEdit();
        }

        UiRect valueBounds = pin.Layout.ValueBounds;
        if (valueBounds.Width <= 0 || valueBounds.Height <= 0)
        {
            return false;
        }

        _editingValueNode = node;
        _editingValuePin = pin;
        _pendingValueEditAction = ValueEditAction.None;
        _valueComposition = UiTextCompositionState.Empty;
        _valueHorizontalScrollOffset = 0;
        _valueDragSelecting = false;
        _valueCaretVisible = true;
        _valueCaretTimer = 0f;
        _valueEditingState.SetText(pin.ValueText);
        _valueEditingState.BeginSession();
        _valueEditingState.SelectAll();
        context.Focus.RequestFocus(this);
        SyncEditingPinState();
        ValueEditStarted?.Invoke(new UiNodeValueEditStartedEvent(this, node, pin, pin.ValueText, valueBounds));
        return true;
    }

    private bool TryBeginCommentEdit(UiUpdateContext context, UiNodeCommentBox comment, CommentEditField field)
    {
        if (field == CommentEditField.None)
        {
            return false;
        }

        if (ReferenceEquals(_editingComment, comment) && _editingCommentField == field)
        {
            context.Focus.RequestFocus(this);
            _commentEditingState.SelectAll();
            SyncEditingCommentState();
            return true;
        }

        if (IsEditingValue)
        {
            CommitValueEdit();
        }

        if (IsEditingComment)
        {
            CommitCommentEdit();
        }

        UiRect fieldBounds = ResolveCommentEditBounds(comment, field);
        if (fieldBounds.Width <= 0 || fieldBounds.Height <= 0)
        {
            return false;
        }

        _editingComment = comment;
        _editingCommentField = field;
        _pendingCommentEditAction = ValueEditAction.None;
        _commentComposition = UiTextCompositionState.Empty;
        _commentCaretVisible = true;
        _commentCaretTimer = 0f;
        _commentEditingState.SetText(ResolveCommentEditText(comment, field));
        _commentEditingState.BeginSession();
        _commentEditingState.SelectAll();
        context.Focus.RequestFocus(this);
        SyncEditingCommentState();
        CommentEditStarted?.Invoke(new UiNodeCommentEditStartedEvent(this, comment, CommentEditFieldKey(field), ResolveCommentEditText(comment, field), fieldBounds));
        return true;
    }

    private bool TryOpenNodeSearch(UiUpdateContext context, UiInputState input, bool mouseInViewport, UiPoint worldMouse)
    {
        if (!EnableNodeSearch
            || IsEditingText
            || IsNodeSearchOpen
            || !mouseInViewport
            || !input.RightClicked
            || HoveredNode != null
            || HoveredPin != null
            || HoveredValuePin != null
            || HoveredComment != null
            || HoveredWire != null
            || PreviewWire.Active
            || IsBoxSelectionPointerActive
            || IsDraggingComment)
        {
            return false;
        }

        OpenNodeSearch(context, input.MousePosition, worldMouse, input.Modifiers, contextNode: null, contextPin: null);
        return true;
    }

    private void OpenNodeSearch(
        UiUpdateContext context,
        UiPoint screenPosition,
        UiPoint worldPosition,
        UiModifierKeys modifiers,
        UiNodeControl? contextNode,
        UiNodePin? contextPin)
    {
        CancelCommentDrag(modifiers);
        CancelBoxSelection(modifiers);
        _nodeSearchOpen = true;
        _nodeSearchScreenPosition = screenPosition;
        _nodeSearchWorldPosition = worldPosition;
        _nodeSearchModifiers = modifiers;
        _nodeSearchContextNode = contextNode;
        _nodeSearchContextPin = contextPin;
        _nodeSearchComposition = UiTextCompositionState.Empty;
        _nodeSearchEditingState.SetText(string.Empty);
        _nodeSearchEditingState.BeginSession();
        _nodeSearchEditingState.MoveEnd();
        _nodeSearchSelectedIndex = 0;
        _nodeSearchScrollY = 0;
        _nodeSearchItems.Clear();
        _nodeSearchExpandedCategories.Clear();
        UpdateNodeSearchBounds();
        context.Focus.RequestFocus(this);
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
        NodeSearchRequested?.Invoke(new UiNodeSearchRequestedEvent(this, string.Empty, screenPosition, worldPosition, modifiers, contextNode, contextPin));
    }

    private void CloseNodeSearch(bool raiseEvent)
    {
        if (!IsNodeSearchOpen)
        {
            return;
        }

        string query = _nodeSearchEditingState.Text;
        _nodeSearchOpen = false;
        _nodeSearchItems.Clear();
        _nodeSearchDisplayRows.Clear();
        _nodeSearchExpandedCategories.Clear();
        _nodeSearchEditingState.EndSession();
        _nodeSearchComposition = UiTextCompositionState.Empty;
        _nodeSearchSelectedIndex = 0;
        _nodeSearchScrollY = 0;
        _nodeSearchBounds = default;
        _nodeSearchInputBounds = default;
        _nodeSearchRowsViewportBounds = default;
        var contextNode = _nodeSearchContextNode;
        var contextPin = _nodeSearchContextPin;
        _nodeSearchContextNode = null;
        _nodeSearchContextPin = null;
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
        if (raiseEvent)
        {
            NodeSearchClosed?.Invoke(new UiNodeSearchClosedEvent(this, query, _nodeSearchWorldPosition, _nodeSearchModifiers, contextNode, contextPin));
        }
    }

    private void InvokeNodeSearchItem(UiNodeSearchItem item)
    {
        if (!IsNodeSearchOpen || string.IsNullOrWhiteSpace(item.Id))
        {
            return;
        }

        string query = _nodeSearchEditingState.Text;
        UiPoint screenPosition = _nodeSearchScreenPosition;
        UiPoint worldPosition = _nodeSearchWorldPosition;
        UiModifierKeys modifiers = _nodeSearchModifiers;
        UiNodeControl? contextNode = _nodeSearchContextNode;
        UiNodePin? contextPin = _nodeSearchContextPin;
        CloseNodeSearch(raiseEvent: false);
        NodeSearchItemInvoked?.Invoke(new UiNodeSearchItemInvokedEvent(this, item, query, screenPosition, worldPosition, modifiers, contextNode, contextPin));
    }

    private void InvokeCurrentNodeSearchItem()
    {
        if (_nodeSearchItems.Count == 0)
        {
            return;
        }

        var itemIndex = IsNodeSearchItemVisible(_nodeSearchSelectedIndex)
            ? _nodeSearchSelectedIndex
            : FirstVisibleNodeSearchItemIndex();
        if (itemIndex < 0 || itemIndex >= _nodeSearchItems.Count)
        {
            return;
        }

        InvokeNodeSearchItem(_nodeSearchItems[itemIndex]);
    }

    private void RaiseNodeSearchQueryChanged()
    {
        _nodeSearchSelectedIndex = 0;
        _nodeSearchScrollY = 0;
        NodeSearchQueryChanged?.Invoke(new UiNodeSearchQueryChangedEvent(
            this,
            _nodeSearchEditingState.Text,
            _nodeSearchScreenPosition,
            _nodeSearchWorldPosition,
            _nodeSearchModifiers,
            _nodeSearchContextNode,
            _nodeSearchContextPin));
        UpdateNodeSearchBounds();
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    private void CommitValueEdit()
    {
        if (_editingValueNode is not { } node || _editingValuePin is not { } pin)
        {
            _pendingValueEditAction = ValueEditAction.None;
            return;
        }

        string text = _valueEditingState.Text;
        pin.ValueText = text;
        EndValueEdit();
        ValueEditCommitted?.Invoke(new UiNodeValueEditCommittedEvent(this, node, pin, text));
    }

    private void CancelValueEdit()
    {
        if (_editingValueNode is not { } node || _editingValuePin is not { } pin)
        {
            _pendingValueEditAction = ValueEditAction.None;
            return;
        }

        _valueEditingState.CancelSession();
        EndValueEdit();
        ValueEditCancelled?.Invoke(new UiNodeValueEditCancelledEvent(this, node, pin));
    }

    private void EndValueEdit()
    {
        UiNodeControl? node = _editingValueNode;
        UiNodePin? pin = _editingValuePin;
        _valueEditingState.EndSession();
        _valueComposition = UiTextCompositionState.Empty;
        _valueHorizontalScrollOffset = 0;
        _valueDragSelecting = false;
        _valueCaretVisible = true;
        _valueCaretTimer = 0f;
        _pendingValueEditAction = ValueEditAction.None;
        _editingValueNode = null;
        _editingValuePin = null;

        if (pin is not null)
        {
            pin.IsValueEditing = false;
            pin.EditingValueText = string.Empty;
            pin.EditingCaretVisible = true;
            pin.EditingCaretIndex = 0;
            pin.EditingSelectionStart = 0;
            pin.EditingSelectionEnd = 0;
            pin.EditingHorizontalScrollOffset = 0;
        }

        node?.Invalidate(UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    private void CommitCommentEdit()
    {
        if (_editingComment is not { } comment || _editingCommentField == CommentEditField.None)
        {
            _pendingCommentEditAction = ValueEditAction.None;
            return;
        }

        CommentEditField field = _editingCommentField;
        string text = _commentEditingState.Text;
        if (field == CommentEditField.Title)
        {
            comment.Title = text;
        }
        else
        {
            comment.Text = text;
        }

        EndCommentEdit();
        CommentEditCommitted?.Invoke(new UiNodeCommentEditCommittedEvent(this, comment, CommentEditFieldKey(field), text));
    }

    private void CancelCommentEdit()
    {
        if (_editingComment is not { } comment || _editingCommentField == CommentEditField.None)
        {
            _pendingCommentEditAction = ValueEditAction.None;
            return;
        }

        CommentEditField field = _editingCommentField;
        _commentEditingState.CancelSession();
        EndCommentEdit();
        CommentEditCancelled?.Invoke(new UiNodeCommentEditCancelledEvent(this, comment, CommentEditFieldKey(field)));
    }

    private void EndCommentEdit()
    {
        UiNodeCommentBox? comment = _editingComment;
        _commentEditingState.EndSession();
        _commentComposition = UiTextCompositionState.Empty;
        _commentCaretVisible = true;
        _commentCaretTimer = 0f;
        _pendingCommentEditAction = ValueEditAction.None;
        _editingComment = null;
        _editingCommentField = CommentEditField.None;

        if (comment is not null)
        {
            comment.IsTitleEditing = false;
            comment.IsBodyEditing = false;
            comment.EditingTitleText = string.Empty;
            comment.EditingBodyText = string.Empty;
            comment.EditingCaretVisible = true;
            comment.Invalidate(UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
        }
    }

    private void HandleValueEditInput(UiUpdateContext context, UiInputState input, UiFont font)
    {
        if (_editingValueNode is null || _editingValuePin is null)
        {
            return;
        }

        _valueComposition = input.Composition;
        HandleValueEditPointerInput(context, input, font);
        HandleValueEditShortcutInput(context, input);
        HandleValueEditNavigation(input);
        HandleValueEditTextInput(input.TextInput);
        UpdateValueEditCaretBlink(context.DeltaSeconds);
        SyncEditingPinState();
    }

    private void HandleCommentEditInput(UiUpdateContext context, UiInputState input)
    {
        if (_editingComment is null || _editingCommentField == CommentEditField.None)
        {
            return;
        }

        _commentComposition = input.Composition;
        HandleCommentEditShortcutInput(context, input);
        HandleCommentEditNavigation(input);
        HandleCommentEditTextInput(input.TextInput);
        UpdateCommentEditCaretBlink(context.DeltaSeconds);
        SyncEditingCommentState();
    }

    private void HandleNodeSearchInput(UiUpdateContext context, UiInputState input)
    {
        if (!IsNodeSearchOpen)
        {
            return;
        }

        _nodeSearchHandledInputThisFrame = true;
        _nodeSearchComposition = input.Composition;
        HandleNodeSearchPointerInput(input);
        HandleNodeSearchShortcutInput(context, input);
        HandleNodeSearchNavigation(input);
        HandleNodeSearchTextInput(input.TextInput);
    }

    private void HandleNodeSearchPointerInput(UiInputState input)
    {
        if (!IsNodeSearchOpen)
        {
            return;
        }

        UpdateNodeSearchBounds();
        if (_nodeSearchBounds.Contains(input.MousePosition) && input.ScrollDelta != 0)
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps == 0)
            {
                steps = input.ScrollDelta > 0 ? 1 : -1;
            }

            _nodeSearchScrollY -= steps * NodeSearchScrollWheelStep;
            ClampNodeSearchScroll();
            Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
        }

        var rowIndex = ResolveNodeSearchDisplayRowIndex(input.MousePosition);
        if (rowIndex >= 0 && rowIndex < _nodeSearchDisplayRows.Count)
        {
            var hoverItemIndex = _nodeSearchDisplayRows[rowIndex].ItemIndex;
            if (hoverItemIndex >= 0 && hoverItemIndex < _nodeSearchItems.Count && hoverItemIndex != _nodeSearchSelectedIndex)
            {
                _nodeSearchSelectedIndex = hoverItemIndex;
                Invalidate(UiInvalidationReason.Paint | UiInvalidationReason.State);
            }
        }

        if (!input.LeftClicked && !input.RightClicked)
        {
            return;
        }

        if (!_nodeSearchBounds.Contains(input.MousePosition))
        {
            CloseNodeSearch(raiseEvent: true);
            return;
        }

        if (rowIndex >= 0 && rowIndex < _nodeSearchDisplayRows.Count)
        {
            var row = _nodeSearchDisplayRows[rowIndex];
            if (string.Equals(row.Kind, "category", StringComparison.Ordinal))
            {
                ToggleNodeSearchCategory(row.Category);
                return;
            }

            var itemIndex = row.ItemIndex;
            if (itemIndex >= 0 && itemIndex < _nodeSearchItems.Count)
            {
                _nodeSearchSelectedIndex = itemIndex;
                InvokeNodeSearchItem(_nodeSearchItems[itemIndex]);
            }
        }
    }

    private void HandleNodeSearchShortcutInput(UiUpdateContext context, UiInputState input)
    {
        if (input.IsPrimaryShortcutPressed(UiKey.A))
        {
            _nodeSearchEditingState.SelectAll();
        }

        if (input.IsPrimaryShortcutPressed(UiKey.V))
        {
            InsertNodeSearchText(context.Clipboard.GetText());
        }
    }

    private void HandleNodeSearchNavigation(UiInputState input)
    {
        UiNavigationInput navigation = input.Navigation;
        if (navigation.Escape)
        {
            CloseNodeSearch(raiseEvent: true);
            return;
        }

        if (navigation.Enter || navigation.KeypadEnter)
        {
            InvokeCurrentNodeSearchItem();
            return;
        }

        if (navigation.MoveUp)
        {
            MoveNodeSearchSelection(-1);
            return;
        }

        if (navigation.MoveDown)
        {
            MoveNodeSearchSelection(1);
            return;
        }

        bool byWord = input.CtrlDown || input.AltDown;
        if (navigation.MoveLeft)
        {
            _nodeSearchEditingState.MoveLeft(input.ShiftDown, byWord);
        }

        if (navigation.MoveRight)
        {
            _nodeSearchEditingState.MoveRight(input.ShiftDown, byWord);
        }

        if (navigation.Home)
        {
            _nodeSearchEditingState.MoveHome(input.ShiftDown);
        }

        if (navigation.End)
        {
            _nodeSearchEditingState.MoveEnd(input.ShiftDown);
        }

        bool changed = false;
        if (navigation.Backspace)
        {
            changed |= _nodeSearchEditingState.Backspace(byWord);
        }

        if (navigation.Delete)
        {
            changed |= _nodeSearchEditingState.Delete(byWord);
        }

        if (changed)
        {
            RaiseNodeSearchQueryChanged();
        }
    }

    private void HandleNodeSearchTextInput(IReadOnlyList<char> input)
    {
        if (input.Count == 0)
        {
            return;
        }

        string pending = string.Empty;
        for (int i = 0; i < input.Count; i++)
        {
            char character = input[i];
            if (!char.IsControl(character))
            {
                pending += character;
            }
        }

        InsertNodeSearchText(pending);
    }

    private void HandleValueEditPointerInput(UiUpdateContext context, UiInputState input, UiFont font)
    {
        if (_editingValueNode is null || _editingValuePin is null)
        {
            return;
        }

        if (input.LeftClicked)
        {
            UiPoint worldMouse = _canvas.ScreenToWorld(input.MousePosition);
            if (_editingValuePin.Layout.ValueBounds.Contains(worldMouse))
            {
                context.Focus.RequestFocus(this);
                MoveValueEditCaretFromPoint(_editingValueNode, _editingValuePin, worldMouse, input.LeftDoubleClicked, input.ShiftDown);
                _valueDragSelectionAnchor = _valueEditingState.SelectionAnchor;
                _valueDragSelecting = !input.LeftDoubleClicked;
            }
            else
            {
                _valueDragSelecting = false;
            }
        }

        if (_valueDragSelecting)
        {
            if (input.LeftDown)
            {
                UiPoint worldMouse = _canvas.ScreenToWorld(input.MousePosition);
                int caret = ResolveValueEditCaretIndex(_editingValueNode, _editingValuePin, worldMouse, font);
                _valueEditingState.SelectRange(_valueDragSelectionAnchor, caret);
                ResetValueEditCaretBlink();
            }
            else
            {
                _valueDragSelecting = false;
            }
        }
    }

    private void HandleValueEditShortcutInput(UiUpdateContext context, UiInputState input)
    {
        if (input.IsPrimaryShortcutPressed(UiKey.A))
        {
            _valueEditingState.SelectAll();
            ResetValueEditCaretBlink();
        }

        if (input.IsPrimaryShortcutPressed(UiKey.C))
        {
            CopyValueEditSelection(context.Clipboard);
        }

        if (input.IsPrimaryShortcutPressed(UiKey.X))
        {
            CutValueEditSelection(context.Clipboard);
        }

        if (input.IsPrimaryShortcutPressed(UiKey.V))
        {
            InsertValueEditText(context.Clipboard.GetText());
        }

        if (input.IsPrimaryShortcutPressed(UiKey.Z, shift: true))
        {
            ApplyValueEdit(_valueEditingState.Redo());
        }
        else if (input.IsPrimaryShortcutPressed(UiKey.Z))
        {
            ApplyValueEdit(_valueEditingState.Undo());
        }
        else if (input.IsPrimaryShortcutPressed(UiKey.Y))
        {
            ApplyValueEdit(_valueEditingState.Redo());
        }
    }

    private void HandleValueEditNavigation(UiInputState input)
    {
        UiNavigationInput navigation = input.Navigation;
        bool extendSelection = input.ShiftDown;
        bool byWord = input.CtrlDown || input.AltDown;

        if (navigation.MoveLeft)
        {
            if (input.SuperDown && !byWord)
            {
                _valueEditingState.MoveHome(extendSelection);
            }
            else
            {
                _valueEditingState.MoveLeft(extendSelection, byWord);
            }

            ResetValueEditCaretBlink();
        }

        if (navigation.MoveRight)
        {
            if (input.SuperDown && !byWord)
            {
                _valueEditingState.MoveEnd(extendSelection);
            }
            else
            {
                _valueEditingState.MoveRight(extendSelection, byWord);
            }

            ResetValueEditCaretBlink();
        }

        if (navigation.Home)
        {
            _valueEditingState.MoveHome(extendSelection);
            ResetValueEditCaretBlink();
        }

        if (navigation.End)
        {
            _valueEditingState.MoveEnd(extendSelection);
            ResetValueEditCaretBlink();
        }

        if (navigation.Backspace)
        {
            ApplyValueEdit(_valueEditingState.Backspace(byWord));
        }

        if (navigation.Delete)
        {
            ApplyValueEdit(_valueEditingState.Delete(byWord));
        }

        if (navigation.Enter || navigation.KeypadEnter)
        {
            _pendingValueEditAction = ValueEditAction.Commit;
            _valueEditingState.MarkSessionOrigin();
        }

        if (navigation.Escape)
        {
            _pendingValueEditAction = ValueEditAction.Cancel;
        }
    }

    private void HandleValueEditTextInput(IReadOnlyList<char> input)
    {
        if (input.Count == 0)
        {
            return;
        }

        string pending = string.Empty;
        for (int i = 0; i < input.Count; i++)
        {
            char character = input[i];
            if (!char.IsControl(character))
            {
                pending += character;
            }
        }

        InsertValueEditText(pending);
    }

    private void HandleCommentEditTextInput(IReadOnlyList<char> input)
    {
        if (input.Count == 0)
        {
            return;
        }

        string pending = string.Empty;
        for (int i = 0; i < input.Count; i++)
        {
            char character = input[i];
            if (!char.IsControl(character) || character == '\n')
            {
                pending += character;
            }
        }

        InsertCommentEditText(pending);
    }

    private void HandleCommentEditShortcutInput(UiUpdateContext context, UiInputState input)
    {
        if (input.IsPrimaryShortcutPressed(UiKey.A))
        {
            _commentEditingState.SelectAll();
            ResetCommentEditCaretBlink();
        }

        if (input.IsPrimaryShortcutPressed(UiKey.C))
        {
            CopyCommentEditSelection(context.Clipboard);
        }

        if (input.IsPrimaryShortcutPressed(UiKey.X))
        {
            CutCommentEditSelection(context.Clipboard);
        }

        if (input.IsPrimaryShortcutPressed(UiKey.V))
        {
            InsertCommentEditText(context.Clipboard.GetText());
        }

        if (input.IsPrimaryShortcutPressed(UiKey.Z, shift: true))
        {
            ApplyCommentEdit(_commentEditingState.Redo());
        }
        else if (input.IsPrimaryShortcutPressed(UiKey.Z))
        {
            ApplyCommentEdit(_commentEditingState.Undo());
        }
        else if (input.IsPrimaryShortcutPressed(UiKey.Y))
        {
            ApplyCommentEdit(_commentEditingState.Redo());
        }
    }

    private void HandleCommentEditNavigation(UiInputState input)
    {
        UiNavigationInput navigation = input.Navigation;
        bool extendSelection = input.ShiftDown;
        bool byWord = input.CtrlDown || input.AltDown;

        if (navigation.MoveLeft)
        {
            if (input.SuperDown && !byWord)
            {
                _commentEditingState.MoveHome(extendSelection);
            }
            else
            {
                _commentEditingState.MoveLeft(extendSelection, byWord);
            }

            ResetCommentEditCaretBlink();
        }

        if (navigation.MoveRight)
        {
            if (input.SuperDown && !byWord)
            {
                _commentEditingState.MoveEnd(extendSelection);
            }
            else
            {
                _commentEditingState.MoveRight(extendSelection, byWord);
            }

            ResetCommentEditCaretBlink();
        }

        if (navigation.Home)
        {
            _commentEditingState.MoveHome(extendSelection);
            ResetCommentEditCaretBlink();
        }

        if (navigation.End)
        {
            _commentEditingState.MoveEnd(extendSelection);
            ResetCommentEditCaretBlink();
        }

        if (navigation.Backspace)
        {
            ApplyCommentEdit(_commentEditingState.Backspace(byWord));
        }

        if (navigation.Delete)
        {
            ApplyCommentEdit(_commentEditingState.Delete(byWord));
        }

        if ((navigation.Enter || navigation.KeypadEnter)
            && _editingCommentField == CommentEditField.Body
            && input.ShiftDown)
        {
            InsertCommentEditText("\n");
            return;
        }

        if (navigation.Enter || navigation.KeypadEnter)
        {
            _pendingCommentEditAction = ValueEditAction.Commit;
            _commentEditingState.MarkSessionOrigin();
        }

        if (navigation.Escape)
        {
            _pendingCommentEditAction = ValueEditAction.Cancel;
        }
    }

    private void InsertValueEditText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int available = Math.Max(0, ValueEditorMaxLength - (_valueEditingState.Text.Length - _valueEditingState.SelectionLength));
        if (available <= 0)
        {
            return;
        }

        string insertion = text.Length > available ? text.Substring(0, available) : text;
        ApplyValueEdit(_valueEditingState.InsertText(insertion));
    }

    private void InsertCommentEditText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        string normalized = _editingCommentField == CommentEditField.Title
            ? text.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal)
            : text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        int available = Math.Max(0, CommentEditorMaxLength - (_commentEditingState.Text.Length - _commentEditingState.SelectionLength));
        if (available <= 0)
        {
            return;
        }

        string insertion = normalized.Length > available ? normalized.Substring(0, available) : normalized;
        ApplyCommentEdit(_commentEditingState.InsertText(insertion));
    }

    private void InsertNodeSearchText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int available = Math.Max(0, NodeSearchMaxLength - (_nodeSearchEditingState.Text.Length - _nodeSearchEditingState.SelectionLength));
        if (available <= 0)
        {
            return;
        }

        string normalized = text.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        string insertion = normalized.Length > available ? normalized.Substring(0, available) : normalized;
        if (_nodeSearchEditingState.InsertText(insertion))
        {
            RaiseNodeSearchQueryChanged();
        }
    }

    private void CopyValueEditSelection(IUiClipboard clipboard)
    {
        if (_valueEditingState.HasSelection)
        {
            clipboard.SetText(_valueEditingState.GetSelectedText());
        }
    }

    private void CutValueEditSelection(IUiClipboard clipboard)
    {
        if (!_valueEditingState.HasSelection)
        {
            return;
        }

        clipboard.SetText(_valueEditingState.GetSelectedText());
        ApplyValueEdit(_valueEditingState.DeleteSelection());
    }

    private void CopyCommentEditSelection(IUiClipboard clipboard)
    {
        if (_commentEditingState.HasSelection)
        {
            clipboard.SetText(_commentEditingState.GetSelectedText());
        }
    }

    private void CutCommentEditSelection(IUiClipboard clipboard)
    {
        if (!_commentEditingState.HasSelection)
        {
            return;
        }

        clipboard.SetText(_commentEditingState.GetSelectedText());
        ApplyCommentEdit(_commentEditingState.DeleteSelection());
    }

    private void ApplyValueEdit(bool changed)
    {
        if (changed)
        {
            ResetValueEditCaretBlink();
        }
    }

    private void ApplyCommentEdit(bool changed)
    {
        if (changed)
        {
            ResetCommentEditCaretBlink();
        }
    }

    private void MoveValueEditCaretFromPoint(UiNodeControl node, UiNodePin pin, UiPoint worldPoint, bool selectWord, bool extendSelection)
    {
        int caret = ResolveValueEditCaretIndex(node, pin, worldPoint, _valueEditFont);
        if (selectWord)
        {
            _valueEditingState.SelectWordAt(caret);
        }
        else
        {
            _valueEditingState.SetCaret(caret, extendSelection);
        }

        ResetValueEditCaretBlink();
        SyncEditingPinState();
    }

    private int ResolveValueEditCaretIndex(UiNodeControl node, UiNodePin pin, UiPoint worldPoint, UiFont font)
    {
        string text = _valueEditingState.Text;
        if (text.Length == 0)
        {
            return 0;
        }

        int padding = Math.Max(0, node.ValueBoxPadding);
        int localX = worldPoint.X - (pin.Layout.ValueBounds.X + padding) + _valueHorizontalScrollOffset;
        if (localX <= 0)
        {
            return 0;
        }

        int previousWidth = 0;
        for (int i = 0; i < text.Length; i++)
        {
            int nextWidth = MeasurePrefixWidth(text, i + 1, node.TextScale, font);
            int midpoint = previousWidth + (nextWidth - previousWidth) / 2;
            if (localX < midpoint)
            {
                return i;
            }

            previousWidth = nextWidth;
        }

        return text.Length;
    }

    private void SyncEditingPinState()
    {
        if (_editingValueNode is null || _editingValuePin is null)
        {
            return;
        }

        UpdateValueEditHorizontalScroll(_editingValueNode, _editingValuePin, _valueEditFont);
        _editingValuePin.IsValueEditing = true;
        _editingValuePin.EditingValueText = _valueEditingState.Text;
        _editingValuePin.EditingCaretVisible = _valueCaretVisible;
        _editingValuePin.EditingCaretIndex = _valueEditingState.CaretIndex;
        _editingValuePin.EditingSelectionStart = _valueEditingState.SelectionStart;
        _editingValuePin.EditingSelectionEnd = _valueEditingState.SelectionEnd;
        _editingValuePin.EditingHorizontalScrollOffset = _valueHorizontalScrollOffset;
        _editingValueNode.Invalidate(UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    private void SyncEditingCommentState()
    {
        if (_editingComment is null || _editingCommentField == CommentEditField.None)
        {
            return;
        }

        _editingComment.IsTitleEditing = _editingCommentField == CommentEditField.Title;
        _editingComment.IsBodyEditing = _editingCommentField == CommentEditField.Body;
        _editingComment.EditingTitleText = _editingCommentField == CommentEditField.Title ? _commentEditingState.Text : string.Empty;
        _editingComment.EditingBodyText = _editingCommentField == CommentEditField.Body ? _commentEditingState.Text : string.Empty;
        _editingComment.EditingCaretVisible = _commentCaretVisible;
        _editingComment.Invalidate(UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    private void UpdateNodeSearchBounds()
    {
        if (!IsNodeSearchOpen)
        {
            _nodeSearchBounds = default;
            _nodeSearchInputBounds = default;
            _nodeSearchRowsViewportBounds = default;
            return;
        }

        RebuildNodeSearchDisplayRows();
        EnsureNodeSearchSelectionVisible();
        int totalRowsHeight = TotalNodeSearchRowsHeight();
        int verticalChrome = 10 + NodeSearchHeaderHeight + NodeSearchInputHeight + 12 + 10;
        int availableRowsHeight = Math.Max(NodeSearchResultHeight, Bounds.Height - verticalChrome - 12);
        int maxRowsHeight = Math.Min(NodeSearchMaxRowsHeight, availableRowsHeight);
        int rowCountHeight = Math.Min(Math.Max(NodeSearchResultHeight, totalRowsHeight), Math.Max(NodeSearchResultHeight, maxRowsHeight));
        int width = Math.Max(180, NodeSearchPopupWidth);
        int height = verticalChrome + rowCountHeight;
        int minX = Bounds.X + 6;
        int minY = Bounds.Y + 6;
        int maxX = Math.Max(minX, Bounds.Right - width - 6);
        int maxY = Math.Max(minY, Bounds.Bottom - height - 6);
        int x = Math.Clamp(_nodeSearchScreenPosition.X, minX, maxX);
        int y = Math.Clamp(_nodeSearchScreenPosition.Y, minY, maxY);
        _nodeSearchBounds = new UiRect(x, y, width, height);
        _nodeSearchInputBounds = new UiRect(x + 10, y + NodeSearchHeaderHeight, width - 20, NodeSearchInputHeight);
        _nodeSearchRowsViewportBounds = new UiRect(x + 10, _nodeSearchInputBounds.Bottom + 10, width - 20, rowCountHeight);
        ClampNodeSearchScroll();
    }

    private int ClampNodeSearchIndex(int index)
    {
        return _nodeSearchItems.Count == 0
            ? -1
            : Math.Clamp(index, 0, Math.Min(NodeSearchMaxResults, _nodeSearchItems.Count) - 1);
    }

    private int ResolveNodeSearchDisplayRowIndex(UiPoint point)
    {
        if (!_nodeSearchRowsViewportBounds.Contains(point))
        {
            return -1;
        }

        int rowTop = _nodeSearchRowsViewportBounds.Y - _nodeSearchScrollY;
        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            int height = Math.Max(1, _nodeSearchDisplayRows[rowIndex].Height);
            if (point.Y >= rowTop && point.Y < rowTop + height)
            {
                return rowIndex;
            }

            rowTop += height;
        }

        return -1;
    }

    private int ResolveNodeSearchCaretX(UiFont font)
    {
        return MeasurePrefixWidth(_nodeSearchEditingState.Text, _nodeSearchEditingState.CaretIndex, NodeSearchTextScale, font);
    }

    private void DrawNodeSearchPopup(UiRenderContext context)
    {
        if (!IsNodeSearchOpen)
        {
            return;
        }

        UpdateNodeSearchBounds();
        UiFont font = ResolveFont(context.DefaultFont);
        IUiRenderer renderer = context.Renderer;
        UiRenderHelpers.FillRectRounded(renderer, _nodeSearchBounds, 2, NodeSearchBackground);
        UiRenderHelpers.DrawRectRounded(renderer, _nodeSearchBounds, 2, NodeSearchBorder, 1);
        DrawNodeSearchHeader(renderer, font);
        UiRenderHelpers.FillRectRounded(renderer, _nodeSearchInputBounds, 14, NodeSearchInputBackground);
        UiRenderHelpers.DrawRectRounded(renderer, _nodeSearchInputBounds, 14, NodeSearchInputBorder, 1);

        DrawNodeSearchInputText(renderer, font);
        if (_nodeSearchItems.Count == 0)
        {
            DrawNodeSearchEmptyText(renderer, font);
            return;
        }

        RebuildNodeSearchDisplayRows();
        renderer.PushClip(_nodeSearchRowsViewportBounds);
        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            var row = _nodeSearchDisplayRows[rowIndex];
            UiRect rowBounds = ResolveNodeSearchDisplayRowBounds(rowIndex);
            if (!Intersects(rowBounds, _nodeSearchRowsViewportBounds))
            {
                continue;
            }

            bool selected = row.ItemIndex >= 0 && row.ItemIndex == ClampNodeSearchIndex(_nodeSearchSelectedIndex);
            if (selected)
            {
                UiRenderHelpers.FillRectRounded(renderer, rowBounds, 3, NodeSearchSelectedBackground);
                UiRenderHelpers.DrawRectRounded(renderer, rowBounds, 3, NodeSearchSelectedBorder, 1);
            }

            if (string.Equals(row.Kind, "category", StringComparison.Ordinal))
            {
                DrawNodeSearchCategoryRow(renderer, font, row.Text, rowIndex);
                continue;
            }

            DrawNodeSearchItemRow(renderer, font, row, rowIndex, selected);
        }

        renderer.PopClip();
        DrawNodeSearchScrollbar(renderer);
    }

    private void DrawNodeSearchHeader(IUiRenderer renderer, UiFont font)
    {
        var titleBounds = new UiRect(
            _nodeSearchBounds.X + 12,
            _nodeSearchBounds.Y + 10,
            Math.Max(1, _nodeSearchBounds.Width - 220),
            18);
        renderer.PushClip(titleBounds);
        renderer.DrawText(
            UiRenderHelpers.BuildElidedText(NodeSearchTitle, titleBounds.Width, NodeSearchTextScale, font),
            new UiPoint(titleBounds.X, titleBounds.Y),
            new UiColor(204, 206, 210, 255),
            NodeSearchTextScale,
            font);
        renderer.PopClip();

        var checkboxBounds = new UiRect(_nodeSearchBounds.Right - 176, _nodeSearchBounds.Y + 7, 18, 18);
        UiRenderHelpers.FillRectRounded(renderer, checkboxBounds, 3, new UiColor(8, 10, 14, 255));
        UiRenderHelpers.DrawRectRounded(renderer, checkboxBounds, 3, new UiColor(44, 50, 58, 255), 1);
        if (NodeSearchContextSensitive)
        {
            renderer.DrawText("v", new UiPoint(checkboxBounds.X + 5, checkboxBounds.Y + 2), new UiColor(55, 162, 255, 255), NodeSearchTextScale, font);
        }

        renderer.DrawText("Context Sensitive", new UiPoint(checkboxBounds.Right + 8, _nodeSearchBounds.Y + 9), NodeSearchTextColor, NodeSearchTextScale, font);
        renderer.DrawText(">", new UiPoint(_nodeSearchBounds.Right - 18, _nodeSearchBounds.Y + 10), NodeSearchPlaceholderColor, NodeSearchTextScale, font);
    }

    private void DrawDebugPinValuePopup(UiRenderContext context)
    {
        _debugPinValueBounds = default;
        _debugPinValueText = string.Empty;
        _debugPinValuePin = null;

        if (IsNodeSearchOpen || HoveredPin is null || string.IsNullOrWhiteSpace(HoveredPin.DebugValueText))
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        IUiRenderer renderer = context.Renderer;
        var text = HoveredPin.DebugValueText;
        var safeText = text.Length > 96 ? text[..93] + "..." : text;
        var textWidth = Math.Min(360, Math.Max(92, font.MeasureTextWidth(safeText, NodeSearchTextScale) + 18));
        var screenCenter = Canvas.WorldToScreen(HoveredPin.Layout.Center);
        var x = Math.Clamp(screenCenter.X + 14, Bounds.X + 6, Math.Max(Bounds.X + 6, Bounds.Right - textWidth - 6));
        var y = Math.Clamp(screenCenter.Y - 10, Bounds.Y + 6, Math.Max(Bounds.Y + 6, Bounds.Bottom - 30));
        var bounds = new UiRect(x, y, textWidth, 26);

        _debugPinValueBounds = bounds;
        _debugPinValueText = safeText;
        _debugPinValuePin = HoveredPin;

        UiRenderHelpers.FillRectRounded(renderer, bounds, 4, DebugPinValueBackground);
        UiRenderHelpers.DrawRectRounded(renderer, bounds, 4, DebugPinValueBorder, 1);
        renderer.PushClip(bounds);
        renderer.DrawText(
            UiRenderHelpers.BuildElidedText(safeText, Math.Max(1, bounds.Width - 14), NodeSearchTextScale, font),
            new UiPoint(bounds.X + 7, bounds.Y + 6),
            DebugPinValueTextColor,
            NodeSearchTextScale,
            font);
        renderer.PopClip();
    }

    private void DrawNodeSearchInputText(IUiRenderer renderer, UiFont font)
    {
        int padding = 38;
        string query = _nodeSearchEditingState.Text;
        string text = string.IsNullOrEmpty(query) ? NodeSearchPlaceholder : query;
        UiColor color = string.IsNullOrEmpty(query) ? NodeSearchPlaceholderColor : NodeSearchTextColor;
        var iconCenter = new UiPoint(_nodeSearchInputBounds.X + 16, _nodeSearchInputBounds.Y + 14);
        UiRenderHelpers.DrawCircle(renderer, iconCenter, 7, NodeSearchPlaceholderColor, 2);
        renderer.FillRect(new UiRect(iconCenter.X + 5, iconCenter.Y + 5, 7, 2), NodeSearchPlaceholderColor);
        UiRect textBounds = new(
            _nodeSearchInputBounds.X + padding,
            _nodeSearchInputBounds.Y + 7,
            Math.Max(1, _nodeSearchInputBounds.Width - padding - 10),
            Math.Max(1, _nodeSearchInputBounds.Height - 10));
        renderer.PushClip(_nodeSearchInputBounds);
        string drawText = UiRenderHelpers.BuildElidedText(text, textBounds.Width, NodeSearchTextScale, font);
        renderer.DrawText(drawText, new UiPoint(textBounds.X, textBounds.Y), color, NodeSearchTextScale, font);
        if (!string.IsNullOrEmpty(query))
        {
            int caretX = textBounds.X + ResolveNodeSearchCaretX(font);
            renderer.FillRect(new UiRect(caretX, textBounds.Y, 1, textBounds.Height), NodeSearchTextColor);
        }

        renderer.PopClip();
    }

    private void DrawNodeSearchEmptyText(IUiRenderer renderer, UiFont font)
    {
        UiRect rowBounds = new(_nodeSearchRowsViewportBounds.X, _nodeSearchRowsViewportBounds.Y, _nodeSearchRowsViewportBounds.Width, NodeSearchResultHeight);
        renderer.PushClip(_nodeSearchRowsViewportBounds);
        string drawText = UiRenderHelpers.BuildElidedText("No matching nodes", Math.Max(1, rowBounds.Width - 12), NodeSearchTextScale, font);
        renderer.DrawText(drawText, new UiPoint(rowBounds.X + 6, rowBounds.Y + 5), NodeSearchPlaceholderColor, NodeSearchTextScale, font);
        renderer.PopClip();
    }

    private void DrawNodeSearchCategoryRow(IUiRenderer renderer, UiFont font, string text, int rowIndex)
    {
        UiRect rowBounds = ResolveNodeSearchDisplayRowBounds(rowIndex);
        var scrollbarInset = HasNodeSearchScrollbar() ? NodeSearchScrollbarWidth + 4 : 0;
        var expanded = text.StartsWith("v ", StringComparison.Ordinal);
        var label = text.Length > 2 ? text[2..] : text;
        renderer.PushClip(rowBounds);
        renderer.DrawText(expanded ? "v" : ">", new UiPoint(rowBounds.X + 4, rowBounds.Y + 4), new UiColor(126, 130, 136, 255), NodeSearchTextScale, font);
        var textBounds = new UiRect(rowBounds.X + 22, rowBounds.Y + 4, Math.Max(1, rowBounds.Width - 28 - scrollbarInset), Math.Max(1, rowBounds.Height - 8));
        string drawText = UiRenderHelpers.BuildElidedText(label, textBounds.Width, NodeSearchTextScale, font);
        renderer.DrawText(drawText, new UiPoint(textBounds.X, textBounds.Y), new UiColor(210, 212, 216, 255), NodeSearchTextScale, font);
        renderer.PopClip();
    }

    private void DrawNodeSearchItemRow(IUiRenderer renderer, UiFont font, UiNodeSearchDisplayRow row, int rowIndex, bool selected)
    {
        UiRect rowBounds = ResolveNodeSearchDisplayRowBounds(rowIndex);
        var scrollbarInset = HasNodeSearchScrollbar() ? NodeSearchScrollbarWidth + 4 : 0;
        var item = row.ItemIndex >= 0 && row.ItemIndex < _nodeSearchItems.Count ? _nodeSearchItems[row.ItemIndex] : null;
        var iconColor = ResolveNodeSearchItemIconColor(item);
        var textColor = selected ? UiColor.White : new UiColor(196, 198, 202, 255);
        renderer.PushClip(rowBounds);
        renderer.DrawText("f", new UiPoint(rowBounds.X + 40, rowBounds.Y + 5), iconColor, NodeSearchTextScale, font);
        var textBounds = new UiRect(rowBounds.X + 62, rowBounds.Y + 5, Math.Max(1, rowBounds.Width - 70 - scrollbarInset), Math.Max(1, rowBounds.Height - 8));
        string drawText = UiRenderHelpers.BuildElidedText(row.Text, textBounds.Width, NodeSearchTextScale, font);
        renderer.DrawText(drawText, new UiPoint(textBounds.X, textBounds.Y), textColor, NodeSearchTextScale, font);
        renderer.PopClip();
    }

    private static UiColor ResolveNodeSearchItemIconColor(UiNodeSearchItem? item)
    {
        if (item is null)
        {
            return new UiColor(105, 205, 255, 255);
        }

        var category = item.Category ?? "";
        var id = item.Id ?? "";
        if (category.Contains("Flow", StringComparison.OrdinalIgnoreCase)
            || category.Contains("Console", StringComparison.OrdinalIgnoreCase)
            || category.Contains("Function", StringComparison.OrdinalIgnoreCase)
            || category.Contains("Event", StringComparison.OrdinalIgnoreCase)
            || id.Contains(".flow", StringComparison.OrdinalIgnoreCase)
            || id.Contains(".console", StringComparison.OrdinalIgnoreCase)
            || id.Contains(".call", StringComparison.OrdinalIgnoreCase))
        {
            return new UiColor(105, 205, 255, 255);
        }

        return new UiColor(150, 242, 142, 255);
    }

    private UiRect ResolveNodeSearchDisplayRowBounds(int rowIndex)
    {
        int rowTop = _nodeSearchRowsViewportBounds.Y - _nodeSearchScrollY;
        for (int index = 0; index < rowIndex && index < _nodeSearchDisplayRows.Count; index++)
        {
            rowTop += Math.Max(1, _nodeSearchDisplayRows[index].Height);
        }

        int height = rowIndex >= 0 && rowIndex < _nodeSearchDisplayRows.Count
            ? Math.Max(1, _nodeSearchDisplayRows[rowIndex].Height)
            : NodeSearchResultHeight;
        return new UiRect(_nodeSearchBounds.X + 8, rowTop, _nodeSearchBounds.Width - 16, height);
    }

    private void RebuildNodeSearchDisplayRows()
    {
        _nodeSearchDisplayRows.Clear();
        if (_nodeSearchItems.Count == 0)
        {
            return;
        }

        var grouped = new List<(string Category, List<(UiNodeSearchItem Item, int Index)> Items)>();
        var categoryLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < _nodeSearchItems.Count; index++)
        {
            var item = _nodeSearchItems[index];
            var category = string.IsNullOrWhiteSpace(item.Category) ? "All Nodes" : item.Category;
            if (!categoryLookup.TryGetValue(category, out var groupIndex))
            {
                groupIndex = grouped.Count;
                categoryLookup.Add(category, groupIndex);
                grouped.Add((category, []));
            }

            grouped[groupIndex].Items.Add((item, index));
        }

        var queryActive = !string.IsNullOrWhiteSpace(_nodeSearchEditingState.Text);
        for (var groupIndex = 0; groupIndex < grouped.Count; groupIndex++)
        {
            var group = grouped[groupIndex];
            var expanded = queryActive || groupIndex == 0 || _nodeSearchExpandedCategories.Contains(group.Category);
            _nodeSearchDisplayRows.Add(new UiNodeSearchDisplayRow("category", $"{(expanded ? "v" : ">")} {group.Category}", group.Category, -1, NodeSearchCategoryHeight));
            if (!expanded)
            {
                continue;
            }

            foreach (var match in group.Items)
            {
                _nodeSearchDisplayRows.Add(new UiNodeSearchDisplayRow("item", match.Item.Title, group.Category, match.Index, NodeSearchResultHeight));
            }
        }
    }

    private void ToggleNodeSearchCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        if (!_nodeSearchExpandedCategories.Add(category))
        {
            _nodeSearchExpandedCategories.Remove(category);
        }

        RebuildNodeSearchDisplayRows();
        EnsureNodeSearchSelectionVisible();
        EnsureNodeSearchSelectionInView();
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    private void EnsureNodeSearchSelectionVisible()
    {
        if (_nodeSearchItems.Count == 0)
        {
            _nodeSearchSelectedIndex = -1;
            return;
        }

        _nodeSearchSelectedIndex = ClampNodeSearchIndex(_nodeSearchSelectedIndex);
        if (!IsNodeSearchItemVisible(_nodeSearchSelectedIndex))
        {
            _nodeSearchSelectedIndex = FirstVisibleNodeSearchItemIndex();
        }
    }

    private void MoveNodeSearchSelection(int delta)
    {
        var visibleItemIndices = VisibleNodeSearchItemIndices();
        if (visibleItemIndices.Count == 0)
        {
            _nodeSearchSelectedIndex = -1;
            return;
        }

        int currentPosition = visibleItemIndices.IndexOf(_nodeSearchSelectedIndex);
        if (currentPosition < 0)
        {
            currentPosition = delta >= 0 ? -1 : visibleItemIndices.Count;
        }

        int nextPosition = Math.Clamp(currentPosition + delta, 0, visibleItemIndices.Count - 1);
        _nodeSearchSelectedIndex = visibleItemIndices[nextPosition];
        EnsureNodeSearchSelectionInView();
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    private List<int> VisibleNodeSearchItemIndices()
    {
        var indices = new List<int>();
        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            int itemIndex = _nodeSearchDisplayRows[rowIndex].ItemIndex;
            if (itemIndex >= 0 && itemIndex < _nodeSearchItems.Count)
            {
                indices.Add(itemIndex);
            }
        }

        return indices;
    }

    private bool IsNodeSearchItemVisible(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _nodeSearchItems.Count)
        {
            return false;
        }

        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            if (_nodeSearchDisplayRows[rowIndex].ItemIndex == itemIndex)
            {
                return true;
            }
        }

        return false;
    }

    private int FirstVisibleNodeSearchItemIndex()
    {
        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            int itemIndex = _nodeSearchDisplayRows[rowIndex].ItemIndex;
            if (itemIndex >= 0 && itemIndex < _nodeSearchItems.Count)
            {
                return itemIndex;
            }
        }

        return -1;
    }

    private void EnsureNodeSearchSelectionInView()
    {
        if (_nodeSearchRowsViewportBounds.Height <= 0 || _nodeSearchSelectedIndex < 0)
        {
            ClampNodeSearchScroll();
            return;
        }

        int rowTop = 0;
        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            int rowHeight = Math.Max(1, _nodeSearchDisplayRows[rowIndex].Height);
            if (_nodeSearchDisplayRows[rowIndex].ItemIndex == _nodeSearchSelectedIndex)
            {
                if (rowTop < _nodeSearchScrollY)
                {
                    _nodeSearchScrollY = rowTop;
                }
                else if (rowTop + rowHeight > _nodeSearchScrollY + _nodeSearchRowsViewportBounds.Height)
                {
                    _nodeSearchScrollY = rowTop + rowHeight - _nodeSearchRowsViewportBounds.Height;
                }

                break;
            }

            rowTop += rowHeight;
        }

        ClampNodeSearchScroll();
    }

    private void ClampNodeSearchScroll()
    {
        int maxScroll = Math.Max(0, TotalNodeSearchRowsHeight() - Math.Max(0, _nodeSearchRowsViewportBounds.Height));
        _nodeSearchScrollY = Math.Clamp(_nodeSearchScrollY, 0, maxScroll);
    }

    private int TotalNodeSearchRowsHeight()
    {
        if (_nodeSearchDisplayRows.Count == 0)
        {
            return NodeSearchResultHeight;
        }

        int height = 0;
        for (int rowIndex = 0; rowIndex < _nodeSearchDisplayRows.Count; rowIndex++)
        {
            height += Math.Max(1, _nodeSearchDisplayRows[rowIndex].Height);
        }

        return height;
    }

    private bool HasNodeSearchScrollbar()
    {
        return _nodeSearchRowsViewportBounds.Height > 0
            && TotalNodeSearchRowsHeight() > _nodeSearchRowsViewportBounds.Height;
    }

    private void DrawNodeSearchScrollbar(IUiRenderer renderer)
    {
        if (!HasNodeSearchScrollbar())
        {
            return;
        }

        int totalHeight = TotalNodeSearchRowsHeight();
        int viewportHeight = Math.Max(1, _nodeSearchRowsViewportBounds.Height);
        int trackHeight = viewportHeight;
        int trackX = _nodeSearchRowsViewportBounds.Right - NodeSearchScrollbarWidth;
        var trackBounds = new UiRect(trackX, _nodeSearchRowsViewportBounds.Y, NodeSearchScrollbarWidth, trackHeight);
        int thumbHeight = Math.Clamp((int)Math.Round((double)viewportHeight * viewportHeight / totalHeight), 24, trackHeight);
        int maxScroll = Math.Max(1, totalHeight - viewportHeight);
        int maxThumbTravel = Math.Max(0, trackHeight - thumbHeight);
        int thumbY = trackBounds.Y + (int)Math.Round((double)_nodeSearchScrollY / maxScroll * maxThumbTravel);
        var thumbBounds = new UiRect(trackBounds.X, thumbY, NodeSearchScrollbarWidth, thumbHeight);

        UiRenderHelpers.FillRectRounded(renderer, trackBounds, 3, new UiColor(12, 18, 26, 180));
        UiRenderHelpers.FillRectRounded(renderer, thumbBounds, 3, new UiColor(104, 136, 176, 220));
    }

    private void UpdateValueEditHorizontalScroll(UiNodeControl node, UiNodePin pin, UiFont font)
    {
        UiRect valueBounds = pin.Layout.ValueBounds;
        int padding = Math.Max(0, node.ValueBoxPadding);
        int clipWidth = Math.Max(0, valueBounds.Width - padding * 2);
        if (clipWidth <= 0)
        {
            _valueHorizontalScrollOffset = 0;
            return;
        }

        string text = _valueEditingState.Text;
        int fullWidth = font.MeasureTextWidth(text, node.TextScale);
        if (fullWidth <= clipWidth)
        {
            _valueHorizontalScrollOffset = 0;
            return;
        }

        int caretX = MeasurePrefixWidth(text, _valueEditingState.CaretIndex, node.TextScale, font);
        if (caretX < _valueHorizontalScrollOffset)
        {
            _valueHorizontalScrollOffset = caretX;
        }
        else if (caretX > _valueHorizontalScrollOffset + clipWidth - 2)
        {
            _valueHorizontalScrollOffset = caretX - clipWidth + 2;
        }

        _valueHorizontalScrollOffset = Math.Clamp(_valueHorizontalScrollOffset, 0, Math.Max(0, fullWidth - clipWidth + 2));
    }

    private UiRect GetValueEditCaretBounds(UiNodeControl node, UiNodePin pin, UiFont font)
    {
        UiRect valueBounds = pin.Layout.ValueBounds;
        int padding = Math.Max(0, node.ValueBoxPadding);
        int textHeight = font.MeasureTextHeight(node.TextScale);
        int textY = valueBounds.Y + Math.Max(0, (valueBounds.Height - textHeight) / 2);
        int caretX = valueBounds.X + padding - _valueHorizontalScrollOffset
            + MeasurePrefixWidth(_valueEditingState.Text, _valueEditingState.CaretIndex, node.TextScale, font);
        int caretWidth = Math.Max(1, Math.Min(2, node.TextScale));
        return new UiRect(caretX, textY, caretWidth, textHeight);
    }

    private void ResetValueEditCaretBlink()
    {
        _valueCaretVisible = true;
        _valueCaretTimer = 0f;
    }

    private void ResetCommentEditCaretBlink()
    {
        _commentCaretVisible = true;
        _commentCaretTimer = 0f;
    }

    private void UpdateValueEditCaretBlink(float deltaSeconds)
    {
        _valueCaretTimer += Math.Max(0f, deltaSeconds);
        if (_valueCaretTimer >= 0.5f)
        {
            _valueCaretTimer = 0f;
            _valueCaretVisible = !_valueCaretVisible;
        }
    }

    private void UpdateCommentEditCaretBlink(float deltaSeconds)
    {
        _commentCaretTimer += Math.Max(0f, deltaSeconds);
        if (_commentCaretTimer >= 0.5f)
        {
            _commentCaretTimer = 0f;
            _commentCaretVisible = !_commentCaretVisible;
        }
    }

    private static int MeasurePrefixWidth(string text, int index, int textScale, UiFont font)
    {
        int clampedIndex = Math.Clamp(index, 0, text.Length);
        if (clampedIndex <= 0)
        {
            return 0;
        }

        return font.MeasureTextWidth(text.Substring(0, clampedIndex), textScale);
    }

    private void HandleCanvasViewportChanged(UiCanvasViewportChangedEvent ev)
    {
        ViewportChanged?.Invoke(new UiNodeGraphViewportChangedEvent(this, ev.PanX, ev.PanY, ev.Zoom, ev.Reason));
    }

    private void ProcessGraphCommandInput(UiUpdateContext context, UiInputState input)
    {
        if (!IsGraphCommandScopeActive(context))
        {
            return;
        }

        if (input.IsPrimaryShortcutPressed(UiKey.Z, shift: true)
            || input.IsPrimaryShortcutPressed(UiKey.Y))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.Redo, input.Modifiers));
            return;
        }

        if (input.IsPrimaryShortcutPressed(UiKey.Z))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.Undo, input.Modifiers));
            return;
        }

        if (input.IsPrimaryShortcutPressed(UiKey.A))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.SelectAll, input.Modifiers));
            return;
        }

        if (input.IsPrimaryShortcutPressed(UiKey.C))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.CopySelection, input.Modifiers));
            return;
        }

        if (input.IsPrimaryShortcutPressed(UiKey.V))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.PasteClipboard, input.Modifiers));
            return;
        }

        if (input.IsPrimaryShortcutPressed(UiKey.D))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.DuplicateSelection, input.Modifiers));
            return;
        }

        if (input.Navigation.Delete
            || input.Navigation.Backspace
            || input.IsKeyPressed(UiKey.Delete)
            || input.IsKeyPressed(UiKey.Backspace))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.DeleteSelection, input.Modifiers));
            return;
        }

        if (input.Navigation.Escape || input.IsKeyPressed(UiKey.Escape))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.ClearSelection, input.Modifiers));
            return;
        }

        if (input.Modifiers == UiModifierKeys.None && input.IsKeyPressed(UiKey.C))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.CreateCommentAroundSelection, input.Modifiers));
            return;
        }

        if (input.Modifiers == UiModifierKeys.None && input.IsKeyPressed(UiKey.F))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.FrameSelection, input.Modifiers));
            return;
        }

        if (input.Modifiers == UiModifierKeys.None && input.IsKeyPressed(UiKey.D0))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.ResetZoom, input.Modifiers));
            return;
        }

        if (input.Modifiers == UiModifierKeys.None && input.IsKeyPressed(UiKey.G))
        {
            CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(this, UiNodeGraphCommand.ToggleGrid, input.Modifiers));
        }
    }

    private bool IsGraphCommandScopeActive(UiUpdateContext context)
    {
        if (IsEditingText)
        {
            return false;
        }

        UiElement? focused = context.Focus.Focused;
        if (focused is null || focused.WantsTextInput)
        {
            return false;
        }

        UiElement? current = focused;
        while (current != null)
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private void RefreshGraphState(UiUpdateContext context, UiInputState input)
    {
        EnsureWireRoutes();

        HoveredNode = null;
        HoveredPin = null;
        HoveredValuePin = null;
        HoveredComment = null;
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

        if (_nodeSearchHandledInputThisFrame)
        {
            return;
        }

        if (IsNodeSearchOpen)
        {
            return;
        }

        if (TryOpenNodeSearch(context, input, mouseInViewport, worldMouse))
        {
            return;
        }

        if (mouseInViewport
            && input.LeftDoubleClicked
            && HoveredNode != null
            && HoveredPin == null
            && HoveredValuePin == null)
        {
            NodeDoubleClicked?.Invoke(new UiNodeDoubleClickedEvent(this, HoveredNode, worldMouse, input.Modifiers));
            return;
        }

        if (EnableWireRerouteHandles
            && mouseInViewport
            && input.LeftDoubleClicked
            && HoveredWire != null
            && HoveredPin == null)
        {
            WireRerouteRequested?.Invoke(new UiNodeWireRerouteRequestedEvent(this, HoveredWire, worldMouse, input.Modifiers));
            return;
        }

        if (input.LeftClicked && HoveredNode != null && HoveredValuePin != null)
        {
            TryBeginValueEdit(context, HoveredNode, HoveredValuePin, worldMouse, input);
            return;
        }

        if (mouseInViewport
            && input.LeftClicked
            && HoveredNode == null
            && TryGetCommentEditTarget(worldMouse, out var comment, out var commentField))
        {
            TryBeginCommentEdit(context, comment, commentField);
            return;
        }

        if (ProcessCommentDragInput(context, input, mouseInViewport, worldMouse))
        {
            return;
        }

        RefreshPreviewState(context, input, mouseInViewport, worldMouse);
        if (ProcessBoxSelectionInput(context, input, mouseInViewport, worldMouse))
        {
            return;
        }

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

    private void RefreshNodeLayouts(UiFont defaultFont)
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            _nodes[i].RefreshLayout(defaultFont);
        }
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

            if (HoveredValuePin == null && node.TryGetValuePinAt(worldMouse, out UiNodePin? valuePin))
            {
                HoveredValuePin = valuePin;
                HoveredNode = node;
                break;
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

        if (HoveredPin != null || HoveredValuePin != null)
        {
            return;
        }

        for (int i = _comments.Count - 1; i >= 0; i--)
        {
            UiNodeCommentBox comment = _comments[i];
            if (!comment.Visible || !comment.Enabled)
            {
                continue;
            }

            if (comment.DebugLayout.Bounds.Contains(worldMouse))
            {
                HoveredComment = comment;
                break;
            }
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

    private bool TryGetCommentEditTarget(UiPoint worldMouse, out UiNodeCommentBox comment, out CommentEditField field)
    {
        for (int i = _comments.Count - 1; i >= 0; i--)
        {
            UiNodeCommentBox candidate = _comments[i];
            if (!candidate.Visible || !candidate.Enabled)
            {
                continue;
            }

            UiNodeCommentBoxDebugLayout layout = candidate.DebugLayout;
            if (layout.TitleBounds.Width > 0
                && layout.TitleBounds.Height > 0
                && layout.TitleBounds.Contains(worldMouse))
            {
                comment = candidate;
                field = CommentEditField.Title;
                return true;
            }

            if (layout.BodyTextBounds.Width > 0
                && layout.BodyTextBounds.Height > 0
                && layout.BodyTextBounds.Contains(worldMouse))
            {
                comment = candidate;
                field = CommentEditField.Body;
                return true;
            }

            if ((string.IsNullOrEmpty(candidate.Text) || candidate.IsBodyEditing)
                && layout.BodyBounds.Width > 0
                && layout.BodyBounds.Height > 0
                && layout.BodyBounds.Contains(worldMouse))
            {
                comment = candidate;
                field = CommentEditField.Body;
                return true;
            }
        }

        comment = null!;
        field = CommentEditField.None;
        return false;
    }

    private static UiRect ResolveCommentEditBounds(UiNodeCommentBox comment, CommentEditField field)
    {
        UiNodeCommentBoxDebugLayout layout = comment.DebugLayout;
        return field switch
        {
            CommentEditField.Title => layout.TitleBounds.Width > 0 && layout.TitleBounds.Height > 0 ? layout.TitleBounds : layout.HeaderBounds,
            CommentEditField.Body => layout.BodyTextBounds.Width > 0 && layout.BodyTextBounds.Height > 0 ? layout.BodyTextBounds : layout.BodyBounds,
            _ => default
        };
    }

    private static string ResolveCommentEditText(UiNodeCommentBox comment, CommentEditField field)
    {
        return field switch
        {
            CommentEditField.Title => comment.Title,
            CommentEditField.Body => comment.Text,
            _ => string.Empty
        };
    }

    private static string CommentEditFieldKey(CommentEditField field)
    {
        return field switch
        {
            CommentEditField.Title => "title",
            CommentEditField.Body => "text",
            _ => string.Empty
        };
    }

    private void RefreshPreviewState(UiUpdateContext context, UiInputState input, bool mouseInViewport, UiPoint worldMouse)
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
            UiNodeControl? startNode = _previewStartNode;
            UiNodePin? startPin = _previewStartPin;
            UiNodeControl? targetNode = targetPin is not null ? HoveredNode : null;
            UiNodeWirePreviewState endedState = PreviewWire;
            ClearPreview();
            WirePreviewEnded?.Invoke(endedState, targetPin);
            if (startNode is not null && startPin is not null && targetNode is not null && targetPin is not null)
            {
                WireConnectionRequested?.Invoke(new UiNodeWireConnectionRequestedEvent(this, startNode, startPin, targetNode, targetPin, endedState));
                return;
            }

            if (startNode is not null && startPin is not null && mouseInViewport && EnableNodeSearch)
            {
                OpenNodeSearch(context, input.MousePosition, previewEnd, input.Modifiers, startNode, startPin);
            }
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

    private bool ProcessCommentDragInput(UiUpdateContext context, UiInputState input, bool mouseInViewport, UiPoint worldMouse)
    {
        if (!EnableCommentDragging)
        {
            CancelCommentDrag(input.Modifiers);
            return false;
        }

        if (IsDraggingComment)
        {
            if (input.IsKeyPressed(UiKey.Escape) || input.Navigation.Escape)
            {
                CancelCommentDrag(input.Modifiers);
                return true;
            }

            if (input.LeftDown || input.LeftReleased)
            {
                UiNodeCommentDragEvent ev = UpdateCommentDragState(input, isCompleting: input.LeftReleased);
                if (input.LeftReleased)
                {
                    EndCommentDrag(ev);
                }
                else
                {
                    CommentDragged?.Invoke(ev);
                }

                return true;
            }

            CancelCommentDrag(input.Modifiers);
            return true;
        }

        if (_commentDragArmed)
        {
            if (input.IsKeyPressed(UiKey.Escape) || input.Navigation.Escape || input.IsKeyDown(UiKey.Space))
            {
                _commentDragArmed = false;
                _commentDragComment = null;
                return true;
            }

            if (input.LeftReleased || !input.LeftDown)
            {
                _commentDragArmed = false;
                _commentDragComment = null;
                return false;
            }

            if (HasExceededDragThreshold(_commentDragStartScreen, input.MousePosition, input.DragThreshold))
            {
                BeginCommentDrag(input);
            }

            return true;
        }

        if (CanArmCommentDrag(input, mouseInViewport, worldMouse))
        {
            _commentDragArmed = true;
            _commentDragComment = HoveredComment;
            _commentDragStartScreen = input.MousePosition;
            _commentDragStartWorld = worldMouse;
            _commentDragStartBounds = HoveredComment!.Bounds;
            _commentDragModifiers = input.Modifiers;
            context.Focus.RequestFocus(this);
            return true;
        }

        return false;
    }

    private bool CanArmCommentDrag(UiInputState input, bool mouseInViewport, UiPoint worldMouse)
    {
        return EnableCommentDragging
            && mouseInViewport
            && input.LeftClicked
            && input.LeftDown
            && !input.RightDown
            && !input.MiddleDown
            && !IsEditingText
            && !input.IsKeyDown(UiKey.Space)
            && !IsLeftButtonPanGesture()
            && !PreviewWire.Active
            && _previewStartPin == null
            && HoveredComment != null
            && HoveredNode == null
            && HoveredPin == null
            && HoveredValuePin == null
            && HoveredWire == null
            && !TryGetCommentEditTarget(worldMouse, out _, out _);
    }

    private void BeginCommentDrag(UiInputState input)
    {
        if (_commentDragComment is null)
        {
            _commentDragArmed = false;
            return;
        }

        _commentDragArmed = false;
        IsDraggingComment = true;
        UiNodeCommentDragEvent ev = UpdateCommentDragState(input, isCompleting: false);
        CommentDragStarted?.Invoke(ev);
        CommentDragged?.Invoke(ev);
    }

    private UiNodeCommentDragEvent UpdateCommentDragState(UiInputState input, bool isCompleting)
    {
        UiNodeCommentBox comment = _commentDragComment!;
        UiPoint currentWorld = _canvas.ScreenToWorld(input.MousePosition);
        UiPoint delta = new(currentWorld.X - _commentDragStartWorld.X, currentWorld.Y - _commentDragStartWorld.Y);
        UiRect currentBounds = new(
            _commentDragStartBounds.X + delta.X,
            _commentDragStartBounds.Y + delta.Y,
            _commentDragStartBounds.Width,
            _commentDragStartBounds.Height);
        comment.Bounds = currentBounds;
        _commentDragModifiers = input.Modifiers;
        return new UiNodeCommentDragEvent(this, comment, _commentDragStartBounds, currentBounds, delta, input.Modifiers, isCompleting);
    }

    private void EndCommentDrag(UiNodeCommentDragEvent ev)
    {
        CommentDragEnded?.Invoke(ev);
        ClearCommentDragState();
    }

    private void CancelCommentDrag(UiModifierKeys modifiers)
    {
        if (!IsDraggingComment && !_commentDragArmed)
        {
            return;
        }

        UiNodeCommentBox? comment = _commentDragComment;
        bool wasDragging = IsDraggingComment;
        UiRect currentBounds = comment?.Bounds ?? _commentDragStartBounds;
        if (comment is not null)
        {
            comment.Bounds = _commentDragStartBounds;
        }

        ClearCommentDragState();
        if (wasDragging && comment is not null)
        {
            UiPoint delta = new(currentBounds.X - _commentDragStartBounds.X, currentBounds.Y - _commentDragStartBounds.Y);
            CommentDragCancelled?.Invoke(new UiNodeCommentDragEvent(this, comment, _commentDragStartBounds, currentBounds, delta, modifiers, IsCompleting: false));
        }
    }

    private void ClearCommentDragState()
    {
        _commentDragArmed = false;
        _commentDragComment = null;
        IsDraggingComment = false;
    }

    private bool ProcessBoxSelectionInput(UiUpdateContext context, UiInputState input, bool mouseInViewport, UiPoint worldMouse)
    {
        if (!EnableBoxSelection)
        {
            CancelBoxSelection(input.Modifiers);
            _boxSelectionArmed = false;
            return false;
        }

        if (IsBoxSelecting)
        {
            if (input.IsKeyPressed(UiKey.Escape) || input.Navigation.Escape)
            {
                CancelBoxSelection(input.Modifiers);
                return true;
            }

            if (input.LeftDown || input.LeftReleased)
            {
                UiNodeBoxSelectionEvent ev = UpdateBoxSelectionState(input, isCompleting: input.LeftReleased);
                if (input.LeftReleased)
                {
                    EndBoxSelection(ev);
                }
                else
                {
                    BoxSelectionUpdated?.Invoke(ev);
                }

                return true;
            }

            CancelBoxSelection(input.Modifiers);
            return true;
        }

        if (_boxSelectionArmed)
        {
            if (input.IsKeyPressed(UiKey.Escape) || input.Navigation.Escape || input.IsKeyDown(UiKey.Space))
            {
                _boxSelectionArmed = false;
                return true;
            }

            if (input.LeftReleased || !input.LeftDown)
            {
                if (input.LeftReleased)
                {
                    CommandRequested?.Invoke(new UiNodeGraphCommandRequestedEvent(
                        this,
                        UiNodeGraphCommand.ClearSelection,
                        _boxSelectionModifiers));
                }

                _boxSelectionArmed = false;
                return false;
            }

            if (HasExceededDragThreshold(_boxSelectionStartScreen, input.MousePosition, input.DragThreshold))
            {
                BeginBoxSelection(input);
            }

            return true;
        }

        if (CanArmBoxSelection(input, mouseInViewport, worldMouse))
        {
            _boxSelectionArmed = true;
            _boxSelectionStartScreen = input.MousePosition;
            _boxSelectionStartGraphLocal = ToGraphLocal(input.MousePosition);
            _boxSelectionStartWorld = _canvas.ScreenToWorld(input.MousePosition);
            _boxSelectionModifiers = input.Modifiers;
            context.Focus.RequestFocus(this);
            return true;
        }

        return false;
    }

    private bool CanArmBoxSelection(UiInputState input, bool mouseInViewport, UiPoint worldMouse)
    {
        return EnableBoxSelection
            && mouseInViewport
            && input.LeftClicked
            && input.LeftDown
            && !input.RightDown
            && !input.MiddleDown
            && !IsEditingText
            && !input.IsKeyDown(UiKey.Space)
            && !IsLeftButtonPanGesture()
            && !PreviewWire.Active
            && _previewStartPin == null
            && HoveredNode == null
            && HoveredPin == null
            && HoveredValuePin == null
            && HoveredComment == null
            && HoveredWire == null
            && !TryGetCommentEditTarget(worldMouse, out _, out _);
    }

    private bool IsLeftButtonPanGesture()
    {
        return _canvas.EnablePan && _canvas.PanButton == UiCanvas.UiCanvasPanButton.Left;
    }

    private void BeginBoxSelection(UiInputState input)
    {
        _boxSelectionArmed = false;
        IsBoxSelecting = true;
        UiNodeBoxSelectionEvent ev = UpdateBoxSelectionState(input, isCompleting: false);
        BoxSelectionStarted?.Invoke(ev);
        BoxSelectionUpdated?.Invoke(ev);
    }

    private UiNodeBoxSelectionEvent UpdateBoxSelectionState(UiInputState input, bool isCompleting)
    {
        UiRect graphLocalBounds = RectFromPoints(_boxSelectionStartGraphLocal, ToGraphLocal(input.MousePosition));
        UiRect worldBounds = RectFromPoints(_boxSelectionStartWorld, _canvas.ScreenToWorld(input.MousePosition));
        IReadOnlyList<UiNodeControl> hitNodes = GetBoxSelectionHitNodes(worldBounds);
        _boxSelectionModifiers = input.Modifiers;
        SelectionMarqueeWorldBounds = worldBounds;
        SelectionMarqueeBounds = graphLocalBounds;
        return new UiNodeBoxSelectionEvent(this, graphLocalBounds, worldBounds, hitNodes, input.Modifiers, isCompleting);
    }

    private void EndBoxSelection(UiNodeBoxSelectionEvent ev)
    {
        BoxSelectionEnded?.Invoke(ev);
        ClearBoxSelectionState();
    }

    private void CancelBoxSelection(UiModifierKeys modifiers)
    {
        if (!IsBoxSelecting && !_boxSelectionArmed)
        {
            return;
        }

        UiRect graphLocalBounds = SelectionMarqueeBounds ?? RectFromPoints(_boxSelectionStartGraphLocal, ToGraphLocal(_boxSelectionStartScreen));
        UiRect worldBounds = SelectionMarqueeWorldBounds ?? RectFromPoints(_boxSelectionStartWorld, _boxSelectionStartWorld);
        IReadOnlyList<UiNodeControl> hitNodes = worldBounds.Width > 0 && worldBounds.Height > 0
            ? GetBoxSelectionHitNodes(worldBounds)
            : Array.Empty<UiNodeControl>();
        bool wasSelecting = IsBoxSelecting;
        ClearBoxSelectionState();
        if (wasSelecting)
        {
            BoxSelectionCancelled?.Invoke(new UiNodeBoxSelectionEvent(this, graphLocalBounds, worldBounds, hitNodes, modifiers, IsCompleting: false));
        }
    }

    private void ClearBoxSelectionState()
    {
        _boxSelectionArmed = false;
        IsBoxSelecting = false;
        SelectionMarqueeWorldBounds = null;
        SelectionMarqueeBounds = null;
    }

    private IReadOnlyList<UiNodeControl> GetBoxSelectionHitNodes(UiRect worldBounds)
    {
        if (worldBounds.Width <= 0 || worldBounds.Height <= 0)
        {
            return Array.Empty<UiNodeControl>();
        }

        List<UiNodeControl>? hitNodes = null;
        for (int i = 0; i < _nodes.Count; i++)
        {
            UiNodeControl node = _nodes[i];
            if (!node.Visible || !node.Enabled || !Intersects(worldBounds, node.Bounds))
            {
                continue;
            }

            hitNodes ??= new List<UiNodeControl>();
            hitNodes.Add(node);
        }

        return hitNodes?.ToArray() ?? Array.Empty<UiNodeControl>();
    }

    private UiPoint ToGraphLocal(UiPoint screenPoint)
    {
        return new UiPoint(screenPoint.X - Bounds.X, screenPoint.Y - Bounds.Y);
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

    private static UiRect RectFromPoints(UiPoint first, UiPoint second)
    {
        int left = Math.Min(first.X, second.X);
        int top = Math.Min(first.Y, second.Y);
        int right = Math.Max(first.X, second.X);
        int bottom = Math.Max(first.Y, second.Y);
        return new UiRect(left, top, right - left, bottom - top);
    }

    private static bool Intersects(UiRect a, UiRect b)
    {
        return a.Width > 0
            && a.Height > 0
            && b.Width > 0
            && b.Height > 0
            && a.Left < b.Right
            && a.Right > b.Left
            && a.Top < b.Bottom
            && a.Bottom > b.Top;
    }

    private static bool HasExceededDragThreshold(UiPoint start, UiPoint current, int threshold)
    {
        int dx = current.X - start.X;
        int dy = current.Y - start.Y;
        int safeThreshold = Math.Max(0, threshold);
        return dx * dx + dy * dy >= safeThreshold * safeThreshold;
    }
}

public sealed record UiNodeSelectionRequestedEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiModifierKeys Modifiers);

public sealed record UiNodeDoubleClickedEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiPoint WorldPosition,
    UiModifierKeys Modifiers);

public sealed record UiNodeClickCompletedEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiModifierKeys Modifiers);

public sealed record UiNodeDragEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiRect StartBounds,
    UiRect CurrentBounds,
    UiPoint Delta);

public sealed record UiNodeWireConnectionRequestedEvent(
    UiNodeGraph Graph,
    UiNodeControl StartNode,
    UiNodePin StartPin,
    UiNodeControl TargetNode,
    UiNodePin TargetPin,
    UiNodeWirePreviewState Preview);

public sealed record UiNodeValueEditStartedEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiNodePin Pin,
    string InitialText,
    UiRect ValueBounds);

public sealed record UiNodeValueEditCommittedEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiNodePin Pin,
    string Text);

public sealed record UiNodeValueEditCancelledEvent(
    UiNodeGraph Graph,
    UiNodeControl Node,
    UiNodePin Pin);

public sealed record UiNodeCommentEditStartedEvent(
    UiNodeGraph Graph,
    UiNodeCommentBox Comment,
    string Key,
    string InitialText,
    UiRect EditBounds);

public sealed record UiNodeCommentEditCommittedEvent(
    UiNodeGraph Graph,
    UiNodeCommentBox Comment,
    string Key,
    string Text);

public sealed record UiNodeCommentEditCancelledEvent(
    UiNodeGraph Graph,
    UiNodeCommentBox Comment,
    string Key);

public sealed record UiNodeCommentDragEvent(
    UiNodeGraph Graph,
    UiNodeCommentBox Comment,
    UiRect StartBounds,
    UiRect CurrentBounds,
    UiPoint Delta,
    UiModifierKeys Modifiers,
    bool IsCompleting);

public sealed record UiNodeBoxSelectionEvent(
    UiNodeGraph Graph,
    UiRect GraphLocalBounds,
    UiRect WorldBounds,
    IReadOnlyList<UiNodeControl> HitNodes,
    UiModifierKeys Modifiers,
    bool IsCompleting);

public sealed record UiNodeGraphViewportChangedEvent(
    UiNodeGraph Graph,
    float PanX,
    float PanY,
    float Zoom,
    UiCanvas.UiCanvasViewportChangeReason Reason);

public enum UiNodeGraphCommand
{
    DeleteSelection,
    Undo,
    Redo,
    ClearSelection,
    SelectAll,
    CopySelection,
    PasteClipboard,
    DuplicateSelection,
    CreateCommentAroundSelection,
    FrameSelection,
    ResetZoom,
    ToggleGrid
}

public sealed record UiNodeGraphCommandRequestedEvent(
    UiNodeGraph Graph,
    UiNodeGraphCommand Command,
    UiModifierKeys Modifiers);

public sealed record UiNodeWireRerouteRequestedEvent(
    UiNodeGraph Graph,
    UiNodeWire Wire,
    UiPoint WorldPosition,
    UiModifierKeys Modifiers);

public sealed record UiNodeSearchItem(
    string Id,
    string Title,
    string Category = "",
    string Description = "",
    string SearchText = "",
    IReadOnlyList<string>? CompatiblePins = null);

public sealed record UiNodeSearchDisplayRow(
    string Kind,
    string Text,
    string Category,
    int ItemIndex,
    int Height);

public sealed record UiNodeSearchDebugRow(
    string Kind,
    string Text,
    string Category,
    string ItemId,
    UiRect Bounds,
    bool Selected);

public sealed record UiNodeSearchRequestedEvent(
    UiNodeGraph Graph,
    string Query,
    UiPoint ScreenPosition,
    UiPoint WorldPosition,
    UiModifierKeys Modifiers,
    UiNodeControl? ContextNode = null,
    UiNodePin? ContextPin = null);

public sealed record UiNodeSearchQueryChangedEvent(
    UiNodeGraph Graph,
    string Query,
    UiPoint ScreenPosition,
    UiPoint WorldPosition,
    UiModifierKeys Modifiers,
    UiNodeControl? ContextNode = null,
    UiNodePin? ContextPin = null);

public sealed record UiNodeSearchItemInvokedEvent(
    UiNodeGraph Graph,
    UiNodeSearchItem Item,
    string Query,
    UiPoint ScreenPosition,
    UiPoint WorldPosition,
    UiModifierKeys Modifiers,
    UiNodeControl? ContextNode = null,
    UiNodePin? ContextPin = null);

public sealed record UiNodeSearchClosedEvent(
    UiNodeGraph Graph,
    string Query,
    UiPoint WorldPosition,
    UiModifierKeys Modifiers,
    UiNodeControl? ContextNode = null,
    UiNodePin? ContextPin = null);
