using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsFramework
{
    /*Enables an effect to occur on receiving damage; this is after shields and damage factors, but before the damage actually harms the pawn
     * If a pawn has multiple PDMs, their effects occur in an order based on their “priority” field; if, at the end of a PDM taking effect the damage is fully negated, subsequent PDMs in the chain will not take effect.
     * To enhance performance (check every hediff rather than check every hediff and their comps), PDMs will only work on Hediffs of the Hediff_PreDamageModification type (which is a HediffWithComps derivative) or its own derivatives.
     * By itself, PDM has no effect. See its children.
     * minSeverityToWork: will not do anything unless severity is higher than this value
     * minDmgToTrigger: only works on instances of incoming damage >= this value
     * unaffectedDamageTypes: will not work on instances of incoming damage which deal this damage type
     * affectedDamageTypes: if this list is not empty, will ONLY work on instances of incoming damage which deal this damage type
     * harmfulDamageTypesOnly: if true, only works if the incoming damage type harmsHealth
     * reactsToRanged: if false, won’t react to isRanged DamageDefs
     * reactsToExplosive: if false, won’t react to isExplosive DamageDefs
     * reactsToOther: if false, won’t react to non-isRanged, non-isExplosive DamageDefs
     * chance: only works on an instance of incoming damage if a random number from 0-1 <= this value…
     * chanceScalar: … times however much of this stat the pawn has…
     * maxChance: …capped at this value
     * shouldUseIncomingDamageFactor: applies incoming damage factor to the work’s effect, AND to the “cost”...
     * severityOnHit: … which is the amount of severity added to this hediff on doing work…
     * severityChangesEvenOnFail: … or even if no work is done because the chance check failed
     * damageScalesSeverityLoss: multiplies the cost by the amount of damage
     * noCostIfInvincible: will not incur cost if incoming damage factor is 0
     * ShouldDoEffect: determines whether or not to do any of the pre-damage modification effects. This is where chance, minSeverityToWork, harmfulDamageTypesOnly, un/affectedDamaggeTypes, and reactsToRanged/Explosive/Other are used
     * PayCostOfHit: if ShouldPayCostOfHit, adds severity to this hediff based on the damage amount. Invoked if ShouldDoEffect is true and either 1) severityChangesEvenOnFail is true or 2) damage >= minDmgToTrigger and the chance check is passed
     * ShouldPayCostOfHit: invoked inside PayCostOfHit, determines whether any severity change should actually occur; this is where noCostIfInvincible is used
     * ShouldDoModificationInner: determines whether or not to DoModificationInner. Does NOT affect whether or not to PayCostOfHit
     * DoModificationInner: the actual effect.
     * TryDoModification: overarching method that invokes all other virtual methods*/
    public class Hediff_PreDamageModification : HediffWithComps
    {

    }
    public class HediffCompProperties_PreDamageModification : HediffCompProperties
    {
        public HediffCompProperties_PreDamageModification()
        {
            this.compClass = typeof(HediffComp_PreDamageModification);
        }
        public float minSeverityToWork = 0f;
        public float minDmgToTrigger = 0f;
        public List<DamageDef> unaffectedDamageTypes;
        public List<DamageDef> affectedDamageTypes;
        public bool harmfulDamageTypesOnly = true;
        public float chance = 1f;
        public StatDef chanceScalar;
        public float maxChance = 1f;
        public bool shouldUseIncomingDamageFactor = true;
        public float severityOnHit = 0f;
        public bool severityChangesEvenOnFail = false;
        public bool damageScalesSeverityLoss = false;
        public bool noCostIfInvincible = true;
        public int priority = 100;
        public bool reactsToRanged = true;
        public bool reactsToExplosive = true;
        public bool reactsToShieldBypassers = true;
        public bool reactsToOther = true;
    }
    public class HediffComp_PreDamageModification : HediffComp
    {
        public HediffCompProperties_PreDamageModification Props
        {
            get
            {
                return (HediffCompProperties_PreDamageModification)this.props;
            }
        }
        public virtual bool ShouldDoEffect(DamageInfo dinfo)
        {
            return this.parent.Severity >= this.Props.minSeverityToWork && (!this.Props.harmfulDamageTypesOnly || dinfo.Def.harmsHealth) && (dinfo.Def.isRanged ? this.Props.reactsToRanged : (dinfo.Def.isExplosive ? this.Props.reactsToExplosive : this.Props.reactsToOther)) && (this.Props.affectedDamageTypes == null || this.Props.affectedDamageTypes.Contains(dinfo.Def)) && (!dinfo.Def.ignoreShields || this.Props.reactsToShieldBypassers) && (this.Props.unaffectedDamageTypes == null || !this.Props.unaffectedDamageTypes.Contains(dinfo.Def));
        }
        public virtual bool ShouldDoModificationInner(DamageInfo dinfo)
        {
            return true;
        }
        public virtual bool ShouldPayCostOfHit(DamageInfo dinfo, bool absorbed)
        {
            if (this.Props.noCostIfInvincible && this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) <= float.Epsilon)
            {
                return false;
            }
            return true;
        }
        public virtual void PayCostOfHit(float damageAmount)
        {
            this.parent.Severity += this.Props.severityOnHit * (this.Props.damageScalesSeverityLoss ? damageAmount : 1f) * (this.Props.shouldUseIncomingDamageFactor ? this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) : 1f);
        }
        public virtual void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
        }
        public bool ChanceCapped()
        {
            return Rand.Chance(Math.Min(this.Props.maxChance, this.Props.chance * (this.Props.chanceScalar != null ? this.Pawn.GetStatValue(this.Props.chanceScalar) : 1f)));
        }
        public virtual void TryDoModification(ref DamageInfo dinfo, ref bool absorbed)
        {
            if (this.ShouldDoEffect(dinfo))
            {
                if (dinfo.Amount >= this.Props.minDmgToTrigger && this.ChanceCapped())
                {
                    float amount = dinfo.Amount;
                    if (this.ShouldPayCostOfHit(dinfo, absorbed))
                    {
                        this.PayCostOfHit(amount);
                    }
                    if (this.ShouldDoModificationInner(dinfo))
                    {
                        this.DoModificationInner(ref dinfo, ref absorbed, amount);
                    }
                } else if (this.Props.severityChangesEvenOnFail && this.ShouldPayCostOfHit(dinfo, absorbed)) {
                    this.PayCostOfHit(dinfo.Amount);
                }
            }
        }
    }
    /*This child is meant to reduce the damage, potentially completely blocking it.
     * reactsToShieldBypassers: if false, will not work on instance of incoming damage whose damage type has ignoresShields set to true (e.g. Odyssey's beam damage def)
     * damageAdded: adds a random amount from this much damage to the incoming damage. In case it's not obvious, negative values lower the incoming damage, positive raise it (the comp is called DamageNegation since that's what I intended it for, but you could totally go the other way).
     * damageMultiplier: multiplies the incoming damage by this much
     * soundOnBlock: plays this sound on doing work
     * fleckOnBlock: shows this fleck on doing work, slightly angled towards the direction of the damage
     * throwDustPuffsOnBlock: throws dust puff visual effects in proportion to the damage intercepted on doing work
     * onlyDoGraphicsOnFullNegation: only plays the sound, flecks, and dust puffs if the damage was fully negated by the work of this hediff
     * removeBadAttachables: (via Harmony patch) prevents AttachableThings with the BadAttachable DefModExtension from attaching to this pawn (ast least if …
     * ShouldPreventAttachment returns true). Fire is a BadAttachable, as are some modded attachables. You should probably make this true for anything that fully negates damage whenever on, as pawns with such a buff tend to not bother dousing fires on themselves.*/
    public class HediffCompProperties_DamageNegation : HediffCompProperties_PreDamageModification
    {
        public HediffCompProperties_DamageNegation()
        {
            this.compClass = typeof(HediffComp_DamageNegation);
        }
        public FloatRange damageAdded = new FloatRange(0f);
        public float damageMultiplier = 0f;
        public SoundDef soundOnBlock;
        public FleckDef fleckOnBlock;
        public bool fleckScaleWithDamage = true;
        public bool centerFleckOnCharacter;
        public float minFleckSize = 10f;
        public bool throwDustPuffsOnBlock = true;
        public bool onlyDoGraphicsOnFullNegation = true;
        public bool throwText;
        public string textToThrow;
        public bool removeBadAttachables;
    }
    public class HediffComp_DamageNegation : HediffComp_PreDamageModification
    {
        public new HediffCompProperties_DamageNegation Props
        {
            get
            {
                return (HediffCompProperties_DamageNegation)this.props;
            }
        }
        public virtual bool ShouldPreventAttachment(Thing attachment)
        {
            return this.parent.Severity >= this.Props.minSeverityToWork && this.ChanceCapped();
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            base.DoModificationInner(ref dinfo, ref absorbed, amount);
            dinfo.SetAmount(Math.Max(0f, ((dinfo.Amount * this.Props.damageMultiplier) + this.Props.damageAdded.RandomInRange) * (this.Props.shouldUseIncomingDamageFactor ? this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) : 1f)));
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                if (dinfo.Amount == 0f)
                {
                    absorbed = true;
                    this.DoGraphics(dinfo, amount);
                }
                else if (!this.Props.onlyDoGraphicsOnFullNegation)
                {
                    this.DoGraphics(dinfo, Math.Min(0f, amount - dinfo.Amount));
                }
            }
        }
        public virtual void DoGraphics(DamageInfo dinfo, float amount)
        {
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                if (this.Props.soundOnBlock != null)
                {
                    this.Props.soundOnBlock.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                }
                if (amount > 0)
                {
                    Vector3 loc = this.Pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle).RotatedBy(180f) * 0.5f;
                    float num = Mathf.Min(this.Props.minFleckSize, 2f + amount / 10f);
                    if (this.Props.fleckOnBlock != null)
                    {
                        if (this.Props.centerFleckOnCharacter)
                        {
                            FleckMaker.Static(this.Pawn.TrueCenter(), this.Pawn.Map, this.Props.fleckOnBlock, this.Props.fleckScaleWithDamage ? num : this.Props.minFleckSize);
                        }
                        else
                        {
                            FleckMaker.Static(loc, this.Pawn.Map, this.Props.fleckOnBlock, this.Props.fleckScaleWithDamage ? num : this.Props.minFleckSize);
                        }
                    }
                    if (this.Props.throwText)
                    {
                        Vector3 locText = new Vector3((float)this.Pawn.Position.x + 1f, (float)this.Pawn.Position.y, (float)this.Pawn.Position.z + 1f);
                        string text = dinfo.Def.adaptedText ?? this.Props.textToThrow.Translate();
                        MoteMaker.ThrowText(locText, this.Pawn.Map, text, Color.white, -1f);
                    }
                    if (this.Props.throwDustPuffsOnBlock)
                    {
                        int num2 = (int)num;
                        for (int i = 0; i < num2; i++)
                        {
                            FleckMaker.ThrowDustPuff(loc, this.Pawn.Map, Rand.Range(0.8f, 1.2f));
                        }
                    }
                }
            }
        }
    }
    /*This GRANDchild behaves closer to a classic shield.
     instantlyOverwhelmedBy: taking this damage type (even if it would otherwise not react to this damage type) breaks the shield
    destroyIfOverwhelmed: if true, this hediff is destroyed once the shield runs out of energy
    breakEffect: this Effecter instantiates over the pawn when the shield breaks
    visualRange: adjusts the size of the on-break Effecter - it should be the same size range as the MoteConditionalShield, if any
    resetSound: plays when the shield begins to recharge
    lightningGlowOnReset: creates a luminous flash over the pawn on reset if true
    EnergyGainPerTick: how much severity the shield gains per second while not broken, initially set to [baseEnergyRechargeRate*however much rechargeRateScalar the pawn has if any/60] but can be adjusted via set; however…
    MaxEnergy: …the shield’s severity can’t exceed its max energy, which you can set to be separate from its max severity if necessary; initially set to [baseMaxEnergy*however much maxEnergyScalar the pawn has if any], but can be adjusted via set
    ResetDelayTicks: how long it takes to reset after being broken, initially set to baseStartingTicksToReset, but can be adjusted via set
    Energy: the current energy the shield has, = [its severity - minSeverityToWork]
    RedetermineAllStats: performs the calculations which determine MaxEnergy, ResetDelayTicks, and Energy; used when this hediff is created and every 60 ticks besides
    ResetShield: handles the changes to severity and graphical effects that occur when the shield resets or is first instantiated, including setting the severity to [minSeverityToWork + energyOnReset]
    BreakShield: handles the graphical effects, destroyIfOverwhelmed, and severity adjustment to [minSeverityToWork/2] that occur when the shield breaks*/
    public class HediffCompProperties_DamageNegationShield : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_DamageNegationShield()
        {
            this.compClass = typeof(HediffComp_DamageNegationShield);
        }
        public DamageDef instantlyOverwhelmedBy;
        public bool destroyIfOverwhelmed;
        public bool blocksRangedWeapons;
        public int baseStartingTicksToReset = 1;
        public float energyOnReset = 1;
        public float baseEnergyRechargeRate = 1;
        public float baseMaxEnergy = 1;
        public StatDef rechargeRateScalar;
        public StatDef maxEnergyScalar;
        public EffecterDef breakEffect;
        public FloatRange visualRange;
        public SoundDef resetSound;
        public bool lightningGlowOnReset = true;
    }
    public class HediffComp_DamageNegationShield : HediffComp_DamageNegation
    {
        public new HediffCompProperties_DamageNegationShield Props
        {
            get
            {
                return (HediffCompProperties_DamageNegationShield)this.props;
            }
        }
        public virtual float EnergyGainPerTick
        {
            get
            {
                return this.energyGainPerTickCached;
            }
            set
            {
                this.energyGainPerTickCached = value;
            }
        }
        public virtual int ResetDelayTicks
        {
            get
            {
                return this.resetDelayTicksCached;
            }
            set
            {
                this.resetDelayTicksCached = value;
            }
        }
        public virtual float MaxEnergy
        {
            get
            {
                return this.maxEnergyCached;
            }
            set
            {
                this.maxEnergyCached = value;
            }
        }
        public float Energy
        {
            get
            {
                return this.parent.Severity - this.Props.minSeverityToWork;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.RedetermineAllStats();
            this.ResetShield();
        }
        public virtual void RedetermineAllStats()
        {
            this.EnergyGainPerTick = this.Props.baseEnergyRechargeRate * (this.Props.rechargeRateScalar != null ? this.Pawn.GetStatValue(this.Props.rechargeRateScalar) : 1f) / 60f;
            this.ResetDelayTicks = this.Props.baseStartingTicksToReset;
            this.MaxEnergy = (this.Props.baseMaxEnergy * (this.Props.maxEnergyScalar != null ? this.Pawn.GetStatValue(this.Props.maxEnergyScalar) : 1f)) + this.Props.minSeverityToWork;
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            return (this.parent.Severity >= this.Props.minSeverityToWork && this.Props.instantlyOverwhelmedBy != null && dinfo.Def == this.Props.instantlyOverwhelmedBy) || base.ShouldDoEffect(dinfo);
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            if (this.Props.instantlyOverwhelmedBy != null && dinfo.Def == this.Props.instantlyOverwhelmedBy)
            {
                this.PayCostOfHit(this.parent.Severity * 2f);
            }
            base.DoModificationInner(ref dinfo, ref absorbed, amount);
        }
        public override void PayCostOfHit(float damageAmount)
        {
            base.PayCostOfHit(damageAmount);
            if (this.Energy < 0)
            {
                this.BreakShield();
            }
        }
        public virtual void ResetShield()
        {
            this.parent.Severity = this.Props.minSeverityToWork + this.Props.energyOnReset;
            if (this.Pawn.Spawned)
            {
                if (this.Props.resetSound != null)
                {
                    this.Props.resetSound.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                }
                if (this.Props.lightningGlowOnReset)
                {
                    FleckMaker.ThrowLightningGlow(this.Pawn.TrueCenter(), this.Pawn.Map, 3f);
                }
            }
        }
        public virtual void BreakShield()
        {
            this.ticksToReset = this.ResetDelayTicks;
            if (this.Pawn.Spawned)
            {
                float num = Mathf.Lerp(this.Props.visualRange.min, this.Props.visualRange.max, this.parent.Severity);
                if (this.Props.breakEffect != null)
                {
                    this.Props.breakEffect.SpawnAttached(this.Pawn, this.Pawn.MapHeld, num);
                }
                if (this.Props.fleckOnBlock != null)
                {
                    FleckMaker.Static(this.Pawn.TrueCenter(), this.Pawn.Map, this.Props.fleckOnBlock, this.Props.minFleckSize * 1.2f);
                }
                if (this.Props.throwDustPuffsOnBlock)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        FleckMaker.ThrowDustPuff(this.Pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle((float)Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), this.Pawn.Map, Rand.Range(0.8f, 1.2f));
                    }
                }
            }
            if (this.Props.destroyIfOverwhelmed)
            {
                this.Pawn.health.RemoveHediff(this.parent);
                return;
            }
            else
            {
                this.parent.Severity = this.Props.minSeverityToWork / 2f;
            }
        }
        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (this.ticksToReset <= 0)
                {
                    return (this.parent.Severity - this.Props.minSeverityToWork).ToStringByStyle(ToStringStyle.FloatOne) + "/" + this.MaxEnergy.ToStringByStyle(ToStringStyle.FloatOne);
                }
                return "Hauts_ShieldRecharge".Translate((this.ticksToReset / 60));
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.ticksToReset > 0)
            {
                this.ticksToReset -= delta;
                if (this.ticksToReset <= 0)
                {
                    if (this.parent.Severity < this.Props.minSeverityToWork)
                    {
                        this.ResetShield();
                    }
                    this.parent.Severity = Math.Min((this.EnergyGainPerTick * delta) + this.parent.Severity, this.MaxEnergy);
                    if (this.Energy < 0)
                    {
                        this.BreakShield();
                    }
                }
            }
            else if (this.parent.Severity < this.Props.minSeverityToWork)
            {
                this.ticksToReset = this.ResetDelayTicks;
            }
            else
            {
                this.parent.Severity = Math.Min((this.EnergyGainPerTick * delta) + this.parent.Severity, this.MaxEnergy);
                if (this.Energy < 0)
                {
                    this.BreakShield();
                }
            }
            if (this.Pawn.IsHashIntervalTick(60, delta))
            {
                this.RedetermineAllStats();
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksToReset, "ticksToReset", -1, false);
            Scribe_Values.Look<float>(ref this.maxEnergyCached, "maxEnergyCached", 1, false);
            Scribe_Values.Look<float>(ref this.energyGainPerTickCached, "energyGainPerTickCached", 1, false);
            Scribe_Values.Look<int>(ref this.resetDelayTicksCached, "resetDelayTicksCached", 1, false);
        }
        public int ticksToReset = -1;
        public float maxEnergyCached;
        public float energyGainPerTickCached;
        public int resetDelayTicksCached;
    }
    /*DME for AttachableThings that makes them not attach to pawns with certain DamageNegations.
     * extinguishingDamageDef does not do anything in this framework, but a couple of my other mods reference it, for phenomena that can extinguish any kind of BAs. It should be the damage def typically used to snuff out the attachable*/
    public class BadAttachable : DefModExtension
    {
        public BadAttachable() { }
        public DamageDef extinguishingDamageDef;
    }
    /* onlyRetaliateVsInstigator: if true, retaliation will not be inflicted upon anyone other than the instigator
     * hitInstigatorRegardlessOfRange: inflicts retaliation on the damage instigator (if any), even if it’s outside range
     * canAffectAnimals|Mechs|Humanlikes|Entities|Mutants|Drones: must be true to be able to retaliate against such a pawn. Re: mutants, if that's false, NO mutant can be affected even if it would belong to another canAffect
     * friendlyFire: enables retaliation to affect non-hostile pawns
     * chanceToInflictHediff: if a random number from 0-1 <= this value, retaliation inflicts…
     * hediff: …this hediff…
     * baseHediffSeverity: …with a starting severity = this value…
     * hediffResistStat: …times the victim’s amount of this stat (if extant)...
     * hediffScaleWithDamage: …times the amount of triggering incoming damage if this is true…
     * hediffScaleWithBodySize: …and divided by the victim’s body size if this is true
     * chanceToDoDamage: if a random number from 0-1 <= this value, retaliation inflicts…
     * baseRetaliationDamage: …this amount of damage…
     * retaliationDamageDef: …of this DamageDef…
     * baseRetaliationDamageFactor: …multiplied by this stat of the retaliating pawn (if extant)…
     * damageScaleWithDamage: …multiplied by the amount of triggering incoming damage if this is true…
     * damageScaleWithBodySize: …and divided by the victim’s body size if this is true
     * visualEffect: plays this effecter at the retaliating pawn’s location when retaliation occurs…
     * vfxCooldownTicks: …so long as it’s been at least this long since the last time it was played. Otherwise, the visual spam can get very distracting on a bullet sponge
     * CanHit: determines whether or not a given pawn p is a valid subject of retaliation
     * RetaliateAgainst: handles the actual retaliation effects against any given Pawn
     * RetaliationRange: determines the range at which retaliation can be inflicted on pawns. By default = “range” field*/
    public class HediffCompProperties_DamageRetaliation : HediffCompProperties_PreDamageModification
    {
        public HediffCompProperties_DamageRetaliation()
        {
            this.compClass = typeof(HediffComp_DamageRetaliation);
        }
        public float range = 0f;
        public bool onlyRetaliateVsInstigator = false;
        public bool hitInstigatorRegardlessOfRange;
        public bool canAffectAnimals = true;
        public bool canAffectMechs = true;
        public bool canAffectDrones = true;
        public bool canAffectHumanlikes = true;
        public bool canAffectEntities = true;
        public bool canAffectMutants = true;
        public bool friendlyFire;
        public float chanceToInflictHediff = 1f;
        public float baseHediffSeverity;
        public HediffDef hediff;
        public StatDef hediffResistStat;
        public bool hediffScaleWithDamage;
        public bool hediffScaleWithBodySize;
        public float chanceToDoDamage = 1f;
        public float baseRetaliationDamage;
        public DamageDef retaliationDamageDef;
        public StatDef baseRetaliationDamageFactor;
        public bool damageScaleWithDamage;
        public bool damageScaleWithBodySize;
        public EffecterDef visualEffect;
        public int vfxCooldownTicks;
    }
    public class HediffComp_DamageRetaliation : HediffComp_PreDamageModification
    {
        public new HediffCompProperties_DamageRetaliation Props
        {
            get
            {
                return (HediffCompProperties_DamageRetaliation)this.props;
            }
        }
        public virtual void RetaliateAgainst(Pawn p, float amount)
        {
            if (this.Props.shouldUseIncomingDamageFactor)
            {
                amount *= this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor);
            }
            if (this.Props.hediff != null && Rand.Chance(this.Props.chanceToInflictHediff))
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediff, p);
                hediff.Severity = this.Props.baseHediffSeverity * (this.Props.hediffScaleWithDamage ? amount : 1f) * (this.Props.hediffResistStat != null ? Mathf.Max(1f - p.GetStatValue(this.Props.hediffResistStat), 0f) : 1f) / (this.Props.hediffScaleWithBodySize ? p.BodySize : 1f);
                p.health.AddHediff(hediff);
            }
            if (this.Props.retaliationDamageDef != null && Rand.Chance(this.Props.chanceToDoDamage))
            {
                DamageInfo dinfo2 = new DamageInfo(this.Props.retaliationDamageDef, this.Props.baseRetaliationDamage * (this.Props.damageScaleWithDamage ? amount : 1f) * (this.Props.baseRetaliationDamageFactor != null ? this.Pawn.GetStatValue(this.Props.baseRetaliationDamageFactor) : 1f) / (this.Props.hediffScaleWithBodySize ? p.BodySize : 1f), 2f, -1f, null, p.health.hediffSet.GetRandomNotMissingPart(this.Props.retaliationDamageDef), null, DamageInfo.SourceCategory.ThingOrUnknown);
                p.TakeDamage(dinfo2);
            }
        }
        public virtual float RetaliationRange
        {
            get
            {
                return this.Props.range;
            }
        }
        public virtual bool CanHit(Pawn pawn, float amount)
        {
            return (this.Props.friendlyFire || pawn.HostileTo(this.Pawn)) && (pawn.IsMutant ? this.Props.canAffectMutants : ((this.Props.canAffectAnimals || !pawn.RaceProps.Animal) && (this.Props.canAffectMechs || !pawn.RaceProps.IsMechanoid) && (this.Props.canAffectDrones || !pawn.RaceProps.IsDrone) && (this.Props.canAffectHumanlikes || !pawn.RaceProps.Humanlike) && (this.Props.canAffectEntities || !pawn.RaceProps.IsAnomalyEntity)));
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            base.DoModificationInner(ref dinfo, ref absorbed, amount);
            Pawn instigator = dinfo.Instigator as Pawn;
            if (instigator != null && this.CanHit(instigator, amount) && (this.Props.hitInstigatorRegardlessOfRange || this.Pawn.Position.DistanceTo(instigator.Position) <= this.RetaliationRange))
            {
                this.RetaliateAgainst(instigator, amount);
            }
            if (!this.Props.onlyRetaliateVsInstigator && this.Pawn.SpawnedOrAnyParentSpawned && this.RetaliationRange > 0f)
            {
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.RetaliationRange, true).OfType<Pawn>().Distinct<Pawn>())
                {
                    if (this.CanHit(p, amount) && (!this.Props.hitInstigatorRegardlessOfRange || p != instigator))
                    {
                        this.RetaliateAgainst(p, amount);
                    }
                }
                if (this.graphicCooldown <= 0 && this.Props.visualEffect != null)
                {
                    this.Props.visualEffect.SpawnMaintained(this.Pawn.PositionHeld, this.Pawn.MapHeld, 1f);
                    this.graphicCooldown = this.Props.vfxCooldownTicks;
                }
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (graphicCooldown > 0)
            {
                graphicCooldown--;
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.graphicCooldown, "graphicCooldown", 0, false);
        }
        private int graphicCooldown = 0;
    }
}
