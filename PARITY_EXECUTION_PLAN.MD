# OpenControls Phase 0 Execution Plan

This document turns the current OpenControls capability/parity audit into an execution plan.

The goal is not to clone another toolkit one-to-one. The goal is to close the high-value capability gaps while preserving OpenControls' retained-mode, explicit-bounds design.

## Operating Principles

- [ ] Preserve retained-mode architecture. Prefer OpenControls-native retained equivalents over direct API imitation.
- [ ] Land infrastructure before surface widgets when the same foundation unlocks multiple demos.
- [ ] Treat "demo parity" and "product parity" separately.
- [ ] Ship in slices that leave the examples app better after every milestone.
- [ ] Add docs and examples in the same milestone as the feature, not later.
- [ ] Treat exhaustive widget-option example coverage as a first-class deliverable, not cleanup polish.
- [ ] Keep renderer adapters thin; put behavior in `OpenControls` where possible.
- [ ] Avoid introducing host-specific logic into core controls unless the feature truly belongs there.

## Success Criteria

- [ ] The documented parity deviations in [README.MD](/Users/thomasmayer/Dev/OpenControls/README.MD#L51) are either removed or intentionally reclassified as "won't match by design".
- [ ] Input and host integration are rich enough to support true shortcuts, cursor requests, capture flags, and better focus routing.
- [x] Text rendering supports custom fonts, Unicode/icon glyph ranges, and merged icon fonts instead of only the built-in bitmap path.
- [ ] Text entry is editor-grade for common tooling workflows.
- [ ] Tables, lists, trees, and selection scale to large datasets.
- [ ] `OpenControls.Examples` exhaustively demonstrates every public widget and every meaningful shipped option, mode, and interaction path, or documents the omission explicitly.
- [ ] `tools/parity/WIDGET_DEMO_COVERAGE.MD` stays in sync with the real example app.
- [ ] Every milestone has exit criteria, docs, and examples.

## Cross-Cutting Definition Of Done

Every milestone below is only complete when all of these are checked:

- [ ] Public API reviewed for naming consistency with existing `Ui*` types.
- [ ] Shared examples updated.
- [ ] `tools/parity/WIDGET_DEMO_COVERAGE.MD` updated for any new public widget surface or demo-coverage change.
- [ ] API docs updated in `API.MD`.
- [ ] Sample usage added to `Samples.MD` or `Quickstart.MD` where appropriate.
- [ ] At least one verification path exists for the new behavior.
- [ ] Editor-blocking behavior has regression coverage beyond "project builds".
- [ ] README parity caveats are updated if the milestone changes them.

## Milestone Status Semantics

- A milestone is only called complete when its required checklist items and exit criteria are fully checked.
- Mutually exclusive design decisions should be recorded as a chosen baseline plus deferred alternatives, not as permanently half-checked checkbox groups.
- Optional or post-baseline work should live in a clearly named deferred follow-on subsection so it does not make a shipped milestone read as incomplete.

## Widget Demo Coverage Rule

- `tools/parity/WIDGET_DEMO_COVERAGE.MD` is the canonical checklist for the shared example app.
- "Implemented but not shown" does not count as demo-complete.
- New public widgets, meaningful widget options, and intentionally omitted demo states should update that matrix in the same change that changes the underlying surface area.
- Milestones can land before the entire matrix is complete, but each milestone still needs to improve honest example coverage for the surface it introduces or changes.

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

- [x] Record the reference snapshot/version this roadmap targets.
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
  - [x] legacy compatibility APIs with poor long-term value
  - [x] niche styling flags that add complexity without product value

### Exit Criteria

- [x] The team can point at one target version and one prioritized backlog.
- [x] The next milestone can start without reopening scope debates.

## Milestone 1: Input Platform Contract

### Why First

Current documented parity gaps around shortcuts, mouse cursors, WantCapture behavior, and parts of advanced text input all point back to the same bottleneck: the input model is too small.

### Primary Targets

- [x] Expand `UiInputState` beyond the current minimal left-button + Ctrl/Shift model.
- [x] Add host-facing capture/cursor/text-input outputs on `UiContext`.
- [x] Introduce a scalable key model instead of one-off booleans for every future key.

### API Work

- [x] Extend `UiInputState` with richer pointer data:
  - [x] right mouse button
  - [x] middle mouse button
  - [x] double click information
  - [x] drag thresholds / drag origin data if needed
  - [x] wheel X in addition to wheel Y if useful
- [x] Extend modifier support:
  - [x] Alt
  - [x] Super / Command / Windows key
  - [x] platform-specific shortcut modifier abstraction
- [x] Introduce richer keyboard state:
  - [x] named key enum
  - [x] per-key down/pressed/released queries
  - [x] key chord representation
  - [x] shortcut routing helper
- [x] Add host output properties somewhere stable, likely on `UiContext`:
  - [x] `WantCaptureMouse`
  - [x] `WantCaptureKeyboard`
  - [x] `WantTextInput`
  - [x] requested mouse cursor shape
  - [x] optional IME/text-input request metadata

### Core Behavior Work

- [x] Ensure focus, hover, active drag, popup, and modal state all contribute to capture outputs.
- [x] Ensure clicks in empty space still clear focus correctly while capture flags remain accurate.
- [x] Define precedence rules for shortcuts:
  - [x] focused widget shortcuts
  - [x] hovered widget shortcuts
  - [x] global shortcuts
  - [x] popup/modal override behavior
- [x] Add a single place where host adapters read "what cursor should I show right now?"

### Host Integration Work

- [x] Update MonoGame host to populate richer input state.
- [x] Update SDL host to populate richer input state.
- [x] Update Silk.NET host to populate richer input state.
- [x] Update OpenGL example host input plumbing as needed.
- [x] Normalize platform shortcut semantics so macOS-style behavior is possible later if desired.

### Demo/Docs Work

- [x] Replace the simulated shortcut demo with real shortcut detection.
- [x] Replace informational WantCapture labels with actual context outputs.
- [x] Implement mouse cursor demo with real cursor changes.
- [x] Document how hosts should consume capture flags.

### Exit Criteria

- [x] The parity caveats for shortcuts, mouse cursors, and WantCapture in `README.MD` can be removed or substantially reduced.
- [x] New input features are available without breaking existing simple hosts.

## Milestone 2: Runtime State Queries And Focus APIs

### Why This Matters

A strong tools UI often depends on easy access to item and window state. OpenControls needs a retained-mode equivalent so complex widgets and examples stop faking status with ad hoc hit tests.

### Primary Targets

- [x] Add runtime status/query APIs for last item, focused item, hovered item, and relevant window/container state.
- [x] Add focus-from-code APIs that are stronger than "request focus on this concrete element now".

### Checklist

- [x] Design an item-state snapshot model:
  - [x] hovered
  - [x] active / pressed / dragging
  - [x] edited
  - [x] activated / deactivated
  - [x] clicked
  - [x] bounds / clip bounds
- [x] Decide where these live:
  - [x] per-element internal state
  - [x] a per-frame state cache on `UiContext`
  - [x] helper query methods
- [x] Add window/container state queries:
  - [x] focused
  - [x] hovered
  - [x] visible
  - [x] clipped
  - [x] open / active tab / active popup
- [x] Add focus control helpers:
  - [x] focus this element next frame
  - [x] focus nth subcomponent / child path if needed
  - [x] default focus behavior for popups and modals
  - [x] clearer tab-order control
- [x] Update examples:
  - [x] item status section
  - [x] window status section
  - [x] focus-from-code section

### Exit Criteria

- [x] The item/window status parity caveat in the README is no longer true in its current form.
- [x] Complex widgets can query shared runtime state instead of recomputing local heuristics.

## Milestone 3: Font System, Atlas, And Icon Glyph Pipeline

### Why This Needs Its Own Milestone

OpenControls currently assumes a tiny built-in bitmap font and a narrow code-page model. To reach real tooling parity, the framework needs a font asset pipeline that can load custom fonts, handle broader Unicode/code point ranges, and merge icon fonts like Font Awesome into the same text flow cleanly.

### Implementation Note

Milestone 3 is complete for the scoped first-pass font/icon parity target. OpenControls ships `UiFont`, `UiFontBuilder`, `UiFontRegistry`, per-element/inherited font selection, renderer-side glyph caches across all backends, and a Silk.NET-first demo path for custom body fonts plus merged icon fonts. GPU atlas work, HarfBuzz shaping depth, and any Slug spike remain deferred follow-on work rather than blockers for the usable parity baseline.

### Scope

- [x] Add a font system that supports custom application fonts instead of only `TinyBitmapFont`.
- [x] Add support for icon-bearing fonts and merged glyph ranges so text and icons can share one rendering path.
- [x] Make Silk.NET the reference host for font parity validation, then keep the other renderers aligned.

### Font Asset Checklist

- [x] Introduce a font asset abstraction decoupled from `TinyBitmapFont`.
- [x] Support loading:
  - [x] TTF
  - [x] OTF
  - [x] WOFF2
- [x] Decide the WOFF2 approach for the shipped baseline.
  Chosen baseline: runtime decode through a dependency.
  Deferred alternatives: managed decode or preprocessing to an internal baked format.
- [x] Add a font registry / cache owned by the framework or host boundary.
- [x] Support multiple named font faces and sizes in one app session.
- [x] Preserve a built-in fallback/default font so simple hosts still work out of the box.

### Glyph And Unicode Checklist

- [x] Replace the current Latin-1 / CP437-only assumptions with a broader code point pipeline.
- [x] Define glyph range requests:
  - [x] default Latin set
  - [x] extended Unicode ranges as needed
  - [x] icon ranges
- [x] Support font fallback chains when a glyph is missing from the primary face.
- [x] Add missing-glyph behavior that is deterministic and debuggable.
- [x] Split font rendering goals into two passes:
  - [x] first-pass custom-font/icon-font parity without requiring full shaping
  - [x] follow-on shaping/text-layout work for complex scripts and higher typography fidelity
- [x] Decide the shaping/kerning baseline for the first usable version.
  Shipped baseline: no shaping, simple glyph-cache path.
  Deferred follow-on: kerning-aware Latin path if product value justifies it.
  Deferred follow-on: future shaping/text-layout milestone for complex scripts.

### HarfBuzz Evaluation Checklist

- [x] Evaluate HarfBuzz as an optional shaping backend rather than a blocker for initial custom-font support.
- [x] Document that HarfBuzz is useful for:
  - [x] ligatures
  - [x] kerning-aware glyph positioning
  - [x] combining marks
  - [x] bidi/RTL-aware shaping pipelines
  - [x] complex scripts such as Arabic and Indic families
- [x] Document that HarfBuzz is not the main dependency for:
  - [x] TTF/OTF/WOFF2 loading
  - [x] glyph atlas generation
  - [x] Font Awesome-style icon font merging
- [x] If adopted, keep HarfBuzz integration behind a clean text-layout/shaping layer so simple hosts can still ship without it.

### Icon Font Parity Checklist

- [x] Add an API for merging icon fonts into an existing text font configuration.
- [x] Support custom code point ranges like Font Awesome icon spans.
- [x] Allow text and icons to coexist in one string/render call without custom widget hacks.
- [x] Support icon glyph use in:
  - [x] labels
  - [x] buttons
  - [x] menus
  - [x] tabs
  - [x] tree nodes
- [x] Add spacing/baseline controls so merged icon fonts align cleanly with body text.

### Renderer / Host Checklist

- [x] Introduce a renderer-facing font atlas or glyph cache abstraction.
- [x] Ensure measurement and drawing use the same glyph metrics across all renderers.
- [x] Implement Silk.NET glyph-cache draw flow first.
- [x] Keep MonoGame, SDL, OpenGL, and software/headless renderers functional with the same font contracts.

### API And Styling Checklist

- [x] Add a stable way to choose fonts in UI code:
  - [x] per-element font
  - [x] inherited/default font
- [x] Add font metrics helpers needed by layout and text editing.
- [x] Document how icon fonts are configured and merged.

### Deferred Follow-On Work

These items remain real backlog, but they are not required for Milestone 3 closure:

- Define rebuild behavior when fonts or glyph ranges change at runtime.
- Revisit whether a temporary push/pop style equivalent is useful in retained mode.
- Evaluate [Slug](https://github.com/EricLengyel/Slug) as an optional advanced GPU font-rendering path for shader-driven/vector-quality text.
- If a Slug spike happens, keep it renderer-optional, bias it toward Silk.NET first, and keep the default font pipeline independent.

### Demo/Docs Work

- [x] Add a custom font demo in `OpenControls.Examples`.
- [x] Add an icon font demo using merged glyph ranges.
- [x] Add a Silk.NET-first sample showing inline icon-font usage.
- [x] Update `README.MD`, `API.MD`, and `Quickstart.MD` with font-loading guidance.

### Exit Criteria

- [x] A user can load a custom TTF/OTF/WOFF2 font and use it throughout the UI.
- [x] A user can merge an icon font range into a text font and render icons inline within standard text content.
- [x] The roadmap clearly distinguishes first-pass custom-font/icon-font parity from optional HarfBuzz-backed shaping work.
- [x] The built-in bitmap font path still works as a fallback/simple-host option.

## Milestone 4: Text Entry Foundation

### Why This Is A Major Milestone

Text editing is the biggest user-facing gap after input. It is also a force multiplier for property editors, command palettes, filters, inspectors, and tool windows.

### Implementation Note

Milestone 4 is complete for the scoped first-pass text-entry target. OpenControls ships `UiTextEditingState`, `IUiClipboard` plus default/delegate clipboard adapters, a substantially upgraded `UiTextField`, and `UiNumericField` riding on the richer single-line editing path. The shared examples now use real password/completion/history/resize behavior, and `OpenControls.Tests` covers the shared editing core. IME composition depth remains deferred follow-on work rather than a blocker for the usable baseline.

### Scope

- [x] Upgrade `UiTextField` from simple insert/delete to production-grade single-line editing.
- [x] Build shared text editing primitives that `UiNumericField` and `UiTextEditor` can reuse.

### Single-Line Text Field Checklist

- [x] Text selection support:
  - [x] click-drag selection
  - [x] shift+arrow selection
  - [x] double-click word select
  - [x] select all
- [x] Clipboard support:
  - [x] copy
  - [x] cut
  - [x] paste
- [x] Better caret movement:
  - [x] word-left / word-right
  - [x] home / end variants
  - [x] selection-preserving navigation
- [x] Editing history:
  - [x] undo
  - [x] redo
- [x] Commit / cancel behavior:
  - [x] submit/accept callback or command hook
  - [x] cancel/revert path for inspector-style editing
- [x] Input modes:
  - [x] normal text
  - [x] password mode
  - [x] read-only mode
  - [x] filtered mode using callbacks or pluggable rules
- [x] Callback/event hooks:
  - [x] edit callback
  - [x] completion callback
  - [x] history callback
  - [x] resize/grow callback or owned-buffer equivalent
- [x] Rendering polish:
  - [x] selected-text background
  - [x] horizontal scroll for long text
  - [x] caret reveal logic

### Shared Text Infrastructure Checklist

- [x] Introduce a reusable text buffer/edit state abstraction.
- [x] Separate text model logic from widget rendering.
- [x] Add clipboard access abstraction usable by hosts.
- [x] Decide whether text buffers should be string-backed, span-like, or pluggable.

### Numeric Field Follow-Through

- [x] Rebase `UiNumericField` on the new text editing primitives.
- [x] Preserve clamp/step behavior while gaining:
  - [x] selection
  - [x] copy/paste
  - [x] undo/redo
  - [x] better parsing lifecycle

### Demo/Docs Work

- [x] Make the completion/history demo real instead of simulated.
- [x] Implement password input properly.
- [x] Implement resize callback or retained-mode equivalent for growing buffers.
- [x] Document callback lifecycle clearly.

### Deferred Follow-On Work

These items remain backlog, but they do not block Milestone 4 closure:

- Add alignment/eliding options where single-line product UX clearly benefits.
- Add UTF-8 and IME handling strategy depth if the host text-input pipeline requires it.
- Add composition/IME state plumbing beyond raw committed characters, likely shared with Milestone 5 and future international text work.

### Exit Criteria

- [x] `UiTextField` feels credible in a real inspector/property editor.
- [x] The README caveat about text callbacks can be removed or narrowed substantially.

## Milestone 5: Text Editor 2.0

### Implementation Note

Milestone 5 is complete for the scoped first-pass text-editor target. OpenControls now ships a `UiTextEditingState`-backed `UiTextEditor` with mouse and keyboard selection, clipboard operations, undo/redo, word/page/home/end navigation, text/caret/selection events, selection rendering, and shared example/test coverage. Search, wrapping, IME composition depth, tokenizer expansion, and minimap/multi-caret work remain deferred follow-on work rather than blockers for the usable editor baseline.

### Scope

- [x] Upgrade `UiTextEditor` from a capable demo/editor widget into a reusable editing surface.

### Checklist

- [x] Add text selection:
  - [x] mouse selection
  - [x] keyboard selection
  - [x] line selection helpers
- [x] Add clipboard operations.
- [x] Add undo/redo.
- [x] Add richer editor navigation:
  - [x] word-left / word-right
  - [x] page up / page down
  - [x] better home / end behavior
- [x] Add editor events:
  - [x] text changed
  - [x] caret moved
  - [x] selection changed
- [x] Ensure editor uses shared text primitives from Milestone 4 where practical.

### Demo/Docs Work

- [x] Make the multiline examples show real selection/clipboard/navigation behavior.
- [x] Add regression coverage for core editor workflows.
- [x] Update parity/docs language so the multiline editor caveat no longer points at Milestone 5 as wholly undone.

### Deferred Follow-On Work

These items remain backlog, but they do not block Milestone 5 closure:

- Add multi-caret support if product needs justify the complexity.
- Add search/find primitives and richer editor tooling helpers for examples.
- Improve tokenization architecture so syntax modes can grow beyond C# cleanly.
- Add line wrapping mode with correct hit-testing/selection behavior.
- Add IME/composition-aware editing depth for multiline text.
- Add optional minimap/gutter polish only after core editing is solid.

### Exit Criteria

- [x] Text editor demos no longer feel like a separate toy path from text field behavior.
- [x] The examples app can plausibly host a code or config editor workflow.

## Milestone 6: Composition APIs For Combo, List, Popup, And Child Content

### Why This Matters

OpenControls has useful concrete widgets, but a lot of tools-UI flexibility comes from container-style composition APIs. In retained mode, this should become reusable retained containers, not just more string-only controls.

### Combo/List/Popup Checklist

- [x] Introduce a retained combo container that can host arbitrary child controls, not only strings.
- [x] Keep `UiComboBox` as a convenience wrapper over the richer container if possible.
- [x] Introduce a retained list-box container for arbitrary child content.
- [x] Support typed-search / filterable combo patterns without bespoke one-off widgets.
- [x] Support richer rows in combo/list content:
  - [x] icons/thumbnails
  - [x] secondary text
  - [x] asset-picker style rows
- [x] Expand popup helpers:
  - [x] context popup attached to an element
  - [x] window context popup
  - [x] empty-space context popup
  - [x] better popup positioning and clamping
- [x] Ensure popup and combo content participate in focus/capture logic correctly.
- [x] Add menu helpers where composition overlap exists with popup behavior.

### Child Content / Child Windows Checklist

- [x] Introduce a framed child-region/container primitive if `UiScrollPanel` is not enough.
- [x] Support embedded scrollable child regions with strong clipping behavior.
- [x] Document how child regions differ from windows and panels.
- [x] Standardize image/texture interop for composed content:
  - [x] renderer-level texture/image primitive or a stable adapter contract
  - [x] thumbnails/icons in combo/list/table rows without ad hoc callback wiring every time
  - [x] examples that prove asset/browser-style usage

### First-Pass Demo

- [x] `OpenControls.Examples` shows a composed combo/list/popup/child-region demo built from `UiCombo`, `UiListView`, `UiSelectableRow`, `UiChildRegion`, and `UiDelegateImageSource`.
- [x] Popup helpers are demonstrated via attached and context opening in the examples UI.
- [x] Milestone 6 is now fully complete in the repo, including the `UiComboBox` compatibility wrapper and element/window/empty-space context popup demos.

### Exit Criteria

- [x] OpenControls can express richer combos, list boxes, popup content, and child regions without building custom one-off widgets every time.

## Milestone 7: Layout System Upgrade

### Why This Needs Its Own Milestone

Many remaining layout gaps are really layout affordances, not widgets. OpenControls should solve them in retained-mode terms instead of chasing exact helper-for-helper imitation.

### Design Goal

- [x] Add a small family of retained layout containers that cover the practical layout use cases still missing from the examples surface.

### Checklist

- [x] Introduce a horizontal stack / row container.
- [x] Introduce a vertical stack / column container if helpful.
- [x] Introduce a wrap/flow container for manual-wrapping style layouts.
- [x] Introduce group semantics so multiple children can be treated as one logical region.
- [x] Add spacing/alignment controls:
  - [x] padding
  - [x] gap
  - [x] baseline alignment
  - [x] stretch vs fixed children
- [x] Add width helpers:
  - [x] fill remaining width
  - [x] fixed width
  - [x] percentage/weight width where useful
- [x] Revisit scrollable layouts:
  - [x] horizontal scrolling containers
  - [x] content-size computation
  - [x] clipped long content
- [x] Map existing examples to new layout containers where they simplify code.

### Docking / Split Pane Checklist

- [x] Add draggable dock splitters.
- [x] Update split ratios from divider hit-testing and drag, not only from initial split creation.
- [x] Add minimum pane sizes / clamp rules so resize feels editor-safe.
- [x] Persist dock split ratios as part of workspace/session state.
- [x] Add visual splitter affordances and hover/active states.
- [x] Verify nested split resizing behaves predictably.

### Explicit Non-Goal

- [x] Do not force OpenControls into cursor-based immediate layout semantics unless a tiny compatibility helper is clearly worth it.

### Exit Criteria

- [x] The major "Layout" demo sections have natural retained-mode equivalents.
- [x] Example UI code gets shorter, not longer.
- [x] Milestone 7 is now fully complete in the repo, including retained layout containers, grouped layout demos, and draggable dock-workspace split resizing with persistence and nested clamp coverage.

## Milestone 8: Tables 2.0

### Implementation Note

Milestone 8 is complete for the scoped first-pass table target. OpenControls now ships a `UiTable` with runtime column state separate from static definitions, user-driven resize/reorder/hide behavior, X/Y scrolling, built-in header context menus, explicit view state, row clipping for large datasets, fixed-vs-stretch column sizing, row-height hooks, per-cell styling hooks, and retained child content for headers/cells. Shared clipper extraction and deeper cross-control virtualization still remain Milestone 9 work.

### Current Gaps To Close

- [x] column resizing
- [x] column reordering
- [x] column hiding/showing
- [x] vertical scrolling
- [x] horizontal scrolling
- [x] header context menus
- [x] richer cell rendering
- [x] nested/advanced layouts where practical
- [x] better handling of large datasets

### Checklist

- [x] Add a column state model distinct from static column definitions.
- [x] Add user-driven column resize.
- [x] Add user-driven column reorder.
- [x] Add hide/show state for columns.
- [x] Add optional built-in header context menu.
- [x] Add X/Y scrolling support.
- [x] Add explicit row-scroll/view state rather than truncating to the currently visible prefix.
- [x] Add fixed vs stretch width modes.
- [x] Add row height customization hooks.
- [x] Add per-cell background / foreground customization hooks.
- [x] Add a richer cell content model:
  - [x] text-only path stays simple
  - [x] child-element / retained-content path where feasible
  - [x] non-string cell content suitable for inspectors, hierarchies, and asset browsers
- [x] Add virtualization/clipping support for large row counts.
- [x] Verify sorting still behaves correctly with hidden/reordered columns.
- [x] Add examples for:
  - [x] reordering and hiding
  - [x] vertical clipping
  - [x] context menus
  - [x] sorting
  - [x] custom headers

### Deferred Follow-On Work

These items remain backlog, but they do not block Milestone 8 closure:

- Lift the table-local row clipping logic into the shared virtualization layer in Milestone 9.
- Add persisted table column/view state once the broader persistence milestone is ready to absorb it cleanly.
- Add variable-height virtualization optimizations only if product-scale datasets demand more than the current clipped rendering path.

### Exit Criteria

- [x] `UiTable` is viable for inspectors, asset browsers, and data-heavy tools.

## Milestone 9: Shared Virtualization / Clipper Layer

Milestone 9 is complete for the scoped first-pass virtualization target. OpenControls now ships a shared fixed-height `UiClipper` / `UiClipRange` layer, `UiListBox` uses that range math directly, uniform-height `UiTable` rows route through it, and the new `UiTreeView` gives the repo a reusable large-tree path instead of a demo-only stub. The `Clipping` window now demonstrates real 4,000-row list, 6,000-row multi-select list, 2,500-row table, and 8,040-row tree datasets with live visible-range readouts, so “with clipper” is no longer conceptual.

### Why This Is Separate

Large-data support should not be reinvented in list boxes, tables, trees, and selection demos independently.

### Checklist

- [x] Design a renderer-agnostic virtualization primitive for sequential item ranges.
- [x] Support fixed-height rows first.
- [x] Integrate with:
  - [x] `UiListBox`
  - [x] `UiTable`
  - [x] large trees
  - [x] advanced multi-select examples
- [x] Ensure selection can survive clipped/non-materialized rows.
- [x] Ensure hover and keyboard navigation behave correctly with partial materialization.
- [x] Add examples using thousands of rows/items.

### Exit Criteria

- [x] "with clipper" examples stop being conceptual and become real capabilities.

### Deferred Follow-On Work

These items remain backlog, but they do not block Milestone 9 closure:

- [ ] Promote variable-height virtualization from fallback clipped rendering to the shared clipper only if product-scale data proves the extra complexity is necessary.

## Milestone 10: Selection And Trees

Milestone 10 is complete for the scoped first-pass selection/tree target. OpenControls now ships a scoped `UiSelectionModel` with deletion-friendly mutation helpers, shared-selection support across multiple views, optional anchor persistence modes, and richer tree behavior through `UiTreeView` hierarchy lines plus programmatic expand/collapse/reveal helpers. The Widget Gallery now includes an explicit Milestone 10 section that demonstrates single-select, multi-select, shared list/table scopes, dual scoped list boxes, anchor-persistence switching, and tree selection/open-state workflows in one place.

### Why This Matters

Selection is already one of OpenControls' better stories, but larger editor-style multi-select workflows show how much deeper the interaction model can go.

### Checklist

- [x] Extend `UiSelectionModel` for larger selection workflows:
  - [x] deletion-friendly updates
  - [x] multiple scopes
  - [x] shared selection across multiple views
  - [x] optional range/anchor persistence rules
- [ ] Add box selection only if it aligns with core product use cases.
- [x] Improve tree support:
  - [x] selectable nodes
  - [x] hierarchy lines
  - [x] better open-state control APIs
  - [x] large-tree virtualization integration
- [x] Add tree/table/list cross-demo coverage:
  - [x] single select
  - [x] multi-select
  - [x] dual list box
  - [x] selection in table
  - [x] selection in trees

### Exit Criteria

- [x] Multi-select no longer feels limited to basic Ctrl/Shift toggling.

### Deferred Follow-On Work

These items remain backlog, but they do not block Milestone 10 closure:

- Box selection is still optional and should only land if it aligns with real product workflows.
- Tree selection still keys off visible row indices today; if a future editor surface needs identity-stable selection across aggressive tree restructuring, that should be a follow-on design pass.

## Milestone 11: Drag, Slider, Numeric, And Color Depth

### Why This Comes Later

OpenControls already covers these areas reasonably well. This phase is about depth and option coverage, not rescuing missing core workflows.

### Drag/Slider Checklist

- [x] Expand drag/slider flags:
  - [x] always clamp
  - [x] no input / disable text entry path if desired
  - [x] no round to format precision
  - [x] wrap-around behavior
- [x] Decide on the retained-mode equivalent of Ctrl+Click direct-input behavior.
- [x] Ensure all vector/scalar variants inherit shared semantics consistently.

### Color Checklist

- [x] Expand `UiColorEdit` options:
  - [x] RGB / HSV / Hex display modes
  - [x] float vs byte display
  - [x] optional picker popup integration
  - [x] option menu or retained settings surface
- [x] Expand `UiColorPicker` options:
  - [x] side preview options
  - [x] no-input / no-preview modes
  - [x] drag/drop of colors
- [x] Ensure color widgets share data conversion logic.

### Exit Criteria

- [x] Color and numeric widgets feel configurable rather than fixed-function.

Milestone 11 is now complete for the shipped first-pass scope: sliders and drags now expose configurable clamp/wrap/direct-input/precision behavior, vector and range variants inherit the same semantics, `UiColorEdit` now covers RGB/HSV/Hex plus byte/float displays and picker-popup integration, `UiColorPicker` now exposes preview/input toggles plus color drag/drop, and the Widget Gallery has a dedicated Milestone 11 demo section plus regression coverage in `OpenControls.Tests`.

### Deferred Follow-On Work

These items remain backlog, but they do not block Milestone 11 closure:

- Multi-component color markers are still optional polish; add them only if they prove genuinely valuable in editor workflows.
- Hue-wheel picker variants remain optional renderer/UI polish; the shipped first-pass scope standardizes on the hue-bar path.

## Milestone 12: Menus, Popups, Tooltips, And Overlay Polish

### Checklist

- [x] Add item-attached context menu helpers.
- [x] Add a real menu command model instead of shortcut labels being display-only.
- [x] Parse and dispatch menu shortcuts through the shared shortcut routing from Milestone 1.
- [x] Support menu items that can be invoked consistently from:
  - [x] mouse activation
  - [x] keyboard shortcut activation
  - [x] programmatic command activation
- [x] Add better tooltip trigger policies:
  - [x] delayed hover
  - [x] keyboard-focus tooltip path
  - [x] tooltip suppression during drag where appropriate
- [x] Improve popup nesting and dismissal rules.
- [x] Add more robust menu keyboard navigation.
- [x] Ensure overlays cooperate with capture and focus rules from Milestones 1 and 2.

### Exit Criteria

- [x] Overlay behavior feels predictable across menus, popups, modals, and tooltips.

Milestone 12 is now complete for the shipped first-pass scope: `UiMenuBar` now exposes a real command path through `CommandId`, `Invoked`, `TryInvokeCommand(...)`, parsed shortcut dispatch, and keyboard navigation; `UiContextMenuRegion` provides reusable item-attached popup/menu helpers; `UiTooltipRegion` now supports delayed hover, focus-target tooltips, and drag suppression; popup/menu hit testing now respects open overlay content; and the Widget Gallery plus `OpenControls.Tests` both include explicit overlay-polish coverage.

## Milestone 13: Renderer, Text Backend, And Editor Readiness

### Why This Is Its Own Phase

The latest backend/editor-readiness audit changed the priority order. Widget breadth is no longer the main blocker; the remaining swap risk is now concentrated in renderer/text sophistication, host IME/composition contracts, persistence depth, and a few editor-facing API breadth gaps.

Milestone 13 is now complete for the shipped first-pass backend/editor-readiness target. The shipping renderers no longer walk glyph alpha buffers pixel-by-pixel; they share atlas-backed textured glyph draws, the core text-input contract now carries composition/preedit plus caret/candidate bounds, persistence now restores richer editor/view state through `IUiStatefulElement`, and the editor-facing picker/image story now has an explicit convenience surface through `UiPicker` plus a stabilized image-adapter contract.

### Checklist

- [x] Replace the current per-pixel glyph drawing hot path in the shipping renderers with a more scalable backend text path:
  - [x] keep Silk.NET as the reference rendering host first
  - [x] move toward atlas-backed textured glyph draws instead of pixel-by-pixel loops
  - [x] ensure measurement and draw metrics stay aligned across all hosts
  - [x] keep MonoGame, SDL, OpenGL, and headless/software paths aligned with the same contracts
- [x] Strengthen text backend maturity for editor use:
  - [x] validate custom-font and merged-icon rendering under the newer backend path
  - [x] tighten baseline / spacing / metric consistency where current approximations show through
  - [x] document the boundary between "usable Unicode rendering" and optional future shaping depth
- [x] Expand the host text-input / IME contract:
  - [x] composition / preedit text state
  - [x] committed-vs-composing input separation
  - [x] candidate window / composition bounds requests
  - [x] ship the broader host contract in core and provide a concrete native composition-rect reference path in the SDL example host while Silk.NET remains the parity validation host overall
- [x] Broaden persistence and session restore beyond the current thin baseline:
  - [x] text selection ranges
  - [x] scroll positions
  - [x] table view state
  - [x] combo/list selection state
  - [x] richer `UiTextEditor` state
  - [x] extension hooks for custom-control state
- [x] Decide and stabilize the editor-facing picker / image abstraction story:
  - [x] keep `UiComboBox` as the simple string-list convenience wrapper
  - [x] add a richer convenience picker surface for editor workflows if the existing composition path is still too low-level
  - [x] formalize image adapters as the intended renderer contract for now instead of adding a first-class renderer texture primitive in this milestone
- [x] Add regression coverage around the remaining backend/editor-readiness risks:
  - [x] renderer/text sanity checks where automation is practical
  - [x] persistence round-trips for richer state
  - [x] IME/host-input contract tests or harness coverage where feasible

### Exit Criteria

- [x] The remaining backend/editor-readiness risks are no longer per-pixel text rendering, ultra-thin IME request metadata, or ultra-narrow persistence fundamentals.

### Deferred Follow-On Work

- HarfBuzz shaping, bidi/RTL layout depth, and Slug evaluation remain follow-on text-system work rather than blockers for this milestone.
- SDL currently provides the concrete native composition/candidate-rect example path; bringing the same level of platform IME integration to Silk.NET, MonoGame, OpenGL, and other hosts remains host-specific follow-on work.
- If a future host needs a first-class renderer texture primitive beyond the stabilized image-adapter contract, that decision can be revisited with real integration pressure instead of preemptive API churn.

## Milestone 14: Tooling, Style Editor, And Examples Parity

### Why This Is Its Own Phase

Once the infrastructure and core widgets are stronger, the examples app can stop papering over missing behaviors and become a real showcase and regression tool.

### Checklist

- [ ] Replace simulated examples with real feature usage throughout `OpenControls.Examples`.
- [ ] Drive the examples backlog from `tools/parity/WIDGET_DEMO_COVERAGE.MD` until every public widget and meaningful option is either demonstrated live or explicitly documented as intentionally omitted.
- [ ] Add dedicated "widget gallery" coverage sections where needed so option discovery does not depend on reading source or guessing hidden interactions.
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
- [ ] Add repo-native regression coverage for editor-blocking workflows:
  - [ ] text field editing
  - [ ] text editor editing
  - [ ] tables and virtualization
  - [ ] dock split resizing
  - [ ] menu shortcut dispatch
- [ ] Add an editor-scale reference surface that proves OpenControls beyond toy/demo windows.
- [ ] Re-run parity review against the current reference snapshot.
- [ ] Update `README.MD`, `API.MD`, `Samples.MD`, and `Quickstart.MD` to reflect the new reality.

### Exit Criteria

- [ ] The examples app is a trustworthy, exhaustive widget gallery rather than a structure-matching demo tree.
- [ ] `tools/parity/WIDGET_DEMO_COVERAGE.MD` is either fully checked or contains explicit omission notes for anything intentionally not surfaced.
- [ ] The repo has a believable validation path for editor-scale workflows, not just isolated widget demos.

## Stretch Goals / Parking Lot

- [ ] Multi-viewport/windowing parity if OpenControls ever wants to chase that class of functionality.
- [ ] Gamepad-first navigation polish beyond current keyboard focus flows.
- [ ] More renderer features if future widgets require them.
- [ ] Accessibility-oriented abstractions if that becomes a product goal.
- [ ] Full style/theme system pass beyond widget-local properties.

## Recommended Delivery Order

If resourcing is tight, do the work in this order:

- [x] Phase 0
- [x] Milestone 1
- [x] Milestone 2
- [x] Milestone 3
- [x] Milestone 4
- [x] Milestone 5
- [x] Milestone 6
- [x] Milestone 7
- [x] Milestone 8
- [x] Milestone 9
- [x] Milestone 10
- [x] Milestone 11
- [x] Milestone 12
- [x] Milestone 13
- [ ] Milestone 14

## Suggested Release Slices

### Release Slice A: Infrastructure

- [x] Phase 0
- [x] Milestone 1
- [x] Milestone 2

### Release Slice B: Text And Editing

- [x] Milestone 3
- [x] Milestone 4
- [x] Milestone 5

### Release Slice C: Data Views

- [x] Milestone 8
- [x] Milestone 9
- [x] Milestone 10

### Release Slice D: Composition And Layout

- [x] Milestone 6
- [x] Milestone 7

### Release Slice E: Backend And Editor Readiness

- [x] Milestone 13

### Release Slice F: Polish And Showcase

- [x] Milestone 11
- [x] Milestone 12
- [ ] Milestone 14

## Final Notes

- [x] If only one thing happens next, it should be Milestone 1.
- [x] If only one thing happens next after Milestone 1, it should be Milestone 2.
- [x] If custom fonts and icon-font parity matter for the near-term product, Milestone 3 should happen immediately after Milestone 2.
- [x] If only one user-facing thing happened after the early foundations, it was correct to prioritize Milestone 5 and then Milestones 8-9 for editor/data-view parity.
- [x] Re-audit after Milestone 11 before committing to the rest of the roadmap tail.
- [x] That re-audit changed the next priority: backend/text maturity should land before tooling/showcase cleanup.
- [x] With Milestone 13 landed, the definitive next milestone is Milestone 14.
