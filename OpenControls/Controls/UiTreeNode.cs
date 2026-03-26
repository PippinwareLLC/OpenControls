namespace OpenControls.Controls;

public sealed class UiTreeNode : UiElement
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

    private bool _hovered;
    private bool _pressed;
    private bool _focused;
    private bool _isOpen;

    public string Text { get; set; } = string.Empty;
    public int TextScale { get; set; } = 1;
    public bool TextBold { get; set; }
    public int Padding { get; set; } = 4;
    public int ArrowSize { get; set; } = 6;
    public int ArrowPadding { get; set; } = 4;
    public int Indent { get; set; } = 14;
    public int HeaderHeight { get; set; }
    public int ContentPadding { get; set; }
    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor ArrowColor { get; set; } = UiColor.White;

    public bool IsOpen
    {
        get => _isOpen;
        set => SetOpen(value);
    }

    public event Action<bool>? Toggled;

    public override bool IsFocusable => true;

    public UiRect HeaderBounds => new UiRect(Bounds.X, Bounds.Y, Bounds.Width, GetHeaderHeight());

    public UiRect ContentBounds
    {
        get
        {
            int headerHeight = GetHeaderHeight();
            int width = Math.Max(0, Bounds.Width - Indent);
            int padding = Math.Max(0, ContentPadding);
            int height = Math.Max(0, Bounds.Height - headerHeight - padding);
            return new UiRect(Bounds.X + Indent, Bounds.Y + headerHeight + padding, width, height);
        }
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        UiRect header = HeaderBounds;
        _hovered = header.Contains(input.MousePosition);

        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
            context.Focus.RequestFocus(this);
        }

        if (_focused && input.Navigation.Activate)
        {
            SetOpen(!_isOpen);
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                SetOpen(!_isOpen);
            }

            _pressed = false;
        }

        if (_isOpen)
        {
            UiInputState childInput = BuildChildInput(input);
            UiUpdateContext childContext = new UiUpdateContext(childInput, context.Focus, context.DragDrop, context.DeltaSeconds, context.DefaultFont, context.Clipboard);
            foreach (UiElement child in Children)
            {
                child.Update(childContext);
            }
        }
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRect header = HeaderBounds;
        UiColor fill = _hovered || _pressed ? HoverBackground : Background;
        if (fill.A > 0)
        {
            context.Renderer.FillRect(header, fill);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            context.Renderer.DrawRect(header, Border, BorderThickness);
        }

        DrawArrow(context, header);

        UiFont font = ResolveFont(context.DefaultFont);
        int textHeight = context.Renderer.MeasureTextHeight(TextScale, font);
        int textY = header.Y + (header.Height - textHeight) / 2;
        int textX = header.X + Padding + Math.Max(4, ArrowSize) + ArrowPadding;
        UiPoint textPoint = new UiPoint(textX, textY);
        if (TextBold)
        {
            UiRenderHelpers.DrawTextBold(context.Renderer, Text, textPoint, TextColor, TextScale, font);
        }
        else
        {
            context.Renderer.DrawText(Text, textPoint, TextColor, TextScale, font);
        }

        if (_isOpen)
        {
            RenderChildren(context);
        }
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible || !_isOpen)
        {
            return;
        }

        RenderChildrenOverlay(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _pressed = false;
    }

    private void RenderChildren(UiRenderContext context)
    {
        UiRect content = ContentBounds;
        UiPoint offset = new UiPoint(content.X, content.Y);
        OffsetRenderer offsetRenderer = new OffsetRenderer(context.Renderer, offset);
        UiRenderContext childContext = new UiRenderContext(offsetRenderer, context.DefaultFont);

        if (ClipChildren)
        {
            context.Renderer.PushClip(content);
        }

        foreach (UiElement child in Children)
        {
            child.Render(childContext);
        }

        if (ClipChildren)
        {
            context.Renderer.PopClip();
        }
    }

    private void RenderChildrenOverlay(UiRenderContext context)
    {
        UiRect content = ContentBounds;
        UiPoint offset = new UiPoint(content.X, content.Y);
        OffsetRenderer offsetRenderer = new OffsetRenderer(context.Renderer, offset);
        UiRenderContext childContext = new UiRenderContext(offsetRenderer, context.DefaultFont);

        if (ClipChildren)
        {
            context.Renderer.PushClip(content);
        }

        foreach (UiElement child in Children)
        {
            child.RenderOverlay(childContext);
        }

        if (ClipChildren)
        {
            context.Renderer.PopClip();
        }
    }

    private int GetHeaderHeight()
    {
        int height = HeaderHeight > 0 ? HeaderHeight : Bounds.Height;
        return Math.Max(1, height);
    }

    private UiInputState BuildChildInput(UiInputState input)
    {
        UiRect content = ContentBounds;
        UiPoint mouse = new UiPoint(input.MousePosition.X - content.X, input.MousePosition.Y - content.Y);
        UiPoint? leftDragOrigin = TranslatePoint(input.LeftDragOrigin, content.X, content.Y);
        UiPoint? rightDragOrigin = TranslatePoint(input.RightDragOrigin, content.X, content.Y);
        UiPoint? middleDragOrigin = TranslatePoint(input.MiddleDragOrigin, content.X, content.Y);

        return new UiInputState
        {
            MousePosition = mouse,
            ScreenMousePosition = input.ScreenMousePosition,
            LeftDown = input.LeftDown,
            LeftClicked = input.LeftClicked,
            LeftDoubleClicked = input.LeftDoubleClicked,
            LeftReleased = input.LeftReleased,
            RightDown = input.RightDown,
            RightClicked = input.RightClicked,
            RightDoubleClicked = input.RightDoubleClicked,
            RightReleased = input.RightReleased,
            MiddleDown = input.MiddleDown,
            MiddleClicked = input.MiddleClicked,
            MiddleDoubleClicked = input.MiddleDoubleClicked,
            MiddleReleased = input.MiddleReleased,
            LeftDragOrigin = leftDragOrigin,
            RightDragOrigin = rightDragOrigin,
            MiddleDragOrigin = middleDragOrigin,
            DragThreshold = input.DragThreshold,
            ShiftDown = input.ShiftDown,
            CtrlDown = input.CtrlDown,
            AltDown = input.AltDown,
            SuperDown = input.SuperDown,
            ScrollDeltaX = input.ScrollDeltaX,
            ScrollDelta = input.ScrollDelta,
            TextInput = input.TextInput,
            KeysDown = input.KeysDown,
            KeysPressed = input.KeysPressed,
            KeysReleased = input.KeysReleased,
            Navigation = input.Navigation
        };
    }

    private static UiPoint? TranslatePoint(UiPoint? point, int offsetX, int offsetY)
    {
        return point is UiPoint value
            ? new UiPoint(value.X - offsetX, value.Y - offsetY)
            : null;
    }

    private void DrawArrow(UiRenderContext context, UiRect header)
    {
        int size = Math.Max(4, ArrowSize);
        int x = header.X + Padding;
        int y = header.Y + (header.Height - size) / 2;
        UiArrowDirection direction = _isOpen ? UiArrowDirection.Down : UiArrowDirection.Right;
        UiArrow.DrawTriangle(context.Renderer, x, y, size, direction, ArrowColor);
    }

    private void SetOpen(bool value)
    {
        if (_isOpen == value)
        {
            return;
        }

        _isOpen = value;
        Toggled?.Invoke(_isOpen);
    }
}
