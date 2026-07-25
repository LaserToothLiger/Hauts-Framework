using HarmonyLib;
using HautsFramework;
using ProgressionEducation;
using ProgressionTherapy;
using RimWorld;
using System;
using Verse;

namespace Hauts_Therapy
{
    [StaticConstructorOnStartup]
    public class Hauts_Therapy
    {
        private static readonly Type patchType = typeof(Hauts_Therapy);
        static Hauts_Therapy()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.progressiontherapy");
            harmony.Patch(AccessTools.Method(typeof(TherapyClassLogic), nameof(TherapyClassLogic.CalculateTeacherScore)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsCalculateTeacherScorePostfix)));
        }
        //makes the instructive ability stat scale the results of PE's new ways of teaching people
        public static void HautsCalculateTeacherScorePostfix(ref float __result, Pawn teacher)
        {
            __result *= teacher.GetStatValue(HautsDefOf.Hauts_InstructiveAbility);
        }
    }
}
