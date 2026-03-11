using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    /*Instead of hardcoded checking of each thought a pawn has to see if it's one of SEVERAL specific defs, and then going hunting for whether that pawn has a specific trait(s) that affects it
     * (the VTE solution, which I initially copied for HAT before devising this system), ModifyingTraits is an XML-exposed way to query ONCE - do you have ModifyingTraits - and only if so, should we go through its parameters
     * and figure out what traits and/or genes you have that should alter the magnitude of this thought. Or absolute value (or even neg abs) it. This is not easily scalable to use by multiple mods with their own ModifyingTraits for
     * the same thought, as the conventional xpath means of adding DMEs would override each other, I think? It would have to be a more convoluted set of xpath patchconditionals instead ig.*/
    public class ModifyingTraits : DefModExtension
    {
        public ModifyingTraits()
        {
        }
        public Dictionary<TraitDef, float> multiplierTraits = new Dictionary<TraitDef, float>();
        public List<TraitDef> forcePositive;
        public List<TraitDef> forceNegative;
        public Dictionary<GeneDef, float> multiplierGenes = new Dictionary<GeneDef, float>();
        public List<GeneDef> forcePositiveG;
        public List<GeneDef> forceNegativeG;
    }
    //it's here because I use it in multiple mods, waow, just like the stated justification for why I have a framework, waow.
    public class ThoughtWorker_OfSameFaction : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!RelationsUtility.PawnsKnowEachOther(pawn, other) || (this.def.hediff != null && !pawn.health.hediffSet.HasHediff(this.def.hediff)) || other.Faction == null || pawn.Faction == null || other.Faction != pawn.Faction)
            {
                return false;
            }
            return true;
        }
    }
}
