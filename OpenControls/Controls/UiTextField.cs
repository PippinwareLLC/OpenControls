namespace OpenControls.Controls;

public sealed class UiTextField : UiElement
{
    private bool _focused;
    private float _caretTimer;
    private bool _caretVisible = true;
    private string _text = string.Empty;
    private bool _clickedThisFrame;
    private bool _editedThisFrame;

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            if (CaretIndex > _text.Length)
            {
                CaretIndex = _text.Length;
            }
        }
    }
    public int CaretIndex { get; private set; }
    public int MaxLength { get; set; } = 200;
    public int Padding { get; set; } = 4;
    public int TextScale { get; set; } = 1;
    public string Placeholder { get; set; } = string.Empty;
    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor FocusBorder { get; set; } = new UiColor(120, 140, 200);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor PlaceholderColor { get; set; } = new UiColor(120, 130, 150);
    public UiColor CaretColor { get; set; } = UiColor.White;
    public int CornerRadius { get; set; }
    public Func<char, bool>? CharacterFilter { get; set; }
    public Func<UiTextField, UiPoint, int>? CaretIndexFromPoint { get; set; }

    public override bool IsFocusable => true;
    public override bool WantsTextInput => true;

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _clickedThisFrame = false;
        _editedThisFrame = false;
        UiInputState input = context.Input;
        if (input.LeftClicked)
        {
            if (Bounds.Contains(input.MousePosition))
            {
                _clickedThisFrame = true;
                context.Focus.RequestFocus(this);
                int caretIndex = Text.Length;
                if (CaretIndexFromPoint != null)
                {
                    caretIndex = CaretIndexFromPoint(this, input.MousePosition);
                }
                SetCaret(caretIndex);
            }
            else if (_focused)
            {
                context.Focus.ClearFocus();
            }
        }

        if (_focused)
        {
            HandleNavigation(input.Navigation);
            HandleTextInput(input.TextInput);
            UpdateCaretBlink(context.DeltaSeconds);
        }

        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        UiColor border = _focused ? FocusBorder : Border;
        UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, border, 1);

        int textX = Bounds.X + Padding;
        int textY = Bounds.Y + Padding;
        int clipWidth = Math.Max(0, Bounds.Width - Padding * 2);
        int clipHeight = Math.Max(0, Bounds.Height - Padding * 2);
        UiRect clip = new UiRect(textX, textY, clipWidth, clipHeight);
        context.Renderer.PushClip(clip);
        string displayText = Text;
        UiColor displayColor = TextColor;
        UiFont font = ResolveFont(context.DefaultFont);
        if (string.IsNullOrEmpty(displayText) && !string.IsNullOrEmpty(Placeholder))
        {
            displayText = Placeholder;
            displayColor = PlaceholderColor;
        }

        context.Renderer.DrawText(displayText, new UiPoint(textX, textY), displayColor, TextScale, font);

        if (_focused && _caretVisible)
        {
            int caretWidth = Math.Max(1, TextScale);
            int caretX = textX + context.Renderer.MeasureTextWidth(Text[..CaretIndex], TextScale, font);
            if (CaretIndex > 0)
            {
                caretX -= caretWidth;
            }

            int caretHeight = context.Renderer.MeasureTextHeight(TextScale, font);
            context.Renderer.FillRect(new UiRect(caretX, textY, caretWidth, caretHeight), CaretColor);
        }

        context.Renderer.PopClip();

        base.Render(context);
    }

    public void SetCaretIndex(int index)
    {
        SetCaret(index);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
        _caretVisible = true;
        _caretTimer = 0f;
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _caretVisible = false;
        _caretTimer = 0f;
    }

    protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
    {
        if (_focused || Bounds.Contains(input.MousePosition))
        {
            cursor = UiMouseCursor.TextInput;
            return true;
        }

        cursor = UiMouseCursor.Arrow;
        return false;
    }

    protected internal override bool TryGetTextInputRequest(out UiTextInputRequest request)
    {
        request = new UiTextInputRequest(Bounds, isMultiLine: false);
        return true;
    }

    protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
    {
        UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
        if (_focused)
        {
            status |= UiItemStatusFlags.Active;
        }

        if (_clickedThisFrame)
        {
            status |= UiItemStatusFlags.Clicked;
        }

        if (_editedThisFrame)
        {
            status |= UiItemStatusFlags.Edited;
        }

        return status;
    }

    private void HandleNavigation(UiNavigationInput navigation)
    {
        if (navigation.MoveLeft)
        {
            SetCaret(CaretIndex - 1);
        }

        if (navigation.MoveRight)
        {
            SetCaret(CaretIndex + 1);
        }

        if (navigation.Home)
        {
            SetCaret(0);
        }

        if (navigation.End)
        {
            SetCaret(Text.Length);
        }

        if (navigation.Backspace)
        {
            Backspace();
        }

        if (navigation.Delete)
        {
            Delete();
        }
    }

    private void HandleTextInput(IReadOnlyList<char> input)
    {
        foreach (char character in input)
        {
            if (!IsCharacterAllowed(character))
            {
                continue;
            }

            if (Text.Length >= MaxLength)
            {
                return;
            }

            Text = Text.Insert(CaretIndex, character.ToString());
            SetCaret(CaretIndex + 1);
            _editedThisFrame = true;
        }
    }

    private bool IsCharacterAllowed(char character)
    {
        if (CharacterFilter != null)
        {
            return CharacterFilter(character);
        }

        return !char.IsControl(character);
    }

    private void Backspace()
    {
        if (CaretIndex <= 0 || Text.Length == 0)
        {
            return;
        }

        int caretIndex = CaretIndex;
        Text = Text.Remove(caretIndex - 1, 1);
        SetCaret(caretIndex - 1);
        _editedThisFrame = true;
    }

    private void Delete()
    {
        if (CaretIndex >= Text.Length)
        {
            return;
        }

        Text = Text.Remove(CaretIndex, 1);
        SetCaret(CaretIndex);
        _editedThisFrame = true;
    }

    private void SetCaret(int index)
    {
        CaretIndex = Math.Clamp(index, 0, Text.Length);
        _caretVisible = true;
        _caretTimer = 0f;
    }

    private void UpdateCaretBlink(float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
        {
            return;
        }

        _caretTimer += deltaSeconds;
        if (_caretTimer >= 0.5f)
        {
            _caretTimer = 0f;
            _caretVisible = !_caretVisible;
        }
    }
}
