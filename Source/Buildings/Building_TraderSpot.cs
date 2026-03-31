using MorrowindMenusTrading.Components;
using Verse;

namespace MorrowindMenusTrading.Buildings;

public class Building_TraderSpot : Building
{
    public InventoryTraderRole SpotRole => def?.defName switch
    {
        "MMT_FoodTradingSpot" => InventoryTraderRole.Food,
        "MMT_MedicineTradingSpot" => InventoryTraderRole.Medicine,
        "MMT_WeaponsTradingSpot" => InventoryTraderRole.Weapons,
        "MMT_ApparelTradingSpot" => InventoryTraderRole.Apparel,
        "MMT_ResourcesTradingSpot" => InventoryTraderRole.Resources,
        "MMT_MiscTradingSpot" => InventoryTraderRole.Misc,
        _ => InventoryTraderRole.None,
    };


    public override string GetInspectString()
    {
        string baseText = base.GetInspectString();
        string roleText = $"Trading role: {SpotRole}";
        if (string.IsNullOrWhiteSpace(baseText))
        {
            return roleText;
        }

        return baseText + "\n" + roleText;
    }
}
