using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    /*abilities with this gizmoClass in caravans, and can be pressed to apply effects to self
     therefore, avoid giving this to any abilities whose comps assume or require the target to be spawned, or the error log will be very unhappy with you
    FleckOnTarget is the only safe such comp, because I specifically excluded it*/
    public class Command_AbilityCanBuffSelfOnCaravan : RimWorld.Command_Ability
    {
        public Command_AbilityCanBuffSelfOnCaravan(RimWorld.Ability ability, Pawn pawn) : base(ability, pawn)
        {
        }
        public override void ProcessInput(Event ev)
        {
            if (this.Pawn.IsCaravanMember())
            {
                this.Ability.Activate(this.Pawn);
                foreach (CompAbilityEffect compAbilityEffect in this.Ability.EffectComps)
                {
                    if (!(compAbilityEffect is CompAbilityEffect_FleckOnTarget))
                    {
                        compAbilityEffect.Apply(this.Pawn, null);
                    }
                }
            }
            base.ProcessInput(ev);
        }
    }
    //the above does not work with Psycasts, since they declare their own gizmoClass. You can, however, use this derivative of Psycast (and this other Command) to bypass that limitation
    public class Psycast_CanBuffSelfOnCaravan : RimWorld.Psycast
    {
        public Psycast_CanBuffSelfOnCaravan(Pawn pawn)
            : base(pawn)
        {
        }
        public Psycast_CanBuffSelfOnCaravan(Pawn pawn, RimWorld.AbilityDef def)
            : base(pawn, def)
        {
        }
        public override IEnumerable<Command> GetGizmos()
        {
            if (!ModLister.RoyaltyInstalled)
            {
                yield break;
            }
            if (this.gizmo == null)
            {
                this.gizmo = new Command_PsycastCanBuffSelfOnCaravan(this, this.pawn);
            }
            yield return this.gizmo;
            yield break;
        }
    }
    public class Command_PsycastCanBuffSelfOnCaravan : Command_Psycast
    {
        public Command_PsycastCanBuffSelfOnCaravan(Psycast ability, Pawn pawn) : base(ability, pawn)
        {
        }
        public override void ProcessInput(Event ev)
        {
            if (this.Pawn.IsCaravanMember())
            {
                this.Ability.Activate(this.Pawn);
                foreach (CompAbilityEffect compAbilityEffect in this.Ability.EffectComps)
                {
                    if (!(compAbilityEffect is CompAbilityEffect_FleckOnTarget))
                    {
                        compAbilityEffect.Apply(this.Pawn, null);
                    }
                }
            }
            base.ProcessInput(ev);
        }
    }
}
