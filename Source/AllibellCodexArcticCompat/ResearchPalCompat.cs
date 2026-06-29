using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AllibellCodex.ArcticCompat;

[HarmonyPatch]
public static class ResearchPalEnqueuePatch
{
    public static bool Prepare()
    {
        return ResearchPalCompat.ResearchPalLoaded;
    }

    public static MethodBase? TargetMethod()
    {
        return AccessTools.Method("ResearchPal.Queue:Enqueue");
    }

    public static bool Prefix(object node, bool add)
    {
        if (!ResearchPalCompat.ShouldSyncQueueCall())
            return true;

        var research = ResearchPalCompat.ResearchFromNode(node);
        if (research == null)
            return true;

        ResearchPalCompat.EnqueueSynced(research.defName, add);
        return false;
    }
}

[HarmonyPatch]
public static class ResearchPalEnqueueRangePatch
{
    public static bool Prepare()
    {
        return ResearchPalCompat.ResearchPalLoaded;
    }

    public static MethodBase? TargetMethod()
    {
        return AccessTools.Method("ResearchPal.Queue:EnqueueRange");
    }

    public static bool Prefix(IEnumerable nodes, bool add)
    {
        if (!ResearchPalCompat.ShouldSyncQueueCall())
            return true;

        var defNames = nodes.Cast<object>()
            .Select(ResearchPalCompat.ResearchFromNode)
            .Where(research => research != null)
            .Select(research => research!.defName)
            .ToArray();
        if (defNames.Length == 0)
            return true;

        ResearchPalCompat.EnqueueRangeSynced(string.Join("|", defNames), add);
        return false;
    }
}

[HarmonyPatch]
public static class ResearchPalDequeuePatch
{
    public static bool Prepare()
    {
        return ResearchPalCompat.ResearchPalLoaded;
    }

    public static MethodBase? TargetMethod()
    {
        return AccessTools.Method("ResearchPal.Queue:Dequeue");
    }

    public static bool Prefix(object node)
    {
        if (!ResearchPalCompat.ShouldSyncQueueCall())
            return true;

        var research = ResearchPalCompat.ResearchFromNode(node);
        if (research == null)
            return true;

        ResearchPalCompat.DequeueSynced(research.defName);
        return false;
    }
}

public static class ResearchPalCompat
{
    private const string PackageId = "notfood.researchpal";

    [ThreadStatic]
    private static bool executingSyncedQueueCall;

    public static bool ResearchPalLoaded => AccessTools.TypeByName("ResearchPal.Queue") != null;

    public static bool ShouldSyncQueueCall()
    {
        return ModsConfig.IsActive(PackageId)
            && !executingSyncedQueueCall
            && Event.current != null;
    }

    public static ResearchProjectDef? ResearchFromNode(object node)
    {
        return node.GetType().GetField("Research", BindingFlags.Public | BindingFlags.Instance)?.GetValue(node) as ResearchProjectDef;
    }

    public static void EnqueueSynced(string defName, bool add)
    {
        var node = FindNode(defName);
        if (node == null)
            return;

        InvokeQueueMethod("Enqueue", node, add);
    }

    public static void EnqueueRangeSynced(string defNames, bool add)
    {
        var nodeType = AccessTools.TypeByName("ResearchPal.ResearchNode");
        if (nodeType == null)
            return;

        var list = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(nodeType));
        foreach (var defName in defNames.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var node = FindNode(defName);
            if (node != null)
                list.Add(node);
        }

        if (list.Count == 0)
            return;

        InvokeQueueMethod("EnqueueRange", list, add);
    }

    public static void DequeueSynced(string defName)
    {
        var node = FindNode(defName);
        if (node == null)
            return;

        InvokeQueueMethod("Dequeue", node);
    }

    private static object? FindNode(string defName)
    {
        var research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
        if (research == null)
            return null;

        var treeType = AccessTools.TypeByName("ResearchPal.Tree");
        var nodes = treeType?.GetProperty("Nodes", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as IEnumerable;
        if (nodes == null)
            return null;

        foreach (var node in nodes)
        {
            if (ResearchFromNode(node) == research)
                return node;
        }

        return null;
    }

    private static void InvokeQueueMethod(string methodName, params object[] args)
    {
        var queueType = AccessTools.TypeByName("ResearchPal.Queue");
        if (queueType == null)
            return;

        var method = queueType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == args.Length);
        if (method == null)
            return;

        executingSyncedQueueCall = true;
        try
        {
            method.Invoke(null, args);
        }
        finally
        {
            executingSyncedQueueCall = false;
        }
    }
}
