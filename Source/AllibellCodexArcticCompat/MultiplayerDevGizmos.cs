using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllibellCodex.ArcticCompat;

[HarmonyPatch(typeof(Corpse), nameof(Corpse.GetGizmos))]
public static class CorpseGetGizmosPatch
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Corpse __instance)
    {
        foreach (var gizmo in __result)
            yield return gizmo;

        var pawn = __instance?.InnerPawn;
        if (Prefs.DevMode && pawn?.Dead == true)
            yield return MultiplayerDevGizmos.ResurrectCommand(pawn);
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public static class PawnGetGizmosPatch
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
    {
        foreach (var gizmo in __result)
            yield return gizmo;

        if (Prefs.DevMode && __instance?.Dead == true)
            yield return MultiplayerDevGizmos.ResurrectCommand(__instance);

        if (Prefs.DevMode && __instance?.InMentalState == true)
            yield return MultiplayerDevGizmos.ClearMentalStateCommand(__instance);
    }
}

public static class MultiplayerDevGizmos
{
    public static Command_Action ResurrectCommand(Pawn pawn)
    {
        return new Command_Action
        {
            defaultLabel = "MP DEV: resurrect",
            defaultDesc = "Resurrect this pawn through Multiplayer sync.",
            icon = TexCommand.DesirePower,
            action = () => ResurrectPawnSynced(pawn),
            groupKey = 941650231
        };
    }

    public static Command_Action ClearMentalStateCommand(Pawn pawn)
    {
        return new Command_Action
        {
            defaultLabel = "MP DEV: end mental state",
            defaultDesc = "End this pawn's current mental state through Multiplayer sync.",
            icon = TexCommand.ClearPrioritizedWork,
            action = () => ClearMentalStateSynced(pawn),
            groupKey = 941650232
        };
    }

    public static void ResurrectPawnSynced(Pawn pawn)
    {
        if (pawn?.Dead != true)
            return;

        ResurrectionUtility.TryResurrect(pawn);
    }

    public static void ClearMentalStateSynced(Pawn pawn)
    {
        if (pawn?.InMentalState != true)
            return;

        pawn.MentalState?.RecoverFromState();
        if (pawn.InMentalState)
            pawn.mindState.mentalStateHandler.Reset();
    }
}
