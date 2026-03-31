using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using MorrowindMenusTrading.Buildings;
using MorrowindMenusTrading.Components;
using MorrowindMenusTrading.Systems;
using UnityEngine;
using Verse;

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
            case MorrowindInventoryTab.Roles:
                DrawRolesPane(rightRect, pawn);
                break;
        }

        DrawFooter(footerRect, pawn, state);
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
        string[] labels = { "Inventory", "Equipment", "Stats", "Roles" };
        MorrowindInventoryTab[] tabs =
        {
            MorrowindInventoryTab.Inventory,
            MorrowindInventoryTab.Equipment,
            MorrowindInventoryTab.Stats,
            MorrowindInventoryTab.Roles,
        };

        float tabWidth = 104f;
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
        var component = MorrowindMenusTradingGameComponent.Instance;
        InventoryTraderRole effectiveRole = component?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
        DrawLabelLeft(new Rect(inner.x, inner.y, inner.width, 22f), $"Equipped: {GatherEquippedThings(pawn).Count}", MorrowindUiResources.TextPrimary);
        DrawLabelLeft(new Rect(inner.x, inner.y + 24f, inner.width, 22f), $"Inventory stacks: {pawn.inventory?.innerContainer?.Count ?? 0}", MorrowindUiResources.TextPrimary);
        DrawLabelLeft(new Rect(inner.x, inner.y + 48f, inner.width, 22f), $"Role: {RoleLabel(effectiveRole)}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 72f, inner.width, 44f), $"Allowed: {component?.AllowedRoleSummary(pawn) ?? "Food, Weapons, Medicine, Apparel, Resources, Misc"}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 118f, inner.width, 22f), $"Food stacks: {pawn.inventory?.innerContainer?.Count(IsFoodThing) ?? 0}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 142f, inner.width, 22f), $"Weapon stacks: {pawn.inventory?.innerContainer?.Count(t => t.def.IsWeapon) ?? 0}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 166f, inner.width, 22f), $"Medicine stacks: {pawn.inventory?.innerContainer?.Count(t => t.def.IsMedicine) ?? 0}", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 190f, inner.width, 22f), $"Armor: {Mathf.RoundToInt((pawn.GetStatValue(StatDefOf.ArmorRating_Sharp) + pawn.GetStatValue(StatDefOf.ArmorRating_Blunt)) * 50f)}", MorrowindUiResources.TextMuted);
    }

    private static void DrawInventoryPane(Rect rect, Pawn pawn, MorrowindInventoryState state)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(10f);
        Rect categoryRect = new(inner.x, inner.y, inner.width, CategoryTabsHeight);
        DrawCategoryTabs(categoryRect, state);

        float columnWidth = 82f;
        Rect bodyRect = new(inner.x, categoryRect.yMax + 6f, inner.width, inner.height - CategoryTabsHeight - 20f);
        Rect equippedRect = new(bodyRect.x, bodyRect.y, columnWidth, bodyRect.height);
        Rect gridRect = new(equippedRect.xMax + 8f, bodyRect.y, bodyRect.width - columnWidth - 8f, bodyRect.height);

        List<MorrowindInventoryEntry> equippedEntries = GatherEquippedEntries(pawn, state.activeCategory);
        List<MorrowindInventoryEntry> entries = GatherInventoryEntries(pawn, state.activeCategory);

        DrawEquippedColumn(equippedRect, equippedEntries, state);
        DrawInventoryGrid(gridRect, entries, state, pawn);

        Rect ornamentRect = new(inner.x + 4f, inner.yMax - 8f, inner.width - 8f, 4f);
        DrawBottomRail(ornamentRect);
    }

    private static void DrawCategoryTabs(Rect rect, MorrowindInventoryState state)
    {
        (MorrowindItemCategory category, string label, float width)[] tabs =
        {
            (MorrowindItemCategory.All, "All", 56f),
            (MorrowindItemCategory.Food, "Food", 64f),
            (MorrowindItemCategory.Medicine, "Medicine", 86f),
            (MorrowindItemCategory.Weapons, "Weapons", 82f),
            (MorrowindItemCategory.Apparel, "Apparel", 78f),
            (MorrowindItemCategory.Resources, "Resources", 88f),
            (MorrowindItemCategory.Misc, "Misc", 64f),
        };

        float x = rect.x;
        foreach ((MorrowindItemCategory category, string label, float width) in tabs)
        {
            Rect tabRect = new(x, rect.y, width, rect.height);
            bool active = state.activeCategory == category;
            DrawTab(tabRect, label, active);
            if (Widgets.ButtonInvisible(tabRect))
            {
                state.activeCategory = category;
            }
            x += width + 4f;
        }
    }

    private static List<MorrowindInventoryEntry> GatherInventoryEntries(Pawn pawn, MorrowindItemCategory category)
    {
        IEnumerable<Thing> inventoryThings = pawn.inventory?.innerContainer?.ToList() ?? Enumerable.Empty<Thing>();
        return inventoryThings
            .Where(t => MatchesCategory(t, category))
            .Select(t => new MorrowindInventoryEntry(t, MorrowindSelectionSource.Inventory, false))
            .OrderBy(entry => CategorySortIndex(entry.thing))
            .ThenBy(entry => entry.thing.LabelCap.ToString())
            .ThenByDescending(entry => entry.thing.stackCount)
            .ToList();
    }

    private static List<MorrowindInventoryEntry> GatherEquippedEntries(Pawn pawn, MorrowindItemCategory category)
    {
        return GatherEquippedThings(pawn)
            .Where(t => MatchesCategory(t, category))
            .Select(t => new MorrowindInventoryEntry(t, t is Apparel ? MorrowindSelectionSource.Apparel : MorrowindSelectionSource.Equipment, true))
            .OrderBy(entry => CategorySortIndex(entry.thing))
            .ThenBy(entry => entry.thing.LabelCap.ToString())
            .ToList();
    }

    private static bool MatchesCategory(Thing thing, MorrowindItemCategory category)
    {
        return category switch
        {
            MorrowindItemCategory.All => true,
            MorrowindItemCategory.Food => IsFoodThing(thing),
            MorrowindItemCategory.Medicine => thing.def.IsMedicine,
            MorrowindItemCategory.Weapons => thing.def.IsWeapon,
            MorrowindItemCategory.Apparel => thing is Apparel,
            MorrowindItemCategory.Resources => IsResourceThing(thing),
            MorrowindItemCategory.Misc => !IsFoodThing(thing) && !thing.def.IsMedicine && !thing.def.IsWeapon && thing is not Apparel && !IsResourceThing(thing),
            _ => true,
        };
    }

    private static int CategorySortIndex(Thing thing)
    {
        if (IsFoodThing(thing)) return 0;
        if (thing.def.IsMedicine) return 1;
        if (thing.def.IsWeapon) return 2;
        if (thing is Apparel) return 3;
        if (IsResourceThing(thing)) return 4;
        return 5;
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
            TooltipHandler.TipRegion(cell, thing.LabelCap.ToString());
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
            TooltipHandler.TipRegion(cell, $"[Equipped] {entry.thing.LabelCap}");
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
        TooltipHandler.TipRegion(rect, thing.LabelCap);
        if (Widgets.ButtonInvisible(rect))
        {
            state.Select(thing, source);
        }
    }

    private static void DrawRolesPane(Rect rect, Pawn pawn)
    {
        MorrowindWindowSkin.DrawPanel(rect);
        Rect inner = rect.ContractedBy(10f);
        var component = MorrowindMenusTradingGameComponent.Instance;
        InventoryTraderRole manual = component?.GetManualRoleOverride(pawn) ?? InventoryTraderRole.Auto;
        InventoryTraderRole effective = component?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;

        DrawLabelLeft(new Rect(inner.x, inner.y, inner.width, 24f), "Trading role", MorrowindUiResources.TextPrimary);
        Rect leftButton = new(inner.x, inner.y + 30f, 30f, 28f);
        Rect valueRect = new(inner.x + 36f, inner.y + 28f, inner.width - 72f, 32f);
        Rect rightButton = new(inner.xMax - 30f, inner.y + 30f, 30f, 28f);
        if (DrawActionButton(leftButton, "<"))
        {
            component?.SetManualRoleOverride(pawn, PreviousRole(manual));
        }
        DrawLabelCentered(valueRect, manual == InventoryTraderRole.Auto ? $"Auto: {RoleLabel(effective)}" : $"Manual: {RoleLabel(manual)}", MorrowindUiResources.TextPrimary);
        if (DrawActionButton(rightButton, ">"))
        {
            component?.SetManualRoleOverride(pawn, NextRole(manual));
        }

        Building_TraderSpot nearestSpot = pawn.MapHeld?.listerThings?.AllThings?.OfType<Building_TraderSpot>()
            .Where(s => s.Spawned && s.SpotRole == effective)
            .OrderBy(s => s.PositionHeld.DistanceToSquared(pawn.PositionHeld))
            .FirstOrDefault();

        DrawLabelLeft(new Rect(inner.x, inner.y + 70f, inner.width, 22f), "Allowed pickup categories", MorrowindUiResources.TextPrimary);
        DrawLabelLeft(new Rect(inner.x, inner.y + 94f, inner.width, 22f), "These decide what this pawn auto collects into inventory.", MorrowindUiResources.TextMuted);
        DrawLabelLeft(new Rect(inner.x, inner.y + 116f, inner.width, 22f), nearestSpot == null ? "Trading spot: none nearby" : $"Trading spot: {nearestSpot.LabelCap}", MorrowindUiResources.TextMuted);

        InventoryTraderRole[] roles =
        {
            InventoryTraderRole.Food,
            InventoryTraderRole.Weapons,
            InventoryTraderRole.Medicine,
            InventoryTraderRole.Apparel,
            InventoryTraderRole.Resources,
            InventoryTraderRole.Misc,
        };

        float buttonWidth = (inner.width - 12f) / 2f;
        for (int i = 0; i < roles.Length; i++)
        {
            int row = i / 2;
            int col = i % 2;
            Rect buttonRect = new(inner.x + col * (buttonWidth + 12f), inner.y + 152f + row * 42f, buttonWidth, 34f);
            bool allowed = component?.IsRoleAllowed(pawn, roles[i]) ?? true;
            if (DrawToggleButton(buttonRect, RoleLabel(roles[i]), allowed))
            {
                component?.ToggleRoleAllowed(pawn, roles[i]);
            }
        }

        Rect noteRect = new(inner.x, inner.y + 290f, inner.width, 90f);
        MorrowindWindowSkin.DrawPanel(noteRect, inset: 3f);
        DrawLabelLeft(noteRect.ContractedBy(8f), "Auto uses the pawn's highest skill until you override it. Allowed categories act like personal stockpile filters. If a matching trader spot exists, idle traders gather there between normal needs and jobs.", MorrowindUiResources.TextMuted);
    }

    private static bool DrawToggleButton(Rect rect, string label, bool enabledState)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 2f);
        if (enabledState)
        {
            MorrowindWindowSkin.DrawEquippedOutline(rect);
        }
        DrawLabelCentered(rect, enabledState ? $"{label}: On" : $"{label}: Off", enabledState ? MorrowindUiResources.TextPrimary : MorrowindUiResources.TextMuted);
        return Widgets.ButtonInvisible(rect);
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
        GUI.enabled = selectedThing != null;
        if (DrawActionButton(dropRect, "Drop")) DropSelectedThing(pawn, state);
        if (DrawActionButton(primaryRect, PrimaryActionLabel(selectedThing, state.selectionSource))) PerformPrimaryAction(pawn, state);
        GUI.enabled = true;
    }

    private static bool DrawActionButton(Rect rect, string label)
    {
        MorrowindWindowSkin.DrawPanel(rect, inset: 2f);
        DrawLabelCentered(rect, label, GUI.enabled ? MorrowindUiResources.TextPrimary : MorrowindUiResources.TextMuted);
        return Widgets.ButtonInvisible(rect);
    }

    private static string PrimaryActionLabel(Thing selectedThing, MorrowindSelectionSource source)
    {
        if (selectedThing == null) return "Use";
        return source switch
        {
            MorrowindSelectionSource.Inventory when selectedThing is Apparel => "Wear",
            MorrowindSelectionSource.Inventory when selectedThing.def != null && selectedThing.def.IsWeapon => "Equip",
            MorrowindSelectionSource.Inventory => "Use",
            MorrowindSelectionSource.Equipment => "Unequip",
            MorrowindSelectionSource.Apparel => "Unequip",
            _ => "Use",
        };
    }

    private static Thing ResolveSelection(Pawn pawn, MorrowindInventoryState state)
    {
        if (pawn == null || state.selectedThingId < 0) return null;
        return state.selectionSource switch
        {
            MorrowindSelectionSource.Inventory => pawn.inventory?.innerContainer?.FirstOrDefault(t => t.thingIDNumber == state.selectedThingId),
            MorrowindSelectionSource.Equipment => pawn.equipment?.AllEquipmentListForReading?.FirstOrDefault(t => t.thingIDNumber == state.selectedThingId),
            MorrowindSelectionSource.Apparel => pawn.apparel?.WornApparel?.FirstOrDefault(t => t.thingIDNumber == state.selectedThingId),
            _ => null,
        };
    }

    private static void PerformPrimaryAction(Pawn pawn, MorrowindInventoryState state)
    {
        Thing selectedThing = ResolveSelection(pawn, state);
        if (pawn == null || selectedThing == null) return;
        switch (state.selectionSource)
        {
            case MorrowindSelectionSource.Inventory:
                if (selectedThing is Apparel apparel)
                {
                    if (pawn.inventory?.innerContainer?.Remove(apparel) == true) pawn.apparel?.Wear(apparel, false, false);
                }
                else if (selectedThing.def != null && selectedThing.def.IsWeapon && selectedThing is ThingWithComps equippable)
                {
                    ThingWithComps primary = pawn.equipment?.Primary;
                    if (primary != null && pawn.inventory?.innerContainer != null) pawn.equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer);
                    if (pawn.inventory?.innerContainer?.Remove(equippable) == true) pawn.equipment?.AddEquipment(equippable);
                }
                break;
            case MorrowindSelectionSource.Equipment:
                if (selectedThing is ThingWithComps gear && pawn.inventory?.innerContainer != null) pawn.equipment?.TryTransferEquipmentToContainer(gear, pawn.inventory.innerContainer);
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
        return thing?.def?.ingestible != null;
    }

    private static bool IsResourceThing(Thing thing)
    {
        if (thing?.def == null)
        {
            return false;
        }

        return thing.def.IsStuff || (thing.def.EverHaulable && thing.def.category == ThingCategory.Item && thing.def.stackLimit > 1 && !IsFoodThing(thing) && !thing.def.IsMedicine && !thing.def.IsWeapon && thing is not Apparel);
    }

    private static void DrawThingIcon(Rect rect, Thing thing)
    {
        Texture icon = thing.def?.uiIcon ?? BaseContent.BadTex;
        Color oldColor = GUI.color;
        GUI.color = thing.DrawColor;
        GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = oldColor;
    }

    private static InventoryTraderRole NextRole(InventoryTraderRole role)
    {
        InventoryTraderRole[] roles = (InventoryTraderRole[])Enum.GetValues(typeof(InventoryTraderRole));
        int index = Array.IndexOf(roles, role);
        return roles[(index + 1) % roles.Length];
    }

    private static InventoryTraderRole PreviousRole(InventoryTraderRole role)
    {
        InventoryTraderRole[] roles = (InventoryTraderRole[])Enum.GetValues(typeof(InventoryTraderRole));
        int index = Array.IndexOf(roles, role);
        return roles[(index - 1 + roles.Length) % roles.Length];
    }

    private static string RoleLabel(InventoryTraderRole role)
    {
        return role switch
        {
            InventoryTraderRole.Auto => "Auto",
            InventoryTraderRole.None => "None",
            InventoryTraderRole.Food => "Food",
            InventoryTraderRole.Weapons => "Weapons",
            InventoryTraderRole.Medicine => "Medicine",
            InventoryTraderRole.Apparel => "Apparel",
            InventoryTraderRole.Resources => "Resources",
            InventoryTraderRole.Misc => "Misc",
            _ => role.ToString(),
        };
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
