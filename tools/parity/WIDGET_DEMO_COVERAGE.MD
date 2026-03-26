# Widget Demo Coverage Matrix

This file tracks a stricter requirement than milestone parity alone:

- Every public widget should have a live, discoverable example in `OpenControls.Examples`.
- Every meaningful option, mode, flag, style variant, and interaction path should either be demonstrated or explicitly documented as intentionally omitted.
- "Implemented but not shown" does not count as demo-complete.

Use this matrix as the working checklist for turning the shared example app into a truthful widget gallery instead of a best-effort sampler.

## Live Coverage Map

- `Widgets` window: the original broad sampler and milestone 1-7 reference surface.
- `Widget Gallery` window: option-level gap fill for widgets and states that were previously easy to miss.
- `Text Editor` window: multiline editing deep dive and editor-focused regression surface.
- `Docking` layout: `UiDockHost`, `UiDockWorkspace`, floating windows, drag/drop docking, and split resizing.
- `Clipping` window: explicit `UiPanel` clipping and resize behavior.

## Demo-Complete Rule

A widget is only considered demo-complete when all applicable items are true:

- [ ] The widget appears in the shared example app with a clear label/section.
- [ ] Default behavior is shown.
- [ ] Non-default visual options are shown where relevant.
- [ ] Keyboard and mouse interaction paths are shown where relevant.
- [ ] Disabled/read-only/empty/error states are shown where relevant.
- [ ] Runtime status or result labels make the behavior visible instead of forcing the user to infer it.
- [ ] If an option is not worth surfacing, the omission is documented here with a short reason.

## Current Top-Level Status

- [ ] The shared example app exhaustively demonstrates every option of every shipped widget.
- [ ] The current gaps are tracked here instead of being inferred from milestone notes.
- [ ] New widgets/options are added to this file in the same change that introduces them.

## Core Widgets

- [ ] `UiPanel`: background, border, corner radius, clipping, resize grip, min/max resize behavior.
- [ ] `UiGroup`: framing, padding, grouped hover ancestry, grouped retained content.
- [ ] `UiLabel`: color, scale, bold, font inheritance/override.
- [ ] `UiTextBlock`: wrapping, line spacing, padding, clipping/eliding behavior where relevant.
- [ ] `UiTextLink`: hover/visited styling expectations, URL open behavior, disabled/non-open mode if supported.
- [ ] `UiBullet`: size/color/placement basics.
- [ ] `UiBulletText`: bullet + text combinations, scale/color variants.
- [ ] `UiButton`: normal, hover, active, disabled, text scale, repeat-style usage where relevant.
- [ ] `UiArrowButton`: direction variants, hover/active states, sizing.
- [ ] `UiSeparator`: horizontal and vertical variants, thickness/color.
- [ ] `UiSeparatorText`: label text, alignment/spacing behavior.
- [ ] `UiSpacer`: fixed-size spacing usage in retained layout containers.
- [ ] `UiDisabledGroup`: disabled visuals, input blocking, mixed content inside the group.

## Boolean / Choice Inputs

- [ ] `UiCheckbox`: checked/unchecked/disabled, text scale, keyboard activation.
- [ ] `UiRadioButton`: grouped selection, disabled state, keyboard activation.

## Text Entry

- [ ] `UiTextField`: selection, caret movement, clipboard, undo/redo, placeholder, password mode, read-only mode, filtering, submit/cancel, resize callback, completion, history, horizontal reveal, disabled state.
- [ ] `UiTextEditor`: selection, multiline editing, clipboard, undo/redo, line numbers, syntax mode variants, word/page/home/end navigation, scrolling, disabled/read-only behavior if supported.
- [ ] `UiInputFloat`: numeric editing, precision/formatting behavior, commit/cancel, disabled state.
- [ ] `UiInputInt`: numeric editing, clamping/whole-number behavior, disabled state.
- [ ] `UiInputDouble`: numeric editing, precision differences from float.
- [ ] `UiInputFloat2`: multi-component editing.
- [ ] `UiInputFloat3`: multi-component editing.
- [ ] `UiInputFloat4`: multi-component editing.
- [ ] `UiInputInt2`: multi-component editing.
- [ ] `UiInputInt3`: multi-component editing.
- [ ] `UiInputInt4`: multi-component editing.
- [ ] `UiNumericField`: numeric text field behavior layered on top of text entry features.
- [ ] `UiNumericVector`: retained vector editing convenience surface and mixed component states.

## Sliders / Drags

- [ ] `UiSlider`: min/max, whole numbers, step size, disabled state, keyboard or drag interaction behavior.
- [ ] `UiSliderFloat2`: multi-component slider behavior.
- [ ] `UiSliderFloat3`: multi-component slider behavior.
- [ ] `UiSliderFloat4`: multi-component slider behavior.
- [ ] `UiSliderInt2`: multi-component slider behavior.
- [ ] `UiSliderInt3`: multi-component slider behavior.
- [ ] `UiSliderInt4`: multi-component slider behavior.
- [ ] `UiSliderVector`: vector slider convenience surface.
- [ ] `UiVSlider`: vertical orientation, value display/status, disabled state.
- [ ] `UiSliderAngle`: degree display, radian storage, min/max degree variants.
- [ ] `UiDragFloat`: speed, min/max clamp, log mode, slow/fast modifier behavior.
- [ ] `UiDragInt`: speed, min/max clamp, log mode where relevant.
- [ ] `UiDragFloat2`: multi-component drag behavior.
- [ ] `UiDragFloat3`: multi-component drag behavior.
- [ ] `UiDragFloat4`: multi-component drag behavior.
- [ ] `UiDragInt2`: multi-component drag behavior.
- [ ] `UiDragInt3`: multi-component drag behavior.
- [ ] `UiDragInt4`: multi-component drag behavior.
- [ ] `UiDragFloatRange`: min/max editing, ensure-order behavior.
- [ ] `UiDragIntRange`: min/max editing, ensure-order behavior.

## Lists / Selection / Composition

- [ ] `UiListBox`: single-select, multi-select via `UiSelectionModel`, scroll-wheel behavior, empty state.
- [ ] `UiSelectable`: hover, selected, disabled, keyboard activation.
- [ ] `UiSelectableRow`: icons/thumbnails, secondary text, selected/hover visuals, invoked state.
- [ ] `UiComboBox`: basic string-list path, scroll-wheel behavior, empty state, compatibility wrapper path.
- [ ] `UiCombo`: composed content, filtering, selection, close-on-selection variants, popup clamping.
- [ ] `UiListView`: arbitrary child rows, scrolling, selection model variants, empty state.
- [ ] `UiChildRegion`: framed embedded content, clipping, scrollbars, nested retained content.

## Tables / Data Display

- [ ] `UiTable`: sortable headers, angled headers, selection, empty state, current scrolling limitations called out honestly.
- [ ] `UiProgressBar`: horizontal, vertical, radial, segmented, overlay text, fill direction, color gradients.
- [ ] `UiPlotPanel`: multiple series, zoom/pan, autofit, tick formatting, labels.
- [ ] `UiWaveform`: min/max mode, line mode, auto-scale, start/count windowing.
- [ ] `UiImage`: placeholder vs real image-source path, sizing, empty/missing image behavior.
- [ ] `UiImageButton`: click behavior, image-source path, placeholder behavior.
- [ ] `UiColorEdit`: RGB vs RGBA editing, alpha behavior, preview.
- [ ] `UiColorPicker`: hue/saturation/value editing, alpha bar, checkerboard background.
- [ ] `UiColorButton`: swatch display, alpha checkerboard, click behavior.

## Layout / Containers

- [ ] `UiStack`: horizontal row, vertical column, padding, gap, baseline alignment, stretch alignment, fixed/fill/weight/percentage sizing, mixed child sizing.
- [ ] `UiWrapPanel`: wrapping behavior, spacing, clipped/small-width behavior.
- [ ] `UiGrid`: fixed and weighted rows/columns, row/column spans, grid lines, spacing/padding.
- [ ] `UiSplitter`: vertical and horizontal manual splitters, hover/active visuals, clamp behavior.
- [ ] `UiScrollPanel`: vertical scrolling, horizontal scrolling, combined scrollbars, content-size behavior, clipped long content.
- [ ] `UiCanvas`: pan, zoom, grid, origin helpers, child interaction.
- [ ] `UiWindow`: title bar, drag, resize grip, optional scroll panel, close button behavior where applicable.
- [ ] `UiDockHost`: tab strip behavior, close buttons, overflow scrolling, reorder/detach behavior.
- [ ] `UiDockWorkspace`: drag/drop docking, floating windows, split resizing, minimum pane clamps, persisted split ratios, nested split behavior.

## Tabs / Trees / Overlays

- [ ] `UiTabBar`: tab switching, active/hover states, overflow, leading/trailing buttons.
- [ ] `UiTabItem`: active/inactive content behavior, close behavior where applicable.
- [ ] `UiTabItemButton`: leading/trailing placement, auto size, click status.
- [ ] `UiTreeNode`: open/close, nested content, keyboard activation.
- [ ] `UiCollapsingHeader`: open/close, nested content, keyboard activation.
- [ ] `UiTooltip`: tooltip content, hover trigger timing/placement if configurable.
- [ ] `UiTooltipRegion`: invisible region hit target, tooltip association.
- [ ] `UiPopup`: attached open, context open, close-on-outside-click, close-on-escape, clamping behavior.
- [ ] `UiModal`: backdrop, modal blocking, close behavior, focus defaults.
- [ ] `UiModalHost`: active modal behavior and blocked background interaction.
- [ ] `UiMenuBar`: bar mode, popup mode, nested menus, separators, checkable items, embedded content, shortcut labels, attached/context opening helpers.

## Drag And Drop

- [ ] `UiDragDropSource`: payload begin/drag state, visuals, multiple source examples.
- [ ] `UiDragDropTarget`: accept/reject behavior, payload-type distinctions, hover state.

## Fonts / Text Rendering

- [ ] Default bitmap fallback behavior is shown honestly.
- [ ] Custom font loading is shown with success/fallback status.
- [ ] Merged icon font usage is shown inline and in controls.
- [ ] Font scale differences are shown across multiple widgets, not only labels.
- [ ] Any unsupported shaping/RTL/HarfBuzz depth is explicitly called out in the demo/docs.

## Runtime / Host Integration Coverage

- [ ] `WantCaptureMouse`, `WantCaptureKeyboard`, and `WantTextInput` are visibly demonstrated.
- [ ] Requested mouse cursor changes are visibly demonstrated.
- [ ] Shortcut routing examples show focused/hovered/global scope behavior where supported.
- [ ] Clipboard integration path is demonstrated with the host-supplied clipboard in at least one host.
- [ ] Text input request / IME request state is visible in the examples UI.

## Omission Notes

- [ ] Record intentional omissions here as the matrix is refined.
