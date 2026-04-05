using System.Text;
using OpenControls.State;

namespace OpenControls.Controls;

public enum UiTextEditorSyntaxMode
{
    None,
    CSharp
}

public sealed class UiTextEditor : UiElement, IUiStatefulElement
{
    private enum CharacterClass
    {
        Whitespace,
        Word,
        Symbol
    }

    private sealed class TextEditorView : UiElement
    {
        private readonly UiTextEditor _owner;

        public TextEditorView(UiTextEditor owner)
        {
            _owner = owner;
        }

        public override bool IsFocusable => true;
        public override bool HandlesTabInput => _owner.AllowTabInput;
        public override bool WantsTextInput => ! _owner.ReadOnly;

        public override void Update(UiUpdateContext context)
        {
            if (!Visible || !Enabled)
            {
                return;
            }

            UiInputState input = context.Input;
            if (input.LeftClicked && Bounds.Contains(input.MousePosition))
            {
                _owner.HandlePointerPressed(context, input);
            }
            else
            {
                _owner.UpdatePointerSelection(input);
            }

            if (context.Focus.Focused == this)
            {
                _owner.HandleInput(context, input);
            }
        }

        public override void Render(UiRenderContext context)
        {
            if (!Visible)
            {
                return;
            }

            _owner.RenderEditor(context);
        }

        protected internal override void OnFocusGained()
        {
            _owner.SetFocusState(true);
        }

        protected internal override void OnFocusLost()
        {
            _owner.SetFocusState(false);
        }

        protected internal override bool TryGetMouseCursor(UiInputState input, bool focused, out UiMouseCursor cursor)
        {
            if (focused || Bounds.Contains(input.MousePosition))
            {
                cursor = UiMouseCursor.TextInput;
                return true;
            }

            cursor = UiMouseCursor.Arrow;
            return false;
        }

        protected internal override bool TryGetTextInputRequest(out UiTextInputRequest request)
        {
            if (_owner.ReadOnly)
            {
                request = default;
                return false;
            }

            request = _owner.BuildTextInputRequest(_owner._scrollPanel.ViewportBounds);
            return true;
        }

        protected internal override UiItemStatusFlags GetItemStatus(UiContext context, UiInputState input, bool focused, bool hovered)
        {
            UiItemStatusFlags status = base.GetItemStatus(context, input, focused, hovered);
            if (_owner._hasFocus)
            {
                status |= UiItemStatusFlags.Active;
            }

            if (_owner._clickedThisFrame)
            {
                status |= UiItemStatusFlags.Clicked;
            }

            if (_owner._editedThisFrame)
            {
                status |= UiItemStatusFlags.Edited;
            }

            return status;
        }
    }

    private readonly struct LineToken
    {
        public LineToken(int start, int length, UiColor color)
        {
            Start = start;
            Length = length;
            Color = color;
        }

        public int Start { get; }
        public int Length { get; }
        public UiColor Color { get; }
    }

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new",
        "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static",
        "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    };

    private readonly UiScrollPanel _scrollPanel;
    private readonly TextEditorView _view;
    private readonly UiTextEditingState _editingState = new();
    private readonly List<string> _lines = new();
    private readonly List<int> _lineStartIndices = new();
    private readonly List<List<LineToken>?> _tokenCache = new();
    private readonly List<bool> _lineStartsInBlockComment = new();
    private UiTextCompositionState _composition;

    private int _maxLineLength;
    private int _caretLine;
    private int _caretColumn;
    private int _preferredColumn;
    private int _lineHeight;
    private int _glyphWidth;
    private int _lineNumberWidth;
    private int _textStartX;
    private UiFont _layoutFont = UiFont.Default;
    private bool _hasFocus;
    private bool _clickedThisFrame;
    private bool _editedThisFrame;
    private bool _dragSelecting;
    private bool _dragSelectingWholeLines;
    private int _dragSelectionAnchor;
    private int _dragSelectionAnchorLine;
    private float _caretTimer;
    private bool _caretVisible = true;

    public UiTextEditor()
    {
        _scrollPanel = new UiScrollPanel
        {
            HorizontalScrollbar = UiScrollbarVisibility.Auto,
            VerticalScrollbar = UiScrollbarVisibility.Auto
        };
        _view = new TextEditorView(this);
        _scrollPanel.AddChild(_view);
        AddChild(_scrollPanel);
        RebuildDocumentFromText();
    }

    public UiColor Background { get; set; } = new UiColor(20, 22, 28);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public int BorderThickness { get; set; } = 1;
    public int CornerRadius { get; set; }

    public UiColor TextColor { get; set; } = new UiColor(220, 220, 230);
    public UiColor KeywordColor { get; set; } = new UiColor(86, 156, 214);
    public UiColor CommentColor { get; set; } = new UiColor(87, 166, 74);
    public UiColor StringColor { get; set; } = new UiColor(214, 157, 133);
    public UiColor NumberColor { get; set; } = new UiColor(181, 206, 168);
    public UiColor LineNumberColor { get; set; } = new UiColor(120, 130, 150);
    public UiColor LineNumberBackground { get; set; } = new UiColor(18, 20, 28);
    public UiColor CaretColor { get; set; } = UiColor.White;
    public UiColor CurrentLineHighlight { get; set; } = new UiColor(32, 36, 48, 140);
    public UiColor SelectionBackground { get; set; } = new UiColor(72, 114, 196, 180);
    public UiColor CompositionTextColor { get; set; } = new UiColor(224, 228, 240);
    public UiColor CompositionUnderlineColor { get; set; } = new UiColor(132, 156, 220);

    public bool HighlightCurrentLine { get; set; } = true;
    public bool ShowLineNumbers { get; set; } = true;
    public int TextScale { get; set; } = 2;
    public int Padding { get; set; } = 6;
    public int LineNumberPadding { get; set; } = 8;
    public int LineSpacing { get; set; } = 2;
    public int TabSize { get; set; } = 4;
    public bool AllowTabInput { get; set; } = true;
    public bool ReadOnly { get; set; }
    public int MinLineNumberDigits { get; set; } = 3;
    public UiTextEditorSyntaxMode SyntaxMode { get; set; } = UiTextEditorSyntaxMode.CSharp;

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

    public int ScrollWheelStep
    {
        get => _scrollPanel.ScrollWheelStep;
        set => _scrollPanel.ScrollWheelStep = value;
    }

    public int LineCount => _lines.Count;
    public int CaretLine => _caretLine;
    public int CaretColumn => _caretColumn;
    public int CaretIndex => _editingState.CaretIndex;
    public bool HasSelection => _editingState.HasSelection;
    public int SelectionStart => _editingState.SelectionStart;
    public int SelectionEnd => _editingState.SelectionEnd;
    public int SelectionLength => _editingState.SelectionLength;
    public string SelectedText => _editingState.GetSelectedText();
    public bool CanUndo => _editingState.CanUndo;
    public bool CanRedo => _editingState.CanRedo;
    public int ScrollX
    {
        get => _scrollPanel.ScrollX;
        set => _scrollPanel.ScrollX = Math.Max(0, value);
    }

    public int ScrollY
    {
        get => _scrollPanel.ScrollY;
        set => _scrollPanel.ScrollY = Math.Max(0, value);
    }

    public UiPoint ScrollOffset
    {
        get => _scrollPanel.ScrollOffset;
        set => _scrollPanel.ScrollOffset = new UiPoint(Math.Max(0, value.X), Math.Max(0, value.Y));
    }

    public UiTextCompositionState Composition => _composition;
    public override bool IsRenderCacheVolatile(UiContext context) => _hasFocus;

    public string Text
    {
        get => _editingState.Text;
        set => SetText(value);
    }

    public event Action? TextChanged;
    public event Action? CaretMoved;
    public event Action? SelectionChanged;

    public void SetText(string text)
    {
        string normalized = NormalizeText(text);
        int previousCaret = _editingState.CaretIndex;
        int previousSelectionStart = _editingState.SelectionStart;
        int previousSelectionEnd = _editingState.SelectionEnd;

        _editingState.SetText(normalized);
        RebuildDocumentFromText();
        UpdateCaretFromIndex(updatePreferredColumn: true);

        TextChanged?.Invoke();
        NotifyCaretAndSelectionChanges(previousCaret, previousSelectionStart, previousSelectionEnd);
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.Text | UiInvalidationReason.Layout | UiInvalidationReason.Paint | UiInvalidationReason.State);
    }

    public void SetCaretIndex(int index)
    {
        int previousCaret = _editingState.CaretIndex;
        int previousSelectionStart = _editingState.SelectionStart;
        int previousSelectionEnd = _editingState.SelectionEnd;
        _editingState.SetCaret(index);
        UpdateCaretFromIndex(updatePreferredColumn: true);
        EnsureCaretVisible();
        NotifyCaretAndSelectionChanges(previousCaret, previousSelectionStart, previousSelectionEnd);
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public void SelectAllText()
    {
        int previousCaret = _editingState.CaretIndex;
        int previousSelectionStart = _editingState.SelectionStart;
        int previousSelectionEnd = _editingState.SelectionEnd;
        _editingState.SelectAll();
        UpdateCaretFromIndex(updatePreferredColumn: true);
        EnsureCaretVisible();
        NotifyCaretAndSelectionChanges(previousCaret, previousSelectionStart, previousSelectionEnd);
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public void SelectRange(int anchorIndex, int caretIndex)
    {
        int previousCaret = _editingState.CaretIndex;
        int previousSelectionStart = _editingState.SelectionStart;
        int previousSelectionEnd = _editingState.SelectionEnd;
        _editingState.SelectRange(anchorIndex, caretIndex);
        UpdateCaretFromIndex(updatePreferredColumn: true);
        EnsureCaretVisible();
        NotifyCaretAndSelectionChanges(previousCaret, previousSelectionStart, previousSelectionEnd);
        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Layout | UiInvalidationReason.Paint);
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        _clickedThisFrame = false;
        _editedThisFrame = false;
        RefreshLayout(ResolveFont(context.DefaultFont));

        _scrollPanel.Background = Background;
        _scrollPanel.Border = Border;
        _scrollPanel.BorderThickness = BorderThickness;
        _scrollPanel.CornerRadius = CornerRadius;
        _scrollPanel.Bounds = Bounds;
        _scrollPanel.Update(context);

        if (_hasFocus)
        {
            UpdateCaretBlink(context.DeltaSeconds);
        }
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        _scrollPanel.Render(context);
    }

    public override void RenderOverlay(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        _scrollPanel.RenderOverlay(context);
    }

    private void RefreshLayout(UiFont font)
    {
        _layoutFont = font;
        _glyphWidth = GetGlyphWidth(font, TextScale);
        _lineHeight = GetLineHeight(font, TextScale) + Math.Max(0, LineSpacing);

        int digits = Math.Max(MinLineNumberDigits, _lines.Count.ToString().Length);
        _lineNumberWidth = ShowLineNumbers
            ? font.MeasureTextWidth(new string('0', digits), TextScale) + LineNumberPadding * 2
            : 0;
        _textStartX = Padding + _lineNumberWidth;

        int contentWidth = _textStartX + GetMaxLineWidth() + Padding;
        int contentHeight = Padding * 2 + _lines.Count * _lineHeight;
        _view.Bounds = new UiRect(0, 0, contentWidth, contentHeight);
    }

    private void HandlePointerPressed(UiUpdateContext context, UiInputState input)
    {
        _clickedThisFrame = true;
        bool hadFocus = _hasFocus;
        context.Focus.RequestFocus(_view);

        int previousCaret = _editingState.CaretIndex;
        int previousSelectionStart = _editingState.SelectionStart;
        int previousSelectionEnd = _editingState.SelectionEnd;

        if (ShowLineNumbers && input.MousePosition.X < _textStartX)
        {
            int line = GetLineFromPoint(input.MousePosition.Y);
            if (input.ShiftDown && hadFocus)
            {
                SelectWholeLineRange(GetLineFromIndex(_editingState.SelectionAnchor), line);
            }
            else
            {
                SelectWholeLineRange(line, line);
            }

            _dragSelectionAnchorLine = input.ShiftDown && hadFocus
                ? GetLineFromIndex(_editingState.SelectionAnchor)
                : line;
            _dragSelecting = true;
            _dragSelectingWholeLines = true;
        }
        else
        {
            int index = GetTextIndexFromPoint(input.MousePosition);
            if (input.LeftDoubleClicked)
            {
                _editingState.SelectWordAt(index);
                _dragSelecting = false;
                _dragSelectingWholeLines = false;
            }
            else
            {
                _editingState.SetCaret(index, extendSelection: input.ShiftDown && hadFocus);
                _dragSelectionAnchor = _editingState.SelectionAnchor;
                _dragSelecting = true;
                _dragSelectingWholeLines = false;
            }
        }

        UpdateCaretFromIndex(updatePreferredColumn: true);
        EnsureCaretVisible();
        NotifyCaretAndSelectionChanges(previousCaret, previousSelectionStart, previousSelectionEnd);
        ResetCaretBlink();
    }

    private void UpdatePointerSelection(UiInputState input)
    {
        if (!_dragSelecting)
        {
            return;
        }

        if (!input.LeftDown)
        {
            _dragSelecting = false;
            _dragSelectingWholeLines = false;
            return;
        }

        int previousCaret = _editingState.CaretIndex;
        int previousSelectionStart = _editingState.SelectionStart;
        int previousSelectionEnd = _editingState.SelectionEnd;

        if (_dragSelectingWholeLines)
        {
            int line = GetLineFromPoint(input.MousePosition.Y);
            SelectWholeLineRange(_dragSelectionAnchorLine, line);
        }
        else
        {
            int index = GetTextIndexFromPoint(input.MousePosition);
            _editingState.SelectRange(_dragSelectionAnchor, index);
        }

        UpdateCaretFromIndex(updatePreferredColumn: true);
        EnsureCaretVisible();
        NotifyCaretAndSelectionChanges(previousCaret, previousSelectionStart, previousSelectionEnd);
        ResetCaretBlink();
    }

    private void HandleInput(UiUpdateContext context, UiInputState input)
    {
        _composition = input.Composition;
        int previousCaret = _editingState.CaretIndex;
        int previousSelectionStart = _editingState.SelectionStart;
        int previousSelectionEnd = _editingState.SelectionEnd;
        bool textChanged = false;
        bool caretOrSelectionChanged = false;

        caretOrSelectionChanged |= HandleShortcutInput(context, input, ref textChanged);

        bool extendSelection = input.ShiftDown;
        bool byWord = input.CtrlDown || input.AltDown;

        if (input.Navigation.MoveLeft)
        {
            if (input.SuperDown && !byWord)
            {
                MoveLineHome(extendSelection, smartHome: true);
            }
            else
            {
                MoveLeft(extendSelection, byWord);
            }

            caretOrSelectionChanged = true;
        }

        if (input.Navigation.MoveRight)
        {
            if (input.SuperDown && !byWord)
            {
                MoveLineEnd(extendSelection);
            }
            else
            {
                MoveRight(extendSelection, byWord);
            }

            caretOrSelectionChanged = true;
        }

        if (input.Navigation.MoveUp)
        {
            if (input.SuperDown)
            {
                MoveDocumentBoundary(toEnd: false, extendSelection);
            }
            else
            {
                MoveVertical(-1, extendSelection);
            }

            caretOrSelectionChanged = true;
        }

        if (input.Navigation.MoveDown)
        {
            if (input.SuperDown)
            {
                MoveDocumentBoundary(toEnd: true, extendSelection);
            }
            else
            {
                MoveVertical(1, extendSelection);
            }

            caretOrSelectionChanged = true;
        }

        if (input.Navigation.Home)
        {
            if (input.CtrlDown || input.SuperDown)
            {
                MoveDocumentBoundary(toEnd: false, extendSelection);
            }
            else
            {
                MoveLineHome(extendSelection, smartHome: true);
            }

            caretOrSelectionChanged = true;
        }

        if (input.Navigation.End)
        {
            if (input.CtrlDown || input.SuperDown)
            {
                MoveDocumentBoundary(toEnd: true, extendSelection);
            }
            else
            {
                MoveLineEnd(extendSelection);
            }

            caretOrSelectionChanged = true;
        }

        if (input.Navigation.PageUp)
        {
            MovePage(-1, extendSelection);
            caretOrSelectionChanged = true;
        }

        if (input.Navigation.PageDown)
        {
            MovePage(1, extendSelection);
            caretOrSelectionChanged = true;
        }

        if (!ReadOnly && input.Navigation.Backspace)
        {
            textChanged |= Backspace(byWord);
        }

        if (!ReadOnly && input.Navigation.Delete)
        {
            textChanged |= Delete(byWord);
        }

        if (!ReadOnly && AllowTabInput && input.Navigation.Tab)
        {
            textChanged |= InsertText(new string(' ', Math.Max(1, TabSize)));
        }

        if (!ReadOnly && (input.Navigation.Enter || input.Navigation.KeypadEnter))
        {
            textChanged |= InsertText("\n");
        }

        if (!ReadOnly && input.TextInput.Count > 0)
        {
            textChanged |= HandleTextInput(input.TextInput);
        }

        if (textChanged)
        {
            RebuildDocumentFromText();
            UpdateCaretFromIndex(updatePreferredColumn: true);
            _editedThisFrame = true;
            TextChanged?.Invoke();
        }
        else if (caretOrSelectionChanged)
        {
            UpdateCaretFromIndex(updatePreferredColumn: false);
        }

        if (textChanged || caretOrSelectionChanged)
        {
            EnsureCaretVisible();
            NotifyCaretAndSelectionChanges(previousCaret, previousSelectionStart, previousSelectionEnd);
            ResetCaretBlink();
        }
    }

    private bool HandleShortcutInput(UiUpdateContext context, UiInputState input, ref bool textChanged)
    {
        bool changed = false;
        if (input.IsPrimaryShortcutPressed(UiKey.A))
        {
            _editingState.SelectAll();
            changed = true;
        }

        if (input.IsPrimaryShortcutPressed(UiKey.C))
        {
            CopySelection(context.Clipboard);
        }

        if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.X))
        {
            textChanged |= CutSelection(context.Clipboard);
        }

        if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.V))
        {
            textChanged |= PasteFromClipboard(context.Clipboard);
        }

        if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.Z, shift: true))
        {
            textChanged |= _editingState.Redo();
        }
        else if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.Z))
        {
            textChanged |= _editingState.Undo();
        }
        else if (!ReadOnly && input.IsPrimaryShortcutPressed(UiKey.Y))
        {
            textChanged |= _editingState.Redo();
        }

        return changed;
    }

    private bool HandleTextInput(IReadOnlyList<char> characters)
    {
        if (characters.Count == 0)
        {
            return false;
        }

        StringBuilder builder = new();
        for (int i = 0; i < characters.Count; i++)
        {
            char character = characters[i];
            if (character == '\r' || character == '\n')
            {
                builder.Append('\n');
            }
            else if (character == '\t')
            {
                builder.Append(new string(' ', Math.Max(1, TabSize)));
            }
            else if (!char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        return builder.Length > 0 && InsertText(builder.ToString());
    }

    private bool InsertText(string text)
    {
        string normalized = NormalizeText(text);
        return !string.IsNullOrEmpty(normalized) && _editingState.InsertText(normalized);
    }

    private bool Backspace(bool byWord)
    {
        if (!byWord)
        {
            return _editingState.Backspace();
        }

        if (_editingState.HasSelection)
        {
            return _editingState.DeleteSelection();
        }

        int start = FindPreviousBoundary(Text, _editingState.CaretIndex);
        if (start == _editingState.CaretIndex)
        {
            return false;
        }

        _editingState.SelectRange(start, _editingState.CaretIndex);
        return _editingState.DeleteSelection();
    }

    private bool Delete(bool byWord)
    {
        if (!byWord)
        {
            return _editingState.Delete();
        }

        if (_editingState.HasSelection)
        {
            return _editingState.DeleteSelection();
        }

        int end = FindNextBoundary(Text, _editingState.CaretIndex);
        if (end == _editingState.CaretIndex)
        {
            return false;
        }

        _editingState.SelectRange(_editingState.CaretIndex, end);
        return _editingState.DeleteSelection();
    }

    private void CopySelection(IUiClipboard clipboard)
    {
        if (!_editingState.HasSelection)
        {
            return;
        }

        clipboard.SetText(_editingState.GetSelectedText());
    }

    private bool CutSelection(IUiClipboard clipboard)
    {
        if (!_editingState.HasSelection)
        {
            return false;
        }

        clipboard.SetText(_editingState.GetSelectedText());
        return _editingState.DeleteSelection();
    }

    private bool PasteFromClipboard(IUiClipboard clipboard)
    {
        return InsertText(clipboard.GetText());
    }

    private void MoveLeft(bool extendSelection, bool byWord)
    {
        if (!extendSelection && _editingState.HasSelection)
        {
            _editingState.SetCaret(_editingState.SelectionStart);
            UpdateCaretFromIndex(updatePreferredColumn: true);
            return;
        }

        int next = byWord
            ? FindPreviousBoundary(Text, _editingState.CaretIndex)
            : Math.Max(0, _editingState.CaretIndex - 1);
        _editingState.SetCaret(next, extendSelection);
        UpdateCaretFromIndex(updatePreferredColumn: true);
    }

    private void MoveRight(bool extendSelection, bool byWord)
    {
        if (!extendSelection && _editingState.HasSelection)
        {
            _editingState.SetCaret(_editingState.SelectionEnd);
            UpdateCaretFromIndex(updatePreferredColumn: true);
            return;
        }

        int next = byWord
            ? FindNextBoundary(Text, _editingState.CaretIndex)
            : Math.Min(Text.Length, _editingState.CaretIndex + 1);
        _editingState.SetCaret(next, extendSelection);
        UpdateCaretFromIndex(updatePreferredColumn: true);
    }

    private void MoveVertical(int deltaLines, bool extendSelection)
    {
        int nextLine = Math.Clamp(_caretLine + deltaLines, 0, _lines.Count - 1);
        int nextColumn = Math.Min(_lines[nextLine].Length, _preferredColumn);
        _editingState.SetCaret(GetTextIndex(nextLine, nextColumn), extendSelection);
        UpdateCaretFromIndex(updatePreferredColumn: false);
    }

    private void MovePage(int direction, bool extendSelection)
    {
        int viewportHeight = Math.Max(1, _scrollPanel.ViewportBounds.Height - Padding * 2);
        int linesPerPage = Math.Max(1, viewportHeight / Math.Max(1, _lineHeight));
        int nextLine = Math.Clamp(_caretLine + direction * linesPerPage, 0, _lines.Count - 1);
        int nextColumn = Math.Min(_lines[nextLine].Length, _preferredColumn);
        _editingState.SetCaret(GetTextIndex(nextLine, nextColumn), extendSelection);
        UpdateCaretFromIndex(updatePreferredColumn: false);
        _scrollPanel.ScrollY = Math.Max(0, _scrollPanel.ScrollY + direction * viewportHeight);
    }

    private void MoveLineHome(bool extendSelection, bool smartHome)
    {
        string line = _lines[_caretLine];
        int firstNonWhitespace = 0;
        while (firstNonWhitespace < line.Length && char.IsWhiteSpace(line[firstNonWhitespace]))
        {
            firstNonWhitespace++;
        }

        int targetColumn = 0;
        if (smartHome && _caretColumn != firstNonWhitespace)
        {
            targetColumn = firstNonWhitespace;
        }

        _editingState.SetCaret(GetTextIndex(_caretLine, targetColumn), extendSelection);
        UpdateCaretFromIndex(updatePreferredColumn: true);
    }

    private void MoveLineEnd(bool extendSelection)
    {
        _editingState.SetCaret(GetTextIndex(_caretLine, _lines[_caretLine].Length), extendSelection);
        UpdateCaretFromIndex(updatePreferredColumn: true);
    }

    private void MoveDocumentBoundary(bool toEnd, bool extendSelection)
    {
        if (toEnd)
        {
            _editingState.MoveEnd(extendSelection);
        }
        else
        {
            _editingState.MoveHome(extendSelection);
        }

        UpdateCaretFromIndex(updatePreferredColumn: true);
    }

    private void SelectWholeLineRange(int anchorLine, int caretLine)
    {
        anchorLine = Math.Clamp(anchorLine, 0, _lines.Count - 1);
        caretLine = Math.Clamp(caretLine, 0, _lines.Count - 1);

        if (caretLine >= anchorLine)
        {
            _editingState.SelectRange(GetLineStartIndex(anchorLine), GetLineSelectionEndIndex(caretLine));
        }
        else
        {
            _editingState.SelectRange(GetLineSelectionEndIndex(anchorLine), GetLineStartIndex(caretLine));
        }
    }

    private int GetTextIndexFromPoint(UiPoint point)
    {
        int line = GetLineFromPoint(point.Y);
        int localX = point.X - _textStartX;
        int column = FindColumnFromPoint(_lines[line], localX);
        return GetTextIndex(line, column);
    }

    private int GetLineFromPoint(int y)
    {
        int lineIndex = (y - Padding) / Math.Max(1, _lineHeight);
        return Math.Clamp(lineIndex, 0, _lines.Count - 1);
    }

    private int GetTextIndex(int line, int column)
    {
        int clampedLine = Math.Clamp(line, 0, _lines.Count - 1);
        int clampedColumn = Math.Clamp(column, 0, _lines[clampedLine].Length);
        return _lineStartIndices[clampedLine] + clampedColumn;
    }

    private int GetLineStartIndex(int line)
    {
        return _lineStartIndices[Math.Clamp(line, 0, _lineStartIndices.Count - 1)];
    }

    private int GetLineEndIndex(int line)
    {
        int clampedLine = Math.Clamp(line, 0, _lines.Count - 1);
        return _lineStartIndices[clampedLine] + _lines[clampedLine].Length;
    }

    private int GetLineSelectionEndIndex(int line)
    {
        int end = GetLineEndIndex(line);
        if (line < _lines.Count - 1)
        {
            return Math.Min(Text.Length, end + 1);
        }

        return end;
    }

    private int GetLineFromIndex(int index)
    {
        int clamped = Math.Clamp(index, 0, Text.Length);
        int low = 0;
        int high = _lineStartIndices.Count - 1;
        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            int lineStart = _lineStartIndices[mid];
            int nextStart = mid + 1 < _lineStartIndices.Count ? _lineStartIndices[mid + 1] : Text.Length + 1;
            if (clamped < lineStart)
            {
                high = mid - 1;
            }
            else if (clamped >= nextStart)
            {
                low = mid + 1;
            }
            else
            {
                return mid;
            }
        }

        return Math.Clamp(low, 0, _lineStartIndices.Count - 1);
    }

    private void UpdateCaretFromIndex(bool updatePreferredColumn)
    {
        int line = GetLineFromIndex(_editingState.CaretIndex);
        int column = _editingState.CaretIndex - GetLineStartIndex(line);
        _caretLine = Math.Clamp(line, 0, _lines.Count - 1);
        _caretColumn = Math.Clamp(column, 0, _lines[_caretLine].Length);
        if (updatePreferredColumn)
        {
            _preferredColumn = _caretColumn;
        }
    }

    private void RenderEditor(UiRenderContext context)
    {
        UiFont font = ResolveFont(context.DefaultFont);
        _layoutFont = font;
        int viewTop = _scrollPanel.ScrollY;
        int viewBottom = viewTop + _scrollPanel.ViewportBounds.Height;
        int lineHeight = Math.Max(1, _lineHeight);
        int firstLine = Math.Clamp(viewTop / lineHeight, 0, _lines.Count - 1);
        int lastLine = Math.Clamp((viewBottom + lineHeight - 1) / lineHeight, 0, _lines.Count - 1);

        if (LineNumberBackground.A > 0 && ShowLineNumbers)
        {
            UiRect lineNumberRect = new UiRect(0, 0, _lineNumberWidth + Padding, _view.Bounds.Height);
            context.Renderer.FillRect(lineNumberRect, LineNumberBackground);
        }

        for (int i = firstLine; i <= lastLine; i++)
        {
            int lineY = Padding + i * lineHeight;

            if (HighlightCurrentLine && i == _caretLine && CurrentLineHighlight.A > 0)
            {
                context.Renderer.FillRect(new UiRect(0, lineY, _view.Bounds.Width, lineHeight), CurrentLineHighlight);
            }

            DrawSelection(context, i, lineY, lineHeight);

            if (ShowLineNumbers)
            {
                string lineNumber = (i + 1).ToString();
                int numberWidth = font.MeasureTextWidth(lineNumber, TextScale);
                int numberX = Math.Max(0, _lineNumberWidth - LineNumberPadding - numberWidth);
                context.Renderer.DrawText(lineNumber, new UiPoint(numberX, lineY), LineNumberColor, TextScale, font);
            }

            string lineText = _lines[i];
            context.Renderer.DrawText(lineText, new UiPoint(_textStartX, lineY), TextColor, TextScale, font);

            if (SyntaxMode != UiTextEditorSyntaxMode.None)
            {
                List<LineToken> tokens = GetLineTokens(i);
                foreach (LineToken token in tokens)
                {
                    if (token.Length <= 0)
                    {
                        continue;
                    }

                    string segment = lineText.Substring(token.Start, token.Length);
                    int tokenX = _textStartX + MeasureLinePrefixWidth(lineText, token.Start);
                    context.Renderer.DrawText(segment, new UiPoint(tokenX, lineY), token.Color, TextScale, font);
                }
            }
        }

        if (_hasFocus && _caretVisible)
        {
            string line = _lines[_caretLine];
            int caretX = _textStartX + MeasureLinePrefixWidth(line, _caretColumn);
            int caretY = Padding + _caretLine * lineHeight;
            int caretWidth = GetCaretWidth(line, _caretColumn);
            if (_composition.IsActive)
            {
                DrawComposition(context, caretX, caretY, lineHeight);
            }

            context.Renderer.FillRect(new UiRect(caretX, caretY, caretWidth, lineHeight), CaretColor);
        }
    }

    private void DrawSelection(UiRenderContext context, int lineIndex, int lineY, int lineHeight)
    {
        if (!_editingState.HasSelection)
        {
            return;
        }

        int lineStart = GetLineStartIndex(lineIndex);
        int lineSelectionEnd = GetLineSelectionEndIndex(lineIndex);
        if (_editingState.SelectionEnd <= lineStart || _editingState.SelectionStart >= lineSelectionEnd)
        {
            return;
        }

        string line = _lines[lineIndex];
        int lineEnd = GetLineEndIndex(lineIndex);
        int startColumn = Math.Clamp(_editingState.SelectionStart - lineStart, 0, line.Length);
        int endColumn = Math.Clamp(_editingState.SelectionEnd - lineStart, 0, line.Length);

        if (_editingState.SelectionStart < lineStart)
        {
            startColumn = 0;
        }

        if (_editingState.SelectionEnd > lineEnd && lineIndex < _lines.Count - 1)
        {
            endColumn = line.Length;
        }

        int selectionX = _textStartX + MeasureLinePrefixWidth(line, startColumn);
        int selectionEndX = _textStartX + MeasureLinePrefixWidth(line, endColumn);
        int selectionWidth = selectionEndX - selectionX;
        if (selectionWidth <= 0)
        {
            selectionWidth = Math.Max(1, GetCaretWidth(line, startColumn));
        }

        context.Renderer.FillRect(new UiRect(selectionX, lineY, selectionWidth, lineHeight), SelectionBackground);
    }

    public void CaptureState(UiElementState state)
    {
        state.Text = Text;
        state.CaretIndex = CaretIndex;
        state.SelectionAnchor = _editingState.SelectionAnchor;
        state.SelectionStart = SelectionStart;
        state.SelectionEnd = SelectionEnd;
        state.ScrollX = _scrollPanel.ScrollX;
        state.ScrollY = _scrollPanel.ScrollY;
    }

    public void ApplyState(UiElementState state)
    {
        if (state.Text != null)
        {
            SetText(state.Text);
        }

        if (state.SelectionAnchor.HasValue && state.CaretIndex.HasValue)
        {
            _editingState.SelectRange(state.SelectionAnchor.Value, state.CaretIndex.Value);
            UpdateCaretFromIndex(updatePreferredColumn: true);
        }
        else if (state.SelectionStart.HasValue && state.SelectionEnd.HasValue)
        {
            SelectRange(state.SelectionStart.Value, state.SelectionEnd.Value);
        }
        else if (state.CaretIndex.HasValue)
        {
            SetCaretIndex(state.CaretIndex.Value);
        }

        if (state.ScrollX.HasValue)
        {
            _scrollPanel.ScrollX = Math.Max(0, state.ScrollX.Value);
        }

        if (state.ScrollY.HasValue)
        {
            _scrollPanel.ScrollY = Math.Max(0, state.ScrollY.Value);
        }
    }

    private UiTextInputRequest BuildTextInputRequest(UiRect bounds)
    {
        UiRect caretBounds = GetCaretBounds(bounds);
        UiRect candidateBounds = _composition.IsActive
            ? new UiRect(caretBounds.X, caretBounds.Y, Math.Max(caretBounds.Width, MeasureCompositionWidth()), caretBounds.Height)
            : caretBounds;
        return new UiTextInputRequest(bounds, isMultiLine: true, caretBounds: caretBounds, candidateBounds: candidateBounds);
    }

    private void DrawComposition(UiRenderContext context, int caretX, int caretY, int lineHeight)
    {
        context.Renderer.DrawText(_composition.Text, new UiPoint(caretX, caretY), CompositionTextColor, TextScale, _layoutFont);
        int width = Math.Max(1, MeasureCompositionWidth());
        int underlineY = caretY + lineHeight - 1;
        context.Renderer.FillRect(new UiRect(caretX, underlineY, width, 1), CompositionUnderlineColor);
    }

    private List<LineToken> GetLineTokens(int lineIndex)
    {
        List<LineToken>? cached = _tokenCache[lineIndex];
        if (cached != null)
        {
            return cached;
        }

        List<LineToken> tokens = new();
        bool inBlockComment = _lineStartsInBlockComment[lineIndex];
        string line = _lines[lineIndex];
        int index = 0;

        while (index < line.Length)
        {
            if (inBlockComment)
            {
                int end = line.IndexOf("*/", index, StringComparison.Ordinal);
                if (end < 0)
                {
                    tokens.Add(new LineToken(index, line.Length - index, CommentColor));
                    break;
                }

                int length = end + 2 - index;
                tokens.Add(new LineToken(index, length, CommentColor));
                index = end + 2;
                inBlockComment = false;
                continue;
            }

            char c = line[index];
            if (c == '/' && index + 1 < line.Length && line[index + 1] == '/')
            {
                tokens.Add(new LineToken(index, line.Length - index, CommentColor));
                break;
            }

            if (c == '/' && index + 1 < line.Length && line[index + 1] == '*')
            {
                int end = line.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    tokens.Add(new LineToken(index, line.Length - index, CommentColor));
                    inBlockComment = true;
                    break;
                }

                int length = end + 2 - index;
                tokens.Add(new LineToken(index, length, CommentColor));
                index = end + 2;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                int end = FindStringEnd(line, index, c);
                int length = Math.Max(1, end - index);
                tokens.Add(new LineToken(index, length, StringColor));
                index = end;
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = index;
                index++;
                while (index < line.Length && IsNumberChar(line[index]))
                {
                    index++;
                }

                tokens.Add(new LineToken(start, index - start, NumberColor));
                continue;
            }

            if (IsIdentifierStart(c))
            {
                int start = index;
                index++;
                while (index < line.Length && IsIdentifierChar(line[index]))
                {
                    index++;
                }

                if (SyntaxMode == UiTextEditorSyntaxMode.CSharp)
                {
                    string word = line.Substring(start, index - start);
                    if (CSharpKeywords.Contains(word))
                    {
                        tokens.Add(new LineToken(start, index - start, KeywordColor));
                    }
                }

                continue;
            }

            index++;
        }

        _tokenCache[lineIndex] = tokens;
        return tokens;
    }

    private void RebuildDocumentFromText()
    {
        _lines.Clear();
        _lineStartIndices.Clear();

        string[] parts = Text.Split('\n');
        int offset = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            _lines.Add(parts[i]);
            _lineStartIndices.Add(offset);
            offset += parts[i].Length;
            if (i < parts.Length - 1)
            {
                offset++;
            }
        }

        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
            _lineStartIndices.Add(0);
        }

        RebuildCaches();
    }

    private void RebuildCaches()
    {
        _maxLineLength = 0;
        _tokenCache.Clear();
        _lineStartsInBlockComment.Clear();

        bool inBlockComment = false;
        foreach (string line in _lines)
        {
            _maxLineLength = Math.Max(_maxLineLength, line.Length);
            _tokenCache.Add(null);
            _lineStartsInBlockComment.Add(inBlockComment);
            inBlockComment = AdvanceBlockCommentState(line, inBlockComment);
        }
    }

    private UiRect GetCaretBounds(UiRect requestBounds)
    {
        string line = _lines[_caretLine];
        int caretX = _textStartX + MeasureLinePrefixWidth(line, _caretColumn);
        int caretY = Padding + _caretLine * _lineHeight;
        int caretWidth = GetCaretWidth(line, _caretColumn);
        int screenX = requestBounds.X + caretX - _scrollPanel.ScrollX;
        int screenY = requestBounds.Y + caretY - _scrollPanel.ScrollY;
        return new UiRect(screenX, screenY, caretWidth, Math.Max(1, _lineHeight));
    }

    private int MeasureCompositionWidth()
    {
        return _composition.IsActive
            ? Math.Max(1, _layoutFont.MeasureTextWidth(_composition.Text, TextScale))
            : 1;
    }

    private void EnsureCaretVisible()
    {
        string line = _lines[_caretLine];
        int caretX = _textStartX + MeasureLinePrefixWidth(line, _caretColumn);
        int caretY = Padding + _caretLine * _lineHeight;
        int caretWidth = GetCaretWidth(line, _caretColumn);
        int viewWidth = Math.Max(1, _scrollPanel.ViewportBounds.Width);
        int viewHeight = Math.Max(1, _scrollPanel.ViewportBounds.Height);

        int scrollX = _scrollPanel.ScrollX;
        int scrollY = _scrollPanel.ScrollY;

        if (caretX < scrollX)
        {
            scrollX = caretX;
        }
        else if (caretX + caretWidth > scrollX + viewWidth)
        {
            scrollX = caretX + caretWidth - viewWidth;
        }

        if (caretY < scrollY)
        {
            scrollY = caretY;
        }
        else if (caretY + _lineHeight > scrollY + viewHeight)
        {
            scrollY = caretY + _lineHeight - viewHeight;
        }

        _scrollPanel.ScrollX = Math.Max(0, scrollX);
        _scrollPanel.ScrollY = Math.Max(0, scrollY);
    }

    private int GetMaxLineWidth()
    {
        int maxWidth = 0;
        foreach (string line in _lines)
        {
            maxWidth = Math.Max(maxWidth, _layoutFont.MeasureTextWidth(line, TextScale));
        }

        return maxWidth;
    }

    private int MeasureLinePrefixWidth(string line, int column)
    {
        if (string.IsNullOrEmpty(line) || column <= 0)
        {
            return 0;
        }

        int clampedColumn = Math.Clamp(column, 0, line.Length);
        return _layoutFont.MeasureTextWidth(line.Substring(0, clampedColumn), TextScale);
    }

    private int FindColumnFromPoint(string line, int localX)
    {
        if (string.IsNullOrEmpty(line) || localX <= 0)
        {
            return 0;
        }

        int previousWidth = 0;
        for (int column = 1; column <= line.Length; column++)
        {
            int currentWidth = MeasureLinePrefixWidth(line, column);
            int midpoint = previousWidth + (currentWidth - previousWidth) / 2;
            if (localX < midpoint)
            {
                return column - 1;
            }

            previousWidth = currentWidth;
        }

        return line.Length;
    }

    private int GetCaretWidth(string line, int column)
    {
        if (column >= 0 && column < line.Length)
        {
            int currentWidth = MeasureLinePrefixWidth(line, column);
            int nextWidth = MeasureLinePrefixWidth(line, column + 1);
            return Math.Max(Math.Max(1, TextScale), nextWidth - currentWidth);
        }

        return Math.Max(Math.Max(1, TextScale), _glyphWidth);
    }

    private void NotifyCaretAndSelectionChanges(int previousCaret, int previousSelectionStart, int previousSelectionEnd)
    {
        if (_editingState.CaretIndex != previousCaret)
        {
            CaretMoved?.Invoke();
        }

        if (_editingState.SelectionStart != previousSelectionStart || _editingState.SelectionEnd != previousSelectionEnd)
        {
            SelectionChanged?.Invoke();
        }
    }

    private void SetFocusState(bool focused)
    {
        _hasFocus = focused;
        if (!focused)
        {
            _dragSelecting = false;
            _dragSelectingWholeLines = false;
            _caretVisible = false;
            _caretTimer = 0f;
            _composition = UiTextCompositionState.Empty;
            Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Volatility);
            return;
        }

        ResetCaretBlink();
        Invalidate(UiInvalidationReason.State | UiInvalidationReason.Paint | UiInvalidationReason.Volatility);
    }

    private void UpdateCaretBlink(float deltaSeconds)
    {
        _caretTimer += Math.Max(0f, deltaSeconds);
        if (_caretTimer >= 0.5f)
        {
            _caretTimer = 0f;
            _caretVisible = !_caretVisible;
        }
    }

    private void ResetCaretBlink()
    {
        _caretTimer = 0f;
        _caretVisible = true;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);

        if (TabSize > 0 && normalized.Contains('\t'))
        {
            normalized = normalized.Replace("\t", new string(' ', TabSize), StringComparison.Ordinal);
        }

        return normalized;
    }

    private static int FindPreviousBoundary(string text, int index)
    {
        int i = Math.Clamp(index, 0, text.Length);
        if (i <= 0)
        {
            return 0;
        }

        i--;
        while (i > 0 && char.IsWhiteSpace(text[i]))
        {
            i--;
        }

        CharacterClass kind = Classify(text[i]);
        while (i > 0 && Classify(text[i - 1]) == kind && kind != CharacterClass.Whitespace)
        {
            i--;
        }

        return i;
    }

    private static int FindNextBoundary(string text, int index)
    {
        int i = Math.Clamp(index, 0, text.Length);
        if (i >= text.Length)
        {
            return text.Length;
        }

        CharacterClass kind = Classify(text[i]);
        while (i < text.Length && Classify(text[i]) == kind && kind != CharacterClass.Whitespace)
        {
            i++;
        }

        while (i < text.Length && char.IsWhiteSpace(text[i]))
        {
            i++;
        }

        return i;
    }

    private static CharacterClass Classify(char character)
    {
        if (char.IsWhiteSpace(character))
        {
            return CharacterClass.Whitespace;
        }

        if (char.IsLetterOrDigit(character) || character == '_')
        {
            return CharacterClass.Word;
        }

        return CharacterClass.Symbol;
    }

    private static int GetLineHeight(UiFont font, int scale)
    {
        return font.MeasureTextHeight(Math.Max(1, scale));
    }

    private static int GetGlyphWidth(UiFont font, int scale)
    {
        int safeScale = Math.Max(1, scale);
        int width = font.MeasureTextWidth("M", safeScale);
        if (width <= 0)
        {
            width = font.MeasureTextWidth(" ", safeScale);
        }

        return Math.Max(1, width);
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private static bool IsNumberChar(char c)
    {
        return char.IsDigit(c) || c == '.' || c == '_' || c == 'x' || c == 'X' || c == 'f' || c == 'F' || c == 'u' || c == 'U' || c == 'l' || c == 'L';
    }

    private static int FindStringEnd(string line, int start, char quote)
    {
        int index = start + 1;
        while (index < line.Length)
        {
            char c = line[index];
            if (c == '\\')
            {
                index += 2;
                continue;
            }

            if (c == quote)
            {
                return index + 1;
            }

            index++;
        }

        return line.Length;
    }

    private static bool AdvanceBlockCommentState(string line, bool inBlockComment)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (inBlockComment)
            {
                int end = line.IndexOf("*/", index, StringComparison.Ordinal);
                if (end < 0)
                {
                    return true;
                }

                index = end + 2;
                inBlockComment = false;
                continue;
            }

            char c = line[index];
            if (c == '/' && index + 1 < line.Length && line[index + 1] == '/')
            {
                return false;
            }

            if (c == '/' && index + 1 < line.Length && line[index + 1] == '*')
            {
                inBlockComment = true;
                index += 2;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                index = FindStringEnd(line, index, c);
                continue;
            }

            index++;
        }

        return inBlockComment;
    }
}
