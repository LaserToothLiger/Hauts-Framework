using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    /*There’s a lot of damage types in the game, especially when you’re using a lot of mods. A lot of these types are logically equivalent to each other (as you will see below).
     * This makes creating damage resistances, immunities, or vulnerabilities a difficult chore in XML. This tool makes this simpler.
     * If you want the amount of damage a Thing takes from a given damage def to be multiplied by a certain stat (hereafter the "specific damage factor"), you add this DME to the damage def
     *   and in its factorStats field, you add an entry whose 1) key is the specific damage factor and
     *   2) value multiplies the extent to which [that specific damage factor's difference from 1] for the purposes of determining damage resistance.
     * Various specific damage factor stats are already defined, see StatDefs_DamageTypes.xml*/
    public class SpecificDamageFactorStats : DefModExtension
    {
        public SpecificDamageFactorStats() { }
        public SpecificDamageFactorStats(Dictionary<StatDef,float> dict)
        {
            this.factorStats = dict;
        }
        public Dictionary<StatDef, float> factorStats;
    }
    /*some damagedefs are parents of others in the XML, and their children will inherit all their SDFS, overriding any SDFS you would otherwise xpath into them.
     * This is good most of the time! But, to use a purely non-modded example, Anomaly's ElectricBurn is a child of Flame, so it would only be resisted by heat resist and not electric resist.
     * So sometimes we need to sidestep parent inheritance. Parents SHOULD NOT GET an SDFS, they should get this instead, which wouldn't override any SDFS declared for children.
     * The patch that applies the effects of SDFS first looks for any regular SDFS a damage def has, and ONLY IF it can't find one does it look for an SDFS_FPS.*/
    public class SpecificDamageFactorStats_ForParentStats : DefModExtension
    {
        public SpecificDamageFactorStats_ForParentStats() { }
        public SpecificDamageFactorStats_ForParentStats(Dictionary<StatDef, float> dict)
        {
            this.factorStats = dict;
        }
        public Dictionary<StatDef, float> factorStats;
    }
    /*OUTDATED - previous approach to doing this, which was clunkier to use (only applicable to hediffs rather than any source of stats) and did not 'survive' the Hot Reload Defs dev tool.
     * I have removed all functionality for the below, do not use them*/
    [Obsolete]
    public class DamageFactorGroupDef : Def
    {
        public List<DamageDef> damageDefs;
        public List<DFG_HediffTarget> applyToHediffs;
    }
    //the meat and potatoes of any DFGDef, you assign a HediffStage (at stageIndex) of a HediffDef (hediff) damageFactors for each of the DFGDef's damage defs of a particular magnitude (factor) 
    [Obsolete]
    public class DFG_HediffTarget
    {
        public DFG_HediffTarget() { }
        public HediffDef hediff;
        public int stageIndex;
        public float factor;
    }
}
