using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiMenuExternalShortcutTests
{
    [Fact]
    public void ExternalDispatchActivatesEnabledItemWithModifiersAndClosesOpenMenu()
    {
        (UiMenuBar menu, UiMenuBar.MenuItem item) = CreatePopupMenu("Primary+Shift+Alt+D");
        UiFocusManager externalFocus = new();
        UiMenuItemActivation? activation = null;
        item.Activated = (_, value) => activation = value;
        menu.OpenPopup();

        bool dispatched = menu.TryDispatchShortcut(
            new UiInputState
            {
                CtrlDown = true,
                ShiftDown = true,
                AltDown = true,
                KeysPressed = [UiKey.D]
            },
            externalFocus);

        Assert.True(dispatched);
        Assert.Equal(
            new UiMenuItemActivation(
                UiMenuItemActivationSource.Shortcut,
                UiModifierKeys.Ctrl | UiModifierKeys.Shift | UiModifierKeys.Alt),
            Assert.IsType<UiMenuItemActivation>(activation));
        Assert.False(menu.IsPopupOpen);
        Assert.False(menu.HasOpenMenu);
    }

    [Fact]
    public void ExternalDispatchIgnoresDisabledItemWithoutClosingOpenMenu()
    {
        (UiMenuBar menu, UiMenuBar.MenuItem item) = CreatePopupMenu("Primary+D");
        UiFocusManager externalFocus = new();
        int activationCount = 0;
        item.Enabled = false;
        item.Activated = (_, _) => activationCount++;
        menu.OpenPopup();

        bool dispatched = menu.TryDispatchShortcut(
            new UiInputState
            {
                CtrlDown = true,
                KeysPressed = [UiKey.D]
            },
            externalFocus);

        Assert.False(dispatched);
        Assert.Equal(0, activationCount);
        Assert.True(menu.IsPopupOpen);
        Assert.True(menu.HasOpenMenu);
    }

    [Fact]
    public void ExternalTextFocusSuppressesShortcutUnlessItemExplicitlyAllowsIt()
    {
        (UiMenuBar menu, UiMenuBar.MenuItem item) = CreatePopupMenu("Primary+S");
        UiFocusManager externalFocus = new();
        UiTextField externalTextField = new();
        externalFocus.RequestFocus(externalTextField);
        int activationCount = 0;
        item.Activated = (_, _) => activationCount++;
        UiInputState input = new()
        {
            CtrlDown = true,
            KeysPressed = [UiKey.S]
        };
        menu.OpenPopup();

        Assert.False(menu.TryDispatchShortcut(input, externalFocus));
        Assert.Equal(0, activationCount);
        Assert.True(menu.IsPopupOpen);
        Assert.Same(externalTextField, externalFocus.Focused);

        item.AllowShortcutDuringTextInput = true;

        Assert.True(menu.TryDispatchShortcut(input, externalFocus));
        Assert.Equal(1, activationCount);
        Assert.False(menu.IsPopupOpen);
        Assert.Same(externalTextField, externalFocus.Focused);
    }

    private static (UiMenuBar Menu, UiMenuBar.MenuItem Item) CreatePopupMenu(string shortcut)
    {
        UiMenuBar menu = new()
        {
            DisplayMode = UiMenuDisplayMode.Popup,
            Bounds = new UiRect(20, 20, 180, 0),
            EnableShortcutDispatch = true
        };
        UiMenuBar.MenuItem item = new()
        {
            Text = "Command",
            CommandId = "test.command",
            Shortcut = shortcut
        };
        menu.Items.Add(item);
        return (menu, item);
    }
}
