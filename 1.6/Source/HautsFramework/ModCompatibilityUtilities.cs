using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    public static class ModCompatibilityUtility
    {
        /*Big and Small Framework (although these are basically for the content you'd associate with Big and Small - Sapient Animals, the relevant code is actually all in the B&S Framework)
         * this method ordinarily just returns whether pawn is an animal. The subdirectory for Big and Small - Sapient Animals patches this to also return true if the pawn is a sapient, fleshy pawn*/
        public static bool IsAnimalOrSapientAnimal(Pawn pawn)
        {
            return pawn.IsAnimal;
        }
        //determines if the pawn's def has a sapient equivalent
        public static bool CanSapienateAnimal(Pawn animal)
        {
            return false;
        }
        //patched to make the animal sapient, and if it had been part of a Lord, to give the resultant sapient pawn back to that Lord right afterward
        public static void SapienateAnimal(Pawn animal)
        {
        }
        //Combat Extended integration: cached bool if CE is running or not
        public static bool CombatIsExtended()
        {
            return ModCompatibilityUtility.combatIsExtended;
        }
        //These COaNN things need to be moved to HAT eventually
        //COaNN integration: these are specifically for the brainwash item. HAT uses the first two to make it awaken pawns if it removes a Latent Psychic trait. IODTs are other reasons for brainwash not to remove a trait (aside from being ETE)
        public static bool COaNN_TraitReset_ShouldDoBonusEffect(TraitDef def)
        {
            return false;
        }
        public static void COaNN_TraitReset_BonusEffects(Pawn user, List<TraitDef> defs)
        {

        }
        //fantasy flavour pack: cached bool if you have RPG Adventure Flavour Pack or its 1.6 fork running
        public static bool IsHighFantasy()
        {
            return ModCompatibilityUtility.isHighFantasy;
        }
        //HAT integration: check if the pawn has a woke trait or gene. HAT has a Harmony patch to make this work. Any mod using this framework can therefore just reference this without needing a whole dedicated subdirectory just to check if a pawn is woke
        public static bool IsAwakenedPsychic(Pawn pawn)
        {
            return false;
        }
        /*rim languages integration: pawn is the learner, other is the pawn whose language is being learned. If pawn isn't player faction, doesn't count because why would a rando's learning impact your colony's knowledge base.
         * *If other is null, it just does a random language. power is the base amount, multiplied by the pawn's language learning ability (if any)*/
        public static void LearnLanguage(Pawn pawn, Pawn other, float power)
        {

        }
        //vpe tools. self-explanatory checks for the first two
        public static bool IsVPEPsycast(VEF.Abilities.Ability ability)
        {
            return false;
        }
        public static int GetVPEPsycastLevel(VEF.Abilities.Ability ability)
        {
            return 0;
        }
        //vpe - returns GetEntropyUsedByPawn, assuming of course that this is a psycast we're talking about. otherwise it returns 0.
        public static float GetVPEEntropyCost(VEF.Abilities.Ability ability)
        {
            return 0f;
        }
        //vpe - returns GetPsyfocusUsedByPawn, as modified by VPE_PsyfocusCostFactor and tier one psycast cost offset. If not a psycast, returns 0.
        public static float GetVPEPsyfocusCost(VEF.Abilities.Ability ability)
        {
            return 0f;
        }
        //vpe - unlocks targeted psycast, and if its path is not yet unlocked, unlocks that too. Pawn must be a psycaster and ability must be a psycast, obviously.
        public static void VPEUnlockAbility(Pawn pawn, VEF.Abilities.AbilityDef abilityDef)
        {

        }
        //vpe - if for some reason you need to give the points and experience of one pawn's Hediff_PsycastAbilities to another (copyFrom to setFor), you can use this.
        public static void VPESetSkillPointsAndExperienceTo(Pawn setFor, Pawn copyFrom)
        {

        }
        /*vgravshipe - adds gravdata to the currently selected gravship research project. Power is the base amount, multiplied by the researcher's usual factor for gravship research speed or whatever it's called.
         * Returns false if you don't have a selected project*/
        public static bool AddGravdata(Pawn researcher, float power)
        {
            return false;
        }
        public static bool isHighFantasy;
        public static bool combatIsExtended;
    }
}
