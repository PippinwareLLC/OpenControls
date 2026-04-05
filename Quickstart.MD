# Quickstart

This guide shows how to host OpenControls in a game loop. OpenControls ships MonoGame, SDL2, Silk.NET, and OpenGL renderer adapters in `OpenControls.MonoGame`, `OpenControls.SdlNet`, `OpenControls.SilkNet`, and `OpenControls.OpenGL`, and you can implement `IUiRenderer` for other hosts (custom engines, etc).

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

## 1a) Wrap it for DPI compensation
The recommended setup is to keep your UI in logical units and wrap the renderer so text and control chrome scale together from a 96 DPI baseline.

```csharp
MyRenderer baseRenderer = new MyRenderer();
UiDpiCompensation dpi = new UiDpiCompensation();
IUiRenderer renderer = new UiScaledRenderer(baseRenderer, dpi);
```

Each frame, update the scale from your logical window size and physical framebuffer/backbuffer size, or from a known monitor DPI:

```csharp
dpi.SetScaleFromContentSize(logicalWidth, logicalHeight, physicalWidth, physicalHeight);
// or:
// dpi.CurrentDpi = monitorDpi;
```

Disable it by setting:

```csharp
dpi.Enabled = false;
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
    Bounds = new UiRect(20, 120, 360, 200),
    AutoSizeTabs = true,
    TabMaxWidth = 160,
    TabTextOverflow = UiTabTextOverflowMode.Ellipsis,
    ShowOverflowMenuButton = true
};

UiWindow assets = new UiWindow { Title = "Assets / Content Browser" };
UiWindow properties = new UiWindow { Title = "Inspector / Selection Details" };
dock.AddWindow(assets);
dock.AddWindow(properties);

root.AddChild(dock);
```

Tabs can be dragged to reorder within the dock bar. Drag a tab outside the dock to detach it; handle `TabDetached` to reattach or host it elsewhere. Long titles can auto-size, ellipsize, and fall back to the overflow menu when the strip runs out of space.
If a tab is being hosted by some external/native window system, `UiDockWorkspace.PreviewExternalDock(...)`, `CommitExternalDock(...)`, and `ClearExternalDockPreview(...)` let that host feed the window back through the same drop-target preview path before it is re-docked.

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

## 2d) Progress bars
Use `FillDirection` for vertical bars and `SegmentCount` for segmented VU-style meters.

```csharp
UiProgressBar vertical = new UiProgressBar
{
    Bounds = new UiRect(20, 160, 20, 80),
    Min = 0f,
    Max = 1f,
    Value = 0.65f,
    FillDirection = UiProgressBarFillDirection.BottomToTop,
    ShowText = false
};

UiColor[] vuColors =
{
    new UiColor(64, 180, 96),
    new UiColor(64, 180, 96),
    new UiColor(64, 180, 96),
    new UiColor(64, 180, 96),
    new UiColor(200, 180, 80),
    new UiColor(210, 90, 80)
};

UiProgressBar vu = new UiProgressBar
{
    Bounds = new UiRect(50, 160, 20, 80),
    Min = 0f,
    Max = 1f,
    Value = 0.65f,
    FillDirection = UiProgressBarFillDirection.BottomToTop,
    SegmentCount = vuColors.Length,
    SegmentGap = 2,
    SegmentFillColors = vuColors,
    ShowText = false
};

root.AddChild(vertical);
root.AddChild(vu);

UiProgressBar radial = new UiProgressBar
{
    Bounds = new UiRect(90, 148, 64, 64),
    Min = 0f,
    Max = 1f,
    Value = 0.65f,
    Style = UiProgressBarStyle.Radial,
    RadialThickness = 8,
    ShowText = false,
    CornerRadius = 32
};

root.AddChild(radial);
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
        Backspace = backspacePressed,
        Tab = tabPressed,
        Enter = enterPressed,
        KeypadEnter = keypadEnterPressed,
        Space = spacePressed
    }
};

UiInputState logicalInput = dpi.ToLogical(input); // Skip this transform if your host already reports logical window coordinates.
ui.Update(logicalInput, deltaSeconds);
ui.Render(renderer);
```

## Notes
- `UiTextField` and `UiTextEditor` expect `TextInput` and `Navigation` for editing.
- For DPI-aware hosts, pass logical mouse positions/window sizes into `UiContext.Update(...)` and `ExamplesUi.Update(...)`, not physical framebuffer pixels.
- `UiScaledRenderer` scales control rendering as well as fonts, so borders, padding-driven chrome, clip rects, checkerboards, and text stay visually consistent together.
- If a platform API expects physical text-input rectangles, convert `ui.TextInputRequest` or `context.TextInputRequest` back with `dpi.ToPhysical(...)`.
- `UiInputState.Composition` carries in-progress IME/preedit text when a host supports it, and `UiContext.TextInputRequest` now exposes overall/caret/candidate bounds plus composition support so hosts can place on-screen keyboards or candidate windows.
- Shipped example/editor hosts now generate held-key repeat for repeatable navigation/editing keys like arrows, backspace/delete, home/end, and page up/down; activation keys like enter/space stay one-shot.
- `UiSlider`, `UiVSlider`, and the drag widgets now support inline numeric entry on Ctrl/Cmd+click or double-click unless `NoInput` is set, and their first-pass flag depth now includes always-clamp, wrap-around, and no-round-to-format behavior.
- `UiTextField` now includes selection, copy/cut/paste, undo/redo, password/read-only/filtering modes, completion/history callbacks, and resize callbacks for single-line editor workflows.
- `UiTextEditor` now includes mouse/keyboard selection, copy/cut/paste, undo/redo, word/page/home/end navigation, and editor events for multiline workflows.
- Additional controls include `UiCheckbox`, `UiRadioButton`, `UiSlider`, `UiDragFloat`, `UiDragInt`, `UiDragFloat2/3/4`, `UiDragInt2/3/4`, `UiDragFloatRange`, `UiDragIntRange`, `UiProgressBar`, `UiListBox`, `UiComboBox`, `UiListView`, `UiCombo`, `UiSelectable`, `UiSelectableRow`, `UiChildRegion`, `UiStack`, `UiWrapPanel`, `UiGroup`, `UiTable`, `UiTreeView`, `UiPlotPanel`, `UiWaveform`, `UiGrid`, `UiCanvas`, `UiColorEdit`, `UiColorPicker`, `UiColorButton`, `UiSeparator`, `UiSplitter`, `UiScrollPanel`, `UiTextEditor`, `UiTreeNode`, `UiCollapsingHeader`, `UiPopup`, `UiModal`, `UiModalHost`, and `UiTooltip`.
- `UiTable` now owns its own resize/reorder/hide/scroll behavior plus runtime `ColumnStates`/`ViewState`, so inspectors and asset-browser style tables no longer need an outer `UiScrollPanel` just to be usable.
- Use `UiClipper.FixedHeight(...)` when you need shared fixed-height virtualization math in a custom large-data control; built-in `UiListBox`, uniform-height `UiTable`, and `UiTreeView` already use it.
- Use `UiArrow.DrawTriangle` when you want arrow indicators that match built-in controls.
- Use `UiWindow.EnsureScrollPanel()` plus `AddContentChild` when window content needs scrollbars.
- `UiScrollPanel` uses panel-local child bounds and applies scroll offsets when rendering.
- Use `UiModalHost` when modal dialogs should block input to the rest of the UI.
- Set `CornerRadius` on common controls for rounded corners, or use `UiRenderHelpers.FillRectRounded`/`DrawRectRounded`/`MaskRectRounded` in custom widgets.
- Use `UiSelectionModel` with `UiListBox`, `UiSelectable`, `UiTable`, and `UiTreeView` for shared or isolated multi-select; `SelectionScope`, `DeleteSelected(...)`, `RemoveRange(...)`, and `AnchorPersistence` now cover the first-pass editor-style selection workflows.
- `UiTreeView` now exposes `ExpandAll()`, `CollapseAll()`, `SetOpen(...)`, `ToggleOpen(...)`, and `RevealItem(...)` so editors can drive tree open-state from code instead of simulating clicks.
- Use `UiCombo`, `UiListView`, `UiSelectableRow`, and `UiChildRegion` when you want composed combo/list/popup content or embedded clipped child regions instead of string-only widgets; `UiComboBox` stays available as the convenience wrapper for plain string data.
- `UiColorEdit` now supports RGB / HSV / HEX display modes, byte-vs-float value surfaces, and optional picker popup integration; `UiColorPicker` now supports side preview/input toggles and color drag/drop.
- Use `UiStack` for retained row/column layout, `UiWrapPanel` for wrapped inline rows, and `UiGroup` when several controls should read and behave as one retained region.
- Use `IUiImageSource`/`UiDelegateImageSource` for repeated thumbnails and icon-like row artwork without wiring a fresh draw callback everywhere.
- Use `UiPicker` when you want a higher-level labeled/detail/icon picker surface without hand-building every combo row from `UiCombo` and `UiListView`.
- Use `UiPopup.OpenAttached(...)` and `UiPopup.OpenContext(...)` for anchored popup demos and context menus.
- Use `UiContextMenuRegion` when you want a reusable item-attached popup or popup-mode menu helper instead of hand-writing right-click hit testing.
- Use `UiChildRegion` for embedded framed subviews inside an existing window, panel, or popup; use `UiWindow` when the content needs its own chrome, title bar, or independent window-like focus surface.
- OpenControls still uses explicit retained bounds overall, but `UiStack`, `UiWrapPanel`, and `UiGroup` now cover the common row/column/wrap/group layout cases without dropping into immediate-mode cursor APIs.
- Use `UiFocusManager` (via `UiContext.Focus`) to drive focus if needed.
- Use `UiContext` runtime-query helpers like `HoveredItemState`, `FocusedContainerState`, `GetItemState`, `TryGetVisibleBounds`, and the focus helpers (`RequestFocusById`, `RequestFocusFirst`, `RequestFocusChild`) when you need real item/window status or code-driven focus.
- Set `UiContext.Clipboard` when you want text controls to use a host/system clipboard instead of the default in-memory clipboard.
- Set `UiMenuBar.DisplayMode = Popup` and call `OpenPopup`, `OpenAttached(...)`, or `OpenContext(...)` to build popup-mode menus from explicit bounds, element anchors, or cursor positions.
- Set `UiMenuBar.EnableShortcutDispatch = true` when a menu should parse `MenuItem.Shortcut` text into real shortcut activation, and use `MenuItem.CommandId` plus `TryInvokeCommand(...)` when the same menu action also needs a programmatic path.
- Use `UiTooltipRegion.HoverTarget` / `FocusTarget` for delayed hover tooltips and keyboard-focus tooltips without wrapping the target in a manual overlay element.
- `UiDockWorkspace` now shows draggable splitters between dock panes, updates split ratios live, and persists those ratios through workspace state capture/apply.
- TinyBitmapFont lives in OpenControls and renderers can switch `CodePage` between Latin-1 and CP437; the Widgets demo includes an ASCII table.
- Use `UiFontBuilder.AddFile(...)` plus `UiFontRegistry` to register custom font files and optional icon-font layers, then assign them through `UiContext.DefaultFont` or `UiElement.Font`.
- The shared examples app now demonstrates custom body fonts plus merged Font Awesome-style icons in the Widgets text section; ASCII/code-page and text-editor demos intentionally keep using `TinyBitmapFont` fallback.
- The shared examples app also includes a composed combo/list/popup demo built from `UiCombo`, `UiListView`, `UiSelectableRow`, `UiChildRegion`, and `UiDelegateImageSource`, plus element/window/empty-space context popup examples, the Milestone 12 overlay/menu polish gallery section, and retained layout container samples.
- The shared examples app also shows live text-input request diagnostics in `Inputs & Focus`, including caret bounds, candidate bounds, and preedit/composition text, and the Widget Gallery now includes a `UiPicker` example beside the richer combo/list surfaces.
- `UiStateSerializer` now restores richer editor/view state such as text selection, scroll offsets, table column/view state, combo/list selection/filter state, and custom control state through `IUiStatefulElement`.
- OpenControls now ships a first-pass HarfBuzz-backed shaping path for outline fonts, while deeper bidi/RTL/layout behavior and Slug-based GPU text rendering remain follow-on work to evaluate.
- Run `dotnet run --project OpenControls.MonoGame.Examples` to launch the MonoGame examples app.
- Run `dotnet run --project OpenControls.SdlNet.Examples` to launch the SDL2-CS example (requires SDL2 native; on Windows place `SDL2.dll` on PATH or beside the executable).
- Run `dotnet run --project OpenControls.SilkNet.Examples` to launch the Silk.NET example host.
- Run `dotnet run --project OpenControls.OpenGL.Examples` to launch the OpenGL examples app.
- For MonoGame `SpriteBatch`, use `SpriteSortMode.Immediate` plus a `RasterizerState` with `ScissorTestEnable = true` so `PushClip`/`PopClip` take effect.
- For OpenGL hosts, keep a top-left pixel-space projection (or equivalent shader transform), enable blending, and keep `GL_SCISSOR_TEST` available for clipping. The shipped Silk.NET host now targets an OpenGL 3.3 core-profile path instead of the older compatibility renderer.
