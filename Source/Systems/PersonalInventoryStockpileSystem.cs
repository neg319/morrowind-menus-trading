using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using MorrowindMenusTrading.Components;

namespace MorrowindMenusTrading.Systems;

public static class PersonalInventoryStockpileSystem
{
    public static void TickMap(Map map)
    {
        if (map == null || !MorrowindMenusTradingMod.Settings.personalStockpileMode)
        {
            return;
        }

        List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned
            .Where(IsValidStockpilePawn)
            .ToList();

        if (colonists.Count == 0)
        {
            return;
        }

        TryAutoStashAllowedItems(map, colonists);
        TryConsolidateInventoriesByRole(colonists);

        if (MorrowindMenusTradingMod.Settings.autoShareFood)
        {
            TryShareFood(colonists);
        }

        if (MorrowindMenusTradingMod.Settings.autoShareWeapons)
        {
            TryShareWeapons(colonists);
        }

        if (MorrowindMenusTradingMod.Settings.autoShareMedicine)
        {
            TryShareMedicine(colonists);
        }

        TryStageConstructionResources(map, colonists);
    }

    public static int CountThingInInventory(Pawn pawn, ThingDef def)
    {
        if (pawn?.inventory?.innerContainer == null || def == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Thing thing in pawn.inventory.innerContainer)
        {
            if (thing.def == def)
            {
                count += thing.stackCount;
            }
        }

        return count;
    }

    private static void TryAutoStashAllowedItems(Map map, List<Pawn> colonists)
    {
        int moved = 0;
        int transferLimit = Mathf.Max(4, MorrowindMenusTradingMod.Settings.personalStockpileTransferBatch);
        List<Thing> allThings = map.listerThings.AllThings
            .Where(t => ShouldStash(t, map))
            .OrderByDescending(t => t.MarketValue)
            .ToList();

        foreach (Thing thing in allThings)
        {
            Pawn carrier = FindBestCarrier(colonists, thing);
            if (carrier == null)
            {
                continue;
            }

            float remainingMass = RemainingMass(carrier);
            float massPerItem = Mathf.Max(0.01f, SafeMass(thing));
            int takeCount = Mathf.Min(thing.stackCount, Mathf.Max(1, Mathf.FloorToInt(remainingMass / massPerItem)));
            if (takeCount <= 0)
            {
                continue;
            }

            Thing taken = thing.stackCount > takeCount ? thing.SplitOff(takeCount) : thing;
            if (carrier.inventory.innerContainer.TryAdd(taken))
            {
                moved++;
                if (MorrowindMenusTradingMod.Settings.showInventoryTradeMessages)
                {
                    Messages.Message($"{carrier.LabelShortCap} stores {taken.LabelNoCount} in personal stockpile.", carrier, MessageTypeDefOf.NeutralEvent, false);
                }

                if (moved >= transferLimit)
                {
                    return;
                }
            }
            else if (!taken.Destroyed && !taken.Spawned)
            {
                GenPlace.TryPlaceThing(taken, carrier.PositionHeld, carrier.MapHeld, ThingPlaceMode.Near);
            }
        }
    }

    private static void TryConsolidateInventoriesByRole(List<Pawn> colonists)
    {
        foreach (Pawn donor in colonists)
        {
            List<Thing> snapshot = donor.inventory?.innerContainer?.ToList() ?? new List<Thing>();
            foreach (Thing thing in snapshot)
            {
                MorrowindMenusTrading.Components.InventoryTraderRole preferredRole = RoleForThing(thing);
                if (preferredRole == InventoryTraderRole.None)
                {
                    continue;
                }

                Pawn bestReceiver = colonists
                    .Where(p => p != donor && WantsThing(p, thing) && RemainingMass(p) >= SafeMass(thing))
                    .OrderByDescending(p => CarrierScore(p, thing))
                    .ThenBy(p => p.PositionHeld.DistanceToSquared(donor.PositionHeld))
                    .FirstOrDefault();

                if (bestReceiver == null)
                {
                    continue;
                }

                if (CarrierScore(bestReceiver, thing) <= CarrierScore(donor, thing))
                {
                    continue;
                }

                int moveCount = SuggestedTransferCount(donor, thing, preferredRole);
                if (moveCount <= 0)
                {
                    continue;
                }

                TransferThingBetweenPawns(donor, bestReceiver, thing, moveCount, $"{donor.LabelShortCap} passes {thing.LabelNoCount} to {bestReceiver.LabelShortCap}.");
            }
        }
    }

    private static int SuggestedTransferCount(Pawn donor, Thing thing, MorrowindMenusTrading.Components.InventoryTraderRole preferredRole)
    {
        if (thing == null)
        {
            return 0;
        }

        if (preferredRole == MorrowindMenusTrading.Components.InventoryTraderRole.Food)
        {
            int foodStacks = CountFoodItems(donor);
            return foodStacks > 1 ? 1 : 0;
        }

        if (preferredRole == MorrowindMenusTrading.Components.InventoryTraderRole.Weapons)
        {
            bool isEquipped = donor.equipment?.Primary == thing;
            return !isEquipped ? 1 : 0;
        }

        if (thing.def.stackLimit > 1)
        {
            return Mathf.Max(1, thing.stackCount / 2);
        }

        return 1;
    }

    private static void TryShareFood(List<Pawn> colonists)
    {
        foreach (Pawn hungryPawn in colonists.Where(NeedsFoodFromOthers))
        {
            Pawn donor = colonists
                .Where(p => p != hungryPawn && CountFoodItems(p) > 1)
                .OrderByDescending(p => CarrierScoreForRole(p, MorrowindMenusTrading.Components.InventoryTraderRole.Food))
                .ThenBy(p => p.PositionHeld.DistanceToSquared(hungryPawn.PositionHeld))
                .FirstOrDefault();

            if (donor == null)
            {
                continue;
            }

            Thing meal = donor.inventory.innerContainer
                .Where(IsFoodThing)
                .OrderByDescending(FoodScore)
                .FirstOrDefault();

            if (meal == null)
            {
                continue;
            }

            TransferThingBetweenPawns(donor, hungryPawn, meal, 1, $"{donor.LabelShortCap} shares {meal.LabelNoCount} with {hungryPawn.LabelShortCap}.");
        }
    }

    private static void TryShareWeapons(List<Pawn> colonists)
    {
        foreach (Pawn unarmedPawn in colonists.Where(NeedsWeaponFromOthers))
        {
            Pawn donor = colonists
                .Where(p => p != unarmedPawn && CountSpareWeapons(p) > 0)
                .OrderByDescending(p => CarrierScoreForRole(p, MorrowindMenusTrading.Components.InventoryTraderRole.Weapons))
                .ThenBy(p => p.PositionHeld.DistanceToSquared(unarmedPawn.PositionHeld))
                .FirstOrDefault();

            if (donor == null)
            {
                continue;
            }

            Thing weapon = donor.inventory.innerContainer
                .Where(t => t.def.IsWeapon)
                .OrderByDescending(t => t.MarketValue)
                .FirstOrDefault();

            if (weapon == null)
            {
                continue;
            }

            TransferThingBetweenPawns(donor, unarmedPawn, weapon, 1, $"{donor.LabelShortCap} hands {weapon.LabelNoCount} to {unarmedPawn.LabelShortCap}.");
        }
    }

    private static void TryShareMedicine(List<Pawn> colonists)
    {
        foreach (Pawn hurtPawn in colonists.Where(NeedsMedicineFromOthers))
        {
            Pawn donor = colonists
                .Where(p => p != hurtPawn && CountMedicineItems(p) > 0)
                .OrderByDescending(p => CarrierScoreForRole(p, MorrowindMenusTrading.Components.InventoryTraderRole.Medicine))
                .ThenBy(p => p.PositionHeld.DistanceToSquared(hurtPawn.PositionHeld))
                .FirstOrDefault();

            if (donor == null)
            {
                continue;
            }

            Thing med = donor.inventory.innerContainer
                .Where(t => t.def.IsMedicine)
                .OrderByDescending(t => t.GetStatValue(StatDefOf.MedicalPotency))
                .FirstOrDefault();

            if (med == null)
            {
                continue;
            }

            TransferThingBetweenPawns(donor, hurtPawn, med, 1, $"{donor.LabelShortCap} gives {med.LabelNoCount} to {hurtPawn.LabelShortCap}.");
        }
    }

    private static void TryStageConstructionResources(Map map, List<Pawn> colonists)
    {
        foreach (Thing site in map.listerThings.AllThings.Where(IsConstructionSite).Take(18))
        {
            ThingDef buildDef = site.def?.entityDefToBuild as ThingDef;
            if (buildDef == null)
            {
                continue;
            }

            List<ThingDefCountClass> needed = buildDef.costList ?? new List<ThingDefCountClass>();
            foreach (ThingDefCountClass cost in needed)
            {
                if (cost?.thingDef == null || HasNearbyResource(site, cost.thingDef, 3.9f))
                {
                    continue;
                }

                Pawn donor = colonists
                    .Where(p => CountThingInInventory(p, cost.thingDef) > 0)
                    .OrderByDescending(p => CarrierScore(p, cost.thingDef))
                    .FirstOrDefault();
                if (donor == null)
                {
                    continue;
                }

                Thing stack = donor.inventory.innerContainer.FirstOrDefault(t => t.def == cost.thingDef);
                if (stack == null)
                {
                    continue;
                }

                int takeCount = Mathf.Min(stack.stackCount, Mathf.Max(1, cost.count));
                Thing moved = stack.stackCount > takeCount ? stack.SplitOff(takeCount) : stack;
                GenPlace.TryPlaceThing(moved, site.PositionHeld, map, ThingPlaceMode.Near);
                if (MorrowindMenusTradingMod.Settings.showInventoryTradeMessages)
                {
                    Messages.Message($"{donor.LabelShortCap} stages {moved.LabelNoCount} for building.", donor, MessageTypeDefOf.NeutralEvent, false);
                }
            }

            if (buildDef.MadeFromStuff && buildDef.costStuffCount > 0 && site.Stuff != null && !HasNearbyResource(site, site.Stuff, 3.9f))
            {
                Pawn donor = colonists
                    .Where(p => CountThingInInventory(p, site.Stuff) > 0)
                    .OrderByDescending(p => CarrierScore(p, site.Stuff))
                    .FirstOrDefault();
                if (donor == null)
                {
                    continue;
                }

                Thing stuffStack = donor.inventory.innerContainer.FirstOrDefault(t => t.def == site.Stuff);
                if (stuffStack == null)
                {
                    continue;
                }

                int takeCount = Mathf.Min(stuffStack.stackCount, Mathf.Max(1, buildDef.costStuffCount));
                Thing moved = stuffStack.stackCount > takeCount ? stuffStack.SplitOff(takeCount) : stuffStack;
                GenPlace.TryPlaceThing(moved, site.PositionHeld, map, ThingPlaceMode.Near);
                if (MorrowindMenusTradingMod.Settings.showInventoryTradeMessages)
                {
                    Messages.Message($"{donor.LabelShortCap} stages {moved.LabelNoCount} for building.", donor, MessageTypeDefOf.NeutralEvent, false);
                }
            }
        }
    }

    private static bool IsConstructionSite(Thing thing)
    {
        return thing is Blueprint || thing is Frame;
    }

    private static bool HasNearbyResource(Thing site, ThingDef def, float radius)
    {
        if (site?.MapHeld == null || def == null)
        {
            return false;
        }

        foreach (IntVec3 cell in GenRadial.RadialCellsAround(site.PositionHeld, radius, true))
        {
            if (!cell.InBounds(site.MapHeld))
            {
                continue;
            }

            List<Thing> things = cell.GetThingList(site.MapHeld);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == def)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void TransferThingBetweenPawns(Pawn donor, Pawn receiver, Thing thing, int count, string message)
    {
        if (donor?.inventory?.innerContainer == null || receiver?.inventory?.innerContainer == null || thing == null)
        {
            return;
        }

        int moveCount = Mathf.Min(count, thing.stackCount);
        if (moveCount <= 0)
        {
            return;
        }

        Thing moved = thing.stackCount > moveCount ? thing.SplitOff(moveCount) : thing;
        if (!receiver.inventory.innerContainer.TryAdd(moved))
        {
            if (!moved.Spawned)
            {
                GenPlace.TryPlaceThing(moved, receiver.PositionHeld, receiver.MapHeld, ThingPlaceMode.Near);
            }
            return;
        }

        if (MorrowindMenusTradingMod.Settings.showInventoryTradeMessages)
        {
            Messages.Message(message, receiver, MessageTypeDefOf.NeutralEvent, false);
        }
    }

    private static Pawn FindBestCarrier(List<Pawn> colonists, Thing thing)
    {
        float massPerItem = Mathf.Max(0.01f, SafeMass(thing));
        return colonists
            .Where(p => RemainingMass(p) >= massPerItem)
            .OrderByDescending(p => CarrierScore(p, thing))
            .ThenByDescending(RemainingMass)
            .ThenBy(p => p.PositionHeld.DistanceToSquared(thing.PositionHeld))
            .FirstOrDefault();
    }

    private static float CarrierScore(Pawn pawn, Thing thing)
    {
        if (pawn == null)
        {
            return -999f;
        }

        return CarrierScore(pawn, thing?.def);
    }

    private static float CarrierScore(Pawn pawn, ThingDef def)
    {
        if (pawn == null)
        {
            return -999f;
        }

        InventoryTraderRole role = MorrowindMenusTrading.Components.MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? MorrowindMenusTrading.Components.InventoryTraderRole.Resources;
        InventoryTraderRole preferred = RoleForThing(def);
        float score = role == preferred ? 100f : role == MorrowindMenusTrading.Components.InventoryTraderRole.None ? -50f : 10f;
        score += RemainingMass(pawn) * 0.1f;
        return score;
    }

    private static bool WantsThing(Pawn pawn, Thing thing)
    {
        if (pawn?.inventory?.innerContainer == null || thing == null)
        {
            return false;
        }

        InventoryTraderRole effective = MorrowindMenusTrading.Components.MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? MorrowindMenusTrading.Components.InventoryTraderRole.Resources;
        InventoryTraderRole preferred = RoleForThing(thing);
        if (effective == preferred)
        {
            return true;
        }

        if (preferred == MorrowindMenusTrading.Components.InventoryTraderRole.Food && NeedsFoodFromOthers(pawn))
        {
            return true;
        }

        if (preferred == MorrowindMenusTrading.Components.InventoryTraderRole.Weapons && NeedsWeaponFromOthers(pawn))
        {
            return true;
        }

        if (preferred == MorrowindMenusTrading.Components.InventoryTraderRole.Medicine && NeedsMedicineFromOthers(pawn))
        {
            return true;
        }

        return effective != MorrowindMenusTrading.Components.InventoryTraderRole.None && CountThingInInventory(pawn, thing.def) == 0;
    }

    private static float RemainingMass(Pawn pawn)
    {
        return Math.Max(0f, MassUtility.Capacity(pawn) - MassUtility.GearAndInventoryMass(pawn));
    }

    private static float SafeMass(Thing thing)
    {
        try
        {
            return thing.GetStatValue(StatDefOf.Mass, true);
        }
        catch
        {
            return 1f;
        }
    }

    private static bool IsValidStockpilePawn(Pawn pawn)
    {
        return pawn != null
            && pawn.IsColonistPlayerControlled
            && pawn.inventory?.innerContainer != null
            && !pawn.Dead
            && !pawn.Downed
            && pawn.Spawned;
    }

    private static bool NeedsFoodFromOthers(Pawn pawn)
    {
        return pawn.needs?.food != null
            && pawn.needs.food.CurLevelPercentage < 0.35f
            && CountFoodItems(pawn) == 0;
    }

    private static bool NeedsWeaponFromOthers(Pawn pawn)
    {
        return pawn.equipment?.Primary == null && !pawn.inventory.innerContainer.Any(t => t.def.IsWeapon);
    }

    private static bool NeedsMedicineFromOthers(Pawn pawn)
    {
        return pawn.health?.hediffSet?.BleedRateTotal > 0f && CountMedicineItems(pawn) == 0;
    }

    private static int CountFoodItems(Pawn pawn)
    {
        return pawn.inventory?.innerContainer?.Count(IsFoodThing) ?? 0;
    }

    private static int CountMedicineItems(Pawn pawn)
    {
        return pawn.inventory?.innerContainer?.Count(t => t.def.IsMedicine) ?? 0;
    }

    private static int CountSpareWeapons(Pawn pawn)
    {
        return pawn.inventory?.innerContainer?.Count(t => t.def.IsWeapon) ?? 0;
    }

    private static float FoodScore(Thing thing)
    {
        return (thing.def.ingestible?.CachedNutrition ?? 0f) * Mathf.Max(1, thing.stackCount);
    }

    private static bool IsFoodThing(Thing thing)
    {
        return thing?.def?.ingestible != null;
    }

    private static bool ShouldStash(Thing thing, Map map)
    {
        if (thing == null || !thing.Spawned || thing.Destroyed || thing.def == null)
        {
            return false;
        }

        if (!thing.def.EverHaulable || thing is Pawn || thing.def.category == ThingCategory.Building)
        {
            return false;
        }

        if (thing.IsForbidden(Faction.OfPlayer) || thing.PositionHeld.Fogged(map))
        {
            return false;
        }

        if (thing.def.Minifiable || thing is Corpse || thing.stackCount <= 0)
        {
            return false;
        }

        if (thing.ParentHolder is Pawn_InventoryTracker)
        {
            return false;
        }

        return true;
    }

    public static MorrowindMenusTrading.Components.InventoryTraderRole RoleForThing(Thing thing)
    {
        return RoleForThing(thing?.def);
    }

    public static MorrowindMenusTrading.Components.InventoryTraderRole RoleForThing(ThingDef def)
    {
        if (def == null)
        {
            return MorrowindMenusTrading.Components.InventoryTraderRole.None;
        }

        if (def.IsWeapon)
        {
            return MorrowindMenusTrading.Components.InventoryTraderRole.Weapons;
        }

        if (def.IsMedicine)
        {
            return MorrowindMenusTrading.Components.InventoryTraderRole.Medicine;
        }

        if (def.ingestible != null)
        {
            return MorrowindMenusTrading.Components.InventoryTraderRole.Food;
        }

        if (def.apparel != null)
        {
            return MorrowindMenusTrading.Components.InventoryTraderRole.Apparel;
        }

        if (def.IsStuff || def.EverHaulable && def.category == ThingCategory.Item && def.stackLimit > 1)
        {
            return MorrowindMenusTrading.Components.InventoryTraderRole.Resources;
        }

        return MorrowindMenusTrading.Components.InventoryTraderRole.Misc;
    }

    private static float CarrierScoreForRole(Pawn pawn, MorrowindMenusTrading.Components.InventoryTraderRole role)
    {
        if (pawn == null)
        {
            return -999f;
        }

        MorrowindMenusTrading.Components.InventoryTraderRole effective = MorrowindMenusTrading.Components.MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? MorrowindMenusTrading.Components.InventoryTraderRole.Resources;
        float score = effective == role ? 100f : effective == MorrowindMenusTrading.Components.InventoryTraderRole.None ? -50f : 10f;
        score += RemainingMass(pawn) * 0.1f;
        return score;
    }
}
