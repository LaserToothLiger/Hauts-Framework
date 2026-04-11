using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF;
using Verse;

namespace HautsFramework
{
    /*basic chassis for EOHEs. Does not do anything by itself - see each of the derivatives below.
     * a lot of these bools could probably just have been TargetingParameters, except for the drones thing. canAffectHostiles/Friendlies are opposites of each other, governed by if the pawn is HostileTo the victim
     * damageScaling: multiplies whatever the specific effect of this EOHE variant is by the damage of the triggering attack (if applicable)
     * entropyCostScaling: ditto, but by the neural heat cost of the triggering psycast
     * psyfocusCostScaling: ditto, but by the psyfocus cost of the triggering psycast
     * tickCooldown: whenever the effect is triggered, it can’t occur again until at least a random number of ticks (chosen from within this range) have elapsed
     * severityChangeOnHit: adds this amount to the hediff’s severity when the effect is triggered
     * cellRange: the effect can only apply to victims within this many cells of this parent pawn. Any value less than 1.5 is treated as 1.5, since that’s functionally melee range (1.42f)
     * worldTileRange: the effect can only apply to victims within this many world tiles of this parent pawn. I can’t conceive of any use for this, because how are you going to attack or psycast pawns on other world tiles? Just in case, though
     * minDmgToTrigger: the effect can only apply to attack victims if the victim took at least this much damage from the triggering attack
     * appliedViaAttacks|Psycasts: makes the pawn's attacks|psycasts apply the effect to whatever pawns they hit|affect
     * attackerScalar: multiplies whatever the specific effect of this EOHE variant is by however much of this stat the parent pawn has
     * victimScalar: multiplies whatever the specific effect of this EOHE variant is by however much of this stat the victim has
     * victimBodySizeInverseScaling: divides the specific effect of this EOHE variant by the victim’s body size
     * chance: the chance for the effect to apply. This is checked independently of any of the previously mentioned checks. 1 = 100%
     * attacker|victimChanceScalar: multiplies the chance for the effect to apply by however much of this stat the parent pawn|pawn-to-be-affected has
     * chanceCap: chance maxes out at this value, regardless of how high other factors scale it
     * showTooltip: shows a (frankly very wordy) description describing the parameters of the extra-on-hit-effect in the hediff’s tooltip
     * triggersPyroThought: if enabled, Pyromaniacs gain the “used incendiary weapon” moodlet on inflicting the EOHE on what the game defines as a ‘valid pyro thought target’ (a non-downed, non-invisible, unfogged hostile pawn); unlike regular causes of this moodlet, it considers pawns hostile to the attacker’s faction as hostile (instead of those hostile to the player faction), and it does not scan every possible hittable pawn in the verb’s range if it didn’t target a spawned pawn who is a valid pyro thought target (to save on performance).
     * FXTooltip: added to the hediff’s tooltip
     * ChanceForVictim: the chance for Pawn victim to be affected on hit
     * ChanceForVictimThing: ditto, but for non-Pawn things
     * RangeCheck: determines whether or not the victim Thing is in valid range of the attacker. It treats the distance between Thing and attacker as being shorter by the damage info’s weapon’s VEF_MeleeWeaponRange, if any. If this check passes, then we also need to check…
     * CanAffectTarget: …whether or not it is valid to inflict this on-hit effect on Pawn victim
     * CanAffectTargetThing(Thing thing): ditto, for non-Pawn things
     * DoExtraEffects: implements the effects on Pawn victim
     * DoExtraEffectsThing: ditto, for non-Pawn things*/
    public class HediffCompProperties_ExtraOnHitEffects : HediffCompProperties
    {
        public HediffCompProperties_ExtraOnHitEffects()
        {
            this.compClass = typeof(HediffComp_ExtraOnHitEffects);
        }
        public bool damageScaling = false;
        public bool entropyCostScaling = false;
        public bool psyfocusCostScaling = false;
        public bool showTooltip = true;
        public IntRange tickCooldown = new IntRange(0, 0);
        public float severityChangeOnHit;
        public float cellRange = 1f;
        public float worldTileRange = 0f;
        public float minDmgToTrigger = 0.01f;
        public bool canAffectAnimals = true;
        public bool canAffectMechs = true;
        public bool canAffectDrones = true;
        public bool canAffectHumanlikes = true;
        public bool canAffectEntities = true;
        public bool canAffectMutants = true;
        public bool canAffectHostiles = true;
        public bool canAffectFriendlies = true;
        public bool canAffectBuildings = true;
        public bool canAffectPlants = true;
        public bool canAffectItems = true;
        public bool appliedViaAttacks;
        public bool appliedViaPsycasts;
        public StatDef attackerScalar;
        public StatDef victimScalar;
        public bool victimBodySizeInverseScaling = false;
        public float chance = 1f;
        public StatDef attackerChanceScalar;
        public StatDef victimChanceScalar;
        public float chanceCap = 1f;
        public bool triggersPyroThought;
    }
    public class HediffComp_ExtraOnHitEffects : HediffComp
    {
        public HediffCompProperties_ExtraOnHitEffects Props
        {
            get
            {
                return (HediffCompProperties_ExtraOnHitEffects)this.props;
            }
        }
        public virtual string FXTooltip()
        {
            return "";
        }
        public virtual float ChanceForVictim(Pawn victim)
        {
            return Math.Min(this.Props.chanceCap, this.Props.chance * (this.Props.attackerChanceScalar != null ? this.Pawn.GetStatValue(this.Props.attackerChanceScalar) : 1f) * (this.Props.victimChanceScalar != null ? victim.GetStatValue(this.Props.victimChanceScalar) : 1f));
        }
        public virtual void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            this.parent.Severity += this.Props.severityChangeOnHit;
        }
        public virtual float ScaledValue(Pawn victim, float basicEffectValue, float valueToScale)
        {
            return basicEffectValue * valueToScale * (this.Props.attackerScalar != null ? this.Pawn.GetStatValue(this.Props.attackerScalar) : 1f) * (this.Props.victimScalar != null ? victim.GetStatValue(this.Props.victimScalar) : 1f) / (this.Props.victimBodySizeInverseScaling ? victim.BodySize : 1f);
        }
        public virtual float ChanceForVictimThing(Thing victim)
        {
            return Math.Min(this.Props.chanceCap, this.Props.chance * (this.Props.attackerChanceScalar != null ? this.Pawn.GetStatValue(this.Props.attackerChanceScalar) : 1f) * (this.Props.victimChanceScalar != null ? victim.GetStatValue(this.Props.victimChanceScalar) : 1f));
        }
        public virtual void DoExtraEffectsThing(Thing victim, float valueToScale)
        {
            this.parent.Severity += this.Props.severityChangeOnHit;
        }
        public virtual float ScaledValueThing(Thing victim, float basicEffectValue, float valueToScale)
        {
            return basicEffectValue * valueToScale * (this.Props.attackerScalar != null ? this.Pawn.GetStatValue(this.Props.attackerScalar) : 1f) * (this.Props.victimScalar != null ? victim.GetStatValue(this.Props.victimScalar) : 1f);
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showTooltip)
                {
                    string result = "";
                    if (this.Props.chance <= 1f)
                    {
                        result += "Hauts_ExtraHitFXPrefixChance".Translate(this.Props.chance.ToStringPercent(), this.Props.minDmgToTrigger);
                    } else {
                        result += "Hauts_ExtraHitFXPrefixAlways".Translate(this.Props.minDmgToTrigger.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                    }
                    if (this.Props.cellRange <= 255f)
                    {
                        result += "Hauts_ExtraHitFXRange".Translate(Mathf.RoundToInt(this.Props.cellRange));
                    }
                    if (this.Props.tickCooldown.max > 0)
                    {
                        if (this.Props.tickCooldown.min != this.Props.tickCooldown.max)
                        {
                            result += "Hauts_ExtraHitFXPrefixCDVariable".Translate(this.Props.tickCooldown.min, this.Props.tickCooldown.max);
                        } else {
                            result += "Hauts_ExtraHitFXPrefixCD".Translate(this.Props.tickCooldown.min);
                        }
                    } else {
                        result += "Hauts_ExtraHitFXPrefixNoCD".Translate();
                    }
                    result += this.FXTooltip();
                    if (this.Props.damageScaling || this.Props.attackerScalar != null || this.Props.victimScalar != null)
                    {
                        result += "Hauts_ExtraHitFXScalars".Translate();
                        bool prev = false;
                        if (this.Props.damageScaling)
                        {
                            result += "Hauts_ExtraHitFXScaleDmgDealt".Translate();
                            prev = true;
                        }
                        if (this.Props.attackerScalar != null)
                        {
                            if (prev)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXScaleAttacker".Translate(this.Props.attackerScalar.label);
                        }
                        if (this.Props.victimScalar != null)
                        {
                            if (prev)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXScaleVictim".Translate(this.Props.victimScalar.label);
                        }
                    }
                    if (!this.Props.canAffectAnimals || !this.Props.canAffectFriendlies || !this.Props.canAffectHostiles || !this.Props.canAffectHumanlikes || !this.Props.canAffectMechs || !this.Props.canAffectDrones)
                    {
                        result += "\n";
                        result += "Hauts_ExtraHitFXSuffix".Translate();
                        bool prev2 = false;
                        if (!this.Props.canAffectAnimals)
                        {
                            result += "Hauts_ExtraHitFXSuffix2A".Translate();
                            prev2 = true;
                        }
                        if (!this.Props.canAffectFriendlies)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffixF".Translate();
                        }
                        if (!this.Props.canAffectHostiles)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffixH".Translate();
                        }
                        if (!this.Props.canAffectHumanlikes)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2H".Translate();
                        }
                        if (!this.Props.canAffectMechs)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2M".Translate();
                        }
                        if (!this.Props.canAffectDrones)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2D".Translate();
                        }
                        if (!this.Props.canAffectMutants)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2Mu".Translate();
                        }
                        if (!this.Props.canAffectEntities)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2E".Translate();
                        }
                    }
                    return result.CapitalizeFirst();
                }
                return null;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.cooldown = Find.TickManager.TicksGame;
        }
        public virtual bool CanAffectTarget(Pawn pawn)
        {
            return Rand.Chance(this.ChanceForVictim(pawn)) && (this.Pawn.HostileTo(pawn) ? this.Props.canAffectHostiles : this.Props.canAffectFriendlies) && (pawn.IsMutant ? this.Props.canAffectMutants : ((this.Props.canAffectAnimals || !pawn.RaceProps.Animal) && (this.Props.canAffectHumanlikes || !pawn.RaceProps.Humanlike) && (this.Props.canAffectMechs || !pawn.RaceProps.IsMechanoid) && (this.Props.canAffectDrones || !pawn.RaceProps.IsDrone) && (this.Props.canAffectEntities || !pawn.RaceProps.IsAnomalyEntity)));
        }
        public virtual bool CanAffectTargetThing(Thing thing)
        {
            return Rand.Chance(this.ChanceForVictimThing(thing)) && (this.Pawn.HostileTo(thing) ? this.Props.canAffectHostiles : this.Props.canAffectFriendlies) && ((thing is Building && this.Props.canAffectBuildings) || (thing is Plant && this.Props.canAffectPlants) || (thing.def.category == ThingCategory.Item));
        }
        public virtual bool RangeCheck(Thing thing, DamageInfo dinfo)
        {
            if (thing.Tile != this.Pawn.Tile)
            {
                return Find.WorldGrid.TraversalDistanceBetween(thing.Tile, this.Pawn.Tile, true) <= this.Props.worldTileRange;
            }
            if (thing.SpawnedOrAnyParentSpawned && this.Pawn.SpawnedOrAnyParentSpawned)
            {
                float cellDist = thing.Position.DistanceTo(this.Pawn.Position) - Math.Max(1.42f, this.Props.cellRange);
                if (cellDist > 0 && dinfo.Weapon != null && dinfo.Weapon.StatBaseDefined(VEFDefOf.VEF_MeleeWeaponRange))
                {
                    cellDist -= dinfo.Weapon.GetStatValueAbstract(VEFDefOf.VEF_MeleeWeaponRange);
                }
                return cellDist <= 0f;
            }
            return false;
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (ModsConfig.RoyaltyActive && this.Props.appliedViaPsycasts && target != null && this.cooldown <= Find.TickManager.TicksGame)
            {
                if (verb is RimWorld.Verb_CastAbility vca && vca.ability is Psycast psycast)
                {
                    List<LocalTargetInfo> targets = vca.ability.GetAffectedTargets(target).ToList();
                    foreach (LocalTargetInfo lti in targets)
                    {
                        if (lti.Thing != null)
                        {
                            if (lti.Pawn != null && this.CanAffectTarget(lti.Pawn))
                            {
                                this.DoExtraEffects(lti.Pawn, (this.Props.psyfocusCostScaling ? 100f * psycast.FinalPsyfocusCost(target) : 1f) * (this.Props.entropyCostScaling ? psycast.def.EntropyGain : 1f), null);
                            }
                            else if (this.CanAffectTargetThing(lti.Thing))
                            {
                                this.DoExtraEffectsThing(lti.Thing, (this.Props.psyfocusCostScaling ? 100f * psycast.FinalPsyfocusCost(target) : 1f) * (this.Props.entropyCostScaling ? psycast.def.EntropyGain : 1f));
                            }
                        }
                    }
                }
                if (verb is VEF.Abilities.Verb_CastAbility vcavfe && vcavfe.Caster != null && vcavfe.CasterIsPawn && ModCompatibilityUtility.IsVPEPsycast(vcavfe.ability))
                {
                    GlobalTargetInfo[] targets = new GlobalTargetInfo[]
                    {
                        target.ToGlobalTargetInfo(vcavfe.Caster.Map)
                    };
                    vcavfe.ability.ModifyTargets(ref targets);
                    foreach (LocalTargetInfo lti in targets)
                    {
                        if (lti.Thing != null)
                        {
                            if (lti.Pawn != null && this.CanAffectTarget(lti.Pawn))
                            {
                                this.DoExtraEffects(lti.Pawn, (this.Props.psyfocusCostScaling ? 100f * ModCompatibilityUtility.GetVPEPsyfocusCost(vcavfe.ability) : 1f) * (this.Props.entropyCostScaling ? ModCompatibilityUtility.GetVPEEntropyCost(vcavfe.ability) : 1f), null);
                            } else if (this.CanAffectTargetThing(lti.Thing)) {
                                this.DoExtraEffectsThing(lti.Thing, (this.Props.psyfocusCostScaling ? 100f * ModCompatibilityUtility.GetVPEPsyfocusCost(vcavfe.ability) : 1f) * (this.Props.entropyCostScaling ? ModCompatibilityUtility.GetVPEEntropyCost(vcavfe.ability) : 1f));
                            }
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.cooldown, "cooldown", 0, false);
        }
        public int cooldown;
    }
    /*hediffsToCure: affects any of these hediffs found on the victim on hit…
     *severityLostOnCure: …by removing a randomly chosen value from within this range from its severity…
     *totallyRemoveOnCure: …or just outright removing these hediffs.
     *maxHediffsCuredPerHit: can only remove this many affectable hediffs per hit
     *onlyOnHitPart: if true, will only affect hediffs on the hit body part (if any).*/
    public class HediffCompProperties_CureHediffsOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_CureHediffsOnHit()
        {
            this.compClass = typeof(HediffComp_CureHediffsOnHit);
        }
        public List<HediffDef> hediffsToCure;
        public FloatRange severityLostOnCure = new FloatRange(99999f, 99999f);
        public bool totallyRemoveOnCure = true;
        public int maxHediffsCuredPerHit = 99999;
        public bool onlyOnHitPart = false;
    }
    public class HediffComp_CureHediffsOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_CureHediffsOnHit Props
        {
            get
            {
                return (HediffCompProperties_CureHediffsOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.hediffsToCure != null)
            {
                if (this.Props.totallyRemoveOnCure)
                {
                    foreach (HediffDef hed in this.Props.hediffsToCure)
                    {
                        result += "Hauts_ExtraHitFXPurge".Translate(hed.LabelCap);
                    }
                }
                else
                {
                    foreach (HediffDef hed in this.Props.hediffsToCure)
                    {
                        if (this.Props.severityLostOnCure.max > 0)
                        {
                            if (this.Props.severityLostOnCure.min != this.Props.severityLostOnCure.max)
                            {
                                result += "Hauts_ExtraHitFXPurgePartialVariable".Translate(this.Props.severityLostOnCure.min.ToStringByStyle(ToStringStyle.FloatTwo), this.Props.severityLostOnCure.max.ToStringByStyle(ToStringStyle.FloatTwo), hed.LabelCap);
                            }
                            else
                            {
                                result += "Hauts_ExtraHitFXPurgePartial".Translate(this.Props.severityLostOnCure.max.ToStringByStyle(ToStringStyle.FloatTwo), hed.LabelCap);
                            }
                        }
                    }
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.hediffsToCure != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                int curesRemaining = this.Props.maxHediffsCuredPerHit;
                for (int i = victim.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff h = victim.health.hediffSet.hediffs[i];
                    if (curesRemaining <= 0)
                    {
                        break;
                    }
                    if (this.Props.hediffsToCure.Contains(h.def) && (hitPart == null || (this.Props.onlyOnHitPart && h.Part == hitPart)))
                    {
                        if (this.Props.totallyRemoveOnCure)
                        {
                            victim.health.RemoveHediff(h);
                            curesRemaining--;
                        }
                        else
                        {
                            h.Severity -= this.ScaledValue(victim, this.Props.severityLostOnCure.RandomInRange, valueToScale);
                            curesRemaining--;
                        }
                    }
                }
            }
        }
    }
    //ExtraDamages, like what zeushammers or plasmaswords have.
    public class HediffCompProperties_ExtraDamageOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_ExtraDamageOnHit()
        {
            this.compClass = typeof(HediffComp_ExtraDamageOnHit);
        }
        public List<ExtraDamage> extraDamages;
    }
    public class HediffComp_ExtraDamageOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_ExtraDamageOnHit Props
        {
            get
            {
                return (HediffCompProperties_ExtraDamageOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.extraDamages != null)
            {
                foreach (ExtraDamage ed in this.Props.extraDamages)
                {
                    if (ed.chance < 1f)
                    {
                        result += "Hauts_ExtraHitFXMoreDmgChance".Translate(ed.chance.ToStringPercent(), ed.amount, ed.def.label);
                    }
                    else
                    {
                        result += "Hauts_ExtraHitFXMoreDmg".Translate(ed.amount, ed.def.label);
                    }
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.extraDamages != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                foreach (ExtraDamage extraDamage in this.Props.extraDamages)
                {
                    if (Rand.Chance(extraDamage.chance))
                    {
                        DamageInfo dinfo2 = new DamageInfo(extraDamage.def, this.ScaledValue(victim, extraDamage.amount, valueToScale), extraDamage.AdjustedArmorPenetration(), -1f, ModCompatibilityUtility.CombatIsExtended() ? null : this.Pawn, hitPart != null ? hitPart : victim.health.hediffSet.GetRandomNotMissingPart(extraDamage.def), null, DamageInfo.SourceCategory.ThingOrUnknown);
                        dinfo2.SetWeaponHediff(this.parent.def);
                        victim.TakeDamage(dinfo2);
                    }
                }
            }
        }
        public override void DoExtraEffectsThing(Thing victim, float valueToScale)
        {
            base.DoExtraEffectsThing(victim, valueToScale);
            if (this.Props.extraDamages != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                foreach (ExtraDamage extraDamage in this.Props.extraDamages)
                {
                    if (Rand.Chance(extraDamage.chance))
                    {
                        DamageInfo dinfo2 = new DamageInfo(extraDamage.def, this.ScaledValueThing(victim, extraDamage.amount, valueToScale), extraDamage.AdjustedArmorPenetration(), -1f, ModCompatibilityUtility.CombatIsExtended() ? null : this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                        dinfo2.SetWeaponHediff(this.parent.def);
                        victim.TakeDamage(dinfo2);
                    }
                }
            }
        }
    }
    /*grants this hediff with baseSeverity (or canOnlyIncreaseSeverityUpTo, if the hediff already exists) severity to the target. Can be localized to the hit part
     !!!!!DO NOT USE THIS TO ADD INJURY HEDIFFS!!!!! Use ExtraDamageOnHIt instead*/
    public class HediffCompProperties_InflictHediffOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_InflictHediffOnHit()
        {
            this.compClass = typeof(HediffComp_InflictHediffOnHit);
        }
        public HediffDef hediff;
        public float baseSeverity = 1f;
        public float canOnlyIncreaseSeverityUpTo = -999f;
        public bool localizedToHitPart = true;
    }
    public class HediffComp_InflictHediffOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_InflictHediffOnHit Props
        {
            get
            {
                return (HediffCompProperties_InflictHediffOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.hediff != null)
            {
                result += "Hauts_ExtraHitFXDebuff".Translate(this.Props.baseSeverity, this.Props.hediff);
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.hediff != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                float severity = this.ScaledValue(victim, this.Props.baseSeverity, valueToScale);
                Hediff alreadyExtant = victim.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff);
                if (alreadyExtant != null && (!this.Props.localizedToHitPart || alreadyExtant.Part == hitPart))
                {
                    severity += alreadyExtant.Severity;
                    if (this.Props.canOnlyIncreaseSeverityUpTo > 0f)
                    {
                        severity = Math.Min(severity, this.Props.canOnlyIncreaseSeverityUpTo);
                    }
                    alreadyExtant.Severity = severity;
                }
                else
                {
                    BodyPartRecord whereToAdd = this.Props.localizedToHitPart ? hitPart : null;
                    Hediff toAdd = HediffMaker.MakeHediff(this.Props.hediff, victim, whereToAdd);
                    if (this.Props.canOnlyIncreaseSeverityUpTo > 0f)
                    {
                        severity = Math.Min(severity, this.Props.canOnlyIncreaseSeverityUpTo);
                    }
                    toAdd.Severity = severity;
                    victim.health.AddHediff(toAdd, whereToAdd, null, null);
                }
            }
        }
    }
    //float values in dictionary are weightings
    public class HediffCompProperties_InspireOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_InspireOnHit()
        {
            this.compClass = typeof(HediffComp_InspireOnHit);
        }
        public Dictionary<InspirationDef, float> inspirationList;
    }
    public class HediffComp_InspireOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_InspireOnHit Props
        {
            get
            {
                return (HediffCompProperties_InspireOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.inspirationList == null)
            {
                result += "Hauts_ExtraHitFXInspireAny".Translate();
            }
            else
            {
                result += "Hauts_ExtraHitFXInspireList".Translate();
                bool subsequentListing = false;
                foreach (InspirationDef id in this.Props.inspirationList.Keys)
                {
                    if (subsequentListing)
                    {
                        result += ", ";
                    }
                    result += "Hauts_ExtraHitFXListicle".Translate(id.LabelCap, this.Props.inspirationList.TryGetValue(id).ToStringByStyle(ToStringStyle.FloatMaxOne));
                    subsequentListing = true;
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (victim.mindState.inspirationHandler != null)
            {
                InspirationDef id = victim.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                if (id != null)
                {
                    if (this.Props.inspirationList == null)
                    {
                        victim.mindState.inspirationHandler.TryStartInspiration(id, "Hauts_GotInspiredOnHit".Translate(victim.Named("PAWN"), this.parent.Label), true);
                    }
                    else
                    {
                        int tries = 100;
                        while (tries > 0)
                        {
                            this.Props.inspirationList.Keys.TryRandomElementByWeight((InspirationDef d) => this.Props.inspirationList.TryGetValue(d), out id);
                            if (victim.mindState.inspirationHandler.TryStartInspiration(id, "Hauts_GotInspiredOnHit".Translate(victim.Name.ToStringShort, this.parent.Label), true))
                            {
                                break;
                            }
                            tries--;
                        }
                    }
                }
            }
        }
    }
    //dictionary works like InspireOnHit. forceWake does what you would expect, and since some mental states need the victim to wake up to do anything, you should probably keep it on
    public class HediffCompProperties_MentalStateOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_MentalStateOnHit()
        {
            this.compClass = typeof(HediffComp_MentalStateOnHit);
        }
        public Dictionary<MentalStateDef, float> mbList;
        public bool forceWake = true;
    }
    public class HediffComp_MentalStateOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_MentalStateOnHit Props
        {
            get
            {
                return (HediffCompProperties_MentalStateOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.mbList == null)
            {
                result += "Hauts_ExtraHitFXMBAny".Translate();
            }
            else
            {
                result += "Hauts_ExtraHitFXMBList".Translate();
                bool subsequentListing = false;
                foreach (MentalStateDef mb in this.Props.mbList.Keys)
                {
                    if (subsequentListing)
                    {
                        result += ", ";
                    }
                    result += "Hauts_ExtraHitFXListicle".Translate(mb.LabelCap, this.Props.mbList.TryGetValue(mb).ToStringByStyle(ToStringStyle.FloatMaxOne));
                    subsequentListing = true;
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            MentalStateDef mb;
            int tries = 100;
            if (this.Props.mbList == null)
            {
                while (tries > 0)
                {
                    mb = DefDatabase<MentalStateDef>.GetRandom();
                    if (mb.Worker.StateCanOccur(victim))
                    {
                        victim.mindState.mentalStateHandler.TryStartMentalState(mb, "Hauts_GotMBOnHit".Translate(this.parent.Label), false, this.Props.forceWake);
                        break;
                    }
                    tries--;
                }
            }
            else
            {
                while (tries > 0)
                {
                    this.Props.mbList.Keys.TryRandomElementByWeight((MentalStateDef d) => this.Props.mbList.TryGetValue(d), out mb);
                    if (mb.Worker.StateCanOccur(victim))
                    {
                        victim.mindState.mentalStateHandler.TryStartMentalState(mb, "Hauts_GotMBOnHit".Translate(this.parent.Label), false, this.Props.forceWake);
                        break;
                    }
                    tries--;
                }
            }
        }
    }
    //oooooh I wonder what this does
    public class HediffCompProperties_StunOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_StunOnHit()
        {
            this.compClass = typeof(HediffComp_StunOnHit);
        }
        public IntRange stunTicksRange = new IntRange(-1, -1);
    }
    public class HediffComp_StunOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_StunOnHit Props
        {
            get
            {
                return (HediffCompProperties_StunOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.stunTicksRange.max > 0)
            {
                if (this.Props.stunTicksRange.min != this.Props.stunTicksRange.max)
                {
                    result += "Hauts_ExtraHitFXStunVariable".Translate(this.Props.stunTicksRange.min, this.Props.stunTicksRange.max);
                }
                else
                {
                    result += "Hauts_ExtraHitFXStun".Translate(this.Props.stunTicksRange.min);
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.stunTicksRange.min > 0 && this.Props.stunTicksRange.max > 0 && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                victim.stances.stunner.StunFor((int)this.ScaledValue(victim, (float)this.Props.stunTicksRange.RandomInRange, valueToScale), this.parent.pawn, false);
            }
        }
    }
    /*as of 1.6, hediffs have the Notify_PawnDamagedThing method which triggers on doing damage. This is to make scaria-infected animals be able to pass on the infection via bites or scratches, although tbf I've literally never seen it happen in the course of play.
     * EOHEs utilize this system (as opposed to requiring a bespoke Harmony patch prior to 1.6) in order to cause some sort of effect on the victim. However, since this is a Hediff method,
     * not a HediffComp method, EOHEs now need to be assigned to hediffs with one of the following classes (or some other custom class which invokes the utility method when doing Notify_PawnDamagedThing)
     * if the EOHE only works with psycasts, not attacks, you don't need to use these.*/
    public class Hediff_HasExtraOnHitEffects : HediffWithComps
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsMiscUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
    public class Hediff_ImplantHasExtraOnHitEffects : Hediff_Implant
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsMiscUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
    public class Hediff_AddedPartHasExtraOnHitEffects : Hediff_AddedPart
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsMiscUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
    //see HediffComps_PreDamageModification.cs
    public class Hediff_PreDamageModificationHasExtraOnHitEffects : Hediff_PreDamageModification
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsMiscUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
}
