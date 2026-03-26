# Parity Baseline

This file freezes the current Dear ImGui comparison target and Phase 0 scoping decisions.

## Frozen Target

| Field | Value |
| --- | --- |
| Capture date | `2026-03-25` |
| Dear ImGui version string | `1.92.7 WIP` |
| Dear ImGui version number | `19267` |
| Dear ImGui branch | `master` |
| Dear ImGui commit | `b62cf3894bc971a1aa8a8bb4006bd502491a06a0` |
| Dear ImGui commit date | `2026-03-25T21:00:14Z` |

## Official Sources Used For This Baseline

- `https://raw.githubusercontent.com/ocornut/imgui/master/imgui.h`
- `https://raw.githubusercontent.com/ocornut/imgui/master/imgui_demo.cpp`
- `https://raw.githubusercontent.com/ocornut/imgui/master/docs/FAQ.md`
- `https://api.github.com/repos/ocornut/imgui/commits/master`

## Scope Buckets

## Must Have For Tool-Building

- Input platform contract: richer keys, modifiers, pointer input, shortcut routing, capture flags, and cursor requests.
- Runtime state queries: item status, window status, focus helpers, and related context outputs.
- Text entry foundation: selection, clipboard, undo/redo, password mode, callback hooks, and stronger numeric-field integration.
- Tables 2.0: resize, reorder, hide/show, scrolling, better cell rendering, and large-row handling.
- Retained composition containers for combo, list, popup, and child content.
- Retained layout containers instead of immediate-mode cursor APIs.
- Shared virtualization/clipper support for large lists, tables, and trees.

## High-Value Parity

- Text editor depth beyond the current basic editor widget.
- Selection and tree upgrades, especially large-data and multi-scope workflows.
- Drag/slider flag depth and click-to-input semantics.
- Color widget depth: more display modes, picker options, and shared settings.
- Overlay polish for menus, popups, tooltips, and keyboard navigation.
- Style editor and examples/tooling cleanup once the underlying systems are real.

## Low-Priority Demo Completeness

- Legacy columns API parity.
- Exact API/UX emulation of immediate-mode layout helpers such as `SameLine()`.
- Demo-only curiosities that do not materially improve OpenControls as a retained-mode toolkit.
- One-off parity work that adds maintenance cost without unlocking common editor/runtime use cases.

## Explicit Non-Goals For Now

- Converting OpenControls into an immediate-mode API.
- Reintroducing historical parity assets or scripts unless they are checked in and maintained for real.
- Chasing every Dear ImGui demo marker before the shared foundations exist.

## Lightweight Verification Hooks

Primary Phase 0 command:

```bash
bash scripts/verify-phase0.sh
```

What it currently verifies:

- Phase 0 parity tracking files exist.
- The core library builds.
- The shared examples library builds.

## Recommended Next Step

Start Milestone 1 from `IMGUI_PARITY_ATTACK_PLAN.MD`:

- expand `UiInputState`
- add `UiContext` capture/cursor outputs
- wire at least one host and one examples path end-to-end
