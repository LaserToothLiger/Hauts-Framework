using RimWorld;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    /*as Vanilla Expanded Framework's "HediffComp_Mote", except it does not throw exceptions when the pawn is not spawned.
     * Derivatives can specify other conditions in which the mote should be disabled via DisableMote, or play with its size via Scale
     * validRange: if the max of this field is non-negative, the mote disappears if the hediff’s severity exceeds this field’s bounds
     * scaleWithBodySize: multiplies the mote’s size by the pawn’s body size
     * DisableMote: if this returns true, the mote disappears (but not the hediff)*/
    public class HediffCompProperties_MoteConditional : HediffCompProperties
    {
        public HediffCompProperties_MoteConditional()
        {
            this.compClass = typeof(HediffComp_MoteConditional);
        }
        public ThingDef mote;
        public float scale;
        public FloatRange validRange = new FloatRange(-1f);
        public bool scaleWithBodySize = true;
    }
    public class HediffComp_MoteConditional : HediffComp
    {
        public HediffCompProperties_MoteConditional Props
        {
            get
            {
                return this.props as HediffCompProperties_MoteConditional;
            }
        }
        public virtual bool DisableMote()
        {
            return false;
        }
        public virtual float Scale
        {
            get
            {
                return this.Props.scale * (this.Props.scaleWithBodySize ? this.Pawn.BodySize : 1f);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.Spawned && (this.Props.validRange.max < 0f || (this.parent.Severity >= this.Props.validRange.min && this.parent.Severity <= this.Props.validRange.max)))
            {
                if (!this.DisableMote())
                {
                    if (this.mote == null || this.mote.Destroyed)
                    {
                        this.mote = MoteMaker.MakeAttachedOverlay(base.Pawn, this.Props.mote, Vector3.zero, this.Scale, -1f);
                        if (this.mote is MoteConditionalText mct)
                        {
                            mct.UpdateText();
                        }
                        if (this.Pawn.IsHashIntervalTick(10))
                        {
                            this.mote.link1.UpdateDrawPos();
                        }
                    }
                    else
                    {
                        this.mote.Maintain();
                    }
                    if (this.Pawn.IsHashIntervalTick(250))
                    {
                        this.mote.Scale = this.Scale;
                    }
                }
                else if (this.mote != null && !this.mote.Destroyed)
                {
                    this.mote.Destroy();
                }
            }
            else if (this.mote != null && !this.mote.Destroyed)
            {
                this.mote.Destroy(DestroyMode.Vanish);
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (this.mote != null && !this.mote.Destroyed)
            {
                this.mote.Destroy(DestroyMode.Vanish);
            }
        }
        public Mote mote;
    }
    /*derivative of the above, intended to be paired with HediffComp_DamageNegationShield to provide the visual representation of a shield's energy
     * its size will scale between minDrawFactor and maxDrawFactor based on its energy
     * randomRotation: randomizes the mote's rotation every tick. This is almost like how shield belt shields look, except it's dependent on in-game time rather than real time (shield belt graphic works in real time, right?)*/
    public class HediffCompProperties_MoteConditionalShield : HediffCompProperties_MoteConditional
    {
        public HediffCompProperties_MoteConditionalShield()
        {
            this.compClass = typeof(HediffComp_MoteConditionalShield);
        }
        public float minDrawFactor = 1.2f;
        public float maxDrawFactor = 1.55f;
        public bool randomRotation = true;
    }
    public class HediffComp_MoteConditionalShield : HediffComp_MoteConditional
    {
        public new HediffCompProperties_MoteConditionalShield Props
        {
            get
            {
                return this.props as HediffCompProperties_MoteConditionalShield;
            }
        }
        public float MaxEnergy
        {
            get
            {
                HediffComp_DamageNegationShield hcdns = this.parent.TryGetComp<HediffComp_DamageNegationShield>();
                if (hcdns != null)
                {
                    return hcdns.MaxEnergy;
                }
                return 1f;
            }
        }
        public override float Scale
        {
            get
            {
                return base.Scale * Mathf.Lerp(this.Props.minDrawFactor, this.Props.maxDrawFactor, (this.parent.Severity - this.Props.validRange.min) / this.MaxEnergy);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.mote != null)
            {
                this.mote.Scale = this.Scale;
                if (this.Props.randomRotation)
                {
                    this.mote.exactRotation = (float)Rand.Range(0, 360);
                }
            }
        }
    }
    /*another derivative which displays floating text*/
    public class MoteConditionalText : MoteAttached
    {
        public override void DrawGUIOverlay()
        {
            Color color = new Color(this.def.graphicData.color.r, this.def.graphicData.color.g, this.def.graphicData.color.b);
            GenMapUI.DrawText(new Vector2(this.exactPosition.x, this.exactPosition.z), this.text, color);
        }
        public void UpdateText()
        {
            this.text = this.TextString;
        }
        public virtual string TextString
        {
            get
            {
                return " ";
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.text, "text", "1_1", false);
        }
        public string text = " ";
    }
}
