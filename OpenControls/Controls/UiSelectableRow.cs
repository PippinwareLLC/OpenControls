namespace OpenControls.Controls;

public sealed class UiSelectableRow : UiElement
{
    private bool _hovered;
    private bool _pressed;
    private bool _focused;
    private bool _selected;
    private UiSelectionModel? _selectionModel;
    private int _selectionIndex = -1;

    internal UiListView? OwnerListView { get; set; }

    public string Text { get; set; } = string.Empty;
    public string SecondaryText { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public IUiImageSource? ImageSource { get; set; }
    public int ImageSize { get; set; } = 18;
    public int ImageTextGap { get; set; } = 8;
    public int Padding { get; set; } = 6;
    public int TextScale { get; set; } = 1;
    public int SecondaryTextScale { get; set; } = 1;
    public bool AllowToggle { get; set; } = true;
    public bool Highlighted { get; set; }
    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor SelectedBackground { get; set; } = new UiColor(70, 80, 100);
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor SecondaryTextColor { get; set; } = new UiColor(170, 180, 200);
    public UiColor SelectedTextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }
    public string SelectionScope { get; set; } = string.Empty;

    public UiRect ContentBounds
    {
        get
        {
            int padding = Math.Max(0, Padding);
            return new UiRect(
                Bounds.X + padding,
                Bounds.Y + padding,
                Math.Max(0, Bounds.Width - padding * 2),
                Math.Max(0, Bounds.Height - padding * 2));
        }
    }

    public bool Selected
    {
        get => _selectionModel != null ? _selectionModel.IsSelected(_selectionIndex, SelectionScope) : _selected;
        set
        {
            if (_selectionModel != null)
            {
                if (_selectionIndex >= 0)
                {
                    _selectionModel.SetSelected(_selectionIndex, value, SelectionScope);
                }

                return;
            }

            SetSelected(value);
        }
    }

    public UiSelectionModel? SelectionModel
    {
        get => _selectionModel;
        set
        {
            if (_selectionModel == value)
            {
                return;
            }

            if (_selectionModel != null)
            {
                _selectionModel.SelectionChanged -= HandleSelectionModelChanged;
            }

            _selectionModel = value;
            if (_selectionModel != null)
            {
                _selectionModel.SelectionChanged += HandleSelectionModelChanged;
            }

            SyncSelectedFromModel();
        }
    }

    public int SelectionIndex
    {
        get => _selectionIndex;
        set
        {
            if (_selectionIndex == value)
            {
                return;
            }

            _selectionIndex = value;
            SyncSelectedFromModel();
        }
    }

    public string EffectiveSearchText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                return SearchText;
            }

            if (string.IsNullOrWhiteSpace(SecondaryText))
            {
                return Text;
            }

            return string.Concat(Text, " ", SecondaryText);
        }
    }

    public event Action<UiSelectableRow>? Invoked;
    public event Action<bool>? SelectedChanged;

    public override bool IsFocusable => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        UiInputState input = context.Input;
        _hovered = Bounds.Contains(input.MousePosition);

        if (input.LeftClicked && _hovered)
        {
            _pressed = true;
            context.Focus.RequestFocus(this);
        }

        if (_focused && OwnerListView != null && OwnerListView.HandleNavigation(this, context))
        {
            UpdateChildren(context);
            return;
        }

        if (_focused && input.Navigation.Activate)
        {
            Invoke(input);
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                Invoke(input);
            }

            _pressed = false;
        }

        UpdateChildren(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        bool selected = Selected;
        UiColor fill = Background;
        if (selected)
        {
            fill = SelectedBackground;
        }
        else if (_pressed || _hovered || Highlighted)
        {
            fill = HoverBackground;
        }

        if (fill.A > 0)
        {
            UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, fill);
        }

        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }

        DrawBuiltInContent(context, selected);
        RenderChildrenTranslated(context, overlay: false);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        RenderChildrenTranslated(context, overlay: true);
    }

    public override UiElement? HitTest(UiPoint point)
    {
        if (!Visible || !Bounds.Contains(point))
        {
            return null;
        }

        UiRect content = ContentBounds;
        UiPoint localPoint = new UiPoint(point.X - content.X, point.Y - content.Y);
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

    protected internal override void OnFocusGained()
    {
        _focused = true;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _pressed = false;
        Highlighted = false;
    }

    private void UpdateChildren(UiUpdateContext context)
    {
        UiRect content = ContentBounds;
        UiInputState childInput = UiInputTransform.Translate(context.Input, content.X, content.Y);
        UiUpdateContext childContext = new UiUpdateContext(
            childInput,
            context.Focus,
            context.DragDrop,
            context.DeltaSeconds,
            context.DefaultFont,
            context.Clipboard,
            context.ActiveInputLayer);

        foreach (UiElement child in Children)
        {
            child.Update(childContext);
        }
    }

    private void RenderChildrenTranslated(UiRenderContext context, bool overlay)
    {
        UiRect content = ContentBounds;
        UiOffsetRenderer offsetRenderer = new UiOffsetRenderer(context.Renderer, new UiPoint(content.X, content.Y));
        UiRenderContext childContext = new UiRenderContext(offsetRenderer, context.DefaultFont);

        if (ClipChildren)
        {
            context.Renderer.PushClip(content);
        }

        foreach (UiElement child in Children)
        {
            if (overlay)
            {
                child.RenderOverlay(childContext);
            }
            else
            {
                child.Render(childContext);
            }
        }

        if (ClipChildren)
        {
            context.Renderer.PopClip();
        }
    }

    private void DrawBuiltInContent(UiRenderContext context, bool selected)
    {
        UiRect content = ContentBounds;
        if (content.Width <= 0 || content.Height <= 0)
        {
            return;
        }

        int x = content.X;
        if (ImageSource != null)
        {
            int imageSize = Math.Max(8, Math.Min(ImageSize, content.Height));
            int imageY = content.Y + (content.Height - imageSize) / 2;
            ImageSource.Draw(context.Renderer, new UiRect(x, imageY, imageSize, imageSize));
            x += imageSize + Math.Max(0, ImageTextGap);
        }

        if (string.IsNullOrEmpty(Text) && string.IsNullOrEmpty(SecondaryText))
        {
            return;
        }

        UiColor textColor = selected ? SelectedTextColor : TextColor;
        UiFont font = ResolveFont(context.DefaultFont);
        int primaryHeight = context.Renderer.MeasureTextHeight(TextScale, font);
        if (string.IsNullOrEmpty(SecondaryText))
        {
            int textY = content.Y + (content.Height - primaryHeight) / 2;
            context.Renderer.DrawText(Text, new UiPoint(x, textY), textColor, TextScale, font);
            return;
        }

        int secondaryHeight = context.Renderer.MeasureTextHeight(SecondaryTextScale, font);
        int totalHeight = primaryHeight + secondaryHeight;
        int primaryY = content.Y + Math.Max(0, (content.Height - totalHeight) / 2);
        int secondaryY = primaryY + primaryHeight;
        context.Renderer.DrawText(Text, new UiPoint(x, primaryY), textColor, TextScale, font);
        context.Renderer.DrawText(SecondaryText, new UiPoint(x, secondaryY), selected ? SelectedTextColor : SecondaryTextColor, SecondaryTextScale, font);
    }

    private void Invoke(UiInputState input)
    {
        if (_selectionModel != null)
        {
            if (_selectionIndex >= 0)
            {
                _selectionModel.ApplySelection(_selectionIndex, input.CtrlDown, input.ShiftDown, SelectionScope);
            }
        }
        else if (AllowToggle)
        {
            SetSelected(!_selected);
        }
        else if (!_selected)
        {
            SetSelected(true);
        }

        Invoked?.Invoke(this);
    }

    private void HandleSelectionModelChanged()
    {
        SyncSelectedFromModel();
    }

    private void SyncSelectedFromModel()
    {
        if (_selectionModel == null)
        {
            return;
        }

        bool selected = _selectionIndex >= 0 && _selectionModel.IsSelected(_selectionIndex, SelectionScope);
        if (_selected == selected)
        {
            return;
        }

        _selected = selected;
        SelectedChanged?.Invoke(_selected);
    }

    private void SetSelected(bool value)
    {
        if (_selected == value)
        {
            return;
        }

        _selected = value;
        SelectedChanged?.Invoke(_selected);
    }
}
