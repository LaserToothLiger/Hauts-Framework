using RimWorld;
using System.Collections.Generic;
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
        //these instantiate a good or bad event. The pawn arguments don't do anything (yet), although in the future I might make them preferentially target a given Map if the pawn is there.
        public static void MakeGoodEvent(Pawn p = null)
        {
            List<IncidentDef> incidents = GoodAndBadIncidentsUtility.goodEventPool;
            if (incidents.Count > 0)
            {
                bool incidentFired = false;
                int tries = 0;
                while (!incidentFired && tries <= 50)
                {
                    IncidentDef toTryFiring = incidents.RandomElement<IncidentDef>();
                    IncidentParms parms = null;
                    if (toTryFiring.TargetAllowed(Find.World))
                    {
                        parms = new IncidentParms
                        {
                            target = Find.World
                        };
                    }
                    else if (Find.Maps.Count > 0)
                    {
                        Map mapToHit = Find.Maps.RandomElement<Map>();
                        if (Find.AnyPlayerHomeMap != null && Rand.Value <= 0.5f)
                        {
                            mapToHit = Find.AnyPlayerHomeMap;
                        }
                        parms = new IncidentParms
                        {
                            target = mapToHit
                        };
                    }
                    if (parms != null)
                    {
                        IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(toTryFiring.category, parms.target);
                        incidentParms.forced = true;
                        if (toTryFiring.Worker.CanFireNow(parms))
                        {
                            incidentFired = true;
                            toTryFiring.Worker.TryExecute(parms);
                            break;
                        }
                    }
                    tries++;
                }
            }
        }
        public static void MakeBadEvent(Pawn p = null)
        {
            List<IncidentDef> incidents = GoodAndBadIncidentsUtility.badEventPool;
            if (incidents.Count > 0)
            {
                bool incidentFired = false;
                int tries = 0;
                while (!incidentFired && tries <= 50)
                {
                    IncidentDef toTryFiring = incidents.RandomElement<IncidentDef>();
                    IncidentParms parms = null;
                    if (toTryFiring.TargetAllowed(Find.World))
                    {
                        parms = new IncidentParms
                        {
                            target = Find.World
                        };
                    }
                    else if (Find.Maps.Count > 0)
                    {
                        Map mapToHit = Find.Maps.RandomElement<Map>();
                        if (Find.AnyPlayerHomeMap != null && Rand.Value <= 0.5f)
                        {
                            mapToHit = Find.AnyPlayerHomeMap;
                        }
                        parms = new IncidentParms
                        {
                            target = mapToHit
                        };
                    }
                    if (parms != null)
                    {
                        IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(toTryFiring.category, parms.target);
                        incidentParms.forced = true;
                        if (toTryFiring.Worker.CanFireNow(parms))
                        {
                            incidentFired = true;
                            toTryFiring.Worker.TryExecute(parms);
                            break;
                        }
                    }
                    tries++;
                }
            }
        }
        public static readonly List<IncidentDef> goodEventPool = new List<IncidentDef>() { };
        public static readonly List<IncidentDef> badEventPool = new List<IncidentDef>() { };
    }
}
