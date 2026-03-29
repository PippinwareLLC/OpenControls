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
    private readonly record struct TopItemTextMetrics(int ContentWidth, int DrawOffsetX);
    private UiFont _lastDefaultFont = UiFont.Default;

    public sealed class MenuItem
    {
        public string Text { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
        public string CommandId { get; set; } = string.Empty;
        public UiKeyChord? ShortcutChord { get; set; }
        public bool AllowShortcutDuringTextInput { get; set; }
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
        public Action<MenuItem, UiMenuItemActivationSource>? Invoked { get; set; }

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

    private readonly struct ShortcutBinding
    {
        public ShortcutBinding(UiKey key, UiModifierKeys modifiers, bool usesPrimaryModifier)
        {
            Key = key;
            Modifiers = modifiers;
            UsesPrimaryModifier = usesPrimaryModifier;
        }

        public UiKey Key { get; }
        public UiModifierKeys Modifiers { get; }
        public bool UsesPrimaryModifier { get; }

        public static bool TryParse(string text, out ShortcutBinding binding)
        {
            binding = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            UiModifierKeys modifiers = UiModifierKeys.None;
            bool usesPrimaryModifier = false;
            UiKey key = UiKey.Unknown;

            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (IsPrimaryModifierToken(token))
                {
                    usesPrimaryModifier = true;
                    continue;
                }

                if (TryParseModifier(token, out UiModifierKeys modifier))
                {
                    modifiers |= modifier;
                    continue;
                }

                if (!TryParseKey(token, out key))
                {
                    return false;
                }
            }

            if (key == UiKey.Unknown)
            {
                return false;
            }

            binding = new ShortcutBinding(key, modifiers, usesPrimaryModifier);
            return true;
        }

        private static bool IsPrimaryModifierToken(string token)
        {
            return token.Equals("ctrl", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("control", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("cmd", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("command", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("cmdorctrl", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("commandorcontrol", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("primary", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseModifier(string token, out UiModifierKeys modifier)
        {
            if (token.Equals("shift", StringComparison.OrdinalIgnoreCase))
            {
                modifier = UiModifierKeys.Shift;
                return true;
            }

            if (token.Equals("alt", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("option", StringComparison.OrdinalIgnoreCase))
            {
                modifier = UiModifierKeys.Alt;
                return true;
            }

            if (token.Equals("super", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("meta", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("win", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("windows", StringComparison.OrdinalIgnoreCase))
            {
                modifier = UiModifierKeys.Super;
                return true;
            }

            modifier = UiModifierKeys.None;
            return false;
        }

        private static bool TryParseKey(string token, out UiKey key)
        {
            if (token.Length == 1)
            {
                char character = char.ToUpperInvariant(token[0]);
                if (character is >= 'A' and <= 'Z')
                {
                    key = (UiKey)((int)UiKey.A + (character - 'A'));
                    return true;
                }

                if (character is >= '0' and <= '9')
                {
                    key = (UiKey)((int)UiKey.D0 + (character - '0'));
                    return true;
                }
            }

            key = token.ToLowerInvariant() switch
            {
                "left" => UiKey.Left,
                "right" => UiKey.Right,
                "up" => UiKey.Up,
                "down" => UiKey.Down,
                "pageup" => UiKey.PageUp,
                "pgup" => UiKey.PageUp,
                "pagedown" => UiKey.PageDown,
                "pgdn" => UiKey.PageDown,
                "home" => UiKey.Home,
                "end" => UiKey.End,
                "backspace" => UiKey.Backspace,
                "delete" => UiKey.Delete,
                "tab" => UiKey.Tab,
                "enter" => UiKey.Enter,
                "return" => UiKey.Enter,
                "space" => UiKey.Space,
                "spacebar" => UiKey.Space,
                "escape" => UiKey.Escape,
                "esc" => UiKey.Escape,
                "f1" => UiKey.F1,
                "f2" => UiKey.F2,
                "f3" => UiKey.F3,
                "f4" => UiKey.F4,
                "f5" => UiKey.F5,
                "f6" => UiKey.F6,
                "f7" => UiKey.F7,
                "f8" => UiKey.F8,
                "f9" => UiKey.F9,
                "f10" => UiKey.F10,
                "f11" => UiKey.F11,
                "f12" => UiKey.F12,
                _ => UiKey.Unknown
            };

            return key != UiKey.Unknown;
        }
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

        public UiFont DefaultFont
        {
            get => _inner.DefaultFont;
            set => _inner.DefaultFont = value;
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

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
            _inner.DrawText(text, Offset(position), color, scale, font);
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return _inner.MeasureTextWidth(text, scale);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            return _inner.MeasureTextWidth(text, scale, font);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return _inner.MeasureTextHeight(scale);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            return _inner.MeasureTextHeight(scale, font);
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
    private bool _suppressOutsideClick;
    private UiElement? _focusBeforeOpen;
    private bool _restoreFocusOnClose;

    public List<MenuItem> Items { get; } = new();

    public UiMenuDisplayMode DisplayMode { get; set; } = UiMenuDisplayMode.Bar;
    public bool IsPopupOpen => _popupOpen;
    public bool HasOpenMenu => DisplayMode == UiMenuDisplayMode.Popup ? _popupOpen : _openPath.Count > 0;
    public bool EnableKeyboardNavigation { get; set; } = true;
    public bool EnableShortcutDispatch { get; set; }
    public bool AllowShortcutsDuringTextInput { get; set; }
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
    public override bool IsFocusable => EnableKeyboardNavigation && (DisplayMode != UiMenuDisplayMode.Popup || _popupOpen);
    public override bool CapturesPointerInput => DisplayMode != UiMenuDisplayMode.Popup || _popupOpen;

    public event Action<MenuItem, UiMenuItemActivationSource>? ItemInvoked;

    public void OpenPopup()
    {
        if (_popupOpen)
        {
            return;
        }

        _popupOpen = true;
        _suppressOutsideClick = true;
        _openPath.Clear();
        ClearSelection();
    }

    public void OpenAttached(UiRect anchorBounds)
    {
        Bounds = anchorBounds;
        OpenPopup();
    }

    public void OpenContext(UiPoint point, int width = 0)
    {
        int popupWidth = width > 0 ? width : DropdownMinWidth;
        Bounds = new UiRect(point.X, point.Y, popupWidth, 0);
        OpenPopup();
    }

    public void ClosePopup()
    {
        ClosePopup(null);
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

    private void ClosePopup(UiFocusManager? focus)
    {
        if (!_popupOpen)
        {
            return;
        }

        _popupOpen = false;
        _openPath.Clear();
        _openLayouts.Clear();
        ClearSelection();
        RestoreFocus(focus);
    }

    public bool TryInvokeCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return false;
        }

        if (!TryFindCommandItem(Items, commandId, out MenuItem? item) || item == null)
        {
            return false;
        }

        ActivateItem(item, UiMenuItemActivationSource.Programmatic);
        return true;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        bool wasOpen = HasOpenMenu;
        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            UpdatePopup(context, wasOpen);
            return;
        }

        BuildLayout(context.DefaultFont);

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;
        _hoveredTopIndex = GetTopItemIndex(mouse);

        TryDispatchShortcut(context);
        BuildLayout(context.DefaultFont);

        if (_openPath.Count > 0 && input.Navigation.Escape)
        {
            CloseMenu(context.Focus);
            BuildLayout(context.DefaultFont);
        }

        bool menuOpen = _openPath.Count > 0;

        if (input.LeftClicked)
        {
            if (_hoveredTopIndex >= 0)
            {
                HandleTopClick(_hoveredTopIndex, context.Focus);
                BuildLayout(context.DefaultFont);
            }
            else if (menuOpen)
            {
                HandleMenuClick(mouse, context.Focus);
                BuildLayout(context.DefaultFont);
            }
        }

        menuOpen = _openPath.Count > 0;

        if (menuOpen)
        {
            if (_hoveredTopIndex >= 0 && _hoveredTopIndex != _openPath[0])
            {
                OpenTopOnHover(_hoveredTopIndex);
                BuildLayout(context.DefaultFont);
            }
            else
            {
                UpdateHoverSubmenu(mouse);
                BuildLayout();
            }
        }

        if (EnableKeyboardNavigation)
        {
            HandleKeyboardNavigation(context);
            BuildLayout(context.DefaultFont);
        }

        UpdateHoveredMenuItem(mouse);

        UpdateOpenContent(context);

        if (!wasOpen && HasOpenMenu)
        {
            CaptureFocus(context.Focus);
        }
        else if (wasOpen && !HasOpenMenu)
        {
            RestoreFocus(context.Focus);
        }
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible || (DisplayMode == UiMenuDisplayMode.Popup && !_popupOpen))
        {
            return null;
        }

        if (TryGetMenuContentHit(point, out UiElement? contentHit) && contentHit != null)
        {
            return contentHit;
        }

        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            if (IsPointInOpenLayouts(point))
            {
                return this;
            }
        }
        else if (GetBarBounds().Contains(point) || IsPointInOpenLayouts(point))
        {
            return this;
        }

        return base.HitTest(point);
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

        UiFont font = ResolveFont(context.DefaultFont);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);
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
            TopItemTextMetrics textMetrics = GetTopItemTextMetrics(item.Text, font);
            context.Renderer.DrawText(item.Text, new UiPoint(rect.X + BarItemPadding + textMetrics.DrawOffsetX, textY), color, TextScale, font);
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

        UiFont font = ResolveFont(context.DefaultFont);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);
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
                    context.Renderer.DrawText(item.Text, new UiPoint(textX, itemTextY), color, TextScale, font);

                    if (!string.IsNullOrEmpty(item.Shortcut))
                    {
                        int shortcutWidth = GetTextWidth(item.Shortcut);
                        int shortcutX = itemRect.Right - ItemPadding - shortcutWidth - GetSubmenuIndicatorWidth(item);
                        context.Renderer.DrawText(item.Shortcut, new UiPoint(shortcutX, itemTextY), color, TextScale, font);
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

    private void UpdatePopup(UiUpdateContext context, bool wasOpen)
    {
        if (!_popupOpen)
        {
            _openLayouts.Clear();
            _openPath.Clear();
            if (wasOpen)
            {
                RestoreFocus(context.Focus);
            }
            return;
        }

        BuildPopupLayouts();

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;

        TryDispatchShortcut(context);
        BuildPopupLayouts();
        if (!_popupOpen)
        {
            if (wasOpen)
            {
                RestoreFocus(context.Focus);
            }

            return;
        }

        if (ClosePopupOnEscape && input.Navigation.Escape)
        {
            ClosePopup(context.Focus);
            return;
        }

        if (_suppressOutsideClick)
        {
            _suppressOutsideClick = false;
        }
        else if (ClosePopupOnOutsideClick && IsAnyMouseClick(input) && !IsPointInOpenLayouts(mouse))
        {
            ClosePopup(context.Focus);
            return;
        }

        if (input.LeftClicked)
        {
            HandlePopupClick(mouse, context.Focus);
            BuildPopupLayouts();
        }

        if (EnableKeyboardNavigation)
        {
            HandleKeyboardNavigation(context);
            BuildPopupLayouts();
        }

        UpdateHoverSubmenu(mouse);
        BuildPopupLayouts();
        UpdateHoveredMenuItem(mouse);
        UpdateOpenContent(context);

        if (!wasOpen && _popupOpen)
        {
            CaptureFocus(context.Focus);
        }
        else if (wasOpen && !_popupOpen)
        {
            RestoreFocus(context.Focus);
        }
    }

    private void BuildLayout()
    {
        BuildLayout(_lastDefaultFont);
    }

    private void BuildLayout(UiFont defaultFont)
    {
        _lastDefaultFont = defaultFont ?? UiFont.Default;
        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            BuildPopupLayouts();
            return;
        }

        BuildTopItemRects(ResolveFont(_lastDefaultFont));
        TrimOpenPath();
        BuildOpenLayouts();
    }

    private void BuildTopItemRects(UiFont font)
    {
        _topItemRects.Clear();
        UiRect barBounds = GetBarBounds();
        int x = barBounds.X;

        foreach (MenuItem item in Items)
        {
            TopItemTextMetrics textMetrics = GetTopItemTextMetrics(item.Text, font);
            int width = textMetrics.ContentWidth + BarItemPadding * 2;
            UiRect rect = new(x, barBounds.Y, width, barBounds.Height);
            _topItemRects.Add(rect);
            x += width + BarItemSpacing;
        }
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (HasOpenMenu)
        {
            status |= UiItemStatusFlags.Active;
        }

        return status;
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

    private void HandleTopClick(int index, UiFocusManager focus)
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
            CloseMenu(focus);
            return;
        }

        if (item.HasChildren)
        {
            _openPath.Clear();
            _openPath.Add(index);
            SelectBoundaryItem(0, first: true);
            return;
        }

        ActivateItem(item, UiMenuItemActivationSource.Mouse);
        CloseMenu(focus);
    }

    private void HandleMenuClick(UiPoint point, UiFocusManager focus)
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
                BuildLayout();
                SelectBoundaryItem(Math.Min(level + 1, _openLayouts.Count - 1), first: true);
                return;
            }

            ActivateItem(item, UiMenuItemActivationSource.Mouse);
            CloseMenu(focus);
            return;
        }

        CloseMenu(focus);
    }

    private void HandlePopupClick(UiPoint point, UiFocusManager focus)
    {
        if (!TryGetMenuItemAt(point, out int level, out int index))
        {
            if (ClosePopupOnOutsideClick)
            {
                ClosePopup(focus);
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
            BuildPopupLayouts();
            SelectBoundaryItem(Math.Min(level + 1, _openLayouts.Count - 1), first: true);
            return;
        }

        ActivateItem(item, UiMenuItemActivationSource.Mouse);
        if (ClosePopupOnItemClick)
        {
            ClosePopup(focus);
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
            _hoveredMenuLevel = 0;
            _hoveredMenuIndex = FindNextInteractiveIndex(item.Items, -1, 1);
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

    private void CloseMenu(UiFocusManager? focus = null)
    {
        _openPath.Clear();
        ClearSelection();
        RestoreFocus(focus);
    }

    private void ActivateItem(MenuItem item, UiMenuItemActivationSource source)
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
        item.Invoked?.Invoke(item, source);
        ItemInvoked?.Invoke(item, source);
    }

    private void UpdateHoveredMenuItem(UiPoint point)
    {
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
                UiPoint? leftDragOrigin = allowInput ? TranslatePoint(input.LeftDragOrigin, contentRect.X, contentRect.Y) : null;
                UiPoint? rightDragOrigin = allowInput ? TranslatePoint(input.RightDragOrigin, contentRect.X, contentRect.Y) : null;
                UiPoint? middleDragOrigin = allowInput ? TranslatePoint(input.MiddleDragOrigin, contentRect.X, contentRect.Y) : null;

                UiInputState childInput = new UiInputState
                {
                    MousePosition = localMouse,
                    ScreenMousePosition = input.ScreenMousePosition,
                    LeftDown = allowInput && input.LeftDown,
                    LeftClicked = allowInput && input.LeftClicked,
                    LeftDoubleClicked = allowInput && input.LeftDoubleClicked,
                    LeftReleased = allowInput && input.LeftReleased,
                    RightDown = allowInput && input.RightDown,
                    RightClicked = allowInput && input.RightClicked,
                    RightDoubleClicked = allowInput && input.RightDoubleClicked,
                    RightReleased = allowInput && input.RightReleased,
                    MiddleDown = allowInput && input.MiddleDown,
                    MiddleClicked = allowInput && input.MiddleClicked,
                    MiddleDoubleClicked = allowInput && input.MiddleDoubleClicked,
                    MiddleReleased = allowInput && input.MiddleReleased,
                    LeftDragOrigin = leftDragOrigin,
                    RightDragOrigin = rightDragOrigin,
                    MiddleDragOrigin = middleDragOrigin,
                    DragThreshold = input.DragThreshold,
                    ShiftDown = input.ShiftDown,
                    CtrlDown = input.CtrlDown,
                    AltDown = input.AltDown,
                    SuperDown = input.SuperDown,
                    ScrollDeltaX = allowInput ? input.ScrollDeltaX : 0,
                    ScrollDelta = allowInput ? input.ScrollDelta : 0,
                    TextInput = input.TextInput,
                    Composition = input.Composition,
                    KeysDown = input.KeysDown,
                    KeysPressed = input.KeysPressed,
                    KeysReleased = input.KeysReleased,
                    Navigation = input.Navigation
                };

                item.Content.Update(new UiUpdateContext(childInput, context.Focus, context.DragDrop, context.DeltaSeconds, context.DefaultFont, context.Clipboard));
            }
        }
    }

    private void HandleKeyboardNavigation(UiUpdateContext context)
    {
        UiInputState input = context.Input;
        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            if (!_popupOpen)
            {
                return;
            }
        }
        else if (_openPath.Count == 0)
        {
            if (context.Focus.Focused != this)
            {
                return;
            }

            if (input.Navigation.MoveDown || input.Navigation.Enter || input.Navigation.KeypadEnter || input.Navigation.Space)
            {
                int topIndex = FindNextEnabledTopIndex(-1, 1);
                if (topIndex >= 0)
                {
                    _openPath.Clear();
                    _openPath.Add(topIndex);
                    BuildLayout();
                    SelectBoundaryItem(0, first: true);
                }
            }

            return;
        }

        if (_openLayouts.Count == 0)
        {
            return;
        }

        if (input.Navigation.Home)
        {
            SelectBoundaryItem(GetSelectedMenuLevel(), first: true);
            return;
        }

        if (input.Navigation.End)
        {
            SelectBoundaryItem(GetSelectedMenuLevel(), first: false);
            return;
        }

        if (input.Navigation.MoveDown)
        {
            MoveSelection(1);
            return;
        }

        if (input.Navigation.MoveUp)
        {
            MoveSelection(-1);
            return;
        }

        if (input.Navigation.MoveRight)
        {
            NavigateRight();
            return;
        }

        if (input.Navigation.MoveLeft)
        {
            NavigateLeft(context.Focus);
            return;
        }

        if (input.Navigation.Enter || input.Navigation.KeypadEnter || input.Navigation.Space)
        {
            ActivateSelection(context.Focus);
        }
    }

    private void NavigateRight()
    {
        if (_openLayouts.Count == 0)
        {
            return;
        }

        int level = GetSelectedMenuLevel();
        int index = EnsureSelectedItem(level, first: true);
        if (index < 0)
        {
            return;
        }

        MenuItem item = _openLayouts[level].Items[index];
        if (item.HasChildren && IsMenuItemInteractive(item))
        {
            OpenSubmenu(level, index);
            RebuildCurrentLayouts();
            SelectBoundaryItem(Math.Min(level + 1, _openLayouts.Count - 1), first: true);
            return;
        }

        if (DisplayMode != UiMenuDisplayMode.Popup && level == 0 && _openPath.Count > 0)
        {
            int nextTopIndex = FindNextEnabledTopIndex(_openPath[0], 1);
            if (nextTopIndex >= 0)
            {
                _openPath.Clear();
                _openPath.Add(nextTopIndex);
                RebuildCurrentLayouts();
                SelectBoundaryItem(0, first: true);
            }
        }
    }

    private void NavigateLeft(UiFocusManager focus)
    {
        if (_openLayouts.Count == 0)
        {
            return;
        }

        int level = GetSelectedMenuLevel();
        if (DisplayMode != UiMenuDisplayMode.Popup && level == 0 && _openPath.Count > 0)
        {
            int previousTopIndex = FindNextEnabledTopIndex(_openPath[0], -1);
            if (previousTopIndex >= 0)
            {
                _openPath.Clear();
                _openPath.Add(previousTopIndex);
                RebuildCurrentLayouts();
                SelectBoundaryItem(0, first: true);
            }

            return;
        }

        if (DisplayMode == UiMenuDisplayMode.Popup && level == 0)
        {
            ClosePopup(focus);
            return;
        }

        if (level <= 0)
        {
            return;
        }

        int parentLevel = level - 1;
        int parentIndex = GetParentItemIndexForLevel(level);
        CloseSubmenuLevel(level);
        RebuildCurrentLayouts();
        if (parentIndex >= 0)
        {
            _hoveredMenuLevel = parentLevel;
            _hoveredMenuIndex = parentIndex;
        }
    }

    private void ActivateSelection(UiFocusManager focus)
    {
        if (_openLayouts.Count == 0)
        {
            return;
        }

        int level = GetSelectedMenuLevel();
        int index = EnsureSelectedItem(level, first: true);
        if (index < 0)
        {
            return;
        }

        MenuItem item = _openLayouts[level].Items[index];
        if (!item.Enabled || item.IsSeparator)
        {
            return;
        }

        if (item.HasChildren)
        {
            OpenSubmenu(level, index);
            RebuildCurrentLayouts();
            SelectBoundaryItem(Math.Min(level + 1, _openLayouts.Count - 1), first: true);
            return;
        }

        ActivateItem(item, UiMenuItemActivationSource.Keyboard);
        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            if (ClosePopupOnItemClick)
            {
                ClosePopup(focus);
            }
        }
        else
        {
            CloseMenu(focus);
        }
    }

    private void MoveSelection(int direction)
    {
        if (_openLayouts.Count == 0)
        {
            return;
        }

        int level = GetSelectedMenuLevel();
        int currentIndex = _hoveredMenuLevel == level ? _hoveredMenuIndex : -1;
        int nextIndex = FindNextInteractiveIndex(_openLayouts[level].Items, currentIndex, direction);
        if (nextIndex >= 0)
        {
            _hoveredMenuLevel = level;
            _hoveredMenuIndex = nextIndex;
        }
    }

    private int GetSelectedMenuLevel()
    {
        if (_hoveredMenuLevel >= 0 && _hoveredMenuLevel < _openLayouts.Count)
        {
            return _hoveredMenuLevel;
        }

        return Math.Max(0, _openLayouts.Count - 1);
    }

    private int EnsureSelectedItem(int level, bool first)
    {
        if (level < 0 || level >= _openLayouts.Count)
        {
            return -1;
        }

        if (_hoveredMenuLevel == level &&
            _hoveredMenuIndex >= 0 &&
            _hoveredMenuIndex < _openLayouts[level].Items.Count &&
            IsMenuItemInteractive(_openLayouts[level].Items[_hoveredMenuIndex]))
        {
            return _hoveredMenuIndex;
        }

        int index = first
            ? FindNextInteractiveIndex(_openLayouts[level].Items, -1, 1)
            : FindNextInteractiveIndex(_openLayouts[level].Items, _openLayouts[level].Items.Count, -1);
        if (index >= 0)
        {
            _hoveredMenuLevel = level;
            _hoveredMenuIndex = index;
        }

        return index;
    }

    private void SelectBoundaryItem(int level, bool first)
    {
        EnsureSelectedItem(level, first);
    }

    private int FindNextEnabledTopIndex(int startIndex, int direction)
    {
        if (Items.Count == 0)
        {
            return -1;
        }

        int index = startIndex;
        for (int attempt = 0; attempt < Items.Count; attempt++)
        {
            index += direction;
            if (index < 0)
            {
                index = Items.Count - 1;
            }
            else if (index >= Items.Count)
            {
                index = 0;
            }

            MenuItem item = Items[index];
            if (item.Enabled && !item.IsSeparator)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindNextInteractiveIndex(IReadOnlyList<MenuItem> items, int startIndex, int direction)
    {
        if (items.Count == 0)
        {
            return -1;
        }

        int index = startIndex;
        if (direction > 0 && startIndex < 0)
        {
            index = -1;
        }
        else if (direction < 0 && startIndex >= items.Count)
        {
            index = items.Count;
        }

        for (int attempt = 0; attempt < items.Count; attempt++)
        {
            index += direction;
            if (index < 0)
            {
                index = items.Count - 1;
            }
            else if (index >= items.Count)
            {
                index = 0;
            }

            MenuItem item = items[index];
            if (!item.IsSeparator && item.Enabled && (!item.HasContent || !item.ContentCapturesInput))
            {
                return index;
            }
        }

        return -1;
    }

    private int GetParentItemIndexForLevel(int level)
    {
        if (level <= 0)
        {
            return -1;
        }

        int pathIndex = DisplayMode == UiMenuDisplayMode.Popup ? level - 1 : level;
        if (pathIndex < 0 || pathIndex >= _openPath.Count)
        {
            return -1;
        }

        return _openPath[pathIndex];
    }

    private void CloseSubmenuLevel(int level)
    {
        int removeIndex = DisplayMode == UiMenuDisplayMode.Popup ? level - 1 : level;
        if (removeIndex >= 0 && removeIndex < _openPath.Count)
        {
            _openPath.RemoveRange(removeIndex, _openPath.Count - removeIndex);
        }
    }

    private void RebuildCurrentLayouts()
    {
        if (DisplayMode == UiMenuDisplayMode.Popup)
        {
            BuildPopupLayouts();
        }
        else
        {
            BuildLayout();
        }
    }

    private bool TryDispatchShortcut(UiUpdateContext context)
    {
        if (!EnableShortcutDispatch)
        {
            return false;
        }

        if (DisplayMode == UiMenuDisplayMode.Popup && !_popupOpen && context.Focus.Focused != this)
        {
            return false;
        }

        if (!TryFindShortcutItem(Items, context.Input, context.Focus.Focused, out MenuItem? item) || item == null)
        {
            return false;
        }

        ActivateItem(item, UiMenuItemActivationSource.Shortcut);
        if (DisplayMode == UiMenuDisplayMode.Popup && _popupOpen && ClosePopupOnItemClick)
        {
            ClosePopup(context.Focus);
        }
        else if (DisplayMode != UiMenuDisplayMode.Popup && HasOpenMenu)
        {
            CloseMenu(context.Focus);
        }

        return true;
    }

    private bool TryFindShortcutItem(
        IReadOnlyList<MenuItem> items,
        UiInputState input,
        UiElement? focusedElement,
        out MenuItem? match)
    {
        for (int i = 0; i < items.Count; i++)
        {
            MenuItem item = items[i];
            if (!item.Enabled || item.IsSeparator)
            {
                continue;
            }

            if (item.HasChildren)
            {
                if (TryFindShortcutItem(item.Items, input, focusedElement, out match))
                {
                    return true;
                }
            }

            if (item.HasContent)
            {
                continue;
            }

            if (focusedElement?.WantsTextInput == true && !(AllowShortcutsDuringTextInput || item.AllowShortcutDuringTextInput))
            {
                continue;
            }

            if (ShortcutMatches(item, input))
            {
                match = item;
                return true;
            }
        }

        match = null;
        return false;
    }

    private static bool ShortcutMatches(MenuItem item, UiInputState input)
    {
        if (!TryGetShortcutBinding(item, out ShortcutBinding binding))
        {
            return false;
        }

        if (binding.UsesPrimaryModifier)
        {
            bool shift = (binding.Modifiers & UiModifierKeys.Shift) != 0;
            bool alt = (binding.Modifiers & UiModifierKeys.Alt) != 0;
            return input.IsPrimaryShortcutPressed(binding.Key, shift, alt);
        }

        return input.IsKeyPressed(binding.Key, binding.Modifiers);
    }

    private static bool TryFindCommandItem(IReadOnlyList<MenuItem> items, string commandId, out MenuItem? match)
    {
        for (int i = 0; i < items.Count; i++)
        {
            MenuItem item = items[i];
            if (item.Enabled && string.Equals(item.CommandId, commandId, StringComparison.OrdinalIgnoreCase))
            {
                match = item;
                return true;
            }

            if (item.HasChildren && TryFindCommandItem(item.Items, commandId, out match))
            {
                return true;
            }
        }

        match = null;
        return false;
    }

    private static bool TryGetShortcutBinding(MenuItem item, out ShortcutBinding binding)
    {
        if (item.ShortcutChord is UiKeyChord shortcutChord)
        {
            binding = new ShortcutBinding(shortcutChord.Key, shortcutChord.Modifiers, usesPrimaryModifier: false);
            return true;
        }

        if (string.IsNullOrWhiteSpace(item.Shortcut))
        {
            binding = default;
            return false;
        }

        return ShortcutBinding.TryParse(item.Shortcut, out binding);
    }

    private bool TryGetMenuContentHit(UiPoint point, out UiElement? hit)
    {
        for (int layoutIndex = _openLayouts.Count - 1; layoutIndex >= 0; layoutIndex--)
        {
            MenuLayout layout = _openLayouts[layoutIndex];
            for (int itemIndex = layout.ItemRects.Count - 1; itemIndex >= 0; itemIndex--)
            {
                MenuItem item = layout.Items[itemIndex];
                if (!item.HasContent || !item.ContentCapturesInput || item.Content == null)
                {
                    continue;
                }

                UiRect contentRect = GetContentRect(item, layout.ItemRects[itemIndex]);
                if (!contentRect.Contains(point))
                {
                    continue;
                }

                EnsureContentBounds(item, contentRect);
                UiPoint localPoint = new UiPoint(point.X - contentRect.X, point.Y - contentRect.Y);
                hit = item.Content.HitTest(localPoint) ?? item.Content;
                return true;
            }
        }

        hit = null;
        return false;
    }

    private void CaptureFocus(UiFocusManager focus)
    {
        if (!EnableKeyboardNavigation)
        {
            return;
        }

        if (!_restoreFocusOnClose)
        {
            _focusBeforeOpen = focus.Focused == this ? null : focus.Focused;
            _restoreFocusOnClose = true;
        }

        focus.RequestFocus(this);
    }

    private void RestoreFocus(UiFocusManager? focus)
    {
        if (!_restoreFocusOnClose)
        {
            return;
        }

        UiElement? restoreTarget = _focusBeforeOpen;
        _focusBeforeOpen = null;
        _restoreFocusOnClose = false;

        if (focus == null)
        {
            return;
        }

        if (restoreTarget != null && (!restoreTarget.Visible || !restoreTarget.Enabled || !restoreTarget.IsFocusable))
        {
            restoreTarget = null;
        }

        if (focus.Focused == this || focus.Focused == null)
        {
            focus.RequestFocus(restoreTarget);
        }
    }

    private void ClearSelection()
    {
        _hoveredMenuLevel = -1;
        _hoveredMenuIndex = -1;
    }

    private static UiPoint? TranslatePoint(UiPoint? point, int offsetX, int offsetY)
    {
        return point is UiPoint value
            ? new UiPoint(value.X - offsetX, value.Y - offsetY)
            : null;
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
        UiRenderContext childContext = new UiRenderContext(offsetRenderer, context.DefaultFont);
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

    private static bool IsAnyMouseClick(UiInputState input)
    {
        return input.LeftClicked || input.RightClicked || input.MiddleClicked;
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

    private TopItemTextMetrics GetTopItemTextMetrics(string text, UiFont font)
    {
        int advanceWidth = GetTextWidth(text);
        UiRect inkBounds = font.MeasureTextInkBounds(text ?? string.Empty, TextScale);
        if (inkBounds.Width <= 0)
        {
            return new TopItemTextMetrics(advanceWidth, 0);
        }

        int drawOffsetX = Math.Max(0, -inkBounds.X);
        int inkExtent = Math.Max(0, inkBounds.X) + inkBounds.Width;
        int contentWidth = Math.Max(advanceWidth + drawOffsetX, inkExtent);
        return new TopItemTextMetrics(contentWidth, drawOffsetX);
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
