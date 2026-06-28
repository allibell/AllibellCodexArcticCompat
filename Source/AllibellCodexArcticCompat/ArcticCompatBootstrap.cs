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
        try
        {
            new Harmony("allibellcodex.arcticcompat").PatchAll();
            RegisterMultiplayerSyncMethods();
            ApplyAndroidTiersMultiplayerSettings();
        }
        catch (Exception ex)
        {
            Log.Warning($"{LogPrefix} Failed while applying compatibility settings: {ex}");
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

        var args = new object?[register.GetParameters().Length];
        args[0] = typeof(BillSearchDialog);
        args[1] = nameof(BillSearchDialog.AddBillSynced);
        register.Invoke(null, args);
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
