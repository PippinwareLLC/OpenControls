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
}
