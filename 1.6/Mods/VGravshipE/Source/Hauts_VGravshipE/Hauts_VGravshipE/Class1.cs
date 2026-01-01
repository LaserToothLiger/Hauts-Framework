using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VanillaGravshipExpanded;
using Verse;

namespace Hauts_VGravshipE
{
    [StaticConstructorOnStartup]
    public static class Hauts_VGravshipE
    {
        private static readonly Type patchType = typeof(Hauts_VGravshipE);
        static Hauts_VGravshipE()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.vgravshipe");
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.AddGravdata)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsAddGravdataPostfix)));
        }
        public static void HautsAddGravdataPostfix(ref bool __result, Pawn researcher, float power)
        {
            if (World_ExposeData_Patch.currentGravtechProject != null)
            {
                if (researcher != null)
                {
                    power *= Math.Max(0.08f, researcher.GetStatValue(VGEDefOf.VGE_GravshipResearch));
                }
                Log.Message(string.Format("[VGE] Adding {0} to project: {1}", power, World_ExposeData_Patch.currentGravtechProject.defName));
                float num4 = World_ExposeData_Patch.currentGravtechProject.Cost - Find.ResearchManager.GetProgress(World_ExposeData_Patch.currentGravtechProject);
                float num5 = Mathf.Min(power, num4);
                GravshipResearchUtility.ResearchPerformed(num5, null);
                __result = true;
            }
        }
    }
}
