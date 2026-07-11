using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiActiveInputLayerTabFocusTests
{
    [Fact]
    public void PopupScopesForwardAndReverseTabTraversalToItsOwnFields()
    {
        TestRoot root = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiTextEditor behind = new()
        {
            Bounds = new UiRect(10, 10, 120, 24),
            AllowTabInput = true
        };
        UiPopup popup = new() { Bounds = new UiRect(100, 80, 260, 160) };
        UiTextField first = new() { Bounds = new UiRect(120, 100, 180, 24) };
        UiTextField second = new() { Bounds = new UiRect(120, 140, 180, 24) };
        popup.AddChild(first);
        popup.AddChild(second);
        root.AddChild(behind);
        root.AddChild(popup);
        popup.Open();
        UiContext context = new(root);
        context.Focus.RequestFocus(behind);

        context.Update(Tab());
        Assert.Same(first, context.Focus.Focused);
        context.Update(Tab());
        Assert.Same(second, context.Focus.Focused);
        context.Update(Tab(reverse: true));
        Assert.Same(first, context.Focus.Focused);
        context.Update(Tab(reverse: true));
        Assert.Same(second, context.Focus.Focused);
        Assert.NotSame(behind, context.Focus.Focused);
    }

    [Fact]
    public void ModalScopesTabTraversalEvenWhenBackgroundOwnedFocusBeforeUpdate()
    {
        UiModalHost host = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiButton behind = new() { Bounds = new UiRect(10, 10, 100, 28), Text = "Behind" };
        UiModal modal = new() { Bounds = new UiRect(100, 80, 260, 160) };
        UiTextField first = new() { Bounds = new UiRect(120, 100, 180, 24) };
        UiTextField second = new() { Bounds = new UiRect(120, 140, 180, 24) };
        modal.AddChild(first);
        modal.AddChild(second);
        host.AddChild(behind);
        host.AddChild(modal);
        modal.Open();
        UiContext context = new(host);
        context.Focus.RequestFocus(behind);

        context.Update(Tab());
        Assert.Same(first, context.Focus.Focused);
        context.Update(Tab());
        Assert.Same(second, context.Focus.Focused);
        Assert.NotSame(behind, context.Focus.Focused);
    }

    [Fact]
    public void OpenMenuNeverLetsTabMoveFocusIntoBackgroundControls()
    {
        TestRoot root = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiButton behind = new() { Bounds = new UiRect(10, 10, 100, 28), Text = "Behind" };
        UiMenuBar menu = new()
        {
            Bounds = new UiRect(100, 80, 180, 0),
            DisplayMode = UiMenuDisplayMode.Popup
        };
        menu.Items.Add(new UiMenuBar.MenuItem { Text = "Command" });
        root.AddChild(behind);
        root.AddChild(menu);
        menu.OpenPopup();
        UiContext context = new(root);
        context.Focus.RequestFocus(behind);

        context.Update(Tab());

        Assert.NotSame(behind, context.Focus.Focused);
        Assert.True(menu.HasOpenMenu);
        Assert.Same(menu, context.ActiveInputLayer);
    }

    [Fact]
    public void DismissedPopupDoesNotBlockTheFirstFollowingInputFrame()
    {
        UiWindow window = new() { Bounds = new UiRect(0, 0, 640, 480) };
        UiButton button = new()
        {
            Bounds = new UiRect(20, 20, 120, 28),
            Text = "Immediate target"
        };
        UiPopup popup = new() { Bounds = new UiRect(100, 80, 260, 160) };
        window.AddContentChild(button);
        window.AddContentChild(popup);
        int clicks = 0;
        button.Clicked += () => clicks++;
        popup.Open();
        UiContext context = new(window);
        context.Update(new UiInputState());
        Assert.Same(popup, context.ActiveInputLayer);

        window.CancelTransientInteractions();
        Assert.False(popup.IsOpen);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(30, 30),
            ScreenMousePosition = new UiPoint(30, 30),
            LeftClicked = true,
            LeftDown = true
        });
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(30, 30),
            ScreenMousePosition = new UiPoint(30, 30),
            LeftReleased = true
        });

        Assert.Equal(1, clicks);
        Assert.Null(context.ActiveInputLayer);
    }

    private static UiInputState Tab(bool reverse = false) => new()
    {
        ShiftDown = reverse,
        Navigation = new UiNavigationInput { Tab = true }
    };

    private sealed class TestRoot : UiElement
    {
    }
}
