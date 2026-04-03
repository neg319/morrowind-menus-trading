using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MorrowindMenusTrading;

public static class DebugTraderVisitUtility
{
    public static bool TryForceTraderVisit(out string message, out MessageTypeDef messageType)
    {
        Map map = Find.CurrentMap ?? Find.Maps?.FirstOrDefault(candidate => candidate != null && candidate.IsPlayerHome);
        if (map == null)
        {
            message = "Open a player home map first.";
            messageType = MessageTypeDefOf.RejectInput;
            return false;
        }

        IncidentDef traderIncident = DefDatabase<IncidentDef>.GetNamedSilentFail("TraderCaravanArrival");
        if (traderIncident == null)
        {
            message = "Could not find the TraderCaravanArrival incident.";
            messageType = MessageTypeDefOf.RejectInput;
            return false;
        }

        List<Faction> candidateFactions = Find.FactionManager.AllFactionsListForReading
            .Where(faction => faction != null
                && !faction.IsPlayer
                && !faction.def.hidden
                && !faction.HostileTo(Faction.OfPlayer)
                && faction.def.caravanTraderKinds != null
                && faction.def.caravanTraderKinds.Count > 0)
            .ToList();

        if (candidateFactions.Count == 0)
        {
            message = "No friendly trader factions are available right now.";
            messageType = MessageTypeDefOf.RejectInput;
            return false;
        }

        IncidentParms parms = StorytellerUtility.DefaultParmsNow(traderIncident.category, map);
        parms.target = map;

        Faction chosenFaction = candidateFactions.RandomElement();
        parms.faction = chosenFaction;

        if (parms.traderKind == null && chosenFaction.def.caravanTraderKinds != null && chosenFaction.def.caravanTraderKinds.Count > 0)
        {
            parms.traderKind = chosenFaction.def.caravanTraderKinds.RandomElement();
        }

        if (!traderIncident.Worker.CanFireNow(parms))
        {
            List<Faction> alternateFactions = candidateFactions.Where(faction => faction != chosenFaction).InRandomOrder().ToList();
            bool foundWorkingFaction = false;
            foreach (Faction faction in alternateFactions)
            {
                parms.faction = faction;
                parms.traderKind = faction.def.caravanTraderKinds.RandomElement();
                if (traderIncident.Worker.CanFireNow(parms))
                {
                    chosenFaction = faction;
                    foundWorkingFaction = true;
                    break;
                }
            }

            if (!foundWorkingFaction)
            {
                message = "No trader caravan can fire right now on this map.";
                messageType = MessageTypeDefOf.RejectInput;
                return false;
            }
        }

        if (!traderIncident.Worker.TryExecute(parms))
        {
            message = "Trader caravan incident failed to execute.";
            messageType = MessageTypeDefOf.RejectInput;
            return false;
        }

        string traderKindLabel = parms.traderKind?.label ?? "trader caravan";
        message = $"Forced {chosenFaction.Name} {traderKindLabel} to visit {map.Parent?.LabelCap ?? map.ToString()}.";
        messageType = MessageTypeDefOf.PositiveEvent;
        return true;
    }
}
