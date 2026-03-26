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
- [x] Milestone 3 landed as a first-pass custom-font and icon-font pipeline.
- [x] Milestone 4 is the next recommended implementation target.

## Status Table

| Area | Bucket | Current State | Next Milestone | Notes |
| --- | --- | --- | --- | --- |
| Input platform contract | Must have | Delivered / foundation in place | Milestone 4 | Milestone 1 is complete: richer input state, drag/double-click/wheel-X data, `WantCapture*` outputs, IME-friendly text-input metadata, requested cursor routing, and updated example hosts are in place. |
| Runtime state queries and focus APIs | Must have | Delivered / foundation in place | Milestone 4 | Milestone 2 is complete: `UiContext` now exposes item/container snapshots, visible-bounds helpers, focus-by-id/next-frame helpers, popup/modal default focus, and the examples use the real query APIs. |
| Font system, atlas, and icon glyph pipeline | Must have | Delivered / first pass | Milestone 4 | Milestone 3 is landed: `UiFont`/`UiFontBuilder`/`UiFontRegistry`, per-element and inherited font selection, renderer-side glyph caches, custom font loading, fallback chains, and merged icon-font ranges are all in place. `OpenControls.SilkNet.Examples` is the reference validation host, the Widgets text demo shows custom fonts plus inline icons, and bitmap fallback is preserved for the ASCII/code-page and editor demos. HarfBuzz and Slug stay optional follow-on work. |
| Text entry foundation | Must have | Partial | Milestone 4 | Current single-line editing is simple and lacks selection/clipboard/history depth. |
| Text editor depth | High-value parity | Partial | Milestone 5 | Current editor is useful but not yet editor-grade. |
| Combo/list/popup composition | Must have | Partial | Milestone 6 | Current combo/list widgets are string-oriented, not retained-content containers. |
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
