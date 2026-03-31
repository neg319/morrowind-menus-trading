using System;
using System.Collections.Generic;
using System.Linq;
using MorrowindMenusTrading.Buildings;
using MorrowindMenusTrading.Components;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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

        TryAssignInventoryFillJobs(map, colonists);
        TryPrioritizeStorageLoads(map, colonists);
        TryFillPawnInventories(map, colonists);
        TryRefillNeedItemsFromStorage(map, colonists);
        TryLoadRoleItemsFromStorage(map, colonists);
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

        if (MorrowindMenusTradingMod.Settings.useTraderSpots)
        {
            TryDirectTradersToSpots(map, colonists);
        }
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


    private static void TryPrioritizeStorageLoads(Map map, List<Pawn> colonists)
    {
        int moved = 0;
        int transferLimit = Mathf.Max(18, MorrowindMenusTradingMod.Settings.personalStockpileTransferBatch);
        List<Pawn> orderedPawns = colonists
            .OrderByDescending(PawnFillPriority)
            .ThenByDescending(RemainingMass)
            .ToList();

        foreach (Pawn pawn in orderedPawns)
        {
            moved += TryFillSinglePawnInventory(map, pawn, transferLimit - moved, storageOnly: true);
            if (moved >= transferLimit)
            {
                return;
            }
        }
    }

    private static void TryFillPawnInventories(Map map, List<Pawn> colonists)
    {
        int moved = 0;
        int transferLimit = Mathf.Max(16, MorrowindMenusTradingMod.Settings.personalStockpileTransferBatch * 2);
        List<Pawn> orderedPawns = colonists
            .OrderByDescending(PawnFillPriority)
            .ThenBy(p => p.IsHashIntervalTick(60) ? 0 : 1)
            .ToList();

        foreach (Pawn pawn in orderedPawns)
        {
            moved += TryFillSinglePawnInventory(map, pawn, transferLimit - moved);
            if (moved >= transferLimit)
            {
                return;
            }
        }
    }


    private static void TryAssignInventoryFillJobs(Map map, List<Pawn> colonists)
    {
        JobDef takeInventoryJob = ResolveTakeInventoryJobDef();
        if (takeInventoryJob == null)
        {
            return;
        }

        foreach (Pawn pawn in colonists
                     .OrderByDescending(PawnFillPriority)
                     .ThenByDescending(RemainingMass))
        {
            if (!CanStartInventoryFillJob(pawn) || RemainingMass(pawn) <= 0.05f)
            {
                continue;
            }

            foreach (InventoryTraderRole role in BuildPickupPriority(pawn))
            {
                if (role is InventoryTraderRole.None or InventoryTraderRole.Auto)
                {
                    continue;
                }

                if (!IsRoleActuallyAllowed(pawn, role))
                {
                    continue;
                }

                int desiredUnits = DesiredUnitsForRole(pawn, role);
                int currentUnits = CountRoleUnits(pawn, role);
                if (currentUnits >= desiredUnits)
                {
                    continue;
                }

                Thing candidate = FindBestJobCandidateForRole(map, pawn, role);
                if (candidate == null)
                {
                    continue;
                }

                int desiredCount = DesiredMoveCount(candidate, desiredUnits - currentUnits);
                if (TryStartInventoryFillJob(pawn, candidate, desiredCount, takeInventoryJob))
                {
                    break;
                }
            }
        }
    }

    private static JobDef ResolveTakeInventoryJobDef()
    {
        return DefDatabase<JobDef>.GetNamedSilentFail("TakeInventory")
            ?? DefDatabase<JobDef>.GetNamedSilentFail("TakeToInventory")
            ?? DefDatabase<JobDef>.GetNamedSilentFail("HaulToInventory");
    }

    private static bool CanStartInventoryFillJob(Pawn pawn)
    {
        if (pawn == null || pawn.Drafted || pawn.Downed || pawn.Dead || pawn.InMentalState)
        {
            return false;
        }

        if (pawn.needs?.rest != null && pawn.needs.rest.CurLevelPercentage < 0.12f)
        {
            return false;
        }

        if (pawn.CurJob == null)
        {
            return true;
        }

        if (pawn.CurJob.playerForced)
        {
            return false;
        }

        JobDef def = pawn.CurJob.def;
        if (def == null)
        {
            return true;
        }

        if (def == JobDefOf.Ingest || def == JobDefOf.LayDown || def == JobDefOf.Flee
            || def == JobDefOf.BeatFire || def == JobDefOf.AttackMelee || def == JobDefOf.AttackStatic
            || def == JobDefOf.Capture || def == JobDefOf.Rescue || def == JobDefOf.TendPatient)
        {
            return false;
        }

        return true;
    }

    private static Thing FindBestJobCandidateForRole(Map map, Pawn pawn, InventoryTraderRole role)
    {
        InventoryTraderRole effectiveRole = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
        return EnumerateInventoryCandidates(map)
            .Where(t => t.Spawned)
            .Where(t => RoleForThing(t) == role)
            .Where(t => CanPawnTakeCandidate(pawn, t))
            .OrderByDescending(t => CandidateScoreForPawn(pawn, t, role, effectiveRole, map, storageOnly: false) + 120f)
            .FirstOrDefault();
    }

    private static bool TryStartInventoryFillJob(Pawn pawn, Thing thing, int desiredCount, JobDef takeInventoryJob)
    {
        if (pawn?.jobs == null || thing == null || takeInventoryJob == null || !thing.Spawned)
        {
            return false;
        }

        if (!pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
        {
            return false;
        }

        if (pawn.CurJob != null && pawn.CurJob.def == takeInventoryJob && pawn.CurJob.targetA.HasThing && pawn.CurJob.targetA.Thing == thing)
        {
            return true;
        }

        int moveCount = Mathf.Max(1, Mathf.Min(desiredCount, thing.stackCount));
        Job job = JobMaker.MakeJob(takeInventoryJob, thing);
        job.count = moveCount;
        job.locomotionUrgency = LocomotionUrgency.Jog;
        job.playerForced = false;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        return true;
    }

    private static int TryFillSinglePawnInventory(Map map, Pawn pawn, int remainingMoves, bool storageOnly = false)
    {
        if (remainingMoves <= 0 || pawn?.inventory?.innerContainer == null)
        {
            return 0;
        }

        int moved = 0;
        foreach (InventoryTraderRole role in BuildPickupPriority(pawn))
        {
            if (role is InventoryTraderRole.None or InventoryTraderRole.Auto)
            {
                continue;
            }

            if (!IsRoleActuallyAllowed(pawn, role))
            {
                continue;
            }

            int desiredUnits = DesiredUnitsForRole(pawn, role);
            int currentUnits = CountRoleUnits(pawn, role);
            if (currentUnits >= desiredUnits)
            {
                continue;
            }

            int safety = 0;
            while (currentUnits < desiredUnits && moved < remainingMoves && RemainingMass(pawn) > 0.05f && safety < 12)
            {
                safety++;
                Thing candidate = FindBestCandidateForRole(map, pawn, role, storageOnly);
                if (candidate == null)
                {
                    break;
                }

                int desiredCount = DesiredMoveCount(candidate, desiredUnits - currentUnits);
                string message = $"{pawn.LabelShortCap} stores {candidate.LabelNoCount} in personal inventory.";
                if (!TryMoveThingIntoPawnInventory(pawn, candidate, desiredCount, message))
                {
                    break;
                }

                moved++;
                currentUnits = CountRoleUnits(pawn, role);
            }

            if (moved >= remainingMoves)
            {
                break;
            }
        }

        return moved;
    }

    private static IEnumerable<InventoryTraderRole> BuildPickupPriority(Pawn pawn)
    {
        HashSet<InventoryTraderRole> yielded = new();

        if (NeedsFoodFromOthers(pawn))
        {
            yielded.Add(InventoryTraderRole.Food);
            yield return InventoryTraderRole.Food;
        }

        if (NeedsMedicineFromOthers(pawn) && yielded.Add(InventoryTraderRole.Medicine))
        {
            yield return InventoryTraderRole.Medicine;
        }

        if (NeedsWeaponFromOthers(pawn) && yielded.Add(InventoryTraderRole.Weapons))
        {
            yield return InventoryTraderRole.Weapons;
        }

        InventoryTraderRole effectiveRole = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
        if (yielded.Add(effectiveRole))
        {
            yield return effectiveRole;
        }

        foreach (InventoryTraderRole role in MorrowindMenusTradingGameComponent.AllowedRoles())
        {
            if (yielded.Add(role))
            {
                yield return role;
            }
        }
    }

    private static int DesiredUnitsForRole(Pawn pawn, InventoryTraderRole role)
    {
        bool primaryRole = (MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources) == role;
        int desired = role switch
        {
            InventoryTraderRole.Food => primaryRole ? 14 : 4,
            InventoryTraderRole.Medicine => primaryRole ? 10 : 3,
            InventoryTraderRole.Weapons => primaryRole ? 3 : 1,
            InventoryTraderRole.Apparel => primaryRole ? 5 : 1,
            InventoryTraderRole.Resources => primaryRole ? 220 : 48,
            InventoryTraderRole.Misc => primaryRole ? 32 : 8,
            _ => 0,
        };

        if (role == InventoryTraderRole.Food && NeedsFoodFromOthers(pawn))
        {
            desired = Mathf.Max(desired, 4);
        }

        if (role == InventoryTraderRole.Medicine && NeedsMedicineFromOthers(pawn))
        {
            desired = Mathf.Max(desired, 3);
        }

        if (role == InventoryTraderRole.Weapons && NeedsWeaponFromOthers(pawn))
        {
            desired = Mathf.Max(desired, 1);
        }

        return desired;
    }

    private static int CountRoleUnits(Pawn pawn, InventoryTraderRole role)
    {
        if (pawn?.inventory?.innerContainer == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Thing thing in pawn.inventory.innerContainer)
        {
            if (RoleForThing(thing) != role)
            {
                continue;
            }

            count += StackUnits(thing);
        }

        return count;
    }

    private static int StackUnits(Thing thing)
    {
        if (thing == null)
        {
            return 0;
        }

        return thing.def.stackLimit > 1 ? thing.stackCount : 1;
    }

    private static int DesiredMoveCount(Thing candidate, int neededUnits)
    {
        if (candidate == null)
        {
            return 0;
        }

        if (candidate.def.stackLimit > 1)
        {
            return Mathf.Max(1, Mathf.Min(candidate.stackCount, neededUnits));
        }

        return 1;
    }

    private static Thing FindBestCandidateForRole(Map map, Pawn pawn, InventoryTraderRole role, bool storageOnly = false)
    {
        InventoryTraderRole effectiveRole = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
        return EnumerateInventoryCandidates(map)
            .Where(t => RoleForThing(t) == role)
            .Where(t => CanPawnTakeCandidate(pawn, t))
            .Where(t => !storageOnly || IsStorageBackedSource(t, map))
            .OrderByDescending(t => CandidateScoreForPawn(pawn, t, role, effectiveRole, map, storageOnly))
            .FirstOrDefault();
    }

    private static IEnumerable<Thing> EnumerateInventoryCandidates(Map map)
    {
        HashSet<int> seen = new();

        foreach (Thing thing in map.listerThings.AllThings)
        {
            if (thing == null || !seen.Add(thing.thingIDNumber) || !IsEligibleInventoryCandidate(thing, map))
            {
                continue;
            }

            yield return thing;
        }

        foreach (Thing holderThing in map.listerThings.AllThings)
        {
            if (holderThing is not IThingHolder holder)
            {
                continue;
            }

            foreach (Thing thing in EnumerateHeldThings(holder, seen, map))
            {
                yield return thing;
            }
        }
    }

    private static IEnumerable<Thing> EnumerateHeldThings(IThingHolder holder, HashSet<int> seen, Map map)
    {
        ThingOwner directlyHeld = holder.GetDirectlyHeldThings();
        if (directlyHeld == null)
        {
            yield break;
        }

        foreach (Thing thing in directlyHeld)
        {
            if (thing == null || !seen.Add(thing.thingIDNumber) || !IsEligibleInventoryCandidate(thing, map))
            {
                continue;
            }

            yield return thing;

            if (thing is IThingHolder nested)
            {
                foreach (Thing child in EnumerateHeldThings(nested, seen, map))
                {
                    yield return child;
                }
            }
        }
    }

    private static bool IsEligibleInventoryCandidate(Thing thing, Map map)
    {
        if (thing == null || thing.Destroyed || thing.def == null)
        {
            return false;
        }

        if (!thing.def.EverHaulable || thing is Pawn || thing.def.category == ThingCategory.Building)
        {
            return false;
        }

        if (thing.def.Minifiable || thing is Corpse || thing.stackCount <= 0)
        {
            return false;
        }

        if (thing.ParentHolder is Pawn_InventoryTracker || thing.ParentHolder is Pawn_EquipmentTracker || thing.ParentHolder is Pawn_ApparelTracker)
        {
            return false;
        }

        if (thing.Spawned)
        {
            if (thing.IsForbidden(Faction.OfPlayer) || thing.PositionHeld.Fogged(map))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanPawnTakeCandidate(Pawn pawn, Thing thing)
    {
        if (pawn?.inventory?.innerContainer == null || thing == null)
        {
            return false;
        }

        if (!CanAutoCollect(pawn, thing))
        {
            return false;
        }

        if (!CanReachCandidateSource(pawn, thing))
        {
            return false;
        }

        if (RemainingMass(pawn) < Mathf.Max(0.01f, SafeMass(thing)))
        {
            return false;
        }

        return true;
    }

    private static float CandidateScoreForPawn(Pawn pawn, Thing thing, InventoryTraderRole role, InventoryTraderRole effectiveRole, Map map, bool storageOnly)
    {
        float score = 0f;
        bool fromStorage = IsStorageBackedSource(thing, map);

        if (role == effectiveRole)
        {
            score += 400f;
        }

        if (fromStorage)
        {
            score += storageOnly ? 220f : 140f;
            score -= SourceDistanceSquared(pawn, thing) * 0.05f;
        }
        else if (thing.Spawned)
        {
            score += storageOnly ? 8f : 46f;
            score -= SourceDistanceSquared(pawn, thing) * 0.12f;
        }
        else
        {
            score += 26f;
            score -= SourceDistanceSquared(pawn, thing) * 0.06f;
        }

        if (role == InventoryTraderRole.Food)
        {
            score += FoodScore(thing) * 5f;
        }
        else if (role == InventoryTraderRole.Resources)
        {
            score += Mathf.Min(thing.stackCount * 2, 120);
        }
        else if (role == InventoryTraderRole.Medicine)
        {
            score += thing.MarketValue * 0.25f + thing.stackCount * 6f;
        }
        else if (role == InventoryTraderRole.Weapons)
        {
            score += thing.MarketValue * 0.2f;
        }
        else
        {
            score += thing.stackCount;
        }

        return score;
    }

    private static int SourceDistanceSquared(Pawn pawn, Thing thing)
    {
        IntVec3 sourceCell = SourceCell(thing);
        return sourceCell.IsValid && pawn.Spawned ? pawn.PositionHeld.DistanceToSquared(sourceCell) : 0;
    }

    private static IntVec3 SourceCell(Thing thing)
    {
        if (thing == null)
        {
            return IntVec3.Invalid;
        }

        if (thing.Spawned)
        {
            return thing.PositionHeld;
        }

        if (thing.ParentHolder is Thing ownerThing)
        {
            return ownerThing.PositionHeld;
        }

        return IntVec3.Invalid;
    }


    private static bool IsStorageBackedSource(Thing thing, Map map)
    {
        if (thing == null)
        {
            return false;
        }

        if (thing.Spawned)
        {
            if (thing.PositionHeld.IsValid && map != null && thing.PositionHeld.InBounds(map))
            {
                if (thing.PositionHeld.GetSlotGroup(map) != null)
                {
                    return true;
                }
            }

            Map storageMap = map ?? thing.MapHeld;
            return storageMap != null && thing.PositionHeld.GetEdifice(storageMap) is Building_Storage;
        }

        Thing ownerThing = SourceOwnerThing(thing);
        if (ownerThing == null)
        {
            return false;
        }

        if (ownerThing is Building_Storage)
        {
            return true;
        }

        return ownerThing.Spawned
            && ownerThing.MapHeld == map
            && ownerThing.PositionHeld.IsValid
            && ownerThing.PositionHeld.GetSlotGroup(map) != null;
    }

    private static Thing SourceOwnerThing(Thing thing)
    {
        if (thing?.ParentHolder is Thing ownerThing)
        {
            return ownerThing;
        }

        return null;
    }

    private static bool CanReachCandidateSource(Pawn pawn, Thing thing)
    {
        if (pawn == null || thing == null)
        {
            return false;
        }

        if (!pawn.Spawned)
        {
            return true;
        }

        if (thing.Spawned)
        {
            return pawn.CanReach(thing, PathEndMode.Touch, Danger.Some);
        }

        if (thing.ParentHolder is Thing ownerThing && ownerThing.Spawned)
        {
            return pawn.CanReach(ownerThing, PathEndMode.Touch, Danger.Some);
        }

        IntVec3 cell = SourceCell(thing);
        return !cell.IsValid || pawn.PositionHeld.DistanceToSquared(cell) <= 9999;
    }

    private static void TryAutoStashAllowedItems(Map map, List<Pawn> colonists)
    {
        int moved = 0;
        int transferLimit = Mathf.Max(12, MorrowindMenusTradingMod.Settings.personalStockpileTransferBatch);
        int radius = Mathf.Max(6, MorrowindMenusTradingMod.Settings.personalStockpileSearchRadius);

        List<Thing> allThings = EnumerateInventoryCandidates(map)
            .Where(t => ShouldAutoStashCandidate(t, map))
            .OrderByDescending(t => IsStorageBackedSource(t, map))
            .ThenByDescending(t => CarrierDemandScore(colonists, t))
            .ThenBy(t => NearestEligibleDistanceSquared(colonists, t, radius))
            .ThenByDescending(t => t.stackCount)
            .ToList();

        foreach (Thing thing in allThings)
        {
            Pawn carrier = FindBestCarrier(colonists, thing, radius);
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

            if (!TryMoveThingIntoPawnInventory(carrier, thing, takeCount, $"{carrier.LabelShortCap} stores {thing.LabelNoCount} in personal stockpile."))
            {
                continue;
            }

            moved++;
            if (moved >= transferLimit)
            {
                return;
            }
        }
    }

    private static void TryRefillNeedItemsFromStorage(Map map, List<Pawn> colonists)
    {
        foreach (Pawn hungryPawn in colonists.Where(NeedsFoodFromOthers))
        {
            TryLoadFromMapStorage(map, hungryPawn, InventoryTraderRole.Food, 1, $"{hungryPawn.LabelShortCap} takes food into inventory.");
        }

        foreach (Pawn hurtPawn in colonists.Where(NeedsMedicineFromOthers))
        {
            TryLoadFromMapStorage(map, hurtPawn, InventoryTraderRole.Medicine, 1, $"{hurtPawn.LabelShortCap} takes medicine into inventory.");
        }

        foreach (Pawn unarmedPawn in colonists.Where(NeedsWeaponFromOthers))
        {
            TryLoadFromMapStorage(map, unarmedPawn, InventoryTraderRole.Weapons, 1, $"{unarmedPawn.LabelShortCap} takes a weapon into inventory.");
        }
    }

    private static void TryLoadRoleItemsFromStorage(Map map, List<Pawn> colonists)
    {
        foreach (Pawn pawn in colonists)
        {
            InventoryTraderRole effectiveRole = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
            if (effectiveRole == InventoryTraderRole.None || effectiveRole == InventoryTraderRole.Auto)
            {
                continue;
            }

            if (!IsRoleActuallyAllowed(pawn, effectiveRole))
            {
                continue;
            }

            if (HasEnoughForRole(pawn, effectiveRole))
            {
                continue;
            }

            int desiredCount = effectiveRole == InventoryTraderRole.Resources ? 12 : 2;
            TryLoadFromMapStorage(map, pawn, effectiveRole, desiredCount, $"{pawn.LabelShortCap} loads {RoleLabel(effectiveRole).ToLowerInvariant()} stock into inventory.");
        }
    }

    private static bool HasEnoughForRole(Pawn pawn, InventoryTraderRole role)
    {
        int count = CountRoleUnits(pawn, role);
        return role switch
        {
            InventoryTraderRole.Food => count >= 4,
            InventoryTraderRole.Medicine => count >= 2,
            InventoryTraderRole.Weapons => count >= 1,
            InventoryTraderRole.Apparel => count >= 1,
            InventoryTraderRole.Resources => count >= 24,
            InventoryTraderRole.Misc => count >= 6,
            _ => true,
        };
    }

    private static void TryConsolidateInventoriesByRole(List<Pawn> colonists)
    {
        foreach (Pawn donor in colonists)
        {
            List<Thing> snapshot = donor.inventory?.innerContainer?.ToList() ?? new List<Thing>();
            foreach (Thing thing in snapshot)
            {
                InventoryTraderRole preferredRole = RoleForThing(thing);
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

    private static int SuggestedTransferCount(Pawn donor, Thing thing, InventoryTraderRole preferredRole)
    {
        if (thing == null)
        {
            return 0;
        }

        if (preferredRole == InventoryTraderRole.Food)
        {
            int foodStacks = CountFoodItems(donor);
            return foodStacks > 1 ? 1 : 0;
        }

        if (preferredRole == InventoryTraderRole.Weapons)
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
                .OrderByDescending(p => CarrierScoreForRole(p, InventoryTraderRole.Food))
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
                .OrderByDescending(p => CarrierScoreForRole(p, InventoryTraderRole.Weapons))
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
                .OrderByDescending(p => CarrierScoreForRole(p, InventoryTraderRole.Medicine))
                .ThenBy(p => p.PositionHeld.DistanceToSquared(hurtPawn.PositionHeld))
                .FirstOrDefault();

            if (donor == null)
            {
                continue;
            }

            Thing med = donor.inventory.innerContainer
                .Where(t => t.def.IsMedicine)
                .OrderByDescending(t => t.MarketValue)
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
                    donor = FindCarrierForStorageThing(map, colonists, cost.thingDef);
                    if (donor != null)
                    {
                        TryLoadSpecificDefFromStorage(map, donor, cost.thingDef, Mathf.Max(1, cost.count), $"{donor.LabelShortCap} pulls {cost.thingDef.label} from storage.");
                    }
                }
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
                    donor = FindCarrierForStorageThing(map, colonists, site.Stuff);
                    if (donor != null)
                    {
                        TryLoadSpecificDefFromStorage(map, donor, site.Stuff, Mathf.Max(1, buildDef.costStuffCount), $"{donor.LabelShortCap} pulls {site.Stuff.label} from storage.");
                    }
                }
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

    private static void TryDirectTradersToSpots(Map map, List<Pawn> colonists)
    {
        List<Building_TraderSpot> spots = map.listerThings.AllThings.OfType<Building_TraderSpot>().Where(s => !s.IsForbidden(Faction.OfPlayer)).ToList();
        if (spots.Count == 0)
        {
            return;
        }

        foreach (Pawn pawn in colonists)
        {
            if (!IsAvailableForSpotDuty(pawn))
            {
                continue;
            }

            InventoryTraderRole role = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
            Building_TraderSpot spot = spots
                .Where(s => s.Spawned && s.Map == map && s.SpotRole == role)
                .OrderBy(s => s.PositionHeld.DistanceToSquared(pawn.PositionHeld))
                .FirstOrDefault();

            if (spot == null)
            {
                continue;
            }

            if (pawn.PositionHeld.DistanceToSquared(spot.PositionHeld) <= 2)
            {
                continue;
            }

            if (!pawn.CanReach(spot, PathEndMode.Touch, Danger.Some))
            {
                continue;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, spot);
            job.locomotionUrgency = LocomotionUrgency.Amble;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    private static bool IsAvailableForSpotDuty(Pawn pawn)
    {
        if (pawn == null || pawn.Drafted || pawn.Downed || pawn.CurJob == null)
        {
            return pawn != null && !pawn.Drafted && !pawn.Downed;
        }

        JobDef def = pawn.CurJob.def;
        return def == JobDefOf.Wait
            || def == JobDefOf.Wait_Wander
            || def == JobDefOf.GotoWander
            || def == JobDefOf.Goto;
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

    private static Pawn FindBestCarrier(List<Pawn> colonists, Thing thing, int radius)
    {
        float massPerItem = Mathf.Max(0.01f, SafeMass(thing));
        Pawn nearby = colonists
            .Where(p => RemainingMass(p) >= massPerItem && CanAutoCollect(p, thing) && CanReachCandidateSource(p, thing))
            .Where(p => SourceDistanceSquared(p, thing) <= radius * radius)
            .OrderByDescending(p => CarrierScore(p, thing))
            .ThenByDescending(RemainingMass)
            .ThenBy(p => SourceDistanceSquared(p, thing))
            .FirstOrDefault();

        if (nearby != null)
        {
            return nearby;
        }

        return colonists
            .Where(p => RemainingMass(p) >= massPerItem && CanAutoCollect(p, thing) && CanReachCandidateSource(p, thing))
            .OrderByDescending(p => CarrierScore(p, thing))
            .ThenByDescending(RemainingMass)
            .ThenBy(p => SourceDistanceSquared(p, thing))
            .FirstOrDefault();
    }

    private static Pawn FindCarrierForStorageThing(Map map, List<Pawn> colonists, ThingDef def)
    {
        InventoryTraderRole role = RoleForThing(def);
        return colonists
            .Where(p => IsRoleActuallyAllowed(p, role))
            .OrderByDescending(p => CarrierScore(p, def))
            .FirstOrDefault();
    }

    private static bool TryLoadFromMapStorage(Map map, Pawn pawn, InventoryTraderRole role, int desiredCount, string message)
    {
        Thing candidate = FindBestCandidateForRole(map, pawn, role, storageOnly: true);
        if (candidate == null)
        {
            return false;
        }

        return TryMoveThingIntoPawnInventory(pawn, candidate, desiredCount, message);
    }

    private static bool TryLoadSpecificDefFromStorage(Map map, Pawn pawn, ThingDef def, int desiredCount, string message)
    {
        Thing candidate = EnumerateInventoryCandidates(map)
            .Where(t => t.def == def && CanPawnTakeCandidate(pawn, t))
            .OrderByDescending(t => CandidateScoreForPawn(pawn, t, RoleForThing(def), MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources, map, storageOnly: true))
            .FirstOrDefault();

        if (candidate == null)
        {
            return false;
        }

        return TryMoveThingIntoPawnInventory(pawn, candidate, desiredCount, message);
    }

    private static bool TryMoveThingIntoPawnInventory(Pawn pawn, Thing thing, int desiredCount, string message)
    {
        if (pawn?.inventory?.innerContainer == null || thing == null)
        {
            return false;
        }

        float remainingMass = RemainingMass(pawn);
        float massPerItem = Mathf.Max(0.01f, SafeMass(thing));
        int capacityCount = Mathf.Max(0, Mathf.FloorToInt(remainingMass / massPerItem));
        int moveCount = Mathf.Min(thing.stackCount, Mathf.Min(desiredCount, capacityCount));
        if (moveCount <= 0)
        {
            return false;
        }

        Thing moved = thing.stackCount > moveCount ? thing.SplitOff(moveCount) : thing;
        if (!pawn.inventory.innerContainer.TryAdd(moved))
        {
            if (!moved.Destroyed && !moved.Spawned)
            {
                GenPlace.TryPlaceThing(moved, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
            }
            return false;
        }

        if (MorrowindMenusTradingMod.Settings.showInventoryTradeMessages)
        {
            Messages.Message(message, pawn, MessageTypeDefOf.NeutralEvent, false);
        }

        return true;
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

        InventoryTraderRole role = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
        InventoryTraderRole preferred = RoleForThing(def);
        float score = role == preferred ? 120f : role == InventoryTraderRole.None ? -200f : 10f;
        if (IsRoleActuallyAllowed(pawn, preferred))
        {
            score += 40f;
        }
        score += RemainingMass(pawn) * 0.1f;
        return score;
    }

    private static bool WantsThing(Pawn pawn, Thing thing)
    {
        if (pawn?.inventory?.innerContainer == null || thing == null)
        {
            return false;
        }

        InventoryTraderRole preferred = RoleForThing(thing);
        if (CanAutoCollect(pawn, thing))
        {
            return true;
        }

        if (preferred == InventoryTraderRole.Food && NeedsFoodFromOthers(pawn) && IsRoleActuallyAllowed(pawn, preferred))
        {
            return true;
        }

        if (preferred == InventoryTraderRole.Weapons && NeedsWeaponFromOthers(pawn) && IsRoleActuallyAllowed(pawn, preferred))
        {
            return true;
        }

        if (preferred == InventoryTraderRole.Medicine && NeedsMedicineFromOthers(pawn) && IsRoleActuallyAllowed(pawn, preferred))
        {
            return true;
        }

        return false;
    }

    private static bool CanAutoCollect(Pawn pawn, Thing thing)
    {
        if (pawn?.inventory?.innerContainer == null || thing == null)
        {
            return false;
        }

        InventoryTraderRole preferred = RoleForThing(thing);
        if (preferred == InventoryTraderRole.None)
        {
            return false;
        }

        return IsRoleActuallyAllowed(pawn, preferred);
    }

    private static bool IsRoleActuallyAllowed(Pawn pawn, InventoryTraderRole role)
    {
        return MorrowindMenusTradingGameComponent.Instance?.IsRoleAllowed(pawn, role) == true;
    }

    private static float CarrierDemandScore(List<Pawn> colonists, Thing thing)
    {
        if (thing == null)
        {
            return -999f;
        }

        InventoryTraderRole role = RoleForThing(thing);
        float demand = colonists.Count(p => CanAutoCollect(p, thing)) * 20f;
        demand += colonists.Count(p => (MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(p) ?? InventoryTraderRole.Resources) == role) * 80f;
        if (role == InventoryTraderRole.Food && colonists.Any(NeedsFoodFromOthers))
        {
            demand += 200f;
        }
        if (role == InventoryTraderRole.Weapons && colonists.Any(NeedsWeaponFromOthers))
        {
            demand += 160f;
        }
        if (role == InventoryTraderRole.Medicine && colonists.Any(NeedsMedicineFromOthers))
        {
            demand += 180f;
        }
        return demand;
    }

    private static int NearestEligibleDistanceSquared(List<Pawn> colonists, Thing thing, int radius)
    {
        int best = radius * radius;
        foreach (Pawn pawn in colonists)
        {
            if (!CanAutoCollect(pawn, thing))
            {
                continue;
            }

            int distance = pawn.PositionHeld.DistanceToSquared(thing.PositionHeld);
            if (distance < best)
            {
                best = distance;
            }
        }

        return best;
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

    private static float PawnFillPriority(Pawn pawn)
    {
        float score = 0f;
        if (NeedsFoodFromOthers(pawn))
        {
            score += 500f;
        }
        if (NeedsMedicineFromOthers(pawn))
        {
            score += 420f;
        }
        if (NeedsWeaponFromOthers(pawn))
        {
            score += 300f;
        }

        InventoryTraderRole role = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
        score += role switch
        {
            InventoryTraderRole.Resources => 40f,
            InventoryTraderRole.Food => 30f,
            InventoryTraderRole.Medicine => 25f,
            InventoryTraderRole.Weapons => 20f,
            _ => 10f,
        };

        score += RemainingMass(pawn) * 0.05f;
        return score;
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

        if (thing.ParentHolder is Pawn_InventoryTracker || thing.ParentHolder is Pawn_EquipmentTracker || thing.ParentHolder is Pawn_ApparelTracker)
        {
            return false;
        }

        return true;
    }

    private static bool ShouldAutoStashCandidate(Thing thing, Map map)
    {
        if (thing == null || thing.Destroyed || thing.def == null || thing.stackCount <= 0)
        {
            return false;
        }

        if (!thing.def.EverHaulable || thing is Pawn || thing.def.category == ThingCategory.Building)
        {
            return false;
        }

        if (thing.def.Minifiable || thing is Corpse)
        {
            return false;
        }

        if (thing.ParentHolder is Pawn_InventoryTracker || thing.ParentHolder is Pawn_EquipmentTracker || thing.ParentHolder is Pawn_ApparelTracker)
        {
            return false;
        }

        if (thing.Spawned)
        {
            return ShouldStash(thing, map);
        }

        if (thing.ParentHolder is Thing ownerThing)
        {
            return ownerThing.MapHeld == map && !ownerThing.IsForbidden(Faction.OfPlayer);
        }

        return true;
    }

    public static InventoryTraderRole RoleForThing(Thing thing)
    {
        return RoleForThing(thing?.def);
    }

    public static InventoryTraderRole RoleForThing(ThingDef def)
    {
        if (def == null)
        {
            return InventoryTraderRole.None;
        }

        if (def.IsWeapon)
        {
            return InventoryTraderRole.Weapons;
        }

        if (def.IsMedicine)
        {
            return InventoryTraderRole.Medicine;
        }

        if (def.ingestible != null)
        {
            return InventoryTraderRole.Food;
        }

        if (def.apparel != null)
        {
            return InventoryTraderRole.Apparel;
        }

        if (def.IsStuff || def.EverHaulable && def.category == ThingCategory.Item && def.stackLimit > 1)
        {
            return InventoryTraderRole.Resources;
        }

        return InventoryTraderRole.Misc;
    }

    private static float CarrierScoreForRole(Pawn pawn, InventoryTraderRole role)
    {
        if (pawn == null)
        {
            return -999f;
        }

        InventoryTraderRole effective = MorrowindMenusTradingGameComponent.Instance?.GetEffectiveRole(pawn) ?? InventoryTraderRole.Resources;
        float score = effective == role ? 100f : effective == InventoryTraderRole.None ? -50f : 10f;
        score += RemainingMass(pawn) * 0.1f;
        return score;
    }

    private static string RoleLabel(InventoryTraderRole role)
    {
        return role switch
        {
            InventoryTraderRole.Food => "Food",
            InventoryTraderRole.Weapons => "Weapons",
            InventoryTraderRole.Medicine => "Medicine",
            InventoryTraderRole.Apparel => "Apparel",
            InventoryTraderRole.Resources => "Resources",
            InventoryTraderRole.Misc => "Misc",
            InventoryTraderRole.Auto => "Auto",
            InventoryTraderRole.None => "None",
            _ => role.ToString(),
        };
    }
}
