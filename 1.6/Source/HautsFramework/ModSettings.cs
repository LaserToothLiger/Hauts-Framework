using UnityEngine;
using Verse;

namespace HautsFramework
{
    /*there are a few pawn stats in this framework for which you could plausibly advance the argument that they are associated with a specific skill.
     * If you're very good at constructing buildings, wouldn't you also better know how to take them down?
     * Skill at medicine necessarily means understanding dosages, because every medicine is a drug is a poison, and virtually everything a human touches, ingests, or breathes has a medical impact. So wouldn't you be better at taking drugs to avoid ODs?
     * If you're good at tailoring apparel, wouldn't you also know the best practices for extending the life of your personal apparel?
     * If you're really good at dealing with other people, might that not be useful in larcenous pursuits?
     * Should you believe any of these arguments (or simply want some more free power for your colonists) you can turn on mod settings for each of them.
     * These are handled via unique SkillNeeds that only work if the corresponding mod setting is enabled.*/
    public class Hauts_Settings : ModSettings
    {
        public bool doForcedBodyTypes = true;
        public bool apparelWearRateCrafting = false;
        public bool breachDamageConstruction = false;
        public bool overdoseSusceptibilityMedicine = false;
        public bool pilferingStealthSocial = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref doForcedBodyTypes, "doForcedBodyTypes", true);
            Scribe_Values.Look(ref apparelWearRateCrafting, "apparelWearRateCrafting", false);
            Scribe_Values.Look(ref breachDamageConstruction, "breachDamageConstruction", false);
            Scribe_Values.Look(ref overdoseSusceptibilityMedicine, "overdoseSusceptibilityMedicine", false);
            Scribe_Values.Look(ref pilferingStealthSocial, "pilferingStealthSocial", false);
            base.ExposeData();
        }
    }
    public class Hauts_Mod : Mod
    {
        public Hauts_Mod(ModContentPack content) : base(content)
        {
            Hauts_Mod.settings = GetSettings<Hauts_Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Hauts_SettingForcedBodyTypes".Translate(), ref settings.doForcedBodyTypes, "Hauts_TooltipForcedBodyTypes".Translate());
            listingStandard.CheckboxLabeled("Hauts_SettingAWRFC".Translate(), ref settings.apparelWearRateCrafting, "Hauts_TooltipAWRFC".Translate());
            listingStandard.CheckboxLabeled("Hauts_SettingBDFC".Translate(), ref settings.breachDamageConstruction, "Hauts_TooltipBDFC".Translate());
            listingStandard.CheckboxLabeled("Hauts_SettingOSC".Translate(), ref settings.overdoseSusceptibilityMedicine, "Hauts_TooltipOSC".Translate());
            listingStandard.CheckboxLabeled("Hauts_SettingPSC".Translate(), ref settings.pilferingStealthSocial, "Hauts_TooltipPSC".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return "Hauts' Framework";
        }
        public static Hauts_Settings settings;
    }
}
