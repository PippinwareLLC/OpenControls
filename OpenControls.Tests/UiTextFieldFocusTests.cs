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

    [Fact]
    public void FocusLostCanRedirectFocusWithoutReenteringTheTransition()
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

        UiContext context = new(root);
        int focusLostCount = 0;
        field.FocusLost += () =>
        {
            focusLostCount++;
            context.Focus.ClearFocus();
        };

        context.Focus.RequestFocus(field);
        context.Focus.RequestFocus(destination);

        Assert.Equal(1, focusLostCount);
        Assert.Null(context.Focus.Focused);
    }

    [Fact]
    public void SwitchingDockTabsClearsTextFocusHiddenByTheInactiveWindowAncestor()
    {
        UiDockHost host = new()
        {
            Bounds = new UiRect(0, 0, 320, 180)
        };
        UiWindow first = new() { Title = "First" };
        UiTextField field = new()
        {
            Bounds = new UiRect(10, 40, 160, 28),
            Text = "Layer 1"
        };
        first.AddContentChild(field);
        UiWindow second = new() { Title = "Second" };
        TabSwitchElement switcher = new(() => host.ActivateWindow(1));
        first.AddContentChild(switcher);
        host.AddWindow(first);
        host.AddWindow(second);

        UiContext context = new(host);
        int focusLostCount = 0;
        field.FocusLost += () => focusLostCount++;
        context.Focus.RequestFocus(field);

        Assert.Same(field, context.Focus.Focused);
        Assert.True(field.Visible);
        Assert.True(first.Visible);

        switcher.Armed = true;
        context.Update(new UiInputState());

        Assert.Null(context.Focus.Focused);
        Assert.False(context.WantTextInput);
        Assert.Equal(1, focusLostCount);
        Assert.False(first.Visible);
        Assert.True(second.Visible);
    }

    private sealed class TabSwitchElement(Action switchTab) : UiElement
    {
        private readonly Action _switchTab = switchTab;

        public bool Armed { get; set; }

        public override void Update(UiUpdateContext context)
        {
            if (!Armed)
            {
                return;
            }

            Armed = false;
            _switchTab();
        }
    }
}
