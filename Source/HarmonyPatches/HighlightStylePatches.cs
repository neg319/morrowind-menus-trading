using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class HighlightStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if (method.Name == nameof(Widgets.DrawHighlight) ||
                method.Name == nameof(Widgets.DrawHighlightIfMouseover) ||
                method.Name == nameof(Widgets.DrawHighlightSelected) ||
                method.Name == "DrawLightHighlight")
            {
                yield return method;
            }
        }
    }

    public static bool Prefix(MethodBase __originalMethod, object[] __args)
    {
        if (Current.ProgramState != ProgramState.Playing || MorrowindMenusTradingMod.Settings == null || !MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return true;
        }

        if (__args == null || __args.Length == 0 || __args[0] is not Rect rect)
        {
            return true;
        }

        string name = __originalMethod?.Name ?? string.Empty;
        if (name == nameof(Widgets.DrawHighlightIfMouseover) && !Mouse.IsOver(rect))
        {
            return false;
        }

        bool strong = name == nameof(Widgets.DrawHighlightSelected) || Mouse.IsOver(rect);
        MorrowindWindowSkin.DrawHighlightOverlay(rect, strong);
        return false;
    }
}
