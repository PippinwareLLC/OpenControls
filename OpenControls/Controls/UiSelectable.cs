namespace OpenControls.Controls;

public sealed class UiSelectable : UiElement
{
    private bool _hovered;
    private bool _pressed;
    private bool _focused;
    private bool _selected;
    private UiSelectionModel? _selectionModel;
    private int _selectionIndex = -1;

    public string Text { get; set; } = string.Empty;
    public int TextScale { get; set; } = 1;
    public int Padding { get; set; } = 6;
    public bool AllowToggle { get; set; } = true;
    public UiColor Background { get; set; } = UiColor.Transparent;
    public UiColor HoverBackground { get; set; } = new UiColor(36, 42, 58);
    public UiColor SelectedBackground { get; set; } = new UiColor(70, 80, 100);
    public UiColor Border { get; set; } = UiColor.Transparent;
    public int BorderThickness { get; set; } = 1;
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor SelectedTextColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }

    public bool Selected
    {
        get => _selectionModel != null ? _selectionModel.IsSelected(_selectionIndex) : _selected;
        set
        {
            if (_selectionModel != null)
            {
                if (_selectionIndex < 0)
                {
                    return;
                }

                _selectionModel.SetSelected(_selectionIndex, value);
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

        if (_focused && input.Navigation.Activate)
        {
            ApplySelection(input);
        }

        if (input.LeftReleased)
        {
            if (_pressed && _hovered)
            {
                ApplySelection(input);
            }

            _pressed = false;
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        bool isSelected = _selectionModel != null ? _selectionModel.IsSelected(_selectionIndex) : _selected;
        UiColor fill = Background;
        if (isSelected)
        {
            fill = SelectedBackground;
        }
        else if (_hovered || _pressed)
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

        UiColor textColor = isSelected ? SelectedTextColor : TextColor;
        int textHeight = context.Renderer.MeasureTextHeight(TextScale);
        int textY = Bounds.Y + (Bounds.Height - textHeight) / 2;
        context.Renderer.DrawText(Text, new UiPoint(Bounds.X + Padding, textY), textColor, TextScale);

        base.Render(context);
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

    private void ApplySelection(UiInputState input)
    {
        if (_selectionModel != null)
        {
            if (_selectionIndex < 0)
            {
                return;
            }

            _selectionModel.ApplySelection(_selectionIndex, input.CtrlDown, input.ShiftDown);
            return;
        }

        if (AllowToggle)
        {
            SetSelected(!_selected);
            return;
        }

        if (!_selected)
        {
            SetSelected(true);
        }
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

        bool selected = _selectionIndex >= 0 && _selectionModel.IsSelected(_selectionIndex);
        if (_selected == selected)
        {
            return;
        }

        _selected = selected;
        SelectedChanged?.Invoke(_selected);
    }
}
