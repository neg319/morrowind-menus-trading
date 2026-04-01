using HarmonyLib;
using RimWorld;
using MorrowindMenusTrading.Systems;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(ResourceCounter), nameof(ResourceCounter.GetCount))]
public static class ResourceCounterPatches
{
    public static void Postfix(ThingDef rDef, ref int __result)
    {
        if (!MorrowindMenusTradingMod.Settings.personalStockpileMode || rDef == null)
        {
            return;
        }

        Map map = Find.CurrentMap;
        if (map == null)
        {
            return;
        }

        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            __result += PersonalInventoryStockpileSystem.CountThingInInventory(pawn, rDef);
        }
    }
}
