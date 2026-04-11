using OpenControls.Controls;
using Xunit;

namespace OpenControls.Tests;

public sealed class UiCompositionControlsTests
{
    private sealed class RemovingElement : UiElement
    {
        public required Action OnUpdateAction { get; init; }

        public override void Update(UiUpdateContext context)
        {
            OnUpdateAction();
        }
    }

    private sealed class TrackingElement : UiElement
    {
        public int UpdateCount { get; private set; }

        public override void Update(UiUpdateContext context)
        {
            UpdateCount++;
        }
    }

    private sealed class RenderTestRenderer : IUiRenderer
    {
        public UiFont DefaultFont { get; set; } = UiFont.Default;

        public void FillRect(UiRect rect, UiColor color)
        {
        }

        public void DrawRect(UiRect rect, UiColor color, int thickness = 1)
        {
        }

        public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight)
        {
        }

        public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale = 1)
        {
        }

        public void DrawText(string text, UiPoint position, UiColor color, int scale, UiFont? font)
        {
        }

        public int MeasureTextWidth(string text, int scale = 1)
        {
            return MeasureTextWidth(text, scale, DefaultFont);
        }

        public int MeasureTextWidth(string text, int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextWidth(text, scale);
        }

        public int MeasureTextHeight(int scale = 1)
        {
            return MeasureTextHeight(scale, DefaultFont);
        }

        public int MeasureTextHeight(int scale, UiFont? font)
        {
            UiFont resolved = font ?? DefaultFont;
            return resolved.MeasureTextHeight(scale);
        }

        public void PushClip(UiRect rect)
        {
        }

        public void PopClip()
        {
        }
    }

    [Fact]
    public void PopupHelpers_ClampContextBoundsToParent()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 100, 100)
        };

        UiPopup popup = new();
        root.AddChild(popup);

        popup.OpenContext(new UiPoint(88, 92), new UiPoint(40, 40));

        Assert.Equal(60, popup.Bounds.X);
        Assert.Equal(60, popup.Bounds.Y);
        Assert.Equal(100, popup.Bounds.Right);
        Assert.Equal(100, popup.Bounds.Bottom);
    }

    [Fact]
    public void ListView_RowNavigationMovesSelectionAndFocus()
    {
        UiListView listView = CreateListView();
        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(listView, focus, clipboard, new UiInputState());
        listView.SelectedIndex = 0;
        focus.RequestFocus(listView.Items[0]);

        Update(listView, focus, clipboard, new UiInputState
        {
            Navigation = new UiNavigationInput
            {
                MoveDown = true
            }
        });

        Assert.Equal(1, listView.SelectedIndex);
        Assert.Same(listView.Items[1], focus.Focused);
    }

    [Fact]
    public void Combo_ComposedFilteringAndSelection_WorkEndToEnd()
    {
        UiCombo combo = new()
        {
            Bounds = new UiRect(0, 0, 180, 26),
            ShowFilterField = true
        };

        combo.AddItem(new UiSelectableRow { Text = "Alpha", SecondaryText = "Scene" });
        combo.AddItem(new UiSelectableRow { Text = "Beta", SecondaryText = "Asset" });
        combo.AddItem(new UiSelectableRow { Text = "Gamma", SecondaryText = "Asset" });

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(combo, focus, clipboard, new UiInputState
        {
            MousePosition = new UiPoint(8, 8),
            ScreenMousePosition = new UiPoint(8, 8),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(combo.IsOpen);
        Assert.Same(combo.FilterField, focus.Focused);

        Update(combo, focus, clipboard, new UiInputState
        {
            TextInput = new[] { 'g' }
        });

        Update(combo, focus, clipboard, new UiInputState());

        Assert.False(combo.ListView.Items[0].Visible);
        Assert.False(combo.ListView.Items[1].Visible);
        Assert.True(combo.ListView.Items[2].Visible);

        UiSelectableRow gamma = combo.ListView.Items[2];
        UiPoint rowPoint = new UiPoint(combo.ListView.Bounds.X + 8, combo.ListView.Bounds.Y + gamma.Bounds.Y + 8);

        Update(combo, focus, clipboard, new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftClicked = true,
            LeftDown = true
        });

        Update(combo, focus, clipboard, new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftReleased = true
        });

        Assert.Equal(2, combo.SelectedIndex);
        Assert.Equal("Gamma", combo.SelectedItem?.Text);
        Assert.False(combo.IsOpen);
    }

    [Fact]
    public void Picker_ReplacingItemsWhilePressed_PreservesRowInvocation()
    {
        UiPicker picker = new()
        {
            Bounds = new UiRect(0, 0, 220, 26),
            ShowFilterField = false,
            Items = new[]
            {
                new UiPickerItem { Text = "Alpha" },
                new UiPickerItem { Text = "Beta" },
                new UiPickerItem { Text = "Gamma" }
            }
        };

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(picker, focus, clipboard, new UiInputState
        {
            MousePosition = new UiPoint(8, 8),
            ScreenMousePosition = new UiPoint(8, 8),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(picker.IsOpen);

        Update(picker, focus, clipboard, new UiInputState());

        UiSelectableRow betaRow = picker.ListView.Items[1];
        UiPoint rowPoint = new UiPoint(picker.ListView.Bounds.X + 8, picker.ListView.Bounds.Y + betaRow.Bounds.Y + 8);

        Update(picker, focus, clipboard, new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftClicked = true,
            LeftDown = true
        });

        picker.Items = new[]
        {
            new UiPickerItem { Text = "Alpha" },
            new UiPickerItem { Text = "Beta Refreshed" },
            new UiPickerItem { Text = "Gamma" }
        };

        Update(picker, focus, clipboard, new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftReleased = true
        });

        Assert.Equal(1, picker.SelectedIndex);
        Assert.Equal("Beta Refreshed", picker.SelectedItem?.Text);
        Assert.False(picker.IsOpen);
    }

    [Fact]
    public void Picker_InUiContext_InvokesSelectionAndBlocksOverlappedSibling()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 800, 600)
        };

        bool underClicked = false;
        UiButton underButton = new()
        {
            Bounds = new UiRect(0, 0, 0, 0),
            Text = "Under"
        };
        underButton.Clicked += () => underClicked = true;
        root.AddChild(underButton);

        UiPicker picker = new()
        {
            Bounds = new UiRect(40, 40, 220, 26),
            ShowFilterField = false,
            ItemHeight = 40,
            MaxVisibleItems = 6,
            Items = new[]
            {
                new UiPickerItem { Text = "Zero" },
                new UiPickerItem { Text = "One" },
                new UiPickerItem { Text = "Two" },
                new UiPickerItem { Text = "Three" },
                new UiPickerItem { Text = "Four" },
                new UiPickerItem { Text = "Five" },
                new UiPickerItem { Text = "Six" }
            }
        };
        root.AddChild(picker);

        UiContext context = new(root);
        context.Update(new UiInputState());

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(48, 48),
            ScreenMousePosition = new UiPoint(48, 48),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(picker.IsOpen);
        context.Update(new UiInputState());

        UiSelectableRow targetRow = picker.ListView.Items[5];
        UiPoint rowPoint = new UiPoint(
            picker.ListView.Bounds.X + 8,
            picker.ListView.Bounds.Y + targetRow.Bounds.Y + 8);

        underButton.Bounds = new UiRect(rowPoint.X - 12, rowPoint.Y - 12, 96, 28);
        context.Update(new UiInputState());

        context.Update(new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftClicked = true,
            LeftDown = true
        });

        Assert.Same(targetRow, context.Hovered);
        Assert.False(underClicked);

        context.Update(new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftReleased = true
        });

        Assert.Equal(5, picker.SelectedIndex);
        Assert.Equal("Five", picker.SelectedItem?.Text);
        Assert.False(picker.IsOpen);
        Assert.False(underClicked);
    }

    [Fact]
    public void Popup_ClickingBackground_DoesNotClearFocusedPopupChild()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 240, 180)
        };

        UiPopup popup = new()
        {
            Bounds = new UiRect(40, 30, 140, 100)
        };

        UiButton button = new()
        {
            Bounds = new UiRect(12, 12, 64, 24),
            Text = "Focus"
        };

        popup.AddChild(button);
        root.AddChild(popup);

        UiContext context = new(root);
        popup.Open();
        context.Focus.RequestFocus(button);

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(150, 110),
            ScreenMousePosition = new UiPoint(150, 110),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.Same(button, context.Focus.Focused);
        Assert.True(popup.IsOpen);
    }

    [Fact]
    public void Combo_InOverlappingPopupRegion_DoesNotCloseOnMouseDownBeforeRowInvoke()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 260, 140)
        };

        UiCombo combo = new()
        {
            Bounds = new UiRect(20, 88, 180, 32),
            ShowFilterField = false,
            DropdownMaxHeight = 60,
            PopupPadding = 0,
            PopupSpacing = 0
        };

        combo.AddItem(new UiSelectableRow { Text = "Alpha" });
        combo.AddItem(new UiSelectableRow { Text = "Beta" });
        combo.AddItem(new UiSelectableRow { Text = "Gamma" });
        root.AddChild(combo);

        UiContext context = new(root);
        context.Update(new UiInputState());

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(28, 96),
            ScreenMousePosition = new UiPoint(28, 96),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(combo.IsOpen);

        UiSelectableRow overlappedRow = combo.ListView.Items[1];
        UiPoint rowPoint = new UiPoint(
            combo.ListView.Bounds.X + 8,
            combo.ListView.Bounds.Y + overlappedRow.Bounds.Y + 8);

        Assert.True(combo.Bounds.Contains(rowPoint));
        Assert.NotNull(combo.HitTest(rowPoint));

        context.Update(new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(combo.IsOpen);

        context.Update(new UiInputState
        {
            MousePosition = rowPoint,
            ScreenMousePosition = rowPoint,
            LeftReleased = true
        });

        Assert.Equal(1, combo.SelectedIndex);
        Assert.Equal("Beta", combo.SelectedItem?.Text);
        Assert.False(combo.IsOpen);
    }

    [Fact]
    public void Picker_OpeningClickInClampedOverlapRegion_DoesNotImmediatelyInvokeRow()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 200)
        };

        UiPicker picker = new()
        {
            Bounds = new UiRect(40, 132, 180, 32),
            ShowFilterField = true,
            ItemHeight = 28,
            MaxVisibleItems = 5,
            DropdownMaxHeight = 140,
            Items = Enumerable.Range(0, 12)
                .Select(index => new UiPickerItem { Text = $"Item {index:00}" })
                .ToArray()
        };
        root.AddChild(picker);

        UiContext context = new(root);
        context.Update(new UiInputState());
        picker.SelectedIndex = 7;

        UiPoint openPoint = new UiPoint(48, 140);
        context.Update(new UiInputState
        {
            MousePosition = openPoint,
            ScreenMousePosition = openPoint,
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(picker.IsOpen);
        Assert.True(picker.Bounds.Contains(openPoint));
        Assert.True(picker.PopupBounds.Contains(openPoint));

        context.Update(new UiInputState
        {
            MousePosition = openPoint,
            ScreenMousePosition = openPoint,
            LeftReleased = true
        });

        Assert.True(picker.IsOpen);
        Assert.Equal(7, picker.SelectedIndex);
        Assert.Equal("Item 07", picker.SelectedItem?.Text);
    }

    [Fact]
    public void ScrollPanel_Update_AllowsChildrenToMutateTreeDuringIteration()
    {
        UiScrollPanel scrollPanel = new()
        {
            Bounds = new UiRect(0, 0, 200, 120)
        };

        TrackingElement sibling = new()
        {
            Bounds = new UiRect(0, 40, 80, 20)
        };

        RemovingElement remover = new()
        {
            Bounds = new UiRect(0, 0, 80, 20),
            OnUpdateAction = () => scrollPanel.RemoveChild(sibling)
        };

        scrollPanel.AddChild(remover);
        scrollPanel.AddChild(sibling);

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(scrollPanel, focus, clipboard, new UiInputState());

        Assert.Equal(0, sibling.UpdateCount);
        Assert.DoesNotContain(sibling, scrollPanel.Children);
    }

    [Fact]
    public void WrapPanel_Update_AllowsChildrenToMutateTreeDuringIteration()
    {
        UiWrapPanel panel = new()
        {
            Bounds = new UiRect(0, 0, 200, 120)
        };

        TrackingElement sibling = new()
        {
            Bounds = new UiRect(84, 0, 80, 20)
        };

        RemovingElement remover = new()
        {
            Bounds = new UiRect(0, 0, 80, 20),
            OnUpdateAction = () => panel.RemoveChild(sibling)
        };

        panel.AddChild(remover);
        panel.AddChild(sibling);

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(panel, focus, clipboard, new UiInputState());

        Assert.Equal(0, sibling.UpdateCount);
        Assert.DoesNotContain(sibling, panel.Children);
    }

    [Fact]
    public void Picker_WheelOverPopup_ScrollsPopupWithoutScrollingAncestorPanel()
    {
        UiPanel root = new()
        {
            Bounds = new UiRect(0, 0, 320, 220)
        };

        UiScrollPanel inspectorScroll = new()
        {
            Bounds = new UiRect(0, 0, 320, 220),
            ScrollWheelStep = 24
        };
        root.AddChild(inspectorScroll);

        UiPicker picker = new()
        {
            Bounds = new UiRect(20, 20, 180, 26),
            ShowFilterField = false,
            ItemHeight = 28,
            MaxVisibleItems = 4,
            ScrollWheelItems = 1,
            DropdownMaxHeight = 120,
            Items = Enumerable.Range(0, 20)
                .Select(index => new UiPickerItem { Text = $"Item {index:00}" })
                .ToArray()
        };
        inspectorScroll.AddChild(picker);

        UiPanel filler = new()
        {
            Bounds = new UiRect(0, 600, 40, 40)
        };
        inspectorScroll.AddChild(filler);

        UiContext context = new(root);
        context.Update(new UiInputState());

        context.Update(new UiInputState
        {
            MousePosition = new UiPoint(28, 28),
            ScreenMousePosition = new UiPoint(28, 28),
            LeftClicked = true,
            LeftDown = true
        });

        Assert.True(picker.IsOpen);
        context.Update(new UiInputState());

        UiPoint popupPoint = new UiPoint(
            picker.ListView.Bounds.X + 12,
            picker.ListView.Bounds.Y + 12);

        context.Update(new UiInputState
        {
            MousePosition = popupPoint,
            ScreenMousePosition = popupPoint,
            ScrollDelta = -120
        });

        Assert.Equal(0, inspectorScroll.ScrollY);
        Assert.True(picker.ListView.ScrollPanel.ScrollY > 0);
    }

    [Fact]
    public void ComboBox_WrapsComboRowsAndPreservesStringSelection()
    {
        UiComboBox comboBox = new()
        {
            Bounds = new UiRect(0, 0, 180, 24),
            Items = new[] { "Alpha", "Beta", "Gamma" }
        };

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();

        Update(comboBox, focus, clipboard, new UiInputState());
        comboBox.SelectedIndex = 1;

        Assert.Equal(3, comboBox.ListView.Items.Count);
        Assert.Equal(1, comboBox.SelectedIndex);
        Assert.Equal("Beta", comboBox.SelectedItem?.Text);
        Assert.False(comboBox.ShowFilterField);
    }

    [Fact]
    public void MenuBar_OpenContextAndAttachedHelpers_EnablePopupMode()
    {
        UiMenuBar menu = new()
        {
            DisplayMode = UiMenuDisplayMode.Popup,
            DropdownMinWidth = 120
        };

        menu.OpenContext(new UiPoint(40, 50));
        Assert.True(menu.IsPopupOpen);
        Assert.Equal(40, menu.Bounds.X);
        Assert.Equal(50, menu.Bounds.Y);
        Assert.Equal(120, menu.Bounds.Width);

        menu.ClosePopup();
        menu.OpenAttached(new UiRect(10, 20, 80, 24));
        Assert.True(menu.IsPopupOpen);
        Assert.Equal(10, menu.Bounds.X);
        Assert.Equal(20, menu.Bounds.Y);
        Assert.Equal(80, menu.Bounds.Width);
    }

    [Fact]
    public void MenuBar_RenderReusesCachedTopItemTextMetrics()
    {
        UiMenuBar menu = new()
        {
            Bounds = new UiRect(0, 0, 240, 24)
        };

        menu.Items.Add(new UiMenuBar.MenuItem { Text = "File" });
        menu.Items.Add(new UiMenuBar.MenuItem { Text = "Edit" });

        int measureWidthCalls = 0;
        menu.MeasureTextWidth = (text, scale) =>
        {
            measureWidthCalls++;
            return UiFont.Default.MeasureTextWidth(text, scale);
        };

        UiFocusManager focus = new();
        UiMemoryClipboard clipboard = new();
        Update(menu, focus, clipboard, new UiInputState());
        Assert.True(measureWidthCalls > 0);

        measureWidthCalls = 0;
        RenderTestRenderer renderer = new();
        menu.Render(new UiRenderContext(renderer, renderer.DefaultFont));

        Assert.Equal(0, measureWidthCalls);
    }

    private static UiListView CreateListView()
    {
        UiListView listView = new()
        {
            Bounds = new UiRect(0, 0, 200, 120),
            ItemHeight = 28
        };

        listView.AddItem(new UiSelectableRow { Text = "Alpha" });
        listView.AddItem(new UiSelectableRow { Text = "Beta" });
        listView.AddItem(new UiSelectableRow { Text = "Gamma" });
        return listView;
    }

    private static void Update(UiElement element, UiFocusManager focus, IUiClipboard clipboard, UiInputState input)
    {
        element.Update(new UiUpdateContext(
            input,
            focus,
            new UiDragDropContext(),
            1f / 60f,
            UiFont.Default,
            clipboard));
    }
}
