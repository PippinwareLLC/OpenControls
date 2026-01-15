using System.Text;

namespace OpenControls.Controls;

public enum UiTextEditorSyntaxMode
{
    None,
    CSharp
}

public sealed class UiTextEditor : UiElement
{
    private sealed class TextEditorView : UiElement
    {
        private readonly UiTextEditor _owner;

        public TextEditorView(UiTextEditor owner)
        {
            _owner = owner;
        }

        public override bool IsFocusable => true;
        public override bool HandlesTabInput => _owner.AllowTabInput;

        public override void Update(UiUpdateContext context)
        {
            if (!Visible || !Enabled)
            {
                return;
            }

            UiInputState input = context.Input;
            if (input.LeftClicked && Bounds.Contains(input.MousePosition))
            {
                context.Focus.RequestFocus(this);
                _owner.SetCaretFromPoint(input.MousePosition);
            }

            if (context.Focus.Focused == this)
            {
                _owner.HandleInput(input);
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
    private readonly List<string> _lines = new();
    private readonly List<List<LineToken>?> _tokenCache = new();
    private readonly List<bool> _lineStartsInBlockComment = new();

    private string _text = string.Empty;
    private bool _textDirty;
    private int _maxLineLength;
    private int _caretLine;
    private int _caretColumn;
    private int _preferredColumn;
    private int _lineHeight;
    private int _glyphWidth;
    private int _lineNumberWidth;
    private int _textStartX;
    private bool _hasFocus;

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
        EnsureLines();
        RebuildCaches();
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

    public bool HighlightCurrentLine { get; set; } = true;
    public bool ShowLineNumbers { get; set; } = true;
    public int TextScale { get; set; } = 2;
    public int Padding { get; set; } = 6;
    public int LineNumberPadding { get; set; } = 8;
    public int LineSpacing { get; set; } = 2;
    public int TabSize { get; set; } = 4;
    public bool AllowTabInput { get; set; } = true;
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

    public string Text
    {
        get
        {
            if (_textDirty)
            {
                _text = string.Join("\n", _lines);
                _textDirty = false;
            }

            return _text;
        }
        set => SetText(value);
    }

    public event Action? TextChanged;

    public void SetText(string text)
    {
        _text = NormalizeText(text);
        _textDirty = false;
        _lines.Clear();
        using StringReader reader = new(_text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            _lines.Add(line);
        }

        EnsureLines();
        _caretLine = Math.Clamp(_caretLine, 0, _lines.Count - 1);
        _caretColumn = Math.Clamp(_caretColumn, 0, _lines[_caretLine].Length);
        _preferredColumn = _caretColumn;
        RebuildCaches();
        TextChanged?.Invoke();
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        RefreshLayout();

        _scrollPanel.Background = Background;
        _scrollPanel.Border = Border;
        _scrollPanel.BorderThickness = BorderThickness;
        _scrollPanel.CornerRadius = CornerRadius;
        _scrollPanel.Bounds = Bounds;
        _scrollPanel.Update(context);
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

    private void EnsureLines()
    {
        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }
    }

    private void RefreshLayout()
    {
        _glyphWidth = GetGlyphWidth(TextScale);
        _lineHeight = GetLineHeight(TextScale);

        int digits = Math.Max(MinLineNumberDigits, _lines.Count.ToString().Length);
        _lineNumberWidth = ShowLineNumbers ? digits * _glyphWidth + LineNumberPadding * 2 : 0;
        _textStartX = Padding + _lineNumberWidth;

        int contentWidth = _textStartX + _maxLineLength * _glyphWidth + Padding;
        int contentHeight = Padding * 2 + _lines.Count * _lineHeight;
        _view.Bounds = new UiRect(0, 0, contentWidth, contentHeight);
    }

    private void HandleInput(UiInputState input)
    {
        bool textChanged = false;

        if (input.Navigation.Enter || input.Navigation.KeypadEnter)
        {
            InsertText("\n");
            textChanged = true;
        }

        if (input.Navigation.Tab)
        {
            InsertText(new string(' ', Math.Max(1, TabSize)));
            textChanged = true;
        }

        if (input.Navigation.Backspace)
        {
            textChanged |= Backspace();
        }

        if (input.Navigation.Delete)
        {
            textChanged |= Delete();
        }

        if (input.TextInput.Count > 0)
        {
            foreach (char character in input.TextInput)
            {
                if (character == '\r' || character == '\n')
                {
                    InsertText("\n");
                }
                else if (character == '\t')
                {
                    InsertText(new string(' ', Math.Max(1, TabSize)));
                }
                else
                {
                    InsertText(character.ToString());
                }
                textChanged = true;
            }
        }

        bool moved = false;
        if (input.Navigation.MoveLeft)
        {
            MoveLeft();
            moved = true;
        }
        if (input.Navigation.MoveRight)
        {
            MoveRight();
            moved = true;
        }
        if (input.Navigation.MoveUp)
        {
            MoveUp();
            moved = true;
        }
        if (input.Navigation.MoveDown)
        {
            MoveDown();
            moved = true;
        }
        if (input.Navigation.Home)
        {
            _caretColumn = 0;
            _preferredColumn = _caretColumn;
            moved = true;
        }
        if (input.Navigation.End)
        {
            _caretColumn = _lines[_caretLine].Length;
            _preferredColumn = _caretColumn;
            moved = true;
        }

        if (textChanged)
        {
            TextChanged?.Invoke();
        }

        if (textChanged || moved)
        {
            EnsureCaretVisible();
        }
    }

    private void SetCaretFromPoint(UiPoint point)
    {
        int lineIndex = (point.Y - Padding) / Math.Max(1, _lineHeight);
        _caretLine = Math.Clamp(lineIndex, 0, _lines.Count - 1);

        int column = (point.X - _textStartX) / Math.Max(1, _glyphWidth);
        if (column < 0)
        {
            column = 0;
        }

        _caretColumn = Math.Clamp(column, 0, _lines[_caretLine].Length);
        _preferredColumn = _caretColumn;
        EnsureCaretVisible();
    }

    private void RenderEditor(UiRenderContext context)
    {
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

            if (ShowLineNumbers)
            {
                string lineNumber = (i + 1).ToString();
                int numberWidth = lineNumber.Length * _glyphWidth;
                int numberX = Math.Max(0, _lineNumberWidth - LineNumberPadding - numberWidth);
                context.Renderer.DrawText(lineNumber, new UiPoint(numberX, lineY), LineNumberColor, TextScale);
            }

            string lineText = _lines[i];
            context.Renderer.DrawText(lineText, new UiPoint(_textStartX, lineY), TextColor, TextScale);

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
                    int tokenX = _textStartX + token.Start * _glyphWidth;
                    context.Renderer.DrawText(segment, new UiPoint(tokenX, lineY), token.Color, TextScale);
                }
            }
        }

        if (_hasFocus)
        {
            int caretX = _textStartX + _caretColumn * _glyphWidth;
            int caretY = Padding + _caretLine * lineHeight;
            context.Renderer.FillRect(new UiRect(caretX, caretY, Math.Max(1, TextScale), lineHeight), CaretColor);
        }
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

    private bool Backspace()
    {
        if (_caretColumn > 0)
        {
            string line = _lines[_caretLine];
            _lines[_caretLine] = line.Remove(_caretColumn - 1, 1);
            _caretColumn--;
            _preferredColumn = _caretColumn;
            MarkTextChanged();
            return true;
        }

        if (_caretLine == 0)
        {
            return false;
        }

        string previous = _lines[_caretLine - 1];
        string current = _lines[_caretLine];
        int newColumn = previous.Length;
        _lines[_caretLine - 1] = previous + current;
        _lines.RemoveAt(_caretLine);
        _caretLine--;
        _caretColumn = newColumn;
        _preferredColumn = _caretColumn;
        MarkTextChanged();
        return true;
    }

    private bool Delete()
    {
        string line = _lines[_caretLine];
        if (_caretColumn < line.Length)
        {
            _lines[_caretLine] = line.Remove(_caretColumn, 1);
            MarkTextChanged();
            return true;
        }

        if (_caretLine >= _lines.Count - 1)
        {
            return false;
        }

        string next = _lines[_caretLine + 1];
        _lines[_caretLine] = line + next;
        _lines.RemoveAt(_caretLine + 1);
        MarkTextChanged();
        return true;
    }

    private void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        string normalized = NormalizeText(text);
        string line = _lines[_caretLine];
        string prefix = line.Substring(0, _caretColumn);
        string suffix = line.Substring(_caretColumn);

        if (!normalized.Contains('\n'))
        {
            _lines[_caretLine] = prefix + normalized + suffix;
            _caretColumn += normalized.Length;
            _preferredColumn = _caretColumn;
            MarkTextChanged();
            return;
        }

        string[] parts = normalized.Split('\n');
        _lines[_caretLine] = prefix + parts[0];
        int insertIndex = _caretLine + 1;
        for (int i = 1; i < parts.Length; i++)
        {
            string segment = parts[i];
            if (i == parts.Length - 1)
            {
                _lines.Insert(insertIndex, segment + suffix);
            }
            else
            {
                _lines.Insert(insertIndex, segment);
            }
            insertIndex++;
        }

        _caretLine += parts.Length - 1;
        _caretColumn = parts[^1].Length;
        _preferredColumn = _caretColumn;
        MarkTextChanged();
    }

    private void MoveLeft()
    {
        if (_caretColumn > 0)
        {
            _caretColumn--;
        }
        else if (_caretLine > 0)
        {
            _caretLine--;
            _caretColumn = _lines[_caretLine].Length;
        }

        _preferredColumn = _caretColumn;
    }

    private void MoveRight()
    {
        string line = _lines[_caretLine];
        if (_caretColumn < line.Length)
        {
            _caretColumn++;
        }
        else if (_caretLine < _lines.Count - 1)
        {
            _caretLine++;
            _caretColumn = 0;
        }

        _preferredColumn = _caretColumn;
    }

    private void MoveUp()
    {
        if (_caretLine <= 0)
        {
            return;
        }

        _caretLine--;
        _caretColumn = Math.Min(_lines[_caretLine].Length, _preferredColumn);
    }

    private void MoveDown()
    {
        if (_caretLine >= _lines.Count - 1)
        {
            return;
        }

        _caretLine++;
        _caretColumn = Math.Min(_lines[_caretLine].Length, _preferredColumn);
    }

    private void EnsureCaretVisible()
    {
        int caretX = _textStartX + _caretColumn * _glyphWidth;
        int caretY = Padding + _caretLine * _lineHeight;
        int viewWidth = Math.Max(1, _scrollPanel.ViewportBounds.Width);
        int viewHeight = Math.Max(1, _scrollPanel.ViewportBounds.Height);

        int scrollX = _scrollPanel.ScrollX;
        int scrollY = _scrollPanel.ScrollY;

        if (caretX < scrollX)
        {
            scrollX = caretX;
        }
        else if (caretX + _glyphWidth > scrollX + viewWidth)
        {
            scrollX = caretX + _glyphWidth - viewWidth;
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

    private void MarkTextChanged()
    {
        _textDirty = true;
        EnsureLines();
        RebuildCaches();
    }

    private void SetFocusState(bool focused)
    {
        _hasFocus = focused;
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

    private static int GetLineHeight(int scale)
    {
        return TinyBitmapFont.GlyphHeight * Math.Max(1, scale) + 2;
    }

    private static int GetGlyphWidth(int scale)
    {
        return (TinyBitmapFont.GlyphWidth + TinyBitmapFont.GlyphSpacing) * Math.Max(1, scale);
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
