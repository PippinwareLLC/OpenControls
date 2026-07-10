using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiTextFieldFocusTests
{
    [Fact]
    public void FocusLostFiresOnceWhenFocusMovesAway()
    {
        UiPanel root = new();
        UiTextField field = new()
        {
            Bounds = new UiRect(10, 10, 160, 28),
            Text = "Layer 1"
        };
        UiButton destination = new()
        {
            Bounds = new UiRect(10, 50, 80, 28),
            Text = "Done"
        };
        root.AddChild(field);
        root.AddChild(destination);

        int focusLostCount = 0;
        field.FocusLost += () => focusLostCount++;

        UiContext context = new(root);
        context.Focus.RequestFocus(field);
        context.Focus.RequestFocus(destination);
        context.Focus.RequestFocus(destination);

        Assert.Equal(1, focusLostCount);
        Assert.Same(destination, context.Focus.Focused);
    }

    [Fact]
    public void EscapeRaisesCancelledBeforeFocusLost()
    {
        UiPanel root = new();
        UiTextField field = new()
        {
            Bounds = new UiRect(10, 10, 160, 28),
            Text = "Layer 1"
        };
        root.AddChild(field);

        List<string> events = new();
        field.Cancelled += () => events.Add("cancelled");
        field.FocusLost += () => events.Add("focus-lost");

        UiContext context = new(root);
        context.Focus.RequestFocus(field);
        context.Update(new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                Escape = true
            }
        });

        Assert.Equal(["cancelled", "focus-lost"], events);
        Assert.Null(context.Focus.Focused);
    }
}
