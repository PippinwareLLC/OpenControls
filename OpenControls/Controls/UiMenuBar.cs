using System;
using System.Collections.Generic;

namespace OpenControls.Controls;

public enum UiMenuDisplayMode
{
    Bar,
    Popup
}

public sealed class UiMenuBar : UiElement
{
    public sealed class MenuItem
    {
        public string Text { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool IsCheckable { get; set; }
        public bool Checked { get; set; }
        public bool IsSeparator { get; set; }
        public UiElement? Content { get; set; }
        public int ContentWidth { get; set; }
        public int ContentHeight { get; set; }
        public int ContentPadding { get; set; } = -1;
        public bool ContentClip { get; set; } = true;
        public bool ContentCapturesInput { get; set; } = true;
        public List<MenuItem> Items { get; } = new();
        public Action<MenuItem>? Clicked { get; set; }

        public bool HasChildren => Items.Count > 0;
        public bool HasContent => Content != null;

        public static MenuItem Separator()
        {
            return new MenuItem { IsSeparator = true, Enabled = false };
        }
    }

    private sealed class MenuLayout
    {
        public MenuLayout(IReadOnlyList<MenuItem> items, UiRect bounds, List<UiRect> itemRects)
        {
            Items = items;
            Bounds = bounds;
            ItemRects = itemRects;
        }

        public IReadOnlyList<MenuItem> Items { get; }
        public UiRect Bounds { get; }
        public List<UiRect> ItemRects { get; }
    }

    private sealed class OffsetRenderer : IUiRenderer
    {
        private readonly IUiRenderer _inner;
        private readonly UiPoint _offset;

        public OffsetRenderer(IUiRenderer inner, UiPoint offset)
        {
            _inner = inner;
            _offset = offset;
        }

        public void FillRect(UiRect rect, UiColor color)
        {
            _inner.FillRect(Offset(rect), color);
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
            _inner.DrawRect(Offset(rect), color, thickness);
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
            _inner.FillRectGradient(Offset(rect), topLeft, topRight, bottomLeft, bottomRight);
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
            _inner.FillRectCheckerboard(Offset(rect), cellSize, colorA, colorB);
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
            _inner.DrawText(text, Offset(position), color, scale);
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return _inner.MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return _inner.MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
            _inner.PushClip(Offset(rect));
        }

        public void PopClip()
        {
            _inner.PopClip();
        }

        private UiRect Offset(UiRect rect)
        {
            return new UiRect(rect.X + _offset.X, rect.Y + _offset.Y, rect.Width, rect.Height);
        }

        private UiPoint Offset(UiPoint point)
        {
            return new UiPoint(point.X + _offset.X, point.Y + _offset.Y);
        }
    }

    private readonly List<UiRect> _topItemRects = new();
    private readonly List<MenuLayout> _openLayouts = new();
    private readonly List<int> _openPath = new();
    private int _hoveredTopIndex = -1;
    private int _hoveredMenuLevel = -1;
    private int _hoveredMenuIndex = -1;
    private bool _popupOpen;

    public List<MenuItem> Items { get; } = new();

    public UiMenuDisplayMode DisplayMode { get; set; } = UiMenuDisplayMode.Bar;
    public bool IsPopupOpen => _popupOpen;
    public bool ClosePopupOnOutsideClick { get; set; } = true;
    public bool ClosePopupOnEscape { get; set; } = true;
    public bool ClosePopupOnItemClick { get; set; } = true;

    public UiColor BarBackground { get; set; } = new(22, 26, 36);
    public UiColor BarBorder { get; set; } = new(60, 70, 90);
    public UiColor DropdownBackground { get; set; } = new(18, 22, 32);
    public UiColor DropdownBorder { get; set; } = new(70, 80, 100);
    public UiColor ItemHover { get; set; } = new(45, 52, 70);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor DisabledTextColor { get; set; } = new(110, 120, 140);
    public UiColor SeparatorColor { get; set; } = new(60, 70, 90);
    public UiColor CheckmarkColor { get; set; } = UiColor.White;

    public int BarHeight { get; set; } = 24;
    public int BarItemPadding { get; set; } = 8;
    public int BarItemSpacing { get; set; } = 4;
    public int TextScale { get; set; } = 1;
    public int DropdownItemHeight { get; set; } = 22;
    public int DropdownMinWidth { get; set; } = 160;
    public int ItemPadding { get; set; } = 8;
    public int ItemVerticalPadding { get; set; } = 3;
    public int SeparatorHeight { get; set; } = 6;
    public int ShortcutPadding { get; set; } = 12;
    public int CheckmarkSize { get; set; } = 6;
    public int CheckmarkAreaWidth { get; set; } = 12;
    public int SubmenuIndicatorWidth { get; set; } = 10;
    public int SubmenuOffset { get; set; } = 2;
    public bool ClampToParent { get; set; } = true;

    public int FallbackCharWidth { get; set; } = 6;
    public int FallbackCharHeight { get; set; } = 7;
    public Func<string, int, int>? MeasureTextWidth { get; set; }
    public Func<int, int>? MeasureTextHeight { get; set; }

    public void OpenPopup()
    {
        if (_popupOpen)
        {
            return;
        }

        _popupOpen = true;
        _openPath.Clear();
    }

    public void ClosePopup()
    {
        if (!_popupOpen)
        {
            return;
        }

        _popupOpen = false;
        _openPath.Clear();
        _openLayouts.Clear();
    }

    public void TogglePopup()
    {
        if (_popupOpen)
        {
            ClosePopup();
        }
        else
        {
            OpenPopup();
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            UpdatePopup(context);
            return;
        }

        BuildLayout();

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;
        _hoveredTopIndex = GetTopItemIndex(mouse);

        if (_openPath.Count > 0 && input.Navigation.Escape)
        {
            CloseMenu();
            BuildLayout();
        }

        bool menuOpen = _openPath.Count > 0;

        if (input.LeftClicked)
        {
            if (_hoveredTopIndex >= 0)
            {
                HandleTopClick(_hoveredTopIndex);
                BuildLayout();
            }
            else if (menuOpen)
            {
                HandleMenuClick(mouse);
                BuildLayout();
            }
        }

        menuOpen = _openPath.Count > 0;

        if (menuOpen)
        {
            if (_hoveredTopIndex >= 0 && _hoveredTopIndex != _openPath[0])
            {
                OpenTopOnHover(_hoveredTopIndex);
                BuildLayout();
            }
            else
            {
                UpdateHoverSubmenu(mouse);
                BuildLayout();
            }
        }

        UpdateHoveredMenuItem(mouse);

        UpdateOpenContent(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            return;
        }

        UiRect barBounds = GetBarBounds();
        context.Renderer.FillRect(barBounds, BarBackground);
        context.Renderer.DrawRect(barBounds, BarBorder, 1);

        int textHeight = GetTextHeight(TextScale);
        for (int i = 0; i < _topItemRects.Count; i++)
        {
            UiRect rect = _topItemRects[i];
            bool isOpen = _openPath.Count > 0 && _openPath[0] == i;
            bool isHovered = _hoveredTopIndex == i;
            if (isOpen || isHovered)
            {
                context.Renderer.FillRect(rect, ItemHover);
            }

            MenuItem item = Items[i];
            UiColor color = item.Enabled ? TextColor : DisabledTextColor;
            int textY = rect.Y + (rect.Height - textHeight) / 2;
            context.Renderer.DrawText(item.Text, new UiPoint(rect.X + BarItemPadding, textY), color, TextScale);
        }

        base.Render(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        if (DisplayMode == UiMenuDisplayMode.Popup && !_popupOpen)
        {
            return;
        }

        if (_openLayouts.Count == 0)
        {
            base.RenderOverlay(context);
            return;
        }

        int textHeight = GetTextHeight(TextScale);
        for (int level = 0; level < _openLayouts.Count; level++)
        {
            MenuLayout layout = _openLayouts[level];
            context.Renderer.FillRect(layout.Bounds, DropdownBackground);
            context.Renderer.DrawRect(layout.Bounds, DropdownBorder, 1);

            for (int i = 0; i < layout.ItemRects.Count; i++)
            {
                UiRect itemRect = layout.ItemRects[i];
                MenuItem item = layout.Items[i];
                if (item.IsSeparator)
                {
                    int y = itemRect.Y + itemRect.Height / 2;
                    context.Renderer.FillRect(new UiRect(itemRect.X + ItemPadding, y, itemRect.Width - ItemPadding * 2, 1), SeparatorColor);
                    continue;
                }

                bool isHovered = _hoveredMenuLevel == level && _hoveredMenuIndex == i;
                bool isOpen = IsMenuItemOpen(level, i);
                bool isInteractive = IsMenuItemInteractive(item);
                if (isInteractive && (isHovered || isOpen))
                {
                    context.Renderer.FillRect(itemRect, ItemHover);
                }

                if (item.HasContent)
                {
                    RenderMenuItemContent(context, item, itemRect);
                }
                else
                {
                    UiColor color = item.Enabled ? TextColor : DisabledTextColor;
                    int itemTextY = itemRect.Y + (itemRect.Height - textHeight) / 2;
                    int textX = itemRect.X + ItemPadding + GetCheckmarkAreaWidth();
                    context.Renderer.DrawText(item.Text, new UiPoint(textX, itemTextY), color, TextScale);

                    if (!string.IsNullOrEmpty(item.Shortcut))
                    {
                        int shortcutWidth = GetTextWidth(item.Shortcut);
                        int shortcutX = itemRect.Right - ItemPadding - shortcutWidth - GetSubmenuIndicatorWidth(item);
                        context.Renderer.DrawText(item.Shortcut, new UiPoint(shortcutX, itemTextY), color, TextScale);
                    }

                    if (item.IsCheckable && item.Checked)
                    {
                        DrawCheckmark(context, itemRect);
                    }

                    if (item.HasChildren)
                    {
                        DrawSubmenuArrow(context, itemRect, color);
                    }
                }

                if (item.HasContent && item.HasChildren)
                {
                    UiColor arrowColor = item.Enabled ? TextColor : DisabledTextColor;
                    DrawSubmenuArrow(context, itemRect, arrowColor);
                }
            }
        }

        base.RenderOverlay(context);
    }

    private void UpdatePopup(UiUpdateContext context)
    {
        if (!_popupOpen)
        {
            _openLayouts.Clear();
            _openPath.Clear();
            return;
        }

        BuildPopupLayouts();

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;

        if (ClosePopupOnEscape && input.Navigation.Escape)
        {
            ClosePopup();
            return;
        }

        if (ClosePopupOnOutsideClick && input.LeftClicked && !IsPointInOpenLayouts(mouse))
        {
            ClosePopup();
            return;
        }

        if (input.LeftClicked)
        {
            HandlePopupClick(mouse);
            BuildPopupLayouts();
        }

        UpdateHoverSubmenu(mouse);
        BuildPopupLayouts();
        UpdateHoveredMenuItem(mouse);
        UpdateOpenContent(context);
    }

    private void BuildLayout()
    {
        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            BuildPopupLayouts();
            return;
        }

        BuildTopItemRects();
        TrimOpenPath();
        BuildOpenLayouts();
    }

    private void BuildTopItemRects()
    {
        _topItemRects.Clear();
        UiRect barBounds = GetBarBounds();
        int x = barBounds.X;

        foreach (MenuItem item in Items)
        {
            int width = GetTextWidth(item.Text) + BarItemPadding * 2;
            UiRect rect = new(x, barBounds.Y, width, barBounds.Height);
            _topItemRects.Add(rect);
            x += width + BarItemSpacing;
        }
    }

    private UiRect GetBarBounds()
    {
        int height = BarHeight;
        if (Bounds.Height > 0)
        {
            height = Math.Min(height, Bounds.Height);
        }

        return new UiRect(Bounds.X, Bounds.Y, Bounds.Width, height);
    }

    private void TrimOpenPath()
    {
        if (_openPath.Count == 0)
        {
            return;
        }

        int topIndex = _openPath[0];
        if (topIndex < 0 || topIndex >= Items.Count)
        {
            _openPath.Clear();
            return;
        }

        MenuItem current = Items[topIndex];
        if (!current.HasChildren || !current.Enabled)
        {
            _openPath.Clear();
            return;
        }

        for (int level = 1; level < _openPath.Count; level++)
        {
            int index = _openPath[level];
            if (index < 0 || index >= current.Items.Count)
            {
                _openPath.RemoveRange(level, _openPath.Count - level);
                return;
            }

            MenuItem item = current.Items[index];
            if (!item.HasChildren || !item.Enabled || item.IsSeparator || (item.HasContent && item.ContentCapturesInput))
            {
                _openPath.RemoveRange(level, _openPath.Count - level);
                return;
            }

            current = item;
        }
    }

    private void BuildOpenLayouts()
    {
        _openLayouts.Clear();
        if (_openPath.Count == 0)
        {
            return;
        }

        int topIndex = _openPath[0];
        if (topIndex < 0 || topIndex >= Items.Count || _topItemRects.Count <= topIndex)
        {
            _openPath.Clear();
            return;
        }

        MenuItem topItem = Items[topIndex];
        if (!topItem.HasChildren)
        {
            _openPath.Clear();
            return;
        }

        UiRect anchor = _topItemRects[topIndex];
        MenuLayout layout = BuildMenuLayout(topItem.Items, anchor, true, anchorBelow: true);
        _openLayouts.Add(layout);

        MenuItem current = topItem;
        for (int level = 1; level < _openPath.Count; level++)
        {
            int index = _openPath[level];
            if (index < 0 || index >= current.Items.Count)
            {
                _openPath.RemoveRange(level, _openPath.Count - level);
                return;
            }

            MenuItem item = current.Items[index];
            if (!item.HasChildren)
            {
                _openPath.RemoveRange(level, _openPath.Count - level);
                return;
            }

            UiRect itemRect = layout.ItemRects[index];
            layout = BuildMenuLayout(item.Items, itemRect, false, anchorBelow: false);
            _openLayouts.Add(layout);
            current = item;
        }
    }

    private void BuildPopupLayouts()
    {
        _openLayouts.Clear();
        if (!_popupOpen)
        {
            return;
        }

        UiRect anchor = Bounds;
        if (anchor.Width <= 0)
        {
            anchor = new UiRect(Bounds.X, Bounds.Y, DropdownMinWidth, Bounds.Height);
        }

        MenuLayout layout = BuildMenuLayout(Items, anchor, true, anchorBelow: false);
        _openLayouts.Add(layout);

        IReadOnlyList<MenuItem> currentItems = Items;
        MenuLayout currentLayout = layout;

        for (int level = 0; level < _openPath.Count; level++)
        {
            int index = _openPath[level];
            if (index < 0 || index >= currentItems.Count)
            {
                _openPath.RemoveRange(level, _openPath.Count - level);
                return;
            }

            MenuItem item = currentItems[index];
            if (!item.HasChildren || !item.Enabled || item.IsSeparator || (item.HasContent && item.ContentCapturesInput))
            {
                _openPath.RemoveRange(level, _openPath.Count - level);
                return;
            }

            UiRect itemRect = currentLayout.ItemRects[index];
            currentLayout = BuildMenuLayout(item.Items, itemRect, false, anchorBelow: false);
            _openLayouts.Add(currentLayout);
            currentItems = item.Items;
        }
    }

    private MenuLayout BuildMenuLayout(IReadOnlyList<MenuItem> items, UiRect anchor, bool topLevel, bool anchorBelow)
    {
        int textHeight = GetTextHeight(TextScale);
        int itemHeight = DropdownItemHeight > 0 ? DropdownItemHeight : textHeight + ItemVerticalPadding * 2;
        if (itemHeight <= 0)
        {
            itemHeight = textHeight + 4;
        }

        int checkArea = GetCheckmarkAreaWidth();
        int submenuWidth = Math.Max(0, SubmenuIndicatorWidth);
        int maxWidth = Math.Max(1, DropdownMinWidth);

        List<int> itemHeights = new(items.Count);
        int totalHeight = 0;

        foreach (MenuItem item in items)
        {
            int height = item.IsSeparator ? SeparatorHeight : GetItemHeight(item, itemHeight);
            if (height <= 0)
            {
                height = 1;
            }

            itemHeights.Add(height);
            totalHeight += height;

            if (item.IsSeparator)
            {
                continue;
            }

            int width = GetItemWidth(item, checkArea, submenuWidth);
            maxWidth = Math.Max(maxWidth, width);
        }

        int x = topLevel ? anchor.X : anchor.Right + SubmenuOffset;
        int y = anchorBelow ? anchor.Bottom : anchor.Y;
        UiRect bounds = new UiRect(x, y, maxWidth, totalHeight);
        bounds = ClampMenu(bounds);

        List<UiRect> rects = new(items.Count);
        int cursorY = bounds.Y;
        foreach (int height in itemHeights)
        {
            rects.Add(new UiRect(bounds.X, cursorY, bounds.Width, height));
            cursorY += height;
        }

        return new MenuLayout(items, bounds, rects);
    }

    private UiRect ClampMenu(UiRect bounds)
    {
        if (!ClampToParent || !TryGetClampBounds(out UiRect clamp))
        {
            return bounds;
        }

        if (clamp.Width <= 0 || clamp.Height <= 0)
        {
            return bounds;
        }

        int x = bounds.X;
        int y = bounds.Y;
        if (bounds.Right > clamp.Right)
        {
            x = clamp.Right - bounds.Width;
        }

        if (bounds.Bottom > clamp.Bottom)
        {
            y = clamp.Bottom - bounds.Height;
        }

        if (x < clamp.X)
        {
            x = clamp.X;
        }

        if (y < clamp.Y)
        {
            y = clamp.Y;
        }

        return new UiRect(x, y, bounds.Width, bounds.Height);
    }

    private bool TryGetClampBounds(out UiRect bounds)
    {
        bounds = default;
        if (Parent == null)
        {
            return false;
        }

        int offsetX = 0;
        int offsetY = 0;
        bool hasFallback = false;
        UiRect fallback = default;

        UiElement? parent = Parent;
        while (parent != null)
        {
            if (parent is UiTreeNode tree)
            {
                UiRect content = tree.ContentBounds;
                offsetX += content.X;
                offsetY += content.Y;
            }
            else if (parent is UiCollapsingHeader header)
            {
                UiRect content = header.ContentBounds;
                offsetX += content.X;
                offsetY += content.Y;
            }
            else if (parent is UiScrollPanel scrollPanel)
            {
                offsetX += scrollPanel.Bounds.X - scrollPanel.ScrollX;
                offsetY += scrollPanel.Bounds.Y - scrollPanel.ScrollY;
                UiRect viewport = scrollPanel.ViewportBounds;
                bounds = new UiRect(viewport.X - offsetX, viewport.Y - offsetY, viewport.Width, viewport.Height);
                return true;
            }
            else
            {
                UiRect parentBounds = parent.Bounds;
                fallback = new UiRect(parentBounds.X - offsetX, parentBounds.Y - offsetY, parentBounds.Width, parentBounds.Height);
                hasFallback = true;
            }

            parent = parent.Parent;
        }

        if (hasFallback)
        {
            bounds = fallback;
            return true;
        }

        return false;
    }

    private void HandleTopClick(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return;
        }

        MenuItem item = Items[index];
        if (!item.Enabled || item.IsSeparator)
        {
            return;
        }

        if (_openPath.Count > 0 && _openPath[0] == index)
        {
            CloseMenu();
            return;
        }

        if (item.HasChildren)
        {
            _openPath.Clear();
            _openPath.Add(index);
            return;
        }

        ActivateItem(item);
        CloseMenu();
    }

    private void HandleMenuClick(UiPoint point)
    {
        if (TryGetMenuItemAt(point, out int level, out int index))
        {
            MenuLayout layout = _openLayouts[level];
            MenuItem item = layout.Items[index];
            if (!item.Enabled || item.IsSeparator)
            {
                return;
            }

            if (item.HasContent && item.ContentCapturesInput)
            {
                return;
            }

            if (item.HasChildren)
            {
                OpenSubmenu(level, index);
                return;
            }

            ActivateItem(item);
            CloseMenu();
            return;
        }

        CloseMenu();
    }

    private void HandlePopupClick(UiPoint point)
    {
        if (!TryGetMenuItemAt(point, out int level, out int index))
        {
            if (ClosePopupOnOutsideClick)
            {
                ClosePopup();
            }

            return;
        }

        MenuLayout layout = _openLayouts[level];
        MenuItem item = layout.Items[index];
        if (!item.Enabled || item.IsSeparator)
        {
            return;
        }

        if (item.HasContent && item.ContentCapturesInput)
        {
            return;
        }

        if (item.HasChildren)
        {
            OpenSubmenu(level, index);
            return;
        }

        ActivateItem(item);
        if (ClosePopupOnItemClick)
        {
            ClosePopup();
        }
    }

    private void OpenTopOnHover(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return;
        }

        MenuItem item = Items[index];
        _openPath.Clear();
        if (item.Enabled && item.HasChildren && !item.IsSeparator)
        {
            _openPath.Add(index);
        }
    }

    private void UpdateHoverSubmenu(UiPoint point)
    {
        if (!TryGetMenuItemAt(point, out int level, out int index))
        {
            return;
        }

        MenuLayout layout = _openLayouts[level];
        MenuItem item = layout.Items[index];
        if (!item.Enabled || item.IsSeparator || (item.HasContent && item.ContentCapturesInput))
        {
            TrimPathAfterLevel(level);
            return;
        }

        if (item.HasChildren)
        {
            OpenSubmenu(level, index);
        }
        else
        {
            TrimPathAfterLevel(level);
        }
    }

    private void OpenSubmenu(int level, int index)
    {
        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            if (_openPath.Count > level)
            {
                _openPath.RemoveRange(level, _openPath.Count - level);
            }

            _openPath.Add(index);
            return;
        }

        if (_openPath.Count == 0)
        {
            return;
        }

        if (_openPath.Count > level + 1)
        {
            _openPath.RemoveRange(level + 1, _openPath.Count - (level + 1));
        }

        _openPath.Add(index);
    }

    private void TrimPathAfterLevel(int level)
    {
        int pathIndex = GetOpenPathIndexForLevel(level);
        if (_openPath.Count <= pathIndex + 1)
        {
            return;
        }

        _openPath.RemoveRange(pathIndex + 1, _openPath.Count - (pathIndex + 1));
    }

    private void CloseMenu()
    {
        _openPath.Clear();
    }

    private void ActivateItem(MenuItem item)
    {
        if (!item.Enabled)
        {
            return;
        }

        if (item.IsCheckable)
        {
            item.Checked = !item.Checked;
        }

        item.Clicked?.Invoke(item);
    }

    private void UpdateHoveredMenuItem(UiPoint point)
    {
        _hoveredMenuLevel = -1;
        _hoveredMenuIndex = -1;

        if (TryGetMenuItemAt(point, out int level, out int index))
        {
            MenuItem item = _openLayouts[level].Items[index];
            if (!(item.HasContent && item.ContentCapturesInput))
            {
                _hoveredMenuLevel = level;
                _hoveredMenuIndex = index;
            }
        }
    }

    private bool TryGetMenuItemAt(UiPoint point, out int level, out int index)
    {
        for (int i = 0; i < _openLayouts.Count; i++)
        {
            MenuLayout layout = _openLayouts[i];
            if (!layout.Bounds.Contains(point))
            {
                continue;
            }

            for (int j = 0; j < layout.ItemRects.Count; j++)
            {
                if (layout.ItemRects[j].Contains(point))
                {
                    level = i;
                    index = j;
                    return true;
                }
            }
        }

        level = -1;
        index = -1;
        return false;
    }

    private void UpdateOpenContent(UiUpdateContext context)
    {
        if (_openLayouts.Count == 0)
        {
            return;
        }

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;

        foreach (MenuLayout layout in _openLayouts)
        {
            for (int i = 0; i < layout.ItemRects.Count; i++)
            {
                MenuItem item = layout.Items[i];
                if (!item.HasContent || item.IsSeparator || item.Content == null)
                {
                    continue;
                }

                UiRect itemRect = layout.ItemRects[i];
                UiRect contentRect = GetContentRect(item, itemRect);
                if (contentRect.Width <= 0 || contentRect.Height <= 0)
                {
                    continue;
                }

                EnsureContentBounds(item, contentRect);
                bool mouseInContent = contentRect.Contains(mouse);
                bool allowInput = item.ContentCapturesInput && mouseInContent;
                UiPoint localMouse = allowInput
                    ? new UiPoint(mouse.X - contentRect.X, mouse.Y - contentRect.Y)
                    : new UiPoint(int.MinValue / 4, int.MinValue / 4);

                UiInputState childInput = new UiInputState
                {
                    MousePosition = localMouse,
                    ScreenMousePosition = input.ScreenMousePosition,
                    LeftDown = allowInput && input.LeftDown,
                    LeftClicked = allowInput && input.LeftClicked,
                    LeftReleased = allowInput && input.LeftReleased,
                    ShiftDown = input.ShiftDown,
                    CtrlDown = input.CtrlDown,
                    ScrollDelta = allowInput ? input.ScrollDelta : 0,
                    TextInput = input.TextInput,
                    Navigation = input.Navigation
                };

                item.Content.Update(new UiUpdateContext(childInput, context.Focus, context.DragDrop, context.DeltaSeconds));
            }
        }
    }

    private void RenderMenuItemContent(UiRenderContext context, MenuItem item, UiRect itemRect)
    {
        if (item.Content == null)
        {
            return;
        }

        UiRect contentRect = GetContentRect(item, itemRect);
        if (contentRect.Width <= 0 || contentRect.Height <= 0)
        {
            return;
        }

        EnsureContentBounds(item, contentRect);

        if (item.ContentClip)
        {
            context.Renderer.PushClip(contentRect);
        }

        OffsetRenderer offsetRenderer = new OffsetRenderer(context.Renderer, new UiPoint(contentRect.X, contentRect.Y));
        UiRenderContext childContext = new UiRenderContext(offsetRenderer);
        item.Content.Render(childContext);
        item.Content.RenderOverlay(childContext);

        if (item.ContentClip)
        {
            context.Renderer.PopClip();
        }
    }

    private void EnsureContentBounds(MenuItem item, UiRect contentRect)
    {
        if (item.Content == null)
        {
            return;
        }

        UiRect bounds = item.Content.Bounds;
        if (bounds.Width != contentRect.Width || bounds.Height != contentRect.Height || bounds.X != 0 || bounds.Y != 0)
        {
            item.Content.Bounds = new UiRect(0, 0, contentRect.Width, contentRect.Height);
        }
    }

    private UiRect GetContentRect(MenuItem item, UiRect itemRect)
    {
        int padding = GetContentPadding(item);
        int submenuWidth = item.HasChildren ? Math.Max(0, SubmenuIndicatorWidth) + ItemPadding : 0;
        int maxWidth = Math.Max(0, itemRect.Width - padding * 2 - submenuWidth);
        int maxHeight = Math.Max(0, itemRect.Height - padding * 2);
        int width = GetContentWidth(item, maxWidth);
        int height = GetContentHeight(item, maxHeight);
        width = Math.Clamp(width, 0, maxWidth);
        height = Math.Clamp(height, 0, maxHeight);
        return new UiRect(itemRect.X + padding, itemRect.Y + padding, width, height);
    }

    private bool IsPointInOpenLayouts(UiPoint point)
    {
        foreach (MenuLayout layout in _openLayouts)
        {
            if (layout.Bounds.Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsMenuItemInteractive(MenuItem item)
    {
        return !item.IsSeparator && item.Enabled && (!item.HasContent || !item.ContentCapturesInput);
    }

    private bool IsMenuItemOpen(int level, int index)
    {
        int pathIndex = GetOpenPathIndexForLevel(level);
        return _openPath.Count > pathIndex && _openPath[pathIndex] == index;
    }

    private int GetOpenPathIndexForLevel(int level)
    {
        return DisplayMode == UiMenuDisplayMode.Popup ? level : level + 1;
    }

    private int GetContentPadding(MenuItem item)
    {
        return item.ContentPadding >= 0 ? item.ContentPadding : ItemPadding;
    }

    private int GetContentWidth(MenuItem item, int fallbackWidth)
    {
        if (item.ContentWidth > 0)
        {
            return item.ContentWidth;
        }

        if (item.Content != null && item.Content.Bounds.Width > 0)
        {
            return item.Content.Bounds.Width;
        }

        return fallbackWidth;
    }

    private int GetContentHeight(MenuItem item, int fallbackHeight)
    {
        if (item.ContentHeight > 0)
        {
            return item.ContentHeight;
        }

        if (item.Content != null && item.Content.Bounds.Height > 0)
        {
            return item.Content.Bounds.Height;
        }

        return fallbackHeight;
    }

    private int GetItemHeight(MenuItem item, int defaultItemHeight)
    {
        if (!item.HasContent)
        {
            return defaultItemHeight;
        }

        int padding = GetContentPadding(item);
        int contentHeight = GetContentHeight(item, Math.Max(0, defaultItemHeight - padding * 2));
        return Math.Max(defaultItemHeight, contentHeight + padding * 2);
    }

    private int GetItemWidth(MenuItem item, int checkArea, int submenuWidth)
    {
        if (!item.HasContent)
        {
            int textWidth = GetTextWidth(item.Text);
            int shortcutWidth = string.IsNullOrEmpty(item.Shortcut) ? 0 : GetTextWidth(item.Shortcut);
            int width = ItemPadding + checkArea + textWidth + ItemPadding;
            if (shortcutWidth > 0)
            {
                width += ShortcutPadding + shortcutWidth;
            }

            if (item.HasChildren)
            {
                width += submenuWidth + ItemPadding;
            }

            return width;
        }

        int padding = GetContentPadding(item);
        int contentWidth = GetContentWidth(item, Math.Max(0, DropdownMinWidth - padding * 2));
        int widthWithPadding = contentWidth + padding * 2;
        if (item.HasChildren)
        {
            widthWithPadding += submenuWidth + ItemPadding;
        }

        return Math.Max(DropdownMinWidth, widthWithPadding);
    }

    private int GetTopItemIndex(UiPoint point)
    {
        UiRect barBounds = GetBarBounds();
        if (!barBounds.Contains(point))
        {
            return -1;
        }

        for (int i = 0; i < _topItemRects.Count; i++)
        {
            if (_topItemRects[i].Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    private int GetTextWidth(string text)
    {
        if (MeasureTextWidth != null)
        {
            return MeasureTextWidth(text, TextScale);
        }

        return text.Length * FallbackCharWidth * TextScale;
    }

    private int GetTextHeight(int scale)
    {
        if (MeasureTextHeight != null)
        {
            return MeasureTextHeight(scale);
        }

        return FallbackCharHeight * scale;
    }

    private int GetCheckmarkAreaWidth()
    {
        if (CheckmarkAreaWidth > 0)
        {
            return CheckmarkAreaWidth;
        }

        return CheckmarkSize + ItemPadding;
    }

    private int GetSubmenuIndicatorWidth(MenuItem item)
    {
        if (!item.HasChildren)
        {
            return 0;
        }

        return Math.Max(0, SubmenuIndicatorWidth);
    }

    private void DrawCheckmark(UiRenderContext context, UiRect rect)
    {
        int size = Math.Min(CheckmarkSize, rect.Height - 4);
        if (size <= 0)
        {
            return;
        }

        int x = rect.X + ItemPadding;
        int y = rect.Y + (rect.Height - size) / 2;
        context.Renderer.FillRect(new UiRect(x, y, size, size), CheckmarkColor);
    }

    private void DrawSubmenuArrow(UiRenderContext context, UiRect rect, UiColor color)
    {
        if (SubmenuIndicatorWidth <= 0)
        {
            return;
        }

        int size = Math.Min(SubmenuIndicatorWidth, rect.Height - 4);
        size = Math.Max(4, size);

        int x = rect.Right - ItemPadding - size;
        int y = rect.Y + (rect.Height - size) / 2;
        UiArrow.DrawTriangle(context.Renderer, x, y, size, UiArrowDirection.Right, color);
    }
}
