using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using VanillaRacesExpandedSanguophage;
using Verse;

namespace Hauts_VRESanguophage
{
    [StaticConstructorOnStartup]
    public class Hauts_VRESanguophage
    {
        private static readonly Type patchType = typeof(Hauts_VRESanguophage);
        static Hauts_VRESanguophage()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.vresanguophage");
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_CorpsefeederBite), nameof(CompAbilityEffect_CorpsefeederBite.DoBite)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_CorpsefeederBite_DoBitePrefix)));
            harmony.Patch(AccessTools.Method(typeof(CompDraincasket), nameof(CompDraincasket.CompTickInterval)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_DraincasketCompTickIntervalPrefix)));
            harmony.Patch(AccessTools.Method(typeof(CompDraincasket), nameof(CompDraincasket.CompTickInterval)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_DraincasketCompTickIntervalPostfix)));
        }
        //makes hemoyield work w corpsefeeding (the other feeding methods are already fine out of the box)
        public static void Hauts_CorpsefeederBite_DoBitePrefix(Corpse corpse, ref float targetHemogenGain)
        {
            if (corpse.InnerPawn != null)
            {
                targetHemogenGain *= corpse.InnerPawn.GetStatValue(HautsDefOf.Hauts_HemogenContentFactor);
            }
        }
        //make it work w the draincasket
        public static void Hauts_DraincasketCompTickIntervalPrefix(CompDraincasket __instance, int delta, ref float __state)
        {
            __state = VanillaRacesExpandedSanguophage_Settings.drainCasketAmount;
            if (__instance.parent.IsHashIntervalTick(60000, delta))
            {
                Pawn pawn = __instance.Occupant;
                if (pawn != null)
                {
                    VanillaRacesExpandedSanguophage_Settings.drainCasketAmount *= pawn.GetStatValue(HautsDefOf.Hauts_HemogenContentFactor);
                }
            }
        }
        public static void Hauts_DraincasketCompTickIntervalPostfix(float __state)
        {
            VanillaRacesExpandedSanguophage_Settings.drainCasketAmount = __state;
        }
    }
}
