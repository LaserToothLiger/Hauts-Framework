using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsFramework
{
    /*rimworld's various letter defs don't necessarily correspond to whether an incident is beneficial or detrimental to the player. Aurora and ambrosia sprout are blue, and obviously good for the player;
     * raids are red and obviously bad. But some beneficial events e.g. ship chunks giving you free steel and components or meteors (well... usually beneficial) aren't blue. It's not exactly a perfect fit, is what I'm saying.
     * So, for my mods with mechanics which cause and-or block 'good' or 'bad' incidents, they're operating off of these two lists.
     * You can specify if something belongs in either list (or even both) with this DME, as well as whether it should be 'makeable' (MakeGoodEvent and MakeBadEvent won't make incidents whose def isn't makeable) and what "impact" it has.
     * impact 0: for incidents that USUALLY don't mean anything to the player. examples include Animal Self-tame (most of the time you're getting an unimpressive or unnecessary animal), Aurora (most of your colonists aren't up to see it and it doesn't impact mood that much for that long),
     *      or Zzt (unless you construct your base really badly and rely on a lot of batteries, this is barely an inconvenience)
     * impact 1: better than 0, but bad impact-1 events rarely possess the power to meaningfully threaten your colony and good impact-1 events rarely provide advantages you didn't already have easy access to. A bad-example would be Solar Flare, which is undoubtedly impactful but rarely capable
     *      of causing substantial losses of property, life, etc. A good-example would be Ship Chunks, which provide a decent amount of valuable-but-far-from-rare resources
     * impact 2: bad- or good-events that meaningfully threaten or advantage the player. Raids and similar are impact 2 at minimum, due to their capacity for destruction AND murder AND theft. Orbital traders/trade caravans are impact 2, since they can provide so many valuable resources
     * impact 3: bad-events that can trivially annihilate an unprepared player and even threaten well-prepared ones, or good-events that provide unique (or near-unique advantages). I might be overestimating its threat, but I consider the Fleshmass Heart to be the only non-modded impact 3 incident.
     *      OTOH, the ancient mechanitor quest, which furnishes you with a mechlink, is impact 3 due to the rarity and importance of mechlinks.*/
    public class BelongsToEventPool : DefModExtension
    {
        public BelongsToEventPool()
        {
        }
        public bool good = false;
        public bool bad = false;
        public bool makeable = true;
        public int impact = 1;
    }
    //DME for letter defs. Any incident def that hasn't been explicitly assigned BelongsToEventPool in the XML can have one generated for it, if its letterDef has this DME. Each of the fields obviously coresponds to a BelongsToEventPool field
    public class AutoAssignEventPool : DefModExtension
    {
        public AutoAssignEventPool()
        {
        }
        public bool autoGood;
        public bool autoBad;
        public bool autoMakeable;
        public int autoImpact;
    }
    public class GoodAndBadIncidentsUtility
    {
        /*these instantiate a good or bad event.
         * preferentially targets the pawn's current map, if any
         * tickDelay: if >0, the incident is added to the storyteller queue on this delay
         * excludedIncidents: cannot roll any incident in this list*/
        public static void MakeGoodEvent(Pawn p = null, int tickDelay = 0, List<IncidentDef> excludedIncidents = null, bool respectMakeable = true, int minImpact = 0, int maxImpact = 99)
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
            if (respectMakeable)
            {
                for (int i = incidents.Count - 1; i >= 0; i--)
                {
                    BelongsToEventPool btep = incidents[i].GetModExtension<BelongsToEventPool>();
                    if (btep != null && (!btep.makeable || btep.impact < minImpact || btep.impact > maxImpact))
                    {
                        incidents.Remove(incidents[i]);
                    }
                }
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
        public static void MakeBadEvent(Pawn p = null, int tickDelay = 0, List<IncidentDef> excludedIncidents = null, bool respectMakeable = true, int minImpact = 0, int maxImpact = 99)
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
            if (respectMakeable)
            {
                for (int i = incidents.Count - 1; i >= 0; i--)
                {
                    BelongsToEventPool btep = incidents[i].GetModExtension<BelongsToEventPool>();
                    if (btep != null && (!btep.makeable || btep.impact < minImpact || btep.impact > maxImpact))
                    {
                        incidents.Remove(incidents[i]);
                    }
                }
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
