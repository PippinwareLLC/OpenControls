# Parity Tracking

This directory is the Phase 0 home for OpenControls capability and parity tracking.

## Files

- `BASELINE.MD`: frozen comparison target, scope buckets, verification hooks, and explicit non-goals.
- `STATUS.MD`: current status board that maps major parity areas to roadmap milestones.
- `WIDGET_DEMO_COVERAGE.MD`: canonical checklist for exhaustive shared example coverage across public widgets, options, and interaction paths.

## Workflow

1. Review `BASELINE.MD` before changing what "parity" means.
2. Update `STATUS.MD` when priorities or milestone ownership change.
3. Update `WIDGET_DEMO_COVERAGE.MD` when public widget surface area, example coverage, or intentional demo omissions change.
4. Use `PARITY_EXECUTION_PLAN.MD` as the execution roadmap.
5. Run `bash scripts/verify-phase0.sh` after changing Phase 0 tracking or parity docs.

## Notes

- The older README references to extracted `widgets/` sources, demo marker artifacts, and PowerShell parity scripts do not match this repo snapshot.
- Phase 0 replaces those stale references with a truthful, checked-in baseline and status board.
- Treat `WIDGET_DEMO_COVERAGE.MD` as the source of truth for whether `OpenControls.Examples` is a sampler or a genuinely exhaustive widget gallery.
