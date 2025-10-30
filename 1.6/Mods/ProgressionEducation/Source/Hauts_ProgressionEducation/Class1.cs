using HarmonyLib;
using HautsFramework;
using ProgressionEducation;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Hauts_ProgressionEducation
{
    [StaticConstructorOnStartup]
    public class Hauts_ProgressionEducation
    {
        private static readonly Type patchType = typeof(Hauts_ProgressionEducation);
        static Hauts_ProgressionEducation()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.progressioneducation");
            /*harmony.Patch(AccessTools.Method(typeof(StudyGroup), nameof(StudyGroup.CalculateProgressPerTick)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsCalculateProgressPerTickPostfix)));*/
            harmony.Patch(AccessTools.Method(typeof(ProficiencyClassLogic), nameof(ProficiencyClassLogic.CalculateTeacherScore)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsCalculateTeacherScorePostfix)));
            harmony.Patch(AccessTools.Method(typeof(SkillClassLogic), nameof(SkillClassLogic.CalculateTeacherScore)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsCalculateTeacherScorePostfix)));
            harmony.Patch(AccessTools.Method(typeof(DaycareClassLogic), nameof(DaycareClassLogic.ApplyTeachingTick)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsApplyTeachingTickPrefix)));
            harmony.Patch(AccessTools.Method(typeof(DaycareClassLogic), nameof(DaycareClassLogic.ApplyTeachingTick)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsApplyTeachingTickPostfix)));
        }
        /*public static void HautsCalculateProgressPerTickPostfix(ref float __result, StudyGroup __instance)
        {
            if (__instance.teacher != null)
            {
                __result *= __instance.teacher.GetStatValue(HautsDefOf.Hauts_InstructiveAbility);
            }
        }*/
        public static void HautsCalculateTeacherScorePostfix(ref float __result, Pawn teacher)
        {
            __result *= teacher.GetStatValue(HautsDefOf.Hauts_InstructiveAbility);
        }
        public static void HautsApplyTeachingTickPrefix(DaycareClassLogic __instance, Pawn student, JobDriver_Teach jobDriver, ref float __state)
        {
            SkillDef skillDef = jobDriver.taughtSkill;
            if (skillDef != null)
            {
                SkillRecord sr = student.skills.GetSkill(skillDef);
                if (!sr.TotallyDisabled)
                {
                    __state = sr.XpTotalEarned + sr.xpSinceLastLevel;
                }
                return;
            }
            __state = -1f;
        }
        public static void HautsApplyTeachingTickPostfix(DaycareClassLogic __instance, Pawn student, JobDriver_Teach jobDriver, float __state)
        {
            if (__state > 0f)
            {
                Pawn pawn = jobDriver.pawn;
                SkillDef skillDef = jobDriver.taughtSkill;
                if (skillDef != null)
                {
                    SkillRecord sr = student.skills.GetSkill(skillDef);
                    float instructiveAbilityOffset = pawn.GetStatValue(HautsDefOf.Hauts_InstructiveAbility) - 1f;
                    if (instructiveAbilityOffset != 0f)
                    {
                        float num = sr.XpTotalEarned + sr.xpSinceLastLevel - __state;
                        student.skills.Learn(skillDef, num * instructiveAbilityOffset, false, false);
                    }
                }
            }
        }
    }
}
