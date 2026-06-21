namespace OpenControls.Controls;

public sealed class UiNodeControl : UiElement
{
    private readonly List<UiNodePin> _pins = new();
    private UiNodeDebugLayout _debugLayout = UiNodeDebugLayout.Empty;
    private bool _layoutValid;
    private UiRect _layoutBounds;
    private string _layoutTitle = string.Empty;
    private string _layoutIcon = string.Empty;
    private string _layoutSubtitle = string.Empty;
    private string _layoutBodyText = string.Empty;
    private string _layoutFontName = string.Empty;
    private int _layoutFontPixelSize;
    private int _layoutHeaderHeight;
    private int _layoutPinRowHeight;
    private int _layoutPinHitSize;
    private int _layoutPinVisualSize;
    private int _layoutPadding;
    private int _layoutIconColumnWidth;
    private int _layoutValueBoxMinWidth;
    private int _layoutValueBoxMaxWidth;
    private int _layoutValueBoxPadding;
    private int _layoutTextScale;
    private int _layoutPinHash;
    private bool _layoutCompact;
    private string _title = string.Empty;
    private string _icon = string.Empty;
    private string _subtitle = string.Empty;
    private string _bodyText = string.Empty;
    private bool _hovered;
    private bool _selected;
    private bool _compact;
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
    private int _iconColumnWidth = 24;
    private int _valueBoxMinWidth = 44;
    private int _valueBoxMaxWidth = 120;
    private int _valueBoxPadding = 6;
    private int _textScale = 1;
    private int _cornerRadius = 6;
    private int _minimumContentWidth = 140;
    private int _maximumContentWidth = 460;
    private int _minimumContentHeight = 72;

    public string Title
    {
        get => _title;
        set => SetInvalidatingValue(ref _title, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Paint | UiInvalidationReason.Layout);
    }

    public string Icon
    {
        get => _icon;
        set => SetInvalidatingValue(ref _icon, value ?? string.Empty, UiInvalidationReason.Text | UiInvalidationReason.Paint | UiInvalidationReason.Layout);
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
    public bool Compact
    {
        get => _compact;
        set => SetInvalidatingValue(ref _compact, value, UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public UiColor Background { get; set; } = new(34, 38, 48);
    public UiColor HeaderBackground { get; set; } = new(48, 56, 72);
    public UiColor ShadowColor { get; set; } = new(0, 0, 0, 130);
    public UiColor HoverBorder { get; set; } = new(120, 150, 210);
    public UiColor SelectedBorder { get; set; } = new(225, 175, 80);
    public UiColor Border { get; set; } = new(82, 92, 112);
    public bool EnableGlow { get; set; } = true;
    public UiColor SelectedGlowColor { get; set; } = new(255, 206, 92, 94);
    public UiColor HoverGlowColor { get; set; } = new(92, 184, 255, 64);
    public int GlowRadius { get; set; } = 8;
    public int GlowPasses { get; set; } = 3;
    public UiColor TitleColor { get; set; } = UiColor.White;
    public UiColor IconColor { get; set; } = new(232, 242, 255);
    public UiColor SubtitleColor { get; set; } = new(172, 186, 205);
    public UiColor BodyTextColor { get; set; } = new(190, 200, 216);
    public UiColor ValueBoxBackground { get; set; } = new(19, 23, 30, 235);
    public UiColor ValueBoxBorder { get; set; } = new(90, 100, 118);
    public UiColor ValueBoxTextColor { get; set; } = new(226, 234, 246);
    public UiColor DataPinColor { get; set; } = new(95, 170, 230);
    public UiColor ExecPinColor { get; set; } = UiColor.White;
    public UiColor PinBorder { get; set; } = new(18, 22, 30);
    public UiColor PinHoverBorder { get; set; } = UiColor.White;

    public int HeaderHeight
    {
        get => _headerHeight;
        set => SetInvalidatingValue(ref _headerHeight, Math.Max(20, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
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

    public int IconColumnWidth
    {
        get => _iconColumnWidth;
        set => SetInvalidatingValue(ref _iconColumnWidth, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int ValueBoxMinWidth
    {
        get => _valueBoxMinWidth;
        set => SetInvalidatingValue(ref _valueBoxMinWidth, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int ValueBoxMaxWidth
    {
        get => _valueBoxMaxWidth;
        set => SetInvalidatingValue(ref _valueBoxMaxWidth, Math.Max(ValueBoxMinWidth, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int ValueBoxPadding
    {
        get => _valueBoxPadding;
        set => SetInvalidatingValue(ref _valueBoxPadding, Math.Max(0, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
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

    public int MinimumContentWidth
    {
        get => _minimumContentWidth;
        set => SetInvalidatingValue(ref _minimumContentWidth, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int MaximumContentWidth
    {
        get => _maximumContentWidth;
        set => SetInvalidatingValue(ref _maximumContentWidth, Math.Max(MinimumContentWidth, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public int MinimumContentHeight
    {
        get => _minimumContentHeight;
        set => SetInvalidatingValue(ref _minimumContentHeight, Math.Max(1, value), UiInvalidationReason.Layout | UiInvalidationReason.Paint);
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
            if (candidate.Enabled && candidate.Layout.IsValid && candidate.Layout.HitBounds.Contains(point))
            {
                pin = candidate;
                return true;
            }
        }

        pin = null;
        return false;
    }

    public UiSize MeasureDesiredSize(UiFont font)
    {
        ArgumentNullException.ThrowIfNull(font);

        int scale = Math.Max(1, TextScale);
        int padding = Math.Max(0, Padding);
        int textHeight = font.MeasureTextHeight(scale);
        int sidePadding = ResolvePinSidePadding(padding);
        int laneGap = ResolveLaneGap(padding);
        bool hasSubtitle = !Compact && !string.IsNullOrWhiteSpace(Subtitle);
        bool hasIcon = !string.IsNullOrWhiteSpace(Icon);
        int iconColumnWidth = ResolveIconColumnWidth(textHeight, padding, hasIcon);
        int titleWidth = font.MeasureTextWidth(Title, scale);
        int subtitleWidth = hasSubtitle ? font.MeasureTextWidth(Subtitle, scale) : 0;
        int headerWidth = Math.Max(titleWidth, subtitleWidth) + padding * 2 + iconColumnWidth;

        int inputCount = 0;
        int outputCount = 0;
        for (int i = 0; i < _pins.Count; i++)
        {
            if (!ShouldLayoutPin(_pins[i]))
            {
                continue;
            }

            if (_pins[i].Direction == UiNodePinDirection.Input)
            {
                inputCount++;
            }
            else
            {
                outputCount++;
            }
        }

        int rowCount = Math.Max(inputCount, outputCount);
        int pinWidth = 0;
        for (int row = 0; row < rowCount; row++)
        {
            UiNodePin? inputPin = FindPinAtRow(UiNodePinDirection.Input, row);
            UiNodePin? outputPin = FindPinAtRow(UiNodePinDirection.Output, row);
            int inputWidth = MeasurePinLaneWidth(inputPin, font, scale, padding);
            int outputWidth = MeasurePinLaneWidth(outputPin, font, scale, padding);
            int rowWidth = inputPin is not null && outputPin is not null
                ? sidePadding * 2 + inputWidth + laneGap + outputWidth
                : sidePadding * 2 + Math.Max(inputWidth, outputWidth);
            pinWidth = Math.Max(pinWidth, rowWidth);
        }

        int bodyTextWidth = Compact || string.IsNullOrEmpty(BodyText)
            ? 0
            : font.MeasureTextWidth(BodyText, scale) + padding * 2;
        int maximumWidth = Math.Max(MinimumContentWidth, MaximumContentWidth);
        int desiredWidth = Math.Clamp(Math.Max(headerWidth, Math.Max(pinWidth, bodyTextWidth)), MinimumContentWidth, maximumWidth);

        int measuredHeaderHeight = ResolveHeaderHeight(textHeight, padding, hasSubtitle);
        int rowHeight = Math.Max(Math.Max(12, PinRowHeight), Math.Max(PinHitSize + 6, textHeight + 6));
        int bodyRowsHeight = rowCount == 0 ? 0 : rowCount * rowHeight;
        int bodyTextHeight = Compact || string.IsNullOrEmpty(BodyText) ? 0 : padding + textHeight;
        int bodyPadding = rowCount == 0 && bodyTextHeight == 0 ? Math.Max(4, padding / 2) : padding;
        int desiredHeight = measuredHeaderHeight + bodyPadding + bodyRowsHeight + bodyTextHeight + bodyPadding;
        desiredHeight = Math.Max(MinimumContentHeight, desiredHeight);

        return new UiSize(desiredWidth, desiredHeight);
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
            bool pinHovered = pin.Enabled && pin.Layout.IsValid && pin.Layout.HitBounds.Contains(mouse);
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

        DrawGlow(context.Renderer);

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

        DrawPinChrome(context);
        DrawIcon(context, font);
        DrawTitle(context, font);
        DrawSubtitle(context, font);
        DrawBodyText(context, font);
        DrawPinText(context, font);

        base.Render(context);
    }

    public bool TryGetGlow(out UiRect bounds, out UiColor color, out int passes)
    {
        bounds = default;
        color = default;
        passes = 0;
        if (!EnableGlow || GlowRadius <= 0 || GlowPasses <= 0)
        {
            return false;
        }

        color = Selected ? SelectedGlowColor : (_hovered ? HoverGlowColor : default);
        if (color.A == 0)
        {
            return false;
        }

        bounds = ExpandRect(Bounds, GlowRadius);
        passes = Math.Max(1, GlowPasses);
        return bounds.Width > 0 && bounds.Height > 0;
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
            && _layoutIconColumnWidth == IconColumnWidth
            && _layoutValueBoxMinWidth == ValueBoxMinWidth
            && _layoutValueBoxMaxWidth == ValueBoxMaxWidth
            && _layoutValueBoxPadding == ValueBoxPadding
            && _layoutTextScale == TextScale
            && _layoutPinHash == pinHash
            && _layoutCompact == Compact
            && _layoutFontPixelSize == font.PixelSize
            && string.Equals(_layoutFontName, font.Name, StringComparison.Ordinal)
            && string.Equals(_layoutIcon, Icon, StringComparison.Ordinal)
            && string.Equals(_layoutTitle, Title, StringComparison.Ordinal)
            && string.Equals(_layoutSubtitle, Subtitle, StringComparison.Ordinal)
            && string.Equals(_layoutBodyText, BodyText, StringComparison.Ordinal))
        {
            return;
        }

        int padding = Math.Max(0, Padding);
        int scale = Math.Max(1, TextScale);
        int textHeight = font.MeasureTextHeight(scale);
        bool hasSubtitle = !Compact && !string.IsNullOrWhiteSpace(Subtitle);
        bool hasIcon = !string.IsNullOrWhiteSpace(Icon);
        int iconColumnWidth = ResolveIconColumnWidth(textHeight, padding, hasIcon);
        int headerHeight = Math.Min(Math.Max(0, Bounds.Height), ResolveHeaderHeight(textHeight, padding, hasSubtitle));
        UiRect headerBounds = new(Bounds.X, Bounds.Y, Bounds.Width, headerHeight);
        UiRect bodyBounds = new(Bounds.X, Bounds.Y + headerHeight, Bounds.Width, Math.Max(0, Bounds.Height - headerHeight));
        int textX = headerBounds.X + padding + iconColumnWidth;
        int textRight = headerBounds.Right - padding;
        int titleAvailableWidth = Math.Max(0, textRight - textX);
        int titleWidth = Math.Min(titleAvailableWidth, font.MeasureTextWidth(Title, scale));
        int titleY = hasSubtitle
            ? headerBounds.Y + Math.Max(4, padding / 2)
            : headerBounds.Y + Math.Max(0, (headerBounds.Height - textHeight) / 2);
        UiRect iconBounds = default;
        if (hasIcon && iconColumnWidth > 0 && headerBounds.Width > padding * 2)
        {
            int iconWidth = Math.Min(Math.Max(1, textHeight), iconColumnWidth);
            int iconY = hasSubtitle
                ? titleY
                : headerBounds.Y + Math.Max(0, (headerBounds.Height - textHeight) / 2);
            iconBounds = new(headerBounds.X + padding, iconY, iconWidth, textHeight);
        }

        UiRect titleBounds = new(textX, titleY, titleWidth, textHeight);
        UiRect subtitleBounds = default;
        if (hasSubtitle)
        {
            int subtitleY = titleBounds.Bottom + 3;
            int subtitleAvailableWidth = Math.Max(0, textRight - textX);
            if (subtitleY + textHeight <= headerBounds.Bottom - Math.Max(2, padding / 3))
            {
                int subtitleWidth = Math.Min(subtitleAvailableWidth, font.MeasureTextWidth(Subtitle, scale));
                subtitleBounds = new(textX, subtitleY, subtitleWidth, textHeight);
            }
        }

        List<UiNodePinLayout> layouts = new(_pins.Count);
        int inputRow = 0;
        int outputRow = 0;
        int inputCount = 0;
        int outputCount = 0;
        for (int i = 0; i < _pins.Count; i++)
        {
            if (!ShouldLayoutPin(_pins[i]))
            {
                continue;
            }

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
            if (!ShouldLayoutPin(pin))
            {
                pin.Layout = UiNodePinLayout.Empty;
                continue;
            }

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
        if (!Compact && !string.IsNullOrEmpty(BodyText) && bodyBounds.Width > 0 && bodyBounds.Height > 0)
        {
            int rowHeight = Math.Max(Math.Max(12, PinRowHeight), Math.Max(PinHitSize + 6, textHeight + 6));
            int bodyTextY = bodyBounds.Y + padding + rowCount * rowHeight + padding;
            if (bodyTextY + textHeight <= bodyBounds.Bottom - padding)
            {
                int bodyTextAvailableWidth = Math.Max(0, bodyBounds.Width - padding * 2);
                int bodyTextWidth = Math.Min(bodyTextAvailableWidth, font.MeasureTextWidth(BodyText, scale));
                bodyTextBounds = new(bodyBounds.X + padding, bodyTextY, bodyTextWidth, textHeight);
            }
        }

        _debugLayout = new UiNodeDebugLayout(Bounds, headerBounds, bodyBounds, iconBounds, titleBounds, subtitleBounds, bodyTextBounds, layouts.ToArray());
        _layoutValid = true;
        _layoutBounds = Bounds;
        _layoutIcon = Icon;
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
        _layoutIconColumnWidth = IconColumnWidth;
        _layoutValueBoxMinWidth = ValueBoxMinWidth;
        _layoutValueBoxMaxWidth = ValueBoxMaxWidth;
        _layoutValueBoxPadding = ValueBoxPadding;
        _layoutTextScale = TextScale;
        _layoutPinHash = pinHash;
        _layoutCompact = Compact;
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
            hash.Add(pin.ValueText, StringComparer.Ordinal);
            hash.Add(pin.Direction);
            hash.Add(pin.Kind);
            hash.Add(pin.Enabled);
        }

        hash.Add(Compact);
        return hash.ToHashCode();
    }

    private UiNodePinLayout BuildPinLayout(UiNodePin pin, int rowIndex, UiRect bodyBounds, UiFont font, int textHeight, bool hasOppositePinInRow)
    {
        int rowHeight = Math.Max(Math.Max(12, PinRowHeight), Math.Max(PinHitSize + 6, textHeight + 6));
        int padding = Math.Max(0, Padding);
        int centerY = bodyBounds.Y + padding + rowHeight / 2 + rowIndex * rowHeight;
        int centerX = pin.Direction == UiNodePinDirection.Input ? Bounds.X : Bounds.Right;
        UiPoint center = new(centerX, centerY);
        int hitSize = Math.Max(4, PinHitSize);
        UiRect hitBounds = new(center.X - hitSize / 2, center.Y - hitSize / 2, hitSize, hitSize);
        UiRect rowBounds = new(Bounds.X, center.Y - rowHeight / 2, Bounds.Width, rowHeight);

        int labelWidth = font.MeasureTextWidth(pin.Text, TextScale);
        int labelY = center.Y - textHeight / 2;
        int sidePadding = ResolvePinSidePadding(padding);
        int laneGap = ResolveLaneGap(padding);
        int sharedLabelWidth = Math.Max(0, Bounds.Width - sidePadding * 2);
        int maxLabelWidth = hasOppositePinInRow
            ? Math.Max(0, (sharedLabelWidth - laneGap) / 2)
            : sharedLabelWidth;
        UiRect valueBounds = default;
        bool showValueBox = ShouldShowValueBox(pin);
        int valueBoxGap = ResolveValueBoxGap(padding);
        int valueWidth = showValueBox && maxLabelWidth > 0
            ? Math.Min(maxLabelWidth, ResolveValueBoxWidth(pin, font, TextScale))
            : 0;
        int labelMaxWidth = showValueBox
            ? Math.Max(0, maxLabelWidth - valueWidth - valueBoxGap)
            : maxLabelWidth;
        int clampedLabelWidth = Math.Min(labelMaxWidth, labelWidth);
        int labelX;
        if (pin.Direction == UiNodePinDirection.Input)
        {
            labelX = Bounds.X + sidePadding;
            if (showValueBox && valueWidth > 0)
            {
                int valueHeight = Math.Min(Math.Max(1, rowHeight - 2), ResolveValueBoxHeight(textHeight));
                int valueX = Bounds.X + sidePadding + Math.Max(0, maxLabelWidth - valueWidth);
                int valueY = centerY - valueHeight / 2;
                valueBounds = new(valueX, valueY, valueWidth, valueHeight);
            }
        }
        else
        {
            labelX = Bounds.Right - sidePadding - clampedLabelWidth;
            if (showValueBox && valueWidth > 0)
            {
                int valueHeight = Math.Min(Math.Max(1, rowHeight - 2), ResolveValueBoxHeight(textHeight));
                int valueX = labelX - valueBoxGap - valueWidth;
                int valueY = centerY - valueHeight / 2;
                valueBounds = new(Math.Max(Bounds.X + sidePadding, valueX), valueY, valueWidth, valueHeight);
            }
        }

        UiRect labelBounds = new(labelX, labelY, clampedLabelWidth, textHeight);

        return new UiNodePinLayout(pin, rowBounds, labelBounds, valueBounds, hitBounds, center);
    }

    private UiNodePin? FindPinAtRow(UiNodePinDirection direction, int rowIndex)
    {
        int row = 0;
        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin pin = _pins[i];
            if (pin.Direction != direction || !ShouldLayoutPin(pin))
            {
                continue;
            }

            if (row == rowIndex)
            {
                return pin;
            }

            row++;
        }

        return null;
    }

    private int ResolvePinSidePadding(int padding)
    {
        return Math.Max(PinVisualSize, PinHitSize) + Math.Max(4, padding / 2);
    }

    private static int ResolveLaneGap(int padding)
    {
        return Math.Max(6, padding / 2);
    }

    private int MeasurePinLaneWidth(UiNodePin? pin, UiFont font, int scale, int padding)
    {
        if (pin is null)
        {
            return 0;
        }

        int width = font.MeasureTextWidth(pin.Text, scale);
        if (ShouldShowValueBox(pin))
        {
            width += ResolveValueBoxGap(padding) + ResolveValueBoxWidth(pin, font, scale);
        }

        return width;
    }

    private bool ShouldShowValueBox(UiNodePin pin)
    {
        return pin.Kind == UiNodePinKind.Data
            && !string.IsNullOrWhiteSpace(pin.ValueText);
    }

    private int ResolveValueBoxGap(int padding)
    {
        return Math.Max(6, padding / 2);
    }

    private int ResolveValueBoxWidth(UiNodePin pin, UiFont font, int scale)
    {
        int minimum = Math.Max(1, ValueBoxMinWidth);
        int maximum = Math.Max(minimum, ValueBoxMaxWidth);
        int desired = font.MeasureTextWidth(pin.ValueText, scale) + Math.Max(0, ValueBoxPadding) * 2;
        return Math.Clamp(desired, minimum, maximum);
    }

    private static int ResolveValueBoxHeight(int textHeight)
    {
        return Math.Max(18, textHeight + 6);
    }

    private int ResolveIconColumnWidth(int textHeight, int padding, bool hasIcon)
    {
        if (!hasIcon)
        {
            return 0;
        }

        return Math.Max(IconColumnWidth, textHeight + Math.Max(4, padding / 2));
    }

    private void DrawIcon(UiRenderContext context, UiFont font)
    {
        if (string.IsNullOrEmpty(Icon) || _debugLayout.IconBounds.Width <= 0 || _debugLayout.IconBounds.Height <= 0)
        {
            return;
        }

        context.Renderer.PushClip(_debugLayout.IconBounds);
        context.Renderer.DrawText(Icon, new UiPoint(_debugLayout.IconBounds.X, _debugLayout.IconBounds.Y), IconColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawTitle(UiRenderContext context, UiFont font)
    {
        if (_debugLayout.TitleBounds.Width <= 0 || _debugLayout.TitleBounds.Height <= 0)
        {
            return;
        }

        int availableWidth = Math.Max(0, _debugLayout.TitleBounds.Width);
        string drawText = UiRenderHelpers.BuildElidedText(Title, availableWidth, TextScale, font);
        context.Renderer.PushClip(_debugLayout.HeaderBounds);
        context.Renderer.DrawText(drawText, new UiPoint(_debugLayout.TitleBounds.X, _debugLayout.TitleBounds.Y), TitleColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawBodyText(UiRenderContext context, UiFont font)
    {
        if (Compact || string.IsNullOrEmpty(BodyText) || _debugLayout.BodyTextBounds.Width <= 0 || _debugLayout.BodyTextBounds.Height <= 0)
        {
            return;
        }

        int availableWidth = Math.Max(0, _debugLayout.BodyTextBounds.Width);
        string drawText = UiRenderHelpers.BuildElidedText(BodyText, availableWidth, TextScale, font);
        context.Renderer.PushClip(_debugLayout.BodyBounds);
        context.Renderer.DrawText(drawText, new UiPoint(_debugLayout.BodyTextBounds.X, _debugLayout.BodyTextBounds.Y), BodyTextColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawSubtitle(UiRenderContext context, UiFont font)
    {
        if (Compact || string.IsNullOrEmpty(Subtitle) || _debugLayout.SubtitleBounds.Width <= 0 || _debugLayout.SubtitleBounds.Height <= 0)
        {
            return;
        }

        int availableWidth = Math.Max(0, _debugLayout.SubtitleBounds.Width);
        string drawText = UiRenderHelpers.BuildElidedText(Subtitle, availableWidth, TextScale, font);
        context.Renderer.PushClip(_debugLayout.HeaderBounds);
        context.Renderer.DrawText(drawText, new UiPoint(_debugLayout.SubtitleBounds.X, _debugLayout.SubtitleBounds.Y), SubtitleColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private void DrawPinChrome(UiRenderContext context)
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin pin = _pins[i];
            UiNodePinLayout layout = pin.Layout;
            if (!layout.IsValid)
            {
                continue;
            }

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

            DrawValueBoxChrome(context, pin, layout);
        }
    }

    private void DrawPinText(UiRenderContext context, UiFont font)
    {
        for (int i = 0; i < _pins.Count; i++)
        {
            UiNodePin pin = _pins[i];
            UiNodePinLayout layout = pin.Layout;
            if (!layout.IsValid)
            {
                continue;
            }

            DrawValueBoxText(context, font, pin, layout);
            if (layout.LabelBounds.Width > 0 && layout.LabelBounds.Height > 0)
            {
                string drawText = UiRenderHelpers.BuildElidedText(pin.Text, layout.LabelBounds.Width, TextScale, font);
                context.Renderer.DrawText(drawText, new UiPoint(layout.LabelBounds.X, layout.LabelBounds.Y), BodyTextColor, TextScale, font);
            }
        }
    }

    private void DrawGlow(IUiRenderer renderer)
    {
        if (!TryGetGlow(out _, out var color, out var passes))
        {
            return;
        }

        int safePasses = Math.Max(1, passes);
        int maxRadius = Math.Max(1, GlowRadius);
        for (int pass = safePasses; pass >= 1; pass--)
        {
            int spread = Math.Max(1, (int)MathF.Round(maxRadius * pass / (float)safePasses));
            int intensity = safePasses - pass + 1;
            UiColor passColor = WithAlpha(color, color.A * intensity / (safePasses + 1));
            UiRenderHelpers.DrawRectRounded(
                renderer,
                ExpandRect(Bounds, spread),
                CornerRadius + spread,
                passColor,
                Math.Max(1, spread / 2));
        }
    }

    private void DrawValueBoxChrome(UiRenderContext context, UiNodePin pin, UiNodePinLayout layout)
    {
        if (!ShouldShowValueBox(pin) || layout.ValueBounds.Width <= 0 || layout.ValueBounds.Height <= 0)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, layout.ValueBounds, 3, ValueBoxBackground);
        UiRenderHelpers.DrawRectRounded(context.Renderer, layout.ValueBounds, 3, ValueBoxBorder, 1);
    }

    private void DrawValueBoxText(UiRenderContext context, UiFont font, UiNodePin pin, UiNodePinLayout layout)
    {
        if (!ShouldShowValueBox(pin) || layout.ValueBounds.Width <= 0 || layout.ValueBounds.Height <= 0)
        {
            return;
        }

        int padding = Math.Max(0, ValueBoxPadding);
        int availableWidth = Math.Max(0, layout.ValueBounds.Width - padding * 2);
        if (availableWidth <= 0)
        {
            return;
        }

        string drawText = UiRenderHelpers.BuildElidedText(pin.ValueText, availableWidth, TextScale, font);
        int textWidth = font.MeasureTextWidth(drawText, TextScale);
        int textHeight = font.MeasureTextHeight(TextScale);
        int textX = layout.ValueBounds.Right - padding - Math.Min(availableWidth, textWidth);
        int textY = layout.ValueBounds.Y + Math.Max(0, (layout.ValueBounds.Height - textHeight) / 2);
        context.Renderer.PushClip(layout.ValueBounds);
        context.Renderer.DrawText(drawText, new UiPoint(textX, textY), ValueBoxTextColor, TextScale, font);
        context.Renderer.PopClip();
    }

    private UiColor ResolvePinColor(UiNodePin pin)
    {
        return pin.Kind == UiNodePinKind.Exec
            ? ExecPinColor
            : pin.Color ?? DataPinColor;
    }

    private bool ShouldLayoutPin(UiNodePin pin)
    {
        return !Compact || pin.Kind != UiNodePinKind.Exec;
    }

    private int ResolveHeaderHeight(int textHeight, int padding, bool hasSubtitle)
    {
        if (Compact)
        {
            return Math.Max(22, textHeight + Math.Max(5, padding / 2));
        }

        return Math.Max(
            Math.Max(30, HeaderHeight),
            hasSubtitle
                ? textHeight * 2 + Math.Max(7, padding)
                : textHeight + Math.Max(8, padding));
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

    private static UiRect ExpandRect(UiRect rect, int padding)
    {
        int safePadding = Math.Max(0, padding);
        return new UiRect(
            rect.X - safePadding,
            rect.Y - safePadding,
            rect.Width + safePadding * 2,
            rect.Height + safePadding * 2);
    }

    private static UiColor WithAlpha(UiColor color, int alpha)
    {
        return new UiColor(color.R, color.G, color.B, (byte)Math.Clamp(alpha, 0, 255));
    }
}
