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
    private const string SpawningCategory = "Spawning";
    private const string SpawnPawnKindLabel = "MP spawn pawn kind";

    private static readonly MethodInfo? GeneratePawnMethod = AccessTools.GetDeclaredMethods(typeof(PawnGenerator))
        .Where(method => method.Name == nameof(PawnGenerator.GeneratePawn))
        .Where(method =>
        {
            var parameters = method.GetParameters();
            return parameters.Length >= 2
                && parameters[0].ParameterType == typeof(PawnKindDef)
                && parameters[1].ParameterType == typeof(Faction);
        })
        .OrderBy(method => method.GetParameters().Length)
        .FirstOrDefault();

    private static readonly FieldInfo? DefaultFactionTypeField =
        AccessTools.Field(typeof(PawnKindDef), "defaultFactionType");

    private static readonly MethodInfo? PostPawnSpawnMethod =
        AccessTools.Method(typeof(DebugToolsSpawning), "PostPawnSpawn");

    public static void InjectDebugActions(DebugActionNode root)
    {
        if (root?.children == null)
            return;

        if (root.children.Any(child => child.label == SpawnPawnKindLabel && child.category == SpawningCategory))
            return;

        root.AddChild(new DebugActionNode(SpawnPawnKindLabel, DebugActionType.Action, null, null)
        {
            category = SpawningCategory,
            displayPriority = 1001,
            childGetter = SpawnPawnKind
        });
    }

    private static List<DebugActionNode> SpawnPawnKind()
    {
        return DefDatabase<PawnKindDef>.AllDefsListForReading
            .Where(kindDef => kindDef.showInDebugSpawner && kindDef.race != null)
            .OrderBy(kindDef => kindDef.defName)
            .Select(kindDef => new DebugActionNode(kindDef.defName, DebugActionType.ToolMap, () =>
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
        var pawn = GeneratePawn(kindDef);
        if (pawn == null)
            return;

        GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
        PostPawnSpawnMethod?.Invoke(null, new object[] { pawn });
    }

    private static Pawn? GeneratePawn(PawnKindDef kindDef)
    {
        if (GeneratePawnMethod == null)
            return null;

        var parameters = GeneratePawnMethod.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = kindDef;
        args[1] = DefaultFactionFor(kindDef);

        for (var i = 2; i < parameters.Length; i++)
            args[i] = DefaultValueFor(parameters[i]);

        return GeneratePawnMethod.Invoke(null, args) as Pawn;
    }

    private static Faction? DefaultFactionFor(PawnKindDef kindDef)
    {
        var factionDef = DefaultFactionTypeField?.GetValue(kindDef) as FactionDef;
        return factionDef == null ? null : FactionUtility.DefaultFactionFrom(factionDef);
    }

    private static object? DefaultValueFor(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;

        if (!parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) != null)
            return null;

        return Activator.CreateInstance(parameter.ParameterType);
    }
}
