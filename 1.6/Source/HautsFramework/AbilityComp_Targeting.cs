using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HautsFramework
{
    /*Limits NPC use of this ability (ability must be aiCanUse, obviously) on the current combat target to only occur if that target is within a distanceRange
     * mustBeMelee: prevents NPC casting if it's not melee (if it would incur the mood penalty from the Brawler trait, it's not melee)*/
    public class CompProperties_AbilityAiTargetingDistanceRange : CompProperties_AbilityEffect
    {
        public FloatRange distanceRange;
        public bool mustBeMelee;
    }
    public class CompAbilityEffect_AiTargetingDistanceRange : CompAbilityEffect
    {
        public new CompProperties_AbilityAiTargetingDistanceRange Props
        {
            get
            {
                return (CompProperties_AbilityAiTargetingDistanceRange)this.props;
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (this.parent.pawn.SpawnedOrAnyParentSpawned && (!this.Props.mustBeMelee || this.parent.pawn.equipment.Primary == null || !this.parent.pawn.equipment.Primary.def.IsRangedWeapon) && this.parent.pawn.PositionHeld.DistanceTo(target.Cell) > this.Props.distanceRange.min && this.parent.pawn.PositionHeld.DistanceTo(target.Cell) <= this.Props.distanceRange.max)
            {
                return true;
            }
            return false;
        }
    }
    //Every "periodicity" ticks, NPCs with this ability run the AdditionalQualifiers method to determine if they should cast this ability on themselves or not
    public class CompProperties_AbilityAiAppliesToSelf : CompProperties_AbilityEffect
    {
        public int periodicity = 250;
    }
    public class CompAbilityEffect_AiAppliesToSelf : CompAbilityEffect
    {
        public new CompProperties_AbilityAiAppliesToSelf Props
        {
            get
            {
                return (CompProperties_AbilityAiAppliesToSelf)this.props;
            }
        }
        public virtual int Periodicity
        {
            get
            {
                return this.Props.periodicity;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.pawn.IsHashIntervalTick(this.Periodicity) && this.parent.pawn.Spawned && !this.parent.pawn.IsColonistPlayerControlled && !this.parent.GizmoDisabled(out string text) && this.parent.def.aiCanUse && (!this.parent.pawn.InMentalState || this.parent.pawn.InAggroMentalState) && this.parent.CanCast && (this.parent.pawn.CurJob == null || (this.parent.pawn.CurJob.ability == null && !(this.parent.pawn.CurJob.verbToUse is VEF.Abilities.Verb_CastAbility))) && this.AdditionalQualifiers())
            {
                this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(this.parent.pawn), null), JobCondition.InterruptForced);
            }
        }
        public virtual bool AdditionalQualifiers()
        {
            return true;
        }
    }
    /*NPCs periodically 'scan' around themselves for targets, according to criteria you can specify in derivatives
     * periodicity: interval between each scan, in ticks
     * scanForPawnsOnly: conducts the scan by going through the list of all Pawns in the same map; if false, scans all Things within range of the caster. If the ability has long range and predominantly/only targets pawns, you should set this to true for generally better performance
     * onlyHostiles: restricts evaluation of potential targets to only those hostile to the caster
     * usableInMentalStates: enables scanning - and therefore casting - even while the caster is in a mental state
     * CanScan: scans are conducted only if this returns true (by default, checks if the AbilityDef has aiCanUse = true, and if the pawn is in a mental state, if usableInMentalStates = true)
     * Range: the radius in which potential targets are examined. If a potential target is vetted via…
     * AdditionalQualifiers: …this, this ability will be cast on that target. Also used to evaluate whether this ability can be used on the current combat target for AICanTargetNow.*/
    public class CompProperties_AbilityAiScansForTargets : CompProperties_AbilityEffect
    {
        public int periodicity = 250;
        public bool scanForPawnsOnly = false;
        public bool onlyHostiles = false;
        public bool usableInMentalStates = false;
    }
    public class CompAbilityEffect_AiScansForTargets : CompAbilityEffect
    {
        public new CompProperties_AbilityAiScansForTargets Props
        {
            get
            {
                return (CompProperties_AbilityAiScansForTargets)this.props;
            }
        }
        public virtual float Range
        {
            get
            {
                return this.parent.verb.verbProps.range * (this.parent.verb.verbProps.rangeStat != null ? this.parent.pawn.GetStatValue(this.parent.verb.verbProps.rangeStat) : 1f);
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.pawn.IsHashIntervalTick(this.Props.periodicity) && this.parent.pawn.Spawned && !this.parent.pawn.IsColonistPlayerControlled && !this.parent.GizmoDisabled(out string text) && this.parent.CanCast && this.CanScan() && (this.parent.pawn.CurJob == null || (this.parent.pawn.CurJob.ability == null && (this.parent.pawn.CurJob.verbToUse == null || !(this.parent.pawn.CurJob.verbToUse is VEF.Abilities.Verb_CastAbility)))))
            {
                if (this.Props.scanForPawnsOnly)
                {
                    foreach (Pawn p in this.parent.pawn.Map.mapPawns.AllPawnsSpawned.InRandomOrder())
                    {
                        if (p.Position.DistanceTo(this.parent.pawn.Position) <= this.Range)
                        {
                            if ((!this.Props.onlyHostiles || this.parent.pawn.HostileTo(p)) && this.VetPotentialTarget(p))
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    foreach (Thing thing in GenRadial.RadialDistinctThingsAround(this.parent.pawn.Position, this.parent.pawn.Map, this.Range, true))
                    {
                        if ((!this.Props.onlyHostiles || (thing.Faction != null && this.parent.pawn.HostileTo(thing.Faction))) && this.VetPotentialTarget(thing))
                        {
                            return;
                        }
                    }
                }
            }
        }
        public virtual bool CanScan()
        {
            return this.parent.def.aiCanUse && (this.Props.usableInMentalStates || !this.parent.pawn.InMentalState);
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (target.Thing != null)
            {
                return this.AdditionalQualifiers(target.Thing);
            }
            return base.AICanTargetNow(target);
        }
        protected bool VetPotentialTarget(Thing thing)
        {
            if (this.AdditionalQualifiers(thing))
            {
                this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(thing), null), JobCondition.InterruptForced);
                return true;
            }
            return false;
        }
        public virtual bool AdditionalQualifiers(Thing thing)
        {
            return true;
        }
    }
    /*NPCs with this ability will try using it on a location away from any detected hostile combatants once every 250 ticks if their % body part hp threshold is below hpThreshold.
     * It's like how hostile sanguophages (rarely) jump away from your pawns, but is intrinsic to having the ability instead of requiring a thinktree patch.
     * Only apply to an ability that targets locations. Intended to be used with mobility abilities, such as jumps or teleports.
     * mustBeRanged: prevents NPC casting if not ranged (as per Brawler rules)
     * maxTicksSinceDamage: only triggers if the last time the caster took damage was no longer than this many ticks ago. If you're not taking fire, why retreat?
     * minDangerCount: only triggers if at least this many hostile combatants are detected…
     * minDangerRange: …within this range.*/
    public class CompProperties_AbilityAiUsesToRetreat : CompProperties_AbilityEffect
    {
        public float hpThreshold;
        public bool mustBeRanged;
        public int maxTicksSinceDamage = 120;
        public float minDangerRange = 2;
        public int minDangerCount = 1;
    }
    public class CompAbilityEffect_AiUsesToRetreat : CompAbilityEffect
    {
        public new CompProperties_AbilityAiUsesToRetreat Props
        {
            get
            {
                return (CompProperties_AbilityAiUsesToRetreat)this.props;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = this.parent.pawn;
            if (pawn.IsHashIntervalTick(250) && pawn.Spawned && !this.parent.pawn.IsColonistPlayerControlled && !this.parent.GizmoDisabled(out string text) && (!this.Props.mustBeRanged || (this.parent.pawn.equipment.Primary != null && this.parent.pawn.equipment.Primary.def.IsRangedWeapon)) && this.parent.def.aiCanUse && !this.parent.pawn.InAggroMentalState && this.parent.CanCast && !this.parent.Casting && this.parent.pawn.mindState.lastHarmTick > 0 && Find.TickManager.TicksGame < this.parent.pawn.mindState.lastHarmTick + this.Props.maxTicksSinceDamage && this.HpCheck)
            {
                if (PawnUtility.EnemiesAreNearby(this.parent.pawn, 9, true, this.Props.minDangerRange, this.Props.minDangerCount))
                {
                    if (this.EscapePosition(out IntVec3 c, this.Range))
                    {
                        this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(c), null), JobCondition.InterruptForced);
                    }
                }
            }
        }
        private float Range
        {
            get
            {
                float result = this.parent.verb.EffectiveRange;
                if (ModsConfig.RoyaltyActive && this.parent.def.category == DefDatabase<AbilityCategoryDef>.GetNamed("Skip"))
                {
                    result *= this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor);
                }
                return result;
            }
        }
        private bool HpCheck
        {
            get
            {
                return HautsMiscUtility.MissingHitPointPercentageFor(this.parent.pawn) > this.Props.hpThreshold;
            }
        }
        private bool EscapePosition(out IntVec3 relocatePosition, float maxDistance)
        {
            this.tmpHostiles.Clear();
            this.tmpHostiles.AddRange(from a in this.parent.pawn.Map.attackTargetsCache.GetPotentialTargetsFor(this.parent.pawn) where !a.ThreatDisabled(this.parent.pawn) select a.Thing);
            relocatePosition = CellFinderLoose.GetFallbackDest(this.parent.pawn, this.tmpHostiles, maxDistance, 5f, 5f, 20, (IntVec3 c) => this.parent.verb.ValidateTarget(c, false));
            this.tmpHostiles.Clear();
            return relocatePosition.IsValid;
        }
        private List<Thing> tmpHostiles = new List<Thing>();
    }
    //exactly what it says on the tin. For use with aiCanUse abilities, obviously
    public class CompAbilityEffect_AvoidTargetingStunnedPawns : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect Props
        {
            get
            {
                return (CompProperties_AbilityEffect)this.props;
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return target.Pawn == null || !target.Pawn.stances.stunner.Stunned;
        }
    }
    /*"faction-agnostic" alternative to the targeting parameter neverTargetHostileFaction, because neverTargetHostileFaction and every other "For Player Use Only!" tool is my enemy
     Ability cannot target pawns hostile to the caster, or to whom the caster is hostile*/
    public class CompProperties_AbilityNeverTargetHostile : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityNeverTargetHostile()
        {
            this.compClass = typeof(CompAbilityEffect_NeverTargetHostile);
        }
    }
    public class CompAbilityEffect_NeverTargetHostile : CompAbilityEffect
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (target.Pawn != null && target.Pawn.HostileTo(this.parent.pawn))
            {
                return false;
            }
            return true;
        }
    }
    //as AbilityTeleport, but instead of choosing target and destination, you just choose destination and the caster teleports there
    public class CompProperties_AbilityTeleportSelf : CompProperties_AbilityEffect
    {
        public bool requiresLineOfSight;
        public IntRange stunTicks;
        public ClamorDef destClamorType;
        public int destClamorRadius;
    }
    public class CompAbilityEffect_TeleportSelf : CompAbilityEffect
    {
        public new CompProperties_AbilityTeleportSelf Props
        {
            get
            {
                return (CompProperties_AbilityTeleportSelf)this.props;
            }
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(this.parent.pawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                        dataAttachedOverlay.link.detachAfterTicks = 5;
                        this.parent.pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                        FleckMaker.Static(t.Cell, this.parent.pawn.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    }
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(this.parent.pawn.Position, this.parent.pawn.Map, false));
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(t.Cell, this.parent.pawn.Map, false));
                    }
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target.IsValid && this.Valid(target))
            {
                Pawn pawn = this.parent.pawn;
                if (!this.parent.def.HasAreaOfEffect)
                {
                    this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, null);
                }
                else
                {
                    this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_EntryNoDelay.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, null);
                }
                this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Exit.Spawn(target.Cell, pawn.Map, 1f), target.Cell, 60, null);
                CompCanBeDormant compCanBeDormant = pawn.TryGetComp<CompCanBeDormant>();
                if (compCanBeDormant != null)
                {
                    compCanBeDormant.WakeUp();
                }
                pawn.Position = target.Cell;
                if ((pawn.Faction == Faction.OfPlayer || pawn.IsPlayerControlled) && pawn.Position.Fogged(pawn.Map))
                {
                    FloodFillerFog.FloodUnfog(pawn.Position, pawn.Map);
                }
                pawn.stances.stunner.StunFor(this.Props.stunTicks.RandomInRange, this.parent.pawn, false, false);
                pawn.Notify_Teleported(true, true);
                CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn.Position, pawn);
                if (this.Props.destClamorType != null)
                {
                    GenClamor.DoClamor(pawn, target.Cell, (float)this.Props.destClamorRadius, this.Props.destClamorType);
                }
            }
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return this.parent.pawn.Spawned && !this.parent.pawn.kindDef.skipResistant && target.IsValid && (this.parent.verb.verbProps.range <= 0f || target.Cell.DistanceTo(target.Cell) <= this.parent.verb.verbProps.range) && (!this.Props.requiresLineOfSight || GenSight.LineOfSight(target.Cell, this.parent.pawn.Position, this.parent.pawn.Map, false, null, 0, 0)) && this.CanPlaceSelectedTargetAt(target);
        }
        public bool CanPlaceSelectedTargetAt(LocalTargetInfo target)
        {
            Pawn pawn = this.parent.pawn;
            Building_Door door = target.Cell.GetDoor(pawn.Map);
            return (door == null || door.CanPhysicallyPass(pawn)) && (pawn.Spawned && !target.Cell.Impassable(this.parent.pawn.Map)) && target.Cell.WalkableBy(this.parent.pawn.Map, pawn);
        }
    }
}
