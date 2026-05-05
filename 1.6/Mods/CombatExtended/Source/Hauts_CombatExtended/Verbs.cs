using CombatExtended;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hauts_CombatExtended
{
    //unsurprisingly, Verb_AbilityShootDontMove does not work in CE. Surprisingly, it is still needed since CE's verbs inherit their OrderForceTarget from regular Verb. Here is the CE-compliant rendition
    public class Verb_AbilityShootCE_DontMove : Verb_AbilityShootCE, IAbilityVerb
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
}
