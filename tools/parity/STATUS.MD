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
- [x] Milestone 6 is complete for the scoped first-pass combo/list/popup composition target.
- [x] Milestone 7 is the next recommended implementation target.

## Status Table

| Area | Bucket | Current State | Next Milestone | Notes |
| --- | --- | --- | --- | --- |
| Input platform contract | Must have | Delivered / foundation in place | Milestone 6 | Milestone 1 is complete: richer input state, drag/double-click/wheel-X data, `WantCapture*` outputs, IME-friendly text-input metadata, requested cursor routing, and updated example hosts are in place. |
| Runtime state queries and focus APIs | Must have | Delivered / foundation in place | Milestone 6 | Milestone 2 is complete: `UiContext` now exposes item/container snapshots, visible-bounds helpers, focus-by-id/next-frame helpers, popup/modal default focus, and the examples use the real query APIs. |
| Font system, atlas, and icon glyph pipeline | Must have | Complete / scoped first pass | Milestone 6 | Milestone 3 is complete for the shipped first-pass target: `UiFont`/`UiFontBuilder`/`UiFontRegistry`, per-element and inherited font selection, renderer-side glyph caches, custom font loading, fallback chains, and merged icon-font ranges are all in place. `OpenControls.SilkNet.Examples` is the reference validation host, the Widgets text demo shows custom fonts plus inline icons, and bitmap fallback is preserved for the ASCII/code-page and editor demos. Deferred font follow-on work stays in the roadmap notes instead of blocking milestone closure. |
| Text entry foundation | Must have | Complete / scoped first pass | Milestone 6 | Milestone 4 is complete for the shipped first-pass target: `UiTextField` supports selection, clipboard, undo/redo, commit/cancel hooks, password/read-only/filtering modes, resize/completion/history callbacks, shared `UiTextEditingState`, and a clipboard abstraction on `UiContext`. `UiNumericField` inherits the richer text-entry behavior, the Widgets text demos are real, and `OpenControls.Tests` covers the shared editing core. Deferred IME/composition depth stays in the roadmap notes instead of blocking milestone closure. |
| Text editor depth | High-value parity | Complete / scoped first pass | Milestone 6 | Milestone 5 is complete for the shipped first-pass target: `UiTextEditor` now reuses `UiTextEditingState`, supports mouse/keyboard selection, clipboard, undo/redo, line helpers, word/page/home/end navigation, caret/selection/text events, demo coverage, and regression tests. Deferred editor follow-on work stays in the roadmap notes instead of blocking milestone closure. |
| Combo/list/popup composition | Must have | Complete / scoped first pass | Milestone 7 | `UiCombo`, `UiListView`, `UiSelectableRow`, `UiChildRegion`, popup open helpers, image-source adapters, and the composed examples demo are now landed; compatibility wrappers and extra popup variants stay in the roadmap notes. |
| Layout containers | Must have | Partial by manual bounds | Milestone 7 | Need retained equivalents for practical layout demos and product UI. |
| Tables 2.0 | Must have | Partial | Milestone 8 | Sorting exists, but resize/reorder/hide/scroll depth does not. |
| Virtualization / clipper | Must have | Missing | Milestone 9 | Needed for large lists, tables, and trees. |
| Selection and tree depth | High-value parity | Partial | Milestone 10 | Basic multi-select exists; advanced workflows do not. |
| Drag/slider/color depth | High-value parity | Partial | Milestone 11 | Functional today, but still shallow compared to Dear ImGui. |
| Menus, popups, tooltips polish | High-value parity | Partial | Milestone 12 | Behavior exists, but input/capture/nav depth still needs work. |
| Tooling, style editor, examples cleanup | High-value parity | Partial | Milestone 13 | Best done after the foundations are real. |
| Legacy columns and exact demo curiosities | Low-priority demo completeness | Deferred | Parking lot | Not worth front-loading. |

## Tracking Notes

- Update this file when milestone order changes.
- Refresh `README.MD` in the same pass when milestone completion, parity caveats, or the recommended next target changes.
- If the Dear ImGui target changes, update `BASELINE.MD` first and then revisit the milestone mapping here.
- Treat `OpenControls.SilkNet.Examples` as the reference host for parity work; keep the other hosts healthy, but bias new parity validation toward Silk.NET first.
