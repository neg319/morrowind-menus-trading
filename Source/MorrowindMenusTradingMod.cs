using RimWorld;
using UnityEngine;
using Verse;

namespace MorrowindMenusTrading;

public sealed class MorrowindMenusTradingMod : Mod
{
    public static MorrowindMenusTradingSettings Settings;

    public MorrowindMenusTradingMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<MorrowindMenusTradingSettings>();
    }

    public override string SettingsCategory() => "MIL_ModName".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.CheckboxLabeled("MIL_MorrowindInventory".Translate(), ref Settings.morrowindInventoryUi);
        listing.CheckboxLabeled("MIL_GlobalWindows".Translate(), ref Settings.globalMorrowindWindows);
        listing.CheckboxLabeled("MIL_PersonalStockpileMode".Translate(), ref Settings.personalStockpileMode);
        listing.CheckboxLabeled("MIL_ShowTradeMessages".Translate(), ref Settings.showInventoryTradeMessages);
        listing.CheckboxLabeled("MIL_AutoShareFood".Translate(), ref Settings.autoShareFood);
        listing.CheckboxLabeled("MIL_AutoShareWeapons".Translate(), ref Settings.autoShareWeapons);
        listing.CheckboxLabeled("MIL_AutoShareMedicine".Translate(), ref Settings.autoShareMedicine);

        listing.Label($"{"MIL_CarryMultiplier".Translate()}: {Settings.personalInventoryCapacityMultiplier:F1}x");
        Settings.personalInventoryCapacityMultiplier = listing.Slider(Settings.personalInventoryCapacityMultiplier, 2f, 100f);

        listing.Label($"{"MIL_StockpileRadius".Translate()}: {Settings.personalStockpileSearchRadius}");
        Settings.personalStockpileSearchRadius = (int)listing.Slider(Settings.personalStockpileSearchRadius, 4, 60);

        listing.Label($"{"MIL_TransferBatch".Translate()}: {Settings.personalStockpileTransferBatch}");
        Settings.personalStockpileTransferBatch = (int)listing.Slider(Settings.personalStockpileTransferBatch, 4, 100);

        if (listing.ButtonText("MIL_Reset".Translate()))
        {
            Settings.Reset();
        }

        listing.GapLine();
        listing.Label("MIL_SettingsBlurb".Translate());
        listing.End();
    }
}
