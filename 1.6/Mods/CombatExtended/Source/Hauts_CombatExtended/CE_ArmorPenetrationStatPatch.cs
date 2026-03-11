using CombatExtended;
using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Hauts_CombatExtended
{
    [StaticConstructorOnStartup]
    public static class Hauts_CombatExtended
    {
        private static readonly Type patchType = typeof(Hauts_CombatExtended);
        static Hauts_CombatExtended()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.combatExtended");
            harmony.Patch(AccessTools.Property(typeof(Verb_MeleeAttackCE), nameof(Verb_MeleeAttackCE.ArmorPenetrationSharp)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HautsArmorPenetration_MeleePostfix)));
            harmony.Patch(AccessTools.Property(typeof(Verb_MeleeAttackCE), nameof(Verb_MeleeAttackCE.ArmorPenetrationBlunt)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HautsArmorPenetration_MeleePostfix)));
            harmony.Patch(AccessTools.Property(typeof(ProjectileCE), nameof(ProjectileCE.PenetrationAmount)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HautsArmorPenetration_RangedPostfix)));
        }
        public static void HautsArmorPenetration_MeleePostfix(ref float __result, Verb_MeleeAttackCE __instance)
        {
            if (__instance.CasterIsPawn)
            {
                __result *= __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_MeleeArmorPenetration);
            }
        }
        public static void HautsArmorPenetration_RangedPostfix(ref float __result, ProjectileCE __instance)
        {
            if (__instance.launcher != null && __instance.launcher is Pawn p)
            {
                __result *= p.GetStatValue(HautsDefOf.Hauts_RangedArmorPenetration);
            }
        }
    }
}
