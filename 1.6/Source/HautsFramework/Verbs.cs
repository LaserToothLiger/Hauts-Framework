using RimWorld;
using Verse;

namespace HautsFramework
{
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
}
