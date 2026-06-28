using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllibellCodex.ArcticCompat;

public static class MultiplayerDevActions
{
    private static readonly MethodInfo? PostPawnSpawnMethod =
        AccessTools.Method(typeof(DebugToolsSpawning), "PostPawnSpawn");

    [DebugAction("Spawning", "MP spawn pawn kind", allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1001)]
    public static List<DebugActionNode> SpawnPawnKind()
    {
        return DefDatabase<PawnKindDef>.AllDefsListForReading
            .Where(kindDef => kindDef.showInDebugSpawner && kindDef.race != null)
            .OrderBy(kindDef => kindDef.LabelCap.RawText)
            .Select(kindDef => new DebugActionNode(LabelFor(kindDef), DebugActionType.ToolMap, () =>
            {
                SpawnPawnKindSynced(kindDef, UI.MouseCell(), Find.CurrentMap);
            }, null))
            .ToList();
    }

    public static void SpawnPawnKindSynced(PawnKindDef kindDef, IntVec3 cell, Map map)
    {
        if (kindDef == null || map == null || !cell.IsValid)
            return;

        var spawnCell = CellFinder.RandomSpawnCellForPawnNear(cell, map, 4);
        var pawn = PawnGenerator.GeneratePawn(kindDef, null, map.Tile);
        GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
        PostPawnSpawnMethod?.Invoke(null, new object[] { pawn });
    }

    private static string LabelFor(PawnKindDef kindDef)
    {
        return $"{kindDef.LabelCap.RawText} ({kindDef.defName})";
    }
}
