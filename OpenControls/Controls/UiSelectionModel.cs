namespace OpenControls.Controls;

public sealed class UiSelectionModel
{
    private readonly HashSet<int> _selected = new();
    private readonly List<int> _sortedIndices = new();
    private int _itemCount = -1;
    private int _anchorIndex = -1;
    private int _primaryIndex = -1;

    public int ItemCount => _itemCount;
    public int AnchorIndex => _anchorIndex;
    public int PrimaryIndex => _primaryIndex;
    public IReadOnlyList<int> SelectedIndices => _sortedIndices;

    public event Action? SelectionChanged;

    public void SetItemCount(int count)
    {
        int clamped = Math.Max(0, count);
        if (_itemCount == clamped)
        {
            return;
        }

        _itemCount = clamped;
        bool changed = false;

        if (_itemCount == 0)
        {
            if (_selected.Count > 0 || _primaryIndex != -1 || _anchorIndex != -1)
            {
                _selected.Clear();
                _sortedIndices.Clear();
                _primaryIndex = -1;
                _anchorIndex = -1;
                changed = true;
            }
        }
        else
        {
            List<int> toRemove = new();
            foreach (int index in _selected)
            {
                if (index < 0 || index >= _itemCount)
                {
                    toRemove.Add(index);
                }
            }

            if (toRemove.Count > 0)
            {
                foreach (int index in toRemove)
                {
                    _selected.Remove(index);
                }
                changed = true;
            }

            if (_primaryIndex >= _itemCount)
            {
                _primaryIndex = -1;
                changed = true;
            }

            if (_anchorIndex >= _itemCount)
            {
                _anchorIndex = -1;
                changed = true;
            }

            if (changed)
            {
                RefreshSortedIndices();
                if (_primaryIndex == -1 && _sortedIndices.Count > 0)
                {
                    _primaryIndex = _sortedIndices[^1];
                }

                if (_anchorIndex == -1 && _primaryIndex >= 0)
                {
                    _anchorIndex = _primaryIndex;
                }
            }
        }

        if (changed)
        {
            SelectionChanged?.Invoke();
        }
    }

    public bool IsSelected(int index)
    {
        if (index < 0)
        {
            return false;
        }

        return _selected.Contains(index);
    }

    public void Clear()
    {
        if (_selected.Count == 0 && _primaryIndex == -1 && _anchorIndex == -1)
        {
            return;
        }

        _selected.Clear();
        _sortedIndices.Clear();
        _primaryIndex = -1;
        _anchorIndex = -1;
        SelectionChanged?.Invoke();
    }

    public void SelectSingle(int index)
    {
        if (!IsIndexValid(index))
        {
            Clear();
            return;
        }

        bool changed = _selected.Count != 1 || !_selected.Contains(index) || _primaryIndex != index || _anchorIndex != index;
        if (!changed)
        {
            return;
        }

        _selected.Clear();
        _selected.Add(index);
        _primaryIndex = index;
        _anchorIndex = index;
        RefreshSortedIndices();
        SelectionChanged?.Invoke();
    }

    public void SetSelected(int index, bool selected)
    {
        if (!IsIndexValid(index))
        {
            return;
        }

        if (selected)
        {
            if (_selected.Add(index))
            {
                _primaryIndex = index;
                _anchorIndex = index;
                RefreshSortedIndices();
                SelectionChanged?.Invoke();
            }
            return;
        }

        if (!_selected.Remove(index))
        {
            return;
        }

        RefreshSortedIndices();
        if (_primaryIndex == index)
        {
            _primaryIndex = _sortedIndices.Count > 0 ? _sortedIndices[^1] : -1;
        }

        if (_anchorIndex == index)
        {
            _anchorIndex = _primaryIndex;
        }

        SelectionChanged?.Invoke();
    }

    public void ApplySelection(int index, bool ctrl, bool shift)
    {
        if (!IsIndexValid(index))
        {
            if (!ctrl && !shift)
            {
                Clear();
            }
            return;
        }

        if (shift && _anchorIndex >= 0)
        {
            if (ctrl)
            {
                AddRange(_anchorIndex, index);
            }
            else
            {
                SetRange(_anchorIndex, index);
            }

            _primaryIndex = index;
            SelectionChanged?.Invoke();
            return;
        }

        if (ctrl)
        {
            Toggle(index);
            return;
        }

        SelectSingle(index);
    }

    private void Toggle(int index)
    {
        if (_selected.Contains(index))
        {
            SetSelected(index, false);
            return;
        }

        SetSelected(index, true);
    }

    private void SetRange(int start, int end)
    {
        _selected.Clear();
        AddRange(start, end);
    }

    private void AddRange(int start, int end)
    {
        if (start > end)
        {
            (start, end) = (end, start);
        }

        bool changed = false;
        for (int i = start; i <= end; i++)
        {
            if (IsIndexValid(i) && _selected.Add(i))
            {
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        RefreshSortedIndices();
        if (_primaryIndex == -1)
        {
            _primaryIndex = end;
        }
    }

    private void RefreshSortedIndices()
    {
        _sortedIndices.Clear();
        if (_selected.Count == 0)
        {
            return;
        }

        _sortedIndices.AddRange(_selected);
        _sortedIndices.Sort();
    }

    private bool IsIndexValid(int index)
    {
        if (index < 0)
        {
            return false;
        }

        return _itemCount < 0 || index < _itemCount;
    }
}
