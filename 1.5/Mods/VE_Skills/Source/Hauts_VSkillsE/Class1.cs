using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using VSE.Passions;

namespace Hauts_VSkillsE
{
    [StaticConstructorOnStartup]
    public class Hauts_VSkillsE
    {
        private static readonly Type patchType = typeof(Hauts_VSkillsE);
        static Hauts_VSkillsE()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.vanillaskillsexpanded");
            harmony.Patch(AccessTools.Method(typeof(HediffComp_SkillAdjustment), nameof(HediffComp_SkillAdjustment.ForgettingSpeed)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsVSE_ForgettingSpeedPostfix)));
        }
        public static void HautsVSE_ForgettingSpeedPostfix(ref float __result, SkillRecord skill)
        {
            __result *= skill.ForgetRateFactor();
        }
    }
}
