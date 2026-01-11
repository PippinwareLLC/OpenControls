namespace OpenControls.Controls;

public sealed class UiDockHost : UiElement
{
    private readonly List<UiWindow> _windows = new();
    private int _activeIndex = -1;
    private UiWindow? _dragWindow;
    private int _dragIndex = -1;
    private UiPoint _dragStart;
    private bool _dragMoved;

    public UiColor Background { get; set; } = new(20, 24, 34);
    public UiColor Border { get; set; } = new(90, 100, 120);
    public UiColor TabBarColor { get; set; } = new(22, 26, 36);
    public UiColor TabActiveColor { get; set; } = new(45, 52, 70);
    public UiColor TabTextColor { get; set; } = UiColor.White;
    public UiColor TabBorderColor { get; set; } = new(60, 70, 90);
    public int TabBarHeight { get; set; } = 24;
    public int TabWidth { get; set; } = 120;
    public int TabPadding { get; set; } = 6;
    public int TabTextScale { get; set; } = 1;
    public bool HideDockedTitleBars { get; set; } = true;
    public bool AllowReorder { get; set; } = true;
    public bool AllowDetach { get; set; } = true;
    public int DragThreshold { get; set; } = 6;
    public bool ExternalDragHandling { get; set; }

    public event Action<UiWindow, UiPoint>? TabDetached;

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
        if (!IsPointInTabBar(point))
        {
            return -1;
        }

        return GetTabIndex(point.X);
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

        if (ExternalDragHandling)
        {
            SetWindowVisibility();
            base.Update(context);
            return;
        }

        UiInputState input = context.Input;
        if (input.LeftClicked && IsPointInTabBar(input.MousePosition))
        {
            int index = GetTabIndex(input.MousePosition.X);
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

        if (_dragWindow != null && input.LeftReleased)
        {
            if (_dragMoved && AllowDetach && !Bounds.Contains(input.MousePosition))
            {
                DetachWindow(_dragWindow, input.MousePosition);
            }

            _dragWindow = null;
            _dragIndex = -1;
            _dragMoved = false;
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

        context.Renderer.FillRect(Bounds, Background);
        context.Renderer.DrawRect(Bounds, Border, 1);

        UiRect tabBar = new(Bounds.X, Bounds.Y, Bounds.Width, TabBarHeight);
        context.Renderer.FillRect(tabBar, TabBarColor);

        for (int i = 0; i < _windows.Count; i++)
        {
            UiRect tabRect = GetTabRect(i);
            UiColor tabColor = i == _activeIndex ? TabActiveColor : TabBarColor;
            context.Renderer.FillRect(tabRect, tabColor);
            context.Renderer.DrawRect(tabRect, TabBorderColor, 1);

            UiWindow window = _windows[i];
            int textHeight = context.Renderer.MeasureTextHeight(TabTextScale);
            int textY = tabRect.Y + (tabRect.Height - textHeight) / 2;
            context.Renderer.DrawText(window.Title, new UiPoint(tabRect.X + TabPadding, textY), TabTextColor, TabTextScale);
        }

        base.Render(context);
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

        int relativeX = mouseX - Bounds.X;
        if (relativeX < 0)
        {
            return -1;
        }

        return relativeX / TabWidth;
    }

    private UiRect GetTabRect(int index)
    {
        int x = Bounds.X + index * TabWidth;
        return new UiRect(x, Bounds.Y, TabWidth, TabBarHeight);
    }
}
