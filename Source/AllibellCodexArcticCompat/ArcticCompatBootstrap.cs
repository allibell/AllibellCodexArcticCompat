using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AllibellCodex.ArcticCompat;

[StaticConstructorOnStartup]
public static class ArcticCompatBootstrap
{
    private const string LogPrefix = "[AllibellCodex ArcticCompat]";

    static ArcticCompatBootstrap()
    {
        ApplyStep("Harmony patches", () => new Harmony("allibellcodex.arcticcompat").PatchAll());
        ApplyStep("multiplayer sync method registration", RegisterMultiplayerSyncMethods);
        ApplyStep("Android Tiers settings", ApplyAndroidTiersMultiplayerSettings);
        ApplyStep("Winston Waves compatibility", ApplyWinstonWavesCompatibility);
    }

    private static void ApplyStep(string name, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Log.Warning($"{LogPrefix} Failed while applying {name}: {ex}");
        }
    }

    private static void RegisterMultiplayerSyncMethods()
    {
        var mpType = Type.GetType("Multiplayer.API.MP, 0MultiplayerAPI");
        var register = mpType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
            {
                if (method.Name != "RegisterSyncMethod")
                    return false;

                var parameters = method.GetParameters();
                return parameters.Length >= 2
                    && parameters[0].ParameterType == typeof(Type)
                    && parameters[1].ParameterType == typeof(string);
            });
        if (register == null)
            return;

        RegisterSyncMethod(register, typeof(BillSearchDialog), nameof(BillSearchDialog.AddBillSynced));
        RegisterSyncMethod(register, typeof(MultiplayerDevActions), nameof(MultiplayerDevActions.SpawnPawnKindSynced), debugOnly: true);
        RegisterSyncMethod(register, typeof(MultiplayerDevGizmos), nameof(MultiplayerDevGizmos.ResurrectPawnSynced), debugOnly: true);
        RegisterSyncMethod(register, typeof(MultiplayerDevGizmos), nameof(MultiplayerDevGizmos.ClearMentalStateSynced), debugOnly: true);
        RegisterSyncMethod(register, typeof(ResearchPalCompat), nameof(ResearchPalCompat.EnqueueSynced));
        RegisterSyncMethod(register, typeof(ResearchPalCompat), nameof(ResearchPalCompat.EnqueueRangeSynced));
        RegisterSyncMethod(register, typeof(ResearchPalCompat), nameof(ResearchPalCompat.DequeueSynced));
        RegisterSRTSSyncMethods(register);
        RegisterWinstonWavesSyncMethods(register);
    }

    private static void RegisterSyncMethod(MethodInfo register, Type type, string methodName, bool debugOnly = false)
    {
        if (type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) == null)
            return;

        var args = new object?[register.GetParameters().Length];
        args[0] = type;
        args[1] = methodName;

        object? syncMethod;
        try
        {
            syncMethod = register.Invoke(null, args);
        }
        catch (Exception ex)
        {
            Log.Warning($"{LogPrefix} Failed to register sync method {type.FullName}.{methodName}: {ex}");
            return;
        }

        if (!debugOnly || syncMethod == null)
            return;

        syncMethod.GetType()
            .GetMethod("SetDebugOnly", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(syncMethod, Array.Empty<object>());
    }

    private static void RegisterWinstonWavesSyncMethods(MethodInfo register)
    {
        if (!ModsConfig.IsActive("vanillastorytellersexpanded.winstonwave"))
            return;

        var debugOptions = Type.GetType("VSEWW.DebugOptions, VSEWW");
        if (debugOptions != null)
        {
            RegisterSyncMethod(register, debugOptions, "AddModifierToWave", debugOnly: true);
            RegisterSyncMethod(register, debugOptions, "ResetToWaveOne", debugOnly: true);
            RegisterSyncMethod(register, debugOptions, "ResetWave", debugOnly: true);
            RegisterSyncMethod(register, debugOptions, "RewardTest", debugOnly: true);
            RegisterSyncMethod(register, debugOptions, "SendAllReward", debugOnly: true);
            RegisterSyncMethod(register, debugOptions, "SendWaveNow", debugOnly: true);
            RegisterSyncMethod(register, debugOptions, "SkipToWave", debugOnly: true);
        }

        var rewardCreator = Type.GetType("VSEWW.RewardCreator, VSEWW");
        if (rewardCreator != null)
            RegisterSyncMethod(register, rewardCreator, "SendReward");
    }

    private static void RegisterSRTSSyncMethods(MethodInfo register)
    {
        if (!ModsConfig.IsActive("smashphil.srtsexpanded"))
            return;

        var launchable = Type.GetType("SRTS.CompLaunchableSRTS, SRTS");
        if (launchable != null)
            RegisterSyncMethod(register, launchable, "TryLaunch");
    }

    private static void ApplyWinstonWavesCompatibility()
    {
        if (!ModsConfig.IsActive("vanillastorytellersexpanded.winstonwave"))
            return;

        var winston = LoadedModManager.RunningModsListForReading
            .FirstOrDefault(mod => string.Equals(mod.PackageId, "VanillaStorytellersExpanded.WinstonWave", StringComparison.OrdinalIgnoreCase));
        if (winston?.ModMetaData?.IncompatibleWith == null)
            return;

        var removed = winston.ModMetaData.IncompatibleWith.RemoveAll(packageId =>
            string.Equals(packageId, "rwmt.Multiplayer", StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
            Log.Message($"{LogPrefix} Removed Winston Waves' stale Multiplayer incompatibility marker.");

        RemoveWinstonNaturalGoodwillPatch();
    }

    private static void RemoveWinstonNaturalGoodwillPatch()
    {
        var patchType = Type.GetType("VanillaStorytellersExpanded.Patch_NaturalGoodwill, VanillaStorytellersExpanded");
        var postfix = patchType?.GetMethod("Postfix", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        var target = AccessTools.PropertyGetter(typeof(RimWorld.Faction), nameof(RimWorld.Faction.NaturalGoodwill));
        if (postfix == null || target == null)
            return;

        new Harmony("allibellcodex.arcticcompat.winston").Unpatch(target, postfix);
        Log.Message($"{LogPrefix} Disabled Winston Waves natural goodwill postfix for Multiplayer.");
    }

    private static void ApplyAndroidTiersMultiplayerSettings()
    {
        if (!ModsConfig.IsActive("atlas.androidtiers"))
            return;

        var settingsType = Type.GetType("MOARANDROIDS.Settings, AndroidTiers");
        if (settingsType == null)
        {
            Log.Warning($"{LogPrefix} Android Tiers is active, but MOARANDROIDS.Settings was not found.");
            return;
        }

        var changed = 0;
        changed += SetStaticField(settingsType, "disableLowNetworkMalusInCaravans", true);
        changed += SetStaticField(settingsType, "disableLowNetworkMalus", true);
        changed += SetStaticField(settingsType, "duringSolarFlaresAndroidsShouldBeDowned", false);

        if (changed > 0)
            Log.Message($"{LogPrefix} Applied {changed} Android Tiers multiplayer compatibility setting(s).");
    }

    private static int SetStaticField(Type type, string fieldName, bool value)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null || field.FieldType != typeof(bool))
        {
            Log.Warning($"{LogPrefix} Could not find expected bool setting {type.FullName}.{fieldName}.");
            return 0;
        }

        var oldValue = (bool)field.GetValue(null);
        if (oldValue == value)
            return 0;

        field.SetValue(null, value);
        return 1;
    }
}
