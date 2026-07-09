using OpenControls;
using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiKeyCompatibilityTests
{
    [Fact]
    public void PunctuationKeysAppendWithoutRenumberingExistingPublicValues()
    {
        Assert.Equal(37, (int)UiKey.F1);
        Assert.Equal(63, (int)UiKey.Escape);
        Assert.Equal(67, (int)UiKey.Super);
        Assert.True((int)UiKey.Minus > (int)UiKey.Super);
        Assert.True((int)UiKey.GraveAccent > (int)UiKey.Minus);
    }

    [Fact]
    public void MenuBarParsesSymbolAndNamedPunctuationShortcuts()
    {
        UiPanel root = new() { Bounds = new UiRect(0, 0, 320, 200) };
        UiMenuBar menu = new()
        {
            Bounds = new UiRect(0, 0, 320, 24),
            EnableShortcutDispatch = true
        };
        UiMenuBar.MenuItem layers = new() { Text = "Layer" };
        int movedDown = 0;
        int cycledUp = 0;
        layers.Items.Add(new UiMenuBar.MenuItem
        {
            Text = "Move Down",
            Shortcut = "Primary+[",
            Invoked = (_, source) =>
            {
                Assert.Equal(UiMenuItemActivationSource.Shortcut, source);
                movedDown++;
            }
        });
        layers.Items.Add(new UiMenuBar.MenuItem
        {
            Text = "Cycle Up",
            Shortcut = "Shift+Equal",
            Invoked = (_, source) =>
            {
                Assert.Equal(UiMenuItemActivationSource.Shortcut, source);
                cycledUp++;
            }
        });
        menu.Items.Add(layers);
        root.AddChild(menu);
        UiContext context = new(root);

        context.Update(new UiInputState
        {
            SuperDown = true,
            KeysPressed = [UiKey.LeftBracket]
        });
        context.Update(new UiInputState
        {
            ShiftDown = true,
            KeysPressed = [UiKey.Equal]
        });

        Assert.Equal(1, movedDown);
        Assert.Equal(1, cycledUp);
    }
}
