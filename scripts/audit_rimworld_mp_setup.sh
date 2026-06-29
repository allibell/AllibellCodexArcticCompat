#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
expected_list="$repo_root/docs/exact-active-mods-1.5.txt"
workshop_dir="${RIMWORLD_WORKSHOP_DIR:-$HOME/Library/Application Support/Steam/steamapps/workshop/content/294100}"
rimworld_app_mods="${RIMWORLD_MODS_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods}"
rimworld_user_dir="${RIMWORLD_USER_DIR:-$HOME/Library/Application Support/RimWorld}"
mods_config="$rimworld_user_dir/Config/ModsConfig.xml"

failures=0

fail() {
  echo "FAIL: $*"
  failures=$((failures + 1))
}

ok() {
  echo "OK: $*"
}

expected_active="$(mktemp)"
current_active="$(mktemp)"
installed_mods="$(mktemp)"
trap 'rm -f "$expected_active" "$current_active" "$installed_mods"' EXIT

awk '
  /^[[:space:]]*[0-9]+[.]/ {
    line = $0
    sub(/^[[:space:]]*[0-9]+[.][[:space:]]*/, "", line)
    split(line, parts, "|")
    gsub(/[[:space:]]+$/, "", parts[1])
    print tolower(parts[1])
  }
' "$expected_list" > "$expected_active"

if [[ ! -f "$mods_config" ]]; then
  fail "ModsConfig.xml not found: $mods_config"
else
  sed -n '/<activeMods>/,/<\/activeMods>/s:.*<li>\(.*\)</li>.*:\1:p' "$mods_config" \
    | tr '[:upper:]' '[:lower:]' > "$current_active"

  if cmp -s "$expected_active" "$current_active"; then
    ok "active mod package order matches docs/exact-active-mods-1.5.txt"
  else
    fail "active mod package order differs from docs/exact-active-mods-1.5.txt"
    diff -u "$expected_active" "$current_active" || true
  fi

  active_dupes="$(sort "$current_active" | uniq -d)"
  if [[ -z "$active_dupes" ]]; then
    ok "no duplicate active package IDs"
  else
    fail "duplicate active package IDs"
    echo "$active_dupes" | sed 's/^/  - /'
  fi
fi

if [[ ! -d "$workshop_dir" ]]; then
  fail "Workshop directory not found: $workshop_dir"
fi

if [[ ! -d "$rimworld_app_mods" ]]; then
  fail "RimWorld app Mods directory not found: $rimworld_app_mods"
fi

{
find "$workshop_dir" -maxdepth 3 -path '*/About/About.xml' -print 2>/dev/null
find -L "$rimworld_app_mods" -maxdepth 3 -path '*/About/About.xml' -print 2>/dev/null
} | while IFS= read -r about; do
  mod_dir="${about%/About/About.xml}"
  metadata="$(perl -0pe 's:<modDependencies>.*?</modDependencies>::gs; s:<loadBefore>.*?</loadBefore>::gs; s:<loadAfter>.*?</loadAfter>::gs; s:<incompatibleWith>.*?</incompatibleWith>::gs' "$about")"
  package_id="$(printf '%s' "$metadata" | sed -n 's:.*<packageId>\(.*\)</packageId>.*:\1:p' | head -1)"
  name="$(printf '%s' "$metadata" | sed -n 's:.*<name>\(.*\)</name>.*:\1:p' | head -1)"
  [[ -z "$package_id" ]] && continue
  if [[ "$mod_dir" == "$workshop_dir/"* ]]; then
    published_id="$(basename "$mod_dir")"
  else
    published_id="Local"
  fi
  printf '%s\t%s\t%s\t%s\n' "$(printf '%s' "$package_id" | tr '[:upper:]' '[:lower:]')" "$published_id" "${name:-unknown}" "$mod_dir"
done | sort > "$installed_mods"

if [[ -f "$current_active" ]]; then
  missing="$(comm -23 <(grep -v '^ludeon[.]rimworld' "$current_active" | sort) <(cut -f1 "$installed_mods" | sort -u))"
  if [[ -z "$missing" ]]; then
    ok "all active package IDs are installed in Workshop or local Mods"
  else
    fail "active package IDs missing from installed mods"
    echo "$missing" | sed 's/^/  - /'
  fi

  active_installed_dupes="$(comm -12 <(sort "$current_active") <(cut -f1 "$installed_mods" | sort | uniq -d))"
  if [[ -z "$active_installed_dupes" ]]; then
    ok "no duplicate installed package IDs for active mods"
  else
    fail "duplicate installed package IDs for active mods"
    while IFS= read -r package_id; do
      echo "  - $package_id"
      awk -F '\t' -v id="$package_id" '$1 == id { print "    " $2 " | " $3 " | " $4 }' "$installed_mods"
    done <<< "$active_installed_dupes"
  fi
fi

check_exact_source() {
  local package_id="$1"
  local expected_source="$2"
  local label="$3"
  local matches
  matches="$(awk -F '\t' -v id="$package_id" '$1 == id { print $2 }' "$installed_mods" | sort -u | tr '\n' ' ')"
  if [[ "$matches" == "$expected_source " ]]; then
    ok "$label uses $expected_source"
  else
    fail "$label expected $expected_source, found: ${matches:-none}"
    awk -F '\t' -v id="$package_id" '$1 == id { print "  - " $2 " | " $3 " | " $4 }' "$installed_mods"
  fi
}

check_exact_source "atlas.androidtiers" "3270639973" "Android Tiers"
check_exact_source "dandman.grothingthings" "2976584286" "GroThing"
check_exact_source "allibellcodex.arcticcompat" "Local" "AllibellCodex Arctic Compatibility"

if (( failures > 0 )); then
  echo
  echo "$failures audit check(s) failed."
  exit 1
fi

echo
echo "Exact RimWorld 1.5 multiplayer setup audit passed."
