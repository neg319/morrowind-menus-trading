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

        if (listing.ButtonText("MIL_Reset".Translate()))
        {
            Settings.Reset();
        }

        listing.GapLine();
        listing.Label("MIL_SettingsBlurb".Translate());
        listing.End();
    }
}
