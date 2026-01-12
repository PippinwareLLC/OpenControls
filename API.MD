# API Reference

This is a brief overview of the current OpenControls surface area.

## Core Types
- `UiContext`: owns the root element and focus manager; call `Update` and `Render` each frame.
- `UiElement`: base class for all UI elements; handles visibility, enablement, child hierarchy, hit tests, IDs, child clipping, and overlay rendering.
- `UiFocusManager`: tracks focused element and handles focus transitions.
- `UiInputState`: host-provided input snapshot (mouse, clicks, scroll delta, text input, navigation).
- `UiNavigationInput`: directional/navigation key flags (arrows, Home/End, Backspace/Delete, etc).
- `IUiRenderer`: rendering abstraction; hosts implement this for their graphics API (includes clip stack, gradient, and checkerboard fills).
- `UiRect`, `UiPoint`, `UiColor`: lightweight geometry and color structs.
- `UiRenderHelpers`: helper methods for gradients, checkerboard fills, and rounded rectangles.
- `UiStateSnapshot`, `UiStateSerializer`: serialize/restore UI state to JSON for persistence.
- `UiScrollbarVisibility`: scrollbar visibility options for scroll panels.
- `UiSelectionModel`: manages multi-selection state (ctrl/shift range behavior) for list/selectable controls.
- `UiArrowDirection`: direction enum for shared arrow glyphs.
- `UiArrow`: helper for drawing consistent arrow indicators in custom controls.
- `UiDragFlags`: drag widget behavior flags (clamping, log scaling, and speed modifiers).
- `UiTableColumn`, `UiTableRow`: column/row data objects for `UiTable`.
- `UiPlotPoint`, `UiPlotSeries`: data objects for `UiPlotPanel` plotting series.
- `UiGridDefinition`: row/column sizing data for `UiGrid`.

## Controls
- `UiPanel`: background and border drawing; acts as a container with optional clipping and resize grip.
- `UiLabel`: draws text at its bounds origin.
- `UiButton`: clickable button with hover/pressed feedback; exposes `Clicked` event.
- `UiCheckbox`: boolean toggle with label; exposes `CheckedChanged`.
- `UiRadioButton`: grouped option selector (use `GroupId`); exposes `CheckedChanged`.
- `UiSlider`: draggable value control with optional step/whole-number snapping.
- `UiDragFloat`, `UiDragInt`: drag-to-adjust value fields with optional clamping and speed modifiers.
- `UiDragFloat2/3/4`, `UiDragInt2/3/4`: multi-component drag widgets for vector values.
- `UiDragFloatRange`, `UiDragIntRange`: two-value drag controls for min/max ranges.
- `UiProgressBar`: value bar with optional overlay text.
- `UiListBox`: list selector with hover/selected states.
- `UiSelectable`: selectable row with hover/selected states.
- `UiComboBox`: dropdown selector with an overlay list.
- `UiTable`: grid/table control with column headers and selectable rows.
- `UiPlotPanel`: plotting panel with axes, tick labels, grid lines, and zoom/pan input.
- `UiGrid`: grid layout container that assigns children to row/column cells.
- `UiCanvas`: pannable/zoomable canvas that renders child elements with an optional grid and origin.
- `UiColorEdit`: RGB(A) editor with per-channel sliders and a preview swatch.
- `UiColorPicker`: hue/saturation/value picker with a gradient SV area, hue bar, and optional alpha bar.
- `UiColorButton`: clickable color swatch with optional alpha checkerboard.
- `UiSeparator`: horizontal or vertical line separator.
- `UiSplitter`: draggable horizontal/vertical splitter for manual layouts.
- `UiScrollPanel`: scrollable panel with auto/always scrollbars and clipped content.
- `UiTextField`: editable text field with caret, text input, navigation handling, and text clipping to its bounds.
- `UiTextEditor`: multi-line text editor with line numbers, scrolling, and optional syntax highlighting.
- `UiWindow`: window container with optional title bar, clipping to content bounds, resize grip, and optional scroll panel support.
- `UiDockHost`: hosts `UiWindow` instances in a tabbed dock area, with optional drag reordering/detach.
- `UiDockWorkspace`: manages dock hosts, drag previews, drop targets, floating windows, and empty-host collapse.
- `UiMenuBar`: top-level menu control with dropdowns, separators, checkable items, nested submenus, and popup mode support.
- `UiTreeNode`: expandable tree node with indented child content.
- `UiCollapsingHeader`: expandable header for grouping child content.
- `UiTooltip`: overlay tooltip panel for hover hints.
- `UiTooltipRegion`: invisible hover region that triggers a tooltip.
- `UiPopup`: overlay popup panel with outside-click/escape close behavior.
- `UiModal`: modal popup with a backdrop overlay.
- `UiModalHost`: container that blocks input to non-modal children when a modal is open.

## Notes
- All bounds are absolute (`UiRect`), so layout is explicit.
- Controls are renderer-agnostic; rendering is done by the host adapter.
- `TinyBitmapFont` is a renderer-agnostic bitmap font helper used by the MonoGame, SDL2-CS, and OpenGL adapters (Latin-1/CP437).
- Additional controls can be built by subclassing `UiElement`.
 - Set `UiElement.ClipChildren` to clip child rendering to `ClipBounds`.
- `UiContext.Render` performs an overlay pass; `UiMenuBar` dropdowns draw there so they render above other controls.

## Rounded Corner Notes
- Use `UiRenderHelpers.FillRectRounded` and `UiRenderHelpers.DrawRectRounded` for rounded rectangles in custom controls.
- Use `UiRenderHelpers.MaskRectRounded` to cover the outside corners when you need rounded clipping.
- Common controls expose a `CornerRadius` property (buttons, panels, text fields, list/combo boxes, selectables, drag widgets, progress bars, tables, windows, popups, tooltips, scroll panels, and color widgets).

## Dock Host Events
- `UiDockHost.TabDetached`: raised when a tab is dragged outside the host bounds.

## Dock Workspace Notes
- `UiDockWorkspace.ResetLayout`: clears dock hosts and floating windows back to a single root host.

## Window Dragging
- `UiWindow.AllowDrag`: enables title-bar dragging (use for floating windows).
- `UiWindow.IsDragging`: true while a drag is in progress.
- `UiWindow.EnsureScrollPanel`: creates a child scroll panel sized to the window content bounds.
- `UiWindow.AddContentChild`: adds to the scroll panel if present, otherwise adds directly to the window.
- `UiWindow.ContentRoot`: returns the scroll panel if present, otherwise the window itself.

## Menu Notes
- `UiMenuBar.MenuItem`: data objects describing menu entries, separators, shortcuts, and submenus.
- `UiMenuBar.MeasureTextWidth/MeasureTextHeight`: set these when you need accurate sizing for a custom renderer.
- Set `UiMenuBar.DisplayMode` to `Popup` and call `OpenPopup`/`ClosePopup`/`TogglePopup` for context-style menus anchored to `Bounds`.
- `MenuItem.Content` can host custom controls (tables, sliders, panels); use `ContentWidth`/`ContentHeight` and `ContentPadding` to size the row, and `ContentClip`/`ContentCapturesInput` to control input behavior.
- Menu dropdowns clamp to the nearest scroll panel viewport when present, otherwise to ancestor bounds in local coordinates.

## Drag Notes
- `UiDragFloat`/`UiDragInt`: drag horizontally to adjust values; set `Speed`, `Min`, `Max`, and `Flags` for clamping behavior.
- `UiDragFloatRange`/`UiDragIntRange`: `ValueMin`/`ValueMax` hold the active range; `EnsureOrder` prevents the values from crossing.
- `UiDragFlags.Logarithmic` enables log-scaled drags when `Min`/`Max` are both greater than zero.
- Ctrl and Shift adjust drag speed unless `UiDragFlags.NoSlowFast` is set.

## List Box Notes
- `UiListBox.Items`: assign items to display in the list.
- `UiListBox.SelectedIndex`: set or read the active selection.
- `UiListBox.SelectionModel`: assign a shared `UiSelectionModel` to enable multi-select.
- `UiListBox.SelectedIndices`: returns selected indices (single selection when no model is set).
- `UiListBox.ScrollWheelItems`: number of items to scroll per mouse wheel step.

## Combo Box Notes
- `UiComboBox.Items`: assign items to display in the dropdown list.
- `UiComboBox.SelectedIndex`: set or read the active selection.
- Dropdown rendering happens in the overlay pass so it draws above other controls.
- `UiComboBox.ScrollWheelItems`: number of items to scroll per mouse wheel step when open.
- Dropdowns clamp to the nearest scroll panel viewport when present, otherwise to ancestor bounds in local coordinates.

## Table Notes
- `UiTable.Columns`: define headers and sizing via `UiTableColumn` (widths or weights).
- `UiTable.Rows`: assign `UiTableRow` data (cells are string lists).
- `UiTable.SelectedIndex`/`SelectionModel`: row selection behavior similar to list boxes.
- The table does not scroll internally; use `UiScrollPanel` for overflow.

## Plot Notes
- `UiPlotPanel.Series`: assign one or more `UiPlotSeries` (each with `UiPlotPoint` data).
- Set `XMin`/`XMax` and `YMin`/`YMax` to control the view range, or call `AutoFit()`.
- `EnablePan` (drag) and `EnableZoom` (mouse wheel) adjust the view range; `ZoomToCursor` keeps the mouse anchor fixed.
- Use `XTickCount`/`YTickCount`, `FormatXTick`/`FormatYTick`, and `TextScale` to tune ticks and labels.
- Provide `MeasureTextWidth`/`MeasureTextHeight` for accurate label sizing with custom renderers.

## Grid Notes
- `UiGrid.Columns`/`UiGrid.Rows`: define fixed sizes (`Size`) or weighted sizing (`Weight`) for each axis.
- `UiGrid.AddChild(child, row, column, rowSpan, columnSpan)` assigns children to grid cells.
- `Padding`, `ColumnSpacing`, `RowSpacing`, and `CellPadding` control layout gaps.
- `ShowGridLines`/`GridLineColor`/`GridLineThickness` let you visualize cell boundaries.

## Canvas Notes
- `UiCanvas` expects child bounds in world coordinates; `PanX`/`PanY` represent the world-space origin at the viewport top-left.
- `Zoom`, `EnablePan`, and `EnableZoom` control the interaction; `ZoomToCursor` keeps the cursor anchored when zooming.
- `ShowGrid`, `GridSpacing`, and `MajorGridSpacing` control the background grid; `ShowOrigin`/`OriginX`/`OriginY` draw a crosshair.
- `Padding` shrinks the viewport inside the canvas bounds; `ClipContent` limits rendering to the viewport.

## Color Notes
- `UiColorEdit.Color`: set or read the current color (RGB/A).
- `UiColorPicker.Color`: set or read the HSV-derived color output.
- `UiColorPicker.ShowAlpha`: shows an alpha bar with checkerboard background.
- `UiColorPicker.AlphaBarHeight`: height of the alpha bar.
- `UiColorPicker.GridSize`/`HueSegments`: set to 0 for full-resolution gradients; higher values reduce steps.
- `UiColorPicker.CheckerSize`/`CheckerColorLight`/`CheckerColorDark`: configure alpha bar checkerboard styling.
- Use `UiColorEdit.ColorChanged` and `UiColorPicker.ColorChanged` to sync values between widgets.

## Color Button Notes
- `UiColorButton.Color`: set the swatch color.
- `UiColorButton.ShowAlpha`: render a checkerboard under the swatch to preview transparency.

## Selectable Notes
- `UiSelectable.Selected`: set or read the selected state.
- Use `UiSelectable.AllowToggle` to toggle selection on/off.
- `UiSelectable.SelectionModel`/`SelectionIndex`: hook selectables into a shared multi-select model.

## Scroll Panel Notes
- `UiScrollPanel` expects child bounds in panel-local coordinates; scroll offset is applied when rendering and hit testing.
- `HorizontalScrollbar`/`VerticalScrollbar` control auto or always-on scrollbars.

## Text Editor Notes
- `UiTextEditor.Text` holds the full document; `LineCount`, `CaretLine`, and `CaretColumn` expose current state.
- `SyntaxMode` toggles highlighting (`None` or `CSharp`).
- `ShowLineNumbers` and `MinLineNumberDigits` control the line gutter.
- `HorizontalScrollbar`/`VerticalScrollbar` mirror `UiScrollPanel` behavior for large files.

## Tooltip Notes
- `UiTooltipRegion` uses `UiInputState.ScreenMousePosition` to place tooltips in screen space.
- `UiTooltip` renders in the overlay pass.

## Popup Notes
- `UiPopup` and `UiModal` render in the overlay pass.
- Use `UiModalHost` when you need modal input blocking for underlying UI.

## Tree Notes
- `UiTreeNode` and `UiCollapsingHeader` render children in local coordinates relative to their content area.
- Set `HeaderHeight` and overall `Bounds` to include expanded content when open.
- Use `ContentPadding` to add vertical spacing between the header and child content.

## Input Notes
- `UiInputState.ScrollDelta` uses mouse wheel delta (positive values scroll up by default).
- `UiInputState.ScreenMousePosition` preserves the screen-space cursor position for overlay UI like tooltips.
- `UiInputState.ShiftDown`/`CtrlDown` drive range/toggle selection for multi-select.
- Prefer platform text input events (for example MonoGame `GameWindow.TextInput` or SDL `SDL_TEXTINPUT`) for `UiInputState.TextInput`; use key mapping as a fallback.

## Serialization Notes
- Assign stable `UiElement.Id` values for controls you want to persist.
- Use `UiStateSerializer.Capture` and `UiStateSerializer.Apply` to save and restore layout and text field state.
- `UiDockWorkspace.CaptureState` / `ApplyState` provide layout persistence for docks and floating windows.
