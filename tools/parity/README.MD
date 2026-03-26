# Parity Tracking

This directory is the Phase 0 home for OpenControls vs Dear ImGui parity work.

## Files

- `BASELINE.MD`: frozen comparison target, scope buckets, verification hooks, and explicit non-goals.
- `STATUS.MD`: current status board that maps major parity areas to roadmap milestones.

## Workflow

1. Review `BASELINE.MD` before changing what "parity" means.
2. Update `STATUS.MD` when priorities or milestone ownership change.
3. Use `IMGUI_PARITY_ATTACK_PLAN.MD` as the execution roadmap.
4. Run `bash scripts/verify-phase0.sh` after changing Phase 0 tracking or parity docs.

## Notes

- The older README references to extracted `widgets/` sources, demo marker artifacts, and PowerShell parity scripts do not match this repo snapshot.
- Phase 0 replaces those stale references with a truthful, checked-in baseline and status board.
