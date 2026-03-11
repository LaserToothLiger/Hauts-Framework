using RimWorld;
using System;
using Verse;

namespace HautsFramework
{
    /*damage type that "ignores" IncomingDamageFactor below 1 by multiplying itself to match. Since you obv can't scale for infinity,
     it stops far shy of that limit at 100x vs <=1% damage taken. Used by skipfrag damage def*/
    public class DamageWorker_AddInjurySkip : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (thing is Pawn pawn)
            {
                dinfo.SetAmount(dinfo.Amount / Math.Max(0.01f, pawn.GetStatValue(StatDefOf.IncomingDamageFactor)));
            }
            return base.Apply(dinfo, thing);
        }
    }
}
