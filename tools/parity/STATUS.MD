# Parity Status Board

## Phase 0 Delivered

- [x] Dear ImGui comparison target frozen.
- [x] Scope buckets defined.
- [x] Parity status board added.
- [x] Lightweight verification hook added.
- [x] README parity workflow corrected to match the checked-in repo.

## Current Recommendation

- [x] Implement Milestone 1 next.

## Status Table

| Area | Bucket | Current State | Next Milestone | Notes |
| --- | --- | --- | --- | --- |
| Input platform contract | Must have | Minimal / blocking | Milestone 1 | Unlocks shortcuts, cursors, capture flags, and stronger text input. |
| Runtime state queries and focus APIs | Must have | Missing / ad hoc | Milestone 2 | Needed to replace simulated item and window status behavior. |
| Text entry foundation | Must have | Partial | Milestone 3 | Current single-line editing is simple and lacks selection/clipboard/history depth. |
| Text editor depth | High-value parity | Partial | Milestone 4 | Current editor is useful but not yet editor-grade. |
| Combo/list/popup composition | Must have | Partial | Milestone 5 | Current combo/list widgets are string-oriented, not retained-content containers. |
| Layout containers | Must have | Partial by manual bounds | Milestone 6 | Need retained equivalents for practical layout demos and product UI. |
| Tables 2.0 | Must have | Partial | Milestone 7 | Sorting exists, but resize/reorder/hide/scroll depth does not. |
| Virtualization / clipper | Must have | Missing | Milestone 8 | Needed for large lists, tables, and trees. |
| Selection and tree depth | High-value parity | Partial | Milestone 9 | Basic multi-select exists; advanced workflows do not. |
| Drag/slider/color depth | High-value parity | Partial | Milestone 10 | Functional today, but still shallow compared to Dear ImGui. |
| Menus, popups, tooltips polish | High-value parity | Partial | Milestone 11 | Behavior exists, but input/capture/nav depth still needs work. |
| Tooling, style editor, examples cleanup | High-value parity | Partial | Milestone 12 | Best done after the foundations are real. |
| Legacy columns and exact demo curiosities | Low-priority demo completeness | Deferred | Parking lot | Not worth front-loading. |

## Tracking Notes

- Update this file when milestone order changes.
- If the Dear ImGui target changes, update `BASELINE.MD` first and then revisit the milestone mapping here.
