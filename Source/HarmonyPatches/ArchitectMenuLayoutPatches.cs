using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.DoWindowContents))]
public static class ArchitectMenuLayoutPatches
{
    private static readonly MethodInfo ClickedCategoryMethod = AccessTools.Method(typeof(MainTabWindow_Architect), "ClickedCategory");
    private static string categorySearch = string.Empty;

    public static bool Prefix(MainTabWindow_Architect __instance, Rect inRect, ref ArchitectCategoryTab ___selectedDesPanel, ref List<ArchitectCategoryTab> ___desPanelsCached)
    {
        if (Current.ProgramState != ProgramState.Playing || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return true;
        }

        if (___selectedDesPanel != null)
        {
            return true;
        }

        if (___desPanelsCached == null || ___desPanelsCached.Count == 0)
        {
            return true;
        }

        DrawCustomArchitectRoot(__instance, inRect, ___desPanelsCached);
        return false;
    }

    private static void DrawCustomArchitectRoot(MainTabWindow_Architect window, Rect inRect, List<ArchitectCategoryTab> categories)
    {
        List<ArchitectCategoryTab> visibleCategories = GetFilteredCategories(categories, categorySearch);

        const float outerPadding = 6f;
        const float buttonGap = 4f;
        const float buttonHeight = 42f;
        const float searchHeight = 36f;
        const float headerGap = 8f;
        const float panelBottomGap = 10f;

        float buttonsHeight = (visibleCategories.Count * buttonHeight) + (Mathf.Max(0, visibleCategories.Count - 1) * buttonGap);
        float minimumTopPanelHeight = 120f;
        float desiredTopPanelHeight = inRect.height * 0.40f;
        float remaining = inRect.height - buttonsHeight - searchHeight - (outerPadding * 2f) - headerGap - panelBottomGap;
        float topPanelHeight = Mathf.Max(minimumTopPanelHeight, Mathf.Min(desiredTopPanelHeight, remaining));

        if (topPanelHeight + buttonsHeight + searchHeight + (outerPadding * 2f) + headerGap + panelBottomGap > inRect.height)
        {
            topPanelHeight = Mathf.Max(72f, inRect.height - buttonsHeight - searchHeight - (outerPadding * 2f) - headerGap - panelBottomGap);
        }

        Rect topPanelRect = new(inRect.x + outerPadding, inRect.y + outerPadding, inRect.width - (outerPadding * 2f), topPanelHeight);
        MorrowindWindowSkin.DrawFlatPanel(topPanelRect, MorrowindUiResources.BackgroundTint);
        MorrowindWindowSkin.DrawSubtleDivider(new Rect(topPanelRect.x, topPanelRect.yMax, topPanelRect.width, 1f));

        float buttonsStartY = topPanelRect.yMax + headerGap;
        float buttonWidth = inRect.width - (outerPadding * 2f);
        for (int i = 0; i < visibleCategories.Count; i++)
        {
            ArchitectCategoryTab category = visibleCategories[i];
            Rect buttonRect = new(inRect.x + outerPadding, buttonsStartY + (i * (buttonHeight + buttonGap)), buttonWidth, buttonHeight);
            bool active = false;
            bool mouseOver = Mouse.IsOver(buttonRect);
            bool clicked = Widgets.ButtonInvisible(buttonRect, true);

            MorrowindWindowSkin.DrawArchitectCategoryButton(buttonRect, category.def.LabelCap, active, mouseOver);
            if (clicked)
            {
                ClickCategory(window, category);
            }
        }

        Rect searchRect = new(inRect.x + outerPadding, inRect.yMax - searchHeight - outerPadding, inRect.width - (outerPadding * 2f), searchHeight);
        categorySearch = Widgets.TextField(searchRect, categorySearch ?? string.Empty);

        if (!string.IsNullOrEmpty(categorySearch))
        {
            Rect clearRect = new(searchRect.xMax - 28f, searchRect.y + 4f, 24f, searchRect.height - 8f);
            if (Widgets.ButtonInvisible(clearRect))
            {
                categorySearch = string.Empty;
                GUI.FocusControl(null);
            }

            MorrowindWindowSkin.DrawCenteredText(clearRect, "X", MorrowindUiResources.GoldSoft, GameFont.Small);
        }

        if (visibleCategories.Count == 0)
        {
            Rect emptyRect = new(inRect.x + outerPadding, buttonsStartY + 8f, buttonWidth, 32f);
            MorrowindWindowSkin.DrawCenteredText(emptyRect, "No categories match", MorrowindUiResources.GoldDark, GameFont.Small);
        }
    }

    private static List<ArchitectCategoryTab> GetFilteredCategories(List<ArchitectCategoryTab> categories, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return categories;
        }

        string trimmed = search.Trim();
        return categories.FindAll(category =>
            category?.def?.LabelCap != null &&
            category.def.LabelCap.ToString().IndexOf(trimmed, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void ClickCategory(MainTabWindow_Architect window, ArchitectCategoryTab category)
    {
        if (window == null || category == null || ClickedCategoryMethod == null)
        {
            return;
        }

        ClickedCategoryMethod.Invoke(window, new object[] { category });
    }
}
