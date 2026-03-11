using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    /*enables the resurrection of specific deceased pawns after a delay. Each pawn added to this world component gets its own timer, and once the timer’s up, it gets resurrected.
     * This effect can be configured to display a message on death (when a pawn is inducted into the world component), as well as when the delayed resurrection actually goes off.
     * If a pawn would be affected by multiple DRs, the shortest one takes priority
     * Resurrection obviously cannot occur if the corpse is destroyed
     * Operates in rare ticks because this used to be a Harmony patch for Corpse rare ticks in 1.4, but that patch did not work in 1.5.*/
    public class WorldComponent_HautsDelayedResurrections : WorldComponent
    {
        public WorldComponent_HautsDelayedResurrections(World world) : base(world)
        {
            this.world = world;
            this.pawns = new List<Hauts_DelayedResurrection>();
        }
        public void StartDelayedResurrection(Corpse corpse, IntRange rareTickRange, string explanationKey, bool shouldSendMessage, bool shouldTranslateMessage, bool preventRisingAsShambler, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            int initialRareTicks = rareTickRange.RandomInRange;
            bool alreadyHasDelayedRes = false;
            foreach (Hauts_DelayedResurrection hdr in this.pawns)
            {
                if (hdr.corpse == corpse && hdr.rareTicksRemaining > initialRareTicks)
                {
                    alreadyHasDelayedRes = true;
                    hdr.rareTicksRemaining = initialRareTicks;
                    hdr.explanationKey = explanationKey;
                    hdr.shouldSendMessage = shouldSendMessage;
                    hdr.shouldTranslateMessage = shouldTranslateMessage;
                    hdr.preventRisingAsShambler = preventRisingAsShambler;
                    hdr.mutation = mutation;
                    hdr.mutationSeverity = mutationSeverity;
                    break;
                }
            }
            if (!alreadyHasDelayedRes)
            {
                this.pawns.Add(new Hauts_DelayedResurrection(corpse, initialRareTicks, explanationKey, shouldSendMessage, shouldTranslateMessage, preventRisingAsShambler, mutation, mutationSeverity));
            }
        }
        public void RemoveFromRoster(Corpse corpse)
        {
            for (int i = this.pawns.Count - 1; i >= 0; i--)
            {
                if (this.pawns[i].corpse == corpse && this.pawns[i].rareTicksRemaining > 0)
                {
                    this.pawns.Remove(this.pawns[i]);
                    break;
                }
            }
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                for (int i = this.pawns.Count - 1; i >= 0; i--)
                {
                    if (this.pawns[i].corpse == null || this.pawns[i].corpse.Destroyed || this.pawns[i].corpse.InnerPawn == null)
                    {
                        this.pawns.Remove(this.pawns[i]);
                        continue;
                    }
                    this.pawns[i].rareTicksRemaining--;
                    if (this.pawns[i].rareTicksRemaining <= 0)
                    {
                        Pawn pawn = this.pawns[i].corpse.InnerPawn;
                        if (ResurrectionUtility.TryResurrect(pawn))
                        {
                            if (ModsConfig.AnomalyActive && pawn.IsShambler)
                            {
                                pawn.mutant.Revert();
                            }
                            if (this.pawns[i].mutation != null)
                            {
                                if (pawn.health.hediffSet.HasHediff(this.pawns[i].mutation))
                                {
                                    pawn.health.hediffSet.GetFirstHediffOfDef(this.pawns[i].mutation).Severity += this.pawns[i].mutationSeverity;
                                }
                                else
                                {
                                    Hediff hediff = HediffMaker.MakeHediff(this.pawns[i].mutation, pawn);
                                    pawn.health.AddHediff(hediff, null);
                                    hediff.Severity = this.pawns[i].mutationSeverity;
                                }
                            }
                            if (this.pawns[i].shouldSendMessage && (PawnUtility.ShouldSendNotificationAbout(pawn) || (pawn.Faction != null && pawn.Faction == Faction.OfPlayer) || pawn.SpawnedOrAnyParentSpawned || (pawn.IsCaravanMember() && pawn.GetCaravan().IsPlayerControlled)))
                            {
                                LookTargets target = null;
                                if (pawn.Spawned)
                                {
                                    target = pawn;
                                }
                                Messages.Message((this.pawns[i].shouldTranslateMessage ? this.pawns[i].explanationKey.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : this.pawns[i].explanationKey), target, MessageTypeDefOf.PositiveEvent, true);
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Hauts_DelayedResurrection>(ref this.pawns, "pawns", LookMode.Deep, Array.Empty<object>());
        }
        public List<Hauts_DelayedResurrection> pawns = new List<Hauts_DelayedResurrection>();
    }
    public class Hauts_DelayedResurrection : IExposable
    {
        public Hauts_DelayedResurrection()
        {
        }
        public Hauts_DelayedResurrection(Corpse corpse, IntRange rareTickRange, string explanationKey, bool shouldSendMessage = true, bool shouldTranslateMessage = true, bool preventRisingAsShambler = true, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            this.corpse = corpse;
            this.rareTicksRemaining = rareTickRange.RandomInRange;
            this.explanationKey = explanationKey;
            this.shouldSendMessage = shouldSendMessage;
            this.shouldTranslateMessage = shouldTranslateMessage;
            this.preventRisingAsShambler = preventRisingAsShambler;
            this.mutation = mutation;
            this.mutationSeverity = mutationSeverity;
        }
        public Hauts_DelayedResurrection(Corpse corpse, int initialRareTicks, string explanationKey, bool shouldSendMessage = true, bool shouldTranslateMessage = true, bool preventRisingAsShambler = true, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            this.corpse = corpse;
            this.rareTicksRemaining = initialRareTicks;
            this.explanationKey = explanationKey;
            this.shouldSendMessage = shouldSendMessage;
            this.shouldTranslateMessage = shouldTranslateMessage;
            this.preventRisingAsShambler = preventRisingAsShambler;
            this.mutation = mutation;
            this.mutationSeverity = mutationSeverity;
        }
        public void ExposeData()
        {
            Scribe_References.Look<Corpse>(ref this.corpse, "corpse", false);
            Scribe_Values.Look<int>(ref this.rareTicksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look<string>(ref this.explanationKey, "explanationKey", "");
            Scribe_Values.Look<bool>(ref this.shouldSendMessage, "shouldSendMessage", true);
            Scribe_Values.Look<bool>(ref this.shouldTranslateMessage, "shouldTranslateMessage", true);
            Scribe_Values.Look<bool>(ref this.preventRisingAsShambler, "preventRisingAsShambler", true);
        }
        public Corpse corpse;
        public int rareTicksRemaining;
        public string explanationKey = "";
        public bool shouldSendMessage;
        public bool shouldTranslateMessage;
        public bool preventRisingAsShambler;
        public HediffDef mutation;
        public float mutationSeverity;
    }
    /*inducts the pawn into the DR system on death, provided they meet the 
     * chance: immediately after dying, if a random number from 0-1 <= this value…
     * rareTickDelay: …the pawn will resurrect after [250 ticks*a randomly selected integer value from this range] has elapsed.
     * onDeathMessage: displays this text message in the top left when the pawn dies.
     * onRezMessage: displays this text message in the top left when the delayed resurrection finally occurs.
     * shouldTranslateOnDeath: makes it so the onDeathMessage string must be a language key, which will be translaged using the same “PAWN_pronoun” sort of syntax trait descriptions have going on.
     * shouldTranslateOnRez: ditto, but for the onRezMessage.
     * shouldSendMessage: onDeathMessage and onRezMessage will only be sent if this is true.
     * requiredTrait: (if specified) this hediff will only induce delayed resurrection if the pawn has this trait.
     * preventRisingAsShambler: delayed resurrection induced through this hediff prevents the corpse from rising as a shambler. Note that this only prevents shambler-ification methods that actually check CanResurrectAsShambler (e.g. deadlife dust or death pall); thus, directly doing the deed via dev mode will still result in a shambler.
     * mutationChance: when the delayed resurrection occurs, if a random number from 0-1 <= this value…
     * potentialMutation: …this hediff is added to the pawn…
     * mutationSeverity: …with a random value from within this range as its severity.*/
    public class HediffCompProperties_DelayedResurrection : HediffCompProperties
    {
        public HediffCompProperties_DelayedResurrection()
        {
            this.compClass = typeof(HediffComp_DelayedResurrection);
        }
        public float chance = 1f;
        public IntRange rareTickDelay;
        public string onDeathMessage;
        public string onRezMessage;
        public bool shouldTranslateOnDeath = true;
        public bool shouldTranslateOnRez = true;
        public bool shouldSendMessage = true;
        public bool preventRisingAsShambler = true;
        public TraitDef requiredTrait = null;
        public HediffDef potentialMutation;
        public float mutationChance;
        public FloatRange mutationSeverity;
    }
    public class HediffComp_DelayedResurrection : HediffComp
    {
        public HediffCompProperties_DelayedResurrection Props
        {
            get
            {
                return (HediffCompProperties_DelayedResurrection)this.props;
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (Rand.Chance(this.Props.chance) && (this.Pawn.story == null || this.Props.requiredTrait == null || this.Pawn.story.traits.HasTrait(this.Props.requiredTrait)))
            {
                if (this.Props.shouldSendMessage)
                {
                    Messages.Message((this.Props.shouldTranslateOnDeath ? this.Props.onDeathMessage.Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve() : this.Props.onDeathMessage), this.Pawn.Corpse != null ? this.Pawn.Corpse : null, MessageTypeDefOf.NeutralEvent, true);
                }
                HediffDef mut = null;
                float mutSeverity = this.Props.mutationSeverity.RandomInRange;
                if (Rand.Chance(this.Props.mutationChance))
                {
                    mut = this.Props.potentialMutation;
                }
                HautsMiscUtility.StartDelayedResurrection(this.Pawn, this.Props.rareTickDelay, this.Props.onRezMessage, this.Props.shouldSendMessage, this.Props.shouldTranslateOnRez, this.Props.preventRisingAsShambler, mut ?? null, mutSeverity);
            }
        }
    }
}
