using AlphaRandom;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
