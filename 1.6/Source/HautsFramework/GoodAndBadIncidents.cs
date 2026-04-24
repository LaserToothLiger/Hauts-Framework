using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsFramework
{
    /*rimworld's various letter defs don't necessarily correspond to whether an incident is beneficial or detrimental to the player. Aurora and ambrosia sprout are blue, and obviously good for the player;
     raids are red and obviously bad. But some beneficial events e.g. ship chunks giving you free steel and components or meteors (well... usually beneficial) aren't blue. It's not exactly a perfect fit,
    is what I'm saying.
    So, for my mods with mechanics which cause and-or block 'good' or 'bad' incidents, they're operating off of these two lists.
    You can specify if something belongs in either list (or even both, although I can't think of an event off the top of my head that deserves to belong in both other than maybe meteor) with this DME.*/
    public class BelongsToEventPool : DefModExtension
    {
        public BelongsToEventPool()
        {
        }
        public bool good = false;
        public bool bad = false;
    }
    public class GoodAndBadIncidentsUtility
    {
        /*these instantiate a good or bad event.
         * preferentially targets the pawn's current map, if any
         * tickDelay: if >0, the incident is added to the storyteller queue on this delay
         * excludedIncidents: cannot roll any incident in this list*/
        public static void MakeGoodEvent(Pawn p = null, int tickDelay = 0, List<IncidentDef> excludedIncidents = null)
        {
            IIncidentTarget m = (p != null && p.MapHeld != null) ? p.MapHeld : Find.AnyPlayerHomeMap;
            if (m == null)
            {
                m = Find.World;
            }
            IncidentParms incidentParms = new IncidentParms
            {
                target = m,
                forced = true,
                points = StorytellerUtility.DefaultThreatPointsNow(m),
            };
            List<IncidentDef> incidents;
            if (excludedIncidents.NullOrEmpty())
            {
                incidents = GoodAndBadIncidentsUtility.goodEventPool.Where((IncidentDef id) => id.Worker.CanFireNow(incidentParms)).ToList();
            } else {
                incidents = GoodAndBadIncidentsUtility.goodEventPool.Where((IncidentDef id) => !excludedIncidents.Contains(id) && id.Worker.CanFireNow(incidentParms)).ToList();
            }
            if (incidents.Count > 0)
            {
                bool incidentFired = false;
                int tries = 0;
                while (!incidentFired && tries <= 50)
                {
                    IncidentDef toTryFiring = incidents.RandomElement<IncidentDef>();
                    if (toTryFiring.Worker.CanFireNow(incidentParms))
                    {
                        incidentFired = true;
                        if (tickDelay > 0)
                        {
                            Find.Storyteller.incidentQueue.Add(toTryFiring, Find.TickManager.TicksGame + tickDelay, incidentParms, 60000);
                        } else {
                            toTryFiring.Worker.TryExecute(incidentParms);
                        }
                        break;
                    }
                    tries++;
                }
            }
        }
        public static void MakeBadEvent(Pawn p = null, int tickDelay = 0, List<IncidentDef> excludedIncidents = null)
        {
            IIncidentTarget m = (p != null && p.MapHeld != null) ? p.MapHeld : Find.AnyPlayerHomeMap;
            if (m == null)
            {
                m = Find.World;
            }
            IncidentParms incidentParms = new IncidentParms
            {
                target = m,
                forced = true,
                points = StorytellerUtility.DefaultThreatPointsNow(m),
            };
            List<IncidentDef> incidents;
            if (excludedIncidents.NullOrEmpty())
            {
                incidents = GoodAndBadIncidentsUtility.badEventPool.Where((IncidentDef id) => id.Worker.CanFireNow(incidentParms)).ToList();
            } else {
                incidents = GoodAndBadIncidentsUtility.badEventPool.Where((IncidentDef id) => !excludedIncidents.Contains(id) && id.Worker.CanFireNow(incidentParms)).ToList();
            }
            if (incidents.Count > 0)
            {
                bool incidentFired = false;
                int tries = 0;
                while (!incidentFired && tries <= 50)
                {
                    IncidentDef toTryFiring = incidents.RandomElement<IncidentDef>();
                    if (toTryFiring.Worker.CanFireNow(incidentParms))
                    {
                        incidentFired = true;
                        if (tickDelay > 0)
                        {
                            Find.Storyteller.incidentQueue.Add(toTryFiring, Find.TickManager.TicksGame + tickDelay, incidentParms, 60000);
                        }
                        else
                        {
                            toTryFiring.Worker.TryExecute(incidentParms);
                        }
                        break;
                    }
                    tries++;
                }
            }
        }
        public static readonly List<IncidentDef> goodEventPool = new List<IncidentDef>() { };
        public static readonly List<IncidentDef> badEventPool = new List<IncidentDef>() { };
    }
}
