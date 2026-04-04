using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class GenericButtonStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if (method.Name == nameof(Widgets.ButtonText) ||
                method.Name == "ButtonTextSubtle" ||
                method.Name == nameof(Widgets.ButtonImage) ||
                method.Name.StartsWith("ButtonImage"))
            {
                yield return method;
            }
        }
    }

    public static bool Prefix(MethodBase __originalMethod, object[] __args, ref bool __result)
    {
        if (Current.ProgramState != ProgramState.Playing || (!MorrowindMenusTradingMod.Settings.globalMorrowindWindows && !TradeDialogStyleState.ShouldStyle))
        {
            return true;
        }

        if (__args == null || __args.Length == 0 || __args[0] is not Rect rect)
        {
            return true;
        }

        if (rect.width < 12f || rect.height < 12f)
        {
            return true;
        }

        string methodName = __originalMethod?.Name ?? string.Empty;
        if (methodName == nameof(Widgets.ButtonText) || methodName == "ButtonTextSubtle")
        {
            string label = __args.Length > 1 ? __args[1] as string : string.Empty;
            __result = DrawTextButton(rect, label);
            return false;
        }

        if (methodName.StartsWith("ButtonImage") && __args.Length > 1 && __args[1] is Texture texture)
        {
            __result = TimeControlsStyleState.Active
                ? DrawOutlinedIconButton(rect, texture)
                : DrawIconButton(rect, texture);
            return false;
        }

        return true;
    }

    private static bool DrawTextButton(Rect rect, string label)
    {
        bool mouseOver = Mouse.IsOver(rect);
        bool clicked = Widgets.ButtonInvisible(rect, true);
        bool active = GUI.enabled;

        MorrowindWindowSkin.DrawTextButton(rect, label, mouseOver, active);
        return clicked && active;
    }

    private static bool DrawIconButton(Rect rect, Texture icon)
    {
        bool mouseOver = Mouse.IsOver(rect);
        bool clicked = Widgets.ButtonInvisible(rect, true);
        bool active = GUI.enabled;

        MorrowindWindowSkin.DrawIconButton(rect, icon, mouseOver, active);
        return clicked && active;
    }

    private static bool DrawOutlinedIconButton(Rect rect, Texture icon)
    {
        bool mouseOver = Mouse.IsOver(rect);
        bool clicked = Widgets.ButtonInvisible(rect, true);
        bool active = GUI.enabled;

        MorrowindWindowSkin.DrawOutlinedIconButton(rect, icon, mouseOver, active);
        return clicked && active;
    }
}
