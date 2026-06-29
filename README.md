# AllibellCodex Arctic Compatibility

Standalone compatibility helper for Allibell and Fyrex's exact RimWorld 1.5 multiplayer list.

This mod targets RimWorld `1.5.4409 rev1141`. It is intentionally separate from installed Workshop mods and does not overwrite Workshop files or the RimWorld app bundle.

## Exact Target List

The current multiplayer list has 68 active entries. The important file-identity pins are:

- `atlas.androidtiers` must be Workshop `3270639973` - Android tiers (Unofficial 1.5 Update).
- `dandman.grothingthings` must be Workshop `2976584286` - GroThing.
- `allibellcodex.arcticcompat` must be the local `AllibellCodexArcticCompat` folder or symlink from this repo.

The active package ID order must match `docs/exact-active-mods-1.5.txt`.

## Current Behavior

- Applies Multiplayer Compatibility's recommended Android Tiers settings at runtime:
  - `disableLowNetworkMalusInCaravans = true`
  - `disableLowNetworkMalus = true`
  - `duringSolarFlaresAndroidsShouldBeDowned = false`
- Replaces known Android Tiers `System.Random` calls with RimWorld's synced `Verse.Rand` calls where this Android Tiers 1.5 build still exposes those methods.
- If GroThing is enabled without Vanilla Furniture Expanded's `MF_ModernFurniture` research project, removes that missing prerequisite from `GroThing Plants`.
- Adds a Multiplayer-synced dev mode action at `Spawning > MP spawn pawn kind` for spawning pawns without using RimWorld's unsynced vanilla spawn command.
- Adds Multiplayer-synced dev gizmos:
  - `MP DEV: resurrect`
  - `MP DEV: end mental state`

Inactive-mod handlers from earlier experiments are deliberately not included.

## Install Or Update On macOS

Run:

```zsh
bash -c "$(curl -fsSL https://raw.githubusercontent.com/allibell/AllibellCodexArcticCompat/main/scripts/install_or_update.sh)"
```

If RimWorld is installed in a non-default Steam library, set `RIMWORLD_MODS_DIR` to that `RimWorldMac.app/Mods` directory before running the script.

## Multiplayer Setup Audit

Before hosting or joining:

```zsh
./scripts/audit_rimworld_mp_setup.sh
```

The audit should report:

- exact active order match
- no missing active mods
- no duplicate active package IDs
- Android Tiers loaded from `3270639973`
- GroThing loaded from `2976584286`
