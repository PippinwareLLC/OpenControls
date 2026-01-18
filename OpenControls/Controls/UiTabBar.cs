using System.Text;

namespace OpenControls.Controls;

public sealed class UiTabBar : UiElement
{
    private static readonly Encoding Latin1Encoding = Encoding.Latin1;

    private int _activeIndex = -1;
    private int _hoverIndex = -1;
    private int _pressedIndex = -1;
    private bool _focused;
    private int _scrollOffset;
    private int _maxScroll;
    private UiRect _tabAreaBounds;
    private UiRect _scrollLeftBounds;
    private UiRect _scrollRightBounds;
    private bool _tabsOverflow;
    private int _closeHoverIndex = -1;
    private int _closePressedIndex = -1;
    private bool _scrollLeftHover;
    private bool _scrollRightHover;

    public UiColor TabBarColor { get; set; } = new(22, 26, 36);
    public UiColor TabActiveColor { get; set; } = new(45, 52, 70);
    public UiColor TabHoverColor { get; set; } = new(32, 36, 48);
    public UiColor TabBorderColor { get; set; } = new(60, 70, 90);
    public UiColor TabTextColor { get; set; } = UiColor.White;
    public UiColor TabActiveTextColor { get; set; } = UiColor.White;
    public int TabBarHeight { get; set; } = 24;
    public int TabPadding { get; set; } = 6;
    public int TabSpacing { get; set; } = 2;
    public int TabTextScale { get; set; } = 1;
    public bool AutoSizeTabs { get; set; } = true;
    public int TabWidth { get; set; }
    public int TabMaxWidth { get; set; }
    public bool ShowCloseButtons { get; set; } = true;
    public int CloseButtonPadding { get; set; } = 4;
    public int ScrollButtonWidth { get; set; } = 18;
    public int ScrollStep { get; set; } = 80;

    public int ActiveIndex
    {
        get => _activeIndex;
        set => SetActiveIndex(value);
    }

    public UiTabItem? ActiveTab
    {
        get
        {
            List<UiTabItem> tabs = CollectTabs();
            return _activeIndex >= 0 && _activeIndex < tabs.Count ? tabs[_activeIndex] : null;
        }
    }

    public UiRect ContentBounds
    {
        get
        {
            int height = Math.Max(0, Bounds.Height - TabBarHeight);
            return new UiRect(Bounds.X, Bounds.Y + TabBarHeight, Bounds.Width, height);
        }
    }

    public UiRect GetTabBounds(int index)
    {
        List<UiTabItem> tabs = CollectTabs();
        if (index < 0 || index >= tabs.Count)
        {
            return default;
        }

        return tabs[index].TabBounds;
    }

    public UiRect GetTabCloseBounds(int index)
    {
        List<UiTabItem> tabs = CollectTabs();
        if (index < 0 || index >= tabs.Count)
        {
            return default;
        }

        return tabs[index].CloseBounds;
    }

    public event Action<UiTabItem>? ActiveTabChanged;
    public event Action<UiTabItem>? TabClosed;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        List<UiTabItem> tabs = CollectTabs();
        UpdateTabLayout(tabs);

        UiInputState input = context.Input;
        _hoverIndex = GetTabIndexAt(input.MousePosition, tabs);
        _closeHoverIndex = GetCloseIndexAt(input.MousePosition, tabs);
        _scrollLeftHover = _tabsOverflow && _scrollLeftBounds.Contains(input.MousePosition);
        _scrollRightHover = _tabsOverflow && _scrollRightBounds.Contains(input.MousePosition);

        int startingActive = _activeIndex;
        bool layoutDirty = false;

        if (input.LeftClicked)
        {
            if (_tabsOverflow && _scrollLeftHover && _scrollOffset > 0)
            {
                ScrollTabs(-1);
                layoutDirty = true;
            }
            else if (_tabsOverflow && _scrollRightHover && _scrollOffset < _maxScroll)
            {
                ScrollTabs(1);
                layoutDirty = true;
            }
            else if (_closeHoverIndex >= 0 && IsTabClosable(tabs, _closeHoverIndex))
            {
                _closePressedIndex = _closeHoverIndex;
                _pressedIndex = -1;
                context.Focus.RequestFocus(this);
            }
            else if (_hoverIndex >= 0 && IsTabEnabled(tabs, _hoverIndex))
            {
                _pressedIndex = _hoverIndex;
                context.Focus.RequestFocus(this);
            }
        }

        if (_focused)
        {
            if (input.Navigation.MoveLeft)
            {
                MoveActive(tabs, -1);
            }

            if (input.Navigation.MoveRight)
            {
                MoveActive(tabs, 1);
            }
        }

        if (input.LeftReleased)
        {
            if (_closePressedIndex >= 0)
            {
                if (_closePressedIndex == _closeHoverIndex && IsTabClosable(tabs, _closePressedIndex))
                {
                    if (CloseTab(tabs, _closePressedIndex))
                    {
                        layoutDirty = true;
                        tabs = CollectTabs();
                    }
                }

                _closePressedIndex = -1;
                _pressedIndex = -1;
            }
            else if (_pressedIndex >= 0 && _pressedIndex == _hoverIndex && IsTabEnabled(tabs, _pressedIndex))
            {
                SetActiveIndex(_pressedIndex, tabs);
            }

            _pressedIndex = -1;
        }

        if (layoutDirty || startingActive != _activeIndex)
        {
            tabs = CollectTabs();
            UpdateTabLayout(tabs);
        }

        UiRect content = ContentBounds;
        for (int i = 0; i < tabs.Count; i++)
        {
            UiTabItem tab = tabs[i];
            tab.Bounds = content;
            tab.SetActive(i == _activeIndex);
            if (tab.IsActive)
            {
                tab.Update(context);
            }
        }
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        List<UiTabItem> tabs = CollectTabs();
        UiRect tabBar = new(Bounds.X, Bounds.Y, Bounds.Width, TabBarHeight);
        context.Renderer.FillRect(tabBar, TabBarColor);

        if (tabs.Count > 0)
        {
            UiRect clipBounds = _tabsOverflow ? _tabAreaBounds : tabBar;
            context.Renderer.PushClip(clipBounds);
            int textHeight = context.Renderer.MeasureTextHeight(TabTextScale);
            int closeTextWidth = MeasureTextWidth("X", TabTextScale);
            for (int i = 0; i < tabs.Count; i++)
            {
                UiTabItem tab = tabs[i];
                UiRect tabRect = tab.TabBounds;
                UiColor tabColor = i == _activeIndex ? TabActiveColor : (_hoverIndex == i ? TabHoverColor : TabBarColor);
                context.Renderer.FillRect(tabRect, tabColor);
                context.Renderer.DrawRect(tabRect, TabBorderColor, 1);

                UiColor textColor = i == _activeIndex ? TabActiveTextColor : TabTextColor;
                int textY = tabRect.Y + (tabRect.Height - textHeight) / 2;
                int textX = tabRect.X + Math.Max(0, TabPadding);
                context.Renderer.DrawText(tab.Text, new UiPoint(textX, textY), textColor, TabTextScale);

                if (ShowCloseButtons && tab.AllowClose)
                {
                    UiRect closeBounds = tab.CloseBounds;
                    UiColor closeColor = _closeHoverIndex == i ? TabActiveTextColor : TabTextColor;
                    int closeTextX = closeBounds.X + (closeBounds.Width - closeTextWidth) / 2;
                    int closeTextY = closeBounds.Y + (closeBounds.Height - textHeight) / 2;
                    context.Renderer.DrawText("X", new UiPoint(closeTextX, closeTextY), closeColor, TabTextScale);
                }
            }
            context.Renderer.PopClip();
        }

        if (_tabsOverflow)
        {
            bool canScrollLeft = _scrollOffset > 0;
            bool canScrollRight = _scrollOffset < _maxScroll;
            RenderScrollButton(context, _scrollLeftBounds, UiArrowDirection.Left, _scrollLeftHover, canScrollLeft);
            RenderScrollButton(context, _scrollRightBounds, UiArrowDirection.Right, _scrollRightHover, canScrollRight);
        }

        UiTabItem? active = _activeIndex >= 0 && _activeIndex < tabs.Count ? tabs[_activeIndex] : null;
        active?.Render(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        List<UiTabItem> tabs = CollectTabs();
        UiTabItem? active = _activeIndex >= 0 && _activeIndex < tabs.Count ? tabs[_activeIndex] : null;
        active?.RenderOverlay(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _pressedIndex = -1;
        _closePressedIndex = -1;
    }

    private List<UiTabItem> CollectTabs()
    {
        List<UiTabItem> tabs = new();
        foreach (UiElement child in Children)
        {
            if (child is UiTabItem tab && tab.Visible)
            {
                tabs.Add(tab);
            }
        }

        return tabs;
    }

    private int GetTotalTabWidth(List<UiTabItem> tabs, int spacing)
    {
        int total = 0;
        for (int i = 0; i < tabs.Count; i++)
        {
            if (i > 0)
            {
                total += spacing;
            }

            total += GetTabWidth(tabs[i]);
        }

        return total;
    }

    private void LayoutTabs(List<UiTabItem> tabs, int startX, int tabHeight, int spacing, int closeAreaWidth)
    {
        int x = startX;
        for (int i = 0; i < tabs.Count; i++)
        {
            UiTabItem tab = tabs[i];
            int width = GetTabWidth(tab);
            tab.TabBounds = new UiRect(x, Bounds.Y, width, tabHeight);
            if (ShowCloseButtons && tab.AllowClose && closeAreaWidth > 0)
            {
                tab.CloseBounds = new UiRect(tab.TabBounds.Right - closeAreaWidth, Bounds.Y, closeAreaWidth, tabHeight);
            }
            else
            {
                tab.CloseBounds = default;
            }

            x += width + spacing;
        }
    }

    private int GetCloseAreaWidth()
    {
        if (!ShowCloseButtons)
        {
            return 0;
        }

        int padding = Math.Max(0, CloseButtonPadding);
        int glyphWidth = MeasureTextWidth("X", TabTextScale);
        return glyphWidth + padding * 2;
    }

    private void EnsureActiveVisible(UiRect tabBounds)
    {
        if (!_tabsOverflow || _tabAreaBounds.Width <= 0)
        {
            return;
        }

        if (tabBounds.X < _tabAreaBounds.X)
        {
            _scrollOffset = Math.Max(0, _scrollOffset - (_tabAreaBounds.X - tabBounds.X));
        }
        else if (tabBounds.Right > _tabAreaBounds.Right)
        {
            _scrollOffset = Math.Min(_maxScroll, _scrollOffset + (tabBounds.Right - _tabAreaBounds.Right));
        }
    }

    private int GetCloseIndexAt(UiPoint point, List<UiTabItem> tabs)
    {
        if (!ShowCloseButtons)
        {
            return -1;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            if (!IsTabClosable(tabs, i))
            {
                continue;
            }

            UiRect rect = tabs[i].CloseBounds;
            if (rect.Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsTabClosable(List<UiTabItem> tabs, int index)
    {
        return index >= 0
            && index < tabs.Count
            && tabs[index].Enabled
            && tabs[index].AllowClose
            && ShowCloseButtons;
    }

    private bool CloseTab(List<UiTabItem> tabs, int index)
    {
        if (index < 0 || index >= tabs.Count)
        {
            return false;
        }

        UiTabItem tab = tabs[index];
        tab.Visible = false;
        tab.SetActive(false);
        TabClosed?.Invoke(tab);

        if (_activeIndex == index)
        {
            List<UiTabItem> updatedTabs = CollectTabs();
            if (updatedTabs.Count == 0)
            {
                _activeIndex = -1;
            }
            else
            {
                int nextIndex = Math.Min(index, updatedTabs.Count - 1);
                SetActiveIndex(nextIndex, updatedTabs);
            }
        }
        else if (_activeIndex > index)
        {
            _activeIndex -= 1;
        }

        return true;
    }

    private void ScrollTabs(int direction)
    {
        int step = Math.Max(1, ScrollStep);
        if (direction < 0)
        {
            _scrollOffset = Math.Max(0, _scrollOffset - step);
        }
        else if (direction > 0)
        {
            _scrollOffset = Math.Min(_maxScroll, _scrollOffset + step);
        }
    }

    private void RenderScrollButton(
        UiRenderContext context,
        UiRect bounds,
        UiArrowDirection direction,
        bool hover,
        bool enabled)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        UiColor background = hover && enabled ? TabHoverColor : TabBarColor;
        context.Renderer.FillRect(bounds, background);
        context.Renderer.DrawRect(bounds, TabBorderColor, 1);

        UiColor arrowColor = enabled ? TabTextColor : TabBorderColor;
        int inset = Math.Max(2, bounds.Height / 4);
        UiRect arrowBounds = new UiRect(
            bounds.X + inset,
            bounds.Y + inset,
            Math.Max(0, bounds.Width - inset * 2),
            Math.Max(0, bounds.Height - inset * 2));
        UiArrow.DrawTriangle(context.Renderer, arrowBounds, direction, arrowColor);
    }

    private void UpdateTabLayout(List<UiTabItem> tabs)
    {
        if (tabs.Count == 0)
        {
            _activeIndex = -1;
            _scrollOffset = 0;
            _maxScroll = 0;
            _tabsOverflow = false;
            _tabAreaBounds = new UiRect(Bounds.X, Bounds.Y, Bounds.Width, Math.Max(0, TabBarHeight));
            return;
        }

        if (_activeIndex < 0 || _activeIndex >= tabs.Count)
        {
            SetActiveIndex(0, tabs);
        }

        int tabHeight = Math.Max(0, TabBarHeight);
        int spacing = Math.Max(0, TabSpacing);
        int totalWidth = GetTotalTabWidth(tabs, spacing);
        _tabsOverflow = totalWidth > Bounds.Width;

        int scrollButtonWidth = _tabsOverflow ? Math.Max(0, ScrollButtonWidth) : 0;
        if (_tabsOverflow)
        {
            scrollButtonWidth = Math.Min(scrollButtonWidth, Math.Max(0, Bounds.Width / 2));
        }

        int tabAreaWidth = Math.Max(0, Bounds.Width - scrollButtonWidth * 2);
        _tabAreaBounds = new UiRect(Bounds.X + scrollButtonWidth, Bounds.Y, tabAreaWidth, tabHeight);
        _scrollLeftBounds = new UiRect(Bounds.X, Bounds.Y, scrollButtonWidth, tabHeight);
        _scrollRightBounds = new UiRect(Bounds.Right - scrollButtonWidth, Bounds.Y, scrollButtonWidth, tabHeight);

        _maxScroll = Math.Max(0, totalWidth - tabAreaWidth);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);

        int closeAreaWidth = GetCloseAreaWidth();
        LayoutTabs(tabs, _tabAreaBounds.X - _scrollOffset, tabHeight, spacing, closeAreaWidth);

        if (_tabsOverflow && _activeIndex >= 0 && _activeIndex < tabs.Count)
        {
            int previousScroll = _scrollOffset;
            EnsureActiveVisible(tabs[_activeIndex].TabBounds);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
            if (_scrollOffset != previousScroll)
            {
                LayoutTabs(tabs, _tabAreaBounds.X - _scrollOffset, tabHeight, spacing, closeAreaWidth);
            }
        }
    }

    private int GetTabIndexAt(UiPoint point, List<UiTabItem> tabs)
    {
        UiRect tabArea = _tabsOverflow ? _tabAreaBounds : new UiRect(Bounds.X, Bounds.Y, Bounds.Width, TabBarHeight);
        if (!tabArea.Contains(point))
        {
            return -1;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            UiRect rect = tabs[i].TabBounds;
            if (rect.Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    private void SetActiveIndex(int index, List<UiTabItem>? tabs = null)
    {
        tabs ??= CollectTabs();
        if (index < 0 || index >= tabs.Count)
        {
            return;
        }

        if (_activeIndex == index)
        {
            return;
        }

        _activeIndex = index;
        ActiveTabChanged?.Invoke(tabs[index]);
    }

    private void MoveActive(List<UiTabItem> tabs, int delta)
    {
        if (tabs.Count == 0)
        {
            return;
        }

        int start = _activeIndex < 0 ? 0 : _activeIndex;
        int index = start;
        for (int i = 0; i < tabs.Count; i++)
        {
            index = (index + delta + tabs.Count) % tabs.Count;
            if (IsTabEnabled(tabs, index))
            {
                SetActiveIndex(index, tabs);
                break;
            }
        }
    }

    private bool IsTabEnabled(List<UiTabItem> tabs, int index)
    {
        return index >= 0 && index < tabs.Count && tabs[index].Enabled;
    }

    private int GetTabWidth(UiTabItem tab)
    {
        int width = Math.Max(0, TabWidth);
        if (AutoSizeTabs)
        {
            int textWidth = MeasureTextWidth(tab.Text, TabTextScale);
            int padding = Math.Max(0, TabPadding);
            width = textWidth + padding * 2;
            if (ShowCloseButtons && tab.AllowClose)
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

    private static int MeasureTextWidth(string text, int scale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int safeScale = Math.Max(1, scale);
        int glyphWidth = (TinyBitmapFont.GlyphWidth + TinyBitmapFont.GlyphSpacing) * safeScale;
        int count = Latin1Encoding.GetByteCount(text);
        return count * glyphWidth;
    }
}
