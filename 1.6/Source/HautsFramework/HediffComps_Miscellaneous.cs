using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsFramework
{
    //Periodically adds or removes boredom (corresponding value) from one or more specified joy kinds (key). Triggers every "ticks" ticks
    public class HediffCompProperties_BoredomAdjustment : HediffCompProperties
    {
        public HediffCompProperties_BoredomAdjustment()
        {
            this.compClass = typeof(HediffComp_BoredomAdjustment);
        }
        public int ticks = 2500;
        public Dictionary<JoyKindDef, float> boredoms;
    }
    public class HediffComp_BoredomAdjustment : HediffComp
    {
        public HediffCompProperties_BoredomAdjustment Props
        {
            get
            {
                return (HediffCompProperties_BoredomAdjustment)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.ticks, delta) && this.Pawn.needs.joy != null && this.Props.boredoms != null)
            {
                DefMap<JoyKindDef, float> tolerances = HautsFramework.GetInstanceField(typeof(JoyToleranceSet), this.Pawn.needs.joy.tolerances, "tolerances") as DefMap<JoyKindDef, float>;
                DefMap<JoyKindDef, bool> bored = HautsFramework.GetInstanceField(typeof(JoyToleranceSet), this.Pawn.needs.joy.tolerances, "bored") as DefMap<JoyKindDef, bool>;
                foreach (JoyKindDef jkd in this.Props.boredoms.Keys)
                {
                    float num2 = tolerances[jkd];
                    num2 = Math.Min(1f, Math.Max(0f, num2 - this.Props.boredoms.TryGetValue(jkd)));
                    tolerances[jkd] = num2;
                    if (bored[jkd] && num2 < 0.3f)
                    {
                        bored[jkd] = false;
                    }
                }
            }
        }
    }
    /* turnInto: transforms into this hediff…
     * whenStat: …when the amount of this stat the pawn has either…
     * goesBelow: …becomes <= this amount, or…
     * goesAbove: …becomes > this amount*/
    public class HediffCompProperties_ChangesBasedOnStat : HediffCompProperties
    {
        public HediffCompProperties_ChangesBasedOnStat()
        {
            this.compClass = typeof(HediffComp_ChangesBasedOnStat);
        }
        public HediffDef turnInto;
        public StatDef whenStat;
        public float goesBelow;
        public float goesAbove = 999f;
    }
    public class HediffComp_ChangesBasedOnStat : HediffComp
    {
        public HediffCompProperties_ChangesBasedOnStat Props
        {
            get
            {
                return (HediffCompProperties_ChangesBasedOnStat)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(2500, delta))
            {
                float statVal = this.Pawn.GetStatValue(this.Props.whenStat);
                if (statVal <= this.Props.goesBelow || statVal > this.Props.goesAbove)
                {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.turnInto, this.Pawn, this.parent.Part ?? null);
                    this.Pawn.health.AddHediff(hediff, this.parent.Part ?? null);
                    this.Pawn.health.RemoveHediff(this.parent);
                }
            }
        }
    }
    /*As CreateHediffsBySpendingSeverity, but it uses time instead of severity. Does not have the severityToTrigger field.
     *  ticksToNextSpawn: a randomly chosen integer from this range is chosen as the timer to next instance of hediff creation
     *  maxStoredCharges: whenever hediff creation would occur but the hediff cannot be created, a “charge” is created instead, which is stored in this hediff and visible in the tooltip. As soon as it becomes possible to create the hediff, one of the charges is expended to create it. This hediff can store up to this amount of charges.
     *  startingCharges: the number of charges this hediff starts with when created.
     *  maxChargesScaleWithSeverity: if severity is higher than 1, multiplies max charges by severity*/
    public class HediffCompProperties_CreateHediffPeriodically : HediffCompProperties
    {
        public HediffCompProperties_CreateHediffPeriodically()
        {
            this.compClass = typeof(HediffComp_CreateHediffPeriodically);
        }
        public IntRange ticksToNextSpawn;
        public HediffDef hediffGiven;
        public float maxSeverityOfCreatedHediff = -1f;
        public int maxStoredCharges = 1;
        public bool maxChargesScaleWithSeverity = false;
        public int startingCharges = 1;
        public float severityToGive;
        public bool showProgressInTooltip;
    }
    public class HediffComp_CreateHediffPeriodically : HediffComp
    {
        public HediffCompProperties_CreateHediffPeriodically Props
        {
            get
            {
                return (HediffCompProperties_CreateHediffPeriodically)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawnCountdown".Translate((this.ticksRemaining / 2500f).ToStringByStyle(ToStringStyle.FloatMaxOne)) + "Hauts_ChargesStored".Translate(this.charges, this.maxCharges);
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.charges = this.Props.startingCharges;
            this.maxCharges = this.Props.maxStoredCharges * (int)(this.Props.maxChargesScaleWithSeverity ? Math.Max(this.parent.Severity, 1f) : 1f);
            if (this.charges < this.maxCharges)
            {
                this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
            }
            else
            {
                this.ticksRemaining = 0;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                this.maxCharges = this.Props.maxStoredCharges * (int)(this.Props.maxChargesScaleWithSeverity ? Math.Max(this.parent.Severity, 1f) : 1f);
            }
            if (this.ticksRemaining <= 0)
            {
                if (this.charges < this.maxCharges)
                {
                    this.charges++;
                    if (this.charges < this.maxCharges)
                    {
                        this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
                    }
                }
            }
            else
            {
                this.ticksRemaining -= delta;
            }
            if (this.charges > 0)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffGiven);
                if (hediff != null)
                {
                    if (hediff.Severity < this.Props.maxSeverityOfCreatedHediff || this.Props.maxSeverityOfCreatedHediff < 0f)
                    {
                        hediff.Severity = hediff.Severity + this.Props.severityToGive;
                        this.charges--;
                        this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
                    }
                }
                else
                {
                    Hediff hediff2 = HediffMaker.MakeHediff(this.Props.hediffGiven, this.Pawn);
                    this.Pawn.health.AddHediff(hediff2);
                    this.charges--;
                    this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining", 0, false);
            Scribe_Values.Look<int>(ref this.charges, "charges", 0, false);
            Scribe_Values.Look<int>(ref this.maxCharges, "maxCharges", 1, false);
        }
        public int ticksRemaining;
        public int charges;
        public int maxCharges;
    }
    /*As CreateThingsBySpendingSeverity, but it uses time instead of severity. Does not have the severityToTrigger field. Just like its spending severity sibling, don't use this to make unminifiable buildings. It's possible, but it would be very fucky-wucky.
     *  ticksToNextSpawn: a randomly chosen integer from this range is chosen as the timer to next instance of thing creation.
     *  SpawnThings(): handles the actual generation and placement of items*/
    public class HediffCompProperties_CreateThingsPeriodically : HediffCompProperties
    {
        public HediffCompProperties_CreateThingsPeriodically()
        {
            this.compClass = typeof(HediffComp_CreateThingsPeriodically);
        }
        public IntRange ticksToNextSpawn;
        public Dictionary<ThingDef, FloatRange> spawnedThingAndCountPerTrigger;
        public bool setToOwnFaction = true;
        public bool spawnInOwnInventory = true;
        public float spawnRadius;
        public bool showProgressInTooltip = true;
        public bool fullyRandomizeSpawns = false;
        public FleckDef spawnFleck1;
        public FleckDef spawnFleck2;
        public SoundDef spawnSound;
    }
    public class HediffComp_CreateThingsPeriodically : HediffComp
    {
        public HediffCompProperties_CreateThingsPeriodically Props
        {
            get
            {
                return (HediffCompProperties_CreateThingsPeriodically)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawnCountdown".Translate((this.ticksRemaining / 2500f).ToStringByStyle(ToStringStyle.FloatMaxOne));
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.ticksRemaining <= 0 && (this.Pawn.Spawned || this.Props.spawnInOwnInventory))
            {
                this.SpawnThings();
            }
            else
            {
                this.ticksRemaining -= delta;
            }
        }
        public virtual KeyValuePair<ThingDef, FloatRange> GetThingToSpawn()
        {
            return this.Props.spawnedThingAndCountPerTrigger.RandomElement();
        }
        public virtual void SpawnThings()
        {
            KeyValuePair<ThingDef, FloatRange> toSpawn = this.GetThingToSpawn();
            ThingDef def = toSpawn.Key;
            float countPerTrigger = toSpawn.Value.RandomInRange;
            if (this.Props.spawnInOwnInventory && !def.EverHaulable)
            {
                Log.Error("Hauts_CantSpawnUnminified".Translate(def.LabelCap, this.Pawn.Name.ToStringFull));
                return;
            }
            this.SpawningBlock(def, countPerTrigger);
        }
        public void SpawningBlock(ThingDef def, float countPerTrigger)
        {
            this.FinalizeThingBeforePlacing(def, countPerTrigger, out Thing thing);
            if (this.Props.spawnInOwnInventory)
            {
                this.AddToInventory(thing);
            }
            else
            {
                if (this.Pawn.Spawned)
                {
                    this.SpawnInRadius(thing);
                }
                else if (this.Pawn.IsCaravanMember())
                {
                    this.AddToInventory(thing);
                }
            }
            this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
            if (this.Props.spawnSound != null && this.Pawn.Spawned)
            {
                this.Props.spawnSound.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
            }
        }
        public void FinalizeThingBeforePlacing(ThingDef def, float countPerTrigger, out Thing thing)
        {
            ThingDef stuff = GenStuff.RandomStuffFor(def);
            thing = ThingMaker.MakeThing(def, stuff);
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Colony);
            }
            if (thing.def.Minifiable)
            {
                thing = thing.MakeMinified();
            }
            if (this.Props.setToOwnFaction && thing.def.CanHaveFaction)
            {
                thing.SetFaction(this.Pawn.Faction, null);
            }
            thing.stackCount = Math.Min((int)Math.Floor(countPerTrigger), def.stackLimit);
            thing.Notify_DebugSpawned();
        }
        public void AddToInventory(Thing thing)
        {
            this.Pawn.inventory.innerContainer.TryAdd(thing, true);
        }
        public virtual void SpawnInRadius(Thing thing)
        {
            IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 6, null);
            if (thing.def.category == ThingCategory.Plant && this.Pawn.Map.wildPlantSpawner.CurrentWholeMapNumNonZeroFertilityCells > 0)
            {
                List<IntVec3> fertileCells = new List<IntVec3>();
                CellRect cellRect = CellRect.WholeMap(this.Pawn.Map);
                using (CellRect.Enumerator enumerator = cellRect.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.GetFertility(this.Pawn.Map) >= thing.def.plant.fertilityMin)
                        {
                            fertileCells.Add(enumerator.Current);
                        }
                    }
                }
                if (fertileCells.Count > 0)
                {
                    bool foundFertileLoc = false;
                    int tries = 1000;
                    while (!foundFertileLoc && tries > 0)
                    {
                        loc = fertileCells.RandomElement();
                        if (loc.Standable(this.Pawn.Map) && !loc.Fogged(this.Pawn.Map))
                        {
                            Plant plant = loc.GetPlant(this.Pawn.Map);
                            if ((plant == null || plant.def.plant.growDays <= 10f) && !loc.GetTerrain(this.Pawn.Map).avoidWander && (!loc.Roofed(this.Pawn.Map) || !thing.def.plant.interferesWithRoof))
                            {
                                foundFertileLoc = true;
                            }
                        }
                        tries--;
                    }
                }
                Plant plant2 = loc.GetPlant(this.Pawn.Map);
                if (plant2 != null)
                {
                    plant2.Destroy(DestroyMode.Vanish);
                }
                GenSpawn.Spawn(thing, loc, this.Pawn.Map, Rot4.North, WipeMode.Vanish, false);
                ((Plant)thing).Growth = Rand.Value * 0.1f;
            }
            else
            {
                loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, (int)Math.Ceiling(this.Props.spawnRadius), null);
                GenPlace.TryPlaceThing(thing, loc, this.Pawn.Map, ThingPlaceMode.Near, null, null, default);
            }
            thing.Notify_DebugSpawned();
            if (thing.SpawnedOrAnyParentSpawned && thing.Map != null && this.Props.spawnFleck1 != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, this.Props.spawnFleck1, 1f);
                dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                thing.Map.flecks.CreateFleck(dataStatic);
                if (this.Props.spawnFleck2 != null)
                {
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, this.Props.spawnFleck2, 1f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    thing.Map.flecks.CreateFleck(dataStatic2);
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining", 0, false);
        }
        public int ticksRemaining;
    }
    /*after spending this many continuous ticks downed, the hediff is removed*/
    public class HediffCompProperties_DisappearsWhileDowned : HediffCompProperties
    {
        public HediffCompProperties_DisappearsWhileDowned()
        {
            this.compClass = typeof(HediffComp_DisappearsWhileDowned);
        }
        public int ticksSpentDownedToStop;
    }
    public class HediffComp_DisappearsWhileDowned : HediffComp
    {
        public HediffCompProperties_DisappearsWhileDowned Props
        {
            get
            {
                return (HediffCompProperties_DisappearsWhileDowned)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.Downed)
            {
                this.ticksSpentDowned++;
                if (this.ticksSpentDowned >= this.Props.ticksSpentDownedToStop)
                {
                    this.Pawn.health.RemoveHediff(this.parent);
                    return;
                }
            }
            else
            {
                this.ticksSpentDowned = 0;
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksSpentDowned, "ticksSpentDowned", this.Props.ticksSpentDownedToStop, false);
        }
        int ticksSpentDowned;
    }
    /*adding this hediff expels all hediffs with the specified hediffTag.
     * onlyExpelItemizableHediffs: only affects hediffs that have a spawnThingOnRemoved
     * onlySameBodyPart: ewisott*/
    public class HediffCompProperties_ExpelsHediffsWithTag : HediffCompProperties
    {
        public HediffCompProperties_ExpelsHediffsWithTag()
        {
            this.compClass = typeof(HediffComp_ExpelsHediffsWithTag);
        }
        public string hediffTag;
        public bool onlyExpelItemizableHediffs;
        public bool onlySameBodyPart = true;
    }
    public class HediffComp_ExpelsHediffsWithTag : HediffComp
    {
        public HediffCompProperties_ExpelsHediffsWithTag Props
        {
            get
            {
                return (HediffCompProperties_ExpelsHediffsWithTag)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            List<Hediff> hediffs = this.Pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (this.parent != hediffs[i] && hediffs[i].def.tags != null && hediffs[i].def.tags.Contains(this.Props.hediffTag) && (!this.Props.onlySameBodyPart || this.parent.Part == null || (hediffs[i].Part != null && hediffs[i].Part == this.parent.Part)))
                {
                    if (hediffs[i].def.spawnThingOnRemoved != null)
                    {
                        if (this.Pawn.SpawnedOrAnyParentSpawned)
                        {
                            GenSpawn.Spawn(hediffs[i].def.spawnThingOnRemoved, this.Pawn.PositionHeld, this.Pawn.MapHeld, WipeMode.Vanish);
                        }
                        else
                        {
                            this.Pawn.inventory.innerContainer.TryAdd(ThingMaker.MakeThing(hediffs[i].def.spawnThingOnRemoved, null));
                        }
                        this.Pawn.health.RemoveHediff(hediffs[i]);
                    }
                    else if (!this.Props.onlyExpelItemizableHediffs)
                    {
                        this.Pawn.health.RemoveHediff(hediffs[i]);
                    }
                }
            }
        }
    }
    /*anyMentalState: when this hediff is removed, causes the pawn’s current mental state to end - unless that mental state is “fleeing in panic”, which requires either…
     * canRemoveFleeing: …this to be true, or for PanicFlee to be in the mentalStates field
     * mentalStates: when this hediff is removed, causes the pawn’s current mental state to end if it is in this list. Therefore you only need ot specify it if anyMentalState is not true
     * canRemoveAggro|Malicious: if false, can’t remove an 'aggro'|'malicious' mental break. In case it isn't clear, those are normal, non-modded properties of mental breaks
     * sendNotification: causes recoveryText to be displayed in a positive event message in the top left of the screen when this hediff is removed and a mental state is resultantly ended. String won’t be displayed if the pawn is not worth notifying you about (as determined by PawnUtility.ShouldSendNotificationAbout). Format this string like you would a trait description: {PAWN_nameDef}, {PAWN_pronoun}, etc.
     * removeEarlyIfNotInMentalState: causes this hediff to be removed if the pawn is ever not in a mental state*/
    public class HediffCompProperties_ExitMentalStateOnRemoval : HediffCompProperties
    {
        public HediffCompProperties_ExitMentalStateOnRemoval()
        {
            this.compClass = typeof(HediffComp_ExitMentalStateOnRemoval);
        }
        public bool anyMentalState;
        public List<MentalStateDef> mentalStates;
        public bool sendNotification;
        public bool canRemoveFleeing;
        public bool canRemoveAggro = true;
        public bool canRemoveMalicious = true;
        public bool removeEarlyIfNotInMentalState;
        public string recoveryText;
    }
    public class HediffComp_ExitMentalStateOnRemoval : HediffComp
    {
        public HediffCompProperties_ExitMentalStateOnRemoval Props
        {
            get
            {
                return (HediffCompProperties_ExitMentalStateOnRemoval)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!this.Pawn.InMentalState && this.Props.removeEarlyIfNotInMentalState)
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (PawnUtility.ShouldSendNotificationAbout(this.Pawn) && this.Pawn.InMentalState && (this.Pawn.MentalStateDef != MentalStateDefOf.PanicFlee || this.Props.canRemoveFleeing) && (!this.Pawn.MentalStateDef.IsAggro || this.Props.canRemoveAggro) && (this.Pawn.MentalStateDef.category != MentalStateCategory.Malicious || this.Props.canRemoveMalicious) && (this.Props.anyMentalState || this.Props.mentalStates.Contains(this.Pawn.MentalStateDef)))
            {
                this.parent.pawn.MentalState.RecoverFromState();
                if (this.Props.sendNotification)
                {
                    TaggedString message = this.Props.recoveryText.Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve();
                    Messages.Message(message, this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    /*This hediff cannot be removed (well, technically it gets removed, but then creates a fresh version of itself which gets added to the pawn) so long as the pawn has at least one of the specified trait(s) and/or gene(s).
     * requiresAForcingProperty: causes certain events to remove the hediff if the pawn has none of these forcing traits or genes
     * -After the player finishes the Game Creation process and clicks start
     * -On selecting a pawn and clicking the “DEV: FixTraitGrantedStuff” button (only visible if you have God Mode in dev mode enabled)
     * alternativeHediffs: even if the pawn has a forcing trait or gene, this hediff CAN be removed if the pawn has at least one of these ‘alternative hediffs’
     * returnAs: whenever this hediff would be recreated by ForcedByOtherProperty it instead turns into a hediff of this Def. Great if you have two hediffs that are supposed to be the same condition - this lets you make one of them the 'factory default'.*/
    public class HediffCompProperties_ForcedByOtherProperty : HediffCompProperties
    {
        public HediffCompProperties_ForcedByOtherProperty()
        {
            this.compClass = typeof(HediffComp_ForcedByOtherProperty);
        }
        public List<TraitDef> forcingTraits;
        public List<GeneDef> forcingGenes;
        public List<HediffDef> alternativeHediffs;
        public bool requiresAForcingProperty = true;
        public HediffDef returnAs;
    }
    public class HediffComp_ForcedByOtherProperty : HediffComp
    {
        public HediffCompProperties_ForcedByOtherProperty Props
        {
            get
            {
                return (HediffCompProperties_ForcedByOtherProperty)this.props;
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!this.Pawn.Dead && !this.Pawn.Destroyed)
            {
                if (this.Props.alternativeHediffs != null)
                {
                    foreach (HediffDef h in this.Props.alternativeHediffs)
                    {
                        if (this.Pawn.health.hediffSet.HasHediff(h))
                        {
                            return;
                        }
                    }
                }
                if (this.Props.forcingTraits != null && this.Pawn.story != null)
                {
                    foreach (TraitDef t in this.Props.forcingTraits)
                    {
                        if (this.Pawn.story.traits.HasTrait(t))
                        {
                            this.RecreateHediff();
                            return;
                        }
                    }
                }
                if (ModsConfig.BiotechActive && this.Props.forcingGenes != null && this.Pawn.genes != null)
                {
                    foreach (GeneDef g in this.Props.forcingGenes)
                    {
                        if (HautsMiscUtility.AnalogHasActiveGene(this.Pawn.genes, g))
                        {
                            this.RecreateHediff();
                            return;
                        }
                    }
                }
            }
        }
        private void RecreateHediff()
        {
            Hediff hediff = HediffMaker.MakeHediff(this.Props.returnAs != null ? this.Props.returnAs : this.parent.def, this.Pawn, null);
            this.Pawn.health.AddHediff(hediff, null, null, null);
        }
    }
    /*Alters the maximum amount and/or the decay rate of one or more ‘genetic resources’ (e.g. hemogen).
     * maxResourceOffsets: if for any of the keys, the pawn has a genetic resource (e.g. hemogen) with that label, increase that genetic resource’s maximum by the corresponding float value.
     * drainRateFactors: if for any of the keys, the pawn has a genetic resource (e.g. hemogen) with that label, the divisor of that genetic resource’s drain rate = 1 (the corresponding float value + the corresponding float values of all other GeneticResourceModifier comps that affect that genetic resource).
     * Relevant Trivia: each “unit” of hemogen is actually 0.01 hemogen, not 1.
     * maxResourceOffsets will not affect hemogen correctly if EBSG Framework is running. You should instead use its max hemogen stat, and probably also xpath out the GRM if it isn't doing anything else.*/
    public class HediffCompProperties_GeneticResourceModifiers : HediffCompProperties
    {
        public HediffCompProperties_GeneticResourceModifiers()
        {
            this.compClass = typeof(HediffComp_GeneticResourceModifiers);
        }
        public Dictionary<string, float> maxResourceOffsets = new Dictionary<string, float>();
        public Dictionary<string, float> drainRateFactors = new Dictionary<string, float>();
    }
    public class HediffComp_GeneticResourceModifiers : HediffComp
    {
        public HediffCompProperties_GeneticResourceModifiers Props
        {
            get
            {
                return (HediffCompProperties_GeneticResourceModifiers)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                string result = "";
                foreach (string s in this.Props.maxResourceOffsets.Keys)
                {
                    result += "Hauts_GRMtooltip".Translate(100f * this.Props.maxResourceOffsets.TryGetValue(s), s) + "\n";
                }
                foreach (string s in this.Props.drainRateFactors.Keys)
                {
                    result += "Hauts_GRMtooltip2".Translate(s.CapitalizeFirst(), this.Props.drainRateFactors.TryGetValue(s).ToStringPercent()) + "\n";
                }
                return result;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (ModsConfig.BiotechActive && this.Pawn.genes != null)
            {
                foreach (Gene g in this.Pawn.genes.GenesListForReading)
                {
                    if (g is Gene_Resource gr && this.Props.maxResourceOffsets.ContainsKey(gr.ResourceLabel))
                    {
                        gr.SetMax(gr.Max + this.Props.maxResourceOffsets.TryGetValue(gr.ResourceLabel));
                    }
                }
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (ModsConfig.BiotechActive && this.Pawn.genes != null)
            {
                foreach (Gene g in this.Pawn.genes.GenesListForReading)
                {
                    if (g is Gene_Resource gr && this.Props.maxResourceOffsets.ContainsKey(gr.ResourceLabel))
                    {
                        gr.SetMax(gr.Max - this.Props.maxResourceOffsets.TryGetValue(gr.ResourceLabel));
                    }
                }
            }
        }
    }
    /*When this hediff is gained, it also creates hediffDef (hereafter 2nd hediff)
     * skipIfAlreadyExists: does not create 2nd hediff if pawn already a hediff of its def
     * durationScalar: if both this hediff and 2nd hediff have HediffComp_Disappears, 2nd hediff’s duration is equal to this hediff’s duration times this amount*/
    public class HediffCompProperties_GiveScalingDurationHediff : HediffCompProperties
    {
        public HediffCompProperties_GiveScalingDurationHediff()
        {
            this.compClass = typeof(HediffComp_GiveScalingDurationHediff);
        }
        public HediffDef hediffDef;
        public float durationScalar;
        public bool skipIfAlreadyExists;
    }
    public class HediffComp_GiveScalingDurationHediff : HediffComp
    {
        public HediffCompProperties_GiveScalingDurationHediff Props
        {
            get
            {
                return (HediffCompProperties_GiveScalingDurationHediff)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            if (this.Props.skipIfAlreadyExists && this.parent.pawn.health.hediffSet.HasHediff(this.Props.hediffDef, false))
            {
                return;
            }
            Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffDef, this.parent.pawn, null);
            if (hediff is HediffWithComps hediffWithComps)
            {
                foreach (HediffComp hediffComp in hediffWithComps.comps)
                {
                    HediffComp_Disappears thisDisappears = this.parent.TryGetComp<HediffComp_Disappears>();
                    if (hediffComp is HediffComp_Disappears hediffComp_Disappears && thisDisappears != null)
                    {
                        hediffComp_Disappears.ticksToDisappear = (int)(this.Props.durationScalar * thisDisappears.ticksToDisappear);
                    }
                }
            }
            this.parent.pawn.health.AddHediff(hediff, null, null, null);
        }
    }
    /*thoughtDefs: gives a randomly selected thought from this list to the pawn…
     * mtbDays: … with this value as the MTB …
     * mtbLossPerExtraSeverity: … lowered by this amount per 1 severity …
     * mtbLossSeverityCap: …but, for the purposes of this calculation, severity higher than this amount is treated as this amount instead.
     * showInTooltip: displays “Random thought MTB: # days” in the hediff tooltip.*/
    public class HediffCompProperties_GiveThoughtsRandomly : HediffCompProperties
    {
        public HediffCompProperties_GiveThoughtsRandomly()
        {
            this.compClass = typeof(HediffComp_GiveThoughtsRandomly);
        }
        public float mtbDays;
        public float mtbLossPerExtraSeverity;
        public float mtbLossSeverityCap;
        public List<ThoughtDef> thoughtDefs;
        public bool showInTooltip = false;
    }
    public class HediffComp_GiveThoughtsRandomly : HediffComp
    {
        public HediffCompProperties_GiveThoughtsRandomly Props
        {
            get
            {
                return (HediffCompProperties_GiveThoughtsRandomly)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (!this.Props.showInTooltip)
                {
                    return base.CompLabelInBracketsExtra;
                }
                return "Hauts_GTRtooltip".Translate(this.Props.mtbDays);
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta) && this.Pawn.needs.mood != null && Rand.MTBEventOccurs(Math.Max(this.Props.mtbDays - Math.Min(this.Props.mtbLossPerExtraSeverity * this.parent.Severity, this.Props.mtbLossSeverityCap), 0.001f), 60000f, 150f))
            {
                Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(this.Props.thoughtDefs.RandomElement<ThoughtDef>());
                this.Pawn.needs.mood.thoughts.memories.TryGainMemory(thought, null);
            }
        }
    }
    /*traitDef: when added to a pawn, gives the pawn this trait at traitDegree.
     * When the hediff is removed, the trait is removed as well, unless it already existed on the pawn or is added by a gene.
     * If the parent pawn has a suppressed version of the trait, adding this hediff will un-suppress it. When added, suppresses any traits conflicting with the specified trait; when removed, recalculates suppression.*/
    public class HediffCompProperties_GiveTrait : HediffCompProperties
    {
        public HediffCompProperties_GiveTrait()
        {
            this.compClass = typeof(HediffComp_GiveTrait);
        }
        public TraitDef traitDef;
        public int traitDegree = 0;
    }
    public class HediffComp_GiveTrait : HediffComp
    {
        public HediffCompProperties_GiveTrait Props
        {
            get
            {
                return (HediffCompProperties_GiveTrait)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (!this.removeTraitOnRemoval)
                {
                    return base.CompLabelInBracketsExtra;
                }
                return "Hauts_GivesTraitTooltip".Translate(this.Props.traitDef.DataAtDegree(this.Props.traitDegree).GetLabelFor(this.parent.pawn));
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            if (this.parent.pawn.story != null && this.parent.pawn.story.traits != null)
            {
                foreach (Trait t in this.parent.pawn.story.traits.allTraits)
                {
                    if (t.def == this.Props.traitDef && t.Degree == this.Props.traitDegree)
                    {
                        this.removeTraitOnRemoval = false;
                        if (t.suppressedByTrait)
                        {
                            t.suppressedByTrait = false;
                            this.AdjustSuppression();
                        }
                        return;
                    }
                }
                this.AdjustSuppression();
                Trait toGain = new Trait(this.Props.traitDef, this.Props.traitDegree);
                this.parent.pawn.story.traits.GainTrait(toGain);
                this.removeTraitOnRemoval = true;
                this.parent.pawn.story.traits.RecalculateSuppression();
            }
        }
        public void AdjustSuppression()
        {
            foreach (Trait tt in this.parent.pawn.story.traits.allTraits)
            {
                if ((tt.def != this.Props.traitDef && tt.def.ConflictsWith(this.Props.traitDef) && tt.def.canBeSuppressed) || (tt.def == this.Props.traitDef && tt.Degree != this.Props.traitDegree))
                {
                    tt.suppressedByTrait = true;
                }
            }
        }
        public override void CompPostPostRemoved()
        {
            if (this.parent.pawn.story != null && this.parent.pawn.story.traits != null)
            {
                if (this.removeTraitOnRemoval)
                {
                    List<Trait> toRemove = new List<Trait>();
                    foreach (Trait t in this.parent.pawn.story.traits.allTraits)
                    {
                        if (t.def == this.Props.traitDef && t.Degree == this.Props.traitDegree)
                        {
                            toRemove.Add(t);
                        }
                    }
                    foreach (Trait t in toRemove)
                    {
                        this.parent.pawn.story.traits.RemoveTrait(t);
                        foreach (Trait tt in this.parent.pawn.story.traits.allTraits)
                        {
                            if (tt.suppressedByTrait && ((tt.def.ConflictsWith(this.Props.traitDef)) || (tt.def == this.Props.traitDef && tt.Degree != this.Props.traitDegree)))
                            {
                                bool flag = true;
                                foreach (Trait ttt in this.parent.pawn.story.traits.allTraits)
                                {
                                    if (ttt != tt && ttt.def.ConflictsWith(tt.def))
                                    {
                                        flag = false;
                                    }
                                }
                                if (flag)
                                {
                                    tt.suppressedByTrait = false;
                                }
                            }
                        }
                    }
                }
            }
            this.parent.pawn.story.traits.RecalculateSuppression();
            base.CompPostPostRemoved();
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.removeTraitOnRemoval, "removeTraitOnRemoval", true, false);
        }
        public bool removeTraitOnRemoval;
    }
    /*Works just like HediffComp_Link, but it can create multiple “linking” motes (all of the same type) to multiple Things. Its HediffCompProperties’ fields are the same.
     * However, since it’s NOT Link and the instantiated comp itself has the List<Thing> others and List<MoteDualAttached> motes fields in lieu of just a Thing other, a regular CompAbilityEffect_GiveHediff won’t add it properly.
     * It only interacts with the following other phenomena:
     * -When removed, HediffComp_PairedHediff removes itself from any paired MultiLink’s list of links
     * -CompAbilityEffect_GiveHediffCasterStatScalingSeverity adds a link from the created hediff to the creator
     * -CompAbilityEffect_GiveHediffPaired adds a link from either created hediff to the other party
     * -Dialog_GiveHediffFromMenu adds a link from the created hediff to the creator
     * DoToDistanceBrokenLink: allows you to do things to a linked Thing whose link breaks due to distance*/
    public class HediffCompProperties_MultiLink : HediffCompProperties
    {
        public HediffCompProperties_MultiLink()
        {
            this.compClass = typeof(HediffComp_MultiLink);
        }
        public bool showName = true;
        public float maxDistance = -1f;
        public ThingDef customMote;
    }
    public class HediffComp_MultiLink : HediffComp
    {
        public HediffCompProperties_MultiLink Props
        {
            get
            {
                return (HediffCompProperties_MultiLink)this.props;
            }
        }
        public override bool CompShouldRemove
        {
            get
            {
                if (base.CompShouldRemove)
                {
                    return true;
                }
                if (this.others == null || this.others.Count == 0)
                {
                    return true;
                }
                if (!this.parent.pawn.SpawnedOrAnyParentSpawned)
                {
                    return true;
                }
                bool anyOtherInRange = false;
                foreach (Thing t in this.others)
                {
                    if (t.SpawnedOrAnyParentSpawned && (this.Props.maxDistance <= 0f || this.parent.pawn.PositionHeld.InHorDistOf(t.PositionHeld, this.Props.maxDistance)))
                    {
                        anyOtherInRange = true;
                        break;
                    }
                }
                if (!anyOtherInRange)
                {
                    return true;
                }
                return false;
            }
        }
        public virtual void DoToDistanceBrokenLink(Thing other)
        {

        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.drawConnection && this.others != null)
            {
                if (this.motes == null)
                {
                    this.motes = new List<MoteDualAttached>();
                }
                int countDiff = this.others.Count - this.motes.Count;
                while (countDiff > 0)
                {
                    this.motes.Add(null);
                    countDiff--;
                }
                for (int i = this.others.Count - 1; i >= 0; i--)
                {
                    if (this.others[i].MapHeld == this.Pawn.MapHeld)
                    {
                        if (this.Props.maxDistance > 0f && !this.Pawn.PositionHeld.InHorDistOf(this.others[i].PositionHeld, this.Props.maxDistance))
                        {
                            this.DoToDistanceBrokenLink(this.others[i]);
                            bool letPairedHediffRemoveItself = false;
                            if (this.others[i] is Pawn p2)
                            {
                                HediffComp_PairedHediff hcph = this.parent.TryGetComp<HediffComp_PairedHediff>();
                                if (hcph != null)
                                {
                                    for (int j = hcph.hediffs.Count - 1; j >= 0; j--)
                                    {
                                        if (hcph.hediffs[j].pawn == p2)
                                        {
                                            letPairedHediffRemoveItself = true;
                                            p2.health.RemoveHediff(hcph.hediffs[j]);
                                        }
                                    }
                                }
                            }
                            if (!letPairedHediffRemoveItself)
                            {
                                this.others.Remove(this.others[i]);
                                if (this.motes.Count >= i + 1)
                                {
                                    this.motes.Remove(this.motes[i]);
                                }
                            }
                            continue;
                        }
                        if (this.others[i] is Pawn p)
                        {
                            if (p.Dead || p.Destroyed || !p.Spawned)
                            {
                                this.others.Remove(this.others[i]);
                                if (this.motes.Count >= i + 1)
                                {
                                    this.motes.Remove(this.motes[i]);
                                }
                                continue;
                            }
                            if (p == this.Pawn)
                            {
                                continue;
                            }
                        }
                        ThingDef thingDef = this.Props.customMote ?? ThingDefOf.Mote_PsychicLinkLine;
                        if (this.motes[i] == null || this.motes[i].Destroyed)
                        {
                            this.motes[i] = MoteMaker.MakeInteractionOverlay(thingDef, this.Pawn, this.others[i]);
                        }
                        this.motes[i].Maintain();
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look<Thing>(ref this.others, "others", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.drawConnection, "drawConnection", false, false);
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.others != null && this.others.Count > 0)
                {
                    string pairedOthers = "Hauts_PairPrefix".Translate();
                    foreach (Thing t in this.others)
                    {
                        pairedOthers += "Hauts_PairedWithOther".Translate(t.Label);
                    }
                    return pairedOthers;
                }
                return base.CompLabelInBracketsExtra;
            }
        }
        public List<Thing> others;
        public List<MoteDualAttached> motes;
        public bool drawConnection;
    }
    /*If this hediff is removed, it alters the severity of - or removes - all other hediffs it is “paired” with. Intended to be used with an ability comp that assigns two hediffs and “pairs” them together (such as AbilityGiveHediffPaired).
     * removeLinkedHediffOnRemoval: causes removal of this hediff to remove all hediffs it’s paired with…
     * addSeverityToLinkedHediffOnRemoval: …otherwise, those hediffs’ severity is merely altered by this amount.
     * addSeverityOnLostHediff: every 10 ticks, clears out any references to hediffs that are 1) null, 2) on a destroyed pawn, or 3) on a {world pawn not in a caravan, not in a world transport object, and not borrowed by another faction}, in its list of paired hediffs (such as might be caused by the pawn of such a paired hediff being mothballed), and gains this much severity per such reference removed.
     * invalidateLinksIfSuspended: removes paired hediffs if they’re on pawns in a state of suspension (e.g. in a cryptosleep casket); if such a hediff is also a PairedHediff, it also removes the link on its end, but doesn’t consequently adjust its severity or remove it from its pawn.
     * SynchronizePairedHediffDurations(): if this hediff has HediffComp_Disappears, this method changes the HediffComp_Disappears timers of all hediffs it’s paired with to the same value as this hediff’s timer.*/
    public class HediffCompProperties_PairedHediff : HediffCompProperties
    {
        public HediffCompProperties_PairedHediff()
        {
            this.compClass = typeof(HediffComp_PairedHediff);
        }
        public bool invalidateLinksIfSuspended = false;
        public bool removeLinkedHediffOnRemoval = true;
        public float addedSeverityToLinkedHediffOnRemoval = 0f;
        public float addSeverityOnLostHediff;
    }
    public class HediffComp_PairedHediff : HediffComp
    {
        public HediffCompProperties_PairedHediff Props
        {
            get
            {
                return (HediffCompProperties_PairedHediff)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.hediffs != null && this.hediffs.Count > 0)
                {
                    string pairedHediffs = "Hauts_PairPrefix".Translate();
                    foreach (Hediff h in this.hediffs)
                    {
                        if (h != null && h.pawn != null)
                        {
                            pairedHediffs += "Hauts_PairedWithHediff".Translate(h.Label, h.pawn.Name != null ? h.pawn.Name.ToStringShort : h.pawn.Label);
                        }
                    }
                    return pairedHediffs;
                }
                return base.CompLabelInBracketsExtra;
            }
        }
        public virtual void SynchronizePairedHediffDurations()
        {
            HediffComp_Disappears hcd = this.parent.TryGetComp<HediffComp_Disappears>();
            if (hcd != null)
            {
                foreach (Hediff h in this.hediffs)
                {
                    if (h is HediffWithComps)
                    {
                        HediffComp_Disappears hcd2 = h.TryGetComp<HediffComp_Disappears>();
                        if (hcd2 != null)
                        {
                            hcd2.ticksToDisappear = hcd.ticksToDisappear;
                        }
                    }
                }
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.hediffs = new List<Hediff>();
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(15))
            {
                for (int i = this.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff h = this.hediffs[i];
                    Pawn p = h.pawn;
                    if (p.DestroyedOrNull() || (p.IsWorldPawn() && !p.IsCaravanMember() && !PawnUtility.IsTravelingInTransportPodWorldObject(p) && !p.IsBorrowedByAnyFaction()))
                    {
                        this.hediffs.RemoveAt(i);
                        this.parent.Severity += this.Props.addSeverityOnLostHediff;
                    }
                    else if (this.Props.invalidateLinksIfSuspended && p.Suspended)
                    {
                        this.hediffs.RemoveAt(i);
                        HediffComp_PairedHediff ph = h.TryGetComp<HediffComp_PairedHediff>();
                        if (ph != null)
                        {
                            ph.hediffs.Remove(this.parent);
                        }
                        this.parent.Severity += this.Props.addSeverityOnLostHediff;
                    }
                }
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            this.RemoveLinksOnRemoval();
        }
        public virtual void RemoveLinksOnRemoval()
        {
            foreach (Hediff h in hediffs)
            {
                if (h != null && h.pawn != null)
                {
                    if (h is HediffWithComps)
                    {
                        HediffComp_PairedHediff ph = h.TryGetComp<HediffComp_PairedHediff>();
                        if (ph != null)
                        {
                            ph.hediffs.Remove(this.parent);
                        }
                    }
                    if (this.Props.removeLinkedHediffOnRemoval)
                    {
                        h.pawn.health.RemoveHediff(h);
                    }
                    else
                    {
                        h.Severity += this.Props.addedSeverityToLinkedHediffOnRemoval;
                        HediffComp_MultiLink hcml = h.TryGetComp<HediffComp_MultiLink>();
                        if (hcml != null && hcml.others != null)
                        {
                            for (int i = hcml.others.Count - 1; i >= 0; i--)
                            {
                                if (hcml.others[i] is Pawn p && p == this.Pawn)
                                {
                                    hcml.others.RemoveAt(i);
                                    hcml.motes.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look<Hediff>(ref this.hediffs, "hediffs", LookMode.Reference, Array.Empty<object>());
        }
        public List<Hediff> hediffs;
    }
    /*Transforms this hediff to a different hediff if it belongs to a pawn of a specified category. Organic and Inorganic are checked only after all the other bools are.*/
    public class HediffCompProperties_PhylumMorphsHediff : HediffCompProperties
    {
        public HediffCompProperties_PhylumMorphsHediff()
        {
            this.compClass = typeof(HediffComp_PhylumMorphsHediff);
        }
        public HediffDef hediffIfOrganic;
        public HediffDef hediffIfInorganic;
        public HediffDef hediffIfAnimal;
        public HediffDef hediffIfDryad;
        public HediffDef hediffIfEntity;
        public HediffDef hediffIfHumanlike;
        public HediffDef hediffIfMech;
        public HediffDef hediffIfDrone;
        public HediffDef hediffIfMutant;
    }
    public class HediffComp_PhylumMorphsHediff : HediffComp
    {
        public HediffCompProperties_PhylumMorphsHediff Props
        {
            get
            {
                return (HediffCompProperties_PhylumMorphsHediff)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.Pawn.RaceProps.Animal && this.Props.hediffIfAnimal != null && (this.Props.hediffIfDryad == null || !this.Pawn.RaceProps.Dryad))
            {
                this.ReplaceHediff(this.Props.hediffIfAnimal);
            }
            else if (this.Pawn.RaceProps.Dryad && this.Props.hediffIfDryad != null)
            {
                this.ReplaceHediff(this.Props.hediffIfDryad);
            }
            else if (this.Pawn.RaceProps.IsAnomalyEntity && this.Props.hediffIfEntity != null)
            {
                this.ReplaceHediff(this.Props.hediffIfEntity);
            }
            else if (this.Pawn.RaceProps.Humanlike && this.Props.hediffIfHumanlike != null)
            {
                this.ReplaceHediff(this.Props.hediffIfHumanlike);
            }
            else if (this.Pawn.RaceProps.IsMechanoid && this.Props.hediffIfMech != null)
            {
                this.ReplaceHediff(this.Props.hediffIfMech);
            }
            else if (this.Pawn.RaceProps.IsDrone && this.Props.hediffIfDrone != null)
            {
                this.ReplaceHediff(this.Props.hediffIfDrone);
            }
            else if (this.Pawn.IsMutant && this.Props.hediffIfMutant != null)
            {
                this.ReplaceHediff(this.Props.hediffIfMutant);
            }
            else if (this.Pawn.RaceProps.IsFlesh)
            {
                if (this.Props.hediffIfOrganic != null)
                {
                    this.ReplaceHediff(this.Props.hediffIfOrganic);
                }
            }
            else if (this.Props.hediffIfInorganic != null)
            {
                this.ReplaceHediff(this.Props.hediffIfInorganic);
                return;
            }
        }
        public void ReplaceHediff(HediffDef newHediff)
        {
            Hediff hediff = HediffMaker.MakeHediff(newHediff, this.Pawn);
            HediffComp_Disappears hcd = this.parent.TryGetComp<HediffComp_Disappears>();
            HediffComp_Disappears hcd2 = hediff.TryGetComp<HediffComp_Disappears>();
            if (hcd != null && hcd2 != null)
            {
                hcd2.ticksToDisappear = hcd.ticksToDisappear;
            }
            this.Pawn.health.AddHediff(hediff, this.parent.Part);
            this.Pawn.health.RemoveHediff(this.parent);
        }
    }
    /*boy howdy I wonder what this one does!!!!!!!!!!!!!
     * satisfiesDrugAddictions: offsets all Need_Chemical needs found on the pawn by drugAddictionSatisfaction
     * ConditionsMetToSatisfyNeeds: only works if this is true*/
    public class HediffCompProperties_SatisfiesNeeds : HediffCompProperties
    {
        public HediffCompProperties_SatisfiesNeeds()
        {
            this.compClass = typeof(HediffComp_SatisfiesNeeds);
        }
        public int periodicity;
        public Dictionary<NeedDef, float> needsSatisfied;
        public bool satisfiesDrugAddictions;
        public float drugAddictionSatisfaction;
    }
    public class HediffComp_SatisfiesNeeds : HediffComp
    {
        public HediffCompProperties_SatisfiesNeeds Props
        {
            get
            {
                return (HediffCompProperties_SatisfiesNeeds)this.props;
            }
        }
        public virtual bool ConditionsMetToSatisfyNeeds
        {
            get
            {
                return true;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity, delta) && this.ConditionsMetToSatisfyNeeds)
            {
                foreach (Need n in this.Pawn.needs.AllNeeds)
                {
                    if (this.Props.needsSatisfied.ContainsKey(n.def))
                    {
                        n.CurLevel += this.Props.needsSatisfied.TryGetValue(n.def);
                    }
                    else if (this.Props.satisfiesDrugAddictions && n.def.needClass == typeof(Need_Chemical))
                    {
                        n.CurLevel += this.Props.drugAddictionSatisfaction;
                    }
                }
            }
        }
    }
    /*Periodically adjusts the xp of all affectedSkills by skillAdjustment amount.
     * ticks: determines the periodicity
     * statMultiplier: multiplies skillAdjustment by however much of this stat this pawn has…
     * statResistor: …and divides it by however much of this stat this pawn has. To avoid dividing by 0, the min possible value here is 0.001f
     * minLevel: can’t drop a skill’s level below this value
     * maxLevel: can’t raise a skill’s level above this value
     * bool affectsAptitudes: if true, minLevel and maxLevel account for skill level adjustment due to aptitudes; otherwise, the additional/missing levels are ignored
     * nullifyingTraits: skill adjustment does not occur if the pawn has one of these traits
     * showInTooltip: if true, displays “Grants # skill name xp per hour” in the hediff tooltip*/
    public class HediffCompProperties_SkillAdjustment : HediffCompProperties
    {
        public HediffCompProperties_SkillAdjustment()
        {
            this.compClass = typeof(HediffComp_SkillAdjustment);
        }
        public int ticks = 2500;
        public int minLevel = 0;
        public int maxLevel = int.MaxValue;
        public float skillAdjustment = -2000;
        public bool affectsAptitudes = false;
        public List<SkillDef> affectedSkills;
        public bool showInTooltip = true;
        public List<TraitDef> nullifyingTraits;
        public StatDef statMultiplier;
        public StatDef statResistor;
    }
    public class HediffComp_SkillAdjustment : HediffComp
    {
        public HediffCompProperties_SkillAdjustment Props
        {
            get
            {
                return (HediffCompProperties_SkillAdjustment)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (!this.Props.showInTooltip)
                {
                    return base.CompLabelInBracketsExtra;
                }
                string result = "";
                foreach (SkillDef s in this.Props.affectedSkills)
                {
                    result += "Hauts_SkillAdjustmentTooltip".Translate(Mathf.RoundToInt(60000f / (this.Props.skillAdjustment * this.Props.ticks)), s.LabelCap) + "\n";
                }
                return result;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.affectsAptitudes = this.Props.affectsAptitudes;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.ticks, delta))
            {
                if (this.Props.nullifyingTraits != null && this.Pawn.story != null)
                {
                    foreach (TraitDef td in this.Props.nullifyingTraits)
                    {
                        if (this.Pawn.story.traits.HasTrait(td))
                        {
                            return;
                        }
                    }
                }
                float skillAdjustment = this.Props.skillAdjustment * (this.Props.statMultiplier != null ? this.Pawn.GetStatValue(this.Props.statMultiplier) : 1f) / (this.Props.statResistor != null ? Math.Max(0.001f, this.Pawn.GetStatValue(this.Props.statResistor)) : 1f);
                foreach (SkillRecord s in this.Pawn.skills.skills)
                {
                    if (this.Props.affectedSkills.Contains(s.def) && s.GetLevel(this.affectsAptitudes) >= this.Props.minLevel && s.GetLevel(this.affectsAptitudes) < this.Props.maxLevel)
                    {
                        float skillAdjustment2 = skillAdjustment;
                        if (skillAdjustment2 < 0f)
                        {
                            if (this.Pawn.story != null && this.Pawn.story.traits.HasTrait(TraitDefOf.GreatMemory))
                            {
                                skillAdjustment2 *= 0.5f;
                            }
                            skillAdjustment2 *= this.ForgettingSpeed(s);
                        }
                        s.Learn(skillAdjustment2, true);
                        if (s.Level < 0)
                        {
                            s.Level = 0;
                        }
                    }
                }
            }
        }
        public float ForgettingSpeed(SkillRecord skill)
        {
            return 1f;
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.affectsAptitudes, "affectsAptitudes", false, false);
        }
        private bool affectsAptitudes;
    }
}
