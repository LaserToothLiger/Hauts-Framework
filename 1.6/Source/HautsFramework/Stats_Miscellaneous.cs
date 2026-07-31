using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    //apparel wear rate factor can scale w the Crafting skill if the mod option is enabled
    public class SkillNeed_BaseBonusAWRF : SkillNeed_BaseBonus
    {
        public override float ValueFor(Pawn pawn)
        {
            if (Hauts_Mod.settings.apparelWearRateCrafting)
            {
                return base.ValueFor(pawn);
            }
            return 0f;
        }
    }
    //basically for cosmetic purposes, boredom decay incorporates the daily boredom loss from the pawn's current expectations
    public class StatPart_BoredomExpectationBand : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            ExpectationDef edef = ExpectationsUtility.CurrentExpectationFor(pawn);
            if (edef != null)
            {
                val += edef.joyToleranceDropPerDay;
            }
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return null;
            }
            ExpectationDef edef = ExpectationsUtility.CurrentExpectationFor(pawn);
            if (edef != null)
            {
                return "Hauts_StatWorkerExpectationLevel".Translate() + ": " + (ExpectationsUtility.CurrentExpectationFor(pawn).joyToleranceDropPerDay).ToStringPercent();
            }
            return "Hauts_StatWorkerExpectationLevel".Translate() + ": 0.00";
        }
    }
    //breach damage factor with Construction via another mod option
    public class SkillNeed_BaseBonusBDF : SkillNeed_BaseBonus
    {
        public override float ValueFor(Pawn pawn)
        {
            if (Hauts_Mod.settings.breachDamageConstruction)
            {
                return base.ValueFor(pawn);
            }
            return 0f;
        }
    }
    //overdose susceptibility with MEdical via another another mod option
    public class SkillNeed_BaseBonusOS : SkillNeed_BaseBonus
    {
        public override float ValueFor(Pawn pawn)
        {
            if (Hauts_Mod.settings.overdoseSusceptibilityMedicine)
            {
                return base.ValueFor(pawn);
            }
            return 0f;
        }
    }
    public class SurveySpeedsExtraDescriptionPopulator : DefModExtension
    {
        public SurveySpeedsExtraDescriptionPopulator() { }
        public List<ThingDef> affectedThings;
    }
    //like with boredom decay, daily psyfocus regen takes into account the psyfocus decay from current psyfocus band
    public class StatPart_PsyfocusBand : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || pawn.psychicEntropy == null || pawn.GetPsylinkLevel() == 0 || pawn.psychicEntropy.IsCurrentlyMeditating)
            {
                return;
            }
            val -= Pawn_PsychicEntropyTracker.FallRatePerPsyfocusBand[pawn.psychicEntropy.PsyfocusBand];
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || pawn.psychicEntropy == null || pawn.GetPsylinkLevel() == 0 || pawn.psychicEntropy.IsCurrentlyMeditating)
            {
                return null;
            }
            return "Hauts_StatWorkerPsyfocusBand".Translate() + ": " + (-1f * Pawn_PsychicEntropyTracker.FallRatePerPsyfocusBand[pawn.psychicEntropy.PsyfocusBand]).ToStringPercent();
        }
    }
    //currently unused - for a stat that is offset by another stat
    public class StatPart_OwnStatOffset : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            val += pawn.GetStatValue(this.stat);
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || pawn.psychicEntropy == null)
            {
                return null;
            }
            return this.label + ": +" + pawn.GetStatValue(this.stat).ToStringPercent();
        }
        private readonly StatDef stat;
        [MustTranslate]
        private readonly string label;
    }
    //currently unused - will remain in this version to stave off backward compatibility issues
    public class HediffCompProperties_MCR_Storage : HediffCompProperties
    {
        public HediffCompProperties_MCR_Storage()
        {
            this.compClass = typeof(HediffComp_MCR_Storage);
        }
    }
    public class HediffComp_MCR_Storage : HediffComp
    {
    }
}
