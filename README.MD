# OpenControls

OpenControls is a lightweight, renderer-agnostic UI logic library. It keeps control behavior separate from rendering so host apps (MonoGame, tooling, or custom engines) can share the same UI logic.

## Quick Links
- `README.MD`: this overview.
- `Quickstart.MD`: getting started guide.
- `API.MD`: core types and controls reference.
- `Samples.MD`: small usage examples.
- `tools/parity/README.MD`: parity baseline, status board, and tracking notes.
- `tools/parity/WIDGET_DEMO_COVERAGE.MD`: exhaustive widget and option coverage checklist for `OpenControls.Examples`.
- `scripts/verify-phase0.sh`: lightweight Phase 0 parity verification hook.
- `LICENSE.MD`: MIT license.

## Goals
- Keep UI behavior and input handling independent from rendering APIs.
- Use a retained element tree so controls manage their own state.
- Provide a small, predictable control set for editor and runtime use.
- Keep layout explicit (manual bounds) so host apps stay in control.

## Screenshots

### Docked Layout
![Docked layout](assets/screenshots/docked_layout.png)

### Examples Overview
![Examples overview](assets/screenshots/examples_overview.png)

### Text Editor
![Text editor](assets/screenshots/text_editor.png)

## Projects
- `OpenControls.Examples`: shared examples UI library (renderer-agnostic).
- `OpenControls.MonoGame`: MonoGame DesktopGL renderer adapter library.
- `OpenControls.MonoGame.Examples`: runnable MonoGame host for the shared examples UI.
- `OpenControls.SdlNet`: SDL2-CS renderer adapter library.
- `OpenControls.SilkNet`: Silk.NET renderer adapter library (legacy OpenGL).
- `OpenControls.SilkNet.Examples`: runnable Silk.NET host for the shared examples UI. This is the primary parity/reference host.
- `OpenControls.OpenGL`: OpenGL renderer adapter library (OpenTK).
- `OpenControls.OpenGL.Examples`: runnable OpenGL host for the shared examples UI.
- `OpenControls.Tests`: regression tests for editor-blocking shared behavior.
- `OpenControls.SdlNet.Examples`: SDL2-CS renderer example that mirrors the OpenControls.Examples UI (requires SDL2 native runtime; on Windows place `SDL2.dll` on PATH or beside the executable).

## Usage
- Build the core library: `dotnet build OpenControls/OpenControls.csproj`
- Run the MonoGame examples: `dotnet run --project OpenControls.MonoGame.Examples/OpenControls.MonoGame.Examples.csproj`
- Run the OpenGL examples: `dotnet run --project OpenControls.OpenGL.Examples/OpenControls.OpenGL.Examples.csproj`
- Run the Silk.NET examples: `dotnet run --project OpenControls.SilkNet.Examples/OpenControls.SilkNet.Examples.csproj`
- Run the shared regression tests: `dotnet test OpenControls.Tests/OpenControls.Tests.csproj`
- Run the current Phase 0 verification hook: `bash scripts/verify-phase0.sh`

## Demo Parity (OpenControls.Examples)
The Widgets window in `OpenControls.Examples` mirrors the structure of the reference demo source. Every demo marker was replaced with a working OpenControls demo panel so the tree now maps to the same sections (Help & Configuration, Menu, Basic widgets, Text, Sliders, Drag & Drop, Tables, Layout, Inputs & Focus, Tools, Examples, etc.).

The Widgets layout now also docks a dedicated `Widget Gallery` window beside the original `Widgets` tree. That gallery is where the option-level gap-filling lives for controls that were previously easy to miss, including `UiArrowButton`, `UiSeparatorText`, `UiBullet`, `UiInputDouble`, `UiNumericField`, `UiNumericVector`, `UiSliderVector`, empty-state list/combo/list-view paths, a dedicated Tables 2.0 demo (resize/reorder/hide/scroll/custom headers/rich cells), explicit `UiScrollPanel`, weighted/spanning `UiGrid`, and retained layout alignment variants.

The `Clipping` window is now the large-data proof surface for Milestone 9. It still includes the original clipped/resizable panel demo, and now also shows a 4,000-row `UiListBox`, a 6,000-row multi-select list, a 2,500-row `UiTable`, and an 8,040-row `UiTreeView`, all driven by the shared `UiClipper` / `UiClipRange` fixed-height virtualization layer with live visible-range status readouts.

Exhaustive demo coverage is now a tracked requirement, not an informal goal:
- `tools/parity/WIDGET_DEMO_COVERAGE.MD` is the canonical checklist for which widgets, options, modes, and interaction paths are visibly demonstrated in `OpenControls.Examples`.
- "Implemented but not shown" does not count as demo-complete.
- New public widgets or meaningful widget options should update the matrix in the same change that introduces them.

Parity deviations (documented and intentional):
- Shortcut routing depth: `UiContext` now exposes scoped shortcut helpers with focused/hovered/global precedence, but `UiMenuBar.MenuItem.Shortcut` remains display-only and app command binding is still app-defined.
- Drag/slider flags: the demo toggles whole-number snapping and step size; advanced flags like logarithmic and always-clamp are not available.
- Multiline editor depth: `UiTextEditor` now shares the same editing core as `UiTextField`, including selection, clipboard, undo/redo, word/page/home/end navigation, and real example coverage. Deferred follow-on work is now editor polish depth such as wrapping, tokenizer expansion, and richer IME/composition handling.
- WantCapture overrides: `UiContext` now exposes actual `WantCaptureMouse`, `WantCaptureKeyboard`, and `WantTextInput` outputs, but manual override toggles are not part of the core API.
- Image demos: `UiImage` and `UiImageButton` rely on renderer-supplied textures, so hosts without texture bindings will show placeholders.

## Parity Tracking
Phase 0 parity tracking now lives in checked-in markdown files:
- `tools/parity/README.MD`: entry point for parity tracking.
- `tools/parity/BASELINE.MD`: frozen Dear ImGui target, scope buckets, and verification notes.
- `tools/parity/STATUS.MD`: current status board that maps major parity areas to roadmap milestones.
- `tools/parity/WIDGET_DEMO_COVERAGE.MD`: exhaustive example-app coverage matrix for public widgets and options.
- `IMGUI_PARITY_ATTACK_PLAN.MD`: execution roadmap and milestone checklists.

Current Phase 0 workflow:
1. Review `tools/parity/BASELINE.MD` before changing what "parity" means.
2. Update `tools/parity/STATUS.MD` when priorities or milestone ordering change.
3. Update `tools/parity/WIDGET_DEMO_COVERAGE.MD` whenever widget surface area, example coverage, or intentional omissions change.
4. Refresh `README.MD` whenever a milestone lands, parity caveats change, or the recommended next milestone changes.
5. If no milestone has landed recently, do a quick README truthfulness pass before starting the next major parity phase so the top-level docs do not drift.
6. Run `bash scripts/verify-phase0.sh` to verify the baseline files exist and the core/shared-examples projects still build.
7. Use `IMGUI_PARITY_ATTACK_PLAN.MD` as the working implementation roadmap.

Documentation cadence:
- Treat the top-level `README.MD` as the public truth source for current parity, reference host guidance, and major caveats.
- Update `README.MD`, `tools/parity/STATUS.MD`, `tools/parity/WIDGET_DEMO_COVERAGE.MD`, and any affected API/quickstart docs in the same pass when milestone status changes or demo coverage meaningfully changes.
- Do not let the roadmap become more current than the README for shipped behavior.

Historical note:
- Earlier README text referenced extracted `widgets/` sources, demo marker artifacts, and PowerShell parity scripts.
- Those assets and scripts are not checked into this repo snapshot, so Phase 0 replaces them with a truthful baseline and status board.

## Project Status Matrix
| Project | Type | Status | Notes |
| --- | --- | --- | --- |
| `OpenControls` | Core UI library | Active | Renderer-agnostic UI logic. |
| `OpenControls.Examples` | Shared UI | Active | Shared examples UI used by hosts. |
| `OpenControls.MonoGame` | Renderer adapter | Active | MonoGame DesktopGL adapter. |
| `OpenControls.MonoGame.Examples` | Host app | Active | MonoGame examples host. |
| `OpenControls.SdlNet` | Renderer adapter | Active | SDL2-CS adapter. |
| `OpenControls.SdlNet.Examples` | Host app | Active | Requires `SDL2.dll` at runtime. |
| `OpenControls.SilkNet` | Renderer adapter | Active | Silk.NET legacy OpenGL adapter. |
| `OpenControls.SilkNet.Examples` | Host app | Active | Silk.NET windowing/input host. |
| `OpenControls.OpenGL` | Renderer adapter | Active | OpenTK compatibility-profile adapter. |
| `OpenControls.OpenGL.Examples` | Host app | Active | OpenTK OpenGL host. |
| `OpenControls.Tests` | Test project | Active | Regression coverage for shared editor-blocking primitives. |

## Supported Widgets
- Core: `UiPanel`, `UiGroup`, `UiLabel`, `UiTextBlock`, `UiTextLink`, `UiBullet`, `UiBulletText`, `UiButton`, `UiArrowButton`, `UiSeparator`, `UiSeparatorText`, `UiSpacer`, `UiDisabledGroup`.
- Inputs: `UiCheckbox`, `UiRadioButton`, `UiTextField`, `UiTextEditor`, `UiInputFloat/Int/Double`, `UiInputFloat2/3/4`, `UiInputInt2/3/4`, `UiNumericField`, `UiNumericVector`, `UiSlider`, `UiSliderFloat2/3/4`, `UiSliderInt2/3/4`, `UiSliderVector`, `UiVSlider`, `UiDragFloat`, `UiDragInt`, `UiDragFloat2/3/4`, `UiDragInt2/3/4`, `UiDragFloatRange`, `UiDragIntRange`.
- Lists and tables: `UiListBox`, `UiSelectable`, `UiComboBox`, `UiTable`, `UiTreeView`.
- Data display: `UiProgressBar` (horizontal/vertical, segmented, radial), `UiPlotPanel`, `UiWaveform`, `UiColorEdit`, `UiColorPicker`, `UiColorButton`, `UiImage`, `UiImageButton`.
- Layout: `UiStack`, `UiWrapPanel`, `UiGrid`, `UiSplitter`, `UiScrollPanel`, `UiCanvas`, `UiWindow`, `UiDockHost`, `UiDockWorkspace`.
- Interaction: `UiTabBar`, `UiTabItem`, `UiTabItemButton`, `UiDragDropSource`, `UiDragDropTarget`.
- Overlays: `UiMenuBar`, `UiTreeNode`, `UiCollapsingHeader`, `UiTooltip`, `UiTooltipRegion`, `UiPopup`, `UiModal`, `UiModalHost`.

## Renderers
OpenControls is renderer-agnostic; hosts provide an `IUiRenderer` implementation.

Current adapters:
- MonoGame DesktopGL (OpenControls.MonoGame).
- SDL2-CS (OpenControls.SdlNet).
- Silk.NET legacy OpenGL (OpenControls.SilkNet).
- OpenGL (OpenControls.OpenGL).

Planned expansion:
- OpenGL or other native renderers.
- Headless test harnesses.

## Status
- Core element tree, focus management, and basic controls are implemented.
- `UiInputState` now supports left/right/middle mouse buttons, Alt/Super modifiers, named keys, and per-key down/pressed/released state.
- `UiContext` now exposes `Hovered`, `PointerCaptureTarget`, `WantCaptureMouse`, `WantCaptureKeyboard`, `WantTextInput`, and `RequestedMouseCursor`, and the example hosts consume those outputs.
- `UiContext` now also exposes per-frame item/container query snapshots (`LastItemState`, hovered/focused item, containing container, active input layer), visible-bounds helpers, and stronger focus APIs such as focus-by-id and next-frame focus requests.
- `UiContext` now also exposes `Clipboard`, with a default in-memory implementation plus host-swappable clipboard adapters for copy/cut/paste-aware controls.
- `UiInputState` also carries double-click state, drag origins/thresholds, and horizontal wheel deltas for richer host input plumbing.
- The shipped example and editor hosts now generate held-key repeat for repeatable navigation/editing keys like arrows, backspace/delete, home/end, and page up/down.
- Shared fixed-height virtualization now lives in `UiClipper` / `UiClipRange`, and the shipped examples use it for large `UiListBox`, `UiTable`, and `UiTreeView` datasets in the `Clipping` window.
- Checkbox, radio, slider, drag (scalar/vector/range with log scaling), progress bar (horizontal/vertical, segmented, radial), list box, combo box, selectable, color button, and separator controls are available.
- Table controls now include runtime column/view state, row selection, resize/reorder/hide, X/Y scrolling, row clipping, row-height hooks, per-cell styling hooks, and richer header/cell content via `UiTableCell`, `UiTableColumnState`, and `UiTableViewState`.
- Table headers support angled labels, sortable columns via `UiTable.SortSpecs`, and built-in header context menus for hide/show/reset workflows.
- Plotting panel control supports axes, tick labels, grid lines, and zoom/pan input.
- Waveform control renders sample buffers with min/max or line modes and an optional zero line.
- Grid layout control assigns children to rows/columns with spacing and padding.
- Canvas control supports pan/zoom with optional grid and origin helpers.
- Color edit and color picker controls include gradient SV, alpha bars, and checkerboard transparency.
- Multi-select selection model for list boxes and selectables (Ctrl/Shift range) is available.
- Scroll panels support auto/always scrollbars for overflowing content.
- `UiStack`, `UiWrapPanel`, `UiGroup`, `UiThickness`, and `UiLayoutLength` now provide retained row/column/wrap/group layout primitives with padding, gap, baseline alignment, and fixed/fill/percentage sizing.
- `UiCombo`, `UiListView`, `UiSelectableRow`, `UiChildRegion`, and popup open helpers now power the composed combo/list/popup demo in `OpenControls.Examples`, and `UiComboBox` remains as a string-list convenience wrapper over that richer combo path.
- `IUiImageSource` plus `UiDelegateImageSource` provide a reusable adapter path for repeated thumbnail/icon drawing inside rich rows and popup content.
- `UiChildRegion` is the embedded, clipped, scrollable viewport primitive for content inside an existing window, panel, or popup; use `UiWindow` when you need independent title/chrome/focus behavior instead of an in-place framed region.
- Windows can host optional scroll panels for overflow content (shown in docked/floating examples).
- Window and dock host controls support tabbed layouts with close buttons and overflow scrolling.
- Tab bars support `UiTabItemButton` with leading/trailing placement.
- Tabs, window titles, table headers, tree node headers, and labels can render bold text.
- Dock workspaces support drag previews, drop targets, floating windows, and draggable persisted splitters with minimum pane clamping.
- Drag/drop payloads are supported via `UiDragDropSource`, `UiDragDropTarget`, and `UiDragDropContext`.
- Menu bar control supports dropdowns, checkable items, nested submenus, popup menus, embedded content, and attached/context opening helpers for popup-mode composition.
- Tree nodes, `UiTreeView`, collapsing headers, tooltips, popups, and modals are available.
- Text editor control supports line numbers, scrolling, and syntax highlighting.
- Text fields now support selection, copy/cut/paste, undo/redo, commit/cancel hooks, password/read-only/filtering modes, completion/history callbacks, resize callbacks, and placeholder text.
- Numeric input fields (`UiInputFloat/Int/Double`, `UiNumericField`, and vector variants) and vector sliders (`UiSliderFloat2/3/4`, `UiSliderInt2/3/4`, `UiSliderVector`) are available.
- Image widgets (`UiImage`, `UiImageButton`) support renderer-specific draw callbacks, and can also consume reusable image-source adapters for texture-backed UI.
- Splitter control provides draggable horizontal/vertical splits for manual layouts.
- Arrow glyph helper (`UiArrow`) keeps tree/combo/menu indicators consistent.
- Common controls support rounded corners and masking for clipped children via `CornerRadius` and `UiRenderHelpers` helpers.
- Panels and windows support child clipping and resize grips.
- UI state serialization is available for layout persistence.
- OpenControls.MonoGame.Examples renders the shared examples UI using MonoGame DesktopGL (via OpenControls.MonoGame).
- OpenControls.SdlNet.Examples renders the shared examples UI using SDL2-CS (via OpenControls.SdlNet).
- OpenControls.SilkNet provides a Silk.NET-based renderer adapter using legacy OpenGL bindings.
- OpenControls.SilkNet.Examples renders the shared examples UI using Silk.NET windowing/input plus the Silk.NET renderer adapter.
- OpenControls.OpenGL provides an OpenTK-based renderer adapter (compatibility profile).
- OpenControls.OpenGL.Examples renders the shared examples UI using OpenGL (via OpenControls.OpenGL).
- OpenControls.Examples includes an Examples menu to open feature windows as dockable panels.
- OpenControls.Examples includes a dedicated Text Editor demo window.
- OpenControls.Examples widgets window groups controls into categorized tree nodes (collapsed by default, with header spacing).
- OpenControls.Examples now shows real shortcut, capture, and requested-cursor state in the Inputs & Focus demo.
- OpenControls.Examples now includes a custom-font and merged-icon demo in the Widgets text section, with Silk.NET as the reference parity host.
- OpenControls.Examples now also includes a dedicated `Widget Gallery` window for the option-level coverage pass that complements the original `Widgets` tree, `Text Editor`, `Docking`, and `Clipping` windows.
- A dedicated UI editor is planned and will be documented once available.
- OpenControls includes a TinyBitmapFont helper that covers full printable ASCII plus Latin-1/CP437 code pages (shown in the Widgets ASCII table).
- OpenControls now ships `UiFont`, `UiFontBuilder`, `UiFontRegistry`, per-element/inherited font selection, custom font-file loading, fallback chains, and merged icon-font ranges for Dear ImGui-style inline icon text.
- The shipped renderer path currently uses shared grayscale glyph caches across MonoGame, SDL2-CS, Silk.NET, and OpenGL; optional HarfBuzz shaping and optional Slug GPU text rendering remain follow-on work in the parity roadmap.
