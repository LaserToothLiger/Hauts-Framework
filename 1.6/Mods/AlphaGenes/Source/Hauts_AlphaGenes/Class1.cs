using AlphaGenes;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                if (t.HasModExtension<ExciseTraitExempt>())
                {
                    bltd.blackListedTraits.Add(t.defName);
                }
            }
        }
    }
}
