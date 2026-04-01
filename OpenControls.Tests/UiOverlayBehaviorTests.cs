using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiOverlayBehaviorTests
{
    [Fact]
    public void MenuBar_SupportsMouseShortcutAndProgrammaticCommandActivation()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 200)
        };

        UiMenuBar menu = CreateMenuBar();
        root.AddChild(menu);

        List<UiMenuItemActivationSource> sources = new();
        UiMenuBar.MenuItem saveItem = new()
        {
            Text = "Save",
            CommandId = "test.save",
            Shortcut = "Ctrl+Alt+S",
            Invoked = (_, source) => sources.Add(source)
        };
        UiMenuBar.MenuItem fileMenu = new() { Text = "File" };
        fileMenu.Items.Add(saveItem);
        menu.Items.Add(fileMenu);

        UiContext context = new(root);
        context.Update(new UiInputState());

        Assert.True(menu.TryInvokeCommand("test.save"));

        context.Update(new UiInputState
        {
            CtrlDown = true,
            AltDown = true,
            KeysPressed = new[] { UiKey.S }
        });

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(10, 10),
            ScreenMousePosition = new UiPoint(10, 10),
            LeftClicked = true,
            LeftDown = true
        });

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(20, 34),
            ScreenMousePosition = new UiPoint(20, 34),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.Equal(
            new[]
            {
                UiMenuItemActivationSource.Programmatic,
                UiMenuItemActivationSource.Shortcut,
                UiMenuItemActivationSource.Mouse
            },
            sources);
    }

    [Fact]
    public void MenuBar_KeyboardNavigation_OpensMenuAndInvokesSelection()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 200)
        };

        UiMenuBar menu = CreateMenuBar();
        root.AddChild(menu);

        UiMenuItemActivationSource? activation = null;
        UiMenuBar.MenuItem fileMenu = new() { Text = "File" };
        fileMenu.Items.Add(new UiMenuBar.MenuItem
        {
            Text = "Open",
            CommandId = "test.open",
            Invoked = (_, source) => activation = source
        });
        menu.Items.Add(fileMenu);

        UiContext context = new(root);
        context.RequestFocus(menu);

        context.Update(new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                MoveDown = true
            }
        });

        Assert.True(menu.HasOpenMenu);

        context.Update(new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                Enter = true
            }
        });

        Assert.Equal(UiMenuItemActivationSource.Keyboard, activation);
        Assert.False(menu.HasOpenMenu);
    }

    [Fact]
    public void TooltipRegion_DelaysHoverAndSupportsKeyboardFocusPath()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 200)
        };

        UiButton button = new()
        {
            Text = "Hover",
            Bounds = new UiRect(20, 20, 120, 28)
        };
        UiTooltip tooltip = new();
        UiTooltipRegion region = new()
        {
            Tooltip = tooltip,
            Text = "Tooltip",
            HoverTarget = button,
            FocusTarget = button,
            HoverDelaySeconds = 0.2f,
            FocusDelaySeconds = 0.1f
        };

        root.AddChild(button);
        root.AddChild(region);
        root.AddChild(tooltip);

        UiContext context = new(root);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(30, 30),
            ScreenMousePosition = new UiPoint(30, 30)
        }, 0.1f);
        Assert.False(tooltip.IsOpen);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(30, 30),
            ScreenMousePosition = new UiPoint(30, 30)
        }, 0.11f);
        Assert.True(tooltip.IsOpen);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(200, 150),
            ScreenMousePosition = new UiPoint(200, 150)
        }, 0.01f);
        Assert.False(tooltip.IsOpen);

        context.RequestFocus(button);
        context.Update(new UiInputState(), 0.05f);
        Assert.False(tooltip.IsOpen);

        context.Update(new UiInputState(), 0.06f);
        Assert.True(tooltip.IsOpen);
    }

    [Fact]
    public void Popup_HitTestAndOutsideClick_RespectOpenChildPopups()
    {
        UiPopup parent = new();
        UiPopup child = new()
        {
            ClampToParent = false
        };
        parent.AddChild(child);

        parent.Open(new UiRect(20, 20, 120, 80));
        child.Open(new UiRect(150, 20, 80, 60));

        Assert.Same(child, parent.HitTest(new UiPoint(160, 30)));

        parent.Update(new UiUpdateContext(
            new UiInputState
            {
                MousePosition = new UiPoint(160, 30),
                ScreenMousePosition = new UiPoint(160, 30),
                LeftClicked = true,
                LeftDown = true
            },
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));

        Assert.True(parent.IsOpen);
        Assert.True(child.IsOpen);
    }

    [Fact]
    public void ContextMenuRegion_OpensAttachedMenuForTarget()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 200)
        };

        UiButton target = new()
        {
            Text = "Target",
            Bounds = new UiRect(24, 32, 120, 28)
        };
        UiMenuBar menu = new()
        {
            DisplayMode = UiMenuDisplayMode.Popup,
            DropdownMinWidth = 160
        };
        UiContextMenuRegion region = new()
        {
            Menu = menu,
            Target = target
        };

        root.AddChild(target);
        root.AddChild(region);
        root.AddChild(menu);

        root.Update(new UiUpdateContext(
            new UiInputState
            {
                MousePosition = new UiPoint(40, 40),
                ScreenMousePosition = new UiPoint(40, 40),
                RightClicked = true,
                RightDown = true
            },
            new UiFocusManager(),
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            new UiMemoryClipboard()));

        Assert.True(menu.IsPopupOpen);
        Assert.Equal(target.Bounds.X, menu.Bounds.X);
        Assert.Equal(target.Bounds.Bottom + 4, menu.Bounds.Y);
    }

    [Fact]
    public void OpenMenuLayouts_ParticipateInHoverAndMouseCapture()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 200)
        };

        UiMenuBar menu = CreateMenuBar();
        root.AddChild(menu);

        UiMenuBar.MenuItem fileMenu = new() { Text = "File" };
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Open" });
        menu.Items.Add(fileMenu);
        menu.Bounds = new UiRect(10, 30, 160, 0);
        menu.OpenPopup();

        UiContext context = new(root);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(20, 40),
            ScreenMousePosition = new UiPoint(20, 40)
        });

        Assert.Same(menu, context.Hovered);
        Assert.True(context.WantCaptureMouse);
    }

    [Fact]
    public void OpenMenuLayouts_HitTestBeforeLaterOverlappingSiblings()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 240)
        };

        UiMenuBar menu = CreateMenuBar();
        root.AddChild(menu);

        UiMenuBar.MenuItem fileMenu = new() { Text = "File" };
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Settings" });
        menu.Items.Add(fileMenu);

        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 24, 200, 180)
        };
        tree.RootItems.Add(new UiTreeViewItem("Root"));
        root.AddChild(tree);

        UiContext context = new(root);
        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(10, 10),
            ScreenMousePosition = new UiPoint(10, 10),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(menu.HasOpenMenu);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(20, 40),
            ScreenMousePosition = new UiPoint(20, 40)
        });

        Assert.Same(menu, context.Hovered);
        Assert.True(context.HoveredContainerState.IsMenuBar);
        Assert.False(context.HoveredContainerState.Element == tree);
    }

    [Fact]
    public void OpenMenuLayouts_BlockUnderlyingTreeSelection()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 240)
        };

        UiMenuBar menu = CreateMenuBar();
        root.AddChild(menu);

        UiMenuBar.MenuItem fileMenu = new() { Text = "File" };
        fileMenu.Items.Add(new UiMenuBar.MenuItem { Text = "Open" });
        menu.Items.Add(fileMenu);

        UiTreeView tree = new()
        {
            Bounds = new UiRect(0, 24, 200, 180)
        };
        tree.RootItems.Add(new UiTreeViewItem("Root"));
        root.AddChild(tree);

        UiContext context = new(root);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(10, 10),
            ScreenMousePosition = new UiPoint(10, 10),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(menu.HasOpenMenu);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(20, 40),
            ScreenMousePosition = new UiPoint(20, 40)
        });

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(20, 40),
            ScreenMousePosition = new UiPoint(20, 40),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.Equal(-1, tree.SelectedIndex);
    }

    [Fact]
    public void PopupMenu_OverlapFromClamp_PrefersTopmostSubmenuLayoutForHoverAndClick()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 220)
        };

        bool portraitInvoked = false;
        UiMenuBar menu = new()
        {
            DisplayMode = UiMenuDisplayMode.Popup,
            DropdownMinWidth = 160,
            ClampToParent = true
        };
        UiMenuBar.MenuItem apple = new() { Text = "Apple" };
        apple.Items.Add(new UiMenuBar.MenuItem
        {
            Text = "iPhone Portrait",
            Invoked = (_, _) => portraitInvoked = true
        });
        apple.Items.Add(new UiMenuBar.MenuItem { Text = "iPhone Landscape" });
        menu.Items.Add(apple);
        menu.Items.Add(new UiMenuBar.MenuItem { Text = "Web" });
        menu.Items.Add(new UiMenuBar.MenuItem { Text = "Desktop" });

        root.AddChild(menu);

        UiContext context = new(root);
        menu.OpenContext(new UiPoint(40, 40), width: 160);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(60, 50),
            ScreenMousePosition = new UiPoint(60, 50)
        });

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(170, 50),
            ScreenMousePosition = new UiPoint(170, 50)
        });

        Assert.True(menu.TryGetDebugHighlightedItemBounds(out UiRect highlightedBounds));
        Assert.True(highlightedBounds.X >= 160);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(170, 50),
            ScreenMousePosition = new UiPoint(170, 50),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(portraitInvoked);
    }

    private static UiMenuBar CreateMenuBar()
    {
        return new UiMenuBar
        {
            Bounds = new UiRect(0, 0, 200, 24),
            EnableShortcutDispatch = true,
            AllowShortcutsDuringTextInput = true
        };
    }
}
