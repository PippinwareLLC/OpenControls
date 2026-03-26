using OpenControls.Controls;
using OpenControls.State;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTextEditorTests
{
    [Fact]
    public void KeyboardShortcuts_CopyCutPasteUndoRedo_WorkAcrossLines()
    {
        UiTextEditor editor = CreateEditor("alpha\nbeta\ngamma");
        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        FocusEditor(editor, focus, clipboard);

        Update(editor, focus, clipboard, new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.A }
        });

        Assert.True(editor.HasSelection);
        Assert.Equal("alpha\nbeta\ngamma", editor.SelectedText);

        Update(editor, focus, clipboard, new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.C }
        });

        Assert.Equal("alpha\nbeta\ngamma", clipboard.GetText());
        Assert.Equal("alpha\nbeta\ngamma", editor.Text);

        Update(editor, focus, clipboard, new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.X }
        });

        Assert.Equal(string.Empty, editor.Text);
        Assert.Equal(0, editor.CaretIndex);
        Assert.False(editor.HasSelection);

        Update(editor, focus, clipboard, new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.V }
        });

        Assert.Equal("alpha\nbeta\ngamma", editor.Text);

        Update(editor, focus, clipboard, new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.Z }
        });

        Assert.Equal(string.Empty, editor.Text);

        Update(editor, focus, clipboard, new UiInputState
        {
            CtrlDown = true,
            KeysPressed = new[] { UiKey.Y }
        });

        Assert.Equal("alpha\nbeta\ngamma", editor.Text);
    }

    [Fact]
    public void Navigation_HomeWordAndPageMove_UseEditorSemantics()
    {
        UiTextEditor editor = CreateEditor("    alpha\nbeta\ngamma\ndelta\nepsilon\nzeta\neta\ntheta\niota\nkappa", width: 320, height: 72);
        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        FocusEditor(editor, focus, clipboard);
        editor.SetCaretIndex("    alpha".Length);

        Update(editor, focus, clipboard, new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                Home = true
            }
        });

        Assert.Equal(0, editor.CaretLine);
        Assert.Equal(4, editor.CaretColumn);

        Update(editor, focus, clipboard, new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                Home = true
            }
        });

        Assert.Equal(0, editor.CaretColumn);

        editor.SetCaretIndex(editor.Text.IndexOf("beta", StringComparison.Ordinal));
        Update(editor, focus, clipboard, new UiInputState
        {
            CtrlDown = true,
            Navigation = new UiNavigationInput
            {
                MoveLeft = true
            }
        });

        Assert.Equal(0, editor.CaretLine);
        Assert.Equal(4, editor.CaretColumn);

        editor.SetCaretIndex(0);
        Update(editor, focus, clipboard, new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                PageDown = true
            }
        });

        int pagedLine = editor.CaretLine;
        Assert.True(pagedLine > 0);

        Update(editor, focus, clipboard, new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                PageUp = true
            }
        });

        Assert.Equal(0, editor.CaretLine);
    }

    [Fact]
    public void EventsAndStateSerialization_TrackEditorChanges()
    {
        UiTextEditor editor = CreateEditor("hello");
        editor.Id = "editor";

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();
        int textChanged = 0;
        int caretMoved = 0;
        int selectionChanged = 0;

        editor.TextChanged += () => textChanged++;
        editor.CaretMoved += () => caretMoved++;
        editor.SelectionChanged += () => selectionChanged++;

        FocusEditor(editor, focus, clipboard);

        Update(editor, focus, clipboard, new UiInputState
        {
            TextInput = new[] { '!' }
        });

        Update(editor, focus, clipboard, new UiInputState
        {
            ShiftDown = true,
            Navigation = new UiNavigationInput
            {
                MoveLeft = true
            }
        });

        Assert.Equal("hello!", editor.Text);
        Assert.True(textChanged > 0);
        Assert.True(caretMoved > 0);
        Assert.True(selectionChanged > 0);
        Assert.True(editor.HasSelection);

        UiPanel root = new();
        root.AddChild(editor);
        UiStateSnapshot snapshot = UiStateSerializer.Capture(root);

        UiPanel restoredRoot = new();
        UiTextEditor restored = CreateEditor(string.Empty);
        restored.Id = "editor";
        restoredRoot.AddChild(restored);

        UiStateSerializer.Apply(restoredRoot, snapshot);

        Assert.Equal(editor.Text, restored.Text);
        Assert.Equal(editor.CaretIndex, restored.CaretIndex);
    }

    private static UiTextEditor CreateEditor(string text, int width = 320, int height = 160)
    {
        UiTextEditor editor = new()
        {
            Bounds = new UiRect(0, 0, width, height),
            ShowLineNumbers = true
        };
        editor.SetText(text);
        return editor;
    }

    private static void FocusEditor(UiTextEditor editor, UiFocusManager focus, IUiClipboard clipboard)
    {
        Update(editor, focus, clipboard, new UiInputState
        {
            MousePosition = new UiPoint(120, 12),
            ScreenMousePosition = new UiPoint(120, 12),
            LeftClicked = true,
            LeftDown = true
        });
    }

    private static void Update(UiTextEditor editor, UiFocusManager focus, IUiClipboard clipboard, UiInputState input)
    {
        editor.Update(new UiUpdateContext(
            input,
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            clipboard));
    }
}
