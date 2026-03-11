using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    /*DME that you apply to a VEF ability if you want its range to be affected by jump and/or skip range factor.
     * You don't need this for good ol' RimWorld.Ability stuff - see the first two utility methods below*/
    public class AbilityStatEffecters : DefModExtension
    {
        public AbilityStatEffecters()
        {
        }
        public bool skip = false;
        public bool leap = false;
    }
    /*replacement for conventional CompAbilityEffect_FireSpew that makes the actual spew effect scale w/ spew range factor, not just the targeting range. (other kinds of spray abilities don't need special code to scale correctly, it's just this comp)
     * xpath is used to substitute this for the ordinary version.
     * could this have been a transpiler? Probably, I've been told transpilers are roughly as capable as God. I also recall that the VE team could not read one of their ex-modders' transpilers and so had to recreate an entire mod from the ground up,
     * that I cannot fucking read any of the transpilers on the example page of HAR's harmony patches provided on the RimWorld wiki, and that apparently poor use of a transpiler can be really dangerous. So this might not be a GREAT solution,
     * but it IS better than me bungling my way around a transpiler.*/
    public class CompProperties_AbilityFireSpewScalable : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityFireSpewScalable()
        {
            this.compClass = typeof(CompAbilityEffect_FireSpewScalable);
        }
        public float range;
        public float lineWidthEnd;
        public ThingDef filthDef;
        public float filthChance = 1f;
        public int damAmount = -1;
        public EffecterDef effecterDef;
        public bool canHitFilledCells;
    }
    public class CompAbilityEffect_FireSpewScalable : CompAbilityEffect
    {
        private new CompProperties_AbilityFireSpewScalable Props
        {
            get
            {
                return (CompProperties_AbilityFireSpewScalable)this.props;
            }
        }
        private Pawn Pawn
        {
            get
            {
                return this.parent.pawn;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            IntVec3 cell = target.Cell;
            Map mapHeld = this.parent.pawn.MapHeld;
            float num = 0f;
            DamageDef flame = DamageDefOf.Flame;
            Thing pawn = this.Pawn;
            GenExplosion.DoExplosion(cell, mapHeld, num, flame, pawn, this.Props.damAmount, -1f, null, null, null, null, this.Props.filthDef, this.Props.filthChance, 1, null, null, 255, false, null, 0f, 1, 1f, false, null, null, null, false, 0.6f, 0f, false, null, 1f, this.parent.verb.verbProps.flammabilityAttachFireChanceCurve, this.AffectedCells(target), null, null);
            base.Apply(target, dest);
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (this.Props.effecterDef != null)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                    {
                        this.parent.AddEffecterToMaintain(this.Props.effecterDef.Spawn(this.parent.pawn.Position, a.Cell, this.parent.pawn.Map, 1f), this.Pawn.Position, a.Cell, 17, this.Pawn.MapHeld);
                    },
                    ticksAwayFromCast = 17
                };
            }
            yield break;
        }
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(this.AffectedCells(target));
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (this.Pawn.Faction != null)
            {
                foreach (IntVec3 intVec in this.AffectedCells(target))
                {
                    List<Thing> thingList = intVec.GetThingList(this.Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == this.Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }
        private float Range
        {
            get
            {
                return this.Props.range * (this.parent.def.HasModExtension<Hauts_SpewAbility>() ? this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) : 1f);
            }
        }
        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            this.tmpCells.Clear();
            Vector3 vector = this.Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 intVec = target.Cell.ClampInsideMap(this.Pawn.Map);
            if (this.Pawn.Position == intVec)
            {
                return this.tmpCells;
            }
            float lengthHorizontal = (intVec - this.Pawn.Position).LengthHorizontal;
            float num = (float)(intVec.x - this.Pawn.Position.x) / lengthHorizontal;
            float num2 = (float)(intVec.z - this.Pawn.Position.z) / lengthHorizontal;
            intVec.x = Mathf.RoundToInt((float)this.Pawn.Position.x + num * this.Range);
            intVec.z = Mathf.RoundToInt((float)this.Pawn.Position.z + num2 * this.Range);
            float num3 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
            float num4 = this.Props.lineWidthEnd * ((this.parent.def.HasModExtension<Hauts_SpewAbility>() && ModsConfig.BiotechActive) ? (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) / (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) > 1.5f ? 1.5f : 1f)) : 1f) / 2f;
            float num5 = Mathf.Sqrt(Mathf.Pow((intVec - this.Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num4, 2f));
            float num6 = 57.29578f * Mathf.Asin(num4 / num5);
            int num7 = GenRadial.NumCellsInRadius(this.Range);
            for (int i = 0; i < num7; i++)
            {
                IntVec3 intVec2 = this.Pawn.Position + GenRadial.RadialPattern[i];
                if (this.CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), num3)) <= num6)
                {
                    this.tmpCells.Add(intVec2);
                }
            }
            List<IntVec3> list = GenSight.BresenhamCellsBetween(this.Pawn.Position, intVec);
            for (int j = 0; j < list.Count; j++)
            {
                IntVec3 intVec3 = list[j];
                if (!this.tmpCells.Contains(intVec3) && this.CanUseCell(intVec3))
                {
                    this.tmpCells.Add(intVec3);
                }
            }
            return this.tmpCells;
        }
        [CompilerGenerated]
        private bool CanUseCell(IntVec3 c)
        {
            ShootLine shootLine;
            return c.InBounds(this.Pawn.Map) && !(c == this.Pawn.Position) && (this.Props.canHitFilledCells || !c.Filled(this.Pawn.Map)) && c.InHorDistOf(this.Pawn.Position, this.Range) && this.parent.verb.TryFindShootLineFromTo(this.parent.pawn.Position, c, out shootLine, false);
        }

        // Token: 0x04004219 RID: 16921
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();
    }
    /*DME that you apply to RimWorld.Abilities to make spew range factor affect their ranges*/
    public class Hauts_SpewAbility : DefModExtension
    {
        public Hauts_SpewAbility()
        {
        }
    }
    public static class Stats_AbilityRangesUtility
    {
        //abilities of Royalty's Skip category are, in fact, skip abilities. WHOAH.
        public static bool IsSkipAbility(RimWorld.AbilityDef ability)
        {
            return ModsConfig.RoyaltyActive && ability.category == DefDatabase<AbilityCategoryDef>.GetNamed("Skip");
        }
        //have you got the DME
        public static bool IsSpewAbility(RimWorld.AbilityDef ability)
        {
            return ability.HasModExtension<Hauts_SpewAbility>();
        }
        //are you a jumping verb? If you are, congratulations, you're jump range factor eligible.
        public static bool IsLeapVerb(Verb verb)
        {
            return verb is Verb_CastAbilityJump || verb is Verb_Jump;
        }
        //AbilityStatEffecters' effect here
        public static bool IsLeapAbility(VEF.Abilities.AbilityDef abilityDef)
        {
            if (abilityDef.HasModExtension<AbilityStatEffecters>() && abilityDef.GetModExtension<AbilityStatEffecters>().leap)
            {
                return true;
            }
            return false;
        }
        public static bool IsSkipAbility(VEF.Abilities.AbilityDef abilityDef)
        {
            if (abilityDef.HasModExtension<AbilityStatEffecters>() && abilityDef.GetModExtension<AbilityStatEffecters>().skip)
            {
                return true;
            }
            return false;
        }
        //a Harmony patch uses this to redraw the range of Skip or Spew abilities, destructively prefixing what would be the ordinary draw attempt. This is not called if the relevant stat is 1f because there would obviously be no change.
        public static void DrawBoostedAbilityRange(RimWorld.Verb_CastAbility ability, LocalTargetInfo target, StatDef stat)
        {

            if (ability.verbProps.range > 0f && Find.CurrentMap != null && !ability.verbProps.IsMeleeAttack && ability.verbProps.targetable)
            {
                VerbProperties verbProps = ability.verbProps;
                float num = verbProps.EffectiveMinRange(true);
                if (num > 0f && num < GenRadial.MaxRadialPatternRadius)
                {
                    GenDraw.DrawRadiusRing(ability.caster.Position, num);
                }
                float newRange = verbProps.range * ability.CasterPawn.GetStatValue(stat);
                if (newRange < (float)(Find.CurrentMap.Size.x + Find.CurrentMap.Size.z) && newRange < GenRadial.MaxRadialPatternRadius)
                {
                    Func<IntVec3, bool> predicate = null;
                    if (verbProps.drawHighlightWithLineOfSight)
                    {
                        predicate = (IntVec3 c) => GenSight.LineOfSight(ability.caster.Position, c, Find.CurrentMap);
                    }
                    GenDraw.DrawRadiusRing(ability.caster.Position, newRange, Color.white, predicate);
                }
            }
            if (ability.CanHitTarget(target) && ability.IsApplicableTo(target, false))
            {
                if (ability.ability.def.HasAreaOfEffect)
                {
                    if (target.IsValid)
                    {
                        GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
                        GenDraw.DrawRadiusRing(target.Cell, ability.ability.def.EffectRadius, RimWorld.Verb_CastAbility.RadiusHighlightColor, null);
                    }
                }
                else
                {
                    GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
                }
            }
            if (target.IsValid)
            {
                ability.ability.DrawEffectPreviews(target);
            }
        }
    }
}
