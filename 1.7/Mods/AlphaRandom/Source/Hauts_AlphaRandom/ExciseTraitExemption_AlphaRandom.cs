using AlphaRandom;
using HautsFramework;
using RimWorld;
using Verse;

namespace Hauts_AlphaRandom
{
    [StaticConstructorOnStartup]
    public class Hauts_AlphaRandom
    {
        static Hauts_AlphaRandom()
        {
            BlackListedTraitsDef bltd = DefDatabase<BlackListedTraitsDef>.GetRandom();
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefs)
            {
                if (t.HasModExtension<ExciseTraitExempt>() || !t.canBeSuppressed)
                {
                    bltd.blackListedTraits.Add(t.defName);
                }
            }
        }
    }
}
