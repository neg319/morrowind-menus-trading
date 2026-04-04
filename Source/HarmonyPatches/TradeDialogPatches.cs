using HarmonyLib;
using MorrowindMenusTrading.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(Dialog_Trade), nameof(Dialog_Trade.DoWindowContents))]
public static class TradeDialogPatches
{
    public static void Prefix(Dialog_Trade __instance, Rect inRect)
    {
        if (Current.ProgramState != ProgramState.Playing || !MorrowindMenusTradingMod.Settings.morrowindInventoryUi)
        {
            return;
        }

        TradeDialogStyleState.Begin();
    }

    public static void Postfix()
    {
        if (!TradeDialogStyleState.Active)
        {
            return;
        }

        TradeDialogStyleState.End();
    }

    public static void Finalizer(System.Exception __exception)
    {
        if (!TradeDialogStyleState.Active)
        {
            return;
        }

        TradeDialogStyleState.End();
    }
}
