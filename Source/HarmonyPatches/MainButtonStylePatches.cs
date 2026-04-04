using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(MainButtonWorker), nameof(MainButtonWorker.DoButton), new[] { typeof(Rect) })]
public static class MainButtonStylePatches
{
    private static readonly FieldInfo DefField = AccessTools.Field(typeof(MainButtonWorker), "def");
    private static readonly PropertyInfo DefProperty = AccessTools.Property(typeof(MainButtonWorker), "def")
        ?? AccessTools.Property(typeof(MainButtonWorker), "Def");
    private static readonly MethodInfo InterfaceTryActivateMethod = AccessTools.Method(typeof(MainButtonWorker), "InterfaceTryActivate");
    private static readonly PropertyInfo LabelCapProperty = AccessTools.Property(typeof(MainButtonDef), "LabelCap");
    private static readonly FieldInfo LabelField = AccessTools.Field(typeof(MainButtonDef), "label");
    private static readonly PropertyInfo IconProperty = AccessTools.Property(typeof(MainButtonDef), "Icon");

    public static bool Prefix(MainButtonWorker __instance, Rect rect)
    {
        if (Current.ProgramState != ProgramState.Playing || (!MorrowindMenusTradingMod.Settings.globalMorrowindWindows && !TradeDialogStyleState.ShouldStyle))
        {
            return true;
        }

        MainButtonDef def = GetDef(__instance);
        bool mouseOver = Mouse.IsOver(rect);
        bool clicked = Widgets.ButtonInvisible(rect, true);

        DrawButton(rect, def, mouseOver);

        if (clicked)
        {
            InterfaceTryActivateMethod?.Invoke(__instance, null);
        }

        return false;
    }

    private static MainButtonDef GetDef(MainButtonWorker worker)
    {
        return (MainButtonDef)(DefField?.GetValue(worker) ?? DefProperty?.GetValue(worker, null));
    }

    private static void DrawButton(Rect rect, MainButtonDef def, bool mouseOver)
    {
        MorrowindWindowSkin.DrawMainButton(rect, mouseOver);

        string label = GetLabel(def);
        Texture icon = GetIcon(def);
        Color contentColor = mouseOver ? MorrowindUiResources.TextPrimary : MorrowindUiResources.Gold;

        if (!string.IsNullOrWhiteSpace(label))
        {
            TooltipHandler.TipRegion(rect, label);
        }

        Rect contentRect = rect.ContractedBy(6f);
        if (icon != null && string.IsNullOrWhiteSpace(label))
        {
            DrawCenteredIcon(contentRect, icon, contentColor);
            return;
        }

        if (icon != null)
        {
            float iconSize = Mathf.Clamp(Mathf.Min(contentRect.width * 0.22f, contentRect.height * 0.28f), 12f, 18f);
            Rect iconRect = new Rect(contentRect.center.x - (iconSize * 0.5f), contentRect.y + 2f, iconSize, iconSize);
            DrawCenteredIcon(iconRect, icon, contentColor);

            Rect labelRect = new Rect(contentRect.x + 2f, iconRect.yMax + 3f, contentRect.width - 4f, Mathf.Max(16f, contentRect.yMax - iconRect.yMax - 3f));
            MorrowindWindowSkin.DrawMainButtonLabel(labelRect, label, contentColor);
            return;
        }

        MorrowindWindowSkin.DrawMainButtonLabel(contentRect, label, contentColor);
    }

    private static void DrawCenteredIcon(Rect rect, Texture texture, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        GUI.color = old;
    }

    private static string GetLabel(MainButtonDef def)
    {
        if (def == null)
        {
            return string.Empty;
        }

        string labelCap = LabelCapProperty?.GetValue(def, null) as string;
        if (!string.IsNullOrWhiteSpace(labelCap))
        {
            return labelCap;
        }

        string label = LabelField?.GetValue(def) as string;
        return label ?? string.Empty;
    }

    private static Texture GetIcon(MainButtonDef def)
    {
        return IconProperty?.GetValue(def, null) as Texture;
    }
}

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawMenuSection), new[] { typeof(Rect) })]
public static class MenuSectionStylePatches
{
    public static bool Prefix(Rect rect)
    {
        if (Current.ProgramState != ProgramState.Playing || (!MorrowindMenusTradingMod.Settings.globalMorrowindWindows && !TradeDialogStyleState.ShouldStyle))
        {
            return true;
        }

        if (rect.width < 36f || rect.height < 20f)
        {
            return true;
        }

        MorrowindWindowSkin.DrawPanel(rect, inset: 4f);
        return false;
    }
}
