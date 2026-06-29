using System.Reflection;
using HarmonyLib;

namespace AllibellCodex.ArcticCompat;

[HarmonyPatch]
public static class AndroidTiersLowNetworkSignalPatch
{
    private static readonly FieldInfo? DisableLowNetworkMalusField =
        AccessTools.Field("MOARANDROIDS.Settings:disableLowNetworkMalus");

    public static bool Prepare()
    {
        return AndroidTiersCompat.AndroidTiersLoaded;
    }

    public static MethodBase? TargetMethod()
    {
        return AccessTools.Method("MOARANDROIDS.Hediff_LowNetworkSignal:CurrentlyLowNetworkSignal");
    }

    public static bool Prefix(ref int __result)
    {
        if (DisableLowNetworkMalusField?.GetValue(null) is true)
        {
            __result = 0;
            return false;
        }

        return true;
    }
}

public static class AndroidTiersCompat
{
    public static bool AndroidTiersLoaded => AccessTools.TypeByName("MOARANDROIDS.Settings") != null;
}
