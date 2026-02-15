using Brainwash;
using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Hauts_BrainwashChair
{
    [StaticConstructorOnStartup]
    public static class Hauts_BrainwashChair
    {
        private static readonly Type patchType = typeof(Hauts_BrainwashChair);
        static Hauts_BrainwashChair()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsbrainwashchair.main");
            harmony.Patch(AccessTools.Constructor(typeof(Window_ChangePersonality), new[]{typeof(CompChangePersonality), typeof(Action) }),
                            postfix: new HarmonyMethod(patchType, nameof(HautsWindow_ChangePersonalityPostfix)));
            harmony.Patch(AccessTools.Method(typeof(JobDriver_StartBrainwashTelevision), nameof(JobDriver_StartBrainwashTelevision.BrainwashEffect)),
                            prefix: new HarmonyMethod(patchType, nameof(HautsBrainwashEffectPrefix)));
        }
        public static void HautsWindow_ChangePersonalityPostfix(Window_ChangePersonality __instance, CompChangePersonality comp)
        {
            __instance.alltraits = new List<TraitEntry>();
            foreach (TraitDef traitDef in DefDatabase<TraitDef>.AllDefs)
            {
                if (!comp.Props.traitsToExclude.Contains(traitDef) && !HautsUtility.IsExciseTraitExempt(traitDef) && traitDef.GetGenderSpecificCommonality(comp.pawn.gender) > 0f)
                {
                    for (int i = 0; i < traitDef.degreeDatas.Count; i++)
                    {
                        __instance.alltraits.Add(new TraitEntry
                        {
                            traitDef = traitDef,
                            degree = traitDef.degreeDatas[i].degree
                        });
                    }
                }
            }
        }
        public static bool HautsBrainwashEffectPrefix(JobDriver_StartBrainwashTelevision __instance)
        {
            CompChangePersonality comp = __instance.pawn.GetComp<CompChangePersonality>();
            List<BackstoryDef> backstoriesToSet = comp.backstoriesToSet;
            for (int i = 0; i < backstoriesToSet.Count; i++)
            {
                BackstoryDef backstoryDef = backstoriesToSet[i];
                if (i == 0)
                {
                    __instance.pawn.story.Childhood = backstoryDef;
                } else {
                    __instance.pawn.story.Adulthood = backstoryDef;
                }
            }
            List<TraitEntry> traitsToSet = comp.traitsToSet;
            for (int j = __instance.pawn.story.traits.allTraits.Count - 1; j >= 0; j--)
            {
                Trait trait = __instance.pawn.story.traits.allTraits[j];
                if (trait.sourceGene == null && trait.def.canBeSuppressed && !HautsUtility.IsExciseTraitExempt(trait.def))
                {
                    __instance.pawn.story.traits.RemoveTrait(trait, false);
                }
            }
            for (int k = 0; k < 4; k++)
            {
                TraitEntry traitEntry = ((traitsToSet.Count > k) ? traitsToSet[k] : null);
                if (traitEntry != null)
                {
                    __instance.pawn.story.traits.GainTrait(new Trait(traitEntry.traitDef, traitEntry.degree, false), false);
                }
            }
            foreach (SkillEntry skillEntry in comp.skillsToSet)
            {
                SkillRecord skill = __instance.pawn.skills.GetSkill(skillEntry.skillDef);
                skill.Level = skillEntry.level;
                skill.passion = skillEntry.passion;
                skill.xpSinceLastLevel = skill.XpRequiredForLevelUp / 2f;
            }
            if (comp.reduceCertainty)
            {
                __instance.pawn.ideo.OffsetCertainty(-1f);
            }
            List<Hediff> hediffsToRemove = comp.hediffsToRemove;
            if (hediffsToRemove != null)
            {
                foreach (Hediff hediff in hediffsToRemove)
                {
                    __instance.pawn.health.RemoveHediff(hediff);
                }
            }
            __instance.pawn.health.AddHediff(HediffDefOf.CatatonicBreakdown, null, null, null);
            Messages.Message("Brainwash_PawnHavingBreakdown".Translate(__instance.pawn.Named("PAWN")), __instance.pawn, MessageTypeDefOf.CautionInput, true);
            return false;
        }
    }
}
