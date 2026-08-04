using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiDataTableTests
{
    private static UiDataTable BuildTable()
    {
        var table = new UiDataTable
        {
            Columns =
            [
                new UiDataTableColumn("Name", 120),
                new UiDataTableColumn("Price", 80),
            ],
        };
        table.SetRows(
        [
            ["Velar", "1,200"],
            ["Astra", "300"],
            ["Korin", "12,000"],
        ]);
        return table;
    }

    [Fact]
    public void UnsortedTablesDisplayRowsInInsertionOrder()
    {
        UiDataTable table = BuildTable();

        Assert.Equal(-1, table.SortColumnIndex);
        Assert.Equal([0, 1, 2], table.SortedRowIndices);
    }

    [Fact]
    public void SortingTogglesDirectionAndComparesNumbersNumerically()
    {
        UiDataTable table = BuildTable();
        (int Column, bool Descending)? observed = null;
        table.SortChanged += (column, descending) => observed = (column, descending);

        table.SortBy(1);
        Assert.Equal([1, 0, 2], table.SortedRowIndices); // 300, 1200, 12000
        Assert.Equal((1, false), observed);

        table.SortBy(1);
        Assert.Equal([2, 0, 1], table.SortedRowIndices);
        Assert.True(table.SortDescending);

        table.SortBy(0);
        Assert.Equal([1, 2, 0], table.SortedRowIndices); // Astra, Korin, Velar
        Assert.False(table.SortDescending);

        Assert.Throws<ArgumentOutOfRangeException>(() => table.SortBy(5));
    }

    [Fact]
    public void SelectionTracksTheUnsortedIndexAndClampsOnRowChanges()
    {
        UiDataTable table = BuildTable();
        table.SelectedRowIndex = 2;

        table.SetRows([["Only", "1"]]);
        Assert.Equal(0, table.SelectedRowIndex);

        table.SetRows([]);
        Assert.Equal(-1, table.SelectedRowIndex);
    }

    [Fact]
    public void MissingCellsCompareAsEmptyStrings()
    {
        var table = new UiDataTable
        {
            Columns = [new UiDataTableColumn("A", 60), new UiDataTableColumn("B", 60)],
        };
        table.SetRows([["z"], ["a", "extra"]]);

        table.SortBy(1);
        Assert.Equal([0, 1], table.SortedRowIndices); // "" before "extra"
    }

    [Fact]
    public void FocusedTablesNavigateWithArrowsInDisplayOrderAndActivateOnEnter()
    {
        UiDataTable table = BuildTable();
        table.Bounds = new UiRect(0, 0, 200, 100);
        table.SortBy(1); // display order by price: Astra, Velar, Korin
        table.SelectedRowIndex = 1; // Astra (display position 0)
        var clicks = new List<int>();
        table.RowClicked += clicks.Add;

        var focus = new UiFocusManager();
        focus.RequestFocus(table);
        UiUpdateContext Context(params UiKey[] pressed) => new(
            new UiInputState { KeysPressed = pressed },
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard());

        table.Update(Context(UiKey.Down));
        Assert.Equal(0, table.SelectedRowIndex); // Velar is next by price
        table.Update(Context(UiKey.Down));
        Assert.Equal(2, table.SelectedRowIndex); // Korin
        table.Update(Context(UiKey.Down));
        Assert.Equal(2, table.SelectedRowIndex); // clamped at the end
        table.Update(Context(UiKey.Up));
        Assert.Equal(0, table.SelectedRowIndex);
        table.Update(Context(UiKey.Enter));

        Assert.Equal([0, 2, 0, 0], clicks);

        // Without focus, keys do nothing.
        focus.RequestFocus(null);
        table.Update(Context(UiKey.Down));
        Assert.Equal(0, table.SelectedRowIndex);
    }
}
