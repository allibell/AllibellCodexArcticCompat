using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace AllibellCodex.ArcticCompat;

[HarmonyPatch]
public static class AndroidTiersSystemRandomTranspiler
{
    private static readonly MethodInfo? RandomNextInt =
        AccessTools.Method(typeof(Random), nameof(Random.Next), new[] { typeof(int), typeof(int) });
    private static readonly MethodInfo? RandomNextMax =
        AccessTools.Method(typeof(Random), nameof(Random.Next), new[] { typeof(int) });
    private static readonly MethodInfo? RandomNextDouble =
        AccessTools.Method(typeof(Random), nameof(Random.NextDouble), Type.EmptyTypes);
    private static readonly MethodInfo? RandRange =
        AccessTools.Method(typeof(Rand), nameof(Rand.Range), new[] { typeof(int), typeof(int) });
    private static readonly MethodInfo? RandRangeInclusive =
        AccessTools.Method(typeof(Rand), nameof(Rand.RangeInclusive), new[] { typeof(int), typeof(int) });
    private static readonly MethodInfo? RandValueGetter =
        AccessTools.PropertyGetter(typeof(Rand), nameof(Rand.Value));
    private static readonly MethodInfo? ConvertFloatToDouble =
        AccessTools.Method(typeof(AndroidTiersSystemRandomTranspiler), nameof(FloatToDouble));

    public static bool Prepare()
    {
        return AndroidTiersCompat.AndroidTiersLoaded;
    }

    public static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new[]
        {
            ("MOARANDROIDS.Components.CompHeatSensitive", new[] { "PostSpawnSetup", "CheckTemperature" }),
            ("MOARANDROIDS.HarmonyPatches.StartingPawnUtility_Patch+NewGeneratedStartingPawn_Patch", new[] { "Listener", "Postfix" }),
            ("MOARANDROIDS.HarmonyPatches.MultiplePawnRacesAtStart", new[] { "Postfix" }),
            ("MOARANDROIDS.Recipe_MemoryCorruptionChance", new[] { "ApplyOnPawn", "RandomCorruption" }),
            ("MOARANDROIDS.Recipe_AndroidRewireSurgery", new[] { "ApplyOnPawn", "RandomCorruption" }),
            ("MOARANDROIDS.Recipe_RemoveSentience", new[] { "ApplyOnPawn", "RandomCorruption" }),
            ("MOARANDROIDS.Recipe_RerollTraits", new[] { "ApplyOnPawn", "RandomCorruption" })
        };

        foreach (var (typeName, methodNames) in names)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type == null)
                continue;

            foreach (var methodName in methodNames)
            foreach (var method in AccessTools.GetDeclaredMethods(type).Where(method => method.Name == methodName))
                yield return method;
        }
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (RandomNextInt != null && RandRangeInclusive != null && instruction.Calls(RandomNextInt))
            {
                yield return CodeInstruction.Call(typeof(AndroidTiersSystemRandomTranspiler), nameof(SyncedRandomNext));
                continue;
            }

            if (RandomNextMax != null && RandRange != null && instruction.Calls(RandomNextMax))
            {
                yield return CodeInstruction.Call(typeof(AndroidTiersSystemRandomTranspiler), nameof(SyncedRandomNextMax));
                continue;
            }

            if (RandomNextDouble != null && RandValueGetter != null && ConvertFloatToDouble != null && instruction.Calls(RandomNextDouble))
            {
                yield return CodeInstruction.Call(typeof(AndroidTiersSystemRandomTranspiler), nameof(SyncedRandomNextDouble));
                continue;
            }

            yield return instruction;
        }
    }

    public static int SyncedRandomNext(Random random, int minValue, int maxValue)
    {
        if (maxValue <= minValue)
            return minValue;

        return Rand.RangeInclusive(minValue, maxValue - 1);
    }

    public static int SyncedRandomNextMax(Random random, int maxValue)
    {
        if (maxValue <= 0)
            return 0;

        return Rand.Range(0, maxValue);
    }

    public static double SyncedRandomNextDouble(Random random)
    {
        return Rand.Value;
    }

    public static double FloatToDouble(float value)
    {
        return value;
    }
}
