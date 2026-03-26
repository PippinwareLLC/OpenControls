namespace OpenControls.Controls;

public sealed class UiChildRegion : UiElement
{
    private readonly UiScrollPanel _scrollPanel;

    public UiChildRegion()
    {
        _scrollPanel = new UiScrollPanel
        {
            Background = UiColor.Transparent,
            Border = UiColor.Transparent
        };
        AddChild(_scrollPanel);
    }

    public UiScrollPanel ScrollPanel => _scrollPanel;
    public UiElement ContentRoot => _scrollPanel;

    public UiColor Background
    {
        get => _scrollPanel.Background;
        set => _scrollPanel.Background = value;
    }

    public UiColor Border
    {
        get => _scrollPanel.Border;
        set => _scrollPanel.Border = value;
    }

    public int BorderThickness
    {
        get => _scrollPanel.BorderThickness;
        set => _scrollPanel.BorderThickness = value;
    }

    public int CornerRadius
    {
        get => _scrollPanel.CornerRadius;
        set => _scrollPanel.CornerRadius = value;
    }

    public UiScrollbarVisibility HorizontalScrollbar
    {
        get => _scrollPanel.HorizontalScrollbar;
        set => _scrollPanel.HorizontalScrollbar = value;
    }

    public UiScrollbarVisibility VerticalScrollbar
    {
        get => _scrollPanel.VerticalScrollbar;
        set => _scrollPanel.VerticalScrollbar = value;
    }

    public int ScrollbarThickness
    {
        get => _scrollPanel.ScrollbarThickness;
        set => _scrollPanel.ScrollbarThickness = value;
    }

    public int ScrollbarPadding
    {
        get => _scrollPanel.ScrollbarPadding;
        set => _scrollPanel.ScrollbarPadding = value;
    }

    public int ScrollWheelStep
    {
        get => _scrollPanel.ScrollWheelStep;
        set => _scrollPanel.ScrollWheelStep = value;
    }

    public UiPoint ScrollOffset
    {
        get => _scrollPanel.ScrollOffset;
        set => _scrollPanel.ScrollOffset = value;
    }

    public void AddContentChild(UiElement child)
    {
        _scrollPanel.AddChild(child);
    }

    public bool RemoveContentChild(UiElement child)
    {
        return _scrollPanel.RemoveChild(child);
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _scrollPanel.Bounds = Bounds;
        base.Update(context);
    }
}
