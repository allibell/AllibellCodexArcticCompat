# codex_arctic_2 compatibility audit

Enabled mods: 67

## Needs attention

- `unlimitedhugs.hugslib` (HugsLib): 1.6=yes, MP status=3. HugsLib itself is mostly a library, but mods using it can still have breakage.
- `murmur.walllight` (Wall Light): 1.6=no, MP status=4.
- `slabby.wallheater` (Wall Heater): 1.6=no, MP status=unknown.
- `ogliss.thewhitecrayon.quarry` (Quarry): 1.6=yes, MP status=3. MP cache says all players need synchronized settings.
- `roolo.runandgun` (RunAndGun): 1.6=no, MP status=4. MP cache says it requires Multiplayer Compatibility.
- `linkolas.stabilize` (Stabilize): 1.6=no, MP status=4.
- `unlimitedhugs.allowtool` (Allow Tool): 1.6=yes, MP status=2. MP cache recommends disabling the Storage space alert.
- `hatti.qualitybuilder` (QualityBuilder): 1.6=no, MP status=4. MP cache says it requires Multiplayer Compatibility.
- `uuugggg.replacestuff` (Replace Stuff): 1.6=no, MP status=3. MP cache says the replace tool does not work, but placing materials over different materials works.
- `vanillaexpanded.vanillatraitsexpanded` (Vanilla Traits Expanded): 1.6=yes, MP status=3. MP cache warns that Absent-minded can cause RNG desyncs.
- `wemd.reinforceddoors` ([WD] Reinforced Doors): 1.6=no, MP status=4.
- `alien.arasakacorporation` (Arasaka Corporation): 1.6=no, MP status=unknown.
- `brrainz.achtung` (Achtung!): 1.6=yes, MP status=2. MP cache warns that Force on a bulk group can desync.

## Installed mods without advertised 1.6 support

- `murmur.walllight` - Wall Light (1423699208)
- `slabby.wallheater` - Wall Heater (2298620806)
- `roolo.runandgun` - RunAndGun (1204108550)
- `linkolas.stabilize` - Stabilize (2023407836)
- `hatti.qualitybuilder` - QualityBuilder (754637870)
- `uuugggg.replacestuff` - Replace Stuff (1372003680)
- `wemd.reinforceddoors` - [WD] Reinforced Doors (1857597307)
- `alien.arasakacorporation` - Arasaka Corporation (2349566282)

## Existing Android coverage

- `atlas.androidtiers` is installed as Android tiers (Unofficial 1.6 Update), supports 1.6, and has a 1.6 assembly.
- Workshop Multiplayer Compatibility has Android Tiers handlers for surrogate/designator/window flows.
- `AllibellCodex Arctic Compatibility` adds runtime Android Tiers setting enforcement for the MP cache recommendations.

## Suggested load additions

Add these only when you choose to switch to the Codex compatibility layer:

- `rwmt.multiplayercompatibility`
- `allibellcodex.arcticcompat`

Place `allibellcodex.arcticcompat` after Multiplayer Compatibility and Android Tiers.
