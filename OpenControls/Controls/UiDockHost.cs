namespace OpenControls.Controls;

public sealed class UiDockHost : UiElement
{
    private sealed class TabMetricsEntry
    {
        public string Title { get; set; } = string.Empty;
        public string IconText { get; set; } = string.Empty;
        public bool AllowClose { get; set; }
        public int TitleWidth { get; set; }
        public int IconRenderWidth { get; set; }
        public int Width { get; set; }
        public int RenderedTitleAvailableWidth { get; set; } = int.MinValue;
        public string RenderedTitle { get; set; } = string.Empty;
    }

    private enum TabMenuCommand
    {
        Detach,
        Close,
        CloseOthers,
        CloseTabsToRight,
        CloseAll
    }

    private readonly List<UiWindow> _windows = new();
    private readonly List<UiRect> _tabRects = new();
    private readonly List<int> _overflowWindowIndices = new();
    private int _activeIndex = -1;
    private UiWindow? _dragWindow;
    private int _dragIndex = -1;
    private UiPoint _dragStart;
    private bool _dragMoved;
    private int _dragPointerOffsetX;
    private int _dragPointerOffsetY;
    private int _tabScrollOffset;
    private int _tabMaxScroll;
    private UiRect _tabAreaBounds;
    private UiRect _scrollLeftBounds;
    private UiRect _scrollRightBounds;
    private UiRect _overflowButtonBounds;
    private bool _tabsOverflow;
    private int _closeHoverIndex = -1;
    private int _closePressedIndex = -1;
    private bool _scrollLeftHover;
    private bool _scrollRightHover;
    private bool _overflowButtonHover;
    private bool _overflowMenuOpen;
    private UiRect _overflowMenuBounds;
    private int _overflowMenuHoverIndex = -1;
    private bool _contextMenuOpen;
    private UiRect _contextMenuBounds;
    private int _contextMenuTabIndex = -1;
    private int _contextMenuHoverIndex = -1;
    private bool _keepActiveTabVisible = true;
    private UiFont _layoutFont = UiFont.Default;
    private readonly List<TabMetricsEntry> _tabMetrics = new();
    private UiFont _tabMetricsFont = UiFont.Default;
    private int _tabMetricsScale = -1;
    private bool _tabMetricsBold;
    private bool _tabMetricsAutoSizeTabs;
    private int _tabMetricsTabWidth = -1;
    private int _tabMetricsTabMaxWidth = -1;
    private int _tabMetricsTabPadding = -1;
    private int _tabMetricsTabIconSpacing = -1;
    private bool _tabMetricsShowCloseButtons;
    private int _tabMetricsCloseButtonPadding = -1;
    private string _tabMetricsCloseButtonText = string.Empty;
    private UiTabTextOverflowMode _tabMetricsOverflowMode;
    private int _cachedCloseAreaWidth = -1;

    public UiColor Background { get; set; } = new(20, 24, 34);
    public UiColor Border { get; set; } = new(90, 100, 120);
    public UiColor TabBarColor { get; set; } = new(22, 26, 36);
    public UiColor TabInactiveColor { get; set; } = new(22, 26, 36);
    public UiColor TabActiveColor { get; set; } = new(45, 52, 70);
    public UiColor TabHoverColor { get; set; } = new(32, 36, 48);
    public UiColor TabTextColor { get; set; } = UiColor.White;
    public UiColor TabActiveTextColor { get; set; } = UiColor.White;
    public UiColor TabBorderColor { get; set; } = new(60, 70, 90);
    public UiColor TabActiveAccentColor { get; set; } = new(98, 148, 222);
    public UiColor MenuBackground { get; set; } = new(18, 22, 32);
    public UiColor MenuHoverColor { get; set; } = new(36, 42, 58);
    public UiColor MenuBorderColor { get; set; } = new(60, 70, 90);
    public UiColor MenuTextColor { get; set; } = UiColor.White;
    public UiColor MenuDisabledTextColor { get; set; } = new(120, 128, 146);
    public int PanelInset { get; set; }
    public int CornerRadius { get; set; }
    public int TabBarHeight { get; set; } = 24;
    public int TabWidth { get; set; } = 120;
    public int TabMaxWidth { get; set; }
    public int TabPadding { get; set; } = 6;
    public int TabIconSpacing { get; set; } = 4;
    public int TabInset { get; set; } = 0;
    public int TabBottomInset { get; set; } = 0;
    public int TabCornerRadius { get; set; } = 0;
    public int TabActiveAccentHeight { get; set; } = 0;
    public int TabTextScale { get; set; } = 1;
    public bool TabTextBold { get; set; }
    public bool AutoSizeTabs { get; set; }
    public UiTabTextOverflowMode TabTextOverflow { get; set; } = UiTabTextOverflowMode.Clip;
    public bool ShowCloseButtons { get; set; } = true;
    public bool AllowClosingLastWindow { get; set; }
    public UiTabCloseButtonPlacement CloseButtonPlacement { get; set; } = UiTabCloseButtonPlacement.Right;
    public int CloseButtonPadding { get; set; } = 4;
    public string CloseButtonText { get; set; } = "X";
    public int ScrollButtonWidth { get; set; } = 18;
    public int OverflowButtonWidth { get; set; } = 18;
    public int ScrollStep { get; set; } = 80;
    public bool ShowOverflowMenuButton { get; set; } = true;
    public bool ShowTabContextMenu { get; set; } = true;
    public bool HideDockedTitleBars { get; set; } = true;
    public bool AllowReorder { get; set; } = true;
    public bool AllowDetach { get; set; } = true;
    public Func<UiWindow, bool>? CanDetachWindowPredicate { get; set; }
    public int DragThreshold { get; set; } = 6;
    public bool ExternalDragHandling { get; set; }
    public override UiRect ClipBounds => GetPanelBounds();

    public event Action<UiWindow, UiPoint>? TabDetached;
    public event Action<UiWindow>? TabClosed;

    public IReadOnlyList<UiWindow> Windows => _windows;
    public UiWindow? ActiveWindow => _activeIndex >= 0 && _activeIndex < _windows.Count ? _windows[_activeIndex] : null;
    public int ActiveIndex => _activeIndex;
    public bool IsEmpty => _windows.Count == 0;

    public void AddWindow(UiWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_windows.Contains(window))
        {
            return;
        }

        _windows.Add(window);
        AddChild(window);
        if (_activeIndex == -1)
        {
            _activeIndex = 0;
            _keepActiveTabVisible = true;
        }

        SetWindowVisibility();
    }

    public void DockWindow(UiWindow window, int index)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_windows.Contains(window))
        {
            MoveWindow(window, index);
            return;
        }

        index = Math.Clamp(index, 0, _windows.Count);
        _windows.Insert(index, window);
        AddChild(window);
        _activeIndex = index;
        _keepActiveTabVisible = true;
        SetWindowVisibility();
    }

    public bool RemoveWindow(UiWindow window)
    {
        if (!_windows.Remove(window))
        {
            return false;
        }

        RemoveChild(window);
        if (_windows.Count == 0)
        {
            _activeIndex = -1;
            _keepActiveTabVisible = true;
        }
        else if (_activeIndex >= _windows.Count)
        {
            _activeIndex = _windows.Count - 1;
            _keepActiveTabVisible = true;
        }

        if (_contextMenuTabIndex >= _windows.Count)
        {
            _contextMenuOpen = false;
            _contextMenuTabIndex = -1;
        }

        SetWindowVisibility();

        return true;
    }

    public void DockWindow(UiWindow window)
    {
        AddWindow(window);
    }

    public bool TryDetachWindow(int index, UiPoint dropPoint)
    {
        if (!CanDetachWindow(index))
        {
            return false;
        }

        DetachWindow(_windows[index], dropPoint);
        return true;
    }

    public void ClearWindows()
    {
        while (_windows.Count > 0)
        {
            RemoveWindow(_windows[0]);
        }
    }

    public void ActivateWindow(int index)
    {
        if (index < 0 || index >= _windows.Count)
        {
            return;
        }

        _activeIndex = index;
        _keepActiveTabVisible = true;
        SetWindowVisibility();
    }

    public void SyncWindowVisibilityToActiveTab()
    {
        SetWindowVisibility();
    }

    public int GetTabIndexAt(UiPoint point)
    {
        UpdateTabLayout();

        if (!IsPointInTabBar(point))
        {
            return -1;
        }

        if (_tabsOverflow && (_scrollLeftBounds.Contains(point) || _scrollRightBounds.Contains(point) || _overflowButtonBounds.Contains(point)))
        {
            return -1;
        }

        for (int i = 0; i < _tabRects.Count; i++)
        {
            if (_tabRects[i].Contains(point))
            {
                if (CanCloseWindow(i) && GetCloseBounds(i).Contains(point))
                {
                    return -1;
                }

                return i;
            }
        }

        return -1;
    }

    public bool IsPointInTabBar(UiPoint point)
    {
        UiRect panelBounds = GetPanelBounds();
        return point.X >= panelBounds.X
            && point.X < panelBounds.Right
            && point.Y >= panelBounds.Y
            && point.Y < panelBounds.Y + GetEffectiveTabBarHeight(panelBounds);
    }

    public UiRect GetTabBounds(int index)
    {
        UpdateTabLayout();
        return GetTabRect(index);
    }

    public IReadOnlyList<int> GetOverflowWindowIndices()
    {
        UpdateTabLayout();
        return _overflowWindowIndices.ToArray();
    }

    public void MoveWindow(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || fromIndex < 0 || fromIndex >= _windows.Count)
        {
            return;
        }

        UiWindow window = _windows[fromIndex];
        _windows.RemoveAt(fromIndex);
        toIndex = Math.Clamp(toIndex, 0, _windows.Count);
        _windows.Insert(toIndex, window);
        _activeIndex = toIndex;
        _keepActiveTabVisible = true;
        SetWindowVisibility();
    }

    public void MoveWindow(UiWindow window, int index)
    {
        int fromIndex = _windows.IndexOf(window);
        if (fromIndex == -1)
        {
            return;
        }

        MoveWindow(fromIndex, index);
    }

    public int CloseOtherWindows(int keepIndex)
    {
        if (keepIndex < 0 || keepIndex >= _windows.Count)
        {
            return 0;
        }

        UiWindow keepWindow = _windows[keepIndex];
        int closed = 0;
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_windows[i], keepWindow))
            {
                continue;
            }

            if (CloseWindowCore(i, allowLastWindow: true))
            {
                closed++;
            }
        }

        _activeIndex = _windows.IndexOf(keepWindow);
        _keepActiveTabVisible = true;
        UpdateTabLayout();
        return closed;
    }

    public int CloseWindowsToRight(int index)
    {
        if (index < 0 || index >= _windows.Count)
        {
            return 0;
        }

        int closed = 0;
        for (int i = _windows.Count - 1; i > index; i--)
        {
            if (CloseWindowCore(i, allowLastWindow: true))
            {
                closed++;
            }
        }

        UpdateTabLayout();
        return closed;
    }

    public int CloseAllWindows()
    {
        int closed = 0;
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (CloseWindowCore(i, allowLastWindow: true))
            {
                closed++;
            }
        }

        UpdateTabLayout();
        return closed;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _layoutFont = ResolveFont(context.DefaultFont);
        UpdateDockedLayout();
        UpdateTabLayout();
        ValidateTransientMenus();

        UiInputState input = context.Input;
        bool menusWereOpen = _overflowMenuOpen || _contextMenuOpen;
        _closeHoverIndex = GetCloseIndexAt(input.MousePosition);
        _scrollLeftHover = _tabsOverflow && _scrollLeftBounds.Contains(input.MousePosition);
        _scrollRightHover = _tabsOverflow && _scrollRightBounds.Contains(input.MousePosition);
        _overflowButtonHover = HasOverflowButton && _overflowButtonBounds.Contains(input.MousePosition);
        _overflowMenuHoverIndex = GetOverflowMenuIndexAt(input.MousePosition);
        _contextMenuHoverIndex = GetContextMenuCommandIndexAt(input.MousePosition);

        int startingActive = _activeIndex;
        bool handledInteraction = false;

        if (input.Navigation.Escape && (_overflowMenuOpen || _contextMenuOpen))
        {
            CloseTransientMenus();
            ResetTabInteraction();
            handledInteraction = true;
        }

        if (!handledInteraction && input.LeftClicked)
        {
            if (_tabsOverflow && _scrollLeftHover && _tabScrollOffset > 0)
            {
                _keepActiveTabVisible = false;
                ScrollTabs(-1);
                _overflowMenuOpen = false;
                _contextMenuOpen = false;
                _contextMenuTabIndex = -1;
                handledInteraction = true;
            }
            else if (_tabsOverflow && _scrollRightHover && _tabScrollOffset < _tabMaxScroll)
            {
                _keepActiveTabVisible = false;
                ScrollTabs(1);
                _overflowMenuOpen = false;
                _contextMenuOpen = false;
                _contextMenuTabIndex = -1;
                handledInteraction = true;
            }
        }

        if (!handledInteraction && input.RightClicked && ShowTabContextMenu)
        {
            int tabIndex = GetTabIndexAt(input.MousePosition);
            if (tabIndex >= 0)
            {
                _activeIndex = tabIndex;
                _keepActiveTabVisible = true;
                OpenContextMenu(tabIndex, input.MousePosition);
                _overflowMenuOpen = false;
                ResetTabInteraction();
                handledInteraction = true;
            }
            else if (_contextMenuOpen && !_contextMenuBounds.Contains(input.MousePosition))
            {
                _contextMenuOpen = false;
                _contextMenuTabIndex = -1;
            }
        }

        if (!handledInteraction && input.LeftClicked)
        {
            if (_overflowMenuOpen && _overflowMenuHoverIndex >= 0 && _overflowMenuHoverIndex < _overflowWindowIndices.Count)
            {
                _activeIndex = _overflowWindowIndices[_overflowMenuHoverIndex];
                _keepActiveTabVisible = true;
                _overflowMenuOpen = false;
                handledInteraction = true;
            }
            else if (_contextMenuOpen && _contextMenuHoverIndex >= 0)
            {
                ExecuteContextMenuCommand(_contextMenuHoverIndex, input.ScreenMousePosition);
                _contextMenuOpen = false;
                _contextMenuTabIndex = -1;
                handledInteraction = true;
            }
            else if (_overflowButtonHover && _overflowWindowIndices.Count > 0)
            {
                _overflowMenuOpen = !_overflowMenuOpen;
                if (_overflowMenuOpen)
                {
                    _contextMenuOpen = false;
                    UpdateOverflowMenuBounds();
                }

                handledInteraction = true;
            }

            if (!handledInteraction)
            {
                if (_overflowMenuOpen && !_overflowMenuBounds.Contains(input.MousePosition) && !_overflowButtonBounds.Contains(input.MousePosition))
                {
                    _overflowMenuOpen = false;
                }

                if (_contextMenuOpen && !_contextMenuBounds.Contains(input.MousePosition))
                {
                    _contextMenuOpen = false;
                    _contextMenuTabIndex = -1;
                }
            }
        }

        if (!handledInteraction && input.LeftClicked)
        {
            if (_closeHoverIndex >= 0 && CanCloseWindow(_closeHoverIndex))
            {
                _closePressedIndex = _closeHoverIndex;
                _dragWindow = null;
                _dragIndex = -1;
                _dragMoved = false;
            }
        }

        if (!handledInteraction && ExternalDragHandling)
        {
            if (input.LeftReleased && _closePressedIndex >= 0)
            {
                if (_closePressedIndex == _closeHoverIndex && CanCloseWindow(_closePressedIndex))
                {
                    CloseWindow(_closePressedIndex);
                }

                _closePressedIndex = -1;
            }

            if (startingActive != _activeIndex)
            {
                UpdateTabLayout();
            }

            SetWindowVisibility();
            UpdateChildren(context, BlockChildInput(input, menusWereOpen || handledInteraction || _overflowMenuOpen || _contextMenuOpen));
            return;
        }

        if (!handledInteraction && input.LeftClicked && _closePressedIndex < 0 && IsPointInTabBar(input.MousePosition))
        {
            int index = GetTabIndexAt(input.MousePosition);
            if (index >= 0 && index < _windows.Count)
            {
                _activeIndex = index;
                _keepActiveTabVisible = true;
                if (AllowReorder || CanDetachWindow(index))
                {
                    _dragWindow = _windows[index];
                    _dragIndex = index;
                    _dragStart = input.MousePosition;
                    _dragMoved = false;
                    UiRect tabRect = GetTabRect(index);
                    _dragPointerOffsetX = Math.Clamp(input.MousePosition.X - tabRect.X, 0, Math.Max(0, tabRect.Width));
                    _dragPointerOffsetY = Math.Clamp(input.MousePosition.Y - tabRect.Y, 0, Math.Max(0, tabRect.Height));
                }
            }
        }

        if (!handledInteraction && _dragWindow != null && input.LeftDown)
        {
            int deltaX = Math.Abs(input.MousePosition.X - _dragStart.X);
            int deltaY = Math.Abs(input.MousePosition.Y - _dragStart.Y);
            if (!_dragMoved && (deltaX >= DragThreshold || deltaY >= DragThreshold))
            {
                _dragMoved = true;
            }

            if (_dragMoved && AllowReorder && IsPointInTabBar(input.MousePosition))
            {
                int targetIndex = GetReorderIndex(input.MousePosition.X);
                if (targetIndex >= 0 && targetIndex < _windows.Count && targetIndex != _dragIndex)
                {
                    MoveWindow(_dragIndex, targetIndex);
                    _dragIndex = targetIndex;
                }
            }
            else if (_dragMoved && CanDetachWindow(_dragIndex) && !Bounds.Contains(input.MousePosition))
            {
                TryDetachWindow(_dragIndex, GetDetachPoint(input));
                ResetDragInteraction();
            }
        }

        if (!handledInteraction && input.LeftReleased)
        {
            if (_closePressedIndex >= 0)
            {
                if (_closePressedIndex == _closeHoverIndex && CanCloseWindow(_closePressedIndex))
                {
                    CloseWindow(_closePressedIndex);
                }

                _closePressedIndex = -1;
                ResetDragInteraction();
            }
            else if (_dragWindow != null && _dragMoved && CanDetachWindow(_dragIndex) && !Bounds.Contains(input.MousePosition))
            {
                TryDetachWindow(_dragIndex, GetDetachPoint(input));
                ResetDragInteraction();
            }
            else
            {
                ResetDragInteraction();
            }
        }

        if (startingActive != _activeIndex)
        {
            UpdateTabLayout();
        }

        SetWindowVisibility();
        UpdateChildren(context, BlockChildInput(input, menusWereOpen || handledInteraction || _overflowMenuOpen || _contextMenuOpen));
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        using IDisposable scope = UiProfiling.Scope($"OpenControls.DockHost.Render.{GetProfileName()}");

        UiFont font = ResolveFont(context.DefaultFont);
        _layoutFont = font;
        UpdateTabLayout();

        UiRect panelBounds = GetPanelBounds();
        if (panelBounds.Width <= 0 || panelBounds.Height <= 0)
        {
            return;
        }

        FillPanelRect(context, panelBounds, Background);

        int tabHeight = GetEffectiveTabBarHeight(panelBounds);
        UiRect tabBar = new(panelBounds.X, panelBounds.Y, panelBounds.Width, tabHeight);
        UiRect contentBounds = new(
            panelBounds.X,
            panelBounds.Y + tabHeight,
            panelBounds.Width,
            Math.Max(0, panelBounds.Height - tabHeight));
        FillTabBarRect(context, tabBar);

        UiRect clipBounds = _tabsOverflow ? _tabAreaBounds : tabBar;
        context.Renderer.PushClip(clipBounds);
        int closeTextWidth = MeasureTextWidth(GetCloseButtonText(), TabTextScale, font);
        for (int i = 0; i < _windows.Count; i++)
        {
            UiRect tabRect = GetTabRect(i);
            bool active = i == _activeIndex;
            UiColor tabColor = active ? TabActiveColor : (_dragIndex == i ? TabHoverColor : TabInactiveColor);
            UiColor textColor = active ? TabActiveTextColor : TabTextColor;
            UiRect renderRect = GetRenderedTabRect(tabRect);
            if (renderRect.Width <= 0 || renderRect.Height <= 0)
            {
                continue;
            }

            if (TabCornerRadius > 0)
            {
                UiRenderHelpers.FillRectTopRounded(context.Renderer, renderRect, TabCornerRadius, tabColor);
                UiRenderHelpers.DrawRectTopRounded(context.Renderer, renderRect, TabCornerRadius, TabBorderColor, 1);
            }
            else
            {
                context.Renderer.FillRect(renderRect, tabColor);
                context.Renderer.DrawRect(renderRect, TabBorderColor, 1);
            }

            if (active && TabActiveAccentHeight > 0 && TabActiveAccentColor.A > 0)
            {
                UiRect accentRect = new(
                    renderRect.X + 1,
                    renderRect.Y + 1,
                    Math.Max(0, renderRect.Width - 2),
                    Math.Min(TabActiveAccentHeight, Math.Max(0, renderRect.Height - 2)));
                if (accentRect.Width > 0 && accentRect.Height > 0)
                {
                    UiRenderHelpers.FillRectRounded(
                        context.Renderer,
                        accentRect,
                        Math.Min(TabCornerRadius, accentRect.Height),
                        TabActiveAccentColor);
                }
            }

            UiWindow window = _windows[i];
            string title = GetRenderedWindowTitle(i);
            int titleTextY = UiRenderHelpers.GetVerticallyCenteredTextY(renderRect, title, TabTextScale, font);
            int textX = renderRect.X + Math.Max(0, TabPadding);
            if (ShowCloseButtons
                && CloseButtonPlacement == UiTabCloseButtonPlacement.Left
                && CanShowCloseButton(i)
                && closeTextWidth > 0)
            {
                textX += GetCloseAreaWidth();
            }

            if (!string.IsNullOrEmpty(window.TabIconText))
            {
                int iconTextY = UiRenderHelpers.GetVerticallyCenteredTextY(renderRect, window.TabIconText, TabTextScale, font);
                UiColor iconColor = active && TabActiveAccentColor.A > 0 ? TabActiveAccentColor : textColor;
                context.Renderer.DrawText(window.TabIconText, new UiPoint(textX, iconTextY), iconColor, TabTextScale, font);
                textX += GetTabIconRenderWidth(i);
            }

            UiPoint textPoint = new(textX, titleTextY);
            if (TabTextBold)
            {
                UiRenderHelpers.DrawTextBold(context.Renderer, title, textPoint, textColor, TabTextScale, font);
            }
            else
            {
                context.Renderer.DrawText(title, textPoint, textColor, TabTextScale, font);
            }

            if (ShowCloseButtons && CanShowCloseButton(i))
            {
                string closeText = GetCloseButtonText();
                UiRect closeBounds = GetCloseBounds(i);
                int closeGlyphWidth = MeasureTextWidth(closeText, TabTextScale, font);
                int closeTextX = closeBounds.X + (closeBounds.Width - closeGlyphWidth) / 2;
                int closeTextY = UiRenderHelpers.GetVerticallyCenteredTextY(closeBounds, closeText, TabTextScale, font);
                context.Renderer.DrawText(closeText, new UiPoint(closeTextX, closeTextY), TabTextColor, TabTextScale, font);
            }
        }
        context.Renderer.PopClip();

        if (_tabsOverflow)
        {
            bool canScrollLeft = _tabScrollOffset > 0;
            bool canScrollRight = _tabScrollOffset < _tabMaxScroll;
            RenderButton(context, _scrollLeftBounds, UiArrowDirection.Left, _scrollLeftHover, canScrollLeft);
            if (HasOverflowButton)
            {
                RenderOverflowButton(context);
            }

            RenderButton(context, _scrollRightBounds, UiArrowDirection.Right, _scrollRightHover, canScrollRight);
        }

        base.Render(context);

        if (ClipChildren && CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, panelBounds, CornerRadius, Background);
        }

        DrawPanelRect(context, contentBounds, Border);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        base.RenderOverlay(context);

        if (_overflowMenuOpen)
        {
            RenderOverflowMenu(context);
        }

        if (_contextMenuOpen)
        {
            RenderContextMenu(context);
        }
    }

    private void UpdateTabLayout()
    {
        _tabRects.Clear();
        _overflowWindowIndices.Clear();

        UiRect panelBounds = GetPanelBounds();
        int tabHeight = GetEffectiveTabBarHeight(panelBounds);
        if (_windows.Count == 0)
        {
            _activeIndex = -1;
            _tabScrollOffset = 0;
            _tabMaxScroll = 0;
            _tabsOverflow = false;
            _keepActiveTabVisible = true;
            _tabAreaBounds = new UiRect(panelBounds.X, panelBounds.Y, panelBounds.Width, tabHeight);
            _scrollLeftBounds = default;
            _scrollRightBounds = default;
            _overflowButtonBounds = default;
            return;
        }

        if (_activeIndex < 0 || _activeIndex >= _windows.Count)
        {
            _activeIndex = Math.Clamp(_activeIndex, 0, _windows.Count - 1);
            _keepActiveTabVisible = true;
        }

        int totalWidth = GetTotalTabWidth();
        _tabsOverflow = totalWidth > panelBounds.Width;

        int scrollButtonWidth = _tabsOverflow ? Math.Max(0, ScrollButtonWidth) : 0;
        int overflowButtonWidth = HasOverflowButton ? Math.Max(0, OverflowButtonWidth) : 0;
        if (_tabsOverflow)
        {
            scrollButtonWidth = Math.Min(scrollButtonWidth, Math.Max(0, panelBounds.Width / 3));
            overflowButtonWidth = Math.Min(overflowButtonWidth, Math.Max(0, panelBounds.Width - scrollButtonWidth * 2));
        }

        int tabAreaWidth = Math.Max(0, panelBounds.Width - scrollButtonWidth * 2 - overflowButtonWidth);
        _scrollLeftBounds = new UiRect(panelBounds.X, panelBounds.Y, scrollButtonWidth, tabHeight);
        _tabAreaBounds = new UiRect(panelBounds.X + scrollButtonWidth, panelBounds.Y, tabAreaWidth, tabHeight);
        _overflowButtonBounds = overflowButtonWidth > 0
            ? new UiRect(_tabAreaBounds.Right, panelBounds.Y, overflowButtonWidth, tabHeight)
            : default;
        _scrollRightBounds = new UiRect(panelBounds.Right - scrollButtonWidth, panelBounds.Y, scrollButtonWidth, tabHeight);

        _tabMaxScroll = Math.Max(0, totalWidth - tabAreaWidth);
        _tabScrollOffset = Math.Clamp(_tabScrollOffset, 0, _tabMaxScroll);

        LayoutTabRects(_tabAreaBounds.X - _tabScrollOffset, tabHeight);

        if (_tabsOverflow && _keepActiveTabVisible && _activeIndex >= 0 && _activeIndex < _windows.Count)
        {
            int previousScroll = _tabScrollOffset;
            EnsureActiveVisible(GetTabRect(_activeIndex));
            _tabScrollOffset = Math.Clamp(_tabScrollOffset, 0, _tabMaxScroll);
            if (_tabScrollOffset != previousScroll)
            {
                LayoutTabRects(_tabAreaBounds.X - _tabScrollOffset, tabHeight);
            }
        }

        UpdateOverflowWindowIndices();
        if (_overflowWindowIndices.Count == 0)
        {
            _overflowMenuOpen = false;
        }
        else if (_overflowMenuOpen)
        {
            UpdateOverflowMenuBounds();
        }
    }

    private void LayoutTabRects(int startX, int tabHeight)
    {
        _tabRects.Clear();
        int x = startX;
        for (int i = 0; i < _windows.Count; i++)
        {
            int width = GetTabWidth(i);
            _tabRects.Add(new UiRect(x, _tabAreaBounds.Y, width, tabHeight));
            x += width;
        }
    }

    private void UpdateOverflowWindowIndices()
    {
        _overflowWindowIndices.Clear();
        if (!_tabsOverflow || _tabAreaBounds.Width <= 0)
        {
            return;
        }

        for (int i = 0; i < _tabRects.Count; i++)
        {
            UiRect rect = _tabRects[i];
            if (rect.X < _tabAreaBounds.X || rect.Right > _tabAreaBounds.Right)
            {
                _overflowWindowIndices.Add(i);
            }
        }
    }

    private void EnsureActiveVisible(UiRect tabBounds)
    {
        if (!_tabsOverflow || _tabAreaBounds.Width <= 0)
        {
            return;
        }

        if (tabBounds.X < _tabAreaBounds.X)
        {
            _tabScrollOffset = Math.Max(0, _tabScrollOffset - (_tabAreaBounds.X - tabBounds.X));
        }
        else if (tabBounds.Right > _tabAreaBounds.Right)
        {
            _tabScrollOffset = Math.Min(_tabMaxScroll, _tabScrollOffset + (tabBounds.Right - _tabAreaBounds.Right));
        }
    }

    private int GetTotalTabWidth()
    {
        int total = 0;
        for (int i = 0; i < _windows.Count; i++)
        {
            total += GetTabWidth(i);
        }

        return total;
    }

    private string GetProfileName()
    {
        return !string.IsNullOrWhiteSpace(Id)
            ? Id
            : "DockHost";
    }

    private void EnsureTabMetrics()
    {
        bool invalidateAll = !ReferenceEquals(_tabMetricsFont, _layoutFont)
            || _tabMetricsScale != TabTextScale
            || _tabMetricsBold != TabTextBold
            || _tabMetricsAutoSizeTabs != AutoSizeTabs
            || _tabMetricsTabWidth != TabWidth
            || _tabMetricsTabMaxWidth != TabMaxWidth
            || _tabMetricsTabPadding != TabPadding
            || _tabMetricsTabIconSpacing != TabIconSpacing
            || _tabMetricsShowCloseButtons != ShowCloseButtons
            || _tabMetricsCloseButtonPadding != CloseButtonPadding
            || !string.Equals(_tabMetricsCloseButtonText, CloseButtonText, StringComparison.Ordinal)
            || _tabMetricsOverflowMode != TabTextOverflow;

        if (invalidateAll)
        {
            _tabMetrics.Clear();
            _cachedCloseAreaWidth = -1;
            _tabMetricsFont = _layoutFont;
            _tabMetricsScale = TabTextScale;
            _tabMetricsBold = TabTextBold;
            _tabMetricsAutoSizeTabs = AutoSizeTabs;
            _tabMetricsTabWidth = TabWidth;
            _tabMetricsTabMaxWidth = TabMaxWidth;
            _tabMetricsTabPadding = TabPadding;
            _tabMetricsTabIconSpacing = TabIconSpacing;
            _tabMetricsShowCloseButtons = ShowCloseButtons;
            _tabMetricsCloseButtonPadding = CloseButtonPadding;
            _tabMetricsCloseButtonText = CloseButtonText ?? string.Empty;
            _tabMetricsOverflowMode = TabTextOverflow;
        }

        while (_tabMetrics.Count < _windows.Count)
        {
            _tabMetrics.Add(new TabMetricsEntry());
        }

        if (_tabMetrics.Count > _windows.Count)
        {
            _tabMetrics.RemoveRange(_windows.Count, _tabMetrics.Count - _windows.Count);
        }
    }

    private TabMetricsEntry GetTabMetrics(int index)
    {
        EnsureTabMetrics();

        if (index < 0 || index >= _windows.Count)
        {
            return new TabMetricsEntry();
        }

        UiWindow window = _windows[index];
        TabMetricsEntry entry = _tabMetrics[index];
        string title = window.Title ?? string.Empty;
        string iconText = window.TabIconText ?? string.Empty;
        bool allowClose = window.AllowClose;
        if (entry.Title != title || entry.IconText != iconText || entry.AllowClose != allowClose)
        {
            entry.Title = title;
            entry.IconText = iconText;
            entry.AllowClose = allowClose;
            entry.TitleWidth = MeasureTextWidth(title, TabTextScale, _layoutFont);
            entry.IconRenderWidth = CalculateTabIconRenderWidth(iconText);
            entry.Width = CalculateTabWidth(entry);
            entry.RenderedTitleAvailableWidth = int.MinValue;
            entry.RenderedTitle = title;
        }

        return entry;
    }

    private int GetTabWidth(int index)
    {
        return GetTabMetrics(index).Width;
    }

    private int CalculateTabWidth(TabMetricsEntry entry)
    {
        int width = Math.Max(0, TabWidth);
        if (AutoSizeTabs)
        {
            int textWidth = entry.TitleWidth;
            int iconWidth = entry.IconRenderWidth;
            if (TabTextBold && textWidth > 0)
            {
                textWidth += 1;
            }

            width = textWidth + iconWidth + Math.Max(0, TabPadding) * 2;
            if (ShowCloseButtons && entry.AllowClose)
            {
                width += GetCloseAreaWidth();
            }

            if (TabWidth > 0)
            {
                width = Math.Max(width, TabWidth);
            }
        }

        if (TabMaxWidth > 0)
        {
            width = Math.Min(width, TabMaxWidth);
        }

        return Math.Max(0, width);
    }

    private int GetCloseAreaWidth()
    {
        if (!ShowCloseButtons)
        {
            return 0;
        }

        if (_cachedCloseAreaWidth >= 0)
        {
            return _cachedCloseAreaWidth;
        }

        int padding = Math.Max(0, CloseButtonPadding);
        int glyphWidth = MeasureTextWidth(GetCloseButtonText(), TabTextScale, _layoutFont);
        _cachedCloseAreaWidth = glyphWidth + padding * 2;
        return _cachedCloseAreaWidth;
    }

    private string GetCloseButtonText()
        => string.IsNullOrEmpty(CloseButtonText) ? "X" : CloseButtonText;

    private UiRect GetCloseBounds(int index)
    {
        if (index < 0 || index >= _tabRects.Count)
        {
            return default;
        }

        int closeWidth = GetCloseAreaWidth();
        if (closeWidth <= 0)
        {
            return default;
        }

        UiRect tabRect = _tabRects[index];
        int closeX = CloseButtonPlacement == UiTabCloseButtonPlacement.Left
            ? tabRect.X
            : tabRect.Right - closeWidth;
        return new UiRect(closeX, tabRect.Y, closeWidth, tabRect.Height);
    }

    private int GetCloseIndexAt(UiPoint point)
    {
        if (!ShowCloseButtons)
        {
            return -1;
        }

        for (int i = 0; i < _windows.Count; i++)
        {
            if (!CanShowCloseButton(i))
            {
                continue;
            }

            if (GetCloseBounds(i).Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    private bool CanRemoveWindow(int index)
    {
        return index >= 0
            && index < _windows.Count
            && _windows[index].AllowClose
            && _windows[index].Enabled;
    }

    private bool CanCloseWindow(int index)
    {
        return CanRemoveWindow(index) && (AllowClosingLastWindow || _windows.Count > 1);
    }

    private bool CanShowCloseButton(int index)
    {
        return index == _activeIndex && CanCloseWindow(index);
    }

    private bool CloseWindow(int index)
    {
        return CloseWindowCore(index, allowLastWindow: false);
    }

    private bool CloseWindowCore(int index, bool allowLastWindow)
    {
        bool canCloseLastWindow = allowLastWindow || AllowClosingLastWindow;
        if (!CanRemoveWindow(index) || (!canCloseLastWindow && _windows.Count <= 1))
        {
            return false;
        }

        UiWindow window = _windows[index];
        bool removed = RemoveWindow(window);
        if (!removed)
        {
            return false;
        }

        TabClosed?.Invoke(window);
        if (_dragWindow == window)
        {
            _dragWindow = null;
            _dragIndex = -1;
            _dragMoved = false;
        }

        UpdateTabLayout();
        return true;
    }

    private void ScrollTabs(int direction)
    {
        int step = Math.Max(1, ScrollStep);
        if (direction < 0)
        {
            _tabScrollOffset = Math.Max(0, _tabScrollOffset - step);
        }
        else if (direction > 0)
        {
            _tabScrollOffset = Math.Min(_tabMaxScroll, _tabScrollOffset + step);
        }

        UpdateTabLayout();
    }

    private void RenderButton(UiRenderContext context, UiRect bounds, UiArrowDirection direction, bool hover, bool enabled)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        UiColor background = hover && enabled ? TabHoverColor : TabBarColor;
        UiRect renderRect = GetRenderedButtonRect(bounds);
        if (TabCornerRadius > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, renderRect, Math.Max(0, TabCornerRadius - 1), background);
            UiRenderHelpers.DrawRectRounded(context.Renderer, renderRect, Math.Max(0, TabCornerRadius - 1), TabBorderColor, 1);
        }
        else
        {
            context.Renderer.FillRect(renderRect, background);
            context.Renderer.DrawRect(renderRect, TabBorderColor, 1);
        }

        UiColor arrowColor = enabled ? TabTextColor : TabBorderColor;
        int inset = Math.Max(2, renderRect.Height / 4);
        UiRect arrowBounds = new(
            renderRect.X + inset,
            renderRect.Y + inset,
            Math.Max(0, renderRect.Width - inset * 2),
            Math.Max(0, renderRect.Height - inset * 2));
        UiArrow.DrawTriangle(context.Renderer, arrowBounds, direction, arrowColor);
    }

    private void RenderOverflowButton(UiRenderContext context)
    {
        if (_overflowButtonBounds.Width <= 0 || _overflowButtonBounds.Height <= 0)
        {
            return;
        }

        UiColor background = (_overflowButtonHover || _overflowMenuOpen) ? TabHoverColor : TabBarColor;
        UiRect renderRect = GetRenderedButtonRect(_overflowButtonBounds);
        if (TabCornerRadius > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, renderRect, Math.Max(0, TabCornerRadius - 1), background);
            UiRenderHelpers.DrawRectRounded(context.Renderer, renderRect, Math.Max(0, TabCornerRadius - 1), TabBorderColor, 1);
        }
        else
        {
            context.Renderer.FillRect(renderRect, background);
            context.Renderer.DrawRect(renderRect, TabBorderColor, 1);
        }

        int inset = Math.Max(2, renderRect.Height / 4);
        UiRect arrowBounds = new(
            renderRect.X + inset,
            renderRect.Y + inset,
            Math.Max(0, renderRect.Width - inset * 2),
            Math.Max(0, renderRect.Height - inset * 2));
        UiArrow.DrawTriangle(context.Renderer, arrowBounds, UiArrowDirection.Down, TabTextColor);
    }

    private UiRect GetRenderedTabRect(UiRect tabRect)
    {
        int inset = Math.Max(0, TabInset);
        int bottomInset = Math.Max(0, TabBottomInset);
        int width = Math.Max(0, tabRect.Width - inset * 2);
        int height = Math.Max(0, tabRect.Height - inset - bottomInset);
        return new UiRect(tabRect.X + inset, tabRect.Y + inset, width, height);
    }

    private UiRect GetRenderedButtonRect(UiRect bounds)
    {
        int inset = Math.Max(0, TabInset);
        int width = Math.Max(0, bounds.Width - inset * 2);
        int height = Math.Max(0, bounds.Height - inset * 2);
        return new UiRect(bounds.X + inset, bounds.Y + inset, width, height);
    }

    private string GetRenderedWindowTitle(int index)
    {
        if (index < 0 || index >= _windows.Count)
        {
            return string.Empty;
        }

        string title = _windows[index].Title ?? string.Empty;
        if (TabTextOverflow != UiTabTextOverflowMode.Ellipsis)
        {
            return title;
        }

        UiRect tabRect = GetTabRect(index);
        int availableWidth = Math.Max(0, tabRect.Width - Math.Max(0, TabPadding) * 2);
        TabMetricsEntry metrics = GetTabMetrics(index);
        availableWidth = Math.Max(0, availableWidth - metrics.IconRenderWidth);
        if (ShowCloseButtons && CanRemoveWindow(index))
        {
            availableWidth = Math.Max(0, availableWidth - GetCloseAreaWidth());
        }

        if (TabTextBold && availableWidth > 0)
        {
            availableWidth = Math.Max(0, availableWidth - 1);
        }

        if (metrics.RenderedTitleAvailableWidth != availableWidth)
        {
            metrics.RenderedTitleAvailableWidth = availableWidth;
            metrics.RenderedTitle = UiTextHelpers.BuildElidedText(title, availableWidth, TabTextScale, _layoutFont);
        }

        return metrics.RenderedTitle;
    }

    private int GetTabIconRenderWidth(int index)
    {
        if (index < 0 || index >= _windows.Count)
        {
            return 0;
        }

        return GetTabMetrics(index).IconRenderWidth;
    }

    private int CalculateTabIconRenderWidth(string iconText)
    {
        if (string.IsNullOrEmpty(iconText))
        {
            return 0;
        }

        int iconWidth = MeasureTextWidth(iconText, TabTextScale, _layoutFont);
        if (iconWidth <= 0)
        {
            return 0;
        }

        return iconWidth + Math.Max(0, TabIconSpacing);
    }

    private static int MeasureTextWidth(string text, int scale, UiFont font)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return font.MeasureTextWidth(text, Math.Max(1, scale));
    }

    private UiRect GetPanelBounds()
    {
        int inset = Math.Max(0, PanelInset);
        int maxInset = Math.Min(Math.Max(0, Bounds.Width) / 2, Math.Max(0, Bounds.Height) / 2);
        inset = Math.Min(inset, maxInset);
        return new UiRect(
            Bounds.X + inset,
            Bounds.Y + inset,
            Math.Max(0, Bounds.Width - inset * 2),
            Math.Max(0, Bounds.Height - inset * 2));
    }

    private int GetEffectiveTabBarHeight(UiRect panelBounds)
    {
        return Math.Min(Math.Max(0, TabBarHeight), Math.Max(0, panelBounds.Height));
    }

    private void FillPanelRect(UiRenderContext context, UiRect bounds, UiColor color)
    {
        if (color.A <= 0)
        {
            return;
        }

        if (CornerRadius > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, bounds, CornerRadius, color);
            return;
        }

        context.Renderer.FillRect(bounds, color);
    }

    private void DrawPanelRect(UiRenderContext context, UiRect bounds, UiColor color)
    {
        if (color.A <= 0)
        {
            return;
        }

        if (CornerRadius > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, bounds, CornerRadius, color, 1);
            return;
        }

        context.Renderer.DrawRect(bounds, color, 1);
    }

    private void FillTabBarRect(UiRenderContext context, UiRect tabBar)
    {
        if (tabBar.Width <= 0 || tabBar.Height <= 0 || TabBarColor.A <= 0)
        {
            return;
        }

        if (CornerRadius <= 0)
        {
            context.Renderer.FillRect(tabBar, TabBarColor);
            return;
        }

        int radius = Math.Min(CornerRadius, Math.Max(1, tabBar.Height / 2));
        UiRenderHelpers.FillRectRounded(context.Renderer, tabBar, radius, TabBarColor);
        int squareHeight = Math.Max(0, tabBar.Height - radius);
        if (squareHeight > 0)
        {
            context.Renderer.FillRect(
                new UiRect(tabBar.X, tabBar.Y + radius, tabBar.Width, squareHeight),
                TabBarColor);
        }
    }

    private void UpdateDockedLayout()
    {
        UiRect panelBounds = GetPanelBounds();
        int tabHeight = GetEffectiveTabBarHeight(panelBounds);
        UiRect content = new(panelBounds.X, panelBounds.Y + tabHeight, panelBounds.Width, Math.Max(0, panelBounds.Height - tabHeight));
        foreach (UiWindow window in _windows)
        {
            window.Bounds = content;
            if (HideDockedTitleBars)
            {
                window.ShowTitleBar = false;
            }
        }
    }

    private void SetWindowVisibility()
    {
        for (int i = 0; i < _windows.Count; i++)
        {
            _windows[i].Visible = i == _activeIndex;
        }
    }

    private void DetachWindow(UiWindow window, UiPoint dropPoint)
    {
        if (HideDockedTitleBars)
        {
            window.ShowTitleBar = true;
        }

        RemoveWindow(window);
        TabDetached?.Invoke(window, dropPoint);
    }

    private int GetReorderIndex(int mouseX)
    {
        if (_tabRects.Count == 0)
        {
            return -1;
        }

        for (int i = 0; i < _tabRects.Count; i++)
        {
            UiRect rect = _tabRects[i];
            int midpoint = rect.X + rect.Width / 2;
            if (mouseX < midpoint)
            {
                return i;
            }
        }

        return _tabRects.Count - 1;
    }

    private UiPoint GetDetachPoint(UiInputState input)
    {
        return new UiPoint(
            input.ScreenMousePosition.X - _dragPointerOffsetX,
            input.ScreenMousePosition.Y - _dragPointerOffsetY);
    }

    private void ResetDragInteraction()
    {
        _dragWindow = null;
        _dragIndex = -1;
        _dragMoved = false;
        _dragPointerOffsetX = 0;
        _dragPointerOffsetY = 0;
    }

    private UiRect GetTabRect(int index)
    {
        return index >= 0 && index < _tabRects.Count ? _tabRects[index] : default;
    }

    private bool HasClosableWindowOtherThan(int index)
    {
        for (int i = 0; i < _windows.Count; i++)
        {
            if (i == index)
            {
                continue;
            }

            if (CanRemoveWindow(i))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasClosableWindowToRight(int index)
    {
        for (int i = index + 1; i < _windows.Count; i++)
        {
            if (CanRemoveWindow(i))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnyClosableWindow()
    {
        for (int i = 0; i < _windows.Count; i++)
        {
            if (CanRemoveWindow(i))
            {
                return true;
            }
        }

        return false;
    }

    private void ValidateTransientMenus()
    {
        if (_overflowWindowIndices.Count == 0)
        {
            _overflowMenuOpen = false;
        }

        if (_contextMenuTabIndex < 0 || _contextMenuTabIndex >= _windows.Count)
        {
            _contextMenuOpen = false;
            _contextMenuTabIndex = -1;
        }
    }

    private void ResetTabInteraction()
    {
        _closePressedIndex = -1;
        _dragWindow = null;
        _dragIndex = -1;
        _dragMoved = false;
    }

    private void CloseTransientMenus()
    {
        _overflowMenuOpen = false;
        _contextMenuOpen = false;
        _contextMenuTabIndex = -1;
    }

    private static UiInputState BlockChildInput(UiInputState input, bool block)
    {
        if (!block)
        {
            return input;
        }

        UiPoint blockedPoint = new(-1_000_000, -1_000_000);
        return new UiInputState
        {
            MousePosition = blockedPoint,
            ScreenMousePosition = blockedPoint,
            DragThreshold = input.DragThreshold,
            Composition = UiTextCompositionState.Empty
        };
    }

    private void UpdateChildren(UiUpdateContext context, UiInputState childInput)
    {
        int childCount = Children.Count;
        if (childCount == 0)
        {
            return;
        }

        UiElement[] snapshot = new UiElement[childCount];
        for (int index = 0; index < childCount; index++)
        {
            snapshot[index] = Children[index];
        }

        foreach (UiElement child in snapshot)
        {
            if (ReferenceEquals(child.Parent, this))
            {
                child.Update(context.CreateChildContext(this, child, childInput));
            }
        }
    }

    private bool HasOverflowButton => _tabsOverflow && ShowOverflowMenuButton && OverflowButtonWidth > 0;

    private void UpdateOverflowMenuBounds()
    {
        if (_overflowWindowIndices.Count == 0)
        {
            _overflowMenuBounds = default;
            return;
        }

        int itemHeight = GetMenuItemHeight();
        int width = GetOverflowMenuWidth();
        int height = _overflowWindowIndices.Count * itemHeight;
        int x = _overflowButtonBounds.Right - width;
        int y = Bounds.Y + TabBarHeight;
        if (x < Bounds.X)
        {
            x = Bounds.X;
        }

        if (x + width > Bounds.Right)
        {
            x = Math.Max(Bounds.X, Bounds.Right - width);
        }

        if (y + height > Bounds.Bottom)
        {
            y = Math.Max(Bounds.Y + TabBarHeight, Bounds.Bottom - height);
        }

        _overflowMenuBounds = new UiRect(x, y, width, height);
    }

    private void OpenContextMenu(int tabIndex, UiPoint point)
    {
        if (!ShowTabContextMenu || tabIndex < 0 || tabIndex >= _windows.Count)
        {
            return;
        }

        _contextMenuTabIndex = tabIndex;
        _contextMenuOpen = true;
        _overflowMenuOpen = false;

        int itemHeight = GetMenuItemHeight();
        int width = GetContextMenuWidth();
        int height = GetContextMenuCommands().Count * itemHeight;
        int x = point.X;
        int y = Bounds.Y + TabBarHeight;
        if (x + width > Bounds.Right)
        {
            x = Math.Max(Bounds.X, Bounds.Right - width);
        }

        if (x < Bounds.X)
        {
            x = Bounds.X;
        }

        if (y + height > Bounds.Bottom)
        {
            y = Math.Max(Bounds.Y + TabBarHeight, Bounds.Bottom - height);
        }

        _contextMenuBounds = new UiRect(x, y, width, height);
    }

    private int GetOverflowMenuIndexAt(UiPoint point)
    {
        if (!_overflowMenuOpen || !_overflowMenuBounds.Contains(point))
        {
            return -1;
        }

        int itemHeight = GetMenuItemHeight();
        int relativeY = point.Y - _overflowMenuBounds.Y;
        int index = itemHeight > 0 ? relativeY / itemHeight : -1;
        return index >= 0 && index < _overflowWindowIndices.Count ? index : -1;
    }

    private int GetContextMenuCommandIndexAt(UiPoint point)
    {
        if (!_contextMenuOpen || !_contextMenuBounds.Contains(point))
        {
            return -1;
        }

        int itemHeight = GetMenuItemHeight();
        int relativeY = point.Y - _contextMenuBounds.Y;
        int index = itemHeight > 0 ? relativeY / itemHeight : -1;
        IReadOnlyList<TabMenuCommand> commands = GetContextMenuCommands();
        return index >= 0 && index < commands.Count && IsContextCommandEnabled(index) ? index : -1;
    }

    private int GetMenuItemHeight()
    {
        return Math.Max(20, TabBarHeight);
    }

    private int GetOverflowMenuWidth()
    {
        int width = 120;
        for (int i = 0; i < _overflowWindowIndices.Count; i++)
        {
            int windowIndex = _overflowWindowIndices[i];
            if (windowIndex < 0 || windowIndex >= _windows.Count)
            {
                continue;
            }

            width = Math.Max(width, MeasureTextWidth(_windows[windowIndex].Title, TabTextScale, _layoutFont) + Math.Max(0, TabPadding) * 2);
        }

        return Math.Min(Math.Max(120, width), Math.Max(120, Bounds.Width));
    }

    private int GetContextMenuWidth()
    {
        int width = 140;
        IReadOnlyList<TabMenuCommand> commands = GetContextMenuCommands();
        for (int i = 0; i < commands.Count; i++)
        {
            width = Math.Max(width, MeasureTextWidth(GetContextMenuLabel(i), TabTextScale, _layoutFont) + Math.Max(0, TabPadding) * 2);
        }

        return Math.Min(Math.Max(140, width), Math.Max(140, Bounds.Width));
    }

    private IReadOnlyList<TabMenuCommand> GetContextMenuCommands()
    {
        if (_contextMenuTabIndex >= 0 && _contextMenuTabIndex < _windows.Count && CanDetachWindow(_contextMenuTabIndex))
        {
            return
            [
                TabMenuCommand.Detach,
                TabMenuCommand.Close,
                TabMenuCommand.CloseOthers,
                TabMenuCommand.CloseTabsToRight,
                TabMenuCommand.CloseAll
            ];
        }

        return
        [
            TabMenuCommand.Close,
            TabMenuCommand.CloseOthers,
            TabMenuCommand.CloseTabsToRight,
            TabMenuCommand.CloseAll
        ];
    }

    private string GetContextMenuLabel(int index)
    {
        IReadOnlyList<TabMenuCommand> commands = GetContextMenuCommands();
        if (index < 0 || index >= commands.Count)
        {
            return string.Empty;
        }

        return commands[index] switch
        {
            TabMenuCommand.Detach => "Detach",
            TabMenuCommand.Close => "Close",
            TabMenuCommand.CloseOthers => "Close Others",
            TabMenuCommand.CloseTabsToRight => "Close Tabs To Right",
            TabMenuCommand.CloseAll => "Close All",
            _ => string.Empty
        };
    }

    private bool IsContextCommandEnabled(int index)
    {
        if (_contextMenuTabIndex < 0 || _contextMenuTabIndex >= _windows.Count)
        {
            return false;
        }

        IReadOnlyList<TabMenuCommand> commands = GetContextMenuCommands();
        if (index < 0 || index >= commands.Count)
        {
            return false;
        }

        return commands[index] switch
        {
            TabMenuCommand.Detach => CanDetachWindow(_contextMenuTabIndex),
            TabMenuCommand.Close => CanRemoveWindow(_contextMenuTabIndex),
            TabMenuCommand.CloseOthers => HasClosableWindowOtherThan(_contextMenuTabIndex),
            TabMenuCommand.CloseTabsToRight => HasClosableWindowToRight(_contextMenuTabIndex),
            TabMenuCommand.CloseAll => HasAnyClosableWindow(),
            _ => false
        };
    }

    private void ExecuteContextMenuCommand(int index, UiPoint screenMousePosition)
    {
        if (_contextMenuTabIndex < 0 || _contextMenuTabIndex >= _windows.Count)
        {
            return;
        }

        IReadOnlyList<TabMenuCommand> commands = GetContextMenuCommands();
        if (index < 0 || index >= commands.Count)
        {
            return;
        }

        switch (commands[index])
        {
            case TabMenuCommand.Detach:
                TryDetachWindow(_contextMenuTabIndex, screenMousePosition);
                break;
            case TabMenuCommand.Close:
                CloseWindowCore(_contextMenuTabIndex, allowLastWindow: true);
                break;
            case TabMenuCommand.CloseOthers:
                CloseOtherWindows(_contextMenuTabIndex);
                break;
            case TabMenuCommand.CloseTabsToRight:
                CloseWindowsToRight(_contextMenuTabIndex);
                break;
            case TabMenuCommand.CloseAll:
                CloseAllWindows();
                break;
        }
    }

    private bool CanDetachWindow(int index)
    {
        if (!AllowDetach || index < 0 || index >= _windows.Count)
        {
            return false;
        }

        UiWindow window = _windows[index];
        return CanDetachWindowPredicate?.Invoke(window) ?? true;
    }

    private void RenderOverflowMenu(UiRenderContext context)
    {
        if (_overflowMenuBounds.Width <= 0 || _overflowMenuBounds.Height <= 0)
        {
            return;
        }

        context.Renderer.FillRect(_overflowMenuBounds, MenuBackground);
        context.Renderer.DrawRect(_overflowMenuBounds, MenuBorderColor, 1);

        int itemHeight = GetMenuItemHeight();
        int textHeight = context.Renderer.MeasureTextHeight(TabTextScale, _layoutFont);
        for (int i = 0; i < _overflowWindowIndices.Count; i++)
        {
            UiRect itemBounds = new(_overflowMenuBounds.X, _overflowMenuBounds.Y + i * itemHeight, _overflowMenuBounds.Width, itemHeight);
            int windowIndex = _overflowWindowIndices[i];
            bool hovered = i == _overflowMenuHoverIndex;
            bool active = windowIndex == _activeIndex;
            if (hovered || active)
            {
                context.Renderer.FillRect(itemBounds, hovered ? MenuHoverColor : TabActiveColor);
            }

            string title = windowIndex >= 0 && windowIndex < _windows.Count ? _windows[windowIndex].Title : string.Empty;
            int textY = itemBounds.Y + (itemBounds.Height - textHeight) / 2;
            int availableWidth = Math.Max(0, itemBounds.Width - Math.Max(0, TabPadding) * 2);
            string renderText = UiTextHelpers.BuildElidedText(title, availableWidth, TabTextScale, _layoutFont);
            context.Renderer.DrawText(renderText, new UiPoint(itemBounds.X + Math.Max(0, TabPadding), textY), MenuTextColor, TabTextScale, _layoutFont);
        }
    }

    private void RenderContextMenu(UiRenderContext context)
    {
        if (_contextMenuBounds.Width <= 0 || _contextMenuBounds.Height <= 0)
        {
            return;
        }

        context.Renderer.FillRect(_contextMenuBounds, MenuBackground);
        context.Renderer.DrawRect(_contextMenuBounds, MenuBorderColor, 1);

        int itemHeight = GetMenuItemHeight();
        int textHeight = context.Renderer.MeasureTextHeight(TabTextScale, _layoutFont);
        IReadOnlyList<TabMenuCommand> commands = GetContextMenuCommands();
        for (int i = 0; i < commands.Count; i++)
        {
            UiRect itemBounds = new(_contextMenuBounds.X, _contextMenuBounds.Y + i * itemHeight, _contextMenuBounds.Width, itemHeight);
            bool enabled = IsContextCommandEnabled(i);
            if (i == _contextMenuHoverIndex)
            {
                context.Renderer.FillRect(itemBounds, MenuHoverColor);
            }

            UiColor textColor = enabled ? MenuTextColor : MenuDisabledTextColor;
            int textY = itemBounds.Y + (itemBounds.Height - textHeight) / 2;
            context.Renderer.DrawText(GetContextMenuLabel(i), new UiPoint(itemBounds.X + Math.Max(0, TabPadding), textY), textColor, TabTextScale, _layoutFont);
        }
    }
}
