# Quickstart

This guide shows how to host OpenControls in a game loop. OpenControls ships MonoGame, SDL2, and OpenGL renderer adapters in `OpenControls.MonoGame`, `OpenControls.SdlNet`, and `OpenControls.OpenGL`, and you can implement `IUiRenderer` for other hosts (custom engines, etc).

## 1) Create a renderer adapter
Implement `IUiRenderer` with your drawing API (sprite batch, canvas, etc).

```csharp
public sealed class MyRenderer : IUiRenderer
{
    public void FillRect(UiRect rect, UiColor color) { /* draw */ }
    public void DrawRect(UiRect rect, UiColor color, int thickness = 1) { /* draw */ }
    public void FillRectGradient(UiRect rect, UiColor topLeft, UiColor topRight, UiColor bottomLeft, UiColor bottomRight) { /* draw */ }
    public void FillRectCheckerboard(UiRect rect, int cellSize, UiColor colorA, UiColor colorB) { /* draw */ }
    public void DrawText(string text, UiPoint position, UiColor color, int scale = 1) { /* draw */ }
    public int MeasureTextWidth(string text, int scale = 1) { /* measure */ }
    public int MeasureTextHeight(int scale = 1) { /* measure */ }
    public void PushClip(UiRect rect) { /* enable clip */ }
    public void PopClip() { /* restore clip */ }
}
```

## 2) Build the UI tree
Create a root element and add controls. Bounds are explicit.

```csharp
UiPanel root = new UiPanel { Bounds = new UiRect(0, 0, 800, 600) };
UiLabel title = new UiLabel
{
    Bounds = new UiRect(20, 20, 200, 16),
    Text = "OpenControls",
    Color = UiColor.White,
    Scale = 2
};
UiButton button = new UiButton
{
    Bounds = new UiRect(20, 60, 120, 28),
    Text = "Click",
    TextScale = 2
};
button.Clicked += () => Console.WriteLine("Clicked");
root.AddChild(title);
root.AddChild(button);

UiContext ui = new UiContext(root);
```

## 2a) Dock windows as tabs
Use `UiDockHost` to host windows as tabs.

```csharp
UiDockHost dock = new UiDockHost
{
    Bounds = new UiRect(20, 120, 360, 200)
};

UiWindow assets = new UiWindow { Title = "Assets" };
UiWindow properties = new UiWindow { Title = "Properties" };
dock.AddWindow(assets);
dock.AddWindow(properties);

root.AddChild(dock);
```

Tabs can be dragged to reorder within the dock bar. Drag a tab outside the dock to detach it; handle `TabDetached` to reattach or host it elsewhere.

## 2b) Workspace docking with drop targets
Use `UiDockWorkspace` to get drag previews, drop targets, and floating windows.

```csharp
UiDockWorkspace workspace = new UiDockWorkspace
{
    Bounds = new UiRect(20, 120, 400, 240)
};

UiDockHost left = workspace.RootHost;
UiDockHost right = workspace.SplitHost(left, UiDockWorkspace.DockTarget.Right);

left.DockWindow(new UiWindow { Title = "Assets" });
right.DockWindow(new UiWindow { Title = "Inspector" });

root.AddChild(workspace);
```

## 2c) Menu bar
Use `UiMenuBar` to add top-level menus with dropdowns.

```csharp
UiMenuBar menu = new UiMenuBar
{
    Bounds = new UiRect(0, 0, 800, 24),
    TextScale = 1
};

UiMenuBar.MenuItem file = new() { Text = "File" };
file.Items.Add(new UiMenuBar.MenuItem { Text = "Open", Shortcut = "Ctrl+O" });
menu.Items.Add(file);

root.AddChild(menu);
```

## 3) Update input and render each frame
Map your host input to `UiInputState` and call `Update`/`Render`.

```csharp
UiInputState input = new UiInputState
{
    MousePosition = new UiPoint(mouseX, mouseY),
    ScreenMousePosition = new UiPoint(mouseX, mouseY),
    LeftDown = mouseDown,
    LeftClicked = mousePressedThisFrame,
    LeftReleased = mouseReleasedThisFrame,
    ShiftDown = shiftHeld,
    CtrlDown = ctrlHeld,
    ScrollDelta = mouseWheelDelta,
    TextInput = typedCharacters,
    Navigation = new UiNavigationInput
    {
        MoveLeft = leftPressed,
        MoveRight = rightPressed,
        Backspace = backspacePressed
    }
};

ui.Update(input, deltaSeconds);
ui.Render(renderer);
```

## Notes
- `UiTextField` expects `TextInput` and `Navigation` for editing.
- Additional controls include `UiCheckbox`, `UiRadioButton`, `UiSlider`, `UiDragFloat`, `UiDragInt`, `UiDragFloat2/3/4`, `UiDragInt2/3/4`, `UiDragFloatRange`, `UiDragIntRange`, `UiProgressBar`, `UiListBox`, `UiComboBox`, `UiTable`, `UiPlotPanel`, `UiGrid`, `UiCanvas`, `UiColorEdit`, `UiColorPicker`, `UiColorButton`, `UiSelectable`, `UiSeparator`, `UiSplitter`, `UiScrollPanel`, `UiTreeNode`, `UiCollapsingHeader`, `UiPopup`, `UiModal`, `UiModalHost`, and `UiTooltip`.
- Use `UiArrow.DrawTriangle` when you want arrow indicators that match built-in controls.
- Use `UiWindow.EnsureScrollPanel()` plus `AddContentChild` when window content needs scrollbars.
- `UiScrollPanel` uses panel-local child bounds and applies scroll offsets when rendering.
- Use `UiModalHost` when modal dialogs should block input to the rest of the UI.
- Set `CornerRadius` on common controls for rounded corners, or use `UiRenderHelpers.FillRectRounded`/`DrawRectRounded`/`MaskRectRounded` in custom widgets.
- Use `UiSelectionModel` with `UiListBox`/`UiSelectable` for multi-select (Ctrl/Shift ranges).
- There is no layout engine; you set bounds each frame or on resize.
- Use `UiFocusManager` (via `UiContext.Focus`) to drive focus if needed.
- Set `UiMenuBar.DisplayMode = Popup` and call `OpenPopup` to build context menus anchored to the menu `Bounds`.
- TinyBitmapFont lives in OpenControls and renderers can switch `CodePage` between Latin-1 and CP437; the Widgets demo includes an ASCII table.
- Run `dotnet run --project OpenControls.MonoGame.Examples` to launch the MonoGame examples app.
- Run `dotnet run --project OpenControls.SdlNet.Examples` to launch the SDL2-CS example (requires SDL2 native; on Windows place `SDL2.dll` on PATH or beside the executable).
- For MonoGame `SpriteBatch`, use `SpriteSortMode.Immediate` plus a `RasterizerState` with `ScissorTestEnable = true` so `PushClip`/`PopClip` take effect.
- For OpenGL, use a compatibility profile with an orthographic top-left projection, enable blending, and keep `GL_SCISSOR_TEST` available for clipping.
