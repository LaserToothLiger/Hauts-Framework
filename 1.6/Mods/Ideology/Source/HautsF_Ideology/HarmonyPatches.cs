using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsF_Ideology
{
    [StaticConstructorOnStartup]
    public class HautsF_Ideology
    {
        private static readonly Type patchType = typeof(HautsF_Ideology);
        static HautsF_Ideology()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.ideology");
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_GiveHediff), nameof(CompAbilityEffect_GiveHediff.Apply), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGiveHediffPostfix)));
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_GiveMentalState), nameof(CompAbilityEffect_GiveMentalState.Apply), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGiveMentalStatePostfix)));
            harmony.Patch(AccessTools.Property(typeof(CompTreeConnection), nameof(CompTreeConnection.MaxDryads)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HautsMaxDryadsPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.SetIdeo)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsSetIdeoPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Book), nameof(Book.GenerateBook)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGenerateBookPostfix)));
        }
        //ideoligious ability susceptibility affects hediffs and mental states caused by ideo role abilities
        public static void HautsGiveHediffPostfix(CompAbilityEffect_GiveHediff __instance, LocalTargetInfo target)
        {
            if (__instance.parent.sourcePrecept != null)
            {
                if (target.Pawn.Ideo == __instance.parent.pawn.Ideo)
                {
                    Pawn realTarget = null;
                    if (!__instance.Props.onlyApplyToSelf && __instance.Props.applyToTarget)
                    {
                        realTarget = target.Pawn;
                    }
                    if (__instance.Props.applyToSelf || __instance.Props.onlyApplyToSelf)
                    {
                        realTarget = __instance.parent.pawn;
                    }
                    if (realTarget != null)
                    {
                        Hediff hediff = realTarget.health.hediffSet.GetFirstHediffOfDef(__instance.Props.hediffDef, false);
                        HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
                        if (hediffComp_Disappears != null)
                        {
                            int newDuration = (int)(hediffComp_Disappears.ticksToDisappear * realTarget.GetStatValue(HautsDefOf.Hauts_IdeoAbilityDurationSelf));
                            hediffComp_Disappears.ticksToDisappear = newDuration;
                        }
                    }
                }
            }
        }
        public static void HautsGiveMentalStatePostfix(CompAbilityEffect_GiveMentalState __instance, LocalTargetInfo target)
        {
            Pawn pawn = __instance.Props.applyToSelf ? __instance.parent.pawn : (target.Thing as Pawn);
            if (pawn != null && pawn.Ideo == __instance.parent.pawn.Ideo && pawn.mindState != null && pawn.mindState.mentalStateHandler.CurStateDef == __instance.Props.stateDef)
            {
                int newDuration = (int)(pawn.mindState.mentalStateHandler.CurState.forceRecoverAfterTicks * pawn.GetStatValue(HautsDefOf.Hauts_IdeoAbilityDurationSelf));
                pawn.mindState.mentalStateHandler.CurState.forceRecoverAfterTicks *= newDuration;
            }
        }
        //max dryad factor
        public static void HautsMaxDryadsPostfix(ref int __result, CompTreeConnection __instance)
        {
            if (__instance.Connected)
            {
                __result = (int)Math.Floor(__result * __instance.ConnectedPawn.GetStatValue(HautsDefOf.Hauts_MaxDryadFactor));
            }
        }
        //ideos remove themselves from the world list at 0 followers.
        public static void HautsSetIdeoPostfix(Pawn_IdeoTracker __instance)
        {
            MethodInfo TryQueueIdeoRemoval = typeof(IdeoManager).GetMethod("TryQueueIdeoRemoval", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!__instance.PreviousIdeos.NullOrEmpty() && Find.IdeoManager.IdeosListForReading.Contains(__instance.PreviousIdeos.Last()))
            {
                TryQueueIdeoRemoval.Invoke(Find.IdeoManager, new object[] { __instance.PreviousIdeos.Last() });
            }
        }
        //ideo book generation - make them use relevant ideoligious terms
        public static void HautsGenerateBookPostfix(Book __instance, Pawn author, long? fixedDate)
        {
            foreach (BookOutcomeDoer bod in __instance.BookComp.Doers)
            {
                if (bod is BookOutcomeDoerPromoteIdeo bodpi)
                {
                    bodpi.subjects.Clear();
                    GrammarRequest grammarRequest = default(GrammarRequest);
                    long num = fixedDate ?? ((long)GenTicks.TicksAbs - (long)(__instance.BookComp.Props.ageYearsRange.RandomInRange * 3600000f));
                    grammarRequest.Rules.Add(new Rule_String("date", GenDate.DateFullStringAt(num, Vector2.zero)));
                    grammarRequest.Rules.Add(new Rule_String("date_season", GenDate.DateMonthYearStringAt(num, Vector2.zero)));
                    grammarRequest.Constants.Add("quality", ((int)__instance.GetComp<CompQuality>().Quality).ToString());
                    foreach (Rule rule in ((author == null) ? TaleData_Pawn.GenerateRandom(true) : TaleData_Pawn.GenerateFrom(author)).GetRules("ANYPAWN", grammarRequest.Constants))
                    {
                        grammarRequest.Rules.Add(rule);
                    }
                    bodpi.AppendDoerRules(__instance, author, grammarRequest);
                    bodpi.AppendRulesForSubject(bodpi.subjects, grammarRequest.Rules, grammarRequest.Constants, "primary", 0);
                    bodpi.AppendRulesForSubject(bodpi.subjects, grammarRequest.Rules, grammarRequest.Constants, "secondary", 1);
                    bodpi.AppendRulesForSubject(bodpi.subjects, grammarRequest.Rules, grammarRequest.Constants, "tertiary", 2);
                    List<RulePack> memeRulePack = new List<RulePack>();
                    if (bodpi.ideo != null && !bodpi.ideo.memes.NullOrEmpty())
                    {
                        memeRulePack.AddRange(bodpi.ideo.memes.Select((MemeDef md) => md.generalRules).ToList<RulePack>());
                    }
                    GrammarRequest grammarRequest2 = grammarRequest;
                    grammarRequest2.Includes.Add(__instance.BookComp.Props.nameMaker);
                    grammarRequest2.IncludesBare.AddRange(memeRulePack);
                    typeof(Book).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, GenText.CapitalizeAsTitle(GrammarResolver.Resolve("title", grammarRequest2, null, false, null, null, null, true)).StripTags());
                    GrammarRequest grammarRequest3 = grammarRequest;
                    grammarRequest3.Includes.Add(__instance.BookComp.Props.descriptionMaker);
                    grammarRequest3.Includes.Add(RulePackDefOf.TalelessImages);
                    grammarRequest3.Includes.Add(RulePackDefOf.ArtDescriptionRoot_Taleless);
                    grammarRequest3.Includes.Add(RulePackDefOf.ArtDescriptionUtility_Global);
                    grammarRequest3.IncludesBare.AddRange(memeRulePack);
                    typeof(Book).GetField("descriptionFlavor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, GrammarResolver.Resolve("desc", grammarRequest3, null, false, null, null, null, true).StripTags());
                    typeof(Book).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, bodpi.GenerateFullDescription(__instance));
                    bodpi.subjects.Clear();
                    break;
                }
            }
        }
    }
}
