using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace HautsFramework
{
    /*turns into another hediff if its severity gets high enough
     * aboveThisSeverity: when the hediff’s severity exceeds this value…
     * alternativeHediff: …it gets replaced with this hediff.
     * addToBrain: adds the new hediff to the brain*/
    public class HediffCompProperties_ChangeAboveSeverity : HediffCompProperties
    {
        public HediffCompProperties_ChangeAboveSeverity()
        {
            this.compClass = typeof(HediffComp_ChangeAboveSeverity);
        }
        public float aboveThisSeverity = 1f;
        public HediffDef alternativeHediff;
        public bool addToBrain = false;
    }
    public class HediffComp_ChangeAboveSeverity : HediffComp
    {
        public HediffCompProperties_ChangeAboveSeverity Props
        {
            get
            {
                return (HediffCompProperties_ChangeAboveSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity > this.Props.aboveThisSeverity)
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.alternativeHediff, this.Pawn);
                if (this.Props.addToBrain)
                {
                    this.Pawn.health.AddHediff(hediff, this.Pawn.health.hediffSet.GetBrain());
                }
                else
                {
                    this.Pawn.health.AddHediff(hediff, this.parent.Part);
                }
                hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
    }
    //opposite
    public class HediffCompProperties_ChangeBelowSeverity : HediffCompProperties
    {
        public HediffCompProperties_ChangeBelowSeverity()
        {
            this.compClass = typeof(HediffComp_ChangeBelowSeverity);
        }
        public float atOrBelowThisSeverity = 1f;
        public HediffDef alternativeHediff;
        public bool addToBrain = false;
    }
    public class HediffComp_ChangeBelowSeverity : HediffComp
    {
        public HediffCompProperties_ChangeBelowSeverity Props
        {
            get
            {
                return (HediffCompProperties_ChangeBelowSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity <= this.Props.atOrBelowThisSeverity)
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.alternativeHediff, this.Pawn, null);
                if (this.Props.addToBrain)
                {
                    this.Pawn.health.AddHediff(hediff, this.Pawn.health.hediffSet.GetBrain());
                }
                else
                {
                    this.Pawn.health.AddHediff(hediff, this.parent.Part);
                }
                hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
    }
    /*turns into another hediff if its severity exceeds the remaining sum hit points of the pawn's body parts
     *  ifAbove: if true, when the hediff’s severity goes above the pawn’s current hit points…
     *  ifBelow: or, if this is true, when its severity goes BELOW the pawn’s current hit points (both can be true; I don't really see the point of this, but you COULD do some weird exact hit point balancing act)…
     *  alternativeHediff, addToBrain: see ChangeAboveSeverity
     *  showSeverityOverHitPoints: displays the hediff’s severity over the pawn’s current hit points in the tooltip.
     *  ShouldTransform: handles the checking of whether or not to replace this hediff with alternativeHediff (i.e. isAbove and isBelow checks)*/
    public class HediffCompProperties_ChangeIfSeverityVsHitPoints : HediffCompProperties
    {
        public HediffCompProperties_ChangeIfSeverityVsHitPoints()
        {
            this.compClass = typeof(HediffComp_ChangeIfSeverityVsHitPoints);
        }
        public HediffDef alternativeHediff;
        public bool addToBrain = false;
        public bool ifAbove = true;
        public bool ifBelow = false;
        public bool showSeverityOverHitPoints = true;
    }
    public class HediffComp_ChangeIfSeverityVsHitPoints : HediffComp
    {
        public HediffCompProperties_ChangeIfSeverityVsHitPoints Props
        {
            get
            {
                return (HediffCompProperties_ChangeIfSeverityVsHitPoints)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showSeverityOverHitPoints)
                {
                    return base.CompTipStringExtra + "\n" + (int)this.parent.Severity + "/" + this.CurrentHitPoints;
                }
                return base.CompTipStringExtra;
            }
        }
        public virtual bool ShouldTransform()
        {
            return (this.Props.ifAbove && this.parent.Severity > this.CurrentHitPoints) || (this.Props.ifBelow && this.parent.Severity < this.CurrentHitPoints);
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.ShouldTransform())
            {
                this.TransformThis();
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        protected virtual void TransformThis()
        {
            Hediff hediff = HediffMaker.MakeHediff(this.Props.alternativeHediff, this.Pawn);
            if (this.Props.addToBrain)
            {
                this.Pawn.health.AddHediff(hediff, this.Pawn.health.hediffSet.GetBrain());
            }
            else
            {
                this.Pawn.health.AddHediff(hediff, this.parent.Part);
            }
        }
        protected float CurrentHitPoints
        {
            get
            {
                return HautsMiscUtility.HitPointTotalFor(this.Pawn);
            }
        }
    }
    //ewisott
    public class HediffCompProperties_KillBelowSeverity : HediffCompProperties
    {
        public HediffCompProperties_KillBelowSeverity()
        {
            this.compClass = typeof(HediffComp_KillBelowSeverity);
        }
        public float killBelow = 0.002f;
    }
    public class HediffComp_KillBelowSeverity : HediffComp
    {
        public HediffCompProperties_KillBelowSeverity Props
        {
            get
            {
                return (HediffCompProperties_KillBelowSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity < this.Props.killBelow)
            {
                this.Pawn.Kill(null, null);
            }
        }
    }
    /*alters if the hediff is visible or not depending on whether it is in a specified range.
    default range is obscenely large, instead of using a null check, because I wrote this before I figured out FLoatRanges can just be null. Whatever, it's not gonna kill anyone*/
    public class HediffCompProperties_SeverityDeterminesVisibility : HediffCompProperties
    {
        public HediffCompProperties_SeverityDeterminesVisibility()
        {
            this.compClass = typeof(HediffComp_SeverityDeterminesVisibility);
        }
        public FloatRange invisibleWithin = new FloatRange(-999f, -998f);
    }
    public class HediffComp_SeverityDeterminesVisibility : HediffComp
    {
        public HediffCompProperties_SeverityDeterminesVisibility Props
        {
            get
            {
                return (HediffCompProperties_SeverityDeterminesVisibility)this.props;
            }
        }
        public override bool CompDisallowVisible()
        {
            if (this.parent.Severity >= this.Props.invisibleWithin.min && this.parent.Severity < this.Props.invisibleWithin.max)
            {
                return true;
            }
            return false;
        }
    }
    /*On reaching a high enough severity, gives the pawn another specified hediff and loses severity for doing so.
     * severityToTrigger: once the hediff reaches this much severity, it begins the process of “creating a hediff by spending severity”, which shaves off this much severity in order to create…
     * hediffGiven: …this ‘created hediff’ and add it to the same pawn. If the pawn already has the created hediff, it will increase its severity instead…
     * severityToGive: …by this amount…
     * maxSeverityOfCreatedHediff: …unless this >0 and the created hediff’s severity is higher, in which case the creation process doesn’t occur (and severity is thus not spent)
     * showProgressInTooltip: shows “[current severity]/[severityToTrigger] charge” in the tooltip, listed to the nearest hundredth*/
    public class HediffCompProperties_CreateHediffBySpendingSeverity : HediffCompProperties
    {
        public HediffCompProperties_CreateHediffBySpendingSeverity()
        {
            this.compClass = typeof(HediffComp_CreateHediffBySpendingSeverity);
        }
        public float severityToTrigger;
        public HediffDef hediffGiven;
        public float maxSeverityOfCreatedHediff = -1f;
        public float severityToGive;
        public bool showProgressInTooltip;
    }
    public class HediffComp_CreateHediffBySpendingSeverity : HediffComp
    {
        public HediffCompProperties_CreateHediffBySpendingSeverity Props
        {
            get
            {
                return (HediffCompProperties_CreateHediffBySpendingSeverity)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawn".Translate(this.parent.Severity.ToStringByStyle(ToStringStyle.FloatMaxTwo), this.Props.severityToTrigger.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta) && this.parent.Severity >= this.Props.severityToTrigger)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffGiven);
                if (hediff != null)
                {
                    if (hediff.Severity < this.Props.maxSeverityOfCreatedHediff || this.Props.maxSeverityOfCreatedHediff <= 0f)
                    {
                        hediff.Severity = Math.Min(hediff.Severity + this.Props.severityToGive, this.Props.maxSeverityOfCreatedHediff);
                        this.parent.Severity -= this.Props.severityToTrigger;
                    }
                }
                else
                {
                    Hediff hediff2 = HediffMaker.MakeHediff(this.Props.hediffGiven, this.Pawn);
                    this.Pawn.health.AddHediff(hediff2);
                    this.parent.Severity -= this.Props.severityToTrigger;
                }
            }
        }
    }
    /*as above, except it plops down a random thing either in the pawn's own inventory or in a radius around it per severity-unit lost
     * Does not use ThingDefCountClasses because, uh, I did not know what those were when I made this comp. This has the disadvantage of requiring xpath to add new nodes to the dictionary,
     * but has the upside of letting you spawn a variable FloatRange of a thing rather than a fixed number each time.
     * Don't use this to make unminifiable buildings.
     * Created plants do not spawn in the target radius, they spawn in a random map cell that is suitable for them to grow in. Man, I really need to go back and touch this comp up*/
    public class HediffCompProperties_CreateThingsBySpendingSeverity : HediffCompProperties
    {
        public HediffCompProperties_CreateThingsBySpendingSeverity()
        {
            this.compClass = typeof(HediffComp_CreateThingsBySpendingSeverity);
        }
        public float severityToTrigger;
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
    public class HediffComp_CreateThingsBySpendingSeverity : HediffComp
    {
        public HediffCompProperties_CreateThingsBySpendingSeverity Props
        {
            get
            {
                return (HediffCompProperties_CreateThingsBySpendingSeverity)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawn".Translate(this.parent.Severity.ToStringByStyle(ToStringStyle.FloatMaxTwo), this.Props.severityToTrigger.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta) && this.parent.Severity >= this.Props.severityToTrigger && (this.Pawn.Spawned || this.Props.spawnInOwnInventory))
            {
                this.SpawnThings();
            }
        }
        public virtual KeyValuePair<ThingDef, FloatRange> GetThingToSpawn()
        {
            return this.Props.spawnedThingAndCountPerTrigger.RandomElement();
        }
        public void SpawnThings()
        {
            KeyValuePair<ThingDef, FloatRange> toSpawn = this.GetThingToSpawn();
            ThingDef def = toSpawn.Key;
            float countPerTrigger = toSpawn.Value.RandomInRange;
            if (this.Props.spawnInOwnInventory && def != null && !def.EverHaulable)
            {
                Log.Error("Hauts_CantSpawnUnminified".Translate(def.LabelCap, this.Pawn.Name.ToStringFull));
                return;
            }
            this.SpawningBlock(def, countPerTrigger);
        }
        public void SpawningBlock(ThingDef def, float countPerTrigger)
        {
            this.FinalizeThingBeforePlacing(def, countPerTrigger, out Thing thing);
            while (this.parent.Severity >= this.Props.severityToTrigger)
            {
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
                this.parent.Severity -= this.Props.severityToTrigger;
                if (!this.Props.fullyRandomizeSpawns && this.parent.Severity >= this.Props.severityToTrigger)
                {
                    this.FinalizeThingBeforePlacing(def, countPerTrigger, out thing);
                }
            }
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
            if (thing.def.CanHaveFaction)
            {
                if (this.Props.setToOwnFaction)
                {
                    thing.SetFaction(this.Pawn.Faction, null);
                }
            }
            thing.stackCount = Math.Min((int)Math.Floor(countPerTrigger), def.stackLimit);
            thing.Notify_DebugSpawned();
        }
        public void AddToInventory(Thing thing)
        {
            if (thing != null)
            {
                this.Pawn.inventory.innerContainer.TryAdd(thing, true);
            }
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
                this.parent.Severity -= this.Props.severityToTrigger;
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
    }
}
