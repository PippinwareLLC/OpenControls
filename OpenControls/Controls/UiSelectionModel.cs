namespace OpenControls.Controls;

public sealed class UiSelectionModel
{
    private sealed class ScopeState
    {
        public HashSet<int> Selected { get; } = new();
        public List<int> SortedIndices { get; } = new();
        public int ItemCount { get; set; } = -1;
        public int AnchorIndex { get; set; } = -1;
        public int PrimaryIndex { get; set; } = -1;
    }

    private const string DefaultScope = "";
    private static readonly IReadOnlyList<int> EmptySelection = Array.Empty<int>();
    private readonly Dictionary<string, ScopeState> _scopes = new(StringComparer.Ordinal);

    public UiSelectionAnchorPersistence AnchorPersistence { get; set; } = UiSelectionAnchorPersistence.ClampToNearestValid;
    public int ItemCount => GetItemCount();
    public int AnchorIndex => GetAnchorIndex();
    public int PrimaryIndex => GetPrimaryIndex();
    public IReadOnlyList<int> SelectedIndices => GetSelectedIndices();
    public IReadOnlyCollection<string> ScopeIds => _scopes.Keys;

    public event Action? SelectionChanged;
    public event Action<string>? ScopeSelectionChanged;

    public int GetItemCount(string? scope = null)
    {
        if (!TryGetScopeState(scope, out ScopeState? state) || state == null)
        {
            return -1;
        }

        return state.ItemCount;
    }

    public int GetAnchorIndex(string? scope = null)
    {
        if (!TryGetScopeState(scope, out ScopeState? state) || state == null)
        {
            return -1;
        }

        return state.AnchorIndex;
    }

    public int GetPrimaryIndex(string? scope = null)
    {
        if (!TryGetScopeState(scope, out ScopeState? state) || state == null)
        {
            return -1;
        }

        return state.PrimaryIndex;
    }

    public IReadOnlyList<int> GetSelectedIndices(string? scope = null)
    {
        if (!TryGetScopeState(scope, out ScopeState? state) || state == null)
        {
            return EmptySelection;
        }

        return state.SortedIndices;
    }

    public bool HasSelection(string? scope = null)
    {
        return TryGetScopeState(scope, out ScopeState? state) && state != null && state.Selected.Count > 0;
    }

    public bool HasScope(string? scope)
    {
        return _scopes.ContainsKey(NormalizeScope(scope));
    }

    public bool IsSelected(int index, string? scope = null)
    {
        if (index < 0)
        {
            return false;
        }

        return TryGetScopeState(scope, out ScopeState? state) && state != null && state.Selected.Contains(index);
    }

    public void SetItemCount(int count, string? scope = null)
    {
        ScopeState state = GetScopeState(scope);
        int clamped = Math.Max(0, count);
        if (state.ItemCount == clamped)
        {
            return;
        }

        state.ItemCount = clamped;
        if (NormalizeState(state, keepAnchorWhenPossible: true))
        {
            RaiseSelectionChanged(scope);
        }
    }

    public void Clear(string? scope = null)
    {
        ScopeState state = GetScopeState(scope);
        if (state.Selected.Count == 0 && state.PrimaryIndex == -1 && state.AnchorIndex == -1)
        {
            return;
        }

        state.Selected.Clear();
        state.SortedIndices.Clear();
        state.PrimaryIndex = -1;
        state.AnchorIndex = -1;
        RaiseSelectionChanged(scope);
    }

    public void ClearAllScopes()
    {
        if (_scopes.Count == 0)
        {
            return;
        }

        List<string> changedScopes = new();
        foreach (KeyValuePair<string, ScopeState> entry in _scopes)
        {
            ScopeState state = entry.Value;
            if (state.Selected.Count == 0 && state.PrimaryIndex == -1 && state.AnchorIndex == -1)
            {
                continue;
            }

            state.Selected.Clear();
            state.SortedIndices.Clear();
            state.PrimaryIndex = -1;
            state.AnchorIndex = -1;
            changedScopes.Add(entry.Key);
        }

        if (changedScopes.Count == 0)
        {
            return;
        }

        SelectionChanged?.Invoke();
        for (int i = 0; i < changedScopes.Count; i++)
        {
            ScopeSelectionChanged?.Invoke(changedScopes[i]);
        }
    }

    public void RemoveScope(string scope)
    {
        string scopeId = NormalizeScope(scope);
        if (scopeId == DefaultScope)
        {
            Clear();
            return;
        }

        if (_scopes.Remove(scopeId))
        {
            SelectionChanged?.Invoke();
            ScopeSelectionChanged?.Invoke(scopeId);
        }
    }

    public void CopyScope(string sourceScope, string destinationScope)
    {
        string sourceId = NormalizeScope(sourceScope);
        string destinationId = NormalizeScope(destinationScope);
        ScopeState source = GetScopeState(sourceId);
        ScopeState destination = GetScopeState(destinationId);

        destination.Selected.Clear();
        foreach (int index in source.Selected)
        {
            destination.Selected.Add(index);
        }

        destination.ItemCount = source.ItemCount;
        destination.PrimaryIndex = source.PrimaryIndex;
        destination.AnchorIndex = source.AnchorIndex;
        RefreshSortedIndices(destination);
        RaiseSelectionChanged(destinationId);
    }

    public void SelectSingle(int index, string? scope = null)
    {
        ScopeState state = GetScopeState(scope);
        if (!IsIndexValid(index, state.ItemCount))
        {
            Clear(scope);
            return;
        }

        bool changed = state.Selected.Count != 1 || !state.Selected.Contains(index) || state.PrimaryIndex != index || state.AnchorIndex != index;
        if (!changed)
        {
            return;
        }

        state.Selected.Clear();
        state.Selected.Add(index);
        state.PrimaryIndex = index;
        state.AnchorIndex = index;
        RefreshSortedIndices(state);
        RaiseSelectionChanged(scope);
    }

    public void SetSelected(int index, bool selected, string? scope = null)
    {
        ScopeState state = GetScopeState(scope);
        if (!IsIndexValid(index, state.ItemCount))
        {
            return;
        }

        if (selected)
        {
            if (!state.Selected.Add(index))
            {
                return;
            }

            state.PrimaryIndex = index;
            state.AnchorIndex = index;
            RefreshSortedIndices(state);
            RaiseSelectionChanged(scope);
            return;
        }

        if (!state.Selected.Remove(index))
        {
            return;
        }

        RefreshSortedIndices(state);
        if (state.PrimaryIndex == index)
        {
            state.PrimaryIndex = state.SortedIndices.Count > 0 ? state.SortedIndices[^1] : -1;
        }

        if (state.AnchorIndex == index)
        {
            state.AnchorIndex = state.PrimaryIndex;
        }

        RaiseSelectionChanged(scope);
    }

    public void ApplySelection(int index, bool ctrl, bool shift, string? scope = null)
    {
        ScopeState state = GetScopeState(scope);
        if (!IsIndexValid(index, state.ItemCount))
        {
            if (!ctrl && !shift)
            {
                Clear(scope);
            }
            return;
        }

        if (shift && state.AnchorIndex >= 0)
        {
            bool changed = ctrl
                ? AddRange(state, state.AnchorIndex, index)
                : SetRange(state, state.AnchorIndex, index);

            if (changed)
            {
                state.PrimaryIndex = index;
                RaiseSelectionChanged(scope);
            }

            return;
        }

        if (ctrl)
        {
            Toggle(state, index, scope);
            return;
        }

        SelectSingle(index, scope);
    }

    public void SelectRange(int start, int end, string? scope = null, bool additive = false)
    {
        ScopeState state = GetScopeState(scope);
        bool changed = additive ? AddRange(state, start, end) : SetRange(state, start, end);
        if (!changed)
        {
            return;
        }

        int clampedEnd = Math.Clamp(Math.Max(start, end), 0, Math.Max(0, state.ItemCount - 1));
        state.PrimaryIndex = clampedEnd;
        state.AnchorIndex = Math.Clamp(Math.Min(start, end), 0, Math.Max(0, state.ItemCount - 1));
        RaiseSelectionChanged(scope);
    }

    public void InsertSpace(int index, int count, string? scope = null)
    {
        if (count <= 0)
        {
            return;
        }

        ScopeState state = GetScopeState(scope);
        if (state.ItemCount >= 0)
        {
            index = Math.Clamp(index, 0, state.ItemCount);
            state.ItemCount += count;
        }
        else
        {
            index = Math.Max(0, index);
        }

        if (state.Selected.Count == 0 && state.PrimaryIndex < index && state.AnchorIndex < index)
        {
            return;
        }

        ShiftIndices(state, index, count);
        RaiseSelectionChanged(scope);
    }

    public void RemoveRange(int index, int count, string? scope = null)
    {
        if (count <= 0)
        {
            return;
        }

        ScopeState state = GetScopeState(scope);
        if (state.ItemCount == 0)
        {
            return;
        }

        index = Math.Max(0, index);
        if (state.ItemCount >= 0)
        {
            if (index >= state.ItemCount)
            {
                return;
            }

            count = Math.Min(count, state.ItemCount - index);
            state.ItemCount = Math.Max(0, state.ItemCount - count);
        }

        int end = index + count;
        bool removedSelected = false;
        HashSet<int> updated = new();
        foreach (int selected in state.Selected)
        {
            if (selected < index)
            {
                updated.Add(selected);
            }
            else if (selected >= end)
            {
                updated.Add(selected - count);
            }
            else
            {
                removedSelected = true;
            }
        }

        state.Selected.Clear();
        foreach (int updatedIndex in updated)
        {
            state.Selected.Add(updatedIndex);
        }

        state.PrimaryIndex = AdjustIndexAfterRemoval(state.PrimaryIndex, index, count, state.ItemCount, state.PrimaryIndex >= 0 && state.Selected.Contains(state.PrimaryIndex));
        state.AnchorIndex = AdjustAnchorAfterRemoval(state.AnchorIndex, index, count, state.ItemCount, state.PrimaryIndex);
        RefreshSortedIndices(state);

        if (state.Selected.Count == 0 && removedSelected && state.ItemCount > 0)
        {
            int fallback = Math.Clamp(index, 0, state.ItemCount - 1);
            state.Selected.Add(fallback);
            state.PrimaryIndex = fallback;
            state.AnchorIndex = fallback;
            RefreshSortedIndices(state);
        }
        else if (state.Selected.Count > 0)
        {
            if (state.PrimaryIndex < 0 || !state.Selected.Contains(state.PrimaryIndex))
            {
                state.PrimaryIndex = state.SortedIndices[^1];
            }

            if (AnchorPersistence == UiSelectionAnchorPersistence.ResetToPrimary || state.AnchorIndex < 0)
            {
                state.AnchorIndex = state.PrimaryIndex;
            }
        }

        RaiseSelectionChanged(scope);
    }

    public IReadOnlyList<int> DeleteSelected(string? scope = null)
    {
        ScopeState state = GetScopeState(scope);
        if (state.SortedIndices.Count == 0)
        {
            return EmptySelection;
        }

        int[] removed = state.SortedIndices.ToArray();
        if (state.ItemCount >= 0)
        {
            state.ItemCount = Math.Max(0, state.ItemCount - removed.Length);
        }

        state.Selected.Clear();
        state.SortedIndices.Clear();

        if (state.ItemCount > 0)
        {
            int fallback = Math.Clamp(removed[0], 0, state.ItemCount - 1);
            state.Selected.Add(fallback);
            state.PrimaryIndex = fallback;
            state.AnchorIndex = fallback;
            state.SortedIndices.Add(fallback);
        }
        else
        {
            state.PrimaryIndex = -1;
            state.AnchorIndex = -1;
        }

        RaiseSelectionChanged(scope);
        return removed;
    }

    private void Toggle(ScopeState state, int index, string? scope)
    {
        if (state.Selected.Contains(index))
        {
            SetSelected(index, false, scope);
            return;
        }

        SetSelected(index, true, scope);
    }

    private bool SetRange(ScopeState state, int start, int end)
    {
        if (start > end)
        {
            (start, end) = (end, start);
        }

        HashSet<int> next = new();
        for (int i = start; i <= end; i++)
        {
            if (IsIndexValid(i, state.ItemCount))
            {
                next.Add(i);
            }
        }

        if (state.Selected.SetEquals(next))
        {
            return false;
        }

        state.Selected.Clear();
        foreach (int index in next)
        {
            state.Selected.Add(index);
        }

        RefreshSortedIndices(state);
        return true;
    }

    private bool AddRange(ScopeState state, int start, int end)
    {
        if (start > end)
        {
            (start, end) = (end, start);
        }

        bool changed = false;
        for (int i = start; i <= end; i++)
        {
            if (IsIndexValid(i, state.ItemCount) && state.Selected.Add(i))
            {
                changed = true;
            }
        }

        if (changed)
        {
            RefreshSortedIndices(state);
        }

        return changed;
    }

    private bool NormalizeState(ScopeState state, bool keepAnchorWhenPossible)
    {
        bool changed = false;
        if (state.ItemCount == 0)
        {
            if (state.Selected.Count > 0 || state.PrimaryIndex != -1 || state.AnchorIndex != -1)
            {
                state.Selected.Clear();
                state.SortedIndices.Clear();
                state.PrimaryIndex = -1;
                state.AnchorIndex = -1;
                return true;
            }

            return false;
        }

        List<int> toRemove = new();
        foreach (int index in state.Selected)
        {
            if (!IsIndexValid(index, state.ItemCount))
            {
                toRemove.Add(index);
            }
        }

        if (toRemove.Count > 0)
        {
            foreach (int index in toRemove)
            {
                state.Selected.Remove(index);
            }

            changed = true;
        }

        if (!IsIndexValid(state.PrimaryIndex, state.ItemCount))
        {
            state.PrimaryIndex = -1;
            changed = true;
        }

        if (!IsIndexValid(state.AnchorIndex, state.ItemCount))
        {
            state.AnchorIndex = -1;
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        RefreshSortedIndices(state);
        if (state.PrimaryIndex == -1 && state.SortedIndices.Count > 0)
        {
            state.PrimaryIndex = state.SortedIndices[^1];
        }

        if (keepAnchorWhenPossible && state.AnchorIndex == -1 && state.PrimaryIndex >= 0)
        {
            state.AnchorIndex = state.PrimaryIndex;
        }

        return true;
    }

    private void ShiftIndices(ScopeState state, int startIndex, int delta)
    {
        HashSet<int> shifted = new();
        foreach (int selected in state.Selected)
        {
            shifted.Add(selected >= startIndex ? selected + delta : selected);
        }

        state.Selected.Clear();
        foreach (int index in shifted)
        {
            state.Selected.Add(index);
        }

        if (state.PrimaryIndex >= startIndex)
        {
            state.PrimaryIndex += delta;
        }

        if (state.AnchorIndex >= startIndex)
        {
            state.AnchorIndex += delta;
        }

        RefreshSortedIndices(state);
    }

    private static int AdjustIndexAfterRemoval(int value, int start, int count, int itemCount, bool preserveExistingSelection)
    {
        if (value < 0)
        {
            return -1;
        }

        int end = start + count;
        if (value < start)
        {
            return value;
        }

        if (value >= end)
        {
            return value - count;
        }

        if (preserveExistingSelection || itemCount <= 0)
        {
            return -1;
        }

        return Math.Clamp(start, 0, itemCount - 1);
    }

    private int AdjustAnchorAfterRemoval(int value, int start, int count, int itemCount, int primaryIndex)
    {
        int adjusted = AdjustIndexAfterRemoval(value, start, count, itemCount, preserveExistingSelection: false);
        if (adjusted >= 0 || itemCount <= 0)
        {
            return adjusted;
        }

        return AnchorPersistence == UiSelectionAnchorPersistence.ClampToNearestValid
            ? Math.Clamp(start, 0, itemCount - 1)
            : primaryIndex;
    }

    private static bool IsIndexValid(int index, int itemCount)
    {
        if (index < 0)
        {
            return false;
        }

        return itemCount < 0 || index < itemCount;
    }

    private static void RefreshSortedIndices(ScopeState state)
    {
        state.SortedIndices.Clear();
        if (state.Selected.Count == 0)
        {
            return;
        }

        state.SortedIndices.AddRange(state.Selected);
        state.SortedIndices.Sort();
    }

    private ScopeState GetScopeState(string? scope)
    {
        string scopeId = NormalizeScope(scope);
        if (!_scopes.TryGetValue(scopeId, out ScopeState? state))
        {
            state = new ScopeState();
            _scopes.Add(scopeId, state);
        }

        return state;
    }

    private bool TryGetScopeState(string? scope, out ScopeState? state)
    {
        return _scopes.TryGetValue(NormalizeScope(scope), out state);
    }

    private static string NormalizeScope(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope) ? DefaultScope : scope.Trim();
    }

    private void RaiseSelectionChanged(string? scope)
    {
        string scopeId = NormalizeScope(scope);
        SelectionChanged?.Invoke();
        ScopeSelectionChanged?.Invoke(scopeId);
    }
}
