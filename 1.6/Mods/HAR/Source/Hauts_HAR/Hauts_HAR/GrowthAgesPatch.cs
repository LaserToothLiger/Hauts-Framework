using HarmonyLib;
using HautsFramework;
using System;
using Verse;

namespace Hauts_HAR
{
    [StaticConstructorOnStartup]
    public static class Hauts_HAR
    {
        private static readonly Type patchType = typeof(Hauts_HAR);
        static Hauts_HAR()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.humanoidalienraces");
            if (ModsConfig.BiotechActive)
            {
                harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.GrowthMomentAgesFor)),
                              postfix: new HarmonyMethod(patchType, nameof(Hauts_GrowthMomentAgesForPostfix)));
            }
        }
        //get the growthAges from an alien race's general settings to find the appropriate birthdays for growth moments
        public static void Hauts_GrowthMomentAgesForPostfix(Pawn p, ref int[] __result)
        {
            __result = AlienRace.HarmonyPatches.GrowthMomentHelper(p.def);
        }
    }
}
