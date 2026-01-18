using System.Text;

namespace OpenControls.Controls;

public sealed class UiDockHost : UiElement
{
    private static readonly Encoding Latin1Encoding = Encoding.Latin1;

    private readonly List<UiWindow> _windows = new();
    private int _activeIndex = -1;
    private UiWindow? _dragWindow;
    private int _dragIndex = -1;
    private UiPoint _dragStart;
    private bool _dragMoved;
    private int _tabScrollOffset;
    private int _tabMaxScroll;
    private UiRect _tabAreaBounds;
    private UiRect _scrollLeftBounds;
    private UiRect _scrollRightBounds;
    private bool _tabsOverflow;
    private int _closeHoverIndex = -1;
    private int _closePressedIndex = -1;
    private bool _scrollLeftHover;
    private bool _scrollRightHover;

    public UiColor Background { get; set; } = new(20, 24, 34);
    public UiColor Border { get; set; } = new(90, 100, 120);
    public UiColor TabBarColor { get; set; } = new(22, 26, 36);
    public UiColor TabActiveColor { get; set; } = new(45, 52, 70);
    public UiColor TabHoverColor { get; set; } = new(32, 36, 48);
    public UiColor TabTextColor { get; set; } = UiColor.White;
    public UiColor TabBorderColor { get; set; } = new(60, 70, 90);
    public int TabBarHeight { get; set; } = 24;
    public int TabWidth { get; set; } = 120;
    public int TabPadding { get; set; } = 6;
    public int TabTextScale { get; set; } = 1;
    public bool ShowCloseButtons { get; set; } = true;
    public int CloseButtonPadding { get; set; } = 4;
    public int ScrollButtonWidth { get; set; } = 18;
    public int ScrollStep { get; set; } = 80;
    public bool HideDockedTitleBars { get; set; } = true;
    public bool AllowReorder { get; set; } = true;
    public bool AllowDetach { get; set; } = true;
    public int DragThreshold { get; set; } = 6;
    public bool ExternalDragHandling { get; set; }

    public event Action<UiWindow, UiPoint>? TabDetached;
    public event Action<UiWindow>? TabClosed;

    public IReadOnlyList<UiWindow> Windows => _windows;
    public UiWindow? ActiveWindow => _activeIndex >= 0 && _activeIndex < _windows.Count ? _windows[_activeIndex] : null;
    public int ActiveIndex => _activeIndex;
    public bool IsEmpty => _windows.Count == 0;

    public void AddWindow(UiWindow window)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        if (_windows.Contains(window))
        {
            return;
        }

        _windows.Add(window);
        AddChild(window);
        if (_activeIndex == -1)
        {
            _activeIndex = 0;
        }
    }

    public void DockWindow(UiWindow window, int index)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        if (_windows.Contains(window))
        {
            MoveWindow(window, index);
            return;
        }

        index = Math.Clamp(index, 0, _windows.Count);
        _windows.Insert(index, window);
        AddChild(window);
        _activeIndex = index;
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
        }
        else if (_activeIndex >= _windows.Count)
        {
            _activeIndex = _windows.Count - 1;
        }

        return true;
    }

    public void DockWindow(UiWindow window)
    {
        AddWindow(window);
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
    }

    public int GetTabIndexAt(UiPoint point)
    {
        UpdateTabLayout();

        if (!IsPointInTabBar(point))
        {
            return -1;
        }

        if (_tabsOverflow && (_scrollLeftBounds.Contains(point) || _scrollRightBounds.Contains(point)))
        {
            return -1;
        }

        if (!_tabAreaBounds.Contains(point))
        {
            return -1;
        }

        int index = GetTabIndex(point.X);
        if (index < 0 || index >= _windows.Count)
        {
            return -1;
        }

        if (CanCloseWindow(index) && GetCloseBounds(GetTabRect(index)).Contains(point))
        {
            return -1;
        }

        return index;
    }

    public bool IsPointInTabBar(UiPoint point)
    {
        return point.X >= Bounds.X
            && point.X < Bounds.Right
            && point.Y >= Bounds.Y
            && point.Y < Bounds.Y + TabBarHeight;
    }

    public UiRect GetTabBounds(int index)
    {
        UpdateTabLayout();
        return GetTabRect(index);
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

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UpdateDockedLayout();
        UpdateTabLayout();

        UiInputState input = context.Input;
        _closeHoverIndex = GetCloseIndexAt(input.MousePosition);
        _scrollLeftHover = _tabsOverflow && _scrollLeftBounds.Contains(input.MousePosition);
        _scrollRightHover = _tabsOverflow && _scrollRightBounds.Contains(input.MousePosition);

        int startingActive = _activeIndex;

        if (input.LeftClicked)
        {
            if (_tabsOverflow && _scrollLeftHover && _tabScrollOffset > 0)
            {
                ScrollTabs(-1);
            }
            else if (_tabsOverflow && _scrollRightHover && _tabScrollOffset < _tabMaxScroll)
            {
                ScrollTabs(1);
            }
            else if (_closeHoverIndex >= 0 && CanCloseWindow(_closeHoverIndex))
            {
                _closePressedIndex = _closeHoverIndex;
                _dragWindow = null;
                _dragIndex = -1;
                _dragMoved = false;
            }
        }

        if (ExternalDragHandling)
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
            base.Update(context);
            return;
        }

        if (input.LeftClicked && _closePressedIndex < 0 && IsPointInTabBar(input.MousePosition))
        {
            int index = GetTabIndexAt(input.MousePosition);
            if (index >= 0 && index < _windows.Count)
            {
                _activeIndex = index;
                if (AllowReorder || AllowDetach)
                {
                    _dragWindow = _windows[index];
                    _dragIndex = index;
                    _dragStart = input.MousePosition;
                    _dragMoved = false;
                }
            }
        }

        if (_dragWindow != null && input.LeftDown)
        {
            int deltaX = Math.Abs(input.MousePosition.X - _dragStart.X);
            int deltaY = Math.Abs(input.MousePosition.Y - _dragStart.Y);
            if (!_dragMoved && (deltaX >= DragThreshold || deltaY >= DragThreshold))
            {
                _dragMoved = true;
            }

            if (_dragMoved && AllowReorder && IsPointInTabBar(input.MousePosition))
            {
                int targetIndex = GetTabIndex(input.MousePosition.X);
                if (targetIndex >= 0 && targetIndex < _windows.Count && targetIndex != _dragIndex)
                {
                    MoveWindow(_dragIndex, targetIndex);
                    _dragIndex = targetIndex;
                }
            }
        }

        if (input.LeftReleased)
        {
            if (_closePressedIndex >= 0)
            {
                if (_closePressedIndex == _closeHoverIndex && CanCloseWindow(_closePressedIndex))
                {
                    CloseWindow(_closePressedIndex);
                }

                _closePressedIndex = -1;
                _dragWindow = null;
                _dragIndex = -1;
                _dragMoved = false;
            }
            else if (_dragWindow != null && _dragMoved && AllowDetach && !Bounds.Contains(input.MousePosition))
            {
                DetachWindow(_dragWindow, input.MousePosition);
            }

            _dragWindow = null;
            _dragIndex = -1;
            _dragMoved = false;
        }

        if (startingActive != _activeIndex)
        {
            UpdateTabLayout();
        }

        SetWindowVisibility();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UpdateTabLayout();

        context.Renderer.FillRect(Bounds, Background);
        context.Renderer.DrawRect(Bounds, Border, 1);

        UiRect tabBar = new(Bounds.X, Bounds.Y, Bounds.Width, TabBarHeight);
        context.Renderer.FillRect(tabBar, TabBarColor);

        UiRect clipBounds = _tabsOverflow ? _tabAreaBounds : tabBar;
        context.Renderer.PushClip(clipBounds);
        int textHeight = context.Renderer.MeasureTextHeight(TabTextScale);
        int closeTextWidth = MeasureTextWidth("X", TabTextScale);
        for (int i = 0; i < _windows.Count; i++)
        {
            UiRect tabRect = GetTabRect(i);
            UiColor tabColor = i == _activeIndex ? TabActiveColor : TabBarColor;
            context.Renderer.FillRect(tabRect, tabColor);
            context.Renderer.DrawRect(tabRect, TabBorderColor, 1);

            UiWindow window = _windows[i];
            int textY = tabRect.Y + (tabRect.Height - textHeight) / 2;
            context.Renderer.DrawText(window.Title, new UiPoint(tabRect.X + TabPadding, textY), TabTextColor, TabTextScale);

            if (ShowCloseButtons && CanCloseWindow(i))
            {
                UiRect closeBounds = GetCloseBounds(tabRect);
                int closeTextX = closeBounds.X + (closeBounds.Width - closeTextWidth) / 2;
                int closeTextY = closeBounds.Y + (closeBounds.Height - textHeight) / 2;
                UiColor closeColor = _closeHoverIndex == i ? TabTextColor : TabTextColor;
                context.Renderer.DrawText("X", new UiPoint(closeTextX, closeTextY), closeColor, TabTextScale);
            }
        }
        context.Renderer.PopClip();

        if (_tabsOverflow)
        {
            bool canScrollLeft = _tabScrollOffset > 0;
            bool canScrollRight = _tabScrollOffset < _tabMaxScroll;
            RenderScrollButton(context, _scrollLeftBounds, UiArrowDirection.Left, _scrollLeftHover, canScrollLeft);
            RenderScrollButton(context, _scrollRightBounds, UiArrowDirection.Right, _scrollRightHover, canScrollRight);
        }

        base.Render(context);
    }

    private void UpdateTabLayout()
    {
        int tabHeight = Math.Max(0, TabBarHeight);
        int tabWidth = Math.Max(0, TabWidth);
        int totalWidth = tabWidth * _windows.Count;

        _tabsOverflow = tabWidth > 0 && totalWidth > Bounds.Width;
        int scrollButtonWidth = _tabsOverflow ? Math.Max(0, ScrollButtonWidth) : 0;
        if (_tabsOverflow)
        {
            scrollButtonWidth = Math.Min(scrollButtonWidth, Math.Max(0, Bounds.Width / 2));
        }

        int tabAreaWidth = Math.Max(0, Bounds.Width - scrollButtonWidth * 2);
        _tabAreaBounds = new UiRect(Bounds.X + scrollButtonWidth, Bounds.Y, tabAreaWidth, tabHeight);
        _scrollLeftBounds = new UiRect(Bounds.X, Bounds.Y, scrollButtonWidth, tabHeight);
        _scrollRightBounds = new UiRect(Bounds.Right - scrollButtonWidth, Bounds.Y, scrollButtonWidth, tabHeight);

        _tabMaxScroll = Math.Max(0, totalWidth - tabAreaWidth);
        _tabScrollOffset = Math.Clamp(_tabScrollOffset, 0, _tabMaxScroll);

        if (_tabsOverflow && _activeIndex >= 0 && _activeIndex < _windows.Count)
        {
            int previousScroll = _tabScrollOffset;
            EnsureActiveVisible(GetTabRect(_activeIndex));
            _tabScrollOffset = Math.Clamp(_tabScrollOffset, 0, _tabMaxScroll);
            if (_tabScrollOffset != previousScroll)
            {
                _tabScrollOffset = Math.Clamp(_tabScrollOffset, 0, _tabMaxScroll);
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

    private UiRect GetCloseBounds(UiRect tabRect)
    {
        int closeWidth = GetCloseAreaWidth();
        if (closeWidth <= 0)
        {
            return default;
        }

        return new UiRect(tabRect.Right - closeWidth, tabRect.Y, closeWidth, tabRect.Height);
    }

    private int GetCloseIndexAt(UiPoint point)
    {
        if (!ShowCloseButtons)
        {
            return -1;
        }

        for (int i = 0; i < _windows.Count; i++)
        {
            if (!CanCloseWindow(i))
            {
                continue;
            }

            UiRect tabRect = GetTabRect(i);
            UiRect closeRect = GetCloseBounds(tabRect);
            if (closeRect.Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    private bool CanCloseWindow(int index)
    {
        return ShowCloseButtons
            && _windows.Count > 1
            && index >= 0
            && index < _windows.Count
            && _windows[index].AllowClose
            && _windows[index].Enabled;
    }

    private bool CloseWindow(int index)
    {
        if (!CanCloseWindow(index))
        {
            return false;
        }

        UiWindow window = _windows[index];
        bool removed = RemoveWindow(window);
        if (removed)
        {
            TabClosed?.Invoke(window);
            if (_dragWindow == window)
            {
                _dragWindow = null;
                _dragIndex = -1;
                _dragMoved = false;
            }
            UpdateTabLayout();
        }

        return removed;
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

    private void UpdateDockedLayout()
    {
        UiRect content = new(Bounds.X, Bounds.Y + TabBarHeight, Bounds.Width, Math.Max(0, Bounds.Height - TabBarHeight));
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

    private bool IsInTabBar(UiPoint point)
    {
        return IsPointInTabBar(point);
    }

    private int GetTabIndex(int mouseX)
    {
        if (TabWidth <= 0)
        {
            return -1;
        }

        int relativeX = mouseX - _tabAreaBounds.X + _tabScrollOffset;
        if (relativeX < 0)
        {
            return -1;
        }

        int index = relativeX / TabWidth;
        return index >= 0 && index < _windows.Count ? index : -1;
    }

    private UiRect GetTabRect(int index)
    {
        int x = _tabAreaBounds.X + index * TabWidth - _tabScrollOffset;
        return new UiRect(x, Bounds.Y, TabWidth, TabBarHeight);
    }
}
