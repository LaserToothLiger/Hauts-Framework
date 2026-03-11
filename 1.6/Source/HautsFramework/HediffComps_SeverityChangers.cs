using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    /*Heads-up: I didn't know how to do float? when writing most of these hediffs. While that would certainly look nicer, and it would free up the ability to specifically use -999 as a value for several parameters,
     * this is such a non-issue from a practical perspective that it's all the way at the bottom of my TODO list. It's straight-up not even on the one I have written out.
     * if any of the abilities granted by this hediff are on cooldown, the hediff's severity = initialSeverity + sevBonusPerGrantedAbilityOnCD; otherwise, it's initialSeverity*/
    public class HediffCompProperties_AbilityCooldownSeverity : HediffCompProperties
    {
        public HediffCompProperties_AbilityCooldownSeverity()
        {
            this.compClass = typeof(HediffComp_AbilityCooldownSeverity);
        }
        public float sevBonusPerGrantedAbilityOnCD;
    }
    public class HediffComp_AbilityCooldownSeverity : HediffComp
    {
        public HediffCompProperties_AbilityCooldownSeverity Props
        {
            get
            {
                return (HediffCompProperties_AbilityCooldownSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            float severity = this.parent.def.initialSeverity;
            if (this.parent.AllAbilitiesForReading != null && this.parent.AllAbilitiesForReading.Count > 0)
            {
                foreach (RimWorld.Ability a in this.parent.AllAbilitiesForReading)
                {
                    if (a.OnCooldown)
                    {
                        severity += this.Props.sevBonusPerGrantedAbilityOnCD;
                    }
                }
            }
            this.parent.Severity = severity;
        }
    }
    /*The severity of this hediff is based upon the pawn’s mood/mental break risk tier (the six of which are defined in the base game).
     * severityForMoodless: if the pawn has no mood, this is the severity, ignoring all other fields below
     * activeDuringMentalStates: determines if this effect is still running while the pawn has a mental state ongoing
     * severityIfInMentalState: …otherwise, if the pawn is in a mental state, this is the severity
     * removeFromMoodless: if true and the pawn has no mood meter, this hediff is removed from the pawn
     * extreme|major|minor|neutral|content|happyMBseverity: if the pawn has mood and is at this particular mb tier, this is the severity.*/
    public class HediffCompProperties_BreakRiskSeverity : HediffCompProperties
    {
        public HediffCompProperties_BreakRiskSeverity()
        {
            this.compClass = typeof(HediffComp_BreakRiskSeverity);
        }
        public bool activeDuringMentalStates = true;
        public float severityIfInMentalState = 0.001f;
        public bool removeFromMoodless = false;
        public float severityForMoodless = 0.001f;
        public float extremeMBseverity = 1f;
        public float majorMBseverity = 2f;
        public float minorMBseverity = 3f;
        public float neutralSeverity = 4f;
        public float contentSeverity = 5f;
        public float happySeverity = 6f;
    }
    public class HediffComp_BreakRiskSeverity : HediffComp
    {
        public HediffCompProperties_BreakRiskSeverity Props
        {
            get
            {
                return (HediffCompProperties_BreakRiskSeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.Pawn.needs.mood == null && this.Props.removeFromMoodless)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                if (this.Pawn.needs.mood != null)
                {
                    if (this.Pawn.MentalState == null || this.Props.activeDuringMentalStates)
                    {
                        Need_Mood mood = this.Pawn.needs.mood;
                        if (mood != null)
                        {
                            if (mood.CurLevel < this.Pawn.mindState.mentalBreaker.BreakThresholdExtreme)
                            {
                                this.parent.Severity = this.Props.extremeMBseverity;
                            }
                            else if (mood.CurLevel < this.Pawn.mindState.mentalBreaker.BreakThresholdMajor)
                            {
                                this.parent.Severity = this.Props.majorMBseverity;
                            }
                            else if (mood.CurLevel < this.Pawn.mindState.mentalBreaker.BreakThresholdMinor)
                            {
                                this.parent.Severity = this.Props.minorMBseverity;
                            }
                            else if (mood.CurLevel < 0.65f)
                            {
                                this.parent.Severity = this.Props.neutralSeverity;
                            }
                            else if (mood.CurLevel < 0.9f)
                            {
                                this.parent.Severity = this.Props.contentSeverity;
                            }
                            else
                            {
                                this.parent.Severity = this.Props.happySeverity;
                            }
                        }
                    }
                    else
                    {
                        this.parent.Severity = this.Props.severityIfInMentalState;
                    }
                }
                else
                {
                    this.parent.Severity = this.Props.severityForMoodless;
                }
            }
        }
    }
    //The severity of this hediff is dependent on the specified health capacity of the pawn. plusInitialSeverity is an option to do ewisott
    public class HediffCompProperties_CapacitySeverity : HediffCompProperties
    {
        public HediffCompProperties_CapacitySeverity()
        {
            this.compClass = typeof(HediffComp_CapacitySeverity);
        }
        public PawnCapacityDef capacity;
        public bool plusInitialSeverity;
    }
    public class HediffComp_CapacitySeverity : HediffComp
    {
        public HediffCompProperties_CapacitySeverity Props
        {
            get
            {
                return (HediffCompProperties_CapacitySeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(250, delta))
            {
                this.parent.Severity = this.Pawn.health.capacities.GetLevel(this.Props.capacity) + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    /*When the pawn ingests nutrition, this hediff’s severity changes based on the nutrition amount (handled via Harmony patch).
     * severityPerNutritionIngested: when the pawn ingests nutrition, the hediff severity increases by [a randomly selected value within this range*the nutrition gained]
     * minAgeTicksToFunction: this comp does not function until the hediff is at least this many ticks old. Default 1 to prevent situations where, possibly, such a hediff is added by eating something and it immediately reacts to its causative ingestion*/
    public class HediffCompProperties_ChangeSeverityOnIngestion : HediffCompProperties
    {
        public HediffCompProperties_ChangeSeverityOnIngestion()
        {
            this.compClass = typeof(HediffComp_ChangeSeverityOnIngestion);
        }
        public FloatRange severityPerNutritionIngested;
        public int minAgeTicksToFunction = 1;
    }
    public class HediffComp_ChangeSeverityOnIngestion : HediffComp
    {
        public HediffCompProperties_ChangeSeverityOnIngestion Props
        {
            get
            {
                return (HediffCompProperties_ChangeSeverityOnIngestion)this.props;
            }
        }
    }
    /*When the pawn attacks or casts an ability, this hediff’s severity changes.
     * minAgeTicksToFunction: after the hediff has become this many ticks old (used to prevent sitches you may not want where the hediff is instantiated by a Verb, and it then reacts to that Verb's use)…
     * specificVerbType: …this comp will respond to finished uses of Verbs of or derived from this type…
     * pilferingCountsAsVerb: … (as well as, if this is true, any call of PilferingSystemUtility.AdjustPickpocketSensitiveHediffs, which is called immediately before a pickpocketing or burglary attempt), by either…
     * severityGainedOnUse: …adding this much severity to the hediff, or…
     * setSeverity: …setting the hediff’s severity to this.
     * AdjustSeverity: handles the aforementioned procedure*/
    public class HediffCompProperties_ChangeSeverityOnVerbUse : HediffCompProperties
    {
        public HediffCompProperties_ChangeSeverityOnVerbUse()
        {
            this.compClass = typeof(HediffComp_ChangeSeverityOnVerbUse);
        }
        public float severityGainedOnUse = 0f;
        public float setSeverity = -1f;
        public int minAgeTicksToFunction = 1;
        public bool pilferingCountsAsVerb;
        [TranslationHandle]
        public Type specificVerbType = typeof(Verb);
    }
    public class HediffComp_ChangeSeverityOnVerbUse : HediffComp
    {
        public HediffCompProperties_ChangeSeverityOnVerbUse Props
        {
            get
            {
                return (HediffCompProperties_ChangeSeverityOnVerbUse)this.props;
            }
        }
        public virtual void AdjustSeverity()
        {
            if (this.Props.setSeverity != -1f)
            {
                this.parent.Severity = this.Props.setSeverity;
            }
            if (this.Props.severityGainedOnUse != 0f)
            {
                this.parent.Severity += this.Props.severityGainedOnUse;
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (this.parent.ageTicks >= this.Props.minAgeTicksToFunction)
            {
                if (this.Props.specificVerbType == null || this.Props.specificVerbType == typeof(Verb) || verb.GetType().IsAssignableFrom(this.Props.specificVerbType))
                {
                    this.AdjustSeverity();
                }
            }
        }
    }
    /*The severity of this hediff = the pawn’s expectation level (0-5 for extremely low to sky high, 6-7 for the two tiers of expectations attainable via ideoligious roles, and 8-9 for the two tiers attainable to conceited nobles).
     * There may be a mod out there that changes the ordering of the expectation levels, in which case hediffs using this comp will not function as expected. However, that would be the fault of that mod for fucking with something very foundational yet inexplicably exposed to XML.
     * Unless you want the hediff to be destroyed at Extremely Low expectations, you will need to set the hediff’s minSeverity > 0, or its initialSeverity > 0 and plusInitialSeverity to true.*/
    public class HediffCompProperties_ExpectationSeverity : HediffCompProperties
    {
        public HediffCompProperties_ExpectationSeverity()
        {
            this.compClass = typeof(HediffComp_ExpectationSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_ExpectationSeverity : HediffComp
    {
        public HediffCompProperties_ExpectationSeverity Props
        {
            get
            {
                return (HediffCompProperties_ExpectationSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(250, delta))
            {
                this.parent.Severity = ExpectationsUtility.CurrentExpectationFor(this.parent.pawn).order + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    /* gasTypes: while in a cell occupied by any of these gas types…
     * whileInGas: …if this amount is anything other than -999, sets the hediff to this severity…
     * perTickInGas: …or if it was -999, adds this amount to the hediff’s severity each tick per each such gas the pawn is in.
     * whileNotInGas: while not in any such gas, and this amount is anything other than -999, sets the hediff to this severity…
     * perTickNoGas: …or if it was -999, adds this amount to the hediff’s severity each tick.*/
    public class HediffCompProperties_GasSeverity : HediffCompProperties
    {
        public HediffCompProperties_GasSeverity()
        {
            this.compClass = typeof(HediffComp_GasSeverity);
        }
        public float whileNotInGas = -999f;
        public float perTickNoGas = 0f;
        public float whileInGas = -999f;
        public float perTickInGas = 0f;
        public List<GasType> gasTypes;
    }
    public class HediffComp_GasSeverity : HediffComp
    {
        public HediffCompProperties_GasSeverity Props
        {
            get
            {
                return (HediffCompProperties_GasSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta) && this.Pawn.Spawned)
            {
                bool inGas = false;
                foreach (GasType g in this.Props.gasTypes)
                {
                    if (this.Pawn.Position.AnyGas(this.Pawn.Map, g))
                    {
                        inGas = true;
                        if (this.Props.whileInGas != -999f)
                        {
                            this.parent.Severity = this.Props.whileInGas;
                            break;
                        }
                        else
                        {
                            this.parent.Severity += this.Props.perTickInGas * 15f;
                        }
                    }
                }
                if (!inGas)
                {
                    if (this.Props.whileNotInGas != -999f)
                    {
                        this.parent.Severity = this.Props.whileNotInGas;
                    }
                    else
                    {
                        this.parent.Severity += this.Props.perTickNoGas * 15f;
                    }
                }
            }
        }
    }
    /*If the pawn doesn't have any of the listed traits…
     * severityIfLacks: …and this is anything other than -999, sets the hediff to this severity…
     * severityIfHas: or, if this is anything other than -999, sets the hediff to this severity.*/
    public class HediffCompProperties_HasTraitSeverity : HediffCompProperties
    {
        public HediffCompProperties_HasTraitSeverity()
        {
            this.compClass = typeof(HediffComp_GasSeverity);
        }
        public List<TraitDef> traits;
        public float severityIfHas = -999f;
        public float severityIfLacks = -999f;
    }
    public class HediffComp_HasTraitSeverity : HediffComp
    {
        public HediffCompProperties_HasTraitSeverity Props
        {
            get
            {
                return (HediffCompProperties_HasTraitSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(200, delta) && this.parent.pawn.story != null)
            {
                if (this.Props.severityIfLacks != -999f)
                {
                    bool hasAnySuchTrait = false;
                    foreach (TraitDef t in this.Props.traits)
                    {
                        if (this.parent.pawn.story.traits.HasTrait(t))
                        {
                            hasAnySuchTrait = true;
                        }
                    }
                    if (!hasAnySuchTrait)
                    {
                        this.parent.Severity = this.Props.severityIfLacks;
                    }
                }
                else if (this.Props.severityIfHas != -999f)
                {
                    foreach (TraitDef t in this.Props.traits)
                    {
                        if (this.parent.pawn.story.traits.HasTrait(t))
                        {
                            this.parent.Severity = this.Props.severityIfHas;
                        }
                        break;
                    }
                }
            }
        }
    }
    /*(Dora the Explorer voice) Can you guess what THIS one does?
     * whileResting: while sleeping, if this amount is anything other than -999, sets the hediff to this severity…
     * perTickResting: … or if it was -999, adds this amount to the hediff’s severity each tick.
     * whileAwak: while not sleeping, if this amount is anything other than -999, sets the hediff to this severity…
     * perTickAwake:...or if it was -999, adds this amount to the hediff’s severity each tick.*/
    public class HediffCompProperties_IsRestingSeverity : HediffCompProperties
    {
        public HediffCompProperties_IsRestingSeverity()
        {
            this.compClass = typeof(HediffComp_IsRestingSeverity);
        }
        public float whileAwake = -999f;
        public float perTickAwake = 0f;
        public float whileResting = -999f;
        public float perTickResting = 0f;
    }
    public class HediffComp_IsRestingSeverity : HediffComp
    {
        public HediffCompProperties_IsRestingSeverity Props
        {
            get
            {
                return (HediffCompProperties_IsRestingSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn pawn = this.Pawn;
            if (pawn.IsHashIntervalTick(15, delta) && pawn.needs != null && pawn.needs.rest != null)
            {
                if (pawn.needs.rest.Resting)
                {
                    if (this.Props.whileResting != -999f)
                    {
                        this.parent.Severity = this.Props.whileResting;
                    }
                    else
                    {
                        this.parent.Severity += this.Props.perTickResting * 15f;
                    }
                }
                else if (this.Props.whileAwake != -999f)
                {
                    this.parent.Severity = this.Props.whileAwake;
                }
                else
                {
                    this.parent.Severity += this.Props.perTickAwake * 15f;
                }
            }
        }
    }
    /*The severity of this hediff = the current lighting in the pawn’s cell. If the pawn is out in the world, RimWorld’s GenCelestial.CelestialSunGlow is used instead.
     * Unless you want the hediff to be destroyed in utter darkness (Kerrigan... how could we have known), you will need to set the hediff’s minSeverity > 0, or its initialSeverity > 0 and plusInitialSeverity to true*/
    public class HediffCompProperties_LightingSeverity : HediffCompProperties
    {
        public HediffCompProperties_LightingSeverity()
        {
            this.compClass = typeof(HediffComp_LightingSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_LightingSeverity : HediffComp
    {
        public HediffCompProperties_LightingSeverity Props
        {
            get
            {
                return (HediffCompProperties_LightingSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(250, delta))
            {
                if (this.parent.pawn.Map != null && this.parent.pawn.Spawned)
                {
                    this.parent.Severity = this.parent.pawn.Map.glowGrid.GroundGlowAt(this.parent.pawn.Position) + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
                else if (this.parent.pawn.Tile != -1)
                {
                    this.parent.Severity = GenCelestial.CelestialSunGlow(this.parent.pawn.Tile, Find.TickManager.TicksAbs) + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
            }
        }
    }
    /*whileInMap: per tick spent in a map, if this isn’t -999, the hediff severity is set to this value…
     * perInMapTick: …or if it was -999, adds this amount to the hediff severity each tick.
     * whileOnCaravan: per tick spent on a caravan, if this isn’t -999, the hediff severity is set to this value…
     * perOnCaravanTick: …or if it was -999, adds this amount to the hediff severity each tick*/
    public class HediffCompProperties_OnCaravanSeverity : HediffCompProperties
    {
        public HediffCompProperties_OnCaravanSeverity()
        {
            this.compClass = typeof(HediffComp_OnCaravanSeverity);
        }
        public float perOnCaravanTick = 0f;
        public float whileOnCaravan = -999f;
        public float perInMapTick = 0f;
        public float whileInMap = -999f;
        public bool respectMinSeverity = false;
    }
    public class HediffComp_OnCaravanSeverity : HediffComp
    {
        public HediffCompProperties_OnCaravanSeverity Props
        {
            get
            {
                return (HediffCompProperties_OnCaravanSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                if (this.Pawn.IsCaravanMember())
                {
                    if (this.Props.whileOnCaravan != -999f)
                    {
                        this.parent.Severity = this.Props.whileOnCaravan;
                    }
                    else
                    {
                        this.parent.Severity += this.Props.perOnCaravanTick * 15f;
                    }
                }
                else
                {
                    if (this.Props.whileInMap != -999f)
                    {
                        this.parent.Severity = this.Props.whileInMap;
                    }
                    else
                    {
                        this.parent.Severity += this.Props.perInMapTick * 15f;
                    }
                }
                if (this.parent.Severity < this.parent.def.minSeverity && this.Props.respectMinSeverity)
                {
                    this.parent.Severity = this.parent.def.minSeverity;
                }
            }
        }
    }
    /*The severity of this hediff = the pawn’s total relations visible on their social card. To avoid unreasonably extensive calculations for pawns who know a lot of people, it only recalculates every "periodicity" ticks (1hr by default).
     * Unless you want the hediff to be destroyed when the pawn has no countable relationships, you will need to set the hediff’s minSeverity > 0, or its initialSeverity > 0 and plusInitialSeverity to true.
     * countPositive|NegativeRelations: setting to false causes severity determination to ignore relations that have net-positive|negative opinion
     * positiveRelationsPositiveSeverity: makes counted positive relations add to the severity, and counted negative relations deduct from it; invert for the reverse behavior.*/
    public class HediffCompProperties_RelationDependentSeverity : HediffCompProperties
    {
        public HediffCompProperties_RelationDependentSeverity()
        {
            this.compClass = typeof(HediffComp_RelationDependentSeverity);
        }
        public int periodicity = 2500;
        public bool countPositiveRelations = true;
        public bool countNegativeRelations = true;
        public bool positiveRelationsPositiveSeverity = true;
        public bool plusInitialSeverity;

    }
    public class HediffComp_RelationDependentSeverity : HediffComp
    {
        public HediffCompProperties_RelationDependentSeverity Props
        {
            get
            {
                return (HediffCompProperties_RelationDependentSeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            RecalculateRDS();
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(this.Props.periodicity, delta))
            {
                RecalculateRDS();
            }
        }
        private void RecalculateRDS()
        {
            List<Pawn> list = SocialCardUtility.PawnsForSocialInfo(this.parent.pawn);
            float relevantRelations = 0f;
            foreach (Pawn p in list)
            {
                int opinion = this.parent.pawn.relations.OpinionOf(p);
                if ((opinion > 0 && this.Props.countPositiveRelations) || (opinion < 0 && this.Props.countNegativeRelations))
                {
                    relevantRelations += opinion;
                }
            }
            if (!this.Props.positiveRelationsPositiveSeverity)
            {
                relevantRelations *= -1;
            }
            this.parent.Severity = relevantRelations + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
        }
    }
    /*The severity of this hediff depends on whether the pawn is in one of the specified mental state(s). anyMentalState is, as the name indicates, a blanket whitelist; if you want a subset, you whitelist via mentalStates.
     * severityInState: … if in a triggering mental state, and this amount is anything other than -999, the hediff’s severity is this amount…
     * severityPerTickInState: …or if it was -999, adds this amount to the hediff’s severity each tick.
     * severityOtherwise: if not in any such state, and this amount isn’t -999, the hediff’s severity is this amount…
     * severityPerTickOtherwise: …or if it was -999, adds this amount to the hediff’s severity each tick.*/
    public class HediffCompProperties_SeverityDuringSpecificMentalStates : HediffCompProperties
    {
        public HediffCompProperties_SeverityDuringSpecificMentalStates()
        {
            this.compClass = typeof(HediffComp_SeverityDuringSpecificMentalStates);
        }
        public bool anyMentalState = true;
        public List<MentalStateDef> mentalStates;
        public float severityInState = -999f;
        public float severityPerTickInState;
        public float severityOtherwise = -999f;
        public float severityPerTickOtherwise;
    }
    public class HediffComp_SeverityDuringSpecificMentalStates : HediffComp
    {
        public HediffCompProperties_SeverityDuringSpecificMentalStates Props
        {
            get
            {
                return (HediffCompProperties_SeverityDuringSpecificMentalStates)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.pawn.MentalStateDef != null && (this.Props.anyMentalState || this.Props.mentalStates.Contains(this.parent.pawn.MentalStateDef)))
            {
                if (this.Props.severityInState != -999f)
                {
                    this.parent.Severity = this.Props.severityInState;
                }
                else
                {
                    this.parent.Severity += this.Props.severityPerTickInState;
                }
            }
            else if (this.Props.severityOtherwise != -999f)
            {
                this.parent.Severity = this.Props.severityOtherwise;
            }
            else
            {
                this.parent.Severity += this.Props.severityPerTickOtherwise;
            }
        }
    }
    /*The severity of this hediff = the sum level of the specified skill(s).
     * Unless you want the hediff to be destroyed when the pawn utterly lacks the specified skill(s), you will need to set the hediff’s minSeverity > 0, or its initialSeverity > 0 and plusInitialSeverity to true.*/
    public class HediffCompProperties_SkillLevelSeverity : HediffCompProperties
    {
        public HediffCompProperties_SkillLevelSeverity()
        {
            this.compClass = typeof(HediffComp_SkillLevelSeverity);
        }
        public List<SkillDef> skills;
        public bool plusInitialSeverity;
    }
    public class HediffComp_SkillLevelSeverity : HediffComp
    {
        public HediffCompProperties_SkillLevelSeverity Props
        {
            get
            {
                return (HediffCompProperties_SkillLevelSeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.Pawn.skills == null)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                float netSkill = 0f;
                foreach (SkillDef s in this.Props.skills)
                {
                    netSkill += this.parent.pawn.skills.GetSkill(s).Level;
                }
                this.parent.Severity = netSkill + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    /*The severity of this hediff changes at a specified rate, multiplied by a specified stat of the pawn’s.
     * As this might be used with stats that change in magnitude over time (and possibly very rapidly), severity change occurs each variable tick (as opposed to the regular SeverityPerDay, which changes every 200 ticks).*/
    public class HediffCompProperties_StatScalingSeverityPerDay : HediffCompProperties
    {
        public HediffCompProperties_StatScalingSeverityPerDay()
        {
            this.compClass = typeof(HediffComp_StatScalingSeverityPerDay);
        }
        public float baseSeverityPerDay;
        public int periodicity;
        public StatDef stat;
    }
    public class HediffComp_StatScalingSeverityPerDay : HediffComp
    {
        public HediffCompProperties_StatScalingSeverityPerDay Props
        {
            get
            {
                return (HediffCompProperties_StatScalingSeverityPerDay)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity, delta))
            {

                this.parent.Severity += this.Props.baseSeverityPerDay * this.Pawn.GetStatValue(this.Props.stat) * this.Props.periodicity / 60000f;
            }
        }
    }
    /*Provided the pawn has >=minStatToScale of the specified stat, the severity of this hediff equals a base value times however much of that stat the pawn has*/
    public class HediffCompProperties_StatScalingSeverityWithMin : HediffCompProperties
    {
        public HediffCompProperties_StatScalingSeverityWithMin()
        {
            this.compClass = typeof(HediffComp_StatScalingSeverityWithMin);
        }
        public float baseSeverity = 1f;
        public StatDef statScalar;
        public float minStatToScale = 1f;
    }
    public class HediffComp_StatScalingSeverityWithMin : HediffComp
    {
        public HediffCompProperties_StatScalingSeverityWithMin Props
        {
            get
            {
                return (HediffCompProperties_StatScalingSeverityWithMin)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.parent.pawn.GetStatValue(this.Props.statScalar) >= this.Props.minStatToScale)
            {
                this.parent.Severity = this.Props.baseSeverity * this.parent.pawn.GetStatValue(this.Props.statScalar);
            }
            else
            {
                this.parent.Severity = this.Props.baseSeverity;
            }
        }
    }
    /*Severity changes at a rate of changePerTick towards the target severity, which is based on the pawn's current temperature.
     * zeroSeverityAt: sets target severity to 0 while within this temperature range. If you don't want the hediff to be destroyed in this range, you either need to set a positive minSeverity or override the hediff's ShouldRemove to permit non-positive severities
     * perTempAbove: sets target severity to [this value*oC above the upper bound of zeroSeverityAt].
     * perTempBelow: sets target severity to [this value*oC below the lower bound of zeroSeverityAt].*/
    public class HediffCompProperties_TemperatureLevelSeverity : HediffCompProperties
    {
        public HediffCompProperties_TemperatureLevelSeverity()
        {
            this.compClass = typeof(HediffComp_TemperatureLevelSeverity);
        }
        public FloatRange zeroSeverityAt;
        public float perTempAbove;
        public float perTempBelow;
        public float changePerTick = 999f;
    }
    public class HediffComp_TemperatureLevelSeverity : HediffComp
    {
        public HediffCompProperties_TemperatureLevelSeverity Props
        {
            get
            {
                return (HediffCompProperties_TemperatureLevelSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {

                float temp = this.Pawn.AmbientTemperature;
                if (this.Props.zeroSeverityAt.Includes(temp))
                {
                    if (this.parent.Severity > 0f)
                    {
                        this.parent.Severity = Math.Max(0f, this.parent.Severity - (this.Props.changePerTick * 15f));
                    }
                    else
                    {
                        this.parent.Severity = Math.Min(0f, this.parent.Severity + (this.Props.changePerTick * 15f));
                    }
                }
                else if (temp > this.Props.zeroSeverityAt.max)
                {
                    float desiredTemp = this.Props.perTempAbove * (temp - this.Props.zeroSeverityAt.max);
                    if (this.parent.Severity > desiredTemp)
                    {
                        this.parent.Severity = Math.Max(desiredTemp, this.parent.Severity - (this.Props.changePerTick * 15f));
                    }
                    else
                    {
                        this.parent.Severity = Math.Min(desiredTemp, this.parent.Severity + (this.Props.changePerTick * 15f));
                    }
                }
                else
                {
                    float desiredTemp = this.Props.perTempBelow * (this.Props.zeroSeverityAt.min - temp);
                    if (this.parent.Severity > desiredTemp)
                    {
                        this.parent.Severity = Math.Max(desiredTemp, this.parent.Severity - (this.Props.changePerTick * 15f));
                    }
                    else
                    {
                        this.parent.Severity = Math.Min(desiredTemp, this.parent.Severity + (this.Props.changePerTick * 15f));
                    }
                }
            }
        }
    }
    /*The severity of this hediff is dependent on how watery (and possibly rainy) the pawn’s current cell is.
     * baseSeverity: default severity of this hediff while spawned. Add 1/100th the path cost of the current cell the pawn is in if it’s watery (which works out to 3.00 for deep water, 0.42 for chest-deep water, 0.30 for shallow water)
     *   to this value to determine severity…
     * disabledIfNotSlowedInWater: …unless this is on, and the pawn is VEF-floating or has a WaterCellCost <= 1. You should probably turn this on if you're using WIS to simulate a swim speed buff - example is the Mariner trait in HAT.
     * rainCountsFor: while in an unroofed area, adds this amount times the current local rainRate to the hediff severity.
     *   Note that there is a delay between the visual onset of rain and the weather actually changing, so don’t expect the rain severity adjustment to kick in until a little bit after the raindrops first appear.
     * basebaseSeverityCaravan: default severity of this hediff while in a caravan.
     * severityPerCaravanRiverSize: if in a caravan, the widthOnMap of the largest river in that caravan’s current tile is evaluated on this curve. That value is then added to the severity.
     *   Values for each vanilla river type as follows: huge river (30), large river (14), river (6), creek (4)*/
    public class HediffCompProperties_WaterImmersionSeverity : HediffCompProperties
    {
        public HediffCompProperties_WaterImmersionSeverity()
        {
            this.compClass = typeof(HediffComp_WaterImmersionSeverity);
        }
        public float baseSeverity = 0.001f;
        public float rainCountsFor = 0.051f;
        public float baseSeverityCaravan = 0.001f;
        public SimpleCurve severityPerCaravanRiverSize = new SimpleCurve(new CurvePoint[]
        {
            new CurvePoint(0f, 0f)
        });
        public float caravanWaterTileSeverity = 3f;
        public bool disabledIfNotSlowedInWater;
    }
    public class HediffComp_WaterImmersionSeverity : HediffComp
    {
        public HediffCompProperties_WaterImmersionSeverity Props
        {
            get
            {
                return (HediffCompProperties_WaterImmersionSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                this.parent.Severity = this.Props.baseSeverity;
                if (this.Pawn.Spawned && this.Pawn.PositionHeld.GetTerrain(this.Pawn.MapHeld) != null)
                {
                    if (this.Props.disabledIfNotSlowedInWater)
                    {
                        if (VEF.AnimalBehaviours.StaticCollectionsClass.floating_animals.Contains(this.Pawn))
                        {
                            return;
                        }
                        int? wcc = this.Pawn.WaterCellCost;
                        if (wcc != null && wcc <= 1)
                        {
                            return;
                        }
                    }
                    if (this.Pawn.PositionHeld.GetTerrain(this.Pawn.MapHeld).IsWater)
                    {
                        this.parent.Severity += ((this.Pawn.PositionHeld.GetTerrain(this.Pawn.MapHeld).pathCost) / 100f);
                    }
                    if (this.Pawn.PositionHeld.GetRoof(this.Pawn.MapHeld) == null)
                    {
                        this.parent.Severity += this.Pawn.MapHeld.weatherManager.RainRate * this.Props.rainCountsFor;
                    }
                }
                else if (this.Pawn.Tile != -1 && this.Pawn.GetCaravan() != null)
                {
                    this.parent.Severity = this.Props.baseSeverityCaravan;
                    if (Find.WorldGrid[this.Pawn.Tile].WaterCovered)
                    {
                        this.parent.Severity += this.Props.caravanWaterTileSeverity;
                    }
                    else if (this.Pawn.Tile.Tile is SurfaceTile st && !st.Rivers.NullOrEmpty<SurfaceTile.RiverLink>())
                    {
                        float riverWidth = 0f;
                        foreach (SurfaceTile.RiverLink rl in st.Rivers)
                        {
                            if (rl.river.widthOnMap > riverWidth)
                            {
                                riverWidth = rl.river.widthOnMap;
                            }
                        }
                        this.parent.Severity += this.Props.severityPerCaravanRiverSize.Evaluate(riverWidth);
                    }
                }
            }
        }
    }
    /*The severity of this hediff is dependent on how windy it is. If in a space it can't work in, severity drops to 0.
     * Unless you want the hediff to be destroyed in a space it can’t work in, you will need to set the hediff’s minSeverity > 0, or its initialSeverity >0 and plusInitialSeverity to true.
     * worksEnclosedSpace: enables it to work in any roofed area that uses the ambient map temperature.
     * worksUnderThinRoof: enables it to work underneath thin roofs.
     * worksUnderThickRoof: enables it to work underneath thick roofs.
     * worldMapValue: while not spawned, the severity of this hediff equals this value.*/
    public class HediffCompProperties_WindLevelSeverity : HediffCompProperties
    {
        public HediffCompProperties_WindLevelSeverity()
        {
            this.compClass = typeof(HediffComp_WindLevelSeverity);
        }
        public bool worksEnclosedSpace = false;
        public bool worksUnderThinRoof = true;
        public bool worksUnderThickRoof = true;
        public float worldMapValue = 0.001f;
        public bool plusInitialSeverity;
    }
    public class HediffComp_WindLevelSeverity : HediffComp
    {
        public HediffCompProperties_WindLevelSeverity Props
        {
            get
            {
                return (HediffCompProperties_WindLevelSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(60, delta))
            {
                if (this.parent.pawn.Map != null)
                {
                    if (this.parent.pawn.Spawned)
                    {
                        if (this.parent.pawn.Position.GetRoof(this.parent.pawn.Map) != null)
                        {
                            if (!this.Props.worksEnclosedSpace && !this.parent.pawn.Position.UsesOutdoorTemperature(this.parent.pawn.Map))
                            {
                                this.parent.Severity = 0f;
                                return;
                            }
                            else if (!this.Props.worksUnderThinRoof && !this.parent.pawn.Position.GetRoof(this.parent.pawn.Map).isThickRoof)
                            {
                                this.parent.Severity = 0f;
                                return;
                            }
                            else if (!this.Props.worksUnderThickRoof && this.parent.pawn.Position.GetRoof(this.parent.pawn.Map).isThickRoof)
                            {
                                this.parent.Severity = 0f;
                                return;
                            }
                        }
                        this.parent.Severity = this.Pawn.Map.windManager.WindSpeed + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                    }
                }
                else
                {
                    this.parent.Severity = this.Props.worldMapValue + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
            }
        }
    }
    /*The severity of this hediff = the pawn’s ideoligious certainty.
     * changesToThisOnApostasy: when the pawn changes ideoligions, the hediff gets replaced with this hediff, unless this is null or…
     * removeOnApostasy: …unless this is true, in which case this hediff just gets removed.
     * severityIfNoIdeo: if Ideology isn’t active or the pawn has no ideoligion, the severity of this hediff is set to this value.*/
    public class HediffCompProperties_IdeoCertaintySeverity : HediffCompProperties
    {
        public HediffCompProperties_IdeoCertaintySeverity()
        {
            this.compClass = typeof(HediffComp_IdeoCertaintySeverity);
        }
        public bool removeOnApostasy;
        public HediffDef changesToThisOnApostasy;
        public float severityIfNoIdeo = 0.001f;
    }
    public class HediffComp_IdeoCertaintySeverity : HediffComp
    {
        public HediffCompProperties_IdeoCertaintySeverity Props
        {
            get
            {
                return (HediffCompProperties_IdeoCertaintySeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (!ModsConfig.IdeologyActive || this.Pawn.Ideo == null)
            {
                this.parent.Severity = this.Props.severityIfNoIdeo;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta))
            {
                if (ModsConfig.IdeologyActive && this.Pawn.Ideo != null)
                {
                    this.parent.Severity = Math.Max(this.Pawn.ideo.Certainty, 0.001f);
                }
            }
        }
    }
    /*The severity of this hediff is dependent on whether or not the pawn is meditating. “Meditating” means performing the last toil in the Meditate or Reign jobs,
     *   as opposed to using the PsychicEntropyTracker, because this does not assume the pawn has an active one.
     * whileMeditating: per tick spent meditating, if this isn’t -999, the hediff severity is set to this value…
     * perMeditationTick: …or if it was -999, adds this amount to the hediff severity each tick.
     * whileNotMeditating: per tick spent NOT meditating, if this isn’t -999, the hediff severity is set to this value…
     * perNotMedTick: …or if it was -999, adds this amount to the hediff severity each tick.
     * medFocusSpeedMatters: multiplies perMeditationTick by the pawn’s meditation focus gain stat. Keep in mind that's 0.5x by default.*/
    public class HediffCompProperties_MeditationSeverity : HediffCompProperties
    {
        public HediffCompProperties_MeditationSeverity()
        {
            this.compClass = typeof(HediffComp_MeditationSeverity);
        }
        public float perMeditationTick = 0f;
        public float perNotMedTick = 0f;
        public bool medFocusSpeedMatters = false;
        public float whileMeditating = -999f;
        public float whileNotMeditating = -999f;
    }
    public class HediffComp_MeditationSeverity : HediffComp
    {
        public HediffCompProperties_MeditationSeverity Props
        {
            get
            {
                return (HediffCompProperties_MeditationSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                if (this.Pawn.CurJob != null && (this.Pawn.CurJobDef == JobDefOf.Meditate || this.Pawn.CurJobDef == JobDefOf.Reign) && this.Pawn.jobs.curDriver != null && this.Pawn.jobs.curDriver.OnLastToil)
                {
                    if (this.Props.whileMeditating != -999f)
                    {
                        this.parent.Severity = this.Props.whileMeditating;
                    }
                    else if (this.Props.medFocusSpeedMatters)
                    {
                        this.parent.Severity += this.Props.perMeditationTick * this.Pawn.GetStatValue(StatDefOf.MeditationFocusGain) * 15f;
                    }
                    else
                    {
                        this.parent.Severity += this.Props.perMeditationTick * 15f;
                    }
                }
                else
                {
                    if (this.Props.whileNotMeditating != -999f)
                    {
                        this.parent.Severity = this.Props.whileNotMeditating;
                    }
                    else
                    {
                        this.parent.Severity += this.Props.perNotMedTick * 15f;
                    }
                }
            }
        }
    }
    /*(Dora the Explainer voice) The hediff severity = the pawn’s current percent psyfocus.
     * Unless you want the hediff to be destroyed at 0% psyfocus, you will need to set the hediff’s minSeverity >0, or its initialSeverity to 0 and set plusInitialSeverity to true.*/
    public class HediffCompProperties_PsyfocusSeverity : HediffCompProperties
    {
        public HediffCompProperties_PsyfocusSeverity()
        {
            this.compClass = typeof(HediffComp_PsyfocusSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_PsyfocusSeverity : HediffComp
    {
        public HediffCompProperties_PsyfocusSeverity Props
        {
            get
            {
                return (HediffCompProperties_PsyfocusSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.pawn.psychicEntropy != null)
            {
                this.parent.Severity = this.Pawn.psychicEntropy.CurrentPsyfocus + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    /*When the pawn’s psyfocus is negatively offset (e.g. by spending psyfocus to use psycasts), the hediff severity increases by [random value within severityPerPsyfocus * the psyfocus lost].
     * You can make derivatives of this that do something else as well with the UpdatePsyfocusExpenditure virtual method. Like, I dunno, make an explosion that scales with the psyfocus spent. That'd be cool, why haven't I done that yet*/
    public class HediffCompProperties_PsyfocusSpentTracker : HediffCompProperties
    {
        public HediffCompProperties_PsyfocusSpentTracker()
        {
            this.compClass = typeof(HediffComp_PsyfocusSpentTracker);
        }
        public FloatRange severityPerPsyfocus = new FloatRange(100f);
    }
    public class HediffComp_PsyfocusSpentTracker : HediffComp
    {
        public HediffCompProperties_PsyfocusSpentTracker Props
        {
            get
            {
                return (HediffCompProperties_PsyfocusSpentTracker)this.props;
            }
        }
        public virtual void UpdatePsyfocusExpenditure(float offset)
        {
            this.parent.Severity -= offset * this.Props.severityPerPsyfocus.RandomInRange;
        }
    }
    /*The hediff severity = the pawn’s psylink level, if any. Checks only every 250 ticks because a pawn's psylink level isn't changing very dynamically, and also because of the VPE compat.
     * Unless you want the hediff to be destroyed if the pawn lacks a psylink, you will need to set the hediff’s minSeverity >0, or its initialSeverity > 0 and set plusInitialSeverity to true*/
    public class HediffCompProperties_PsylinkSeverity : HediffCompProperties
    {
        public HediffCompProperties_PsylinkSeverity()
        {
            this.compClass = typeof(HediffComp_PsylinkSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_PsylinkSeverity : HediffComp
    {
        public HediffCompProperties_PsylinkSeverity Props
        {
            get
            {
                return (HediffCompProperties_PsylinkSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                {
                    Hediff_Level psylink = (Hediff_Level)this.Pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                    if (psylink == null)
                    {
                        this.parent.Severity = psylink.level;
                        return;
                    }
                }
                else
                {
                    this.parent.Severity = this.Pawn.GetPsylinkLevel() + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
            }
        }
    }
    /*The severity of this hediff is dependent on the current monolith level (or, if not playing the Standard anomaly playstyle, a different default value).
     * If the current anomaly playstyle has a CustomAnomalyPlaystyleActivityLevels DME (see CustomAnomalyPlaystyleLevels.cs), the level is whatever its CAPAL's Worker says it is.
float defaultSeverityAmbientHorror (default 2f): if the current playthrough’s anomaly playstyle is Ambient Horror, the hediff’s severity is set to this value.
Dictionary<int, float> severityAtEachLevel: if the current playthrough is NOT Ambient Horror and the current anomalous activity level is one of the keys in this Dictionary, the hediff’s severity is set to that key’s corresponding value. Anomalous activity level is a property o
float defaultSeverity: if none of the prior conditions are fulfilled, the hediff’s severity is set to this value.*/
    public class HediffCompProperties_AnomalousActivitySeverity : HediffCompProperties
    {
        public HediffCompProperties_AnomalousActivitySeverity()
        {
            this.compClass = typeof(HediffComp_AnomalousActivitySeverity);
        }
        public Dictionary<int, float> severityAtEachLevel;
        public float defaultSeverity;
        public float defaultSeverityAmbientHorror = 2f;
    }
    public class HediffComp_AnomalousActivitySeverity : HediffComp
    {
        public HediffCompProperties_AnomalousActivitySeverity Props
        {
            get
            {
                return (HediffCompProperties_AnomalousActivitySeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(2500, delta) && ModsConfig.AnomalyActive)
            {
                if (Find.Storyteller.difficulty.AnomalyPlaystyleDef == DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror"))
                {
                    this.parent.Severity = this.Props.defaultSeverityAmbientHorror;
                    return;
                }
                int level = Find.Anomaly.Level;
                CustomAnomalyPlaystyleActivityLevels capal = Find.Storyteller.difficulty.AnomalyPlaystyleDef.GetModExtension<CustomAnomalyPlaystyleActivityLevels>();
                if (capal != null)
                {
                    level = capal.Worker.CurrentLevel(capal);
                }
                this.parent.Severity = this.Props.severityAtEachLevel.TryGetValue(level, this.Props.defaultSeverity);
            }
        }
    }
    /*As SeverityPerDay, but the rate is determined by the current anomalous activity level (as defined above). The following fields replace the severityPerDay field, and recalculate every 250 ticks:
     * defaultSeverityPerDayAmbientHorror: if the current playthrough’s anomaly playstyle is Ambient Horror, this is the value used in place of severityPerDay.
     * severityPerDayAtEachLevel: if the current playthrough is NOT Ambient Horror and the current anomalous activity level is one of the keys in this Dictionary, that key’s corresponding value is used in place of severityPerDay.
     * defaultSeverity: if none of the prior conditions are fulfilled, this is the value used in place of severityPerDay*/
    public class HediffCompProperties_SeverityPerDayPerAnomalousActivity : HediffCompProperties
    {
        public HediffCompProperties_SeverityPerDayPerAnomalousActivity()
        {
            this.compClass = typeof(HediffComp_SeverityPerDayPerAnomalousActivity);
        }
        public float CalculateSeverityPerDay()
        {
            if (ModsConfig.AnomalyActive)
            {
                float num = this.defaultSeverityPerDayAmbientHorror;
                if (Find.Storyteller.difficulty.AnomalyPlaystyleDef != DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror"))
                {
                    int level = Find.Anomaly.Level;
                    CustomAnomalyPlaystyleActivityLevels capal = Find.Storyteller.difficulty.AnomalyPlaystyleDef.GetModExtension<CustomAnomalyPlaystyleActivityLevels>();
                    if (capal != null)
                    {
                        level = capal.Worker.CurrentLevel(capal);
                    }
                    num = this.severityPerDayAtEachLevel.TryGetValue(level, this.defaultSeverityPerDay);
                }
                num += this.severityPerDayRange.RandomInRange;
                if (Rand.Chance(this.reverseSeverityChangeChance))
                {
                    num *= -1f;
                }
                return num;
            }
            return 0f;
        }
        public Dictionary<int, float> severityPerDayAtEachLevel;
        public float defaultSeverityPerDay;
        public float defaultSeverityPerDayAmbientHorror;
        public bool showDaysToRecover;
        public bool showHoursToRecover;
        public float mechanitorFactor = 1f;
        public float reverseSeverityChangeChance;
        public FloatRange severityPerDayRange = FloatRange.Zero;
        public float minAge;
    }
    public class HediffComp_SeverityPerDayPerAnomalousActivity : HediffComp_SeverityModifierBase
    {
        private HediffCompProperties_SeverityPerDayPerAnomalousActivity Props
        {
            get
            {
                return (HediffCompProperties_SeverityPerDayPerAnomalousActivity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.severityPerDay = this.Props.CalculateSeverityPerDay();
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.severityPerDay, "severityPerDay", 0f, false);
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                this.severityPerDay = this.Props.CalculateSeverityPerDay();
            }
        }
        public override float SeverityChangePerDay()
        {
            if (base.Pawn.ageTracker.AgeBiologicalYearsFloat < this.Props.minAge)
            {
                return 0f;
            }
            float num = this.severityPerDay;
            HediffStage curStage = this.parent.CurStage;
            float num2 = num * ((curStage != null) ? curStage.severityGainFactor : 1f);
            if (ModsConfig.BiotechActive && MechanitorUtility.IsMechanitor(base.Pawn))
            {
                num2 *= this.Props.mechanitorFactor;
            }
            return num2;
        }
        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (this.Props.showHoursToRecover && this.SeverityChangePerDay() < 0f)
                {
                    return Mathf.RoundToInt(this.parent.Severity / Mathf.Abs(this.SeverityChangePerDay()) * 24f) + "LetterHour".Translate();
                }
                return null;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showDaysToRecover && this.SeverityChangePerDay() < 0f)
                {
                    return "DaysToRecover".Translate((this.parent.Severity / Mathf.Abs(this.SeverityChangePerDay())).ToString("0.0")).Resolve();
                }
                return null;
            }
        }
        public float severityPerDay;
    }
    /*The severity of this hediff is dependent on the current planet layer the pawn is on. Recalculates every "periodicity" ticks.
     * setToInLayer: if the pawn’s tile’s planet layer def is one of the keys in this Dictionary, the hediff’s severity is set to that key’s corresponding value; otherwise…
     * incrementInLayer: if the pawn’s tile’s planet layer def is one of the keys in this Dictionary, that key’s corresponding value is added to the hediff’s severity otherwise…
     * defaultSeverity: if none of the prior conditions are fulfilled, the hediff’s severity is set to this value*/
    public class HediffCompProperties_PlanetLayerSeverity : HediffCompProperties
    {
        public HediffCompProperties_PlanetLayerSeverity()
        {
            this.compClass = typeof(HediffComp_PlanetLayerSeverity);
        }
        public float defaultSeverity;
        public int periodicity = 250;
        public Dictionary<PlanetLayerDef, float> setToInLayer;
        public Dictionary<PlanetLayerDef, float> incrementInLayer;
    }
    public class HediffComp_PlanetLayerSeverity : HediffComp
    {
        public HediffCompProperties_PlanetLayerSeverity Props
        {
            get
            {
                return (HediffCompProperties_PlanetLayerSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity, delta) && this.Pawn.Tile != null && this.Pawn.Tile.Layer != null)
            {
                if (!this.Props.setToInLayer.NullOrEmpty() && this.Props.setToInLayer.TryGetValue(this.Pawn.Tile.LayerDef, out float value))
                {
                    this.parent.Severity = value;
                    return;
                }
                if (!this.Props.incrementInLayer.NullOrEmpty() && this.Props.incrementInLayer.TryGetValue(this.Pawn.Tile.LayerDef, out value))
                {
                    this.parent.Severity += value;
                    return;
                }
                this.parent.Severity = this.Props.defaultSeverity;
            }
        }
    }
    /*The severity of this hediff is dependent on whether or not the pawn is in a vacuum.
     * vacuumThreshold: if in at least this much % vacuum…
     * freezeAtVacuumImmunityInVacuum: …do nothing if this is true AND Pawn.HarmedByVacuum would return false for this pawn; otherwise…
     * onlyInVacuumIfHarmedByIt: …if this is false, OR if Pawn.HarmedByVacuum would return true for this pawn… the pawn is considered to be in vacuum. This matters for the following fields:
     * whileInVacuum: per tick in vacuum, if this isn’t -999, the hediff severity is set to this value…
     * perTickInVacuum: …or if it was -999, adds this amount to the hediff severity each tick.
     * whileNotInVacuum: per tick spent NOT in vacuum, if this isn’t -999, the hediff severity is set to this value…
     * perTickNotInVacuum: …or if it was -999, adds this amount to the hediff severity each tick.*/
    public class HediffCompProperties_VacuumSeverity : HediffCompProperties
    {
        public HediffCompProperties_VacuumSeverity()
        {
            this.compClass = typeof(HediffComp_VacuumSeverity);
        }
        public float vacuumThreshold = 0.5f;
        public float perTickInVacuum;
        public float perTickNotInVacuum;
        public float whileInVacuum = -999f;
        public float whileNotInVacuum = -999f;
        public bool onlyInVacuumIfHarmedByIt = true;
        public bool freezeAtVacuumImmunityInVacuum;
    }
    public class HediffComp_VacuumSeverity : HediffComp
    {
        public HediffCompProperties_VacuumSeverity Props
        {
            get
            {
                return (HediffCompProperties_VacuumSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                Pawn pawn = this.Pawn;
                if (pawn.Spawned && pawn.Map.Biome.inVacuum && pawn.Position.GetVacuum(pawn.Map) >= this.Props.vacuumThreshold)
                {
                    if (!pawn.HarmedByVacuum)
                    {
                        if (this.Props.freezeAtVacuumImmunityInVacuum)
                        {
                            return;
                        }
                        if (this.Props.onlyInVacuumIfHarmedByIt)
                        {
                            this.NonVacuumEffects();
                        }
                        else
                        {
                            this.VacuumEffects();
                        }
                    }
                    else
                    {
                        this.VacuumEffects();
                    }
                }
                else
                {
                    this.NonVacuumEffects();
                }
            }
        }
        public void VacuumEffects()
        {
            if (this.Props.whileInVacuum != -999f)
            {
                this.parent.Severity = this.Props.whileInVacuum;
            }
            else if (this.Props.perTickInVacuum != 0f)
            {
                this.parent.Severity += this.Props.perTickInVacuum * 15f;
            }
        }
        public void NonVacuumEffects()
        {
            if (this.Props.whileNotInVacuum != -999f)
            {
                this.parent.Severity = this.Props.whileNotInVacuum;
            }
            else
            {
                this.parent.Severity += this.Props.perTickNotInVacuum * 15f;
            }
        }
    }
}
