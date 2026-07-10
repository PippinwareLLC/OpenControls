using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiMenuActivationTests
{
    [Fact]
    public void MouseActivationExposesCurrentModifiersAndPreservesLegacyCallbacks()
    {
        (UiContext context, UiMenuBar menu, UiMenuBar.MenuItem item) = CreateOpenPopupMenu();
        List<string> callbacks = new();
        UiMenuItemActivation itemActivation = default;
        UiMenuItemActivation barActivation = default;
        UiMenuItemActivationSource? legacyItemSource = null;
        UiMenuItemActivationSource? legacyBarSource = null;

        item.Clicked = _ => callbacks.Add("clicked");
        item.Invoked = (_, source) =>
        {
            legacyItemSource = source;
            callbacks.Add("item-invoked");
        };
        menu.ItemInvoked += (_, source) =>
        {
            legacyBarSource = source;
            callbacks.Add("bar-invoked");
        };
        item.Activated = (_, activation) =>
        {
            itemActivation = activation;
            callbacks.Add("item-activated");
        };
        menu.ItemActivated += (_, activation) =>
        {
            barActivation = activation;
            callbacks.Add("bar-activated");
        };

        UiRect itemBounds = Assert.Single(menu.GetDebugOpenItemBounds());
        UiPoint point = new(itemBounds.X + itemBounds.Width / 2, itemBounds.Y + itemBounds.Height / 2);
        context.Update(new UiInputState
        {
            MousePosition = point,
            ScreenMousePosition = point,
            LeftClicked = true,
            LeftDown = true,
            ShiftDown = true,
            AltDown = true
        });

        Assert.Equal(
            ["clicked", "item-invoked", "item-activated", "bar-invoked", "bar-activated"],
            callbacks);
        Assert.Equal(UiMenuItemActivationSource.Mouse, legacyItemSource);
        Assert.Equal(UiMenuItemActivationSource.Mouse, legacyBarSource);
        Assert.Equal(
            new UiMenuItemActivation(UiMenuItemActivationSource.Mouse, UiModifierKeys.Shift | UiModifierKeys.Alt),
            itemActivation);
        Assert.Equal(itemActivation, barActivation);
    }

    [Fact]
    public void KeyboardActivationExposesModifiersHeldOnTheActivationFrame()
    {
        (UiContext context, UiMenuBar menu, UiMenuBar.MenuItem item) = CreateOpenPopupMenu();
        UiMenuItemActivation? actual = null;
        item.Activated = (_, activation) => actual = activation;

        context.RequestFocus(menu);
        context.Update(new UiInputState
        {
            CtrlDown = true,
            AltDown = true,
            Navigation = new UiNavigationInput { Enter = true }
        });

        Assert.Equal(
            new UiMenuItemActivation(UiMenuItemActivationSource.Keyboard, UiModifierKeys.Ctrl | UiModifierKeys.Alt),
            Assert.IsType<UiMenuItemActivation>(actual));
    }

    [Fact]
    public void ShortcutActivationReportsActualInputModifiers()
    {
        UiPanel root = new() { Bounds = new UiRect(0, 0, 320, 200) };
        UiMenuBar menu = new()
        {
            Bounds = new UiRect(0, 0, 320, 24),
            EnableShortcutDispatch = true
        };
        UiMenuBar.MenuItem item = new()
        {
            Text = "Duplicate",
            Shortcut = "Primary+Shift+Alt+D"
        };
        UiMenuBar.MenuItem layer = new() { Text = "Layer" };
        layer.Items.Add(item);
        menu.Items.Add(layer);
        root.AddChild(menu);

        UiMenuItemActivation? actual = null;
        item.Activated = (_, activation) => actual = activation;
        UiContext context = new(root);
        context.Update(new UiInputState
        {
            CtrlDown = true,
            ShiftDown = true,
            AltDown = true,
            KeysPressed = [UiKey.D]
        });

        Assert.Equal(
            new UiMenuItemActivation(
                UiMenuItemActivationSource.Shortcut,
                UiModifierKeys.Ctrl | UiModifierKeys.Shift | UiModifierKeys.Alt),
            Assert.IsType<UiMenuItemActivation>(actual));
    }

    [Fact]
    public void ProgrammaticActivationReportsNoModifiers()
    {
        UiMenuBar menu = new();
        UiMenuBar.MenuItem item = new()
        {
            Text = "Duplicate",
            CommandId = "layer.duplicate"
        };
        menu.Items.Add(item);

        UiMenuItemActivation? actual = null;
        item.Activated = (_, activation) => actual = activation;

        Assert.True(menu.TryInvokeCommand("layer.duplicate"));
        Assert.Equal(
            new UiMenuItemActivation(UiMenuItemActivationSource.Programmatic, UiModifierKeys.None),
            Assert.IsType<UiMenuItemActivation>(actual));
    }

    private static (UiContext Context, UiMenuBar Menu, UiMenuBar.MenuItem Item) CreateOpenPopupMenu()
    {
        UiPanel root = new() { Bounds = new UiRect(0, 0, 320, 200) };
        UiMenuBar menu = new()
        {
            DisplayMode = UiMenuDisplayMode.Popup,
            Bounds = new UiRect(20, 20, 180, 0)
        };
        UiMenuBar.MenuItem item = new() { Text = "Duplicate Layer" };
        menu.Items.Add(item);
        root.AddChild(menu);

        menu.OpenPopup();
        UiContext context = new(root);
        context.Update(new UiInputState());
        return (context, menu, item);
    }
}
