#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"

required_files=(
  "IMGUI_PARITY_ATTACK_PLAN.MD"
  "tools/parity/README.MD"
  "tools/parity/BASELINE.MD"
  "tools/parity/STATUS.MD"
)

for path in "${required_files[@]}"; do
  if [[ ! -f "$path" ]]; then
    echo "Missing required Phase 0 file: $path" >&2
    exit 1
  fi
done

echo "Phase 0 tracking files: OK"

dotnet build OpenControls/OpenControls.csproj
dotnet build OpenControls.Examples/OpenControls.Examples.csproj

echo "Phase 0 verification complete."
