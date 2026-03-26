using OpenControls;
using OpenControls.Controls;
using OpenControls.State;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiEditorReadinessTests
{
    [Fact]
    public void TextFieldTextInputRequestIncludesCaretAndCandidateBounds()
    {
        UiPanel root = new();
        UiTextField field = new()
        {
            Id = "field",
            Bounds = new UiRect(20, 30, 180, 28),
            Text = "hello"
        };
        field.SetCaretIndex(field.Text.Length);
        root.AddChild(field);

        UiContext context = new(root);
        context.Focus.RequestFocus(field);
        context.Update(new UiInputState
        {
            Composition = new UiTextCompositionState("xy", 0, 2, 2)
        });

        UiTextInputRequest request = Assert.IsType<UiTextInputRequest>(context.TextInputRequest);
        Assert.False(request.IsMultiLine);
        Assert.Equal(field.Bounds, request.Bounds);
        Assert.True(request.CaretBounds.Width > 0);
        Assert.True(request.CandidateBounds.Width >= request.CaretBounds.Width);
        Assert.True(request.SupportsComposition);
    }

    [Fact]
    public void StateSerializerRestoresEditorListAndTableState()
    {
        UiPanel root = new();

        UiTextEditor editor = CreateEditor("alpha\nbeta");
        editor.Id = "editor";
        editor.SelectRange(1, 4);
        editor.ScrollOffset = new UiPoint(12, 24);
        root.AddChild(editor);

        UiListView listView = CreateListView();
        listView.Id = "list";
        listView.FilterText = "be";
        listView.ScrollPanel.ScrollY = 32;
        listView.SelectionModel = new UiSelectionModel();
        listView.SelectionScope = "assets";
        listView.SelectionModel.SetSelection(new[] { 1, 2 }, primaryIndex: 2, anchorIndex: 1, listView.SelectionScope);
        root.AddChild(listView);

        UiTable table = CreateTable();
        table.Id = "table";
        table.ScrollOffset = new UiPoint(18, 44);
        table.SelectionModel = new UiSelectionModel();
        table.SelectionScope = "rows";
        table.SelectionModel.SetSelection(new[] { 0, 2 }, primaryIndex: 2, anchorIndex: 0, table.SelectionScope);
        table.ColumnStates[0].DisplayIndex = 1;
        table.ColumnStates[1].DisplayIndex = 0;
        table.ColumnStates[1].Visible = false;
        table.ColumnStates[2].WidthMode = UiTableColumnWidthMode.Fixed;
        table.ColumnStates[2].Width = 180;
        root.AddChild(table);

        UiStateSnapshot snapshot = UiStateSerializer.Capture(root);

        UiPanel restoredRoot = new();
        UiTextEditor restoredEditor = CreateEditor(string.Empty);
        restoredEditor.Id = "editor";
        restoredRoot.AddChild(restoredEditor);

        UiListView restoredList = CreateListView();
        restoredList.Id = "list";
        restoredList.SelectionModel = new UiSelectionModel();
        restoredList.SelectionScope = "assets";
        restoredRoot.AddChild(restoredList);

        UiTable restoredTable = CreateTable();
        restoredTable.Id = "table";
        restoredTable.SelectionModel = new UiSelectionModel();
        restoredTable.SelectionScope = "rows";
        restoredRoot.AddChild(restoredTable);

        UiStateSerializer.Apply(restoredRoot, snapshot);

        Assert.Equal(editor.Text, restoredEditor.Text);
        Assert.Equal(editor.SelectionStart, restoredEditor.SelectionStart);
        Assert.Equal(editor.SelectionEnd, restoredEditor.SelectionEnd);
        Assert.Equal(editor.ScrollX, restoredEditor.ScrollX);
        Assert.Equal(editor.ScrollY, restoredEditor.ScrollY);

        Assert.Equal(listView.FilterText, restoredList.FilterText);
        Assert.Equal(listView.ScrollPanel.ScrollY, restoredList.ScrollPanel.ScrollY);
        Assert.Equal(listView.SelectedIndices, restoredList.SelectedIndices);

        Assert.Equal(table.ScrollX, restoredTable.ScrollX);
        Assert.Equal(table.ScrollY, restoredTable.ScrollY);
        Assert.Equal(table.SelectedIndices, restoredTable.SelectedIndices);
        Assert.Equal(table.ColumnStates[0].DisplayIndex, restoredTable.ColumnStates[0].DisplayIndex);
        Assert.Equal(table.ColumnStates[1].Visible, restoredTable.ColumnStates[1].Visible);
        Assert.Equal(table.ColumnStates[2].Width, restoredTable.ColumnStates[2].Width);
    }

    [Fact]
    public void StateSerializerInvokesCustomStatefulElements()
    {
        UiPanel root = new();
        TestStatefulElement source = new()
        {
            Id = "custom",
            Counter = 42,
            Bounds = new UiRect(0, 0, 10, 10)
        };
        root.AddChild(source);

        UiStateSnapshot snapshot = UiStateSerializer.Capture(root);

        UiPanel restoredRoot = new();
        TestStatefulElement restored = new()
        {
            Id = "custom"
        };
        restoredRoot.AddChild(restored);

        UiStateSerializer.Apply(restoredRoot, snapshot);

        Assert.Equal(42, restored.Counter);
    }

    [Fact]
    public void DelegateImageSourceReportsOptionalMetadata()
    {
        UiDelegateImageSource source = new((_, _) => { }, () => new UiPoint(64, 32), "Preview");

        Assert.True(source.TryGetIntrinsicSize(out UiPoint size));
        Assert.Equal(new UiPoint(64, 32), size);
        Assert.Equal("Preview", source.DebugName);
    }

    private static UiTextEditor CreateEditor(string text)
    {
        UiTextEditor editor = new()
        {
            Bounds = new UiRect(0, 0, 320, 160),
            ShowLineNumbers = true
        };
        editor.SetText(text);
        return editor;
    }

    private static UiListView CreateListView()
    {
        UiListView list = new()
        {
            Bounds = new UiRect(0, 0, 220, 120),
            ItemHeight = 24
        };
        list.AddItem(new UiSelectableRow { Text = "Alpha" });
        list.AddItem(new UiSelectableRow { Text = "Beta" });
        list.AddItem(new UiSelectableRow { Text = "Gamma" });
        return list;
    }

    private static UiTable CreateTable()
    {
        UiTable table = new()
        {
            Bounds = new UiRect(0, 0, 360, 160),
            Rows =
            [
                new UiTableRow("Alpha", "Folder", "Loaded"),
                new UiTableRow("Beta", "File", "Dirty"),
                new UiTableRow("Gamma", "File", "Clean")
            ]
        };
        table.Columns.Add(new UiTableColumn("Name", width: 120));
        table.Columns.Add(new UiTableColumn("Type", width: 100));
        table.Columns.Add(new UiTableColumn("State", width: 140));
        return table;
    }

    private sealed class TestStatefulElement : UiElement, IUiStatefulElement
    {
        public int Counter { get; set; }

        public void CaptureState(UiElementState state)
        {
            state.CustomState["counter"] = Counter.ToString();
        }

        public void ApplyState(UiElementState state)
        {
            if (state.CustomState.TryGetValue("counter", out string? value)
                && int.TryParse(value, out int counter))
            {
                Counter = counter;
            }
        }
    }
}
