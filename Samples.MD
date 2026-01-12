# Samples

## Simple Dialog
This sample builds a dialog with a title, a text field, and OK/Cancel buttons.

```csharp
UiPanel dialog = new UiPanel
{
    Bounds = new UiRect(200, 140, 320, 160),
    Background = new UiColor(18, 20, 28),
    Border = new UiColor(90, 100, 120)
};

UiLabel title = new UiLabel
{
    Bounds = new UiRect(212, 152, 200, 16),
    Text = "Name",
    Color = UiColor.White,
    Scale = 2
};

UiTextField nameField = new UiTextField
{
    Bounds = new UiRect(212, 186, 220, 22),
    TextScale = 2,
    MaxLength = 24
};

UiButton ok = new UiButton
{
    Bounds = new UiRect(260, 250, 80, 26),
    Text = "Ok",
    TextScale = 2
};

UiButton cancel = new UiButton
{
    Bounds = new UiRect(350, 250, 80, 26),
    Text = "Cancel",
    TextScale = 2
};

ok.Clicked += () => Console.WriteLine($"Name: {nameField.Text}");
cancel.Clicked += () => Console.WriteLine("Cancelled");

dialog.AddChild(title);
dialog.AddChild(nameField);
dialog.AddChild(ok);
dialog.AddChild(cancel);
```

## Rounded Corners
Set `CornerRadius` on common controls to round backgrounds and borders.

```csharp
UiPanel card = new UiPanel
{
    Bounds = new UiRect(200, 140, 320, 160),
    Background = new UiColor(18, 20, 28),
    Border = new UiColor(90, 100, 120),
    CornerRadius = 6
};

UiButton apply = new UiButton
{
    Bounds = new UiRect(220, 210, 120, 26),
    Text = "Apply",
    TextScale = 2,
    CornerRadius = 6
};
```

If you need to mask child content to rounded corners, call `UiRenderHelpers.MaskRectRounded` after rendering children.

## Widget Sampler
Create a few common widgets like checkboxes, radios, sliders, and list boxes.

```csharp
UiCheckbox snap = new UiCheckbox
{
    Bounds = new UiRect(20, 40, 200, 24),
    Text = "Snap to Grid",
    TextScale = 2,
    Checked = true
};

UiRadioButton qualityLow = new UiRadioButton
{
    Bounds = new UiRect(20, 72, 200, 24),
    Text = "Quality: Low",
    GroupId = "quality",
    TextScale = 2
};

UiSlider volume = new UiSlider
{
    Bounds = new UiRect(20, 110, 220, 24),
    Min = 0f,
    Max = 1f,
    Value = 0.5f,
    TextScale = 2
};

UiProgressBar meter = new UiProgressBar
{
    Bounds = new UiRect(20, 142, 220, 20),
    Min = 0f,
    Max = 1f,
    Value = volume.Value,
    TextScale = 2
};

UiListBox scenes = new UiListBox
{
    Bounds = new UiRect(20, 170, 220, 120),
    Items = new[] { "Intro", "Town Square", "Workshop" },
    TextScale = 2,
    SelectedIndex = 0
};

UiComboBox sceneCombo = new UiComboBox
{
    Bounds = new UiRect(20, 300, 220, 24),
    Items = new[] { "Intro", "Town Square", "Workshop" },
    TextScale = 2,
    SelectedIndex = 0,
    Placeholder = "Select scene"
};

UiSelectable lighting = new UiSelectable
{
    Bounds = new UiRect(20, 334, 220, 24),
    Text = "Lighting",
    TextScale = 2,
    Selected = true
};

UiScrollPanel logPanel = new UiScrollPanel
{
    Bounds = new UiRect(20, 370, 220, 120),
    Background = new UiColor(18, 22, 32),
    Border = new UiColor(70, 80, 100),
    HorizontalScrollbar = UiScrollbarVisibility.Auto,
    VerticalScrollbar = UiScrollbarVisibility.Auto
};

string[] entries =
{
    "Entry 01: Boot sequence complete",
    "Entry 02: Asset scan complete",
    "Entry 03: Dock workspace ready",
    "Entry 04: Shader warmup finished"
};

int logY = 0;
foreach (string entry in entries)
{
    UiLabel logEntry = new UiLabel
    {
        Bounds = new UiRect(0, logY, 360, 16),
        Text = entry,
        Color = UiColor.White,
        Scale = 2
    };
    logPanel.AddChild(logEntry);
    logY += 20;
}
```

## Vertical and Segmented Progress
Use `FillDirection` for vertical bars and segmented fills for VU meters.

```csharp
UiProgressBar vertical = new UiProgressBar
{
    Bounds = new UiRect(20, 520, 20, 80),
    Min = 0f,
    Max = 1f,
    Value = 0.4f,
    FillDirection = UiProgressBarFillDirection.BottomToTop,
    ShowText = false
};

UiColor[] vuColors =
{
    new UiColor(64, 180, 96),
    new UiColor(64, 180, 96),
    new UiColor(64, 180, 96),
    new UiColor(200, 180, 80),
    new UiColor(210, 90, 80)
};

UiProgressBar vuMeter = new UiProgressBar
{
    Bounds = new UiRect(60, 520, 20, 80),
    Min = 0f,
    Max = 1f,
    Value = 0.6f,
    FillDirection = UiProgressBarFillDirection.BottomToTop,
    SegmentCount = vuColors.Length,
    SegmentGap = 2,
    SegmentFillColors = vuColors,
    ShowText = false
};
```

## Drag Widgets
Use drag widgets to tweak values with horizontal mouse drags.

```csharp
UiDragFloat exposure = new UiDragFloat
{
    Bounds = new UiRect(20, 40, 200, 24),
    Min = 0f,
    Max = 2f,
    Value = 0.5f,
    Speed = 0.01f,
    TextScale = 2
};

UiDragInt samples = new UiDragInt
{
    Bounds = new UiRect(20, 70, 200, 24),
    Min = 1,
    Max = 64,
    Value = 8,
    TextScale = 2
};

UiDragFloatRange window = new UiDragFloatRange
{
    Bounds = new UiRect(20, 100, 240, 24),
    Min = 0f,
    Max = 1f,
    ValueMin = 0.2f,
    ValueMax = 0.8f,
    Speed = 0.01f,
    TextScale = 2
};

UiDragFloat2 offset = new UiDragFloat2
{
    Bounds = new UiRect(20, 130, 240, 24),
    Min = 0.1f,
    Max = 10f,
    ValueX = 1f,
    ValueY = 2f,
    Speed = 0.1f,
    TextScale = 2
};
offset.Flags = UiDragFlags.Clamp | UiDragFlags.Logarithmic;
```

## Table
Use `UiTable` for column headers and row data. Place it inside a scroll panel if it needs overflow handling.

```csharp
UiTable table = new UiTable
{
    Bounds = new UiRect(20, 40, 360, 140),
    TextScale = 2,
    HeaderTextScale = 2,
    ShowHeader = true,
    ShowGrid = true
};
table.Columns.Add(new UiTableColumn("Scene", weight: 2f));
table.Columns.Add(new UiTableColumn("Status", weight: 1f));
table.Columns.Add(new UiTableColumn("Size", weight: 1f));
table.Rows = new[]
{
    new UiTableRow("Intro Scene", "Loaded", "320x180"),
    new UiTableRow("Town Square", "Unloaded", "640x360")
};
```

## Plot Panel
Use `UiPlotPanel` to render line series with axes, ticks, and zoom/pan.

```csharp
UiPlotPanel plot = new UiPlotPanel
{
    Bounds = new UiRect(20, 40, 360, 180),
    Title = "Frame Time",
    XAxisLabel = "Time (s)",
    YAxisLabel = "ms",
    TextScale = 2
};

List<UiPlotPoint> series = new();
for (int i = 0; i <= 60; i++)
{
    float x = i * 0.1f;
    float y = 16f + MathF.Sin(x) * 2f;
    series.Add(new UiPlotPoint(x, y));
}

plot.Series.Add(new UiPlotSeries
{
    LineColor = new UiColor(120, 180, 220),
    LineThickness = 2,
    Points = series
});
plot.AutoFit();
```

## Waveform
Use `UiWaveform` to render waveform samples with min/max or line modes.

```csharp
float[] samples = new float[1024];
for (int i = 0; i < samples.Length; i++)
{
    float t = i / (float)(samples.Length - 1);
    samples[i] = MathF.Sin(t * MathF.PI * 4f);
}

UiWaveform waveform = new UiWaveform
{
    Bounds = new UiRect(20, 240, 360, 140),
    Samples = samples,
    AutoScale = true,
    RenderMode = UiWaveformRenderMode.MinMax,
    ShowZeroLine = true
};
```

## Grid Layout
Use `UiGrid` to place controls in rows and columns.

```csharp
UiGrid grid = new UiGrid
{
    Bounds = new UiRect(20, 240, 260, 120),
    Padding = 4,
    ColumnSpacing = 6,
    RowSpacing = 6,
    CellPadding = 4
};
grid.SetColumnCount(2);
grid.SetRowCount(2);

UiButton primary = new UiButton { Text = "Primary", TextScale = 2 };
UiButton secondary = new UiButton { Text = "Secondary", TextScale = 2 };
UiLabel info = new UiLabel { Text = "Cell: Info", Color = UiColor.White, Scale = 2 };
UiLabel status = new UiLabel { Text = "Cell: Status", Color = UiColor.White, Scale = 2 };

grid.AddChild(primary, 0, 0);
grid.AddChild(secondary, 0, 1);
grid.AddChild(info, 1, 0);
grid.AddChild(status, 1, 1);
```

## Splitter
Use `UiSplitter` to drag a manual split between panels.

```csharp
int leftWidth = 220;
UiSplitter splitter = new UiSplitter
{
    Bounds = new UiRect(leftWidth, 20, 6, 240),
    Orientation = UiSplitterOrientation.Vertical
};

splitter.Dragged += delta =>
{
    leftWidth = Math.Clamp(leftWidth + delta, 160, 360);
    splitter.Bounds = new UiRect(leftWidth, 20, 6, 240);
};
```

## Canvas
Use `UiCanvas` for a pannable/zoomable area with a grid and world-space children.

```csharp
UiCanvas canvas = new UiCanvas
{
    Bounds = new UiRect(20, 380, 320, 180),
    PanX = -40f,
    PanY = -30f,
    Zoom = 1f,
    ShowGrid = true,
    GridSpacing = 32f,
    MajorGridSpacing = 128f,
    ShowOrigin = true
};

UiButton nodeA = new UiButton
{
    Bounds = new UiRect(-20, -10, 100, 24),
    Text = "Node A",
    TextScale = 2
};

UiButton nodeB = new UiButton
{
    Bounds = new UiRect(120, 60, 100, 24),
    Text = "Node B",
    TextScale = 2
};

canvas.AddChild(nodeA);
canvas.AddChild(nodeB);
```

## Color Edit and Picker
Use `UiColorEdit` for inline RGB edits and `UiColorPicker` for a hue/saturation/value picker.

```csharp
UiColorEdit edit = new UiColorEdit
{
    Bounds = new UiRect(20, 40, 240, 110),
    TextScale = 2,
    Color = new UiColor(120, 180, 220),
    ShowAlpha = true
};

UiColorPicker picker = new UiColorPicker
{
    Bounds = new UiRect(20, 160, 240, 140),
    Color = edit.Color,
    ShowAlpha = true
};

edit.ColorChanged += color => picker.Color = color;
picker.ColorChanged += color => edit.Color = color;
```

The picker alpha bar uses a checkerboard background to preview transparency.

## Color Buttons
Use `UiColorButton` for clickable color swatches.

```csharp
UiColorButton primary = new UiColorButton
{
    Bounds = new UiRect(20, 40, 24, 24),
    Color = new UiColor(72, 140, 220)
};

UiColorButton alpha = new UiColorButton
{
    Bounds = new UiRect(52, 40, 24, 24),
    Color = new UiColor(140, 200, 240, 120),
    ShowAlpha = true
};
```

## Multi-Select List
Use `UiSelectionModel` for Ctrl/Shift range selection in list boxes or groups of selectables.

```csharp
UiSelectionModel selection = new UiSelectionModel();
UiListBox assets = new UiListBox
{
    Bounds = new UiRect(20, 40, 220, 120),
    Items = new[] { "Textures", "Audio", "Shaders", "Scripts" },
    TextScale = 2,
    SelectionModel = selection,
    AllowDeselect = true
};

UiSelectionModel filters = new UiSelectionModel();
UiSelectable lighting = new UiSelectable
{
    Bounds = new UiRect(20, 180, 220, 24),
    Text = "Lighting",
    TextScale = 2,
    SelectionModel = filters,
    SelectionIndex = 0
};
```

## Scrollable Window
Create a window that uses a scroll panel for overflow content.

```csharp
UiWindow logWindow = new UiWindow
{
    Bounds = new UiRect(20, 40, 320, 200),
    Title = "Log",
    TitleTextScale = 2
};

UiScrollPanel windowScroll = logWindow.EnsureScrollPanel();
windowScroll.VerticalScrollbar = UiScrollbarVisibility.Auto;

int y = 0;
string[] logEntries =
{
    "Line 01: Boot sequence complete",
    "Line 02: Shader warmup finished",
    "Line 03: Dock workspace ready"
};

foreach (string line in logEntries)
{
    UiLabel label = new UiLabel
    {
        Bounds = new UiRect(0, y, 480, 16),
        Text = line,
        Color = UiColor.White,
        Scale = 2
    };
    logWindow.AddContentChild(label);
    y += 20;
}
```

## Text Editor
Use `UiTextEditor` for large, scrollable documents with line numbers and syntax highlighting.

```csharp
UiTextEditor editor = new UiTextEditor
{
    Bounds = new UiRect(20, 40, 480, 280),
    TextScale = 2,
    ShowLineNumbers = true,
    SyntaxMode = UiTextEditorSyntaxMode.CSharp,
    HorizontalScrollbar = UiScrollbarVisibility.Auto,
    VerticalScrollbar = UiScrollbarVisibility.Always
};
editor.SetText("namespace Demo;\n\npublic sealed class Program\n{\n}\n");
```

## Tooltips and Popups
Use tooltip regions to show hover hints and popups for lightweight overlays.

```csharp
UiTooltip tooltip = new UiTooltip
{
    TextScale = 2
};

UiTooltipRegion hint = new UiTooltipRegion
{
    Bounds = new UiRect(20, 20, 140, 20),
    Text = "Tooltip: hover for details",
    Tooltip = tooltip
};

UiPopup popup = new UiPopup
{
    Bounds = new UiRect(20, 50, 180, 80)
};

UiLabel popupLabel = new UiLabel
{
    Bounds = new UiRect(30, 60, 160, 16),
    Text = "Popup content",
    Color = UiColor.White,
    Scale = 2
};
popup.AddChild(popupLabel);

UiButton popupButton = new UiButton
{
    Bounds = new UiRect(20, 90, 140, 24),
    Text = "Open Popup",
    TextScale = 2
};
popupButton.Clicked += popup.Open;

root.AddChild(hint);
root.AddChild(popupButton);
root.AddChild(tooltip);
root.AddChild(popup);
```

Use `UiModalHost` when modal dialogs should block input to the rest of the UI.

## Tree and Collapsing Headers
Use tree nodes or collapsing headers to group child content.
Use `ContentPadding` to add breathing room between the header and child content.

```csharp
UiTreeNode tree = new UiTreeNode
{
    Bounds = new UiRect(20, 140, 220, 80),
    HeaderHeight = 22,
    Text = "Scene Root",
    TextScale = 2,
    ContentPadding = 4
};

UiLabel treeItem = new UiLabel
{
    Bounds = new UiRect(0, 0, 200, 16),
    Text = "Camera",
    Color = UiColor.White,
    Scale = 2
};
tree.AddChild(treeItem);

UiCollapsingHeader header = new UiCollapsingHeader
{
    Bounds = new UiRect(20, 230, 220, 70),
    HeaderHeight = 22,
    Text = "Advanced Settings",
    TextScale = 2,
    ContentPadding = 4
};

UiLabel headerItem = new UiLabel
{
    Bounds = new UiRect(0, 0, 200, 16),
    Text = "Baked Lighting",
    Color = UiColor.White,
    Scale = 2
};
header.AddChild(headerItem);
```

## Examples Project
Run the dedicated examples host to exercise all controls:

```powershell
dotnet run --project OpenControls.MonoGame.Examples
```

This launches a MonoGame DesktopGL window that renders the shared examples UI (via `OpenControls.MonoGame` and `OpenControls.Examples`).
The Widgets window organizes controls into categorized tree nodes for easier browsing (collapsed by default).

Run the SDL2-CS renderer example:

```powershell
dotnet run --project OpenControls.SdlNet.Examples
```

This launches an SDL window (requires the SDL2 native runtime; on Windows place `SDL2.dll` on PATH or beside the executable). It uses the shared SDL2 renderer adapter from `OpenControls.SdlNet`.

OpenControls also ships an OpenGL renderer adapter in `OpenControls.OpenGL` for hosts that provide a compatibility-profile OpenGL context.

Run the OpenGL examples host:

```powershell
dotnet run --project OpenControls.OpenGL.Examples
```
It mirrors the OpenControls.Examples UI to exercise the same controls.

## Custom Control
Extend `UiElement` if you need custom input or rendering.
Use `UiArrow.DrawTriangle` if you want arrow glyphs consistent with built-in controls.

```csharp
public sealed class UiSeparator : UiElement
{
    public UiColor Color { get; set; } = new UiColor(90, 100, 120);

    public override void Render(UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        context.Renderer.FillRect(Bounds, Color);
        base.Render(context);
    }
}
```

## Docked Windows
Create docked tool windows inside a tab host.

```csharp
UiDockHost dock = new UiDockHost
{
    Bounds = new UiRect(40, 40, 320, 200),
    TabWidth = 100
};

UiWindow scenes = new UiWindow { Title = "Scenes" };
UiWindow console = new UiWindow { Title = "Console" };

dock.DockWindow(scenes);
dock.DockWindow(console);
```

Handle detach events to reattach or float a window:

```csharp
dock.TabDetached += (window, dropPoint) =>
{
    Console.WriteLine($"Detached {window.Title} at {dropPoint.X},{dropPoint.Y}");
    // Optionally attach to another dock host.
};
```

## Dock Workspace
Use `UiDockWorkspace` to show drop targets and floating windows.

```csharp
UiDockWorkspace workspace = new UiDockWorkspace
{
    Bounds = new UiRect(20, 20, 360, 200)
};

UiDockHost left = workspace.RootHost;
UiDockHost right = workspace.SplitHost(left, UiDockWorkspace.DockTarget.Right);

left.DockWindow(new UiWindow { Title = "Assets" });
right.DockWindow(new UiWindow { Title = "Inspector" });
```

## Menu Bar
Create a menu bar with nested items and checkable entries.

```csharp
UiMenuBar menu = new UiMenuBar
{
    Bounds = new UiRect(0, 0, 640, 24),
    TextScale = 1
};

UiMenuBar.MenuItem file = new() { Text = "File" };
file.Items.Add(new UiMenuBar.MenuItem { Text = "Open", Shortcut = "Ctrl+O" });
file.Items.Add(UiMenuBar.MenuItem.Separator());
file.Items.Add(new UiMenuBar.MenuItem { Text = "Exit" });

UiMenuBar.MenuItem view = new() { Text = "View" };
UiMenuBar.MenuItem statusBar = new() { Text = "Status Bar", IsCheckable = true, Checked = true };
view.Items.Add(statusBar);

menu.Items.Add(file);
menu.Items.Add(view);
root.AddChild(menu);
```

## Popup Menu with Content
Use `UiMenuBar` in popup mode to show a context menu with embedded widgets.

```csharp
UiMenuBar popup = new UiMenuBar
{
    DisplayMode = UiMenuDisplayMode.Popup,
    Bounds = new UiRect(240, 120, 0, 0),
    TextScale = 2
};

UiTable quickTable = new UiTable
{
    Bounds = new UiRect(0, 0, 160, 60),
    TextScale = 2,
    HeaderTextScale = 2,
    ShowHeader = true,
    ShowGrid = true
};
quickTable.Columns.Add(new UiTableColumn("Key", weight: 1f));
quickTable.Columns.Add(new UiTableColumn("Value", weight: 1f));
quickTable.Rows = new[]
{
    new UiTableRow("Width", "320"),
    new UiTableRow("Height", "180")
};

UiMenuBar.MenuItem tableItem = new UiMenuBar.MenuItem
{
    Content = quickTable,
    ContentWidth = 160,
    ContentHeight = 60,
    ContentPadding = 6
};

popup.Items.Add(new UiMenuBar.MenuItem { Text = "Action" });
popup.Items.Add(tableItem);

popup.OpenPopup();
root.AddChild(popup);
```

## Resizable Panel
Panels and windows can show resize grips when `AllowResize` is enabled.

```csharp
UiPanel resizable = new UiPanel
{
    Bounds = new UiRect(40, 320, 240, 120),
    Background = new UiColor(18, 22, 32),
    Border = new UiColor(70, 80, 100),
    AllowResize = true,
    ShowResizeGrip = true
};

UiLabel content = new UiLabel
{
    Bounds = new UiRect(50, 330, 220, 16),
    Text = "Resizable panel content",
    Color = UiColor.White
};

resizable.AddChild(content);
root.AddChild(resizable);
```

## Serialize UI State
Persist layout and restore it later using `UiStateSerializer`.

```csharp
root.Id = "root";
window.Id = "window-standalone";
textField.Id = "text-field";

UiStateSnapshot snapshot = UiStateSerializer.Capture(root);
string json = UiStateSerializer.ToJson(snapshot);
// Save json to disk, then later:
UiStateSnapshot loaded = UiStateSerializer.FromJson(json);
UiStateSerializer.Apply(root, loaded);
```
