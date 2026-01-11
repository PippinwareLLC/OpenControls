namespace OpenControls.Controls;

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
    public bool ShowTitleBar { get; set; } = true;
    public int TitleBarHeight { get; set; } = 24;
    public int TitleTextScale { get; set; } = 1;
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
    public bool ClampToParent { get; set; } = true;
    public bool IsDragging => _dragging;
    public bool IsResizing => _resizing;
    public UiScrollPanel? ScrollPanel => _scrollPanel;
    public UiElement ContentRoot => _scrollPanel != null ? _scrollPanel : this;

    public override UiRect ClipBounds => ContentBounds;

    public UiRect ContentBounds
    {
        get
        {
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
                    int maxWidth = parentBounds.Right - _resizeStartBounds.X;
                    int maxHeight = parentBounds.Bottom - _resizeStartBounds.Y;
                    width = Math.Min(width, maxWidth);
                    height = Math.Min(height, maxHeight);
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
                    newX = Math.Clamp(newX, parentBounds.X, maxX);
                    newY = Math.Clamp(newY, parentBounds.Y, maxY);
                }

                Bounds = new UiRect(newX, newY, Bounds.Width, Bounds.Height);
            }

            if (_dragging && input.LeftReleased)
            {
                _dragging = false;
            }
        }

        UpdateScrollPanelBounds();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

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

            int textHeight = context.Renderer.MeasureTextHeight(TitleTextScale);
            int textY = titleBar.Y + (TitleBarHeight - textHeight) / 2;
            context.Renderer.DrawText(Title, new UiPoint(titleBar.X + TitlePadding, textY), TitleTextColor, TitleTextScale);
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
}
