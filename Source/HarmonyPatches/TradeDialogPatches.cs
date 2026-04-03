using HarmonyLib;
using MorrowindMenusTrading.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(Dialog_Trade), nameof(Dialog_Trade.DoWindowContents))]
public static class TradeDialogPatches
{
    public static bool Prefix(Dialog_Trade __instance, Rect inRect)
    {
        if (Current.ProgramState != ProgramState.Playing || !MorrowindMenusTradingMod.Settings.morrowindInventoryUi)
        {
            return true;
        }

        MorrowindTradeDialogRenderer.Draw(__instance, inRect.ContractedBy(6f));
        return false;
    }
}
