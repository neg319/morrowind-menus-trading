using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using MorrowindMenusTrading.UI;

namespace MorrowindMenusTrading;

public sealed class MorrowindMenusTradingMod : Mod
{
    public static MorrowindMenusTradingSettings Settings;
    private Vector2 settingsScroll;

    public MorrowindMenusTradingMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<MorrowindMenusTradingSettings>();
        Settings.NormalizeQuickTabOrder();
    }

    public override string SettingsCategory() => "MIL_ModName".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, Prefs.DevMode ? 640f : 520f);
        Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);

        var listing = new Listing_Standard();
        listing.Begin(viewRect);

        listing.CheckboxLabeled("MIL_MorrowindInventory".Translate(), ref Settings.morrowindInventoryUi);
        listing.CheckboxLabeled("MIL_GlobalWindows".Translate(), ref Settings.globalMorrowindWindows);
        listing.GapLine();
        listing.Label("Quick inventory tabs");
        listing.Label("The All tab is always shown first and cannot be disabled.");
        listing.Label("Choose which other quick tabs appear in the inventory screen and reorder them with the arrow buttons.");
        listing.Gap(6f);

        DrawQuickTabSettings(listing);

        listing.GapLine();
        if (listing.ButtonText("Reset all settings"))
        {
            Settings.Reset();
            WriteSettings();
        }

        if (listing.ButtonText("Reset quick tabs to default"))
        {
            Settings.quickTabOrder = MorrowindMenusTradingSettings.GetDefaultQuickTabOrder();
            Settings.NormalizeQuickTabOrder();
            WriteSettings();
        }

        if (Prefs.DevMode)
        {
            listing.GapLine();
            listing.Label("MIL_DebugHeader".Translate());
            listing.Label("MIL_DebugTraderVisitBlurb".Translate());

            if (listing.ButtonText("MIL_ForceTraderVisit".Translate()))
            {
                DebugTraderVisitUtility.TryForceTraderVisit(out string message, out MessageTypeDef messageType);
                Messages.Message(message, messageType, historical: false);
            }
        }

        listing.GapLine();
        listing.Label("MIL_SettingsBlurb".Translate());
        listing.End();
        Widgets.EndScrollView();
    }

    private void DrawQuickTabSettings(Listing_Standard listing)
    {
        Settings.NormalizeQuickTabOrder();
        List<MorrowindItemCategory> enabled = Settings.GetQuickTabs().ToList();
        List<MorrowindItemCategory> orderedDisplay = new();
        orderedDisplay.AddRange(enabled);
        orderedDisplay.AddRange(MorrowindMenusTradingSettings.AllQuickTabCategories().Where(category => !enabled.Contains(category)));

        for (int i = 0; i < orderedDisplay.Count; i++)
        {
            MorrowindItemCategory category = orderedDisplay[i];
            bool isEnabled = Settings.IsQuickTabEnabled(category);
            Rect row = listing.GetRect(28f);

            Rect checkRect = new Rect(row.x, row.y + 4f, 24f, 24f);
            Widgets.Checkbox(checkRect.position, ref isEnabled, 24f, true);
            if (isEnabled != Settings.IsQuickTabEnabled(category))
            {
                Settings.SetQuickTabEnabled(category, isEnabled);
                Settings.NormalizeQuickTabOrder();
                WriteSettings();
            }

            Rect labelRect = new Rect(checkRect.xMax + 6f, row.y + 2f, row.width - 120f, 24f);
            Widgets.Label(labelRect, GetQuickTabLabel(category));

            if (Settings.IsQuickTabEnabled(category))
            {
                int currentIndex = Settings.quickTabOrder.IndexOf(category.ToString());
                Rect upRect = new Rect(row.xMax - 56f, row.y + 2f, 24f, 24f);
                Rect downRect = new Rect(row.xMax - 28f, row.y + 2f, 24f, 24f);
                GUI.enabled = currentIndex > 0;
                if (Widgets.ButtonText(upRect, "↑"))
                {
                    Settings.MoveQuickTab(category, -1);
                    WriteSettings();
                }

                GUI.enabled = currentIndex >= 0 && currentIndex < Settings.quickTabOrder.Count - 1;
                if (Widgets.ButtonText(downRect, "↓"))
                {
                    Settings.MoveQuickTab(category, 1);
                    WriteSettings();
                }

                GUI.enabled = true;
            }
        }
    }

    public static string GetQuickTabLabel(MorrowindItemCategory category)
    {
        return category switch
        {
            MorrowindItemCategory.All => "All",
            MorrowindItemCategory.Weapons => "Weapons",
            MorrowindItemCategory.Apparel => "Apparel",
            MorrowindItemCategory.Foods => "Food",
            MorrowindItemCategory.Medicine => "Medicine",
            MorrowindItemCategory.Items => "Items",
            MorrowindItemCategory.RawResources => "Resources",
            MorrowindItemCategory.Manufactured => "Manufactured",
            MorrowindItemCategory.Misc => "Misc",
            _ => category.ToString(),
        };
    }
}
