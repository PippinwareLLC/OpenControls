using System.Text;

namespace OpenControls.Controls;

public sealed class UiTabBar : UiElement
{
    private static readonly Encoding Latin1Encoding = Encoding.Latin1;

    private int _activeIndex = -1;
    private int _hoverIndex = -1;
    private int _pressedIndex = -1;
    private bool _focused;

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

    public event Action<UiTabItem>? ActiveTabChanged;

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

        if (input.LeftClicked && _hoverIndex >= 0 && IsTabEnabled(tabs, _hoverIndex))
        {
            _pressedIndex = _hoverIndex;
            context.Focus.RequestFocus(this);
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
            if (_pressedIndex >= 0 && _pressedIndex == _hoverIndex && IsTabEnabled(tabs, _pressedIndex))
            {
                SetActiveIndex(_pressedIndex, tabs);
            }

            _pressedIndex = -1;
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
            context.Renderer.PushClip(Bounds);
            for (int i = 0; i < tabs.Count; i++)
            {
                UiTabItem tab = tabs[i];
                UiRect tabRect = tab.TabBounds;
                UiColor tabColor = i == _activeIndex ? TabActiveColor : (_hoverIndex == i ? TabHoverColor : TabBarColor);
                context.Renderer.FillRect(tabRect, tabColor);
                context.Renderer.DrawRect(tabRect, TabBorderColor, 1);

                UiColor textColor = i == _activeIndex ? TabActiveTextColor : TabTextColor;
                int textHeight = context.Renderer.MeasureTextHeight(TabTextScale);
                int textY = tabRect.Y + (tabRect.Height - textHeight) / 2;
                int textX = tabRect.X + Math.Max(0, TabPadding);
                context.Renderer.DrawText(tab.Text, new UiPoint(textX, textY), textColor, TabTextScale);
            }
            context.Renderer.PopClip();
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

    private void UpdateTabLayout(List<UiTabItem> tabs)
    {
        if (tabs.Count == 0)
        {
            _activeIndex = -1;
            return;
        }

        if (_activeIndex < 0 || _activeIndex >= tabs.Count)
        {
            SetActiveIndex(0, tabs);
        }

        int tabHeight = Math.Max(0, TabBarHeight);
        int spacing = Math.Max(0, TabSpacing);
        int x = Bounds.X;
        for (int i = 0; i < tabs.Count; i++)
        {
            UiTabItem tab = tabs[i];
            int width = GetTabWidth(tab);
            tab.TabBounds = new UiRect(x, Bounds.Y, width, tabHeight);
            x += width + spacing;
        }
    }

    private int GetTabIndexAt(UiPoint point, List<UiTabItem> tabs)
    {
        if (point.X < Bounds.X || point.X >= Bounds.Right || point.Y < Bounds.Y || point.Y >= Bounds.Y + TabBarHeight)
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
            width = textWidth + Math.Max(0, TabPadding) * 2;
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
