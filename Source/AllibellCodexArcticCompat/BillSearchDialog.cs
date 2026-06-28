using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AllibellCodex.ArcticCompat;

[HarmonyPatch(typeof(BillStack), nameof(BillStack.DoListing))]
public static class BillStackSearchButtonPatch
{
    public static void Postfix(BillStack __instance, Rect rect)
    {
        if (__instance?.billGiver is not Building_WorkTable table || __instance.Count >= 15)
            return;

        var buttonRect = new Rect(rect.x + 155f, rect.y, 120f, 29f);
        if (Widgets.ButtonText(buttonRect, "Search bills"))
        {
            Find.WindowStack.Add(new BillSearchDialog(table));
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        TooltipHandler.TipRegion(buttonRect, "Search recipes available at this worktable.");
    }
}

public sealed class BillSearchDialog : Window
{
    private readonly Building_WorkTable table;
    private Vector2 scrollPosition;
    private string search = string.Empty;

    public override Vector2 InitialSize => new(620f, 720f);

    public BillSearchDialog(Building_WorkTable table)
    {
        this.table = table;
        doCloseButton = true;
        doCloseX = true;
        absorbInputAroundWindow = false;
        forcePause = false;
        draggable = true;
        resizeable = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), $"Bills: {table.LabelCap}");

        Text.Font = GameFont.Small;
        var searchRect = new Rect(inRect.x, inRect.y + 38f, inRect.width, 30f);
        GUI.SetNextControlName("AllibellCodexBillSearch");
        search = Widgets.TextField(searchRect, search ?? string.Empty);
        if (Event.current.type == EventType.Layout && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            GUI.FocusControl("AllibellCodexBillSearch");

        var recipes = AvailableRecipes(table)
            .Where(recipe => Matches(recipe, search))
            .OrderBy(recipe => recipe.LabelCap.RawText)
            .ToList();

        var outRect = new Rect(inRect.x, searchRect.yMax + 10f, inRect.width, inRect.height - searchRect.yMax - 58f);
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, Math.Max(outRect.height, recipes.Count * 44f + 8f));

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        var y = 4f;
        foreach (var recipe in recipes)
        {
            var row = new Rect(0f, y, viewRect.width, 38f);
            DrawRecipeRow(row, recipe);
            y += 44f;
        }
        Widgets.EndScrollView();

        if (recipes.Count == 0)
            Widgets.Label(outRect.ContractedBy(8f), "No matching bills.");
    }

    private void DrawRecipeRow(Rect row, RecipeDef recipe)
    {
        if (Mouse.IsOver(row))
            Widgets.DrawHighlight(row);

        var iconRect = new Rect(row.x + 4f, row.y + 4f, 30f, 30f);
        var iconThing = recipe.UIIconThing;
        if (iconThing != null)
        {
            GUI.color = iconThing.uiIconColor;
            Widgets.DrawTextureFitted(iconRect, iconThing.uiIcon, 1f);
            GUI.color = Color.white;
        }

        var labelRect = new Rect(row.x + 42f, row.y + 2f, row.width - 142f, 34f);
        Widgets.Label(labelRect, recipe.LabelCap);

        var addRect = new Rect(row.xMax - 88f, row.y + 5f, 84f, 28f);
        if (Widgets.ButtonText(addRect, "Add"))
        {
            AddBillSynced(table, recipe);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            Close();
        }

        TooltipHandler.TipRegion(row, recipe.description ?? recipe.LabelCap);
    }

    private static IEnumerable<RecipeDef> AvailableRecipes(Building_WorkTable workTable)
    {
        return (workTable.def?.AllRecipes ?? Enumerable.Empty<RecipeDef>())
            .Where(recipe => recipe.AvailableNow && recipe.AvailableOnNow(workTable));
    }

    private static bool Matches(RecipeDef recipe, string query)
    {
        if (query.NullOrEmpty())
            return true;

        return Contains(recipe.label, query)
            || Contains(recipe.defName, query)
            || Contains(recipe.description, query)
            || Contains(recipe.modContentPack?.Name, query)
            || Contains(recipe.ProducedThingDef?.label, query)
            || Contains(recipe.ProducedThingDef?.defName, query)
            || Contains(recipe.researchPrerequisite?.label, query)
            || recipe.ingredients.Any(ingredient => Contains(ingredient.filter?.Summary, query));
    }

    private static bool Contains(string? value, string query)
    {
        if (value == null || value.Length == 0)
            return false;

        return value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static void AddBillSynced(Building_WorkTable workTable, RecipeDef recipe)
    {
        if (workTable == null || recipe == null || workTable.billStack == null || workTable.billStack.Count >= 15)
            return;

        if (!recipe.AvailableNow || !recipe.AvailableOnNow(workTable))
            return;

        var bill = recipe.MakeNewBill();
        workTable.billStack.AddBill(bill);
    }
}
