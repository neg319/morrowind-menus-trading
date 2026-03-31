using HarmonyLib;
using RimWorld;
using Verse;

namespace MorrowindMenusTrading.HarmonyPatches;

[HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity))]
public static class InventoryCapacityPatches
{
    public static void Postfix(Pawn p, ref float __result)
    {
        if (p == null || !MorrowindMenusTradingMod.Settings.personalStockpileMode)
        {
            return;
        }

        if (!p.IsColonistPlayerControlled)
        {
            return;
        }

        __result *= MorrowindMenusTradingMod.Settings.personalInventoryCapacityMultiplier;
    }
}
