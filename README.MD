# OpenControls

OpenControls is a lightweight UI logic library designed to keep control behavior separate from rendering. It provides a small set of primitives and controls that can be hosted by any renderer (MonoGame, tooling, or custom engines).

## Goals
- Keep UI behavior and input handling independent from rendering APIs.
- Use a retained element tree so controls manage their own state.
- Provide a small, predictable control set for editor and runtime use.
- Keep layout explicit (manual bounds) so host apps stay in control.

## Contents
- `Quickstart.MD`: getting started guide.
- `API.MD`: core types and controls reference.
- `Samples.MD`: small usage examples.
- `OpenControls.Examples`: runnable demo project exercising all controls.

## Status
- Core element tree, focus management, and basic controls are implemented.
- Checkbox, radio, slider, drag (scalar/vector/range with log scaling), progress bar, list box, combo box, selectable, color button, and separator controls are available.
- Table controls with column headers and selectable rows are available.
- Plotting panel control supports axes, tick labels, grid lines, and zoom/pan input.
- Grid layout control assigns children to rows/columns with spacing and padding.
- Canvas control supports pan/zoom with optional grid and origin helpers.
- Color edit and color picker controls include gradient SV, alpha bars, and checkerboard transparency.
- Multi-select selection model for list boxes and selectables (Ctrl/Shift range) is available.
- Scroll panels support auto/always scrollbars for overflowing content.
- Windows can host optional scroll panels for overflow content (shown in docked/floating examples).
- Window and dock host controls support tabbed layouts.
- Dock workspaces support drag previews, drop targets, and floating windows.
- Menu bar control supports dropdowns, checkable items, nested submenus, popup menus, and embedded content.
- Tree nodes, collapsing headers, tooltips, popups, and modals are available.
- Splitter control provides draggable horizontal/vertical splits for manual layouts.
- Arrow glyph helper (`UiArrow`) keeps tree/combo/menu indicators consistent.
- Common controls support rounded corners and masking for clipped children via `CornerRadius` and `UiRenderHelpers` helpers.
- Panels and windows support child clipping and resize grips.
- UI state serialization is available for layout persistence.
- OpenControls.Examples renders a live UI using MonoGame DesktopGL.
- OpenControls.Examples includes an Examples menu to open feature windows as dockable panels.
- OpenControls.Examples widgets window groups controls into categorized tree nodes (collapsed by default, with header spacing).
- The editor uses OpenControls via a MonoGame renderer adapter.
- OpenControls includes a TinyBitmapFont helper that covers full printable ASCII plus Latin-1/CP437 code pages (shown in the Widgets ASCII table).
