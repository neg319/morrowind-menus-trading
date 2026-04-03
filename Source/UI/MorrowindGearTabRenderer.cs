using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MorrowindMenusTrading.UI;

public static class MorrowindGearTabRenderer
{
    private const float TitleHeight = 30f;
    private const float ModeTabsHeight = 28f;
    private const float CategoryTabsHeight = 26f;
    private const float FooterHeight = 42f;
    private const float LeftPaneWidth = 258f;
    private const float InventoryCellSize = 58f;
    private const float InventoryCellPadding = 5f;

    public static void Draw(Rect rect, Pawn pawn)
    {
        if (pawn == null)
        {
            return;
        }

        try
        {
            MorrowindInventoryState state = MorrowindInventoryStateStore.For(pawn);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            MorrowindWindowSkin.DrawWindow(rect);

            Rect titleRect = new(rect.x + 10f, rect.y + 8f, rect.width - 20f, TitleHeight);
            Rect topBarRect = new(rect.x + 10f, titleRect.yMax + 5f, rect.width - 20f, ModeTabsHeight);
            Rect contentRect = new(rect.x + 10f, topBarRect.yMax + 8f, rect.width - 20f, rect.height - TitleHeight - ModeTabsHeight - FooterHeight - 34f);
            Rect footerRect = new(rect.x + 10f, contentRect.yMax + 8f, rect.width - 20f, FooterHeight);

            DrawTitleBar(titleRect, pawn);
            DrawTopBar(topBarRect, pawn, state);

            Rect leftRect = new(contentRect.x, contentRect.y, LeftPaneWidth, contentRect.height);
            Rect rightRect = new(leftRect.xMax + 10f, contentRect.y, contentRect.width - LeftPaneWidth - 10f, contentRect.height);

            DrawLeftPane(leftRect, pawn);

            switch (state.activeTab)
            {
                case MorrowindInventoryTab.Inventory:
                    DrawInventoryPane(rightRect, pawn, state);
                    break;
                case MorrowindInventoryTab.Equipment:
                    DrawEquipmentPane(rightRect, pawn, state);
                    break;
                case MorrowindInventoryTab.Stats:
                    DrawStatsPane(rightRect, pawn, state);
                    break;
            }

            DrawFooter(footerRect, pawn, state);
        }
        finally
        {
            MorrowindWindowSkin.ResetTextState();
        }
    }

    private static void DrawTitleBar(Rect rect, Pawn pawn)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f, darkFill: false);
        DrawLabelCentered(rect, pawn.Name?.ToStringShort ?? pawn.LabelCap, MorrowindUiResources.TextPrimary);
    }

    private static void DrawTopBar(Rect rect, Pawn pawn, MorrowindInventoryState state)
    {
        Rect weightRect = new(rect.x, rect.y, 130f, rect.height);
        DrawWeightBox(weightRect, pawn);

        Rect tabsRect = new(weightRect.xMax + 10f, rect.y, rect.width - weightRect.width - 10f, rect.height);
        DrawModeTabs(tabsRect, state);
    }

    private static void DrawWeightBox(Rect rect, Pawn pawn)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f);
        GUI.color = MorrowindUiResources.CarryWeightFill;
        GUI.DrawTexture(rect.ContractedBy(4f), BaseContent.WhiteTex);
        GUI.color = Color.white;
        float carriedMass = MassUtility.GearAndInventoryMass(pawn);
        float capacity = MassUtility.Capacity(pawn);
        DrawLabelCentered(rect, $"{Mathf.RoundToInt(carriedMass)}/{Mathf.RoundToInt(capacity)}", MorrowindUiResources.TextPrimary);
    }

    private static void DrawModeTabs(Rect rect, MorrowindInventoryState state)
    {
        string[] labels = { "Inventory", "Equipment", "Stats" };
        MorrowindInventoryTab[] tabs =
        {
            MorrowindInventoryTab.Inventory,
            MorrowindInventoryTab.Equipment,
            MorrowindInventoryTab.Stats,
        };

        float tabWidth = 120f;
        for (int i = 0; i < tabs.Length; i++)
        {
            Rect tabRect = new(rect.x + i * (tabWidth + 6f), rect.y, tabWidth, rect.height);
            bool active = state.activeTab == tabs[i];
            DrawTab(tabRect, labels[i], active);
            if (Widgets.ButtonInvisible(tabRect))
            {
                state.activeTab = tabs[i];
            }
        }
    }

    private static void DrawTab(Rect rect, string label, bool active)
    {
        GUI.color = Color.white;
        GUI.DrawTexture(rect, active ? MorrowindUiResources.TabActive : MorrowindUiResources.TabInactive, ScaleMode.StretchToFill, true);
        DrawLabelCentered(rect, label, active ? MorrowindUiResources.ActiveTabText : MorrowindUiResources.InactiveTabText);
    }

    private static void DrawLeftPane(Rect rect, Pawn pawn)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect portraitRect = new(rect.x + 10f, rect.y + 10f, rect.width - 20f, 332f);
        DrawPortrait(portraitRect, pawn);

        Rect infoRect = new(rect.x + 10f, portraitRect.yMax + 8f, rect.width - 20f, rect.height - (portraitRect.yMax - rect.y) - 18f);
        DrawInfoPanel(infoRect, pawn);
    }

    private static void DrawPortrait(Rect rect, Pawn pawn)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(8f);
        Texture portrait = PortraitsCache.Get(
            pawn,
            inner.size,
            Rot4.South,
            Vector3.zero,
            1f,
            true,
            true,
            true,
            true,
            null,
            null,
            false);
        GUI.color = Color.white;
        GUI.DrawTexture(inner, portrait, ScaleMode.ScaleToFit, true);
    }

    private static void DrawInfoPanel(Rect rect, Pawn pawn)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(8f);
        List<Thing> colonyItems = MorrowindColonyInventoryCache.GetItemsForMap(pawn.MapHeld);
        DrawLabelLeft(new Rect(inner.x, inner.y, inner.width, 22f), $"Equipped: {GatherEquippedThings(pawn).Count}", MorrowindUiResources.TextPrimary);
        DrawLabelLeft(new Rect(inner.x, inner.y + 24f, inner.width, 22f), $"Carried stacks: {pawn.inventory?.innerContainer?.Count ?? 0}", MorrowindUiResources.TextPrimary);
        DrawLabelLeft(new Rect(inner.x, inner.y + 48f, inner.width, 22f), $"Colony stacks: {colonyItems.Count}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 72f, inner.width, 22f), $"Foods: {colonyItems.Count(IsFoodThing)}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 96f, inner.width, 22f), $"Manufactured: {colonyItems.Count(IsManufacturedThing)}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 120f, inner.width, 22f), $"Raw resources: {colonyItems.Count(IsRawResourceThing)}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 144f, inner.width, 22f), $"Items: {colonyItems.Count(IsItemThing)}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 168f, inner.width, 22f), $"Misc: {colonyItems.Count(IsMiscThing)}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 192f, inner.width, 22f), $"Move speed: {pawn.GetStatValue(StatDefOf.MoveSpeed):F2}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 216f, inner.width, 22f), $"Armor: {Mathf.RoundToInt((pawn.GetStatValue(StatDefOf.ArmorRating_Sharp) + pawn.GetStatValue(StatDefOf.ArmorRating_Blunt)) * 50f)}", MorrowindUiResources.TextMuted);
    }

    private static void DrawInventoryPane(Rect rect, Pawn pawn, MorrowindInventoryState state)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(10f);
        Rect categoryRect = new(inner.x, inner.y, inner.width, CategoryTabsHeight);
        DrawCategoryTabs(categoryRect, pawn, state);

        float columnWidth = 82f;
        Rect bodyRect = new(inner.x, categoryRect.yMax + 6f, inner.width, inner.height - CategoryTabsHeight - 20f);
        Rect equippedRect = new(bodyRect.x, bodyRect.y, columnWidth, bodyRect.height);
        Rect gridRect = new(equippedRect.xMax + 8f, bodyRect.y + 28f, bodyRect.width - columnWidth - 8f, bodyRect.height - 28f);
        Rect gridHeaderRect = new(equippedRect.xMax + 8f, bodyRect.y, bodyRect.width - columnWidth - 8f, 24f);

        List<MorrowindInventoryEntry> equippedEntries = GatherEquippedEntries(pawn, state);
        List<MorrowindInventoryEntry> entries = GatherColonyEntries(pawn, state);

        DrawEquippedColumn(equippedRect, equippedEntries, state);
        DrawLabelLeft(gridHeaderRect, GetInventoryHeaderText(pawn, state), MorrowindUiResources.TextPrimary);
        DrawInventoryGrid(gridRect, entries, state, pawn);

        Rect ornamentRect = new(inner.x + 4f, inner.yMax - 8f, inner.width - 8f, 4f);
        DrawBottomRail(ornamentRect);
    }

    private static void DrawCategoryTabs(Rect rect, Pawn pawn, MorrowindInventoryState state)
    {
        List<MorrowindItemCategory> configuredTabs = (MorrowindMenusTradingMod.Settings?.GetQuickTabs()
            ?? MorrowindMenusTradingSettings.GetDefaultQuickTabOrder()
                .Select(key => Enum.TryParse(key, out MorrowindItemCategory category) ? (MorrowindItemCategory?)category : null)
                .Where(category => category.HasValue && category.Value != MorrowindItemCategory.All)
                .Select(category => category.Value)
                .ToList()).ToList();

        List<MorrowindItemCategory> visibleTabs = new() { MorrowindItemCategory.All };
        visibleTabs.AddRange(configuredTabs.Where(category => category != MorrowindItemCategory.All));

        if (!visibleTabs.Contains(state.activeCategory))
        {
            state.activeCategory = MorrowindItemCategory.All;
        }

        float dropdownWidth = 92f;
        float tabGap = 4f;
        float x = rect.x;
        float maxTabsWidth = rect.width - dropdownWidth - tabGap;

        for (int i = 0; i < visibleTabs.Count; i++)
        {
            MorrowindItemCategory category = visibleTabs[i];
            string label = MorrowindMenusTradingMod.GetQuickTabLabel(category);
            float width = Mathf.Clamp(Text.CalcSize(label).x + 18f, 48f, 96f);
            if (x + width > rect.x + maxTabsWidth)
            {
                break;
            }

            Rect tabRect = new(x, rect.y, width, rect.height);
            bool active = !state.HasExtraCategorySelection && state.activeCategory == category;
            DrawTab(tabRect, label, active);
            if (Widgets.ButtonInvisible(tabRect))
            {
                state.activeCategory = category;
                state.ClearExtraCategories();
                state.inventoryScroll = Vector2.zero;
                state.ClearSelection();
            }

            x += width + tabGap;
        }

        float dropdownX = Mathf.Min(rect.xMax - dropdownWidth, x);
        Rect dropdownRect = new(dropdownX, rect.y, dropdownWidth, rect.height);
        string dropdownLabel = state.HasExtraCategorySelection ? $"More ({state.selectedExtraCategoryDefs.Count})" : "More...";
        DrawTab(dropdownRect, dropdownLabel, state.HasExtraCategorySelection);
        TooltipHandler.TipRegion(dropdownRect, BuildExtraCategoryTooltip(pawn, state));
        if (Widgets.ButtonInvisible(dropdownRect))
        {
            OpenExtraCategoryMenu(pawn, state);
        }
    }

    private static List<MorrowindInventoryEntry> GatherColonyEntries(Pawn pawn, MorrowindInventoryState state)
    {
        return MorrowindColonyInventoryCache.GetItemsForMap(pawn?.MapHeld)
            .Where(t => MatchesActiveFilter(t, state))
            .Select(t => new MorrowindInventoryEntry(t, MorrowindSelectionSource.Colony, false))
            .OrderBy(entry => CategorySortIndex(entry.thing))
            .ThenBy(entry => entry.thing.LabelCap.ToString())
            .ThenByDescending(entry => entry.thing.stackCount)
            .ToList();
    }

    private static List<MorrowindInventoryEntry> GatherEquippedEntries(Pawn pawn, MorrowindInventoryState state)
    {
        return GatherEquippedThings(pawn)
            .Where(t => MatchesActiveFilter(t, state))
            .Select(t => new MorrowindInventoryEntry(t, t is Apparel ? MorrowindSelectionSource.Apparel : MorrowindSelectionSource.Equipment, true))
            .OrderBy(entry => CategorySortIndex(entry.thing))
            .ThenBy(entry => entry.thing.LabelCap.ToString())
            .ToList();
    }

    private static bool MatchesActiveFilter(Thing thing, MorrowindInventoryState state)
    {
        if (thing == null)
        {
            return false;
        }

        if (state?.HasExtraCategorySelection == true)
        {
            return MatchesExtraCategorySelection(thing, state.selectedExtraCategoryDefs);
        }

        return MatchesCategory(thing, state?.activeCategory ?? MorrowindItemCategory.All);
    }

    private static bool MatchesCategory(Thing thing, MorrowindItemCategory category)
    {
        if (thing?.def == null)
        {
            return false;
        }

        return category switch
        {
            MorrowindItemCategory.All => true,
            MorrowindItemCategory.Foods => IsFoodThing(thing),
            MorrowindItemCategory.Manufactured => IsManufacturedThing(thing),
            MorrowindItemCategory.RawResources => IsRawResourceThing(thing),
            MorrowindItemCategory.Items => IsItemThing(thing),
            MorrowindItemCategory.Weapons => thing.def.IsWeapon,
            MorrowindItemCategory.Apparel => thing is Apparel,
            MorrowindItemCategory.Medicine => thing.def.IsMedicine,
            MorrowindItemCategory.Misc => IsMiscThing(thing),
            _ => false,
        };
    }

    private static bool MatchesExtraCategorySelection(Thing thing, IEnumerable<string> selectedCategoryDefs)
    {
        if (thing?.def?.thingCategories == null || selectedCategoryDefs == null)
        {
            return false;
        }

        HashSet<string> selected = selectedCategoryDefs as HashSet<string> ?? new HashSet<string>(selectedCategoryDefs);
        if (selected.Count == 0)
        {
            return false;
        }

        return thing.def.thingCategories.Any(category => category != null && selected.Contains(category.defName));
    }

    private static string GetInventoryHeaderText(Pawn pawn, MorrowindInventoryState state)
    {
        if (state?.HasExtraCategorySelection != true)
        {
            return "Colony inventory";
        }

        List<string> names = GetAvailableExtraCategories(pawn)
            .Where(category => state.selectedExtraCategoryDefs.Contains(category.defName))
            .Select(GetCategoryLabel)
            .OrderBy(label => label)
            .ToList();

        if (names.Count == 0)
        {
            return "Colony inventory";
        }

        string joined = string.Join(", ", names.Take(2));
        if (names.Count > 2)
        {
            joined += $" +{names.Count - 2}";
        }

        return $"Colony inventory - {joined}";
    }

    private static string BuildExtraCategoryTooltip(Pawn pawn, MorrowindInventoryState state)
    {
        List<ThingCategoryDef> categories = GetAvailableExtraCategories(pawn);
        if (categories.Count == 0)
        {
            return "No extra categories are available on this map right now.";
        }

        if (state?.HasExtraCategorySelection != true)
        {
            return "Browse more item categories that are not on the quick tabs.";
        }

        List<string> names = categories
            .Where(category => state.selectedExtraCategoryDefs.Contains(category.defName))
            .Select(GetCategoryLabel)
            .OrderBy(label => label)
            .ToList();

        return names.Count == 0
            ? "Browse more item categories that are not on the quick tabs."
            : "Selected categories: " + string.Join(", ", names);
    }

    private static void OpenExtraCategoryMenu(Pawn pawn, MorrowindInventoryState state)
    {
        List<ThingCategoryDef> categories = GetAvailableExtraCategories(pawn);
        if (categories.Count == 0)
        {
            Messages.Message("No other item categories are available on this map right now.", MessageTypeDefOf.RejectInput, false);
            return;
        }

        List<FloatMenuOption> options = new();
        if (state.HasExtraCategorySelection)
        {
            options.Add(new FloatMenuOption("Clear extra categories", () =>
            {
                state.ClearExtraCategories();
                state.inventoryScroll = Vector2.zero;
                state.ClearSelection();
            }));
        }

        foreach (ThingCategoryDef category in categories)
        {
            bool selected = state.selectedExtraCategoryDefs.Contains(category.defName);
            string label = (selected ? "[x] " : "[ ] ") + GetCategoryLabel(category);
            options.Add(new FloatMenuOption(label, () =>
            {
                state.ToggleExtraCategory(category.defName);
                state.inventoryScroll = Vector2.zero;
                state.ClearSelection();
            }));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static List<ThingCategoryDef> GetAvailableExtraCategories(Pawn pawn)
    {
        return MorrowindColonyInventoryCache.GetItemsForMap(pawn?.MapHeld)
            .Where(thing => thing?.def?.thingCategories != null)
            .SelectMany(thing => thing.def.thingCategories)
            .Where(IsSelectableExtraCategory)
            .GroupBy(category => category.defName)
            .Select(group => group.First())
            .OrderBy(category => GetCategoryLabel(category))
            .ToList();
    }

    private static bool IsSelectableExtraCategory(ThingCategoryDef category)
    {
        if (category == null)
        {
            return false;
        }

        string label = GetCategoryLabel(category);
        string defName = category.defName ?? string.Empty;
        string combined = (label + " " + defName).ToLowerInvariant();

        string[] blockedTokens =
        {
            "weapon",
            "apparel",
            "food",
            "meal",
            "medicine",
            "medic",
            "resource",
            "raw",
            "manufact",
            "item",
            "misc",
        };

        return !blockedTokens.Any(token => combined.Contains(token));
    }

    private static string GetCategoryLabel(ThingCategoryDef category)
    {
        if (category == null)
        {
            return string.Empty;
        }

        string label = category.label;
        if (!label.NullOrEmpty())
        {
            return label.CapitalizeFirst();
        }

        return category.defName;
    }

    private static int CategorySortIndex(Thing thing)
    {
        if (IsFoodThing(thing)) return 0;
        if (IsManufacturedThing(thing)) return 1;
        if (IsRawResourceThing(thing)) return 2;
        if (IsItemThing(thing)) return 3;
        if (thing.def.IsWeapon) return 4;
        if (thing is Apparel) return 5;
        if (thing.def.IsMedicine) return 6;
        if (IsMiscThing(thing)) return 7;
        return 8;
    }

    private static void DrawInventoryGrid(Rect rect, List<MorrowindInventoryEntry> entries, MorrowindInventoryState state, Pawn pawn)
    {
        int columns = Mathf.Max(1, Mathf.FloorToInt((rect.width - 4f) / (InventoryCellSize + InventoryCellPadding)));
        int rows = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)columns));
        float viewHeight = Mathf.Max(rect.height, rows * (InventoryCellSize + InventoryCellPadding) + 4f);
        Rect view = new(0f, 0f, rect.width - 16f, viewHeight);

        Widgets.BeginScrollView(rect, ref state.inventoryScroll, view);
        for (int index = 0; index < entries.Count; index++)
        {
            MorrowindInventoryEntry entry = entries[index];
            Thing thing = entry.thing;
            int row = index / columns;
            int col = index % columns;
            Rect cell = new(col * (InventoryCellSize + InventoryCellPadding), row * (InventoryCellSize + InventoryCellPadding), InventoryCellSize, InventoryCellSize);
            bool selected = state.selectedThingId == thing.thingIDNumber && state.selectionSource == entry.source;
            MorrowindWindowSkin.DrawSlot(cell, selected);
            DrawThingIcon(cell.ContractedBy(4f), thing);
            if (thing.stackCount > 1)
            {
                Text.Anchor = TextAnchor.LowerRight;
                GUI.color = MorrowindUiResources.TextPrimary;
                Widgets.Label(cell.ContractedBy(4f), thing.stackCount.ToString());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            TooltipHandler.TipRegion(cell, BuildThingTooltip(thing, entry.equipped));
            if (Widgets.ButtonInvisible(cell))
            {
                state.Select(thing, entry.source);
                if (Event.current != null && Event.current.clickCount > 1)
                {
                    PerformPrimaryAction(pawn, state);
                }
            }
        }
        Widgets.EndScrollView();
    }

    private static void DrawEquippedColumn(Rect rect, List<MorrowindInventoryEntry> entries, MorrowindInventoryState state)
    {
        MorrowindWindowSkin.DrawFlatPanel(rect, MorrowindUiResources.PanelShade);
        DrawLabelCentered(new Rect(rect.x, rect.y + 2f, rect.width, 20f), "Equipped", MorrowindUiResources.TextPrimary);
        Rect dividerRect = new(rect.xMax - 1f, rect.y + 4f, 1f, rect.height - 8f);
        MorrowindWindowSkin.DrawSubtleDivider(dividerRect);
        Rect listRect = new(rect.x + 5f, rect.y + 24f, rect.width - 10f, rect.height - 30f);
        float cellSize = Mathf.Min(66f, rect.width - 10f);
        float y = 0f;
        foreach (MorrowindInventoryEntry entry in entries)
        {
            Rect cell = new(listRect.x, listRect.y + y, cellSize, cellSize);
            bool selected = state.selectedThingId == entry.thing.thingIDNumber && state.selectionSource == entry.source;
            MorrowindWindowSkin.DrawSlot(cell, selected);
            MorrowindWindowSkin.DrawEquippedOutline(cell);
            DrawThingIcon(cell.ContractedBy(6f), entry.thing);
            TooltipHandler.TipRegion(cell, BuildThingTooltip(entry.thing, true));
            if (Widgets.ButtonInvisible(cell))
            {
                state.Select(entry.thing, entry.source);
            }
            y += cellSize + 6f;
        }
    }

    private static void DrawBottomRail(Rect rect)
    {
        GUI.color = MorrowindUiResources.GoldDark;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = MorrowindUiResources.Gold;
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), BaseContent.WhiteTex);
        GUI.color = Color.white;
    }

    private static void DrawEquipmentPane(Rect rect, Pawn pawn, MorrowindInventoryState state)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(10f);
        Rect headerRect = new(inner.x, inner.y, inner.width, 24f);
        DrawLabelLeft(headerRect, "Equipped items", MorrowindUiResources.TextPrimary);
        List<(string slot, Thing thing, MorrowindSelectionSource source)> equipped = GatherEquippedSlots(pawn);
        Rect listRect = new(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
        float viewHeight = Mathf.Max(listRect.height, equipped.Count * 50f + 4f);
        Rect view = new(0f, 0f, listRect.width - 16f, viewHeight);
        Widgets.BeginScrollView(listRect, ref state.equipmentScroll, view);
        float y = 0f;
        foreach ((string slot, Thing thing, MorrowindSelectionSource source) in equipped)
        {
            DrawEquipmentRow(new Rect(0f, y, view.width, 46f), slot, thing, state, source);
            y += 50f;
        }
        Widgets.EndScrollView();
    }

    private static List<(string slot, Thing thing, MorrowindSelectionSource source)> GatherEquippedSlots(Pawn pawn)
    {
        List<(string, Thing, MorrowindSelectionSource)> list = new();
        if (pawn.equipment?.Primary != null)
        {
            list.Add(("Main hand", pawn.equipment.Primary, MorrowindSelectionSource.Equipment));
        }
        foreach (Thing thing in GatherEquippedThings(pawn).Where(t => t is Apparel))
        {
            list.Add((InferSlotLabel(thing), thing, MorrowindSelectionSource.Apparel));
        }
        return list;
    }

    private static string InferSlotLabel(Thing thing)
    {
        if (thing is not Apparel apparel) return "Gear";
        if (Covers(apparel, "Head")) return "Head";
        if (Covers(apparel, "Neck")) return "Neck";
        if (Covers(apparel, "Torso") || Covers(apparel, "Chest")) return "Chest";
        if (Covers(apparel, "Waist") || CoversLayer(apparel, "Belt")) return "Belt";
        if (Covers(apparel, "Leg")) return "Legs";
        if (Covers(apparel, "Foot")) return "Feet";
        if (Covers(apparel, "Hand") || Covers(apparel, "Arm")) return "Hands";
        return "Gear";
    }

    private static void DrawEquipmentRow(Rect rect, string slot, Thing thing, MorrowindInventoryState state, MorrowindSelectionSource source)
    {
        bool selected = state.selectedThingId == thing.thingIDNumber && state.selectionSource == source;
        if (selected)
        {
            GUI.color = MorrowindUiResources.SelectedOverlay;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Color.white;
        }
        Rect slotLabelRect = new(rect.x, rect.y, 90f, rect.height);
        DrawLabelLeft(slotLabelRect, slot, MorrowindUiResources.TextMuted);
        Rect iconRect = new(rect.x + 94f, rect.y + 2f, 42f, 42f);
        MorrowindWindowSkin.DrawSlot(iconRect, selected);
        MorrowindWindowSkin.DrawEquippedOutline(iconRect);
        DrawThingIcon(iconRect.ContractedBy(4f), thing);
        Rect labelRect = new(rect.x + 144f, rect.y, rect.width - 144f, rect.height);
        DrawLabelLeft(labelRect, thing.LabelCap, MorrowindUiResources.TextPrimary);
        TooltipHandler.TipRegion(rect, BuildThingTooltip(thing, true));
        if (Widgets.ButtonInvisible(rect))
        {
            state.Select(thing, source);
        }
    }

    private static void DrawStatsPane(Rect rect, Pawn pawn, MorrowindInventoryState state)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(10f);
        Rect listRect = new(inner.x, inner.y + 4f, inner.width, inner.height - 4f);
        float viewHeight = Mathf.Max(listRect.height, 360f + (pawn.skills?.skills.Count ?? 0) * 24f);
        Rect view = new(0f, 0f, listRect.width - 16f, viewHeight);
        Widgets.BeginScrollView(listRect, ref state.statsScroll, view);
        float y = 0f;
        y = DrawStatLine(view, y, "Move speed", pawn.GetStatValue(StatDefOf.MoveSpeed).ToString("F2"));
        y = DrawStatLine(view, y, "Shooting accuracy", pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn).ToStringPercent());
        y = DrawStatLine(view, y, "Melee hit chance", pawn.GetStatValue(StatDefOf.MeleeHitChance).ToStringPercent());
        y = DrawStatLine(view, y, "Armor sharp", pawn.GetStatValue(StatDefOf.ArmorRating_Sharp).ToStringPercent());
        y = DrawStatLine(view, y, "Armor blunt", pawn.GetStatValue(StatDefOf.ArmorRating_Blunt).ToStringPercent());
        y = DrawStatLine(view, y, "Armor heat", pawn.GetStatValue(StatDefOf.ArmorRating_Heat).ToStringPercent());
        y = DrawStatLine(view, y, "Pain", pawn.health.hediffSet.PainTotal.ToStringPercent());
        y = DrawStatLine(view, y, "Health", pawn.health.summaryHealth.SummaryHealthPercent.ToStringPercent());
        y += 10f;
        y = DrawSectionLabel(view, y, "Skills");
        if (pawn.skills != null)
        {
            foreach (SkillRecord skill in pawn.skills.skills.OrderByDescending(s => s.Level))
            {
                y = DrawStatLine(view, y, skill.def.skillLabel.CapitalizeFirst(), skill.Level.ToString());
            }
        }
        Widgets.EndScrollView();
    }

    private static float DrawSectionLabel(Rect view, float y, string label)
    {
        Rect row = new(0f, y, view.width, 24f);
        DrawLabelLeft(row, label, MorrowindUiResources.TextPrimary);
        return y + 26f;
    }

    private static float DrawStatLine(Rect view, float y, string left, string right)
    {
        Rect row = new(0f, y, view.width, 22f);
        DrawLabelLeft(new Rect(row.x, row.y, row.width * 0.62f, row.height), left, MorrowindUiResources.TextMuted);
        DrawLabelRight(new Rect(row.x + row.width * 0.62f, row.y, row.width * 0.35f, row.height), right, MorrowindUiResources.TextPrimary);
        GUI.color = MorrowindUiResources.GoldSoft;
        GUI.DrawTexture(new Rect(row.x, row.yMax, row.width, 1f), BaseContent.WhiteTex);
        GUI.color = Color.white;
        return y + 24f;
    }

    private static void DrawFooter(Rect rect, Pawn pawn, MorrowindInventoryState state)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 3f);
        Thing selectedThing = ResolveSelection(pawn, state);
        Rect labelRect = new(rect.x + 10f, rect.y + 7f, rect.width * 0.52f, 28f);
        DrawLabelLeft(labelRect, selectedThing?.LabelCap ?? "Nothing selected", MorrowindUiResources.TextPrimary);
        float buttonWidth = 88f;
        float gap = 6f;
        Rect clearRect = new(rect.xMax - buttonWidth, rect.y + 7f, buttonWidth, 28f);
        Rect dropRect = new(clearRect.x - gap - buttonWidth, clearRect.y, buttonWidth, clearRect.height);
        Rect primaryRect = new(dropRect.x - gap - 110f, clearRect.y, 110f, clearRect.height);
        if (DrawActionButton(clearRect, "Clear")) state.ClearSelection();
        GUI.enabled = selectedThing != null && CanDrop(state.selectionSource);
        if (DrawActionButton(dropRect, "Drop")) DropSelectedThing(pawn, state);
        GUI.enabled = selectedThing != null;
        if (DrawActionButton(primaryRect, PrimaryActionLabel(selectedThing, state.selectionSource))) PerformPrimaryAction(pawn, state);
        GUI.enabled = true;
    }

    private static bool DrawActionButton(Rect rect, string label)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 2f);
        DrawLabelCentered(rect, label, GUI.enabled ? MorrowindUiResources.TextPrimary : MorrowindUiResources.TextMuted);
        return Widgets.ButtonInvisible(rect);
    }

    private static bool CanDrop(MorrowindSelectionSource source)
    {
        return source == MorrowindSelectionSource.Inventory || source == MorrowindSelectionSource.Equipment || source == MorrowindSelectionSource.Apparel;
    }

    private static string PrimaryActionLabel(Thing selectedThing, MorrowindSelectionSource source)
    {
        if (selectedThing == null) return "Pick Up";
        return source switch
        {
            MorrowindSelectionSource.Colony when selectedThing is Apparel => "Wear",
            MorrowindSelectionSource.Colony when selectedThing.def != null && selectedThing.def.IsWeapon => "Equip",
            MorrowindSelectionSource.Colony => "Pick Up",
            MorrowindSelectionSource.Inventory when selectedThing is Apparel => "Wear",
            MorrowindSelectionSource.Inventory when selectedThing.def != null && selectedThing.def.IsWeapon => "Equip",
            MorrowindSelectionSource.Inventory => "Use",
            MorrowindSelectionSource.Equipment => "Unequip",
            MorrowindSelectionSource.Apparel => "Unequip",
            _ => "Pick Up",
        };
    }

    private static Thing ResolveSelection(Pawn pawn, MorrowindInventoryState state)
    {
        if (pawn == null || state.selectedThingId < 0) return null;
        return state.selectionSource switch
        {
            MorrowindSelectionSource.Inventory => pawn.inventory?.innerContainer?.FirstOrDefault(t => t.thingIDNumber == state.selectedThingId),
            MorrowindSelectionSource.Colony => MorrowindColonyInventoryCache.GetItemsForMap(pawn.MapHeld).FirstOrDefault(t => t.thingIDNumber == state.selectedThingId),
            MorrowindSelectionSource.Equipment => pawn.equipment?.AllEquipmentListForReading?.FirstOrDefault(t => t.thingIDNumber == state.selectedThingId),
            MorrowindSelectionSource.Apparel => pawn.apparel?.WornApparel?.FirstOrDefault(t => t.thingIDNumber == state.selectedThingId),
            _ => null,
        };
    }

    private static void PerformPrimaryAction(Pawn pawn, MorrowindInventoryState state)
    {
        Thing selectedThing = ResolveSelection(pawn, state);
        if (pawn == null || selectedThing == null)
        {
            return;
        }

        switch (state.selectionSource)
        {
            case MorrowindSelectionSource.Colony:
                QueueColonyAction(pawn, selectedThing);
                break;
            case MorrowindSelectionSource.Inventory:
                if (selectedThing is Apparel apparel)
                {
                    if (pawn.inventory?.innerContainer?.Remove(apparel) == true)
                    {
                        pawn.apparel?.Wear(apparel, false, false);
                    }
                }
                else if (selectedThing.def != null && selectedThing.def.IsWeapon && selectedThing is ThingWithComps equippable)
                {
                    ThingWithComps primary = pawn.equipment?.Primary;
                    if (primary != null && pawn.inventory?.innerContainer != null)
                    {
                        pawn.equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer);
                    }
                    if (pawn.inventory?.innerContainer?.Remove(equippable) == true)
                    {
                        pawn.equipment?.AddEquipment(equippable);
                    }
                }
                break;
            case MorrowindSelectionSource.Equipment:
                if (selectedThing is ThingWithComps gear && pawn.inventory?.innerContainer != null)
                {
                    pawn.equipment?.TryTransferEquipmentToContainer(gear, pawn.inventory.innerContainer);
                }
                break;
            case MorrowindSelectionSource.Apparel:
                if (selectedThing is Apparel worn && pawn.inventory?.innerContainer != null)
                {
                    pawn.apparel?.Remove(worn);
                    pawn.inventory.innerContainer.TryAdd(worn);
                }
                break;
        }

        state.ClearSelection();
    }

    private static void QueueColonyAction(Pawn pawn, Thing selectedThing)
    {
        if (pawn.MapHeld == null || selectedThing.MapHeld != pawn.MapHeld || !selectedThing.Spawned)
        {
            Messages.Message("That item is no longer available.", MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (!pawn.CanReserveAndReach(selectedThing, PathEndMode.Touch, Danger.Deadly))
        {
            Messages.Message("That pawn cannot reach the selected item.", MessageTypeDefOf.RejectInput, false);
            return;
        }

        Job job;
        if (selectedThing is Apparel)
        {
            job = JobMaker.MakeJob(JobDefOf.Wear, selectedThing);
            job.count = 1;
        }
        else if (selectedThing.def.IsWeapon)
        {
            job = JobMaker.MakeJob(JobDefOf.Equip, selectedThing);
            job.count = 1;
        }
        else
        {
            job = JobMaker.MakeJob(JobDefOf.TakeInventory, selectedThing);
            job.count = selectedThing.stackCount;
        }

        pawn.jobs?.TryTakeOrderedJob(job);
    }

    private static void DropSelectedThing(Pawn pawn, MorrowindInventoryState state)
    {
        Thing selectedThing = ResolveSelection(pawn, state);
        if (pawn == null || selectedThing == null) return;
        switch (state.selectionSource)
        {
            case MorrowindSelectionSource.Inventory:
                pawn.inventory?.innerContainer?.TryDrop(selectedThing, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near, out _);
                break;
            case MorrowindSelectionSource.Equipment:
                if (selectedThing is ThingWithComps gear) pawn.equipment?.TryDropEquipment(gear, out _, pawn.PositionHeld, forbid: false);
                break;
            case MorrowindSelectionSource.Apparel:
                if (selectedThing is Apparel apparel) pawn.apparel?.TryDrop(apparel, out _, pawn.PositionHeld, forbid: false);
                break;
        }
        state.ClearSelection();
    }

    private static List<Thing> GatherEquippedThings(Pawn pawn)
    {
        List<Thing> list = new();
        if (pawn.equipment?.AllEquipmentListForReading != null) list.AddRange(pawn.equipment.AllEquipmentListForReading);
        if (pawn.apparel?.WornApparel != null) list.AddRange(pawn.apparel.WornApparel);
        return list;
    }

    private static bool Covers(Apparel apparel, string token)
    {
        return apparel.def.apparel?.bodyPartGroups?.Any(group => group.defName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) == true;
    }

    private static bool CoversLayer(Apparel apparel, string token)
    {
        return apparel.def.apparel?.layers?.Any(layer => layer.defName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) == true;
    }

    private static bool IsFoodThing(Thing thing)
    {
        return thing?.def?.ingestible != null && !thing.def.IsMedicine;
    }

    private static bool IsRawResourceThing(Thing thing)
    {
        if (thing?.def == null)
        {
            return false;
        }

        ThingDef def = thing.def;
        if (IsFoodThing(thing) || def.IsMedicine || def.IsWeapon || thing is Apparel)
        {
            return false;
        }

        if (def.IsStuff || def.stuffProps != null)
        {
            return true;
        }

        if (HasCategoryToken(def, "Raw"))
        {
            return true;
        }

        if (HasCategoryToken(def, "Resource") && !HasCategoryToken(def, "Manufactured"))
        {
            return true;
        }

        if (HasCategoryToken(def, "Metal") || HasCategoryToken(def, "Stone") || HasCategoryToken(def, "Wood") ||
            HasCategoryToken(def, "Textile") || HasCategoryToken(def, "Leather") || HasCategoryToken(def, "Fabric"))
        {
            return true;
        }

        return false;
    }

    private static bool IsManufacturedThing(Thing thing)
    {
        if (thing?.def == null)
        {
            return false;
        }

        ThingDef def = thing.def;
        if (IsFoodThing(thing) || def.IsMedicine || def.IsWeapon || thing is Apparel || IsRawResourceThing(thing))
        {
            return false;
        }

        if (def.recipeMaker != null || def.MadeFromStuff)
        {
            return true;
        }

        if (HasCategoryToken(def, "Manufactured") || HasCategoryToken(def, "Component"))
        {
            return true;
        }

        return def.stackLimit > 1;
    }

    private static bool IsItemThing(Thing thing)
    {
        if (thing?.def == null)
        {
            return false;
        }

        ThingDef def = thing.def;
        if (IsFoodThing(thing) || def.IsMedicine || def.IsWeapon || thing is Apparel || IsRawResourceThing(thing) || IsManufacturedThing(thing))
        {
            return false;
        }

        if (HasCategoryToken(def, "Item") || HasCategoryToken(def, "Items") || HasCategoryToken(def, "Utility") ||
            HasCategoryToken(def, "Tool") || HasCategoryToken(def, "Device") || HasCategoryToken(def, "Book") ||
            HasCategoryToken(def, "Artifact") || HasCategoryToken(def, "Drug") || HasCategoryToken(def, "Joy"))
        {
            return true;
        }

        if (def.comps != null && def.comps.Any(comp => comp.compClass != null &&
            comp.compClass.Name.IndexOf("UseEffect", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return true;
        }

        return false;
    }

    private static bool IsMiscThing(Thing thing)
    {
        if (thing?.def == null)
        {
            return false;
        }

        return !IsFoodThing(thing) &&
               !thing.def.IsMedicine &&
               !thing.def.IsWeapon &&
               thing is not Apparel &&
               !IsRawResourceThing(thing) &&
               !IsManufacturedThing(thing) &&
               !IsItemThing(thing);
    }

    private static bool HasCategoryToken(ThingDef def, string token)
    {
        if (def == null || token.NullOrEmpty())
        {
            return false;
        }

        if (def.thingCategories != null && def.thingCategories.Any(category => category.defName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return true;
        }

        if (def.tradeTags != null && def.tradeTags.Any(tag => tag.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return true;
        }

        return def.defName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void DrawThingIcon(Rect rect, Thing thing)
    {
        Texture icon = thing.def?.uiIcon ?? BaseContent.BadTex;
        Color oldColor = GUI.color;
        GUI.color = thing.DrawColor;
        GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = oldColor;
    }

    private static string BuildThingTooltip(Thing thing, bool equipped)
    {
        if (thing?.def == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        builder.AppendLine(thing.LabelCap.ToString());

        if (equipped)
        {
            builder.AppendLine("Currently equipped");
        }

        if (thing.stackCount > 1)
        {
            builder.AppendLine($"Stack: {thing.stackCount}");
        }

        if (QualityUtility.TryGetQuality(thing, out QualityCategory quality))
        {
            builder.AppendLine($"Quality: {quality.GetLabel().CapitalizeFirst()}");
        }

        if (thing.def.IsWeapon)
        {
            builder.AppendLine("Weapon. Can be equipped by a pawn.");
        }
        else if (thing is Apparel)
        {
            builder.AppendLine("Apparel. Can be worn by a pawn.");
        }
        else if (IsFoodThing(thing))
        {
            builder.AppendLine($"Food. Nutrition: {thing.def.ingestible.CachedNutrition:F2}");
        }
        else if (thing.def.IsMedicine)
        {
            builder.AppendLine($"Medicine. Potency: {thing.GetStatValue(StatDefOf.MedicalPotency):F2}");
        }
        else if (IsRawResourceThing(thing))
        {
            builder.AppendLine("Raw resource. Basic colony material.");
        }
        else if (IsManufacturedThing(thing))
        {
            builder.AppendLine("Manufactured item. Processed colony good.");
        }
        else if (IsItemThing(thing))
        {
            builder.AppendLine("Item. Utility colony item.");
        }
        else
        {
            builder.AppendLine("Miscellaneous colony item.");
        }

        if (!thing.def.description.NullOrEmpty())
        {
            builder.AppendLine();
            builder.AppendLine(thing.def.description.Trim());
        }

        builder.AppendLine();
        builder.Append($"Mass: {thing.GetStatValue(StatDefOf.Mass):0.##}");
        if (thing.MarketValue > 0f)
        {
            builder.Append($"   Value: {thing.MarketValue:0.#}");
        }

        return builder.ToString().TrimEnd();
    }

    private static void DrawLabelCentered(Rect rect, string text, Color color)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = color;
        Widgets.Label(rect, text);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static void DrawLabelLeft(Rect rect, string text, Color color)
    {
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = color;
        Widgets.Label(rect, text);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static void DrawLabelRight(Rect rect, string text, Color color)
    {
        Text.Anchor = TextAnchor.MiddleRight;
        GUI.color = color;
        Widgets.Label(rect, text);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }
}
