namespace OpenControls.Controls;

public sealed class UiNodeControl : UiElement
{
    private readonly List<UiNodePin> _pins = new();
    private UiNodeDebugLayout _debugLayout = UiNodeDebugLayout.Empty;
    private bool _layoutValid;
    private UiRect _layoutBounds;
    private string _layoutTitle = string.Empty;
    private string _layoutSubtitle = string.Empty;
    private string _layoutBodyText = string.Empty;
    private string _layoutFontName = string.Empty;
    private int _layoutFontPixelSize;
    private int _layoutHeaderHeight;
    private int _layoutPinRowHeight;
    private int _layoutPinHitSize;
    private int _layoutPinVisualSize;
    private int _layoutPadding;
    private int _layoutTextScale;
    private int _layoutPinHash;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _bodyText = string.Empty;
    private bool _hovered;
    private bool _selected;
    private bool _pressed;
    private bool _dragging;
    private bool _clickedThisFrame;
    private UiPoint _dragStartMouse;
    private UiRect _dragStartBounds;
    private int _headerHeight = 32;
    private int _pinRowHeight = 22;
    private int _pinHitSize = 14;
    private int _pinVisualSize = 8;
    private int _padding = 8;
    private int _textScale = 1;
    private int _cornerRadius = 6;

    public string Title
    {
        get => _title;
        set => SetInvalidatingValue(ref _title, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Paint | UiInvalidationReason.Layout);
    }

    public string BodyText
    {
        get => _bodyText;
        set => SetInvalidatingValue(ref _bodyText, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Paint | UiInvalidationReason.Layout);
    }

    public string Subtitle
    {
        get => _subtitle;
        set => SetInvalidatingValue(ref _subtitle, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Paint | UiInvalidationReason.Layout);
    }

    public IList<UiNodePin> Pins => _pins;
    public UiNodeDebugLayout DebugLayout => _debugLayout;
    public UiNodePin? HoveredPin { get; private set; }
    public bool Hovered => _hovered;
    public bool IsDragging => _dragging;
    public bool Draggable { get; set; } = true;
    public bool DragFromHeaderOnly { get; set; } = true;
    public UiColor Background { get; set; } = new(34, 38, 48);
    public UiColor HeaderBackground { get; set; } = new(48, 56, 72);
    public UiColor ShadowColor { get; set; } = new(0, 0, 0, 130);
    public UiColor HoverBorder { get; set; } = new(120, 150, 210);
    public UiColor SelectedBorder { get; set; } = new(225, 175, 80);
    public UiColor Border { get; set; } = new(82, 92, 112);
    public UiColor TitleColor { get; set; } = UiColor.White;
    public UiColor SubtitleColor { get; set; } = new(172, 186, 205);
    public UiColor BodyTextColor { get; set; } = new(190, 200, 216);
    public UiColor DataPinColor { get; set; } = new(95, 170, 230);
    public UiColor ExecPinColor { get; set; } = UiColor.White;
    public UiColor PinBorder { get; set; } = new(18, 22, 30);
    public UiColor PinHoverBorder { get; set; } = UiColor.White;

    public int HeaderHeight
    {
        get => _headerHeight;
        set => SetInvalidatingValue(ref _headerHeight, Math.Max(30, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int PinRowHeight
    {
        get => _pinRowHeight;
        set => SetInvalidatingValue(ref _pinRowHeight, Math.Max(12, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int PinHitSize
    {
        get => _pinHitSize;
        set => SetInvalidatingValue(ref _pinHitSize, Math.Max(4, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int PinVisualSize
    {
        get => _pinVisualSize;
        set => SetInvalidatingValue(ref _pinVisualSize, Math.Max(2, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int Padding
    {
        get => _padding;
        set => SetInvalidatingValue(ref _padding, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int TextScale
    {
        get => _textScale;
        set => SetInvalidatingValue(ref _textScale, Math.Max(1, value), UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set => SetInvalidatingValue(ref _cornerRadius, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.Clip);
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            if (SetInvalidatingValue(ref _selected, value, UiInvalidationReason.State | UiInvalidationReason.Paint))
            {
                SelectionChanged?.Invoke(this);
            }
        }
    }

    public event Action<UiNodeControl>? SelectionChanged;
    public event Action<UiNodeControl>? DragStarted;
    public event Action<UiNodeControl>? Dragged;
    public event Action<UiNodeControl>? DragEnded;

    public override bool IsFocusable => true;

    public UiNodePin AddPin(string id, string text, UiNodePinDirection direction, UiNodePinKind kind = UiNodePinKind.Data)
    {
        UiNodePin pin = new(id, text, direction, kind);
        _pins.Add(pin);
        Invalidate(UiInvalidationReason.Children | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
        return pin;
    }

    public UiNodePin AddInput(string id, string text, UiNodePinKind kind = UiNodePinKind.Data)
    {
        return AddPin(id, text, UiNodePinDirection.Input, kind);
    }

    public UiNodePin AddOutput(string id, string text, UiNodePinKind kind = UiNodePinKind.Data)
    {
        return AddPin(id, text, UiNodePinDirection.Output, kind);
    }

    public bool TryGetPin(string id, out UiNodePin? pin)
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin candidate = _pins[i];
            if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
            {
                pin = candidate;
                return true;
            }
        }

        pin = null;
        return false;
    }

    public bool TryGetPinAt(UiPoint point, out UiNodePin? pin)
    {
        for (int i = _pins.Count - 1; i >= 0; i--)
        {
            UiNodePin candidate = _pins[i];
            if (candidate.Enabled && candidate.Layout.HitBounds.Contains(point))
            {
                pin = candidate;
                return true;
            }
        }

        pin = null;
        return false;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _clickedThisFrame = false;
        UiFont font = ResolveFont(context.DefaultFont);
        UpdateLayout(font);

        UiInputState input = context.Input;
        UiPoint mouse = input.MousePosition;
        HoveredPin = null;
        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin pin = _pins[i];
            bool pinHovered = pin.Enabled && pin.Layout.HitBounds.Contains(mouse);
            pin.Hovered = pinHovered;
            if (pinHovered)
            {
                HoveredPin = pin;
            }
        }

        _hovered = Bounds.Contains(mouse) || HoveredPin != null;

        if (input.LeftClicked && _hovered)
        {
            context.Focus.RequestFocus(this);
            Selected = true;
            _clickedThisFrame = true;

            if (Draggable && HoveredPin == null && CanStartDrag(mouse))
            {
                _pressed = true;
                _dragStartMouse = mouse;
                _dragStartBounds = Bounds;
            }
        }

        if (_pressed && input.LeftDown && Draggable)
        {
            int dx = mouse.X - _dragStartMouse.X;
            int dy = mouse.Y - _dragStartMouse.Y;
            if (!_dragging && HasExceededDragThreshold(dx, dy, input.DragThreshold))
            {
                _dragging = true;
                DragStarted?.Invoke(this);
            }

            if (_dragging)
            {
                Bounds = new UiRect(_dragStartBounds.X + dx, _dragStartBounds.Y + dy, _dragStartBounds.Width, _dragStartBounds.Height);
                UpdateLayout(font);
                Dragged?.Invoke(this);
            }
        }

        if (input.LeftReleased)
        {
            if (_dragging)
            {
                DragEnded?.Invoke(this);
            }

            _pressed = false;
            _dragging = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiFont font = ResolveFont(context.DefaultFont);
        UpdateLayout(font);

        if (ShadowColor.A > 0)
        {
            UiRenderHelpers.FillRectRounded(
                context.Renderer,
                new UiRect(Bounds.X + 3, Bounds.Y + 4, Bounds.Width, Bounds.Height),
                CornerRadius,
                ShadowColor);
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        UiRenderHelpers.FillRectTopRounded(context.Renderer, _debugLayout.HeaderBounds, CornerRadius, HeaderBackground);
        UiColor border = Selected ? SelectedBorder : (_hovered ? HoverBorder : Border);
        UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, border, Selected ? 2 : 1);

        DrawTitle(context, font);
        DrawSubtitle(context, font);
        DrawBodyText(context, font);
        DrawPins(context, font);

        base.Render(context);
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible)
        {
            return null;
        }

        bool nodeHit = Bounds.Contains(point) || TryGetPinAt(point, out _);
        if (!nodeHit)
        {
            return null;
        }

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            UiElement? childHit = Children[i].HitTest(point);
            if (childHit != null)
            {
                return childHit;
            }
        }

        return this;
    }

    protected internal override void OnFocusLost()
    {
        bool changed = _pressed || _dragging;
        _pressed = false;
        _dragging = false;
        if (changed)
        {
            Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Volatility);
        }
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered || _hovered);
        if (_pressed)
        {
            status |= UiItemStatusFlags.Pressed | UiItemStatusFlags.Active;
        }

        if (_dragging)
        {
            status |= UiItemStatusFlags.Dragging | UiItemStatusFlags.Active;
        }

        if (_clickedThisFrame)
        {
            status |= UiItemStatusFlags.Clicked;
        }

        return status;
    }

    private void UpdateLayout(UiFont font)
    {
        int pinHash = ComputePinLayoutHash();
        if (_layoutValid
            && _layoutBounds.Equals(Bounds)
            && _layoutHeaderHeight == HeaderHeight
            && _layoutPinRowHeight == PinRowHeight
            && _layoutPinHitSize == PinHitSize
            && _layoutPinVisualSize == PinVisualSize
            && _layoutPadding == Padding
            && _layoutTextScale == TextScale
            && _layoutPinHash == pinHash
            && _layoutFontPixelSize == font.PixelSize
            && string.Equals(_layoutFontName, font.Name, StringComparison.Ordinal)
            && string.Equals(_layoutTitle, Title, StringComparison.Ordinal)
            && string.Equals(_layoutSubtitle, Subtitle, StringComparison.Ordinal)
            && string.Equals(_layoutBodyText, BodyText, StringComparison.Ordinal))
        {
            return;
        }

        int headerHeight = Math.Min(Math.Max(0, Bounds.Height), Math.Max(30, HeaderHeight));
        UiRect headerBounds = new(Bounds.X, Bounds.Y, Bounds.Width, headerHeight);
        UiRect bodyBounds = new(Bounds.X, Bounds.Y + headerHeight, Bounds.Width, Math.Max(0, Bounds.Height - headerHeight));
        int padding = Math.Max(0, Padding);
        int scale = Math.Max(1, TextScale);
        int textHeight = font.MeasureTextHeight(scale);
        int titleAvailableWidth = Math.Max(0, headerBounds.Width - padding * 2);
        int titleWidth = Math.Min(titleAvailableWidth, font.MeasureTextWidth(Title, scale));
        bool hasSubtitle = !string.IsNullOrWhiteSpace(Subtitle);
        int titleY = hasSubtitle
            ? headerBounds.Y + Math.Max(4, padding / 2)
            : headerBounds.Y + Math.Max(0, (headerBounds.Height - textHeight) / 2);
        UiRect titleBounds = new(headerBounds.X + padding, titleY, titleWidth, textHeight);
        UiRect subtitleBounds = default;
        if (hasSubtitle)
        {
            int subtitleY = titleBounds.Bottom + 3;
            int subtitleAvailableWidth = Math.Max(0, headerBounds.Width - padding * 2);
            if (subtitleY + textHeight <= headerBounds.Bottom - Math.Max(2, padding / 3))
            {
                int subtitleWidth = Math.Min(subtitleAvailableWidth, font.MeasureTextWidth(Subtitle, scale));
                subtitleBounds = new(headerBounds.X + padding, subtitleY, subtitleWidth, textHeight);
            }
        }

        List<UiNodePinLayout> layouts = new(_pins.Count);
        int inputRow = 0;
        int outputRow = 0;
        int inputCount = 0;
        int outputCount = 0;
        for (int i = 0; i < _pins.Count; i++)
        {
            if (_pins[i].Direction == UiNodePinDirection.Input)
            {
                inputCount++;
            }
            else
            {
                outputCount++;
            }
        }

        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin pin = _pins[i];
            int rowIndex = pin.Direction == UiNodePinDirection.Input ? inputRow++ : outputRow++;
            bool hasOppositePinInRow = pin.Direction == UiNodePinDirection.Input
                ? outputCount > rowIndex
                : inputCount > rowIndex;
            UiNodePinLayout layout = BuildPinLayout(pin, rowIndex, bodyBounds, font, textHeight, hasOppositePinInRow);
            pin.Layout = layout;
            layouts.Add(layout);
        }

        int rowCount = Math.Max(inputCount, outputCount);
        UiRect bodyTextBounds = default;
        if (!string.IsNullOrEmpty(BodyText) && bodyBounds.Width > 0 && bodyBounds.Height > 0)
        {
            int bodyTextY = bodyBounds.Y + padding + rowCount * Math.Max(12, PinRowHeight) + padding;
            if (bodyTextY + textHeight <= bodyBounds.Bottom - padding)
            {
                int bodyTextAvailableWidth = Math.Max(0, bodyBounds.Width - padding * 2);
                int bodyTextWidth = Math.Min(bodyTextAvailableWidth, font.MeasureTextWidth(BodyText, scale));
                bodyTextBounds = new(bodyBounds.X + padding, bodyTextY, bodyTextWidth, textHeight);
            }
        }

        _debugLayout = new UiNodeDebugLayout(Bounds, headerBounds, bodyBounds, titleBounds, subtitleBounds, bodyTextBounds, layouts.ToArray());
        _layoutValid = true;
        _layoutBounds = Bounds;
        _layoutTitle = Title;
        _layoutSubtitle = Subtitle;
        _layoutBodyText = BodyText;
        _layoutFontName = font.Name;
        _layoutFontPixelSize = font.PixelSize;
        _layoutHeaderHeight = HeaderHeight;
        _layoutPinRowHeight = PinRowHeight;
        _layoutPinHitSize = PinHitSize;
        _layoutPinVisualSize = PinVisualSize;
        _layoutPadding = Padding;
        _layoutTextScale = TextScale;
        _layoutPinHash = pinHash;
    }

    private int ComputePinLayoutHash()
    {
        HashCode hash = new();
        hash.Add(_pins.Count);
        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin pin = _pins[i];
            hash.Add(pin.Id, StringComparer.Ordinal);
            hash.Add(pin.Text, StringComparer.Ordinal);
            hash.Add(pin.Direction);
            hash.Add(pin.Kind);
            hash.Add(pin.Enabled);
        }

        return hash.ToHashCode();
    }

    private UiNodePinLayout BuildPinLayout(UiNodePin pin, int rowIndex, UiRect bodyBounds, UiFont font, int textHeight, bool hasOppositePinInRow)
    {
        int rowHeight = Math.Max(12, PinRowHeight);
        int padding = Math.Max(0, Padding);
        int centerY = bodyBounds.Y + padding + rowHeight / 2 + rowIndex * rowHeight;
        int centerX = pin.Direction == UiNodePinDirection.Input ? Bounds.X : Bounds.Right;
        UiPoint center = new(centerX, centerY);
        int hitSize = Math.Max(4, PinHitSize);
        UiRect hitBounds = new(center.X - hitSize / 2, center.Y - hitSize / 2, hitSize, hitSize);
        UiRect rowBounds = new(Bounds.X, center.Y - rowHeight / 2, Bounds.Width, rowHeight);

        int labelWidth = font.MeasureTextWidth(pin.Text, TextScale);
        int labelY = center.Y - textHeight / 2;
        int sidePadding = Math.Max(PinVisualSize, PinHitSize) + Math.Max(4, padding / 2);
        int laneGap = Math.Max(6, padding / 2);
        int sharedLabelWidth = Math.Max(0, Bounds.Width - sidePadding * 2);
        int maxLabelWidth = hasOppositePinInRow
            ? Math.Max(0, (sharedLabelWidth - laneGap) / 2)
            : sharedLabelWidth;
        int clampedLabelWidth = Math.Min(maxLabelWidth, labelWidth);
        int labelX = pin.Direction == UiNodePinDirection.Input
            ? Bounds.X + sidePadding
            : Bounds.Right - sidePadding - clampedLabelWidth;
        UiRect labelBounds = new(labelX, labelY, clampedLabelWidth, textHeight);

        return new UiNodePinLayout(pin, rowBounds, labelBounds, hitBounds, center);
    }

    private void DrawTitle(UiRenderContext context, UiFont font)
    {
        if (_debugLayout.TitleBounds.Width <= 0 || _debugLayout.TitleBounds.Height <= 0)
        {
            return;
        }

        int availableWidth = Math.Max(0, _debugLayout.HeaderBounds.Width - Padding * 2);
        string drawText = UiRenderHelpers.BuildElidedText(Title, availableWidth, TextScale, font);
        context.Renderer.PushClip(_debugLayout.HeaderBounds);
        context.Renderer.DrawText(drawText, new UiPoint(_debugLayout.TitleBounds.X, _debugLayout.TitleBounds.Y), TitleColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawBodyText(UiRenderContext context, UiFont font)
    {
        if (string.IsNullOrEmpty(BodyText) || _debugLayout.BodyTextBounds.Width <= 0 || _debugLayout.BodyTextBounds.Height <= 0)
        {
            return;
        }

        int availableWidth = Math.Max(0, _debugLayout.BodyBounds.Width - Padding * 2);
        string drawText = UiRenderHelpers.BuildElidedText(BodyText, availableWidth, TextScale, font);
        context.Renderer.PushClip(_debugLayout.BodyBounds);
        context.Renderer.DrawText(drawText, new UiPoint(_debugLayout.BodyTextBounds.X, _debugLayout.BodyTextBounds.Y), BodyTextColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawSubtitle(UiRenderContext context, UiFont font)
    {
        if (string.IsNullOrEmpty(Subtitle) || _debugLayout.SubtitleBounds.Width <= 0 || _debugLayout.SubtitleBounds.Height <= 0)
        {
            return;
        }

        int availableWidth = Math.Max(0, _debugLayout.HeaderBounds.Width - Padding * 2);
        string drawText = UiRenderHelpers.BuildElidedText(Subtitle, availableWidth, TextScale, font);
        context.Renderer.PushClip(_debugLayout.HeaderBounds);
        context.Renderer.DrawText(drawText, new UiPoint(_debugLayout.SubtitleBounds.X, _debugLayout.SubtitleBounds.Y), SubtitleColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawPins(UiRenderContext context, UiFont font)
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin pin = _pins[i];
            UiNodePinLayout layout = pin.Layout;
            UiColor pinColor = ResolvePinColor(pin);
            UiColor border = pin.Hovered || pin.Selected ? PinHoverBorder : PinBorder;
            int visualSize = Math.Max(2, PinVisualSize + (pin.Kind == UiNodePinKind.Exec ? 2 : 0));
            UiRect visual = new(layout.Center.X - visualSize / 2, layout.Center.Y - visualSize / 2, visualSize, visualSize);

            if (pin.Kind == UiNodePinKind.Exec)
            {
                UiRenderHelpers.FillTriangleRight(context.Renderer, visual, pinColor);
            }
            else
            {
                int radius = Math.Max(2, visualSize / 2);
                UiRenderHelpers.FillCircle(context.Renderer, layout.Center, radius, pinColor);
                UiRenderHelpers.DrawCircle(context.Renderer, layout.Center, radius, border, 1);
            }

            if (layout.LabelBounds.Width > 0 && layout.LabelBounds.Height > 0)
            {
                string drawText = UiRenderHelpers.BuildElidedText(pin.Text, layout.LabelBounds.Width, TextScale, font);
                context.Renderer.DrawText(drawText, new UiPoint(layout.LabelBounds.X, layout.LabelBounds.Y), BodyTextColor, TextScale, font);
            }
        }
    }

    private UiColor ResolvePinColor(UiNodePin pin)
    {
        return pin.Kind == UiNodePinKind.Exec
            ? ExecPinColor
            : pin.Color ?? DataPinColor;
    }

    private bool CanStartDrag(UiPoint mouse)
    {
        return !DragFromHeaderOnly || _debugLayout.HeaderBounds.Contains(mouse);
    }

    private static bool HasExceededDragThreshold(int dx, int dy, int threshold)
    {
        int safeThreshold = Math.Max(0, threshold);
        return dx * dx + dy * dy >= safeThreshold * safeThreshold;
    }
}
