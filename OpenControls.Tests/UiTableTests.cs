using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTableTests
{
    [Fact]
    public void Table_ReorderedVisibleColumnsStillSortByStableModelIndex()
    {
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 240, 120),
            RowHeight = 20,
            HeaderHeight = 20
        };
        table.Columns.Add(new UiTableColumn("Name", width: 80) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Type", width: 80) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Columns.Add(new UiTableColumn("Size", width: 80) { WidthMode = UiTableColumnWidthMode.Fixed });
        table.Rows = new[]
        {
            new UiTableRow("Asset 00", "Scene", "64 KB"),
            new UiTableRow("Asset 01", "Texture", "72 KB")
        };
        table.GetColumnState(1).Visible = false;

        Update(table, new UiInputState());
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(120, 10),
            ScreenMousePosition = new UiPoint(120, 10),
            LeftClicked = true,
            LeftDown = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftDown = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftReleased = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftClicked = true,
            LeftDown = true
        });
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 10),
            ScreenMousePosition = new UiPoint(20, 10),
            LeftReleased = true
        });

        Assert.Single(table.SortSpecs);
        Assert.Equal(2, table.SortSpecs[0].ColumnIndex);
    }

    [Fact]
    public void Table_ViewStateTracksScrollAndVisibleRows()
    {
        UiTable table = CreateTable(220, 120, rowCount: 20);
        table.ScrollY = 40;

        Update(table, new UiInputState());

        Assert.True(table.ViewState.ContentSize.Y > table.ViewState.ViewportSize.Y);
        Assert.Equal(2, table.ViewState.FirstVisibleRowIndex);
        Assert.Equal(6, table.ViewState.LastVisibleRowIndex);
    }

    [Fact]
    public void Table_HeaderContextMenuCanHideColumns()
    {
        UiTable table = CreateTable(240, 120, rowCount: 6);

        Update(table, new UiInputState());
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(12, 10),
            ScreenMousePosition = new UiPoint(12, 10),
            RightClicked = true,
            RightDown = true
        });
        Update(table, new UiInputState());
        Update(table, new UiInputState
        {
            MousePosition = new UiPoint(20, 42),
            ScreenMousePosition = new UiPoint(20, 42),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.False(table.GetColumnState(0).Visible);
        Assert.Equal(2, table.ColumnStates.Count(state => state.Visible));
    }

    [Fact]
    public void Table_RichCellContentParticipatesInHitTesting()
    {
        UiLabel content = new()
        {
            Text = "Nested",
            Bounds = new UiRect(0, 0, 0, 0)
        };

        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 220, 100),
            RowHeight = 28
        };
        table.Columns.Add(new UiTableColumn("Name", weight: 2f));
        table.Columns.Add(new UiTableColumn("Kind", weight: 1f));
        table.Rows = new[]
        {
            new UiTableRow
            {
                CellItems = new[]
                {
                    new UiTableCell { Content = content },
                    new UiTableCell("Folder")
                }
            }
        };

        Update(table, new UiInputState());

        UiElement? hit = table.HitTest(new UiPoint(12, 36));

        Assert.Same(content, hit);
        Assert.True(content.Bounds.Width > 0);
        Assert.True(content.Bounds.Height > 0);
    }

    [Fact]
    public void Table_RichCellContentExposesScreenSpaceDebugBoundsThroughUiContext()
    {
        UiLabel content = new()
        {
            Text = "Nested",
            Bounds = new UiRect(0, 0, 0, 0)
        };

        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 400, 240)
        };
        UiTable table = new()
        {
            Bounds = new UiRect(40, 30, 220, 100),
            RowHeight = 28
        };
        table.Columns.Add(new UiTableColumn("Name", weight: 2f));
        table.Columns.Add(new UiTableColumn("Kind", weight: 1f));
        table.Rows = new[]
        {
            new UiTableRow
            {
                CellItems = new[]
                {
                    new UiTableCell { Content = content },
                    new UiTableCell("Folder")
                }
            }
        };
        root.AddChild(table);

        UiContext context = new(root);
        context.Update(new UiInputState());

        UiItemStateSnapshot state = context.GetItemState(content);

        Assert.Contains(content, context.GetDebugChildren(table));
        Assert.True(context.TryGetVisibleBounds(content, out UiRect visibleBounds));
        Assert.True(state.Bounds.X >= table.Bounds.X);
        Assert.True(state.Bounds.Y >= table.Bounds.Y);
        Assert.Equal(state.Bounds, visibleBounds);
        Assert.True(visibleBounds.Contains(new UiPoint(table.Bounds.X + 12, table.Bounds.Y + 36)));
    }

    private static UiTable CreateTable(int width, int height, int rowCount)
    {
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, width, height),
            RowHeight = 20,
            HeaderHeight = 20,
            ScrollbarThickness = 12
        };

        table.Columns.Add(new UiTableColumn("Name", weight: 2f));
        table.Columns.Add(new UiTableColumn("Type", weight: 1f));
        table.Columns.Add(new UiTableColumn("Size", weight: 1f));

        UiTableRow[] rows = new UiTableRow[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            rows[i] = new UiTableRow($"Asset {i:D2}", i % 2 == 0 ? "Scene" : "Texture", $"{64 + i} KB");
        }

        table.Rows = rows;
        return table;
    }

    private static void Update(UiElement element, UiInputState input)
    {
        element.Update(new UiUpdateContext(
            input,
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));
    }
}
