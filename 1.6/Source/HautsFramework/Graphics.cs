using RimWorld;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    public class Graphic_PawnBodySilhouette_OkForAnimalsInBed : Graphic_PawnBodySilhouette
    {
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (thing is Mote mote)
            {
                Pawn pawn = mote.link1.Target.Thing as Pawn;
                if (pawn == null)
                {
                    Corpse corpse = mote.link1.Target.Thing as Corpse;
                    pawn = corpse.InnerPawn;
                }
                if (pawn.CurrentBed() != null && pawn.story == null)
                {
                    return;
                }
            }
            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }
    }
}
