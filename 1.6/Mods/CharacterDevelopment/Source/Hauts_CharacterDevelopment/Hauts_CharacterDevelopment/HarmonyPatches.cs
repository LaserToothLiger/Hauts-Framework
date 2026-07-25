using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Linq;
using Verse;
using WantsAndQuirks;

namespace Hauts_CharacterDevelopment
{
    [StaticConstructorOnStartup]
    public class Hauts_CharacterDevelopment
    {
        private static readonly Type patchType = typeof(Hauts_CharacterDevelopment);
        static Hauts_CharacterDevelopment()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsbrainwashchair.main");
            harmony.Patch(AccessTools.Method(typeof(RewardWorker_RandomTrait), nameof(RewardWorker_RandomTrait.OnAcquired)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsRandomTrait_OnAcquirePrefix)));
            harmony.Patch(AccessTools.Method(typeof(RewardWorker_RemoveTrait), nameof(RewardWorker_RemoveTrait.CanBestowOn)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsRemoveTrait_CanBestowOnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(RewardWorker_RemoveTrait), nameof(RewardWorker_RemoveTrait.OnAcquired)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsRemoveTrait_OnAcquirePrefix)));
        }
        /*GAIN RANDOM TRAIT
         * don't grant excise trait exempt traits randomly. "CONGRATULATIONS YER A SHELLCASKET NOW. SUCKS TO SUCK." cmon chief*/
        public static bool HautsRandomTrait_OnAcquirePrefix(Pawn pawn)
        {
            TraitDef traitDef = DefDatabase<TraitDef>.AllDefsListForReading.Where((TraitDef t) => !pawn.story.traits.HasTrait(t) && !t.HasModExtension<ExciseTraitExempt>() && t.GetGenderSpecificCommonality(pawn.gender) > 0f).RandomElementWithFallback(null);
            if (traitDef != null)
            {
                int num = PawnGenerator.RandomTraitDegree(traitDef);
                pawn.story.traits.GainTrait(new Trait(traitDef, num, false), false);
                Messages.Message("WQ_TraitGained".Translate(pawn.Named("PAWN"), traitDef.label), pawn, MessageTypeDefOf.PositiveEvent, true);
            }
            return false;
        }
        /*REMOVE RANDOM TRAIT
         * excise-trait exempt traits don't count as removable traits. This is because they shouldn't be removed.*/
        public static void HautsRemoveTrait_CanBestowOnPostfix(Pawn pawn, ref bool __result)
        {
            if (__result && pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (!t.Suppressed && !t.def.HasModExtension<ExciseTraitExempt>() && t.def.GetGenderSpecificCommonality(pawn.gender) > 0f)
                    {
                        return;
                    }
                }
                __result = false;
            }
        }
        //don't remove excise trait exempt traits. "Wow, my shellcasketism was cured by the power of positive thinking!" ...
        public static bool HautsRemoveTrait_OnAcquirePrefix(Pawn pawn)
        {
            Trait trait = pawn.story.traits.allTraits.Where((Trait t) => !t.Suppressed && !t.def.HasModExtension<ExciseTraitExempt>() && t.def.GetGenderSpecificCommonality(pawn.gender) > 0f).RandomElementWithFallback(null);
            if (trait != null)
            {
                pawn.story.traits.RemoveTrait(trait, false);
                Messages.Message("WQ_TraitRemoved".Translate(pawn.Named("PAWN"), trait.Label), pawn, MessageTypeDefOf.PositiveEvent, true);
            }
            return false;
        }
    }
}
