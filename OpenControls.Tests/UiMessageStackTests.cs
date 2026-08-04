using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiMessageStackTests
{
    [Fact]
    public void PushedMessagesDisplayOldestFirstAndOverflowDropsTheOldest()
    {
        var stack = new UiMessageStack { MaxMessages = 2 };

        Assert.True(stack.Push("one"));
        Assert.True(stack.Push("two"));
        Assert.True(stack.Push("three"));

        Assert.Equal(["two", "three"], stack.Messages.Select(static entry => entry.Text));
    }

    [Fact]
    public void ConsecutiveDuplicatesAreSuppressedByTextDespiteChangingPrefixes()
    {
        var stack = new UiMessageStack();

        Assert.True(stack.Push("Docked.", prefix: "[00:00] "));
        Assert.False(stack.Push("Docked.", prefix: "[00:01] "));
        Assert.True(stack.Push("Launched.", prefix: "[00:02] "));
        Assert.True(stack.Push("Docked.", prefix: "[00:03] "));

        Assert.Equal(3, stack.Messages.Count);
    }

    [Fact]
    public void SuppressionCanBeDisabledAndBlankMessagesAreAlwaysRejected()
    {
        var stack = new UiMessageStack { SuppressConsecutiveDuplicates = false };

        Assert.True(stack.Push("ping"));
        Assert.True(stack.Push("ping"));
        Assert.False(stack.Push("   "));
        Assert.False(stack.Push(""));

        Assert.Equal(2, stack.Messages.Count);
    }

    [Fact]
    public void SeverityAndPrefixAreRetainedAndPushRaisesTheEvent()
    {
        var stack = new UiMessageStack();
        UiMessageEntry? observed = null;
        stack.MessagePushed += entry => observed = entry;

        stack.Push("Hull critical.", UiMessageSeverity.Alert, "[3210] ");

        Assert.Equal(new UiMessageEntry("[3210] ", "Hull critical.", UiMessageSeverity.Alert), stack.Messages[^1]);
        Assert.Equal(observed, stack.Messages[^1]);
    }

    [Fact]
    public void ShrinkingMaxMessagesTrimsExistingOverflowAndClearEmptiesTheLog()
    {
        var stack = new UiMessageStack { MaxMessages = 4 };
        stack.Push("a");
        stack.Push("b");
        stack.Push("c");

        stack.MaxMessages = 2;
        Assert.Equal(["b", "c"], stack.Messages.Select(static entry => entry.Text));

        stack.Clear();
        Assert.Empty(stack.Messages);
    }
}
