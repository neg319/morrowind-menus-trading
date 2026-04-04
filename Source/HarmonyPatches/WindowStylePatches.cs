using HarmonyLib;
using MorrowindMenusTrading.UI;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawWindowBackground), new[] { typeof(Rect) })]
public static class WindowStylePatches
{
    public static bool Prefix(Rect rect)
    {
        if (!MorrowindMenusTradingMod.Settings.globalMorrowindWindows && !TradeDialogStyleState.ShouldStyle)
        {
            return true;
        }

        if (rect.width < 60f || rect.height < 36f)
        {
            return true;
        }

        MorrowindWindowSkin.DrawWindow(rect);
        return false;
    }
}
