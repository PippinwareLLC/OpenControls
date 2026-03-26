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
- [x] Text rendering supports custom fonts, Unicode/icon glyph ranges, and merged icon fonts instead of only the built-in bitmap path.
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
- [ ] Editor-blocking behavior has regression coverage beyond "project builds".
- [ ] README parity caveats are updated if the milestone changes them.

## Milestone Status Semantics

- A milestone is only called complete when its required checklist items and exit criteria are fully checked.
- Mutually exclusive design decisions should be recorded as a chosen baseline plus deferred alternatives, not as permanently half-checked checkbox groups.
- Optional or post-baseline work should live in a clearly named deferred follow-on subsection so it does not make a shipped milestone read as incomplete.

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

Dear ImGui's power comes partly from easy access to item and window state. OpenControls needs a retained-mode equivalent so complex widgets and examples stop faking status with ad hoc hit tests.

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

OpenControls currently assumes a tiny built-in bitmap font and a narrow code-page model. To reach real tooling parity, the framework needs a font asset pipeline that can load custom fonts, handle broader Unicode/code point ranges, and merge icon fonts like Font Awesome into the same text flow the way Dear ImGui does.

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

### ImGui-Style Icon Font Parity Checklist

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
- [x] Add a Silk.NET-first sample showing Font Awesome-style usage.
- [x] Update `README.MD`, `API.MD`, and `Quickstart.MD` with font-loading guidance.

### Exit Criteria

- [x] A user can load a custom TTF/OTF/WOFF2 font and use it throughout the UI.
- [x] A user can merge an icon font range into a text font and render icons inline, Dear ImGui style.
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

OpenControls has useful concrete widgets, but Dear ImGui's flexibility often comes from container-style composition APIs. In retained mode, this should become reusable retained containers, not just more string-only controls.

### Combo/List/Popup Checklist

- [ ] Introduce a retained combo container that can host arbitrary child controls, not only strings.
- [ ] Keep `UiComboBox` as a convenience wrapper over the richer container if possible.
- [ ] Introduce a retained list-box container for arbitrary child content.
- [ ] Support typed-search / filterable combo patterns without bespoke one-off widgets.
- [ ] Support richer rows in combo/list content:
  - [ ] icons/thumbnails
  - [ ] secondary text
  - [ ] asset-picker style rows
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
- [ ] Standardize image/texture interop for composed content:
  - [ ] renderer-level texture/image primitive or a stable adapter contract
  - [ ] thumbnails/icons in combo/list/table rows without ad hoc callback wiring every time
  - [ ] examples that prove asset/browser-style usage

### Exit Criteria

- [ ] OpenControls can express richer combos, list boxes, and popup content without building custom one-off widgets every time.

## Milestone 7: Layout System Upgrade

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

### Docking / Split Pane Checklist

- [ ] Add draggable dock splitters.
- [ ] Update split ratios from divider hit-testing and drag, not only from initial split creation.
- [ ] Add minimum pane sizes / clamp rules so resize feels editor-safe.
- [ ] Persist dock split ratios as part of workspace/session state.
- [ ] Add visual splitter affordances and hover/active states.
- [ ] Verify nested split resizing behaves predictably.

### Explicit Non-Goal

- [ ] Do not force OpenControls into cursor-based immediate layout semantics unless a tiny compatibility helper is clearly worth it.

### Exit Criteria

- [ ] The major "Layout" demo sections have natural retained-mode equivalents.
- [ ] Example UI code gets shorter, not longer.

## Milestone 8: Tables 2.0

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
- [ ] Add explicit row-scroll/view state rather than truncating to the currently visible prefix.
- [ ] Add fixed vs stretch width modes.
- [ ] Add row height customization hooks.
- [ ] Add per-cell background / foreground customization hooks.
- [ ] Add a richer cell content model:
  - [ ] text-only path stays simple
  - [ ] child-element / retained-content path where feasible
  - [ ] non-string cell content suitable for inspectors, hierarchies, and asset browsers
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

## Milestone 9: Shared Virtualization / Clipper Layer

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

## Milestone 10: Selection And Trees

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

## Milestone 11: Drag, Slider, Numeric, And Color Depth

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

## Milestone 12: Menus, Popups, Tooltips, And Overlay Polish

### Checklist

- [ ] Add item-attached context menu helpers.
- [ ] Add a real menu command model instead of shortcut labels being display-only.
- [ ] Parse and dispatch menu shortcuts through the shared shortcut routing from Milestone 1.
- [ ] Support menu items that can be invoked consistently from:
  - [ ] mouse activation
  - [ ] keyboard shortcut activation
  - [ ] programmatic command activation
- [ ] Add better tooltip trigger policies:
  - [ ] delayed hover
  - [ ] keyboard-focus tooltip path
  - [ ] tooltip suppression during drag where appropriate
- [ ] Improve popup nesting and dismissal rules.
- [ ] Add more robust menu keyboard navigation.
- [ ] Ensure overlays cooperate with capture and focus rules from Milestones 1 and 2.

### Exit Criteria

- [ ] Overlay behavior feels predictable across menus, popups, modals, and tooltips.

## Milestone 13: Tooling, Style Editor, And Examples Parity

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
- [ ] Broaden state persistence and session recovery:
  - [ ] `UiTextEditor` state
  - [ ] combo/list selection state
  - [ ] generic scroll positions
  - [ ] table view state
  - [ ] extension hooks for custom-control state
- [ ] Add repo-native regression coverage for editor-blocking workflows:
  - [ ] text field editing
  - [ ] text editor editing
  - [ ] tables and virtualization
  - [ ] dock split resizing
  - [ ] menu shortcut dispatch
- [ ] Add an editor-scale reference surface that proves OpenControls beyond toy/demo windows.
- [ ] Re-run parity review against the target Dear ImGui snapshot.
- [ ] Update `README.MD`, `API.MD`, `Samples.MD`, and `Quickstart.MD` to reflect the new reality.

### Exit Criteria

- [ ] The examples app is a trustworthy parity showcase, not just a structure-matching demo tree.
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
- [ ] Milestone 8
- [ ] Milestone 9
- [ ] Milestone 6
- [ ] Milestone 7
- [ ] Milestone 10
- [ ] Milestone 11
- [ ] Milestone 12
- [ ] Milestone 13
- [ ] Milestone 5 when editor depth becomes important enough to justify the extra scope

## Suggested Release Slices

### Release Slice A: Infrastructure

- [x] Phase 0
- [x] Milestone 1
- [x] Milestone 2

### Release Slice B: Text And Editing

- [x] Milestone 3
- [x] Milestone 4
- [ ] Milestone 5

### Release Slice C: Data Views

- [ ] Milestone 8
- [ ] Milestone 9
- [ ] Milestone 10

### Release Slice D: Composition And Layout

- [ ] Milestone 6
- [ ] Milestone 7

### Release Slice E: Polish And Showcase

- [ ] Milestone 11
- [ ] Milestone 12
- [ ] Milestone 13

## Final Notes

- [x] If only one thing happens next, it should be Milestone 1.
- [x] If only one thing happens next after Milestone 1, it should be Milestone 2.
- [x] If custom fonts and icon-font parity matter for the near-term product, Milestone 3 should happen immediately after Milestone 2.
- [ ] If only one user-facing thing happens after that, it should be Milestone 5 or Milestone 8 depending on whether your near-term product is more editor-heavy or data-view-heavy.
- [ ] Re-audit after Milestone 5 and again after Milestone 8 before committing to the entire tail of the roadmap.
