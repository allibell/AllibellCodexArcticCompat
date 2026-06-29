# AllibellCodex Arctic Compatibility

Standalone compatibility helper for Allibell's `codex_arctic_2` RimWorld mod list.

This mod is built for RimWorld 1.6 and is intentionally separate from installed Workshop mods. It does not overwrite files in Workshop or the RimWorld app bundle.

## Current behavior

- Loads after Multiplayer, Multiplayer Compatibility, Android Tiers, and selected higher-risk enabled mods.
- If `atlas.androidtiers` is enabled, applies Multiplayer Compatibility's recommended Android Tiers settings at runtime:
  - `disableLowNetworkMalusInCaravans = true`
  - `disableLowNetworkMalus = true`
  - `duringSolarFlaresAndroidsShouldBeDowned = false`
- If GroThing is enabled without Vanilla Furniture Expanded's `MF_ModernFurniture` research project, removes that missing prerequisite from `GroThing Plants` so its research remains usable.
- Adds a Multiplayer-synced dev mode action at `Spawning > MP spawn pawn kind` for spawning pawns such as huskies without using RimWorld's unsynced vanilla spawn command.
- Adds a Multiplayer-synced `MP DEV: resurrect` dev gizmo on dead pawns/corpses; use this instead of the vanilla `DEV: resurrect` button in Multiplayer.
- If Vanilla Storytellers Expanded - Winston Waves is enabled, removes its stale Multiplayer incompatibility marker, disables a null-prone natural goodwill postfix during Multiplayer loads, and syncs Winston's player-triggered reward and debug wave actions through Multiplayer.
- If ResearchPal is enabled, syncs its research queue UI operations through Multiplayer using stable research def names.

## Suggested load order

Place this after `rwmt.multiplayercompatibility`, Android Tiers, GroThing, Winston Waves, and ResearchPal.

Do not use it as a replacement for `rwmt.multiplayercompatibility`; use both when playing Multiplayer.

## Install or update on macOS

Run:

```zsh
bash -c "$(curl -fsSL https://raw.githubusercontent.com/allibell/AllibellCodexArcticCompat/main/scripts/install_or_update.sh)"
```

If RimWorld is installed in a non-default Steam library, set `RIMWORLD_MODS_DIR` to that `RimWorldMac.app/Mods` directory before running the script.
