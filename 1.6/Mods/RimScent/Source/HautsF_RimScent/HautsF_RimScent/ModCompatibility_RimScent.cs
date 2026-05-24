using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using Verse;

namespace HautsF_RimScent
{
    [StaticConstructorOnStartup]
    public static class HautsF_RimScent
    {
        private static readonly Type patchType = typeof(HautsF_RimScent);
        static HautsF_RimScent()
        {
            HautsF_RimScent.rimScentCapacity = DefDatabase<PawnCapacityDef>.GetNamedSilentFail("RimScent_Smell");
            HautsF_RimScent.rimScentStat = DefDatabase<StatDef>.GetNamedSilentFail("RimScent_SmellSensitivity");
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.rimscent");
            harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.RimScentFactorFor)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_RimScentFactorForPostfix)));
        }
        //pawn (if of player faction) accrues progress towards learning other's language (or random unlearned language if other is null), using their own stats to scale a base "power" amount. See description in ModCompatibilityUtility.cs
        public static void Hauts_RimScentFactorForPostfix(Pawn p, ref float __result)
        {
            __result *= p.health.capacities.GetLevel(HautsF_RimScent.rimScentCapacity);
            __result *= p.GetStatValue(HautsF_RimScent.rimScentStat);
        }
        public static PawnCapacityDef rimScentCapacity;
        public static StatDef rimScentStat;
    }
}
