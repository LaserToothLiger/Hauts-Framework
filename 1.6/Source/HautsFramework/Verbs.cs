using RimWorld;
using Verse;
using Verse.AI;

namespace HautsFramework
{
    //a Harmony patch allows this kind of verb to be used even while threatened in melee
    public class Verb_MeleeShot : Verse.Verb_Shoot
    {
    }
    /*Derived from Verb_CastAbility. Provided their thinktree is set to use abilities on combat targets, and provided their target is a pawn or turret,
     * NPCs will cast this ability on themselves in combat (the target is redirected to self via a Harmony patch)*/
    public class Verb_CastAbilityCombatSelfBuff : RimWorld.Verb_CastAbility
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Pawn != null || target.Thing is Building_Turret)
            {
                return true;
            }
            return false;
        }
    }
    /*it's like Verb_AbilityShoot, except, you aren't forced to switch between the shooter repeatedly using the verb like a regular attack until the target is invalid (ai_IsWeapon = true in verb props)
     * or the shooter automatically deciding to reposition itself before using the verb (ai_IsWeapon = false) when ordered to use it.
     * While this is technically a thing you could do with Verb_LaunchProjectileStatic, launch proj static does not link to a causative ability the way that Verb_AbilityShoot does*/
    public class Verb_AbilityShootDontMove : Verb_AbilityShoot
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (this.verbProps.IsMeleeAttack)
            {
                Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                job.playerForced = true;
                if (target.Thing is Pawn pawn)
                {
                    job.killIncappedTarget = pawn.Downed;
                }
                this.CasterPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                return;
            }
            float num = this.verbProps.EffectiveMinRange(target, this.CasterPawn);
            if ((float)this.CasterPawn.Position.DistanceToSquared(target.Cell) < num * num && this.CasterPawn.Position.AdjacentTo8WayOrInside(target.Cell))
            {
                Messages.Message("MessageCantShootInMelee".Translate(), this.CasterPawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            Job job2 = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStatic);
            job2.verbToUse = this;
            job2.targetA = target;
            job2.endIfCantShootInMelee = true;
            this.CasterPawn.jobs.TryTakeOrderedJob(job2, new JobTag?(JobTag.Misc), false);
        }
    }
    /*as Verb_CastAbility, but blocked by any apparel that prevents ranged weapon use*/
    public class Verb_CastAbilityBlockedByBlocksRanged : Verb_CastAbility
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (this.CasterIsPawn && this.CasterPawn.apparel != null)
            {
                foreach (Apparel a in this.CasterPawn.apparel.WornApparel)
                {
                    if (!a.AllComps.NullOrEmpty())
                    {
                        foreach (ThingComp tc in a.AllComps)
                        {
                            if (tc is CompShield shield && shield.Props.blocksRangedWeapons)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return base.CanHitTargetFrom(root, targ);
        }
    }
}
