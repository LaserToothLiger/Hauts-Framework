using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    /*if you generate a quest very like a mineral lump quest, it doesn't quite generate all the parameters it should.
     * This tag is currently only used by RoyalTItlePermitWorker_GenerateQuest (see PermitWorkers.cs) to ensure proper generation for such a quest
     * Example found in HEMP - the HVMP_MineralLump quest script def.
     * Couldn't this just have been a questnode? Yeah, in retrospect. I did not feel confident messing about with quest structures at the time. Actually, they were completely kicking my ass when I started HEMP.
     *   I still find working on quest scripts time-consuming enough that turning this into a proper questnode is low priority.*/
    public class LumpQuest : DefModExtension
    {
        public LumpQuest() { }
    }
    public static class QuestSetupUtility
    {
        //returns a random map, prioritizing surface-layer, non-underground maps that are player colonies. Maps that fail all three stipulations are ineligible
        public static Map Quest_TryGetMap()
        {
            List<Map> mapCandidates = new List<Map>();
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome && !map.generatorDef.isUnderground && map.Tile.Layer.IsRootSurface)
                {
                    mapCandidates.Add(map);
                }
            }
            if (mapCandidates.Count > 0)
            {
                return mapCandidates.RandomElementWithFallback(null);
            }
            mapCandidates.Clear();
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome && !map.generatorDef.isUnderground)
                {
                    mapCandidates.Add(map);
                }
            }
            if (mapCandidates.Count > 0)
            {
                return mapCandidates.RandomElementWithFallback(null);
            }
            mapCandidates.Clear();
            foreach (Map map in Find.Maps)
            {
                if (!map.generatorDef.isUnderground && map.mapPawns.FreeColonists.Count > 0)
                {
                    mapCandidates.Add(map);
                }
            }
            if (mapCandidates.Count > 0)
            {
                return mapCandidates.RandomElementWithFallback(null);
            }
            return null;
        }
        /*returns either the PlanetTile of a Quest_TryGetMap outcome, or a random starting tile otherwise*/
        public static PlanetTile Quest_TryGetPlanetTile()
        {
            Map m = QuestSetupUtility.Quest_TryGetMap();
            if (m != null)
            {
                return m.Tile;
            }
            return TileFinder.RandomStartingTile();
        }
    }
}
