using OpenControls.State;

namespace OpenControls.Controls;

public sealed class UiTextField : UiElement, IUiStatefulElement
{
    private readonly UiTextEditingState _editingState = new();
    private UiFont _layoutFont = UiFont.Default;
    private UiTextCompositionState _composition;
    private bool _focused;
    private float _caretTimer;
    private bool _caretVisible = true;
    private bool _clickedThisFrame;
    private bool _editedThisFrame;
    private bool _dragSelecting;
    private int _dragSelectionAnchor;
    private int _horizontalScrollOffset;

    public string Text
    {
        get => _editingState.Text;
        set
        {
            _editingState.SetText(value ?? string.Empty);
            ResetCaretBlink();
            Invalidate(UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
        }
    }

    public int CaretIndex => _editingState.CaretIndex;
    public bool HasSelection => _editingState.HasSelection;
    public int SelectionStart => _editingState.SelectionStart;
    public int SelectionEnd => _editingState.SelectionEnd;
    public int SelectionLength => _editingState.SelectionLength;
    public string SelectedText => _editingState.GetSelectedText();
    public int HorizontalScrollOffset => _horizontalScrollOffset;
    public UiTextCompositionState Composition => _composition;
    public int MaxLength { get; set; } = 200;
    public int Padding { get; set; } = 4;
    public int TextScale { get; set; } = 1;
    public string Placeholder { get; set; } = string.Empty;
    public bool AllowTabInput { get; set; }
    public bool ReadOnly { get; set; }
    public bool PasswordMode { get; set; }
    public char PasswordMaskCharacter { get; set; } = '*';
    public UiColor Background { get; set; } = new UiColor(28, 32, 44);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public UiColor FocusBorder { get; set; } = new UiColor(120, 140, 200);
    public UiColor TextColor { get; set; } = UiColor.White;
    public UiColor PlaceholderColor { get; set; } = new UiColor(120, 130, 150);
    public UiColor CaretColor { get; set; } = UiColor.White;
    public UiColor SelectionBackground { get; set; } = new UiColor(72, 114, 196, 180);
    public UiColor CompositionTextColor { get; set; } = new UiColor(224, 228, 240);
    public UiColor CompositionUnderlineColor { get; set; } = new UiColor(132, 156, 220);
    public int CornerRadius { get; set; }
    public Func<char, bool>? CharacterFilter { get; set; }
    public Func<UiTextField, int, int>? ResizeCallback { get; set; }
    public Func<UiTextField, UiPoint, int>? CaretIndexFromPoint { get; set; }

    public override bool IsFocusable => true;
    public override bool HandlesTabInput => AllowTabInput;
    public override bool WantsTextInput => !ReadOnly;
    public override bool IsRenderCacheVolatile(UiContext context) => _focused;

    public event Action<string>? TextChanged;
    public event Action? Submitted;
    public event Action? Cancelled;
    public event Action<UiTextField>? CompletionRequested;
    public event Action<UiTextField, int>? HistoryRequested;

    public string GetDisplayText()
    {
        if (!PasswordMode || string.IsNullOrEmpty(Text))
        {
            return Text;
        }

        return new string(PasswordMaskCharacter, Text.Length);
    }

    public void SetCaretIndex(int index)
    {
        _editingState.SetCaret(index);
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public void SelectAllText()
    {
        _editingState.SelectAll();
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public void SelectRange(int anchorIndex, int caretIndex)
    {
        _editingState.SelectRange(anchorIndex, caretIndex);
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public void SetHorizontalScrollOffset(int offset)
    {
        _horizontalScrollOffset = Math.Max(0, offset);
        Invalidate(UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _clickedThisFrame = false;
        _editedThisFrame = false;

        UiInputState input = context.Input;
        _layoutFont = ResolveFont(context.DefaultFont);
        HandlePointerInput(context, input);

        if (_focused)
        {
            _composition = input.Composition;
            HandleShortcutInput(context, input);
            HandleNavigation(context, input);
            HandleTextInput(input.TextInput);
            UpdateCaretBlink(context.DeltaSeconds);
        }
        else if (_composition.IsActive)
        {
            _composition = UiTextCompositionState.Empty;
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

        UiFont font = ResolveFont(context.DefaultFont);
        _layoutFont = font;
        int textX = Bounds.X + Padding;
        int textY = GetTextTopY(font);
        int clipWidth = Math.Max(0, Bounds.Width - Padding * 2);
        int clipHeight = Math.Max(0, Bounds.Height - Padding * 2);
        UiRect clip = new UiRect(textX, textY, clipWidth, clipHeight);
        string renderText = GetDisplayText();

        UpdateHorizontalScroll(context.Renderer, renderText, clipWidth, font);

        context.Renderer.PushClip(clip);
        if (_focused && HasSelection && !string.IsNullOrEmpty(renderText))
        {
            int selectionX = textX - _horizontalScrollOffset + MeasurePrefixWidth(context.Renderer, renderText, SelectionStart, TextScale, font);
            int selectionEndX = textX - _horizontalScrollOffset + MeasurePrefixWidth(context.Renderer, renderText, SelectionEnd, TextScale, font);
            int selectionWidth = Math.Max(1, selectionEndX - selectionX);
            int selectionHeight = context.Renderer.MeasureTextHeight(TextScale, font);
            context.Renderer.FillRect(new UiRect(selectionX, textY, selectionWidth, selectionHeight), SelectionBackground);
        }

        string displayText = renderText;
        UiColor displayColor = TextColor;
        if (string.IsNullOrEmpty(displayText) && !string.IsNullOrEmpty(Placeholder))
        {
            displayText = Placeholder;
            displayColor = PlaceholderColor;
        }

        context.Renderer.DrawText(displayText, new UiPoint(textX - _horizontalScrollOffset, textY), displayColor, TextScale, font);

        if (_focused && _composition.IsActive)
        {
            DrawComposition(context.Renderer, renderText, textX, textY, font);
        }

        if (_focused && _caretVisible)
        {
            int caretWidth = Math.Max(1, Math.Min(2, TextScale));
            int caretX = textX - _horizontalScrollOffset + MeasurePrefixWidth(context.Renderer, renderText, CaretIndex, TextScale, font);
            int caretHeight = context.Renderer.MeasureTextHeight(TextScale, font);
            context.Renderer.FillRect(new UiRect(caretX, textY, caretWidth, caretHeight), CaretColor);
        }

        context.Renderer.PopClip();
        base.Render(context);
    }

    protected internal override void OnFocusGained()
    {
        _focused = true;
        _editingState.BeginSession();
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Volatility);
    }

    protected internal override void OnFocusLost()
    {
        _focused = false;
        _dragSelecting = false;
        _caretVisible = false;
        _caretTimer = 0f;
        _composition = UiTextCompositionState.Empty;
        _editingState.ClearSelection();
        _editingState.EndSession();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Volatility);
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
        if (ReadOnly)
        {
            request = default;
            return false;
        }

        UiRect caretBounds = GetCaretBounds(_layoutFont);
        UiRect candidateBounds = _composition.IsActive
            ? new UiRect(caretBounds.X, caretBounds.Y, Math.Max(caretBounds.Width, MeasureCompositionWidth(_layoutFont)), caretBounds.Height)
            : caretBounds;
        request = new UiTextInputRequest(Bounds, isMultiLine: false, caretBounds: caretBounds, candidateBounds: candidateBounds);
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

    private void HandlePointerInput(UiUpdateContext context, UiInputState input)
    {
        if (input.LeftClicked)
        {
            if (Bounds.Contains(input.MousePosition))
            {
                _clickedThisFrame = true;
                bool hadFocus = _focused;
                context.Focus.RequestFocus(this);

                int caretIndex = ResolveCaretIndex(input.MousePosition);
                if (input.LeftDoubleClicked)
                {
                    SelectWordAt(caretIndex);
                    _dragSelecting = false;
                }
                else
                {
                    if (input.ShiftDown && hadFocus)
                    {
                        _editingState.SetCaret(caretIndex, extendSelection: true);
                    }
                    else
                    {
                        _editingState.SetCaret(caretIndex);
                    }

                    _dragSelectionAnchor = _editingState.SelectionAnchor;
                    _dragSelecting = true;
                }

                ResetCaretBlink();
            }
            else if (_focused)
            {
                _dragSelecting = false;
                context.Focus.ClearFocus();
            }
        }

        if (_dragSelecting)
        {
            if (input.LeftDown)
            {
                int caretIndex = ResolveCaretIndex(input.MousePosition);
                _editingState.SelectRange(_dragSelectionAnchor, caretIndex);
                ResetCaretBlink();
            }
            else
            {
                _dragSelecting = false;
            }
        }
    }

    private void HandleShortcutInput(UiUpdateContext context, UiInputState input)
    {
        if (input.IsPrimaryShortcutPressed(UiKey.A))
        {
            _editingState.SelectAll();
            ResetCaretBlink();
        }

        if (input.IsPrimaryShortcutPressed(UiKey.C))
        {
            CopySelection(context.Clipboard);
        }

        if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.X))
        {
            CutSelection(context.Clipboard);
        }

        if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.V))
        {
            PasteFromClipboard(context.Clipboard);
        }

        if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.Z, shift: true))
        {
            ApplyEdit(_editingState.Redo());
        }
        else if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.Z))
        {
            ApplyEdit(_editingState.Undo());
        }
        else if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.Y))
        {
            ApplyEdit(_editingState.Redo());
        }
    }

    private void HandleNavigation(UiUpdateContext context, UiInputState input)
    {
        UiNavigationInput navigation = input.Navigation;
        bool extendSelection = input.ShiftDown;
        bool byWord = input.CtrlDown || input.AltDown;

        if (navigation.MoveLeft)
        {
            if (input.SuperDown && !byWord)
            {
                _editingState.MoveHome(extendSelection);
            }
            else
            {
                _editingState.MoveLeft(extendSelection, byWord);
            }

            ResetCaretBlink();
        }

        if (navigation.MoveRight)
        {
            if (input.SuperDown && !byWord)
            {
                _editingState.MoveEnd(extendSelection);
            }
            else
            {
                _editingState.MoveRight(extendSelection, byWord);
            }

            ResetCaretBlink();
        }

        if (navigation.Home)
        {
            _editingState.MoveHome(extendSelection);
            ResetCaretBlink();
        }

        if (navigation.End)
        {
            _editingState.MoveEnd(extendSelection);
            ResetCaretBlink();
        }

        if (!ReadOnly && navigation.Backspace)
        {
            ApplyEdit(_editingState.Backspace(byWord));
        }

        if (!ReadOnly && navigation.Delete)
        {
            ApplyEdit(_editingState.Delete(byWord));
        }

        if (AllowTabInput && navigation.Tab)
        {
            CompletionRequested?.Invoke(this);
        }

        if (HistoryRequested != null)
        {
            if (navigation.MoveUp)
            {
                HistoryRequested(this, -1);
            }

            if (navigation.MoveDown)
            {
                HistoryRequested(this, 1);
            }
        }

        if (navigation.Enter || navigation.KeypadEnter)
        {
            Submitted?.Invoke();
            _editingState.MarkSessionOrigin();
        }

        if (navigation.Escape)
        {
            bool reverted = _editingState.CancelSession();
            if (reverted)
            {
                NotifyTextChanged();
                _editedThisFrame = true;
            }

            Cancelled?.Invoke();
            context.Focus.ClearFocus();
        }
    }

    private void HandleTextInput(IReadOnlyList<char> input)
    {
        if (ReadOnly || input.Count == 0)
        {
            return;
        }

        string pending = string.Empty;
        for (int i = 0; i < input.Count; i++)
        {
            char character = input[i];
            if (IsCharacterAllowed(character))
            {
                pending += character;
            }
        }

        if (pending.Length > 0)
        {
            InsertText(pending);
        }
    }

    private void CopySelection(IUiClipboard clipboard)
    {
        if (!HasSelection)
        {
            return;
        }

        clipboard.SetText(SelectedText);
    }

    private void CutSelection(IUiClipboard clipboard)
    {
        if (!HasSelection)
        {
            return;
        }

        clipboard.SetText(SelectedText);
        ApplyEdit(_editingState.DeleteSelection());
    }

    private void PasteFromClipboard(IUiClipboard clipboard)
    {
        InsertText(clipboard.GetText());
    }

    private void InsertText(string text)
    {
        string filtered = FilterText(text);
        if (string.IsNullOrEmpty(filtered))
        {
            return;
        }

        int requiredLength = Text.Length - SelectionLength + filtered.Length;
        EnsureCapacity(requiredLength);

        int available = GetAvailableInsertionLength();
        if (available <= 0)
        {
            return;
        }

        if (filtered.Length > available)
        {
            filtered = filtered.Substring(0, available);
        }

        ApplyEdit(_editingState.InsertText(filtered));
    }

    private void EnsureCapacity(int requiredLength)
    {
        if (MaxLength <= 0 || requiredLength <= MaxLength || ResizeCallback == null)
        {
            return;
        }

        int nextMaxLength = ResizeCallback(this, requiredLength);
        if (nextMaxLength > MaxLength)
        {
            MaxLength = nextMaxLength;
        }
    }

    private int GetAvailableInsertionLength()
    {
        if (MaxLength <= 0)
        {
            return int.MaxValue;
        }

        return Math.Max(0, MaxLength - (Text.Length - SelectionLength));
    }

    private string FilterText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return new string(text.Where(IsCharacterAllowed).ToArray());
    }

    private bool IsCharacterAllowed(char character)
    {
        if (CharacterFilter != null)
        {
            return CharacterFilter(character);
        }

        return !char.IsControl(character);
    }

    private int ResolveCaretIndex(UiPoint point)
    {
        if (CaretIndexFromPoint != null)
        {
            return CaretIndexFromPoint(this, point);
        }

        string displayText = GetDisplayText();
        if (displayText.Length == 0)
        {
            return 0;
        }

        int localX = point.X - (Bounds.X + Padding) + _horizontalScrollOffset;
        if (localX <= 0)
        {
            return 0;
        }

        int previousWidth = 0;
        for (int i = 0; i < displayText.Length; i++)
        {
            int nextWidth = MeasurePrefixWidth(displayText, i + 1, TextScale, _layoutFont);
            int midpoint = previousWidth + (nextWidth - previousWidth) / 2;
            if (localX < midpoint)
            {
                return i;
            }

            previousWidth = nextWidth;
        }

        return displayText.Length;
    }

    private void SelectWordAt(int index)
    {
        if (string.IsNullOrEmpty(Text))
        {
            _editingState.SetCaret(0);
            return;
        }

        _editingState.SelectWordAt(index);
        ResetCaretBlink();
    }

    private void ApplyEdit(bool changed)
    {
        if (!changed)
        {
            return;
        }

        NotifyTextChanged();
        _editedThisFrame = true;
        ResetCaretBlink();
    }

    public void CaptureState(UiElementState state)
    {
        state.Text = Text;
        state.CaretIndex = CaretIndex;
        state.SelectionAnchor = _editingState.SelectionAnchor;
        state.SelectionStart = SelectionStart;
        state.SelectionEnd = SelectionEnd;
        state.ScrollX = _horizontalScrollOffset;
    }

    public void ApplyState(UiElementState state)
    {
        if (state.Text != null)
        {
            Text = state.Text;
        }

        if (state.SelectionAnchor.HasValue && state.CaretIndex.HasValue)
        {
            _editingState.SelectRange(state.SelectionAnchor.Value, state.CaretIndex.Value);
        }
        else if (state.SelectionStart.HasValue && state.SelectionEnd.HasValue)
        {
            _editingState.SelectRange(state.SelectionStart.Value, state.SelectionEnd.Value);
        }
        else if (state.CaretIndex.HasValue)
        {
            _editingState.SetCaret(state.CaretIndex.Value);
        }

        if (state.ScrollX.HasValue)
        {
            _horizontalScrollOffset = Math.Max(0, state.ScrollX.Value);
        }
    }

    private void NotifyTextChanged()
    {
        TextChanged?.Invoke(Text);
    }

    private void UpdateHorizontalScroll(IUiRenderer renderer, string renderText, int clipWidth, UiFont font)
    {
        if (clipWidth <= 0)
        {
            _horizontalScrollOffset = 0;
            return;
        }

        int fullWidth = renderer.MeasureTextWidth(renderText, TextScale, font);
        if (fullWidth <= clipWidth)
        {
            _horizontalScrollOffset = 0;
            return;
        }

        int caretX = MeasurePrefixWidth(renderer, renderText, CaretIndex, TextScale, font);
        if (caretX < _horizontalScrollOffset)
        {
            _horizontalScrollOffset = caretX;
        }
        else if (caretX > _horizontalScrollOffset + clipWidth - 2)
        {
            _horizontalScrollOffset = caretX - clipWidth + 2;
        }

        _horizontalScrollOffset = Math.Clamp(_horizontalScrollOffset, 0, Math.Max(0, fullWidth - clipWidth + 2));
    }

    private void DrawComposition(IUiRenderer renderer, string renderText, int textX, int textY, UiFont font)
    {
        int compositionX = textX - _horizontalScrollOffset + MeasurePrefixWidth(renderer, renderText, CaretIndex, TextScale, font);
        renderer.DrawText(_composition.Text, new UiPoint(compositionX, textY), CompositionTextColor, TextScale, font);
        int width = Math.Max(1, renderer.MeasureTextWidth(_composition.Text, TextScale, font));
        int underlineY = textY + renderer.MeasureTextHeight(TextScale, font) - 1;
        renderer.FillRect(new UiRect(compositionX, underlineY, width, 1), CompositionUnderlineColor);
    }

    private UiRect GetCaretBounds(UiFont font)
    {
        string renderText = GetDisplayText();
        int textX = Bounds.X + Padding;
        int textY = GetTextTopY(font);
        int caretHeight = font.MeasureTextHeight(TextScale);
        int caretWidth = Math.Max(1, Math.Min(2, TextScale));
        int caretX = textX - _horizontalScrollOffset + MeasurePrefixWidth(renderText, CaretIndex, TextScale, font);
        return new UiRect(caretX, textY, caretWidth, caretHeight);
    }

    private int GetTextTopY(UiFont font)
    {
        int padding = Math.Max(0, Padding);
        int innerHeight = Math.Max(0, Bounds.Height - padding * 2);
        int textHeight = font.MeasureTextHeight(TextScale);
        int centeredOffset = Math.Max(0, (innerHeight - textHeight) / 2);
        return Bounds.Y + padding + centeredOffset;
    }

    private int MeasureCompositionWidth(UiFont font)
    {
        return _composition.IsActive
            ? Math.Max(1, font.MeasureTextWidth(_composition.Text, TextScale))
            : Math.Max(1, Math.Min(2, TextScale));
    }

    private static int MeasurePrefixWidth(IUiRenderer renderer, string text, int index, int textScale, UiFont font)
    {
        int clampedIndex = Math.Clamp(index, 0, text.Length);
        if (clampedIndex <= 0)
        {
            return 0;
        }

        return renderer.MeasureTextWidth(text.Substring(0, clampedIndex), textScale, font);
    }

    private static int MeasurePrefixWidth(string text, int index, int textScale, UiFont font)
    {
        int clampedIndex = Math.Clamp(index, 0, text.Length);
        if (clampedIndex <= 0)
        {
            return 0;
        }

        return font.MeasureTextWidth(text.Substring(0, clampedIndex), textScale);
    }

    private void ResetCaretBlink()
    {
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
