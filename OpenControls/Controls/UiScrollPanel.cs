namespace OpenControls.Controls;

public enum UiScrollbarVisibility
{
    Disabled,
    Auto,
    Always
}

public sealed class UiScrollPanel : UiElement
{
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

    private bool _draggingVertical;
    private bool _draggingHorizontal;
    private bool _hoverVerticalThumb;
    private bool _hoverHorizontalThumb;
    private int _dragStartMouse;
    private int _dragStartScroll;
    private UiPoint _contentSize;
    private UiPoint _viewportSize;
    private bool _showVertical;
    private bool _showHorizontal;
    private int _scrollX;
    private int _scrollY;

    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }

    public UiScrollbarVisibility HorizontalScrollbar { get; set; } = UiScrollbarVisibility.Auto;
    public UiScrollbarVisibility VerticalScrollbar { get; set; } = UiScrollbarVisibility.Auto;
    public int ScrollbarThickness { get; set; } = 12;
    public int ScrollbarPadding { get; set; } = 2;
    public int MinThumbSize { get; set; } = 12;
    public int ScrollWheelStep { get; set; } = 40;
    public UiColor ScrollbarTrack { get; set; } = new UiColor(20, 24, 34);
    public UiColor ScrollbarBorder { get; set; } = new UiColor(60, 70, 90);
    public UiColor ScrollbarThumb { get; set; } = new UiColor(70, 80, 100);
    public UiColor ScrollbarThumbHover { get; set; } = new UiColor(90, 110, 140);

    public int ScrollX
    {
        get => _scrollX;
        set => _scrollX = value;
    }

    public int ScrollY
    {
        get => _scrollY;
        set => _scrollY = value;
    }

    public UiPoint ScrollOffset
    {
        get => new UiPoint(_scrollX, _scrollY);
        set
        {
            _scrollX = value.X;
            _scrollY = value.Y;
        }
    }

    public UiPoint ContentSize => _contentSize;
    public UiRect ViewportBounds => new UiRect(Bounds.X, Bounds.Y, _viewportSize.X, _viewportSize.Y);

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        RefreshLayout();

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;
        UiRect viewport = ViewportBounds;
        bool mouseInViewport = viewport.Contains(mouse);
        bool mouseInScrollbar = false;

        if (_showVertical)
        {
            UiRect verticalBar = GetVerticalScrollbarBounds();
            mouseInScrollbar |= verticalBar.Contains(mouse);
            UpdateVerticalScrollbar(input, verticalBar);
        }

        if (_showHorizontal)
        {
            UiRect horizontalBar = GetHorizontalScrollbarBounds();
            mouseInScrollbar |= horizontalBar.Contains(mouse);
            UpdateHorizontalScrollbar(input, horizontalBar);
        }

        if (input.ScrollDelta != 0 && !_draggingVertical && !_draggingHorizontal && (mouseInViewport || mouseInScrollbar))
        {
            int steps = (int)Math.Round(input.ScrollDelta / 120f);
            if (steps != 0)
            {
                if (_showVertical)
                {
                    _scrollY -= steps * ScrollWheelStep;
                }
                else if (_showHorizontal)
                {
                    _scrollX -= steps * ScrollWheelStep;
                }
            }
        }

        ClampScrollOffset();

        UiInputState childInput = BuildChildInput(input, mouseInViewport && !mouseInScrollbar);
        UiUpdateContext childContext = new UiUpdateContext(childInput, context.Focus, context.DragDrop, context.DeltaSeconds);
        foreach (UiElement child in Children)
        {
            child.Update(childContext);
        }
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        RefreshLayout();

        if (Background.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        UiRect viewport = ViewportBounds;
        UiPoint offset = new UiPoint(Bounds.X - _scrollX, Bounds.Y - _scrollY);
        OffsetRenderer offsetRenderer = new OffsetRenderer(context.Renderer, offset);
        context.Renderer.PushClip(viewport);
        UiRenderContext childContext = new UiRenderContext(offsetRenderer);
        foreach (UiElement child in Children)
        {
            child.Render(childContext);
        }
        context.Renderer.PopClip();

        if (CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }

        DrawScrollbars(context);

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        RefreshLayout();

        UiRect viewport = ViewportBounds;
        UiPoint offset = new UiPoint(Bounds.X - _scrollX, Bounds.Y - _scrollY);
        OffsetRenderer offsetRenderer = new OffsetRenderer(context.Renderer, offset);
        context.Renderer.PushClip(viewport);
        UiRenderContext childContext = new UiRenderContext(offsetRenderer);
        foreach (UiElement child in Children)
        {
            child.RenderOverlay(childContext);
        }
        context.Renderer.PopClip();

        if (CornerRadius > 0 && Background.A > 0)
        {
            UiRenderHelpers.MaskRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        }
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible || !Bounds.Contains(point))
        {
            return null;
        }

        UiRect viewport = ViewportBounds;
        if (!viewport.Contains(point))
        {
            return this;
        }

        UiPoint localPoint = new UiPoint(point.X - Bounds.X + _scrollX, point.Y - Bounds.Y + _scrollY);
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            UiElement? childHit = Children[i].HitTest(localPoint);
            if (childHit != null)
            {
                return childHit;
            }
        }

        return this;
    }

    private void RefreshLayout()
    {
        _contentSize = CalculateContentSize();
        ResolveScrollbars();
        ClampScrollOffset();
    }

    private UiPoint CalculateContentSize()
    {
        int maxRight = 0;
        int maxBottom = 0;

        foreach (UiElement child in Children)
        {
            if (!child.Visible)
            {
                continue;
            }

            maxRight = Math.Max(maxRight, child.Bounds.Right);
            maxBottom = Math.Max(maxBottom, child.Bounds.Bottom);
        }

        return new UiPoint(Math.Max(0, maxRight), Math.Max(0, maxBottom));
    }

    private void ResolveScrollbars()
    {
        int width = Math.Max(0, Bounds.Width);
        int height = Math.Max(0, Bounds.Height);
        int thickness = Math.Max(1, ScrollbarThickness);

        bool showH = HorizontalScrollbar == UiScrollbarVisibility.Always;
        bool showV = VerticalScrollbar == UiScrollbarVisibility.Always;
        bool autoH = HorizontalScrollbar == UiScrollbarVisibility.Auto;
        bool autoV = VerticalScrollbar == UiScrollbarVisibility.Auto;

        int viewWidth = width;
        int viewHeight = height;

        if (autoH && _contentSize.X > viewWidth)
        {
            showH = true;
        }

        if (autoV && _contentSize.Y > viewHeight)
        {
            showV = true;
        }

        if (showV)
        {
            viewWidth = Math.Max(0, viewWidth - thickness);
        }

        if (showH)
        {
            viewHeight = Math.Max(0, viewHeight - thickness);
        }

        if (autoH && !showH && _contentSize.X > viewWidth)
        {
            showH = true;
            viewHeight = Math.Max(0, height - thickness);
        }

        if (autoV && !showV && _contentSize.Y > viewHeight)
        {
            showV = true;
            viewWidth = Math.Max(0, width - thickness);
        }

        _showHorizontal = showH;
        _showVertical = showV;
        _viewportSize = new UiPoint(viewWidth, viewHeight);
    }

    private void ClampScrollOffset()
    {
        int maxX = Math.Max(0, _contentSize.X - _viewportSize.X);
        int maxY = Math.Max(0, _contentSize.Y - _viewportSize.Y);

        _scrollX = Math.Clamp(_scrollX, 0, maxX);
        _scrollY = Math.Clamp(_scrollY, 0, maxY);
    }

    private UiRect GetVerticalScrollbarBounds()
    {
        int thickness = Math.Max(1, ScrollbarThickness);
        return new UiRect(Bounds.Right - thickness, Bounds.Y, thickness, _viewportSize.Y);
    }

    private UiRect GetHorizontalScrollbarBounds()
    {
        int thickness = Math.Max(1, ScrollbarThickness);
        return new UiRect(Bounds.X, Bounds.Bottom - thickness, _viewportSize.X, thickness);
    }

    private UiRect GetVerticalThumbBounds(UiRect bar)
    {
        int padding = Math.Max(0, ScrollbarPadding);
        int trackHeight = Math.Max(0, bar.Height - padding * 2);
        int trackTop = bar.Y + padding;
        int scrollRange = Math.Max(0, _contentSize.Y - _viewportSize.Y);

        int thumbHeight = trackHeight;
        if (scrollRange > 0)
        {
            float ratio = _viewportSize.Y / (float)_contentSize.Y;
            thumbHeight = Math.Max(MinThumbSize, (int)Math.Round(trackHeight * ratio));
        }

        int travel = Math.Max(0, trackHeight - thumbHeight);
        int thumbY = trackTop;
        if (scrollRange > 0 && travel > 0)
        {
            float t = _scrollY / (float)scrollRange;
            thumbY = trackTop + (int)Math.Round(travel * t);
        }

        return new UiRect(bar.X + padding, thumbY, Math.Max(1, bar.Width - padding * 2), thumbHeight);
    }

    private UiRect GetHorizontalThumbBounds(UiRect bar)
    {
        int padding = Math.Max(0, ScrollbarPadding);
        int trackWidth = Math.Max(0, bar.Width - padding * 2);
        int trackLeft = bar.X + padding;
        int scrollRange = Math.Max(0, _contentSize.X - _viewportSize.X);

        int thumbWidth = trackWidth;
        if (scrollRange > 0)
        {
            float ratio = _viewportSize.X / (float)_contentSize.X;
            thumbWidth = Math.Max(MinThumbSize, (int)Math.Round(trackWidth * ratio));
        }

        int travel = Math.Max(0, trackWidth - thumbWidth);
        int thumbX = trackLeft;
        if (scrollRange > 0 && travel > 0)
        {
            float t = _scrollX / (float)scrollRange;
            thumbX = trackLeft + (int)Math.Round(travel * t);
        }

        return new UiRect(thumbX, bar.Y + padding, thumbWidth, Math.Max(1, bar.Height - padding * 2));
    }

    private void UpdateVerticalScrollbar(UiInputState input, UiRect bar)
    {
        UiRect thumb = GetVerticalThumbBounds(bar);
        _hoverVerticalThumb = thumb.Contains(input.MousePosition);

        if (!_draggingVertical && input.LeftClicked && bar.Contains(input.MousePosition))
        {
            if (_hoverVerticalThumb)
            {
                _draggingVertical = true;
                _dragStartMouse = input.MousePosition.Y;
                _dragStartScroll = _scrollY;
            }
            else
            {
                PageVertical(input.MousePosition.Y < thumb.Y);
            }
        }

        if (_draggingVertical && input.LeftDown)
        {
            int trackHeight = Math.Max(1, bar.Height - ScrollbarPadding * 2);
            int scrollRange = Math.Max(0, _contentSize.Y - _viewportSize.Y);
            int thumbHeight = thumb.Height;
            int travel = Math.Max(1, trackHeight - thumbHeight);
            int delta = input.MousePosition.Y - _dragStartMouse;
            float scrollDelta = scrollRange > 0 ? delta / (float)travel * scrollRange : 0f;
            _scrollY = _dragStartScroll + (int)Math.Round(scrollDelta);
        }

        if (_draggingVertical && input.LeftReleased)
        {
            _draggingVertical = false;
        }
    }

    private void UpdateHorizontalScrollbar(UiInputState input, UiRect bar)
    {
        UiRect thumb = GetHorizontalThumbBounds(bar);
        _hoverHorizontalThumb = thumb.Contains(input.MousePosition);

        if (!_draggingHorizontal && input.LeftClicked && bar.Contains(input.MousePosition))
        {
            if (_hoverHorizontalThumb)
            {
                _draggingHorizontal = true;
                _dragStartMouse = input.MousePosition.X;
                _dragStartScroll = _scrollX;
            }
            else
            {
                PageHorizontal(input.MousePosition.X < thumb.X);
            }
        }

        if (_draggingHorizontal && input.LeftDown)
        {
            int trackWidth = Math.Max(1, bar.Width - ScrollbarPadding * 2);
            int scrollRange = Math.Max(0, _contentSize.X - _viewportSize.X);
            int thumbWidth = thumb.Width;
            int travel = Math.Max(1, trackWidth - thumbWidth);
            int delta = input.MousePosition.X - _dragStartMouse;
            float scrollDelta = scrollRange > 0 ? delta / (float)travel * scrollRange : 0f;
            _scrollX = _dragStartScroll + (int)Math.Round(scrollDelta);
        }

        if (_draggingHorizontal && input.LeftReleased)
        {
            _draggingHorizontal = false;
        }
    }

    private void PageVertical(bool up)
    {
        if (_viewportSize.Y <= 0)
        {
            return;
        }

        _scrollY += up ? -_viewportSize.Y : _viewportSize.Y;
    }

    private void PageHorizontal(bool left)
    {
        if (_viewportSize.X <= 0)
        {
            return;
        }

        _scrollX += left ? -_viewportSize.X : _viewportSize.X;
    }

    private void DrawScrollbars(UiRenderContext context)
    {
        if (_showVertical)
        {
            UiRect bar = GetVerticalScrollbarBounds();
            context.Renderer.FillRect(bar, ScrollbarTrack);
            context.Renderer.DrawRect(bar, ScrollbarBorder, 1);

            UiRect thumb = GetVerticalThumbBounds(bar);
            UiColor thumbColor = (_hoverVerticalThumb || _draggingVertical) ? ScrollbarThumbHover : ScrollbarThumb;
            context.Renderer.FillRect(thumb, thumbColor);
        }

        if (_showHorizontal)
        {
            UiRect bar = GetHorizontalScrollbarBounds();
            context.Renderer.FillRect(bar, ScrollbarTrack);
            context.Renderer.DrawRect(bar, ScrollbarBorder, 1);

            UiRect thumb = GetHorizontalThumbBounds(bar);
            UiColor thumbColor = (_hoverHorizontalThumb || _draggingHorizontal) ? ScrollbarThumbHover : ScrollbarThumb;
            context.Renderer.FillRect(thumb, thumbColor);
        }

        if (_showVertical && _showHorizontal)
        {
            int thickness = Math.Max(1, ScrollbarThickness);
            UiRect corner = new UiRect(Bounds.Right - thickness, Bounds.Bottom - thickness, thickness, thickness);
            context.Renderer.FillRect(corner, ScrollbarTrack);
            context.Renderer.DrawRect(corner, ScrollbarBorder, 1);
        }
    }

    private UiInputState BuildChildInput(UiInputState input, bool useViewportMouse)
    {
        UiPoint mouse = useViewportMouse
            ? new UiPoint(input.MousePosition.X - Bounds.X + _scrollX, input.MousePosition.Y - Bounds.Y + _scrollY)
            : new UiPoint(int.MinValue / 4, int.MinValue / 4);

        return new UiInputState
        {
            MousePosition = mouse,
            ScreenMousePosition = input.ScreenMousePosition,
            LeftDown = input.LeftDown,
            LeftClicked = input.LeftClicked,
            LeftReleased = input.LeftReleased,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            ScrollDelta = input.ScrollDelta,
            TextInput = input.TextInput,
            Navigation = input.Navigation
        };
    }
}
