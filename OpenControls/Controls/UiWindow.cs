namespace OpenControls.Controls;

internal static class UiTransientInputSuppression
{
    [ThreadStatic]
    private static List<UiElement>? s_roots;

    public static IDisposable Enter(UiElement root)
    {
        s_roots ??= new List<UiElement>();
        s_roots.Add(root);
        return new Scope(root);
    }

    public static IDisposable Enter(IEnumerable<UiElement> roots)
    {
        UiElement[] snapshot = roots.Distinct().ToArray();
        s_roots ??= new List<UiElement>();
        s_roots.AddRange(snapshot);
        return new MultiScope(snapshot);
    }

    public static bool IsSuppressed(UiElement element)
    {
        if (s_roots == null || s_roots.Count == 0)
        {
            return false;
        }

        for (UiElement? current = element; current != null; current = current.Parent)
        {
            if (s_roots.Contains(current))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class Scope : IDisposable
    {
        private UiElement? _root;

        public Scope(UiElement root)
        {
            _root = root;
        }

        public void Dispose()
        {
            if (_root == null || s_roots == null)
            {
                return;
            }

            int index = s_roots.LastIndexOf(_root);
            if (index >= 0)
            {
                s_roots.RemoveAt(index);
            }

            _root = null;
        }
    }

    private sealed class MultiScope : IDisposable
    {
        private UiElement[]? _roots;

        public MultiScope(UiElement[] roots)
        {
            _roots = roots;
        }

        public void Dispose()
        {
            if (_roots == null || s_roots == null)
            {
                return;
            }

            for (int rootIndex = _roots.Length - 1; rootIndex >= 0; rootIndex--)
            {
                int index = s_roots.LastIndexOf(_roots[rootIndex]);
                if (index >= 0)
                {
                    s_roots.RemoveAt(index);
                }
            }

            _roots = null;
        }
    }
}

public sealed class UiWindow : UiElement
{
    private bool _dragging;
    private UiPoint _dragOffset;
    private bool _resizing;
    private UiPoint _resizeStart;
    private UiRect _resizeStartBounds;
    private UiScrollPanel? _scrollPanel;

    public UiWindow()
    {
        ClipChildren = true;
    }

    public string Title { get; set; } = string.Empty;
    public string TabIconText { get; set; } = string.Empty;
    public bool AllowClose { get; set; } = true;
    public bool ShowTitleBar { get; set; } = true;
    public int TitleBarHeight { get; set; } = 24;
    public int TitleTextScale { get; set; } = 1;
    public bool TitleTextBold { get; set; }
    public int TitlePadding { get; set; } = 4;
    public UiColor Background { get; set; } = new(24, 28, 38);
    public UiColor Border { get; set; } = new(90, 100, 120);
    public UiColor TitleBarColor { get; set; } = new(32, 36, 48);
    public UiColor TitleTextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }
    public bool AllowDrag { get; set; }
    public bool AllowResize { get; set; }
    public bool ShowResizeGrip { get; set; } = true;
    public int ResizeGripSize { get; set; } = 12;
    public UiPoint MinSize { get; set; } = new(80, 60);
    public UiPoint MaxSize { get; set; } = new(int.MaxValue, int.MaxValue);
    public bool ClampResizeToParent { get; set; } = true;
    public UiColor ResizeGripColor { get; set; } = new(90, 100, 120);
    /// <summary>
    /// Optional callback invoked after this window's content bounds are current
    /// and before child elements process input.
    /// </summary>
    public Action<UiRect>? LayoutContent { get; set; }
    public bool ClampToParent { get; set; } = true;
    public bool IsDragging => _dragging;
    public bool IsResizing => _resizing;
    public UiScrollPanel? ScrollPanel => _scrollPanel;
    public UiElement ContentRoot => _scrollPanel != null ? _scrollPanel : this;
    public override bool CapturesPointerInput => true;

    public override UiRect ClipBounds => ContentBounds;

    /// <summary>
    /// Cancels this window's drag/resize gesture and dismisses every descendant
    /// popup, modal, menu, and queued popup-focus request. Native-window hosts can
    /// call this before hiding or deactivating an external peer.
    /// </summary>
    public void CancelTransientInteractions()
    {
        _dragging = false;
        _resizing = false;
        using IDisposable suppression = UiTransientInputSuppression.Enter(this);
        const int maximumDismissPasses = 64;
        for (int pass = 0; pass < maximumDismissPasses; pass++)
        {
            DismissTransientInputLayers(this, notifyClosed: true);
            if (!HasDescendantTransientInputState(this))
            {
                return;
            }
        }

        // A hostile close subscriber can continually attach an already-open
        // replacement. Finish authoritatively without another callback cycle.
        DismissTransientInputLayers(this, notifyClosed: false);
    }

    internal bool HasOpenDescendantInputLayer()
    {
        return HasDescendantTransientInputState(this);
    }

    internal void ForceCancelTransientInteractions()
    {
        _dragging = false;
        _resizing = false;
        using IDisposable suppression = UiTransientInputSuppression.Enter(this);
        DismissTransientInputLayers(this, notifyClosed: false);
    }

    private static bool HasDescendantTransientInputState(UiElement root)
    {
        foreach (UiElement child in root.Children)
        {
            if (child is UiPopup { HasTransientInputState: true }
                || child is UiMenuBar { HasOpenMenu: true }
                || child is UiContextMenuRegion { Popup.HasTransientInputState: true }
                || child is UiContextMenuRegion { Menu.HasOpenMenu: true }
                || HasDescendantTransientInputState(child))
            {
                return true;
            }
        }

        return false;
    }

    private static void DismissTransientInputLayers(UiElement root, bool notifyClosed)
    {
        UiElement[] children = root.Children.ToArray();
        foreach (UiElement child in children)
        {
            if (child is UiPopup popup)
            {
                if (notifyClosed)
                {
                    popup.DismissForAncestorSuppression();
                }
                else
                {
                    popup.ForceDismissForAncestorSuppression();
                }
            }
            else if (child is UiMenuBar menu)
            {
                menu.DismissForAncestorSuppression();
            }
            else if (child is UiContextMenuRegion contextMenu)
            {
                if (notifyClosed)
                {
                    contextMenu.Popup?.DismissForAncestorSuppression();
                }
                else
                {
                    contextMenu.Popup?.ForceDismissForAncestorSuppression();
                }

                contextMenu.Menu?.DismissForAncestorSuppression();
            }

            DismissTransientInputLayers(child, notifyClosed);
        }
    }

    public UiRect ContentBounds
    {
        get
        {
            if (Parent is UiDockHost dockHost && dockHost.HideDockedTitleBars)
            {
                return Bounds;
            }

            if (!ShowTitleBar)
            {
                return Bounds;
            }

            int height = Math.Max(0, Bounds.Height - TitleBarHeight);
            return new UiRect(Bounds.X, Bounds.Y + TitleBarHeight, Bounds.Width, height);
        }
    }

    public UiRect TitleBarBounds => new(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);
    public UiRect ResizeGripBounds
    {
        get
        {
            int size = Math.Max(1, ResizeGripSize);
            return new UiRect(Bounds.Right - size, Bounds.Bottom - size, size, size);
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        using IDisposable scope = UiProfiling.Scope($"OpenControls.Window.Update.{GetProfileName()}");

        UiInputState input = context.Input;
        if (AllowResize)
        {
            if (!_resizing && input.LeftClicked && ResizeGripBounds.Contains(input.MousePosition))
            {
                _resizing = true;
                _resizeStart = input.MousePosition;
                _resizeStartBounds = Bounds;
                context.Focus.RequestFocus(null);
            }

            if (_resizing && input.LeftDown)
            {
                int deltaX = input.MousePosition.X - _resizeStart.X;
                int deltaY = input.MousePosition.Y - _resizeStart.Y;
                int width = _resizeStartBounds.Width + deltaX;
                int height = _resizeStartBounds.Height + deltaY;

                width = Math.Clamp(width, MinSize.X, MaxSize.X);
                height = Math.Clamp(height, MinSize.Y, MaxSize.Y);

                if (ClampResizeToParent && Parent != null)
                {
                    UiRect parentBounds = Parent.Bounds;
                    int maxWidth = Math.Max(1, parentBounds.Right - _resizeStartBounds.X);
                    int maxHeight = Math.Max(1, parentBounds.Bottom - _resizeStartBounds.Y);
                    width = Math.Clamp(width, Math.Min(MinSize.X, maxWidth), maxWidth);
                    height = Math.Clamp(height, Math.Min(MinSize.Y, maxHeight), maxHeight);
                }

                Bounds = new UiRect(_resizeStartBounds.X, _resizeStartBounds.Y, width, height);
            }

            if (_resizing && input.LeftReleased)
            {
                _resizing = false;
            }
        }

        if (!_resizing && AllowDrag && ShowTitleBar)
        {
            if (!_dragging && input.LeftClicked && TitleBarBounds.Contains(input.MousePosition))
            {
                _dragging = true;
                _dragOffset = new UiPoint(input.MousePosition.X - Bounds.X, input.MousePosition.Y - Bounds.Y);
            }

            if (_dragging && input.LeftDown)
            {
                int newX = input.MousePosition.X - _dragOffset.X;
                int newY = input.MousePosition.Y - _dragOffset.Y;

                if (ClampToParent && Parent != null)
                {
                    UiRect parentBounds = Parent.Bounds;
                    int maxX = parentBounds.Right - Bounds.Width;
                    int maxY = parentBounds.Bottom - Bounds.Height;
                    newX = maxX < parentBounds.X
                        ? parentBounds.X
                        : Math.Clamp(newX, parentBounds.X, maxX);
                    newY = maxY < parentBounds.Y
                        ? parentBounds.Y
                        : Math.Clamp(newY, parentBounds.Y, maxY);
                }

                Bounds = new UiRect(newX, newY, Bounds.Width, Bounds.Height);
            }

            if (_dragging && input.LeftReleased)
            {
                _dragging = false;
            }
        }

        ArrangeContent();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        using IDisposable scope = UiProfiling.Scope($"OpenControls.Window.Render.{GetProfileName()}");

        UpdateScrollPanelBounds();
        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);

        base.Render(context);

        if (ClipChildren && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        if (ShowTitleBar)
        {
            UiRect titleBar = new(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);
            if (TitleBarColor.A > 0)
            {
                int titleRadius = Math.Min(CornerRadius, TitleBarHeight / 2);
                if (titleRadius > 0)
                {
                    UiRenderHelpers.FillRectRounded(context.Renderer, titleBar, titleRadius, TitleBarColor);
                    int squareHeight = Math.Max(0, titleBar.Height - titleRadius);
                    if (squareHeight > 0)
                    {
                        UiRect square = new UiRect(titleBar.X, titleBar.Y + titleRadius, titleBar.Width, squareHeight);
                        context.Renderer.FillRect(square, TitleBarColor);
                    }
                }
                else
                {
                    context.Renderer.FillRect(titleBar, TitleBarColor);
                }
            }

            UiFont titleFont = ResolveFont(context.DefaultFont);
            int textHeight = context.Renderer.MeasureTextHeight(TitleTextScale, titleFont);
            int textY = titleBar.Y + (TitleBarHeight - textHeight) / 2;
            UiPoint textPoint = new UiPoint(titleBar.X + TitlePadding, textY);
            if (TitleTextBold)
            {
                UiRenderHelpers.DrawTextBold(context.Renderer, Title, textPoint, TitleTextColor, TitleTextScale, titleFont);
            }
            else
            {
                context.Renderer.DrawText(Title, textPoint, TitleTextColor, TitleTextScale, titleFont);
            }
        }

        if (Border.A > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, 1);
        }

        if (AllowResize && ShowResizeGrip)
        {
            context.Renderer.FillRect(ResizeGripBounds, ResizeGripColor);
        }
    }

    private string GetProfileName()
    {
        if (!string.IsNullOrWhiteSpace(Id))
        {
            return Id;
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            return Title;
        }

        return "Window";
    }

    internal void ArrangeContent()
    {
        UpdateScrollPanelBounds();
        LayoutContent?.Invoke(ContentBounds);
    }

    public UiScrollPanel EnsureScrollPanel()
    {
        if (_scrollPanel != null)
        {
            return _scrollPanel;
        }

        _scrollPanel = new UiScrollPanel
        {
            Background = UiColor.Transparent,
            Border = UiColor.Transparent,
            HorizontalScrollbar = UiScrollbarVisibility.Auto,
            VerticalScrollbar = UiScrollbarVisibility.Auto
        };

        AddChild(_scrollPanel);
        UpdateScrollPanelBounds();
        return _scrollPanel;
    }

    public void AddContentChild(UiElement child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        UiElement target = _scrollPanel != null ? _scrollPanel : this;
        target.AddChild(child);
    }

    private void UpdateScrollPanelBounds()
    {
        if (_scrollPanel == null)
        {
            return;
        }

        _scrollPanel.Bounds = ContentBounds;
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        if (AllowResize && (_resizing || ResizeGripBounds.Contains(input.MousePosition)))
        {
            cursor = UiMouseCursor.ResizeNWSE;
            return true;
        }

        if (AllowDrag && ShowTitleBar && (_dragging || TitleBarBounds.Contains(input.MousePosition)))
        {
            cursor = UiMouseCursor.ResizeAll;
            return true;
        }

        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (_dragging || _resizing)
        {
            status |= UiItemStatusFlags.Active | UiItemStatusFlags.Dragging;
        }

        if (input.LeftClicked && Bounds.Contains(input.MousePosition))
        {
            status |= UiItemStatusFlags.Clicked;
        }

        return status;
    }
}
