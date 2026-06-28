#!/usr/bin/env bash
set -euo pipefail

repo_url="${1:-https://github.com/allibell/AllibellCodexArcticCompat.git}"
repo_dir="${ALLIBELL_CODEX_ARCTIC_COMPAT_DIR:-$HOME/dev/AllibellCodexArcticCompat}"
rimworld_mods_dir="${RIMWORLD_MODS_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods}"
link_path="$rimworld_mods_dir/AllibellCodexArcticCompat"

if ! command -v git >/dev/null 2>&1; then
  echo "git is required. Install Git or Xcode Command Line Tools first." >&2
  exit 1
fi

if [ -d "$repo_dir/.git" ]; then
  git -C "$repo_dir" pull --ff-only
else
  mkdir -p "$(dirname "$repo_dir")"
  git clone "$repo_url" "$repo_dir"
fi

mkdir -p "$rimworld_mods_dir"

if [ -L "$link_path" ]; then
  current_target="$(readlink "$link_path")"
  if [ "$current_target" != "$repo_dir" ]; then
    rm "$link_path"
    ln -s "$repo_dir" "$link_path"
  fi
elif [ -e "$link_path" ]; then
  echo "A non-symlink mod already exists at:" >&2
  echo "$link_path" >&2
  echo "Move or remove it, then rerun this script." >&2
  exit 1
else
  ln -s "$repo_dir" "$link_path"
fi

echo "AllibellCodex Arctic Compatibility is installed/updated:"
echo "$link_path -> $repo_dir"
