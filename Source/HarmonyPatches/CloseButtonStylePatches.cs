using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch]
public static class CloseButtonStylePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(Widgets))
            .Where(method => method.Name == "CloseButtonFor");
    }

    public static bool Prefix(object[] __args, ref bool __result)
    {
        if (!MorrowindMenusTradingMod.Settings.globalMorrowindWindows)
        {
            return true;
        }

        if (__args == null || __args.Length == 0 || __args[0] is not Rect rect)
        {
            return true;
        }

        __result = MorrowindWindowSkin.DrawCloseButton(rect);
        MorrowindWindowSkin.ResetTextState();
        return false;
    }
}
