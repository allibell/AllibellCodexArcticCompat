using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using Verse;

namespace AllibellCodex.ArcticCompat;

[HarmonyPatch(typeof(DebugTabMenu_Actions), nameof(DebugTabMenu_Actions.InitActions))]
public static class MultiplayerDevActionsDebugMenuPatch
{
    public static void Postfix(DebugActionNode __result)
    {
        MultiplayerDevActions.InjectDebugActions(__result);
    }
}

public static class MultiplayerDevActions
{
    private const string SpawningLabel = "Spawning";
    private const string SpawnPawnKindLabel = "MP spawn pawn kind";

    private static readonly MethodInfo? PostPawnSpawnMethod =
        AccessTools.Method(typeof(DebugToolsSpawning), "PostPawnSpawn");

    public static void InjectDebugActions(DebugActionNode root)
    {
        if (root?.children == null)
            return;

        var spawning = root.children.FirstOrDefault(child => child.label == SpawningLabel);
        if (spawning?.children?.Any(child => child.label == SpawnPawnKindLabel) == true)
            return;

        if (spawning == null)
        {
            spawning = new DebugActionNode(SpawningLabel, DebugActionType.Action, null, null);
            root.AddChild(spawning);
        }

        var node = new DebugActionNode(SpawnPawnKindLabel, DebugActionType.Action, null, null)
        {
            childGetter = SpawnPawnKind
        };
        spawning.AddChild(node);
    }

    private static List<DebugActionNode> SpawnPawnKind()
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
        var pawn = PawnGenerator.GeneratePawn(kindDef, null);
        GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
        PostPawnSpawnMethod?.Invoke(null, new object[] { pawn });
    }

    private static string LabelFor(PawnKindDef kindDef)
    {
        return $"{kindDef.LabelCap.RawText} ({kindDef.defName})";
    }
}
