using HarmonyLib;
using HautsFramework;
using RimWorld;
using Verse;
using VFEInsectoids;

namespace HautsFramework_VFEInsectoids2
{
    [StaticConstructorOnStartup]
    public class HautsFramework_VFEInsectoids2
    {
        private static readonly Type patchType = typeof(HautsFramework_VFEInsectoids2);
        static HautsFramework_VFEInsectoids2()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.insectoids2");
            harmony.Patch(AccessTools.Method(typeof(Stats_AbilityRangesUtility), nameof(Stats_AbilityRangesUtility.IsLeapVerb)),
                           postfix: new HarmonyMethod(patchType, nameof(HVFEI2IsLeapVerbPostfix)));
            harmony.Patch(AccessTools.Property(typeof(Verb_CastAbilityJumpUnrestricted), nameof(Verb_CastAbilityJumpUnrestricted.EffectiveRange)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HVFEI2_VCAJU_EffectiveRangePostfix)));
        }
        public static void HVFEI2IsLeapVerbPostfix(ref bool __result, Verb verb)
        {
            if (verb is Verb_CastAbilityJumpUnrestricted)
            {
                __result = true;
            }
        }
        public static void HVFEI2_VCAJU_EffectiveRangePostfix(ref float __result, Verb_CastAbilityJumpUnrestricted __instance)
        {
            if (__instance.CasterPawn != null)
            {
                __result *= __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_JumpRangeFactor);
            }
        }
    }
}
