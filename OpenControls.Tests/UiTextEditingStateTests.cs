using OpenControls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTextEditingStateTests
{
    [Fact]
    public void InsertText_ReplacesSelectionAndMovesCaret()
    {
        UiTextEditingState state = new();
        state.SetText("hello world");
        state.SelectRange(6, 11);

        bool changed = state.InsertText("there");

        Assert.True(changed);
        Assert.Equal("hello there", state.Text);
        Assert.Equal(11, state.CaretIndex);
        Assert.False(state.HasSelection);
    }

    [Fact]
    public void UndoAndRedo_RestorePreviousSnapshots()
    {
        UiTextEditingState state = new();
        state.SetText("abc");
        state.MoveEnd();

        Assert.True(state.InsertText("d"));
        Assert.Equal("abcd", state.Text);

        Assert.True(state.Undo());
        Assert.Equal("abc", state.Text);

        Assert.True(state.Redo());
        Assert.Equal("abcd", state.Text);
    }

    [Fact]
    public void CancelSession_RestoresOriginalFocusedText()
    {
        UiTextEditingState state = new();
        state.SetText("before");
        state.MoveEnd();
        state.BeginSession();

        Assert.True(state.InsertText(" after"));
        Assert.Equal("before after", state.Text);

        Assert.True(state.CancelSession());
        Assert.Equal("before", state.Text);
        Assert.Equal(6, state.CaretIndex);
        Assert.False(state.HasSelection);
    }

    [Fact]
    public void WordNavigation_UsesWordBoundaries()
    {
        UiTextEditingState state = new();
        state.SetText("alpha beta.gamma");
        state.MoveEnd();

        state.MoveLeft(byWord: true);
        Assert.Equal(11, state.CaretIndex);

        state.MoveLeft(byWord: true);
        Assert.Equal(10, state.CaretIndex);

        state.MoveLeft(byWord: true);
        Assert.Equal(6, state.CaretIndex);

        state.MoveRight(byWord: true);
        Assert.Equal(10, state.CaretIndex);

        state.MoveRight(byWord: true);
        Assert.Equal(11, state.CaretIndex);
    }
}
