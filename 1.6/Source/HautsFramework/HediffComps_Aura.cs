using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    /*Emits a visible circular zone around the parent pawn, affecting pawns inside it. Aura instantiates its effects periodically (default every 15 ticks).
     * In order to prevent auras of identical size from “hiding” each other, their visual effect will randomly dilate/contract slightly over time.
     * This comp has no effect on pawns inside it. See its children, AuraHediff and AuraThought.
     * tickPeriodicity: how often the aura instantiates its effects. As the name indicates, this measures time in ticks
     * range: aura radius = this value…
     * bonusRangePerSeverity: + [this value *however much severity this hediff has]...
     * rangeModifier: * however much of this stat the pawn has.
     * scanByPawnListerNotByGrid: while in a map, makes it find which pawns to affect by iterating through the map’s pawn lister; if false, iterates by searching all cells in the radius. The latter may be more performant for small auras, especially if you would predominantly expect the aura to see heavy use in high-pawn-per-map environments.
     * max|minRangeModifier: radius calculation uses this value instead if rangeModifier is higher|lower.
     * affectsSelf: makes AffectSelf() run.
     * affectsHostiles: makes pawns that are hostile to the pawn eligible for AffectPawn().
     * affectsAllies|Mechs|Fleshies|Entities|Drones|OthersInCaravan: ditto for non-hostile pawns, mechs, non-mechs, Anomaly unnatural entities, drones
     * mutantsAreEntities: makes mutants count as entities when determining whether or not they are eligible for AffectPawn()
     * functionalSeverity: turns the aura off while this hediff’s severity is outside this range.
     * color: rgb or rgba tint to apply to the graphical representation of the aura’s effect.
     * mote: the graphical representation of the aura’s effect. Use a MoteThrownAttached_Aura, such as the one provided in the XML expressly for this purpose. !!!!!Do not set its realTime to true, or the game will freeze!!!!!
     * canToggleVisualization: grants the pawn a gizmo to determine whether the aura’s Mote should be rendered (from one of four options: always visible, visible while drafted, visible while selected, and not visible). This does not affect the aura’s function in any other way; visualization is automatically reset to always-visible if the pawn stops being player-controlled.
     * visIcon (default Other/ShieldBubble): texture path of the Mote-rendering gizmo. Personal preference is to use the same graphic as the aura itself, which for all the auras I've made is just the shield bubble tbf
     * visLabel: label of the Mote-rendering gizmo. Considering making a standard language string for this that just substitutes in this string (or remove this altogether, and just have it have the name of the hediff) for a particular name argument
     * visTooltip|visTooltipFantasy: tooltip of the Mote-rendering gizmo, but only if IsHighFantasy is false|true.
     * ShouldBeActive: won't attempt to affect anything and will not be drawn UNLESS this is true. This is where disappearsWhileDowned comes into play
     * FunctionalRange: the radius calculation
     * ValidatePawn: handles if a pawn should be affected by AffectPawn
     * AffectSelf: determines what effect happens to self, if anything
     * AffectPawn: determines what effect happens to affected pawns other than self, if anything*/
    public class HediffCompProperties_Aura : HediffCompProperties
    {
        public HediffCompProperties_Aura()
        {
            this.compClass = typeof(HediffComp_Aura);
        }
        public float range;
        public float bonusRangePerSeverity;
        public int tickPeriodicity = 15;
        public StatDef rangeModifier = null;
        public float maxRangeModifier = 1f;
        public float minRangeModifier = 1f;
        public Color color;
        public bool affectsSelf = true;
        public bool affectsHostiles;
        public bool affectsAllies;
        public bool affectsMechs = true;
        public bool affectsDrones = true;
        public bool affectsFleshies = true;
        public bool affectsEntities = true;
        public bool mutantsAreEntities = true;
        public bool affectsOthersInCaravan = true;
        public bool disappearsWhileDowned = true;
        public FloatRange functionalSeverity = new FloatRange(-999f, 99999f);
        public bool scanByPawnListerNotByGrid = true;
        public ThingDef mote;
        public bool canToggleVisualization;
        public string visIcon = "Other/ShieldBubble";
        public string visLabel;
        public string visTooltip;
        public string visTooltipFantasy;
    }
    public class HediffComp_Aura : HediffComp
    {
        public HediffCompProperties_Aura Props
        {
            get
            {
                return (HediffCompProperties_Aura)this.props;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (this.Props.canToggleVisualization)
            {
                if (this.uiIcon == null)
                {
                    this.uiIcon = ContentFinder<Texture2D>.Get(this.Props.visIcon, true);
                    this.buttonLabel = this.Props.visLabel;
                    this.buttonTooltip = (ModCompatibilityUtility.IsHighFantasy() ? this.Props.visTooltipFantasy : this.Props.visTooltip);
                }
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = this.buttonLabel;
                command_Action.defaultDesc = this.buttonTooltip;
                command_Action.icon = this.uiIcon;
                if (Find.Selector.NumSelected > 1)
                {
                    Command_Action command_Action2 = command_Action;
                    command_Action2.defaultLabel = command_Action2.defaultLabel + " (" + this.Pawn.LabelShort + ")";
                }
                command_Action.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    Action action0 = delegate
                    {
                        this.visSetting = AuraVisSetting.Enabled;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting0".Translate(), action0, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Action action1 = delegate
                    {
                        this.visSetting = AuraVisSetting.WhileDrafted;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting1".Translate(), action1, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Action action2 = delegate
                    {
                        this.visSetting = AuraVisSetting.WhileSelected;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting2".Translate(), action2, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Action action3 = delegate
                    {
                        this.visSetting = AuraVisSetting.Disabled;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting3".Translate(), action3, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                yield return command_Action;
                yield break;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                return "Hauts_AuraTooltip".Translate(Mathf.RoundToInt(this.FunctionalRange));
            }
        }
        public virtual float FunctionalRange
        {
            get
            {
                return this.cachedRange;
            }
        }
        public virtual float DetermineRange()
        {
            if (this.Props.rangeModifier != null)
            {
                return (this.Props.range + (this.Props.bonusRangePerSeverity * this.parent.Severity)) * Math.Min(this.Props.maxRangeModifier, Math.Max(this.Props.minRangeModifier, this.parent.pawn.GetStatValue(this.Props.rangeModifier)));
            }
            return this.Props.range + (this.Props.bonusRangePerSeverity * this.parent.Severity);
        }
        public virtual bool ShouldBeActive
        {
            get
            {
                return !this.Pawn.Downed || !this.Props.disappearsWhileDowned || !this.Props.functionalSeverity.Includes(this.parent.Severity);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Props.mote != null)
            {
                if (this.Pawn.Spawned)
                {
                    if (this.ShouldBeActive && (this.visSetting == AuraVisSetting.Enabled || (this.Pawn.Drafted && this.visSetting == AuraVisSetting.WhileDrafted) || (Find.Selector.SelectedPawns.Contains(this.Pawn) && this.visSetting == AuraVisSetting.WhileSelected)))
                    {
                        if (this.mote == null || this.mote.Destroyed)
                        {
                            this.mote = (MoteThrownAttached_Aura)MoteMaker.MakeAttachedOverlay(base.Pawn, this.Props.mote, Vector3.zero, 2 * this.FunctionalRange, -1f);
                            this.mote.instanceColor = this.Props.color;
                            this.mote.range = this.FunctionalRange;
                            if (this.Pawn.IsHashIntervalTick(10))
                            {
                                this.mote.link1.UpdateDrawPos();
                            }
                        }
                        else
                        {
                            this.mote.range = this.FunctionalRange;
                            this.mote.Maintain();
                        }
                    }
                }
                else if ((this.mote != null && !this.mote.Destroyed))
                {
                    this.mote.Destroy(DestroyMode.Vanish);
                }
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn pawn = this.parent.pawn;
            if (!pawn.IsHashIntervalTick(this.Props.tickPeriodicity, delta) || !this.ShouldBeActive)
            {
                return;
            }
            this.cachedRange = Math.Max(this.DetermineRange(),0f);
            if (pawn.Spawned && !pawn.IsPlayerControlled)
            {
                this.visSetting = AuraVisSetting.Enabled;
            }
            if (this.Props.affectsSelf)
            {
                this.AffectSelf();
            }
            if (pawn.Spawned)
            {
                List<Pawn> pawns = new List<Pawn>();
                if (this.Props.scanByPawnListerNotByGrid)
                {
                    pawns = (List<Pawn>)pawn.Map.mapPawns.AllPawnsSpawned;
                }
                else
                {
                    pawns = GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.FunctionalRange, true).OfType<Pawn>().Distinct<Pawn>().ToList();
                }
                this.AffectPawns(pawn, pawns);
                return;
            }
            Caravan caravan = pawn.GetCaravan();
            if (caravan != null)
            {
                this.AffectPawns(pawn, caravan.pawns.InnerListForReading, true);
            }
        }
        protected virtual void AffectPawns(Pawn p, List<Pawn> pawns, bool inCaravan = false)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && this.ValidatePawn(p, pawn, inCaravan))
                {
                    AffectPawn(p, pawn);
                }
            }
        }
        public virtual void AffectSelf() { }
        public virtual void AffectPawn(Pawn self, Pawn pawn) { }
        public virtual bool ValidatePawn(Pawn self, Pawn p, bool inCaravan)
        {
            if (p == self)
            {
                return false;
            }
            if (!this.Props.affectsMechs && p.RaceProps.IsMechanoid)
            {
                return false;
            }
            if (!this.Props.affectsDrones && p.RaceProps.IsDrone)
            {
                return false;
            }
            if (!this.Props.affectsFleshies && p.RaceProps.IsFlesh)
            {
                return false;
            }
            if (!this.Props.affectsEntities && (p.RaceProps.IsAnomalyEntity || (this.Props.mutantsAreEntities && p.IsMutant)))
            {
                return false;
            }
            if (p.HostileTo(self))
            {
                if (!this.Props.affectsHostiles)
                {
                    return false;
                }
            }
            else if (!this.Props.affectsAllies)
            {
                return false;
            }
            if (inCaravan)
            {
                if (!this.Props.affectsOthersInCaravan)
                {
                    return false;
                }
            }
            else if (self.Spawned && p.Position.DistanceTo(self.Position) > this.FunctionalRange)
            {
                return false;
            }
            return true;
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (this.mote != null && !this.mote.Destroyed)
            {
                this.mote.Destroy(DestroyMode.Vanish);
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            if (this.Props.canToggleVisualization)
            {
                Scribe_Values.Look<AuraVisSetting>(ref this.visSetting, "AuraVisSetting", AuraVisSetting.Enabled, false);
                Scribe_Values.Look<string>(ref this.buttonLabel, "buttonLabel", this.Props.visLabel, false);
                Scribe_Values.Look<string>(ref this.buttonTooltip, "buttonTooltip", this.Props.visTooltip, false);
            }
        }
        public MoteThrownAttached_Aura mote;
        public AuraVisSetting visSetting = AuraVisSetting.Enabled;
        Texture2D uiIcon;
        string buttonLabel;
        string buttonTooltip;
        public float cachedRange;
    }
    public enum AuraVisSetting : short
    {
        Enabled,
        WhileDrafted = 8,
        WhileSelected = 16,
        Disabled = 24
    }
    //applies all the listed hediffs to whoever is affected. Has a virtual method HediffSeverity to determine what severity it should have
    public class HediffCompProperties_AuraHediff : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraHediff()
        {
            this.compClass = typeof(HediffComp_AuraHediff);
        }
        public List<HediffDef> hediffs;
    }
    public class HediffComp_AuraHediff : HediffComp_Aura
    {
        public new HediffCompProperties_AuraHediff Props
        {
            get
            {
                return (HediffCompProperties_AuraHediff)this.props;
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            foreach (HediffDef h in this.Props.hediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(h, this.Pawn, null);
                hediff.Severity = this.HediffSeverity(this.Pawn, h);
                this.parent.pawn.health.AddHediff(hediff, null);
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            foreach (HediffDef h in this.Props.hediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(h, pawn, null);
                hediff.Severity = this.HediffSeverity(pawn, h);
                pawn.health.AddHediff(hediff, null);
            }
        }
        public virtual float HediffSeverity(Pawn p, HediffDef h)
        {
            return h.initialSeverity;
        }
    }
    //applies all the listed memories to whoever is affected. Situational thoughts, lacking a duration, don't really work with this system. Remember, this basically works like WarCraft III auras, where we trust the inflicted condition to go away after a duration set by its own internal timer, unless reapplied
    public class HediffCompProperties_AuraThought : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraThought()
        {
            this.compClass = typeof(HediffComp_AuraThought);
        }
        public List<ThoughtDef> thoughts;
    }
    public class HediffComp_AuraThought : HediffComp_Aura
    {
        public new HediffCompProperties_AuraThought Props
        {
            get
            {
                return (HediffCompProperties_AuraThought)this.props;
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            if (this.parent.pawn.needs.mood != null && this.parent.pawn.needs.mood.thoughts != null)
            {
                foreach (ThoughtDef t in this.Props.thoughts)
                {
                    Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(t);
                    if (!thought.def.IsSocial)
                    {
                        this.parent.pawn.needs.mood.thoughts.memories.TryGainMemory(thought, null);
                    }
                }
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (pawn.needs.mood != null && pawn.needs.mood.thoughts != null)
            {
                foreach (ThoughtDef t in this.Props.thoughts)
                {
                    Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(t);
                    if (thought.def.IsSocial)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(thought, self);
                    }
                    else
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(thought, null);
                    }
                }
            }
        }
    }
    public class MoteThrownAttached_Aura : MoteThrown
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.link1.Linked)
            {
                drawVal = 2 * this.range;
                this.attacheeLastPosition = this.link1.LastDrawPos;
            }
            this.exactPosition += this.def.mote.attachedDrawOffset;
        }
        protected override void TimeInterval(float deltaTime)
        {
            deltaTime = 0.01f;
            switch (Find.TickManager.CurTimeSpeed)
            {
                case TimeSpeed.Fast:
                    deltaTime /= 3f;
                    break;
                case TimeSpeed.Superfast:
                    deltaTime /= 6f;
                    break;
                case TimeSpeed.Ultrafast:
                    deltaTime /= 15f;
                    break;
                default:
                    break;
            }
            base.TimeInterval(deltaTime);
            if (Rand.Value <= 0.99f)
            {
                if (dilating)
                {
                    drawVal += deltaTime;
                }
                else
                {
                    drawVal -= deltaTime;
                }
            }
            else
            {
                if (dilating)
                {
                    dilating = false;
                }
                else
                {
                    dilating = true;
                }
            }
            if (drawVal > (2 * this.range) + 0.5f)
            {
                drawVal = (2 * this.range) + 0.5f;
            }
            else if (drawVal < (2 * this.range) - 0.5f)
            {
                drawVal = (2 * this.range) - 0.5f;
            }
            this.Scale = drawVal;
        }
        protected override Vector3 NextExactPosition(float deltaTime)
        {
            Vector3 vector = base.NextExactPosition(deltaTime);
            if (this.link1.Linked)
            {
                bool flag = this.detachAfterTicks == -1 || Find.TickManager.TicksGame - this.spawnTick < this.detachAfterTicks;
                if (!this.link1.Target.ThingDestroyed && flag)
                {
                    this.link1.UpdateDrawPos();
                }
                Vector3 b = this.link1.LastDrawPos - this.attacheeLastPosition;
                vector += b;
                vector.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                this.attacheeLastPosition = this.link1.LastDrawPos;
            }
            return vector;
        }
        public float drawVal = 0.1f;
        public float range = 1f;
        private bool dilating = false;
        private Vector3 attacheeLastPosition = new Vector3(-1000f, -1000f, -1000f);
    }
}
