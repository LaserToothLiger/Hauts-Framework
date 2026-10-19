using AlphaGenes;
using HautsFramework;
using RimWorld;
using Verse;

namespace Hauts_AlphaGenes
{
    [StaticConstructorOnStartup]
    public class Hauts_AlphaGenes
    {
        static Hauts_AlphaGenes()
        {
            BlackListedTraitsDef bltd = DefDatabase<BlackListedTraitsDef>.GetRandom();
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefs)
            {
                if (TraitModExtensionUtility.IsExciseTraitExempt(t) || !t.canBeSuppressed)
                {
                    bltd.blackListedTraits.Add(t.defName);
                }
            }
        }
    }
}
