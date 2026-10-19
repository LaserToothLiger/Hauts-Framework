using BigAndSmall;
using HarmonyLib;
using HautsFramework;
using Verse;
using Verse.AI.Group;

namespace Hauts_BigAndSmallSapientAnimals
{
    [StaticConstructorOnStartup]
    public static class HVMI_BigAndSmall
    {
        private static readonly Type patchType = typeof(HVMI_BigAndSmall);
        static HVMI_BigAndSmall()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsdeepenedmemes.bigandsmall");
            harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.IsAnimalOrSapientAnimal)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_BAS_IsAnimalOrSapientAnimalPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.CanSapienateAnimal)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_BAS_CanSapienateAnimalPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.SapienateAnimal)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_BAS_SapienateAnimalPostfix)));
        }
        //makes these three tools work: 1) a boolean for detecting if a pawn is an animal OR a sapient animal, 2) determine if a sapient version of a given pawn exists, 3) make a pawn into its sapient version (basically a copy of sapienator code)
        public static void Hauts_BAS_IsAnimalOrSapientAnimalPostfix(Pawn pawn, ref bool __result)
        {
            if (!__result)
            {
                __result = pawn.def.IsHumanlikeAnimal() && pawn.RaceProps.IsFlesh;
            }
        }
        public static void Hauts_BAS_CanSapienateAnimalPostfix(Pawn animal, ref bool __result)
        {
            __result = HumanlikeAnimalGenerator.reverseLookupHumanlikeAnimals.ContainsKey(animal.def);
        }
        public static void Hauts_BAS_SapienateAnimalPostfix(Pawn animal)
        {
            if (HumanlikeAnimalGenerator.reverseLookupHumanlikeAnimals.ContainsKey(animal.def))
            {
                Lord lord = animal.lord;
                Pawn newPawn = animal.SwapAnimalToSapientVersion();
                if (lord != null)
                {
                    if (newPawn.lord != null)
                    {
                        newPawn.lord.RemovePawn(newPawn);
                    }
                    lord.AddPawn(newPawn);
                }
                return;
            }
            Log.Warning(string.Format("Tried to swap {0} to a sapient version, but no sapient version found for {1}.", animal.Name, animal.def.defName));
        }
    }
}
