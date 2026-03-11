using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    /*As the Aura Hediff Comp (see HediffComps_Aura.cs), with some small differences:
     * no “AffectSelf” method - instead has a Pawn creator field in case you want the emitter to have a unique effect on its creator (if any);
     *    DoOnTrigger() is where you'd write an effect into a derivative, which will occur whenever the emitter would apply the aura’s effects;
     *    and affectCreator determines whether or not the creator should be exempt from the effects.
     *    Graphic for the visual area of effect currently can’t be changed from the shield bubble texture
     *    ShouldAffectPawn enables children of this comp to further specify what kinds of pawns can be affected. The base version checks for whether each pawn has enough of the minimum stat (if any) and is of the appropriate faction-relation-to-its-creator.
     *    tickPeriodicity is called periodicity here because my memory is blah. Despite the different name, it functions the same as in the Aura hediff comp.*/
    public class CompProperties_AuraEmitter : CompProperties
    {
        public CompProperties_AuraEmitter()
        {
            this.compClass = typeof(CompAuraEmitter);
        }
        public float range;
        public int periodicity = 150;
        public StatDef rangeModifier = null;
        public float maxRangeModifier = 1f;
        public float minRangeModifier = 1f;
        public bool affectEnemies = true;
        public bool affectNeutrals = true;
        public bool affectOwnFaction = true;
        public bool affectCreator = true;
        public StatDef requiredStat = null;
        public float minStat = 1E-45f;
        public bool scanByPawnListerNotByGrid = true;
        public Color color;
    }
    public class CompAuraEmitter : ThingComp
    {
        public CompProperties_AuraEmitter Props
        {
            get
            {
                return (CompProperties_AuraEmitter)this.props;
            }
        }
        public virtual float FunctionalRange
        {
            get
            {
                if (this.Props.rangeModifier != null)
                {
                    return this.Props.range * Math.Min(this.Props.maxRangeModifier, Math.Max(this.Props.minRangeModifier, this.parent.GetStatValue(this.Props.rangeModifier)));
                }
                return this.Props.range * this.radiusScalar;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (this.creator != null)
            {
                this.faction = this.creator.Faction;
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(this.Props.periodicity, delta) && this.parent.SpawnedOrAnyParentSpawned)
            {
                this.DoOnTrigger();
                if (this.Props.scanByPawnListerNotByGrid)
                {
                    foreach (Pawn p in this.parent.MapHeld.mapPawns.AllPawnsSpawned)
                    {
                        if (p.Position.DistanceTo(this.parent.PositionHeld) <= this.FunctionalRange && this.ShouldAffectPawn(p))
                        {
                            this.AffectPawn(p);
                        }
                    }
                }
                else
                {
                    foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.parent.PositionHeld, this.parent.MapHeld, this.FunctionalRange, true).OfType<Pawn>().Distinct<Pawn>())
                    {
                        if (this.ShouldAffectPawn(p))
                        {
                            this.AffectPawn(p);
                        }
                    }
                }
            }
        }
        public virtual void DoOnTrigger()
        {

        }
        public virtual bool ShouldAffectPawn(Pawn pawn)
        {
            if ((this.Props.requiredStat == null || pawn.GetStatValue(this.Props.requiredStat) >= this.Props.minStat))
            {
                if (pawn == this.creator && this.Props.affectCreator)
                {
                    return true;
                }
                if (this.faction == null)
                {
                    return true;
                }
                else if (pawn.Faction == null && this.Props.affectNeutrals)
                {
                    return true;
                }
                else if (this.Props.affectEnemies && pawn.Faction.HostileTo(this.faction) || (this.Props.affectOwnFaction && pawn.Faction == this.faction))
                {
                    return true;
                }
            }
            return false;
        }
        public virtual void AffectPawn(Pawn pawn) { }
        public override void PostDraw()
        {
            base.PostDraw();
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(this.parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverheadLow), Quaternion.AngleAxis(0f, Vector3.up), Vector3.one * this.FunctionalRange * 2f), MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, this.Props.color), 0);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.radiusScalar, "radiusScalar", 1f, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_References.Look<Pawn>(ref this.creator, "creator", false);
        }
        public float radiusScalar = 1f;
        public Faction faction;
        public Pawn creator;
    }
    //functional example child of AuraEmitter. Applies all the listed hediffs to affected pawns
    public class CompProperties_AuraEmitterHediff : CompProperties_AuraEmitter
    {
        public CompProperties_AuraEmitterHediff()
        {
            this.compClass = typeof(CompAuraEmitterHediff);
        }
        public List<HediffDef> hediffs;
    }
    public class CompAuraEmitterHediff : CompAuraEmitter
    {
        public new CompProperties_AuraEmitterHediff Props
        {
            get
            {
                return (CompProperties_AuraEmitterHediff)this.props;
            }
        }
        public override void AffectPawn(Pawn pawn)
        {
            base.AffectPawn(pawn);
            foreach (HediffDef h in this.Props.hediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(h, pawn, null);
                pawn.health.AddHediff(hediff, null);
            }
        }
    }
    /*1.5 made it so non-apparel items can't have charges. unfortunately, I had already made HAT's personality neuroformatter in 1.4 and was quite happy with its implementation, so I remade that capacity.
     * most fields are identical to CompProperties_ApparelVerbOwner or its Charged child
     * Also has the following unique field: priceScalesByRemainingCharges multiplies the market value of the item by its remaining charges, and divides it by its max charges.
     * InitialCharges() determines the number of charges the item starts with. Defaults to max charges. This is how PNF max charge mod setting for HAT works.*/
    public class CompProperties_ItemCharged : CompProperties
    {
        public NamedArgument ChargeNounArgument
        {
            get
            {
                return this.chargeNoun.Named("CHARGENOUN");
            }
        }
        public CompProperties_ItemCharged()
        {
            this.compClass = typeof(Comp_ItemCharged);
        }
        public int maxCharges;
        public bool destroyOnEmpty;
        [MustTranslate]
        public string chargeNoun = "charge";
        public KeyBindingDef hotKey;
        public bool displayGizmoWhileUndrafted = true;
        public bool displayGizmoWhileDrafted = true;
        public bool displayChargesInLabel = true;
        public bool priceScalesByRemainingCharges = true;
    }
    public class Comp_ItemCharged : ThingComp
    {
        public CompProperties_ItemCharged Props
        {
            get
            {
                return this.props as CompProperties_ItemCharged;
            }
        }
        public int RemainingCharges
        {
            get
            {
                return this.remainingCharges;
            }
        }
        public int MaxCharges
        {
            get
            {
                return this.Props.maxCharges;
            }
        }
        public string LabelRemaining
        {
            get
            {
                return string.Format("{0} / {1}", this.RemainingCharges, this.MaxCharges);
            }
        }
        public virtual string GizmoExtraLabel
        {
            get
            {
                return this.LabelRemaining;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.remainingCharges = this.InitialCharges();
        }
        public virtual int InitialCharges()
        {
            return this.MaxCharges;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.remainingCharges, "remainingCharges", -999, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.remainingCharges == -999)
            {
                this.remainingCharges = this.MaxCharges;
            }
        }
        public override string TransformLabel(string label)
        {
            if (this.Props.displayChargesInLabel)
            {
                return base.TransformLabel(label) + " (" + this.remainingCharges + "/" + this.Props.maxCharges + ")";
            }
            return base.TransformLabel(label);
        }
        public override string CompInspectStringExtra()
        {
            return "ChargesRemaining".Translate(this.Props.ChargeNounArgument) + ": " + this.LabelRemaining;
        }
        public virtual void UsedOnce()
        {
            if (this.remainingCharges > 0)
            {
                this.remainingCharges--;
            }
            if (this.Props.destroyOnEmpty && this.remainingCharges == 0 && !this.parent.Destroyed)
            {
                this.parent.Destroy(DestroyMode.Vanish);
            }
        }
        public virtual bool CanBeUsed(out string reason)
        {
            reason = "";
            if (this.parent.MapHeld == null)
            {
                return false;
            }
            return true;
        }
        protected int remainingCharges;
    }
    //applied via xpath to MarketValue stat. makes priceScalesByRemainingCharges work, so less charged items are less valuable. I'm not buying your nasty half-drunk soda for full price
    public class StatPart_ItemCharged : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            StatPart_ItemCharged.TransformAndExplain(req, ref val, null);
        }
        public override string ExplanationPart(StatRequest req)
        {
            float num = 1f;
            StringBuilder stringBuilder = new StringBuilder();
            StatPart_ItemCharged.TransformAndExplain(req, ref num, stringBuilder);
            return stringBuilder.ToString().TrimEndNewlines();
        }
        private static void TransformAndExplain(StatRequest req, ref float val, StringBuilder explanation)
        {
            Thing thing = req.Thing;
            if (thing != null)
            {
                Comp_ItemCharged cic = thing.TryGetComp<Comp_ItemCharged>();
                if (cic != null && cic.Props.priceScalesByRemainingCharges)
                {
                    float num3 = ((float)cic.RemainingCharges / (float)cic.MaxCharges);
                    if (explanation != null)
                    {
                        explanation.AppendLine("StatsReport_ReloadRemainingChargesMultipler".Translate(cic.Props.ChargeNounArgument, cic.LabelRemaining) + ": x" + num3.ToStringPercent());
                    }
                    val *= num3;
                }
            }
            if (val < 0f)
            {
                val = 0f;
            }
        }
    }
    //it's CompColorable, but the color is set every 15 ticks to its Faction's color (if any), multiplied by its colorFactor (so you could make it darker or brighter if u wanted)
    public class CompProperties_FactionColored : CompProperties
    {
        public CompProperties_FactionColored()
        {
            this.compClass = typeof(CompFactionColored);
        }
        public float colorFactor = -1f;
    }
    public class CompFactionColored : CompColorable
    {
        public CompProperties_FactionColored Props
        {
            get
            {
                return (CompProperties_FactionColored)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.SetTeamColor();
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(15, delta))
            {
                this.SetTeamColor();
            }
        }
        public void SetTeamColor()
        {
            Color newColor = new Color(1f, 1f, 1f);
            if (this.parent.Faction != null)
            {
                newColor = this.parent.Faction.Color;
            }
            if (this.Props.colorFactor >= 0f)
            {
                newColor.r *= this.Props.colorFactor;
                newColor.g *= this.Props.colorFactor;
                newColor.b *= this.Props.colorFactor;
            }
            this.teamColor = newColor;
            this.SetColor(this.teamColor);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<Color>(ref this.teamColor, "teamColor", Color.white, false);
        }
        public Color teamColor;
    }
}
