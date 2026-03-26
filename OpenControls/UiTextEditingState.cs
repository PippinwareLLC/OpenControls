namespace OpenControls;

public sealed class UiTextEditingState
{
    private enum CharacterClass
    {
        Whitespace,
        Word,
        Symbol
    }

    private readonly List<Snapshot> _undoStack = new();
    private readonly List<Snapshot> _redoStack = new();
    private Snapshot _sessionOrigin = new(string.Empty, 0, 0);
    private string _text = string.Empty;

    public string Text => _text;
    public int CaretIndex { get; private set; }
    public int SelectionAnchor { get; private set; }
    public int SelectionStart => Math.Min(SelectionAnchor, CaretIndex);
    public int SelectionEnd => Math.Max(SelectionAnchor, CaretIndex);
    public int SelectionLength => SelectionEnd - SelectionStart;
    public bool HasSelection => SelectionLength > 0;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void SetText(string text, bool clearHistory = true)
    {
        _text = text ?? string.Empty;
        CaretIndex = Math.Clamp(CaretIndex, 0, _text.Length);
        SelectionAnchor = Math.Clamp(SelectionAnchor, 0, _text.Length);
        if (clearHistory)
        {
            ClearHistory();
        }
    }

    public void BeginSession()
    {
        ClearHistory();
        MarkSessionOrigin();
    }

    public void EndSession()
    {
        ClearHistory();
        MarkSessionOrigin();
    }

    public void MarkSessionOrigin()
    {
        _sessionOrigin = Capture();
    }

    public bool CancelSession()
    {
        bool changed = ApplySnapshot(_sessionOrigin);
        ClearHistory();
        return changed;
    }

    public void ClearSelection()
    {
        SelectionAnchor = CaretIndex;
    }

    public void SelectAll()
    {
        SelectionAnchor = 0;
        CaretIndex = _text.Length;
    }

    public void SelectWordAt(int index)
    {
        if (_text.Length == 0)
        {
            SetCaret(0);
            return;
        }

        int clamped = Math.Clamp(index, 0, _text.Length);
        if (clamped >= _text.Length)
        {
            clamped = _text.Length - 1;
        }

        CharacterClass kind = Classify(_text[clamped]);
        int start = clamped;
        int end = clamped + 1;

        while (start > 0 && Classify(_text[start - 1]) == kind)
        {
            start--;
        }

        while (end < _text.Length && Classify(_text[end]) == kind)
        {
            end++;
        }

        SelectRange(start, end);
    }

    public void SelectRange(int anchor, int caret)
    {
        SelectionAnchor = Math.Clamp(anchor, 0, _text.Length);
        CaretIndex = Math.Clamp(caret, 0, _text.Length);
    }

    public void SetCaret(int index, bool extendSelection = false)
    {
        CaretIndex = Math.Clamp(index, 0, _text.Length);
        if (!extendSelection)
        {
            SelectionAnchor = CaretIndex;
        }
    }

    public string GetSelectedText()
    {
        return HasSelection
            ? _text.Substring(SelectionStart, SelectionLength)
            : string.Empty;
    }

    public bool InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return ReplaceRange(SelectionStart, SelectionLength, text, SelectionStart + text.Length);
    }

    public bool ReplaceAll(string text)
    {
        string replacement = text ?? string.Empty;
        if (string.Equals(_text, replacement, StringComparison.Ordinal))
        {
            CaretIndex = Math.Clamp(CaretIndex, 0, replacement.Length);
            SelectionAnchor = CaretIndex;
            return false;
        }

        return ReplaceRange(0, _text.Length, replacement, replacement.Length);
    }

    public bool DeleteSelection()
    {
        return HasSelection && ReplaceRange(SelectionStart, SelectionLength, string.Empty, SelectionStart);
    }

    public bool Backspace(bool byWord = false)
    {
        if (HasSelection)
        {
            return DeleteSelection();
        }

        if (CaretIndex <= 0)
        {
            return false;
        }

        int start = byWord ? FindPreviousBoundary(_text, CaretIndex) : CaretIndex - 1;
        return ReplaceRange(start, CaretIndex - start, string.Empty, start);
    }

    public bool Delete(bool byWord = false)
    {
        if (HasSelection)
        {
            return DeleteSelection();
        }

        if (CaretIndex >= _text.Length)
        {
            return false;
        }

        int end = byWord ? FindNextBoundary(_text, CaretIndex) : CaretIndex + 1;
        return ReplaceRange(CaretIndex, end - CaretIndex, string.Empty, CaretIndex);
    }

    public void MoveLeft(bool extendSelection = false, bool byWord = false)
    {
        if (!extendSelection && HasSelection && !byWord)
        {
            SetCaret(SelectionStart);
            return;
        }

        int next = byWord ? FindPreviousBoundary(_text, CaretIndex) : CaretIndex - 1;
        SetCaret(next, extendSelection);
    }

    public void MoveRight(bool extendSelection = false, bool byWord = false)
    {
        if (!extendSelection && HasSelection && !byWord)
        {
            SetCaret(SelectionEnd);
            return;
        }

        int next = byWord ? FindNextBoundary(_text, CaretIndex) : CaretIndex + 1;
        SetCaret(next, extendSelection);
    }

    public void MoveHome(bool extendSelection = false)
    {
        SetCaret(0, extendSelection);
    }

    public void MoveEnd(bool extendSelection = false)
    {
        SetCaret(_text.Length, extendSelection);
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        _redoStack.Add(Capture());
        Snapshot snapshot = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        ApplySnapshot(snapshot);
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        _undoStack.Add(Capture());
        Snapshot snapshot = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        ApplySnapshot(snapshot);
        return true;
    }

    private bool ReplaceRange(int start, int length, string replacement, int nextCaret)
    {
        start = Math.Clamp(start, 0, _text.Length);
        length = Math.Clamp(length, 0, _text.Length - start);
        string safeReplacement = replacement ?? string.Empty;
        string nextText = _text.Remove(start, length).Insert(start, safeReplacement);
        if (string.Equals(nextText, _text, StringComparison.Ordinal))
        {
            CaretIndex = Math.Clamp(nextCaret, 0, nextText.Length);
            SelectionAnchor = CaretIndex;
            return false;
        }

        PushUndoSnapshot();
        _text = nextText;
        CaretIndex = Math.Clamp(nextCaret, 0, _text.Length);
        SelectionAnchor = CaretIndex;
        _redoStack.Clear();
        return true;
    }

    private void PushUndoSnapshot()
    {
        _undoStack.Add(Capture());
        if (_undoStack.Count > 128)
        {
            _undoStack.RemoveAt(0);
        }
    }

    private Snapshot Capture()
    {
        return new Snapshot(_text, CaretIndex, SelectionAnchor);
    }

    private bool ApplySnapshot(Snapshot snapshot)
    {
        bool changed =
            !string.Equals(_text, snapshot.Text, StringComparison.Ordinal)
            || CaretIndex != snapshot.CaretIndex
            || SelectionAnchor != snapshot.SelectionAnchor;

        _text = snapshot.Text;
        CaretIndex = Math.Clamp(snapshot.CaretIndex, 0, _text.Length);
        SelectionAnchor = Math.Clamp(snapshot.SelectionAnchor, 0, _text.Length);
        return changed;
    }

    private void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private static int FindPreviousBoundary(string text, int index)
    {
        int i = Math.Clamp(index, 0, text.Length);
        if (i <= 0)
        {
            return 0;
        }

        i--;
        CharacterClass kind = Classify(text[i]);
        if (kind == CharacterClass.Whitespace)
        {
            while (i > 0 && Classify(text[i - 1]) == CharacterClass.Whitespace)
            {
                i--;
            }

            return i;
        }

        while (i > 0 && Classify(text[i - 1]) == kind)
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
        if (kind == CharacterClass.Whitespace)
        {
            while (i < text.Length && Classify(text[i]) == CharacterClass.Whitespace)
            {
                i++;
            }

            return i;
        }

        while (i < text.Length && Classify(text[i]) == kind)
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

    private readonly record struct Snapshot(string Text, int CaretIndex, int SelectionAnchor);
}
