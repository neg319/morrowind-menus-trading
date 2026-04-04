using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class TextFieldStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        foreach (MethodInfo method in typeof(Widgets).GetMethods(flags))
        {
            if (method.Name == nameof(Widgets.TextField) && method.ReturnType == typeof(string))
            {
                yield return method;
            }
        }
    }

    public static bool Prefix(object[] __args, ref string __result)
    {
        if (Current.ProgramState != ProgramState.Playing || (!MorrowindMenusTradingMod.Settings.globalMorrowindWindows && !TradeDialogStyleState.ShouldStyle))
        {
            return true;
        }

        if (__args == null || __args.Length < 2 || __args[0] is not Rect rect)
        {
            return true;
        }

        string text = __args[1] as string ?? string.Empty;
        int maxLength = 0;
        for (int i = 2; i < __args.Length; i++)
        {
            if (__args[i] is int intValue && intValue > 0)
            {
                maxLength = intValue;
                break;
            }
        }

        bool focused = GUI.GetNameOfFocusedControl() == GetFieldControlName(rect);
        MorrowindWindowSkin.DrawTextFieldFrame(rect, focused || Mouse.IsOver(rect));

        GUI.SetNextControlName(GetFieldControlName(rect));

        Color oldColor = GUI.color;
        TextAnchor oldAnchor = Text.Anchor;
        GameFont oldFont = Text.Font;
        GUI.color = MorrowindUiResources.GoldSoft;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Small;

        Rect inner = rect.ContractedBy(4f);
        __result = maxLength > 0 ? GUI.TextField(inner, text, maxLength) : GUI.TextField(inner, text);

        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
        GUI.color = oldColor;
        return false;
    }

    private static string GetFieldControlName(Rect rect)
    {
        return $"MorrowindField_{rect.x:F0}_{rect.y:F0}_{rect.width:F0}_{rect.height:F0}";
    }
}
