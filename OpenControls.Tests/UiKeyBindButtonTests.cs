using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiKeyBindButtonTests
{
    private static UiUpdateContext Context(UiInputState input) => new(
        input,
        new UiFocusManager(),
        new UiDragDropContext(),
        1f / 60f,
        UiFont.Default,
        new UiMemoryClipboard());

    private static UiKeyBindButton Bound() => new()
    {
        Label = "Pitch up",
        Bounds = new UiRect(0, 0, 160, 24),
        BoundKey = UiKey.Up,
    };

    private static void ClickToListen(UiKeyBindButton button)
    {
        button.Update(Context(new UiInputState
        {
            MousePosition = new UiPoint(10, 10),
            LeftClicked = true,
            LeftDown = true,
        }));
        button.Update(Context(new UiInputState
        {
            MousePosition = new UiPoint(10, 10),
            LeftReleased = true,
        }));
    }

    [Fact]
    public void ClickingEntersListeningAndTheNextKeyBinds()
    {
        UiKeyBindButton button = Bound();
        UiKey? reported = null;
        button.KeyBound += key => reported = key;

        ClickToListen(button);
        Assert.True(button.Listening);

        button.Update(Context(new UiInputState { KeysPressed = [UiKey.J] }));
        Assert.False(button.Listening);
        Assert.Equal(UiKey.J, button.BoundKey);
        Assert.Equal(UiKey.J, reported);
    }

    [Fact]
    public void EscapeCancelsListeningWithoutRebinding()
    {
        UiKeyBindButton button = Bound();
        bool raised = false;
        button.KeyBound += _ => raised = true;

        ClickToListen(button);
        button.Update(Context(new UiInputState { KeysPressed = [UiKey.Escape] }));

        Assert.False(button.Listening);
        Assert.Equal(UiKey.Up, button.BoundKey);
        Assert.False(raised);
    }

    [Fact]
    public void KeysAreIgnoredWhenNotListening()
    {
        UiKeyBindButton button = Bound();
        button.Update(Context(new UiInputState { KeysPressed = [UiKey.Z] }));
        Assert.Equal(UiKey.Up, button.BoundKey);
    }
}
