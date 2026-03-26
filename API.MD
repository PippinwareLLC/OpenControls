# API Reference

This is a brief overview of the current OpenControls surface area.

## Core Types
- `UiContext`: owns the root element and focus manager, publishes host-facing hover/capture/cursor outputs after each `Update`, exposes per-frame item/container query snapshots, provides scoped shortcut-query plus focus-helper methods, and carries the active clipboard adapter for text controls.
- `UiElement`: base class for all UI elements; handles visibility, enablement, child hierarchy, hit tests, IDs, child clipping, overlay rendering, and optional text-input/pointer-capture/cursor hints.
- `UiFocusManager`: tracks focused element and handles focus transitions.
- `UiInputState`: host-provided input snapshot (mouse buttons, clicks, modifiers, scroll delta, text input, named key state, and navigation).
- `UiTextCompositionState`: host-provided composition/preedit snapshot for IME-aware text entry.
- `UiNavigationInput`: directional/navigation key flags (arrows, Home/End, Backspace/Delete, Tab, Enter/KeypadEnter, Space).
- `UiKey`, `UiModifierKeys`, `UiKeyChord`, `UiShortcutScope`: named key and shortcut types for richer keyboard input and scoped routing.
- `UiItemStatusFlags`, `UiItemStateSnapshot`: per-frame item interaction/status model (`Hovered`, `Active`, `Pressed`, `Dragging`, `Edited`, `Activated`, `Deactivated`, `Clicked`, plus bounds/clip-bounds).
- `UiContainerKind`, `UiContainerStateSnapshot`: per-frame container/window-like state model for windows, popups, modals, menu bars, tab items/bars, dock hosts, and modal hosts.
- `UiMouseCursor`: host-facing cursor request enum returned by `UiContext`.
- `UiTextInputRequest`: focused text-input metadata for IME/on-screen-keyboard friendly hosts, including overall bounds, caret bounds, candidate bounds, and composition support.
- `IUiClipboard`, `UiMemoryClipboard`, `UiDelegateClipboard`: clipboard abstraction plus default in-memory and delegate-backed implementations for copy/cut/paste aware controls.
- `UiTextEditingState`: shared single-line text editing primitive with selection, undo/redo, word movement, and session cancel/commit support.
- `UiInputHostHelpers`: shared helper methods for host-side double-click and drag-origin tracking.
- `UiClipRange`, `UiClipper`: fixed-height virtualization math helpers for large sequential item ranges.
- `IUiRenderer`: rendering abstraction; hosts implement this for their graphics API (includes clip stack, gradient, and checkerboard fills).
- `UiRect`, `UiPoint`, `UiColor`: lightweight geometry and color structs.
- `UiRenderHelpers`: helper methods for gradients, checkerboard fills, rounded rectangles, and bold text.
- `UiStateSnapshot`, `UiStateSerializer`, `IUiStatefulElement`: serialize/restore richer UI state to JSON for persistence, including built-in editor/view state plus custom-control extensions.
- `UiScrollbarVisibility`: scrollbar visibility options for scroll panels.
- `UiSelectionModel`: manages multi-selection state, scoped selections, delete-friendly mutations, and anchor persistence for list/selectable/table/tree controls.
- `UiSelectionAnchorPersistence`: controls how range anchors behave after destructive selection updates.
- `UiArrowDirection`: direction enum for shared arrow glyphs.
- `UiArrow`: helper for drawing consistent arrow indicators in custom controls.
- `UiProgressBarStyle`: progress bar style (linear or radial).
- `UiProgressBarFillDirection`: progress bar fill direction (left/right/top/bottom).
- `UiDragFlags`: drag widget behavior flags (always-clamp, no-input, no-round-to-format, wrap-around, log scaling, and slow/fast modifier control).
- `UiSliderFlags`: slider widget behavior flags (always-clamp, no-input, no-round-to-format, and wrap-around).
- `UiColorDisplayMode`, `UiColorValueDisplayMode`: shared color-widget mode enums for RGB/HSV/Hex and byte/float value display.
- `UiColorConversion`: shared RGB/HSV/hex conversion helpers used by the color widgets.
- `UiTableColumn`, `UiTableRow`, `UiTableCell`, `UiTableColumnState`, `UiTableViewState`, `UiTableCellContext`: table definition, content, runtime state, and styling objects for `UiTable`.
- `UiTreeViewItem`: retained tree data item for `UiTreeView`.
- `UiPlotPoint`, `UiPlotSeries`: data objects for `UiPlotPanel` plotting series.
- `UiWaveformRenderMode`: waveform rendering modes for `UiWaveform` (min/max columns or line plots).
- `UiGridDefinition`: row/column sizing data for `UiGrid`.

## Controls
- `UiPanel`: background and border drawing; acts as a container with optional clipping and resize grip.
- `UiLabel`: draws text at its bounds origin (supports `Bold`).
- `UiTextBlock`: multi-line text rendering with optional word wrapping and padding.
- `UiTextLink`: hyperlink-style text button with hover/pressed feedback (optional URL launch).
- `UiBullet`: draws a bullet glyph.
- `UiBulletText`: draws a bullet with adjacent text.
- `UiArrowButton`: arrow-only button for compact navigation controls.
- `UiSpacer`: empty element used to reserve layout space.
- `UiButton`: clickable button with hover/pressed feedback; exposes `Clicked` event.
- `UiInvisibleButton`: invisible hit target for custom-drawn widgets; exposes hover/pressed state and `Clicked`.
- `UiCheckbox`: boolean toggle with label; exposes `CheckedChanged`.
- `UiRadioButton`: grouped option selector (use `GroupId`); exposes `CheckedChanged`.
- `UiSlider`: draggable value control with optional step/whole-number snapping, flag-controlled wrap/clamp/direct-input behavior, and Ctrl/Cmd+click or double-click inline numeric entry.
- `UiVSlider`: vertical slider variant with the same value behavior.
- `UiSliderAngle`: angle slider that stores radians while displaying degrees.
- `UiDragFloat`, `UiDragInt`: drag-to-adjust value fields with optional inline numeric entry, wrap/clamp/precision flags, and slow/fast or logarithmic behavior where relevant.
- `UiDragFloat2/3/4`, `UiDragInt2/3/4`: multi-component drag widgets for vector values.
- `UiDragFloatRange`, `UiDragIntRange`: two-value drag controls for min/max ranges.
- `UiProgressBar`: value bar with optional overlay text (horizontal/vertical, segmented, radial).
- `UiListBox`: list selector with hover/selected states.
- `UiSelectable`: selectable row with hover/selected states.
- `UiComboBox`: string-list convenience wrapper over `UiCombo` and `UiListView`.
- `UiPicker`, `UiPickerItem`: richer editor-facing convenience picker surface over `UiCombo` for labeled/detail/icon rows.
- `UiTable`: grid/table control with column headers, selectable rows, runtime column/view state, scrolling, resize/reorder/hide, and richer header/cell content.
- `UiTreeView`: virtualized retained tree control for large fixed-height tree datasets with open/close, selection, wheel scrolling, and keyboard navigation.
- `UiPlotPanel`: plotting panel with axes, tick labels, grid lines, and zoom/pan input.
- `UiWaveform`: waveform panel for rendering sample data with optional zero line.
- `UiGrid`: grid layout container that assigns children to row/column cells.
- `UiStack`: retained row/column layout container with gap, padding, baseline/stretch alignment, and fixed/fill/percentage sizing.
- `UiWrapPanel`: retained flow layout container for wrapped rows of child content.
- `UiGroup`: logical grouped region for treating multiple children as one retained section.
- `UiCanvas`: pannable/zoomable canvas that renders child elements with an optional grid and origin.
- `UiColorEdit`: RGB/HSV/Hex color editor with per-channel sliders or hex entry, byte/float value displays, preview swatch, retained option surface, and optional picker popup integration.
- `UiColorPicker`: HSV picker with a gradient SV area, hue bar, optional alpha bar, side preview, optional embedded `UiColorEdit`, and color drag/drop support.
- `UiColorButton`: clickable color swatch with optional alpha checkerboard plus optional color drag/drop source/target behavior.
- `UiSeparator`: horizontal or vertical line separator.
- `UiSeparatorText`: separator line with centered label text.
- `UiSplitter`: draggable horizontal/vertical splitter for manual layouts.
- `UiScrollPanel`: scrollable panel with auto/always scrollbars and clipped content.
- `UiDisabledGroup`: container that disables interaction for its children and optionally draws a dimming overlay.
- `UiTextField`: editable single-line text field with selection, copy/cut/paste, undo/redo, password/read-only/filtering modes, completion/history hooks, resize callbacks, and horizontal caret reveal.
- `UiTextEditor`: multi-line text editor with line numbers, scrolling, optional syntax highlighting, shared selection/clipboard/undo/redo behavior, and editor-style navigation.
- `UiWindow`: window container with optional title bar, clipping to content bounds, resize grip, and optional scroll panel support (title text can be bold).
- `UiTabBar`, `UiTabItem`: tab strip container and tab content items for retained-mode tabbed layouts (tab text can be bold).
- `UiDockHost`: hosts `UiWindow` instances in a tabbed dock area, with optional drag reordering/detach (tab text can be bold).
- `UiDockWorkspace`: manages dock hosts, drag previews, drop targets, floating windows, and empty-host collapse.
- `UiMenuBar`: top-level menu control with dropdowns, separators, checkable items, nested submenus, command IDs, shortcut dispatch, keyboard navigation, and popup mode support.
- `UiTreeNode`: expandable tree node with indented child content (header text can be bold).
- `UiCollapsingHeader`: expandable header for grouping child content.
- `UiTooltip`: overlay tooltip panel for hover hints.
- `UiTooltipRegion`: invisible hover/focus helper that triggers a tooltip without stealing hit tests from the target control.
- `UiContextMenuRegion`: invisible helper that opens an attached popup or popup-mode menu for a target element.
- `UiPopup`: overlay popup panel with outside-click/escape close behavior.
- `UiModal`: modal popup with a backdrop overlay.
- `UiModalHost`: container that blocks input to non-modal children when a modal is open.
- `UiCombo`, `UiListView`, `UiSelectableRow`, and `UiChildRegion`: retained composition APIs for rich combo/list/popup content and embedded clipped child regions.
- `IUiImageSource` and `UiDelegateImageSource`: reusable image adapters for thumbnail/icon rows and other repeated draw surfaces, with optional debug-name and intrinsic-size metadata.

## Notes
- All bounds are absolute (`UiRect`), so layout is explicit.
- Controls are renderer-agnostic; rendering is done by the host adapter.
- `TinyBitmapFont` is a renderer-agnostic bitmap font helper used for ASCII/code-page fallback and legacy demos (Latin-1/CP437).
- `UiFont`, `UiFontBuilder`, `UiFontRegistry`, and `UiCodePointRange` are the core font APIs for custom font files, fallback chains, and merged icon ranges.
- Use `UiContext.DefaultFont` for inherited font selection and `UiElement.Font` for per-element overrides.
- Set `UiContext.Clipboard` when a host wants system clipboard integration; otherwise the default in-memory clipboard still enables copy/cut/paste within the UI session.
- The shipped first-pass runtime now uses shared atlas-backed renderer glyph caches for custom fonts and inline icon text; HarfBuzz-backed shaping and Slug-backed GPU text rendering remain optional follow-on work in `IMGUI_PARITY_ATTACK_PLAN.MD`.
- `UiInputState.Composition` and `UiTextInputRequest` are the core hooks for host-side IME/preedit integration; the SDL example host currently provides the concrete native candidate-rect reference path.
- `UiPopup.OpenAttached(...)` and `UiPopup.OpenContext(...)` are the preferred helpers for anchored and point-based popup opening in retained demos.
- `UiMenuBar.OpenAttached(...)` and `UiMenuBar.OpenContext(...)` provide the same anchored and point-based helpers for popup-mode menus.
- `UiChildRegion` is an embedded clipped viewport inside an existing parent; use it for framed sub-content, and use `UiWindow` when the content needs independent title/chrome/focus behavior.
- `UiClipper.FixedHeight(...)` is the shared fixed-height virtualization helper used by large list, table, and tree controls.
- `UiThickness` is the shared padding/margin-style value object used by the retained layout containers.
- `UiLayoutLength` is the sizing helper for `UiStack` items (`Auto`, `Fixed`, `Fill`, `Weight`, `Percentage`).
- Additional controls can be built by subclassing `UiElement`.
 - Set `UiElement.ClipChildren` to clip child rendering to `ClipBounds`.
- `UiContext.Render` performs an overlay pass; `UiMenuBar` dropdowns draw there so they render above other controls.
- `UiContext.Update` also refreshes `Hovered`, `ActiveInputLayer`, `PointerCaptureTarget`, `WantCaptureMouse`, `WantCaptureKeyboard`, `WantTextInput`, `RequestedMouseCursor`, `LastItemState`, hovered/focused item snapshots, and hovered/focused/active-layer container snapshots.

## Rounded Corner Notes
- Use `UiRenderHelpers.FillRectRounded` and `UiRenderHelpers.DrawRectRounded` for rounded rectangles in custom controls.
- Use `UiRenderHelpers.MaskRectRounded` to cover the outside corners when you need rounded clipping.
- Common controls expose a `CornerRadius` property (buttons, panels, text fields, list/combo boxes, selectables, drag widgets, progress bars, tables, windows, popups, tooltips, scroll panels, and color widgets).

## Dock Host Events
- `UiDockHost.TabDetached`: raised when a tab is dragged outside the host bounds.
- `UiDockHost.TabClosed`: raised when a tab close button is clicked and the window is removed.

## Layout Notes
- Use `UiStack` for retained row/column layout instead of manually spacing siblings when the arrangement is stable.
- Use `UiWrapPanel` for tag clouds, toolbar chips, and other wrapped inline rows.
- Use `UiGroup` when several controls should read as one logical retained region for hover/focus ancestry and shared framing.
- `UiDockWorkspace` now exposes visible draggable splitters between dock panes, and persisted workspace state includes the updated split ratios.

## Dock Host Notes
- `UiDockHost.ShowCloseButtons` toggles close buttons; `UiWindow.AllowClose` controls per-window visibility.
- Dock hosts show scroll arrows when tabs exceed the available width; use `ScrollStep` and `ScrollButtonWidth` to tune behavior.

## Dock Workspace Notes
- `UiDockWorkspace.ResetLayout`: clears dock hosts and floating windows back to a single root host.

## Window Dragging
- `UiWindow.AllowDrag`: enables title-bar dragging (use for floating windows).
- `UiWindow.AllowClose`: enables dock tab close buttons for the window when hosted in a `UiDockHost`.
- `UiWindow.IsDragging`: true while a drag is in progress.
- `UiWindow.EnsureScrollPanel`: creates a child scroll panel sized to the window content bounds.
- `UiWindow.AddContentChild`: adds to the scroll panel if present, otherwise adds directly to the window.
- `UiWindow.ContentRoot`: returns the scroll panel if present, otherwise the window itself.

## Runtime Query Notes
- Use `UiContext.GetItemState`, `HoveredItemState`, `FocusedItemState`, and `LastItemState` to inspect per-frame interaction state without ad hoc hit-testing.
- Use `UiContext.GetContainerState`, `GetContainingContainerState`, `HoveredContainerState`, `FocusedContainerState`, and `ActiveInputLayerState` for window/popup/menu/tab/dock runtime status.
- Use `UiContext.IsHovered`, `IsFocused`, and `TryGetVisibleBounds` when you want a lighter-weight query than the full snapshot structs.
- Use `UiContext.RequestFocus`, `RequestFocusById`, `RequestFocusFirst`, and `RequestFocusChild` for code-driven focus changes, including next-frame focus.

## Menu Notes
- `UiMenuBar.MenuItem`: data objects describing menu entries, separators, shortcuts, command IDs, and submenus.
- `MenuItem.CommandId` and `UiMenuBar.TryInvokeCommand(...)` let callers route the same menu item from mouse, keyboard shortcut, or programmatic command paths.
- `MenuItem.Invoked` and `UiMenuBar.ItemInvoked` report the activation source through `UiMenuItemActivationSource`.
- `UiMenuBar.MeasureTextWidth/MeasureTextHeight`: set these when you need accurate sizing for a custom renderer.
- Set `UiMenuBar.EnableShortcutDispatch = true` when the menu should parse and dispatch `MenuItem.Shortcut` text instead of rendering it as a label only.
- Set `UiMenuBar.DisplayMode` to `Popup` and call `OpenPopup`/`ClosePopup`/`TogglePopup` for context-style menus anchored to `Bounds`.
- `MenuItem.Content` can host custom controls (tables, sliders, panels); use `ContentWidth`/`ContentHeight` and `ContentPadding` to size the row, and `ContentClip`/`ContentCapturesInput` to control input behavior.
- Menu dropdowns clamp to the nearest scroll panel viewport when present, otherwise to ancestor bounds in local coordinates.

## Overlay Notes
- `UiContextMenuRegion.Target` lets a popup or popup-mode menu follow a separate control without needing a manual overlay wrapper element in the layout tree.
- `UiTooltipRegion.HoverTarget` and `FocusTarget` support delayed hover tooltips plus keyboard-focus tooltips for the same target control.
- `UiTooltipRegion` is hit-test transparent, so it can sit beside interactive controls without stealing pointer input.

## Modal Notes
- `UiModalHost.ActiveModal` returns the topmost open modal when one is blocking input.

## Drag Notes
- `UiDragFloat`/`UiDragInt`: drag horizontally to adjust values; set `Speed`, `Min`, `Max`, and `Flags` for clamping behavior.
- `UiDragFloatRange`/`UiDragIntRange`: `ValueMin`/`ValueMax` hold the active range; `EnsureOrder` prevents the values from crossing.
- `UiDragFlags.Logarithmic` enables log-scaled drags when `Min`/`Max` are both greater than zero.
- Ctrl and Shift adjust drag speed unless `UiDragFlags.NoSlowFast` is set.

## Angle Slider Notes
- `UiSliderAngle.Value` is stored in radians while the slider display uses degrees.
- Use `MinDegrees`/`MaxDegrees` and `ValueDegrees` for degree-based ranges.

## Progress Bar Notes
- `UiProgressBar.Style` selects `Linear` or `Radial` rendering.
- `UiProgressBar.FillDirection` controls fill direction (left/right/top/bottom).
- Set `SegmentCount` >= 2 to render segmented fills; `SegmentGap` controls spacing.
- `SegmentFillColors` maps colors from the fill start to end; falls back to `Fill` when unset.
- `RadialStartAngleDegrees` and `RadialClockwise` control the radial sweep; set `RadialThickness` > 0 for a ring.
- Segmented fills apply to linear bars.

## List Box Notes
- `UiListBox.Items`: assign items to display in the list.
- `UiListBox.SelectedIndex`: set or read the active selection.
- `UiListBox.SelectionModel`: assign a shared `UiSelectionModel` to enable multi-select.
- `UiListBox.SelectionScope`: isolate or share a view inside a shared `UiSelectionModel`.
- `UiListBox.SelectedIndices`: returns selected indices (single selection when no model is set).
- `UiListBox.ScrollWheelItems`: number of items to scroll per mouse wheel step.

## Combo Box Notes
- `UiComboBox.Items`: assign items to display in the dropdown list.
- `UiComboBox.SelectedIndex`: set or read the active selection.
- `UiComboBox` now rides on the richer `UiCombo`/`UiListView` composition path while preserving the string-list API.
- Dropdown rendering happens in the overlay pass so it draws above other controls.
- `UiComboBox.ScrollWheelItems`: number of items to scroll per mouse wheel step when open.
- Dropdowns clamp to the nearest scroll panel viewport when present, otherwise to ancestor bounds in local coordinates.

## Table Notes
- `UiTable.Columns`: define headers and sizing via `UiTableColumn` (fixed/stretch sizing, resize/reorder/hide flags, optional header content).
- `UiTable.Rows`: assign `UiTableRow` data (plain string cells or richer `UiTableCell` content).
- `UiTable.ColumnStates` / `GetColumnState(...)`: inspect or tweak runtime column order/visibility/width without mutating the static column definitions.
- `UiTable.ViewState`: inspect scroll offsets, viewport size, content size, and visible row range.
- `UiTable.SelectedIndex`/`SelectionModel`: row selection behavior similar to list boxes.
- `UiTable.SelectionScope`: lets a table share or isolate selection state inside a shared `UiSelectionModel`.
- `UiTable.CellBackgroundSelector` / `CellTextColorSelector`: apply per-cell styling without replacing the whole table renderer.

## Tree View Notes
- `UiTreeView.SelectionModel` / `SelectionScope`: enable shared or isolated tree selection using `UiSelectionModel`.
- `UiTreeView.ShowHierarchyLines`: draws connector lines between parent/child/sibling rows.
- `UiTreeView.ExpandAll()`, `CollapseAll()`, `SetOpen(...)`, `ToggleOpen(...)`, and `RevealItem(...)`: programmatic tree-open helpers for editor workflows and demos.
- `UiTreeView` uses the shared fixed-height `UiClipper` path for large trees, so open-state helpers and selection continue to work under virtualization.

## Plot Notes
- `UiPlotPanel.Series`: assign one or more `UiPlotSeries` (each with `UiPlotPoint` data).
- Set `XMin`/`XMax` and `YMin`/`YMax` to control the view range, or call `AutoFit()`.
- `EnablePan` (drag) and `EnableZoom` (mouse wheel) adjust the view range; `ZoomToCursor` keeps the mouse anchor fixed.
- Use `XTickCount`/`YTickCount`, `FormatXTick`/`FormatYTick`, and `TextScale` to tune ticks and labels.
- Provide `MeasureTextWidth`/`MeasureTextHeight` for accurate label sizing with custom renderers.

## Waveform Notes
- `UiWaveform.Samples`: assign a sample buffer (typically normalized -1 to 1).
- `UiWaveform.RenderMode`: choose `MinMax` for peak/valley columns or `Line` for connected samples.
- `UiWaveform.AutoScale`: derive the display range from the visible samples.
- `UiWaveform.StartIndex`/`SampleCount`: window into the sample data without copying.

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

## Tab Bar Notes
- `UiTabBar.ContentBounds` returns the tab content region below the strip.
- `ActiveTabChanged` fires when a new `UiTabItem` becomes active.
- `UiTabBar.TabClosed` fires when a close button is clicked; set `UiTabItem.AllowClose` or `UiTabBar.ShowCloseButtons` to control visibility.
- `UiTabBar.GetTabBounds` and `UiTabBar.GetTabCloseBounds` expose the current tab and close-button rectangles.
- Tab bars show left/right scroll arrows when tabs exceed the available width; use `ScrollStep` and `ScrollButtonWidth` to tune behavior.
- Inactive tab items do not update or render their children.
- Inactive tab items are skipped during focus traversal.

## Text Editor Notes
- `UiTextEditor.Text` holds the full document; `LineCount`, `CaretLine`, `CaretColumn`, `CaretIndex`, `SelectionStart`, `SelectionEnd`, and `SelectedText` expose current state.
- `SyntaxMode` toggles highlighting (`None` or `CSharp`).
- `ShowLineNumbers` and `MinLineNumberDigits` control the line gutter.
- `HorizontalScrollbar`/`VerticalScrollbar` mirror `UiScrollPanel` behavior for large files.
- `AllowTabInput` lets Tab insert spaces instead of moving focus.
- `TextChanged`, `CaretMoved`, and `SelectionChanged` expose editor-state changes.

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
- `UiInputState.ScrollDelta` uses vertical mouse wheel delta (positive values scroll up by default), and `ScrollDeltaX` carries horizontal wheel deltas when the host can provide them.
- `UiInputState` includes left/right/middle button state plus `AltDown` and `SuperDown` modifier flags.
- `UiInputState.LeftDoubleClicked` / `RightDoubleClicked` / `MiddleDoubleClicked` expose double-clicks, and `LeftDragOrigin` / `RightDragOrigin` / `MiddleDragOrigin` plus `DragThreshold` expose drag-start metadata.
- `UiInputState.ScreenMousePosition` preserves the screen-space cursor position for overlay UI like tooltips.
- `UiInputState.ShiftDown`/`CtrlDown` still drive range/toggle selection for multi-select, and `PrimaryShortcutDown` / `IsPrimaryShortcutPressed(...)` provide a simple Ctrl-or-Super shortcut helper.
- `UiInputState.KeysDown`, `KeysPressed`, and `KeysReleased` expose named key state, and `IsKeyPressed(...)` / `IsKeyChordPressed(...)` help with shortcut handling.
- Prefer platform text input events (for example MonoGame `GameWindow.TextInput`, Silk.NET `IKeyboard.KeyChar`, or SDL `SDL_TEXTINPUT`) for `UiInputState.TextInput`; use key mapping as a fallback.
- The shipped example/editor hosts also generate held-key repeat for repeatable navigation/editing keys such as arrows, backspace/delete, home/end, and page up/down.
- `UiContext` skips Tab traversal when the focused element reports `HandlesTabInput` (used by `UiTextEditor.AllowTabInput`).
- When `UiContext` moves focus via Tab, it consumes the Tab input so the newly focused element does not receive a tab insert.
- `UiNavigationInput.Activate` is true for Enter, Keypad Enter, or Space.
- Shortcut routing precedence is `Focused`, then `Hovered`, then `Global`; global shortcuts are blocked while text entry is active unless the caller opts in, and popup/modal/menu layers only allow shortcuts owned by the active layer.
- Hosts should read `UiContext.WantCaptureMouse`, `UiContext.WantCaptureKeyboard`, `UiContext.WantTextInput`, `UiContext.TextInputRequest`, and `UiContext.RequestedMouseCursor` after `Update`.
- `OpenControls.SilkNet.Examples` is the reference host for parity validation; use it as the first target when bringing up new input features.

## Serialization Notes
- Assign stable `UiElement.Id` values for controls you want to persist.
- Use `UiStateSerializer.Capture` and `UiStateSerializer.Apply` to save and restore layout and text field state.
- `UiDockWorkspace.CaptureState` / `ApplyState` provide layout persistence for docks and floating windows.
