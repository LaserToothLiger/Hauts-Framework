using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    /*There’s a lot of damage types in the game, especially when you’re using a lot of mods. A lot of these types are logically equivalent to each other (as you will see below).
     * This makes creating damage resistances, immunities, or vulnerabilities a difficult chore in XML. DFGDefs make this simpler.
     * You specify a list of damage defs, you target whatever hediffs you think should have damage factors for ALL of those defs (and for each such hediff you specify the magnitude of those damage factors),
     * and when the game starts up the desired damage factors will get written into those defs.
     * These changes get erased when you use the Hot Reload Defs tool. I tried patching this at one point, but the patch didn't work. I'll come back to it later since this is rarely an issue in regular play
     * Various DFGs are already defined, see DamageFactorGroupDefs.xml*/
    public class DamageFactorGroupDef : Def
    {
        public List<DamageDef> damageDefs;
        public List<DFG_HediffTarget> applyToHediffs;
    }
    //the meat and potatoes of any DFGDef, you assign a HediffStage (at stageIndex) of a HediffDef (hediff) damageFactors for each of the DFGDef's damage defs of a particular magnitude (factor) 
    public class DFG_HediffTarget
    {
        public DFG_HediffTarget() { }
        public HediffDef hediff;
        public int stageIndex;
        public float factor;
    }
}
