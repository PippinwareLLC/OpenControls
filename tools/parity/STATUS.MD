# Parity Status Board

## Phase 0 Delivered

- [x] Dear ImGui comparison target frozen.
- [x] Scope buckets defined.
- [x] Parity status board added.
- [x] Lightweight verification hook added.
- [x] README parity workflow corrected to match the checked-in repo.

## Current Recommendation

- [x] Milestone 1 landed.
- [x] Milestone 2 landed.
- [x] Milestone 3 is complete for the scoped first-pass font/icon parity target.
- [x] Milestone 4 is complete for the scoped first-pass text-entry target.
- [x] Milestone 5 is complete for the scoped first-pass text-editor target.
- [x] Milestone 6 is complete.
- [x] Milestone 7 is complete.
- [x] `OpenControls.Examples` now has a dedicated `Widget Gallery` window that closes the biggest option-level coverage gaps beside the existing Widgets/Text Editor/Docking/Clipping demos.
- [x] Milestone 8 is complete for the scoped first-pass table target.
- [x] Milestone 9 is complete for the scoped first-pass virtualization target.
- [x] Milestone 10 is complete for the scoped first-pass selection/tree target.
- [x] Milestone 11 is the next recommended implementation target.

## Status Table

| Area | Bucket | Current State | Next Milestone | Notes |
| --- | --- | --- | --- | --- |
| Input platform contract | Must have | Delivered / foundation in place | Milestone 6 | Milestone 1 is complete: richer input state, drag/double-click/wheel-X data, `WantCapture*` outputs, IME-friendly text-input metadata, requested cursor routing, and updated example hosts are in place. |
| Runtime state queries and focus APIs | Must have | Delivered / foundation in place | Milestone 6 | Milestone 2 is complete: `UiContext` now exposes item/container snapshots, visible-bounds helpers, focus-by-id/next-frame helpers, popup/modal default focus, and the examples use the real query APIs. |
| Font system, atlas, and icon glyph pipeline | Must have | Complete / scoped first pass | Milestone 6 | Milestone 3 is complete for the shipped first-pass target: `UiFont`/`UiFontBuilder`/`UiFontRegistry`, per-element and inherited font selection, renderer-side glyph caches, custom font loading, fallback chains, and merged icon-font ranges are all in place. `OpenControls.SilkNet.Examples` is the reference validation host, the Widgets text demo shows custom fonts plus inline icons, and bitmap fallback is preserved for the ASCII/code-page and editor demos. Deferred font follow-on work stays in the roadmap notes instead of blocking milestone closure. |
| Text entry foundation | Must have | Complete / scoped first pass | Milestone 6 | Milestone 4 is complete for the shipped first-pass target: `UiTextField` supports selection, clipboard, undo/redo, commit/cancel hooks, password/read-only/filtering modes, resize/completion/history callbacks, shared `UiTextEditingState`, and a clipboard abstraction on `UiContext`. `UiNumericField` inherits the richer text-entry behavior, the Widgets text demos are real, and `OpenControls.Tests` covers the shared editing core. Deferred IME/composition depth stays in the roadmap notes instead of blocking milestone closure. |
| Text editor depth | High-value parity | Complete / scoped first pass | Milestone 6 | Milestone 5 is complete for the shipped first-pass target: `UiTextEditor` now reuses `UiTextEditingState`, supports mouse/keyboard selection, clipboard, undo/redo, line helpers, word/page/home/end navigation, caret/selection/text events, demo coverage, and regression tests. Deferred editor follow-on work stays in the roadmap notes instead of blocking milestone closure. |
| Combo/list/popup composition | Must have | Complete | Milestone 8 | Milestone 6 is now fully complete: `UiCombo`, `UiListView`, `UiSelectableRow`, `UiChildRegion`, popup open helpers, image-source adapters, the `UiComboBox` convenience wrapper, menu popup helpers, and element/window/empty-space context popup demos are all in place. |
| Layout containers | Must have | Complete | Milestone 8 | Milestone 7 is now fully complete: `UiStack`, `UiWrapPanel`, `UiGroup`, `UiThickness`, and `UiLayoutLength` now cover row/column/wrap/group layout, and the layout demo sections were refit around those retained containers. |
| Dock split resizing | Must have | Complete | Milestone 8 | `UiDockWorkspace` now exposes draggable splitters with hover/active affordances, minimum pane clamping, live split-ratio updates, and nested resize coverage while continuing to persist split ratios in workspace state. |
| Widget example coverage | High-value parity | Expanded / gallery in place | Every milestone | `OpenControls.Examples` now combines the original Widgets tree with a dedicated `Widget Gallery` window plus the Text Editor, Docking, and Clipping windows. Use `tools/parity/WIDGET_DEMO_COVERAGE.MD` as the canonical matrix for every public widget, meaningful option, and documented omission. |
| Tables 2.0 | Must have | Complete / scoped first pass | Milestone 11 | Milestone 8 is complete for the shipped first-pass target: `UiTable` now has `UiTableColumnState`/`UiTableViewState`, resize/reorder/hide, header context menus, X/Y scrolling, row clipping, fixed-vs-stretch widths, row-height hooks, per-cell styling hooks, and richer header/cell content. The Widget Gallery now includes a dedicated Tables 2.0 demo for resize/reorder/hide/scroll/custom-header coverage. |
| Virtualization / clipper | Must have | Complete / scoped first pass | Milestone 11 | Milestone 9 is complete for the shipped first-pass target: shared `UiClipper` / `UiClipRange` math now drives `UiListBox`, uniform-height `UiTable` rows, and the new `UiTreeView`, and the `Clipping` window now demonstrates large list/table/tree datasets with live visible-range status. |
| Selection and tree depth | High-value parity | Complete / scoped first pass | Milestone 11 | Milestone 10 is complete for the shipped first-pass target: `UiSelectionModel` now supports scopes, delete-friendly mutation helpers, shared selection across multiple views, optional anchor persistence rules, and `UiTreeView` now adds hierarchy lines plus programmatic expand/collapse/reveal helpers. The Widget Gallery includes a dedicated Milestone 10 section for single-select, multi-select, shared list/table scopes, dual list boxes, and tree selection workflows. |
| Drag/slider/color depth | High-value parity | Partial | Milestone 11 | Functional today, but still shallow compared to Dear ImGui. |
| Menus, popups, tooltips polish | High-value parity | Partial | Milestone 12 | Behavior exists, but input/capture/nav depth still needs work. |
| Tooling, style editor, examples cleanup | High-value parity | Partial | Milestone 13 | Best done after the foundations are real. |
| Legacy columns and exact demo curiosities | Low-priority demo completeness | Deferred | Parking lot | Not worth front-loading. |

## Tracking Notes

- Update this file when milestone order changes.
- Update `WIDGET_DEMO_COVERAGE.MD` in the same pass when widget surface area or example coverage changes.
- Refresh `README.MD` in the same pass when milestone completion, parity caveats, or the recommended next target changes.
- If the Dear ImGui target changes, update `BASELINE.MD` first and then revisit the milestone mapping here.
- Treat `OpenControls.SilkNet.Examples` as the reference host for parity work; keep the other hosts healthy, but bias new parity validation toward Silk.NET first.
