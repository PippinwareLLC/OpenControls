using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiModalFocusTests
{
    [Fact]
    public void EscapeRestoresPreviousFocusWithoutLeavingHiddenTextInputFocused()
    {
        (UiContext context, UiModalHost host, UiButton previous, UiModal modal, UiTextField field) =
            CreateModalFixture();

        context.Focus.RequestFocus(previous);
        modal.QueueFocus(field);
        modal.Open();
        context.Update(new UiInputState());

        Assert.Same(field, context.Focus.Focused);
        Assert.True(context.WantTextInput);

        context.Update(new UiInputState
        {
            Navigation = new UiNavigationInput { Escape = true }
        });

        Assert.False(modal.IsOpen);
        Assert.Null(host.ActiveModal);
        Assert.Same(previous, context.Focus.Focused);
        Assert.False(context.WantTextInput);
    }

    [Fact]
    public void StackedAndReplacementModalsRestoreOnlyAfterTheFinalModalCloses()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiButton previous = new()
        {
            Bounds = new UiRect(10, 10, 100, 28),
            Text = "Previous"
        };
        UiModal first = CreateModal(new UiRect(100, 80, 260, 140), out UiTextField firstField);
        UiModal second = CreateModal(new UiRect(140, 110, 260, 140), out UiTextField secondField);
        host.AddChild(previous);
        host.AddChild(first);
        host.AddChild(second);

        UiContext context = new(host);
        context.Focus.RequestFocus(previous);
        first.QueueFocus(firstField);
        first.Open();
        context.Update(new UiInputState());
        Assert.Same(firstField, context.Focus.Focused);

        second.QueueFocus(secondField);
        second.Open();
        context.Update(new UiInputState());
        Assert.Same(secondField, context.Focus.Focused);

        second.Close();
        context.Update(new UiInputState());
        Assert.Same(first, host.ActiveModal);
        Assert.Same(firstField, context.Focus.Focused);
        Assert.NotSame(previous, context.Focus.Focused);

        // Replace the remaining modal without exposing a no-modal frame. The original
        // pre-sequence focus must remain the restore target rather than firstField.
        first.Close();
        second.QueueFocus(secondField);
        second.Open();
        context.Update(new UiInputState());
        Assert.Same(secondField, context.Focus.Focused);

        second.Close();
        context.Update(new UiInputState());
        Assert.Same(previous, context.Focus.Focused);
    }

    [Fact]
    public void DetachedOrDisabledPreviousTargetIsNotRestored()
    {
        (UiContext context, UiModalHost host, UiButton previous, UiModal modal, UiTextField field) =
            CreateModalFixture();

        context.Focus.RequestFocus(previous);
        modal.QueueFocus(field);
        modal.Open();
        context.Update(new UiInputState());
        Assert.Same(field, context.Focus.Focused);

        Assert.True(host.RemoveChild(previous));
        modal.Close();
        context.Update(new UiInputState());

        Assert.Null(context.Focus.Focused);
        Assert.False(context.WantTextInput);
    }

    [Fact]
    public void ExplicitValidFocusChosenByCloseHandlerIsPreserved()
    {
        (UiContext context, UiModalHost host, UiButton previous, UiModal modal, UiTextField field) =
            CreateModalFixture();
        UiButton explicitDestination = new()
        {
            Bounds = new UiRect(120, 10, 100, 28),
            Text = "Destination"
        };
        host.AddChild(explicitDestination);

        context.Focus.RequestFocus(previous);
        modal.QueueFocus(field);
        modal.Open();
        context.Update(new UiInputState());

        modal.Closed += () => context.Focus.RequestFocus(explicitDestination);
        modal.Close();
        context.Update(new UiInputState());

        Assert.Same(explicitDestination, context.Focus.Focused);
    }

    [Fact]
    public void ModalOpenedByEarlierSiblingDuringUpdateRestoresTheOpeningControl()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiButton opener = new()
        {
            Bounds = new UiRect(10, 10, 100, 28),
            Text = "Open"
        };
        UiModal modal = CreateModal(new UiRect(100, 80, 260, 140), out UiTextField field);
        opener.Clicked += () =>
        {
            modal.QueueFocus(field);
            modal.Open();
        };
        host.AddChild(opener);
        host.AddChild(modal);

        UiContext context = new(host);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(20, 20),
            LeftClicked = true,
            LeftDown = true
        });
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(20, 20),
            LeftReleased = true
        });

        Assert.True(modal.IsOpen);
        Assert.Same(field, context.Focus.Focused);

        context.Update(new UiInputState
        {
            Navigation = new UiNavigationInput { Escape = true }
        });

        Assert.False(modal.IsOpen);
        Assert.Same(opener, context.Focus.Focused);
    }

    [Fact]
    public void PreviousFocusInsideATabThatBecomesInactiveIsNotRestored()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiTabBar tabs = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiTabItem first = new() { Text = "First" };
        UiTextField previous = new() { Bounds = new UiRect(20, 50, 180, 28), Text = "Previous" };
        first.AddChild(previous);
        UiTabItem second = new() { Text = "Second" };
        first.Bounds = tabs.ContentBounds;
        second.Bounds = tabs.ContentBounds;
        tabs.AddChild(first);
        tabs.AddChild(second);
        tabs.ActiveIndex = 0;
        UiModal modal = CreateModal(new UiRect(100, 80, 260, 140), out UiTextField modalField);
        host.AddChild(tabs);
        host.AddChild(modal);

        UiContext context = new(host);
        context.Update(new UiInputState());
        context.Focus.RequestFocus(previous);
        modal.QueueFocus(modalField);
        modal.Open();
        context.Update(new UiInputState());
        Assert.Same(modalField, context.Focus.Focused);

        tabs.ActiveIndex = 1;
        modal.Close();
        context.Update(new UiInputState());

        Assert.NotSame(previous, context.Focus.Focused);
        Assert.False(context.WantTextInput);
    }

    private static (UiContext Context, UiModalHost Host, UiButton Previous, UiModal Modal, UiTextField Field)
        CreateModalFixture()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiButton previous = new()
        {
            Bounds = new UiRect(10, 10, 100, 28),
            Text = "Previous"
        };
        UiModal modal = CreateModal(new UiRect(100, 80, 260, 140), out UiTextField field);
        host.AddChild(previous);
        host.AddChild(modal);
        return (new UiContext(host), host, previous, modal, field);
    }

    private static UiModal CreateModal(UiRect bounds, out UiTextField field)
    {
        UiModal modal = new() { Bounds = bounds };
        field = new UiTextField
        {
            Bounds = new UiRect(bounds.X + 20, bounds.Y + 30, 180, 28),
            Text = "Layer"
        };
        modal.AddChild(field);
        return modal;
    }
}
