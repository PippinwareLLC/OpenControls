using System;
using System.Collections.Generic;

namespace OpenControls.Controls;

public sealed class UiGridDefinition
{
    public int Size { get; set; }
    public float Weight { get; set; } = 1f;
}

public sealed class UiGrid : UiElement
{
    private sealed class GridPlacement
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;
    }

    private readonly Dictionary<UiElement, GridPlacement> _placements = new();
    private int[] _columnSizes = Array.Empty<int>();
    private int[] _rowSizes = Array.Empty<int>();
    private int[] _columnPositions = Array.Empty<int>();
    private int[] _rowPositions = Array.Empty<int>();
    private UiRect _gridBounds;

    public List<UiGridDefinition> Columns { get; } = new();
    public List<UiGridDefinition> Rows { get; } = new();

    public int Padding { get; set; } = 4;
    public int ColumnSpacing { get; set; } = 4;
    public int RowSpacing { get; set; } = 4;
    public int CellPadding { get; set; } = 2;

    public bool ShowGridLines { get; set; } = true;
    public UiColor Background { get; set; } = new UiColor(18, 22, 32);
    public UiColor Border { get; set; } = new UiColor(60, 70, 90);
    public int BorderThickness { get; set; } = 1;
    public UiColor GridLineColor { get; set; } = new UiColor(50, 60, 80);
    public int GridLineThickness { get; set; } = 1;
    public int CornerRadius { get; set; }

    public UiRect GridBounds => _gridBounds;

    public void SetRowCount(int count)
    {
        count = Math.Max(0, count);
        EnsureDefinitionCount(Rows, count);
    }

    public void SetColumnCount(int count)
    {
        count = Math.Max(0, count);
        EnsureDefinitionCount(Columns, count);
    }

    public void AddChild(UiElement child, int row, int column, int rowSpan = 1, int columnSpan = 1)
    {
        base.AddChild(child);
        SetCell(child, row, column, rowSpan, columnSpan);
    }

    public void SetCell(UiElement child, int row, int column, int rowSpan = 1, int columnSpan = 1)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        row = Math.Max(0, row);
        column = Math.Max(0, column);
        rowSpan = Math.Max(1, rowSpan);
        columnSpan = Math.Max(1, columnSpan);

        EnsureDefinitionCount(Rows, row + rowSpan);
        EnsureDefinitionCount(Columns, column + columnSpan);

        if (!_placements.TryGetValue(child, out GridPlacement? placement))
        {
            placement = new GridPlacement();
            _placements[child] = placement;
        }

        placement.Row = row;
        placement.Column = column;
        placement.RowSpan = rowSpan;
        placement.ColumnSpan = columnSpan;
    }

    public new bool RemoveChild(UiElement child)
    {
        if (base.RemoveChild(child))
        {
            _placements.Remove(child);
            return true;
        }

        return false;
    }

    public override void Update(UiUpdateContext context)
    {
        if (!Visible || !Enabled)
        {
            return;
        }

        LayoutChildren();
        base.Update(context);
    }

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        LayoutChildren();

        UiRenderHelpers.FillRectRounded(context.Renderer, Bounds, CornerRadius, Background);
        if (Border.A > 0 && BorderThickness > 0)
        {
            UiRenderHelpers.DrawRectRounded(context.Renderer, Bounds, CornerRadius, Border, BorderThickness);
        }

        if (ShowGridLines)
        {
            DrawGridLines(context);
        }

        base.Render(context);
    }

    private void LayoutChildren()
    {
        CleanupPlacements();

        int columnCount = Columns.Count;
        int rowCount = Rows.Count;
        int padding = Math.Max(0, Padding);

        int gridX = Bounds.X + padding;
        int gridY = Bounds.Y + padding;
        int gridWidth = Math.Max(0, Bounds.Width - padding * 2);
        int gridHeight = Math.Max(0, Bounds.Height - padding * 2);
        _gridBounds = new UiRect(gridX, gridY, gridWidth, gridHeight);

        if (columnCount == 0 || rowCount == 0)
        {
            return;
        }

        _columnSizes = ResolveSizes(Columns, gridWidth, ColumnSpacing);
        _rowSizes = ResolveSizes(Rows, gridHeight, RowSpacing);
        _columnPositions = BuildPositions(gridX, _columnSizes, ColumnSpacing);
        _rowPositions = BuildPositions(gridY, _rowSizes, RowSpacing);

        int cellPadding = Math.Max(0, CellPadding);

        foreach (UiElement child in Children)
        {
            if (!_placements.TryGetValue(child, out GridPlacement? placement))
            {
                placement = new GridPlacement();
            }

            int column = Math.Clamp(placement.Column, 0, columnCount - 1);
            int row = Math.Clamp(placement.Row, 0, rowCount - 1);
            int columnSpan = Math.Max(1, placement.ColumnSpan);
            int rowSpan = Math.Max(1, placement.RowSpan);
            columnSpan = Math.Min(columnSpan, columnCount - column);
            rowSpan = Math.Min(rowSpan, rowCount - row);

            int lastColumn = column + columnSpan - 1;
            int lastRow = row + rowSpan - 1;

            int x = _columnPositions[column];
            int y = _rowPositions[row];
            int right = _columnPositions[lastColumn] + _columnSizes[lastColumn];
            int bottom = _rowPositions[lastRow] + _rowSizes[lastRow];

            int width = Math.Max(0, right - x);
            int height = Math.Max(0, bottom - y);

            UiRect cellBounds = new UiRect(
                x + cellPadding,
                y + cellPadding,
                Math.Max(0, width - cellPadding * 2),
                Math.Max(0, height - cellPadding * 2));

            child.Bounds = cellBounds;
        }
    }

    private void DrawGridLines(UiRenderContext context)
    {
        int columnCount = _columnSizes.Length;
        int rowCount = _rowSizes.Length;
        if (columnCount == 0 || rowCount == 0 || _gridBounds.Width <= 0 || _gridBounds.Height <= 0)
        {
            return;
        }

        int thickness = Math.Max(1, GridLineThickness);

        for (int col = 0; col < columnCount - 1; col++)
        {
            int x = _columnPositions[col] + _columnSizes[col];
            context.Renderer.FillRect(new UiRect(x, _gridBounds.Y, thickness, _gridBounds.Height), GridLineColor);
        }

        for (int row = 0; row < rowCount - 1; row++)
        {
            int y = _rowPositions[row] + _rowSizes[row];
            context.Renderer.FillRect(new UiRect(_gridBounds.X, y, _gridBounds.Width, thickness), GridLineColor);
        }
    }

    private static void EnsureDefinitionCount(List<UiGridDefinition> list, int count)
    {
        if (count < list.Count)
        {
            list.RemoveRange(count, list.Count - count);
            return;
        }

        while (list.Count < count)
        {
            list.Add(new UiGridDefinition());
        }
    }

    private static int[] ResolveSizes(IReadOnlyList<UiGridDefinition> definitions, int total, int spacing)
    {
        int count = definitions.Count;
        if (count == 0)
        {
            return Array.Empty<int>();
        }

        int spacingTotal = Math.Max(0, spacing) * Math.Max(0, count - 1);
        int available = Math.Max(0, total - spacingTotal);

        int fixedTotal = 0;
        float totalWeight = 0f;
        for (int i = 0; i < count; i++)
        {
            UiGridDefinition definition = definitions[i];
            if (definition.Size > 0)
            {
                fixedTotal += definition.Size;
            }
            else
            {
                totalWeight += definition.Weight > 0f ? definition.Weight : 1f;
            }
        }

        int remaining = Math.Max(0, available - fixedTotal);
        int[] sizes = new int[count];
        int used = 0;

        for (int i = 0; i < count; i++)
        {
            UiGridDefinition definition = definitions[i];
            int size;
            if (definition.Size > 0)
            {
                size = definition.Size;
            }
            else if (totalWeight > 0f)
            {
                float weight = definition.Weight > 0f ? definition.Weight : 1f;
                size = (int)Math.Round(remaining * (weight / totalWeight));
            }
            else
            {
                size = 0;
            }

            size = Math.Max(0, size);
            sizes[i] = size;
            used += size;
        }

        int diff = available - used;
        if (diff != 0 && count > 0)
        {
            int last = count - 1;
            sizes[last] = Math.Max(0, sizes[last] + diff);
        }

        return sizes;
    }

    private static int[] BuildPositions(int start, int[] sizes, int spacing)
    {
        int count = sizes.Length;
        int[] positions = new int[count];
        int cursor = start;
        int gap = Math.Max(0, spacing);

        for (int i = 0; i < count; i++)
        {
            positions[i] = cursor;
            cursor += sizes[i] + gap;
        }

        return positions;
    }

    private void CleanupPlacements()
    {
        if (_placements.Count == 0)
        {
            return;
        }

        List<UiElement>? toRemove = null;
        foreach (UiElement element in _placements.Keys)
        {
            if (!HasChild(element))
            {
                toRemove ??= new List<UiElement>();
                toRemove.Add(element);
            }
        }

        if (toRemove == null)
        {
            return;
        }

        foreach (UiElement element in toRemove)
        {
            _placements.Remove(element);
        }
    }

    private bool HasChild(UiElement element)
    {
        foreach (UiElement child in Children)
        {
            if (ReferenceEquals(child, element))
            {
                return true;
            }
        }

        return false;
    }
}
