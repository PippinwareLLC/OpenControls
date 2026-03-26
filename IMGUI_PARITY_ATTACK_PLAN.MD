# OpenControls ImGui Parity Attack Plan

This document turns the current OpenControls vs Dear ImGui audit into an execution plan.

The goal is not to clone Dear ImGui's immediate-mode API one-to-one. The goal is to close the high-value capability gaps while preserving OpenControls' retained-mode, explicit-bounds design.

## Operating Principles

- [ ] Preserve retained-mode architecture. Prefer OpenControls-native retained equivalents over direct API imitation.
- [ ] Land infrastructure before surface widgets when the same foundation unlocks multiple demos.
- [ ] Treat "demo parity" and "product parity" separately.
- [ ] Ship in slices that leave the examples app better after every milestone.
- [ ] Add docs and examples in the same milestone as the feature, not later.
- [ ] Keep renderer adapters thin; put behavior in `OpenControls` where possible.
- [ ] Avoid introducing host-specific logic into core controls unless the feature truly belongs there.

## Success Criteria

- [ ] The documented parity deviations in [README.MD](/Users/thomasmayer/Dev/OpenControls/README.MD#L51) are either removed or intentionally reclassified as "won't match by design".
- [ ] Input and host integration are rich enough to support true shortcuts, cursor requests, capture flags, and better focus routing.
- [ ] Text entry is editor-grade for common tooling workflows.
- [ ] Tables, lists, trees, and selection scale to large datasets.
- [ ] OpenControls examples demonstrate credible coverage for the major Dear ImGui demo sections without hand-wavy simulation.
- [ ] Every milestone has exit criteria, docs, and examples.

## Cross-Cutting Definition Of Done

Every milestone below is only complete when all of these are checked:

- [ ] Public API reviewed for naming consistency with existing `Ui*` types.
- [ ] Shared examples updated.
- [ ] API docs updated in `API.MD`.
- [ ] Sample usage added to `Samples.MD` or `Quickstart.MD` where appropriate.
- [ ] At least one verification path exists for the new behavior.
- [ ] README parity caveats are updated if the milestone changes them.

## Phase 0: Baseline, Scope Freeze, And Tracking

### Objectives

- [x] Freeze the comparison target so "parity" has a stable meaning.
- [x] Create a backlog format that supports multi-week execution without losing the rationale.
- [x] Add cheap verification hooks before deeper refactors begin.

Implemented in:

- `tools/parity/BASELINE.MD`
- `tools/parity/STATUS.MD`
- `tools/parity/README.MD`
- `scripts/verify-phase0.sh`

### Checklist

- [x] Record the Dear ImGui snapshot/version this roadmap targets.
- [x] Split features into three buckets:
  - [x] "Must have for tool-building"
  - [x] "High-value parity"
  - [x] "Low-priority demo completeness"
- [ ] Turn this roadmap into issues or tracked work items if you want repo-native tracking.
- [x] Add a simple parity status table to this document or a separate tracking file.
- [x] Decide where automated behavioral coverage should live:
  - [x] headless/unit-style tests
  - [x] examples-driven smoke checks
  - [x] renderer-agnostic interaction tests
- [x] Confirm what is intentionally out of scope for parity:
  - [x] immediate-mode layout idioms that conflict with retained mode
  - [x] legacy ImGui APIs with poor long-term value
  - [x] niche styling flags that add complexity without product value

### Exit Criteria

- [x] The team can point at one target version and one prioritized backlog.
- [x] The next milestone can start without reopening scope debates.

## Milestone 1: Input Platform Contract

### Why First

Current documented parity gaps around shortcuts, mouse cursors, WantCapture behavior, and parts of advanced text input all point back to the same bottleneck: the input model is too small.

### Primary Targets

- [ ] Expand `UiInputState` beyond the current minimal left-button + Ctrl/Shift model.
- [ ] Add host-facing capture/cursor/text-input outputs on `UiContext`.
- [ ] Introduce a scalable key model instead of one-off booleans for every future key.

### API Work

- [ ] Extend `UiInputState` with richer pointer data:
  - [ ] right mouse button
  - [ ] middle mouse button
  - [ ] double click information
  - [ ] drag thresholds / drag origin data if needed
  - [ ] wheel X in addition to wheel Y if useful
- [ ] Extend modifier support:
  - [ ] Alt
  - [ ] Super / Command / Windows key
  - [ ] platform-specific shortcut modifier abstraction
- [ ] Introduce richer keyboard state:
  - [ ] named key enum
  - [ ] per-key down/pressed/released queries
  - [ ] key chord representation
  - [ ] shortcut routing helper
- [ ] Add host output properties somewhere stable, likely on `UiContext`:
  - [ ] `WantCaptureMouse`
  - [ ] `WantCaptureKeyboard`
  - [ ] `WantTextInput`
  - [ ] requested mouse cursor shape
  - [ ] optional IME/text-input request metadata

### Core Behavior Work

- [ ] Ensure focus, hover, active drag, popup, and modal state all contribute to capture outputs.
- [ ] Ensure clicks in empty space still clear focus correctly while capture flags remain accurate.
- [ ] Define precedence rules for shortcuts:
  - [ ] focused widget shortcuts
  - [ ] hovered widget shortcuts
  - [ ] global shortcuts
  - [ ] popup/modal override behavior
- [ ] Add a single place where host adapters read "what cursor should I show right now?"

### Host Integration Work

- [ ] Update MonoGame host to populate richer input state.
- [ ] Update SDL host to populate richer input state.
- [ ] Update Silk.NET host to populate richer input state.
- [ ] Update OpenGL example host input plumbing as needed.
- [ ] Normalize platform shortcut semantics so macOS-style behavior is possible later if desired.

### Demo/Docs Work

- [ ] Replace the simulated shortcut demo with real shortcut detection.
- [ ] Replace informational WantCapture labels with actual context outputs.
- [ ] Implement mouse cursor demo with real cursor changes.
- [ ] Document how hosts should consume capture flags.

### Exit Criteria

- [ ] The parity caveats for shortcuts, mouse cursors, and WantCapture in `README.MD` can be removed or substantially reduced.
- [ ] New input features are available without breaking existing simple hosts.

## Milestone 2: Runtime State Queries And Focus APIs

### Why This Matters

Dear ImGui's power comes partly from easy access to item and window state. OpenControls needs a retained-mode equivalent so complex widgets and examples stop faking status with ad hoc hit tests.

### Primary Targets

- [ ] Add runtime status/query APIs for last item, focused item, hovered item, and relevant window/container state.
- [ ] Add focus-from-code APIs that are stronger than "request focus on this concrete element now".

### Checklist

- [ ] Design an item-state snapshot model:
  - [ ] hovered
  - [ ] active / pressed / dragging
  - [ ] edited
  - [ ] activated / deactivated
  - [ ] clicked
  - [ ] bounds / clip bounds
- [ ] Decide where these live:
  - [ ] per-element internal state
  - [ ] a per-frame state cache on `UiContext`
  - [ ] helper query methods
- [ ] Add window/container state queries:
  - [ ] focused
  - [ ] hovered
  - [ ] visible
  - [ ] clipped
  - [ ] open / active tab / active popup
- [ ] Add focus control helpers:
  - [ ] focus this element next frame
  - [ ] focus nth subcomponent / child path if needed
  - [ ] default focus behavior for popups and modals
  - [ ] clearer tab-order control
- [ ] Update examples:
  - [ ] item status section
  - [ ] window status section
  - [ ] focus-from-code section

### Exit Criteria

- [ ] The item/window status parity caveat in the README is no longer true in its current form.
- [ ] Complex widgets can query shared runtime state instead of recomputing local heuristics.

## Milestone 3: Text Entry Foundation

### Why This Is A Major Milestone

Text editing is the biggest user-facing gap after input. It is also a force multiplier for property editors, command palettes, filters, inspectors, and tool windows.

### Scope

- [ ] Upgrade `UiTextField` from simple insert/delete to production-grade single-line editing.
- [ ] Build shared text editing primitives that `UiNumericField` and `UiTextEditor` can reuse.

### Single-Line Text Field Checklist

- [ ] Text selection support:
  - [ ] click-drag selection
  - [ ] shift+arrow selection
  - [ ] double-click word select
  - [ ] select all
- [ ] Clipboard support:
  - [ ] copy
  - [ ] cut
  - [ ] paste
- [ ] Better caret movement:
  - [ ] word-left / word-right
  - [ ] home / end variants
  - [ ] selection-preserving navigation
- [ ] Editing history:
  - [ ] undo
  - [ ] redo
- [ ] Input modes:
  - [ ] normal text
  - [ ] password mode
  - [ ] read-only mode
  - [ ] filtered mode using callbacks or pluggable rules
- [ ] Callback/event hooks:
  - [ ] edit callback
  - [ ] completion callback
  - [ ] history callback
  - [ ] resize/grow callback or owned-buffer equivalent
- [ ] Rendering polish:
  - [ ] selected-text background
  - [ ] horizontal scroll for long text
  - [ ] caret reveal logic
  - [ ] alignment / eliding options where valuable

### Shared Text Infrastructure Checklist

- [ ] Introduce a reusable text buffer/edit state abstraction.
- [ ] Separate text model logic from widget rendering.
- [ ] Add clipboard access abstraction usable by hosts.
- [ ] Add UTF-8 and IME handling strategy if text input pipeline requires it.
- [ ] Decide whether text buffers should be string-backed, span-like, or pluggable.

### Numeric Field Follow-Through

- [ ] Rebase `UiNumericField` on the new text editing primitives.
- [ ] Preserve clamp/step behavior while gaining:
  - [ ] selection
  - [ ] copy/paste
  - [ ] undo/redo
  - [ ] better parsing lifecycle

### Demo/Docs Work

- [ ] Make the completion/history demo real instead of simulated.
- [ ] Implement password input properly.
- [ ] Implement resize callback or retained-mode equivalent for growing buffers.
- [ ] Document callback lifecycle clearly.

### Exit Criteria

- [ ] `UiTextField` feels credible in a real inspector/property editor.
- [ ] The README caveat about text callbacks can be removed or narrowed substantially.

## Milestone 4: Text Editor 2.0

### Scope

- [ ] Upgrade `UiTextEditor` from a capable demo/editor widget into a reusable editing surface.

### Checklist

- [ ] Add text selection:
  - [ ] mouse selection
  - [ ] keyboard selection
  - [ ] line selection helpers
- [ ] Add clipboard operations.
- [ ] Add undo/redo.
- [ ] Add multi-caret support only if still aligned with product needs.
- [ ] Add search/find primitives if desired for examples.
- [ ] Improve tokenization architecture so syntax modes can grow beyond C#.
- [ ] Add line wrapping mode as an option.
- [ ] Add optional minimap/gutter polish only after core editing is solid.
- [ ] Add editor events:
  - [ ] text changed
  - [ ] caret moved
  - [ ] selection changed
- [ ] Ensure editor uses shared text primitives from Milestone 3 where practical.

### Exit Criteria

- [ ] Text editor demos no longer feel like a separate toy path from text field behavior.
- [ ] The examples app can plausibly host a code or config editor workflow.

## Milestone 5: Composition APIs For Combo, List, Popup, And Child Content

### Why This Matters

OpenControls has useful concrete widgets, but Dear ImGui's flexibility often comes from container-style composition APIs. In retained mode, this should become reusable retained containers, not just more string-only controls.

### Combo/List/Popup Checklist

- [ ] Introduce a retained combo container that can host arbitrary child controls, not only strings.
- [ ] Keep `UiComboBox` as a convenience wrapper over the richer container if possible.
- [ ] Introduce a retained list-box container for arbitrary child content.
- [ ] Expand popup helpers:
  - [ ] context popup attached to an element
  - [ ] window context popup
  - [ ] empty-space context popup
  - [ ] better popup positioning and clamping
- [ ] Ensure popup and combo content participate in focus/capture logic correctly.
- [ ] Add menu helpers where composition overlap exists with popup behavior.

### Child Content / Child Windows Checklist

- [ ] Introduce a framed child-region/container primitive if `UiScrollPanel` is not enough.
- [ ] Support embedded scrollable child regions with strong clipping behavior.
- [ ] Document how child regions differ from windows and panels.

### Exit Criteria

- [ ] OpenControls can express richer combos, list boxes, and popup content without building custom one-off widgets every time.

## Milestone 6: Layout System Upgrade

### Why This Needs Its Own Milestone

Many missing ImGui demos are really layout affordances, not widgets. OpenControls should solve them in retained-mode terms instead of chasing exact `SameLine()` parity.

### Design Goal

- [ ] Add a small family of retained layout containers that cover the practical use cases behind ImGui layout demos.

### Checklist

- [ ] Introduce a horizontal stack / row container.
- [ ] Introduce a vertical stack / column container if helpful.
- [ ] Introduce a wrap/flow container for manual-wrapping style layouts.
- [ ] Introduce group semantics so multiple children can be treated as one logical region.
- [ ] Add spacing/alignment controls:
  - [ ] padding
  - [ ] gap
  - [ ] baseline alignment
  - [ ] stretch vs fixed children
- [ ] Add width helpers:
  - [ ] fill remaining width
  - [ ] fixed width
  - [ ] percentage/weight width where useful
- [ ] Revisit scrollable layouts:
  - [ ] horizontal scrolling containers
  - [ ] content-size computation
  - [ ] clipped long content
- [ ] Map existing examples to new layout containers where they simplify code.

### Explicit Non-Goal

- [ ] Do not force OpenControls into cursor-based immediate layout semantics unless a tiny compatibility helper is clearly worth it.

### Exit Criteria

- [ ] The major "Layout" demo sections have natural retained-mode equivalents.
- [ ] Example UI code gets shorter, not longer.

## Milestone 7: Tables 2.0

### Why This Is High Priority

Tables are one of the most important tooling surfaces, and OpenControls currently has a useful but shallow implementation.

### Current Gaps To Close

- [ ] column resizing
- [ ] column reordering
- [ ] column hiding/showing
- [ ] vertical scrolling
- [ ] horizontal scrolling
- [ ] header context menus
- [ ] richer cell rendering
- [ ] nested/advanced layouts where practical
- [ ] better handling of large datasets

### Checklist

- [ ] Add a column state model distinct from static column definitions.
- [ ] Add user-driven column resize.
- [ ] Add user-driven column reorder.
- [ ] Add hide/show state for columns.
- [ ] Add optional built-in header context menu.
- [ ] Add X/Y scrolling support.
- [ ] Add fixed vs stretch width modes.
- [ ] Add row height customization hooks.
- [ ] Add per-cell background / foreground customization hooks.
- [ ] Add a richer cell content model:
  - [ ] text-only path stays simple
  - [ ] child-element / retained-content path where feasible
- [ ] Add virtualization/clipping support for large row counts.
- [ ] Verify sorting still behaves correctly with hidden/reordered columns.
- [ ] Add examples for:
  - [ ] reordering and hiding
  - [ ] vertical clipping
  - [ ] context menus
  - [ ] sorting
  - [ ] custom headers

### Exit Criteria

- [ ] `UiTable` is viable for inspectors, asset browsers, and data-heavy tools.

## Milestone 8: Shared Virtualization / Clipper Layer

### Why This Is Separate

Large-data support should not be reinvented in list boxes, tables, trees, and selection demos independently.

### Checklist

- [ ] Design a renderer-agnostic virtualization primitive for sequential item ranges.
- [ ] Support:
  - [ ] fixed-height rows first
  - [ ] variable-height rows later only if needed
- [ ] Integrate with:
  - [ ] `UiListBox`
  - [ ] `UiTable`
  - [ ] large trees
  - [ ] advanced multi-select examples
- [ ] Ensure selection can survive clipped/non-materialized rows.
- [ ] Ensure hover and keyboard navigation behave correctly with partial materialization.
- [ ] Add examples using thousands of rows/items.

### Exit Criteria

- [ ] "with clipper" examples stop being conceptual and become real capabilities.

## Milestone 9: Selection And Trees

### Why This Matters

Selection is already one of OpenControls' better stories, but Dear ImGui's newer multi-select demos show how much deeper the interaction model can go.

### Checklist

- [ ] Extend `UiSelectionModel` for larger selection workflows:
  - [ ] deletion-friendly updates
  - [ ] multiple scopes
  - [ ] shared selection across multiple views
  - [ ] optional range/anchor persistence rules
- [ ] Add box selection only if it aligns with core product use cases.
- [ ] Improve tree support:
  - [ ] selectable nodes
  - [ ] hierarchy lines
  - [ ] better open-state control APIs
  - [ ] large-tree virtualization integration
- [ ] Add tree/table/list cross-demo coverage:
  - [ ] single select
  - [ ] multi-select
  - [ ] dual list box
  - [ ] selection in table
  - [ ] selection in trees

### Exit Criteria

- [ ] Multi-select no longer feels limited to basic Ctrl/Shift toggling.

## Milestone 10: Drag, Slider, Numeric, And Color Depth

### Why This Comes Later

OpenControls already covers these areas reasonably well. This phase is about depth and option coverage, not rescuing missing core workflows.

### Drag/Slider Checklist

- [ ] Expand drag/slider flags:
  - [ ] always clamp
  - [ ] no input / disable text entry path if desired
  - [ ] no round to format precision
  - [ ] wrap-around behavior
  - [ ] color markers for multi-component numeric controls if valuable
- [ ] Decide on the retained-mode equivalent of ImGui's Ctrl+Click-to-input behavior.
- [ ] Ensure all vector/scalar variants inherit shared semantics consistently.

### Color Checklist

- [ ] Expand `UiColorEdit` options:
  - [ ] RGB / HSV / Hex display modes
  - [ ] float vs byte display
  - [ ] optional picker popup integration
  - [ ] option menu or retained settings surface
- [ ] Expand `UiColorPicker` options:
  - [ ] hue bar vs hue wheel if desired
  - [ ] side preview options
  - [ ] no-input / no-preview modes
  - [ ] drag/drop of colors
- [ ] Ensure color widgets share data conversion logic.

### Exit Criteria

- [ ] Color and numeric widgets feel configurable rather than fixed-function.

## Milestone 11: Menus, Popups, Tooltips, And Overlay Polish

### Checklist

- [ ] Add item-attached context menu helpers.
- [ ] Add better tooltip trigger policies:
  - [ ] delayed hover
  - [ ] keyboard-focus tooltip path
  - [ ] tooltip suppression during drag where appropriate
- [ ] Improve popup nesting and dismissal rules.
- [ ] Add more robust menu keyboard navigation.
- [ ] Ensure overlays cooperate with capture and focus rules from Milestones 1 and 2.

### Exit Criteria

- [ ] Overlay behavior feels predictable across menus, popups, modals, and tooltips.

## Milestone 12: Tooling, Style Editor, And Examples Parity

### Why This Is Its Own Phase

Once the infrastructure and core widgets are stronger, the examples app can stop papering over missing behaviors and become a real showcase and regression tool.

### Checklist

- [ ] Replace simulated examples with real feature usage throughout `OpenControls.Examples`.
- [ ] Build a richer style editor:
  - [ ] color editing
  - [ ] spacing/padding controls
  - [ ] typography/text scale controls
  - [ ] corner radius and border controls
  - [ ] state previews
- [ ] Improve the "About OpenControls" tooling surface if desired.
- [ ] Add a regression-oriented example section for:
  - [ ] large list/table/tree datasets
  - [ ] focus/capture behavior
  - [ ] advanced text input
  - [ ] cursor changes
- [ ] Re-run parity review against the target Dear ImGui snapshot.
- [ ] Update `README.MD`, `API.MD`, `Samples.MD`, and `Quickstart.MD` to reflect the new reality.

### Exit Criteria

- [ ] The examples app is a trustworthy parity showcase, not just a structure-matching demo tree.

## Stretch Goals / Parking Lot

- [ ] Multi-viewport/windowing parity if OpenControls ever wants to chase that class of functionality.
- [ ] Gamepad-first navigation polish beyond current keyboard focus flows.
- [ ] More renderer features if future widgets require them.
- [ ] Accessibility-oriented abstractions if that becomes a product goal.
- [ ] Full style/theme system pass beyond widget-local properties.

## Recommended Delivery Order

If resourcing is tight, do the work in this order:

- [ ] Phase 0
- [ ] Milestone 1
- [ ] Milestone 2
- [ ] Milestone 3
- [ ] Milestone 7
- [ ] Milestone 8
- [ ] Milestone 5
- [ ] Milestone 6
- [ ] Milestone 9
- [ ] Milestone 10
- [ ] Milestone 11
- [ ] Milestone 12
- [ ] Milestone 4 when editor depth becomes important enough to justify the extra scope

## Suggested Release Slices

### Release Slice A: Infrastructure

- [ ] Phase 0
- [ ] Milestone 1
- [ ] Milestone 2

### Release Slice B: Text And Editing

- [ ] Milestone 3
- [ ] Milestone 4

### Release Slice C: Data Views

- [ ] Milestone 7
- [ ] Milestone 8
- [ ] Milestone 9

### Release Slice D: Composition And Layout

- [ ] Milestone 5
- [ ] Milestone 6

### Release Slice E: Polish And Showcase

- [ ] Milestone 10
- [ ] Milestone 11
- [ ] Milestone 12

## Final Notes

- [ ] If only one thing happens next, it should be Milestone 1.
- [ ] If only one user-facing thing happens after that, it should be Milestone 3 or Milestone 7 depending on whether your near-term product is more editor-heavy or data-view-heavy.
- [ ] Re-audit after Milestone 3 and again after Milestone 7 before committing to the entire tail of the roadmap.
