using Verse;

namespace MorrowindMenusTrading;

public sealed class MorrowindMenusTradingSettings : ModSettings
{
    public bool morrowindInventoryUi = true;
    public bool personalStockpileMode = true;
    public float personalInventoryCapacityMultiplier = 25f;
    public int personalStockpileSearchRadius = 28;
    public bool globalMorrowindWindows = true;
    public bool showInventoryTradeMessages = true;
    public int personalStockpileTransferBatch = 40;
    public bool autoShareFood = true;
    public bool autoShareWeapons = true;
    public bool autoShareMedicine = true;
    public bool useTraderSpots = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref morrowindInventoryUi, nameof(morrowindInventoryUi), true);
        Scribe_Values.Look(ref personalStockpileMode, nameof(personalStockpileMode), true);
        Scribe_Values.Look(ref personalInventoryCapacityMultiplier, nameof(personalInventoryCapacityMultiplier), 25f);
        Scribe_Values.Look(ref personalStockpileSearchRadius, nameof(personalStockpileSearchRadius), 28);
        Scribe_Values.Look(ref globalMorrowindWindows, nameof(globalMorrowindWindows), true);
        Scribe_Values.Look(ref showInventoryTradeMessages, nameof(showInventoryTradeMessages), true);
        Scribe_Values.Look(ref personalStockpileTransferBatch, nameof(personalStockpileTransferBatch), 40);
        Scribe_Values.Look(ref autoShareFood, nameof(autoShareFood), true);
        Scribe_Values.Look(ref autoShareWeapons, nameof(autoShareWeapons), true);
        Scribe_Values.Look(ref autoShareMedicine, nameof(autoShareMedicine), true);
        Scribe_Values.Look(ref useTraderSpots, nameof(useTraderSpots), true);
    }

    public void Reset()
    {
        morrowindInventoryUi = true;
        personalStockpileMode = true;
        personalInventoryCapacityMultiplier = 25f;
        personalStockpileSearchRadius = 28;
        globalMorrowindWindows = true;
        showInventoryTradeMessages = true;
        personalStockpileTransferBatch = 40;
        autoShareFood = true;
        autoShareWeapons = true;
        autoShareMedicine = true;
        useTraderSpots = true;
    }
}
