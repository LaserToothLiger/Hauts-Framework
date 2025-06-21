using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VanillaRacesExpandedSanguophage;
using Verse;

namespace Hauts_VRESanguophage
{
    [StaticConstructorOnStartup]
    public class Hauts_VRESanguophage
    {
        private static readonly Type patchType = typeof(Hauts_VRESanguophage);
        static Hauts_VRESanguophage()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.vresanguophage");
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_CorpsefeederBite), nameof(CompAbilityEffect_CorpsefeederBite.DoBite)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_CorpsefeederBite_DoBitePrefix)));
        }
        public static void Hauts_CorpsefeederBite_DoBitePrefix(Corpse corpse, ref float targetHemogenGain)
        {
            if (corpse.InnerPawn != null)
            {
                targetHemogenGain *= corpse.InnerPawn.GetStatValue(HautsDefOf.Hauts_HemogenContentFactor);
            }
        }
    }
}
