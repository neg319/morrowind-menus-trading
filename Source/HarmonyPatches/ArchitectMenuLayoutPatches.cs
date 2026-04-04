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
        const float searchHeight = 36f;
        const float headerGap = 8f;
        const float searchGap = 8f;
        const float minButtonHeight = 28f;
        const float preferredButtonHeight = 42f;
        const float preferredButtonGap = 4f;
        const float minimumTopPanelHeight = 48f;

        float topPanelHeight = Mathf.Clamp(inRect.height * 0.12f, minimumTopPanelHeight, 72f);
        float buttonWidth = inRect.width - (outerPadding * 2f);
        float buttonsStartY = inRect.y + outerPadding + topPanelHeight + headerGap;
        float searchY = inRect.yMax - outerPadding - searchHeight;
        float availableButtonsHeight = Mathf.Max(0f, searchY - searchGap - buttonsStartY);

        float buttonGap = preferredButtonGap;
        float buttonHeight = preferredButtonHeight;
        if (visibleCategories.Count > 0)
        {
            float totalPreferredHeight = (visibleCategories.Count * preferredButtonHeight) + ((visibleCategories.Count - 1) * preferredButtonGap);
            if (totalPreferredHeight > availableButtonsHeight)
            {
                float compressedGap = visibleCategories.Count > 1 ? 2f : 0f;
                float compressedHeight = (availableButtonsHeight - ((visibleCategories.Count - 1) * compressedGap)) / visibleCategories.Count;
                buttonGap = compressedGap;
                buttonHeight = Mathf.Max(minButtonHeight, compressedHeight);
            }
        }

        Rect topPanelRect = new(inRect.x + outerPadding, inRect.y + outerPadding, buttonWidth, topPanelHeight);
        MorrowindWindowSkin.DrawFlatPanel(topPanelRect, Color.black);
        MorrowindWindowSkin.DrawSubtleDivider(new Rect(topPanelRect.x, topPanelRect.yMax, topPanelRect.width, 1f));

        for (int i = 0; i < visibleCategories.Count; i++)
        {
            ArchitectCategoryTab category = visibleCategories[i];
            Rect buttonRect = new(inRect.x + outerPadding, buttonsStartY + (i * (buttonHeight + buttonGap)), buttonWidth, buttonHeight);
            bool active = false;
            bool mouseOver = Mouse.IsOver(buttonRect);
            bool clicked = Widgets.ButtonInvisible(buttonRect, true);

            MorrowindWindowSkin.DrawArchitectCategoryButton(buttonRect, category.def.LabelCap, active, mouseOver, buttonHeight >= 36f ? GameFont.Medium : GameFont.Small);
            if (clicked)
            {
                ClickCategory(window, category);
            }
        }

        Rect searchRect = new(inRect.x + outerPadding, searchY, buttonWidth, searchHeight);
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
