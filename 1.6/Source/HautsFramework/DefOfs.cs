using RimWorld;
using Verse;

namespace HautsFramework
{
    [DefOf]
    public static class HautsDefOf
    {
        static HautsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HautsDefOf));
        }
        public static DamageDef Hauts_SkipFrag;
        public static EffecterDef Hauts_ToxThornsMist;

        public static Hauts_FactionCompDef Hauts_FCHolder;

        public static IncidentDef Hauts_InvestmentReturn;

        public static StatDef Hauts_ApparelWearRateFactor;
        public static StatDef Hauts_OverdoseSusceptibility;
        public static StatDef Hauts_BoredomDropPerDay;
        public static StatDef Hauts_PilferingStealth;
        public static StatDef Hauts_MaxPilferingValue;
        public static StatDef Hauts_PawnAlertLevel;
        public static StatDef Hauts_SkillGainFromRecreation;
        public static StatDef Hauts_CaravanVisibilityOffset;
        public static StatDef Hauts_PersonalCaravanVisibilityFactor;
        public static StatDef Hauts_TrackSize;
        public static StatDef Hauts_JumpRangeFactor;
        [MayRequireIdeology]
        public static StatDef Hauts_IdeoAbilityDurationSelf;
        [MayRequireIdeology]
        public static StatDef Hauts_IdeoThoughtFactor;
        [MayRequireIdeology]
        public static StatDef Hauts_MaxDryadFactor;
        [MayRequireBiotech]
        public static StatDef Hauts_InstructiveAbility;
        public static StatDef Hauts_SpewRangeFactor;
        [MayRequireBiotech]
        public static StatDef Hauts_HemogenContentFactor;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsycastFocusRefund;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsyfocusFromFood;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsyfocusGainOnKill;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsyfocusRegenRate;
        [MayRequireRoyalty]
        public static StatDef Hauts_TierOnePsycastCostOffset;
        [MayRequireRoyalty]
        public static StatDef Hauts_SkipcastRangeFactor;
        public static StatDef Hauts_BreachDamageFactor;
        [MayRequireAnomaly]
        public static StatDef Hauts_EntityDamageFactor;
        public static StatDef Hauts_MeleeArmorPenetration;
        public static StatDef Hauts_RangedArmorPenetration;

        public static ThingDef Hauts_DefaultAuraGraphic;

        [MayRequireIdeology]
        public static ThoughtDef Hauts_FailedConversionByBook;

        public static HediffDef HVT_Spy;
        public static HediffDef Hauts_PsycastLoopBreaker;
        public static HediffDef Hauts_RaisedAlertLevel;

        public static JobDef Hauts_Pickpocket;
    }
}
