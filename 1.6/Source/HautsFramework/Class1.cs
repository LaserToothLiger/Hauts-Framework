using HarmonyLib;
using HeavyWeapons;
using MVCF.VerbComps;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Sound;
using VEF;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.GraphicsBuffer;
using VEF.Abilities;
using System.Linq.Expressions;
using VEF.AnimalBehaviours;
using VEF.Weapons;
using System.Security.Cryptography;

namespace HautsFramework
{
    [StaticConstructorOnStartup]
    public static class HautsFramework
    {
        private static readonly Type patchType = typeof(HautsFramework);
        static HautsFramework()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.main");
            //tweaks
            harmony.Patch(AccessTools.Method(typeof(VEF.Abilities.Ability), nameof(VEF.Abilities.Ability.Cast), new[] { typeof(GlobalTargetInfo[]) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsVFEAbility_CastPostfix)));
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnKindDef), typeof(Faction), typeof(PlanetTile) }),
                          postfix: new HarmonyMethod(patchType, nameof(Hauts_GeneratePawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.OpenPermitWindow)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_OpenPermitTabPostfix)));
            //faction 'comps'
            harmony.Patch(AccessTools.Method(typeof(FactionManager), nameof(FactionManager.Add)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsFactionManager_AddPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TaleManager), nameof(TaleManager.Notify_FactionRemoved)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsNotify_FactionRemovedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.AdjustedRaidPoints)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsAdjustedRaidPointsPostfix)));
            //stats
            harmony.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedArmorPenetration), new[] { typeof(Verb), typeof(Pawn) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsAdjustedArmorPenetrationPostfix)));
            harmony.Patch(AccessTools.Property(typeof(Projectile), nameof(Projectile.ArmorPenetration)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HautsArmorPenetrationPostfix)));
            harmony.Patch(AccessTools.Method(typeof(DamageWorker), nameof(DamageWorker.Apply)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsDamageWorker_ApplyPrefix)));
            harmony.Patch(AccessTools.Method(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.Apply)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsDamageWorker_AddInjury_ApplyPrefix)));
            harmony.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedRange)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_AdjustedRangePostfix)));
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_WithDest), nameof(CompAbilityEffect_WithDest.CanHitTarget)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_CAE_WD_CanHitTargetPrefix)));
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Verb_CastAbility), nameof(RimWorld.Verb_CastAbility.DrawHighlight)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_VCA_DH_DrawHighlightPrefix)));
            harmony.Patch(AccessTools.Method(typeof(CaravanVisibilityCalculator), nameof(CaravanVisibilityCalculator.Visibility), new[] { typeof(List<Pawn>), typeof(bool), typeof(StringBuilder) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsCaravanVisibilityPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_FilthTracker), nameof(Pawn_FilthTracker.GainFilth), new[] { typeof(ThingDef) }),
                          prefix: new HarmonyMethod(patchType, nameof(HautsGainFilthPrefix)));
            MethodInfo methodInfo = typeof(Pawn_FilthTracker).GetMethod("DropCarriedFilth", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo,
                          postfix: new HarmonyMethod(patchType, nameof(HautsDropCarriedFilthPostfix)));
            harmony.Patch(AccessTools.Method(typeof(VEF.Abilities.Ability), nameof(VEF.Abilities.Ability.GetRangeForPawn)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_GetRangeForPawnPostfix)));
            if (ModsConfig.IdeologyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_GiveMentalState), nameof(CompAbilityEffect_GiveMentalState.Apply), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                              postfix: new HarmonyMethod(patchType, nameof(HautsGiveMentalStatePostfix)));
                harmony.Patch(AccessTools.Property(typeof(CompTreeConnection), nameof(CompTreeConnection.MaxDryads)).GetGetMethod(),
                               postfix: new HarmonyMethod(patchType, nameof(HautsMaxDryadsPostfix)));
            }
            harmony.Patch(AccessTools.Method(typeof(Book), nameof(Book.OnBookReadTick)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsOnBookReadTickPostfix)));
            harmony.Patch(AccessTools.Method(typeof(JoyUtility), nameof(JoyUtility.JoyTickCheckEnd)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsJoyTickCheckEndPostfix)));
            harmony.Patch(AccessTools.Method(typeof(JoyToleranceSet), nameof(JoyToleranceSet.NeedInterval)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsJoyToleranceSet_NeedIntervalPostfix)));
            harmony.Patch(AccessTools.Method(typeof(LearningUtility), nameof(LearningUtility.LearningRateFactor)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsLearningRateFactorPostfix)));
            if (ModsConfig.BiotechActive)
            {
                if (!ModsConfig.IsActive("lts.I"))
                {
                    harmony.Patch(AccessTools.Method(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.DrawCommandRadius)),
                                   prefix: new HarmonyMethod(patchType, nameof(Hauts_DrawCommandRadiusPrefix)));
                }
                harmony.Patch(AccessTools.Method(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanCommandTo)),
                               postfix: new HarmonyMethod(patchType, nameof(HautsCanCommandToPostfix)));
            }
            harmony.Patch(AccessTools.Method(typeof(ThoughtHandler), nameof(ThoughtHandler.MoodOffsetOfGroup)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsMoodOffsetOfGroupPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ThoughtHandler), nameof(ThoughtHandler.OpinionOffsetOfGroup)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsOpinionOffsetOfGroupPostfix)));
            harmony.Patch(AccessTools.Property(typeof(Verb_CastAbilityJump), nameof(Verb_CastAbilityJump.EffectiveRange)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_VCAJ_EffectiveRangePostfix)));
            harmony.Patch(AccessTools.Property(typeof(Verb_Jump), nameof(Verb_Jump.EffectiveRange)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_VJ_EffectiveRangePostfix)));
            if (ModsConfig.BiotechActive)
            {
                harmony.Patch(AccessTools.Method(typeof(SanguophageUtility), nameof(SanguophageUtility.HemogenGainBloodlossFactor)),
                               postfix: new HarmonyMethod(patchType, nameof(Hauts_HemogenGainBloodlossFactorPostfix)));
            }
            if (!ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
            {
                harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                               postfix: new HarmonyMethod(patchType, nameof(HautsPsycast_ActivatePostfix)));
                harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(GlobalTargetInfo) }),
                               postfix: new HarmonyMethod(patchType, nameof(HautsPsycast_ActivatePostfix)));
                harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                               postfix: new HarmonyMethod(patchType, nameof(HautsIngestedPostfix)));
                harmony.Patch(AccessTools.Method(typeof(MeditationUtility), nameof(MeditationUtility.CheckMeditationScheduleTeachOpportunity)),
                               postfix: new HarmonyMethod(patchType, nameof(HautsCheckMeditationScheduleTeachOpportunityPostfix)));
                harmony.Patch(AccessTools.Method(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled)),
                              postfix: new HarmonyMethod(patchType, nameof(HautsNotify_PawnKilledPostfix)));
            }
            //trait stuff
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGainTraitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.RemoveTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsRemoveTraitPostfix)));
            /*harmony.Patch(AccessTools.Method(typeof(Scenario), nameof(Scenario.PostGameStart)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsPostGameStartPostfix)));*/
            harmony.Patch(AccessTools.Method(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_SetGuestStatusPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGetGizmosPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Recipe_RemoveBodyPart), nameof(Recipe_RemoveBodyPart.ApplyOnPawn)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsApplyOnPawnPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Recipe_RemoveImplant), nameof(Recipe_RemoveImplant.ApplyOnPawn)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsApplyOnPawnPrefix)));
            MethodInfo methodInfo2 = typeof(StatPart_Glow).GetMethod("ActiveFor", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo2,
                          postfix: new HarmonyMethod(patchType, nameof(HautsStatPart_GlowActiveForPostfix)));
            harmony.Patch(AccessTools.Method(typeof(LifeStageWorker_HumanlikeAdult), nameof(LifeStageWorker_HumanlikeAdult.Notify_LifeStageStarted)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsNotify_LifeStageStartedPostfix)));
            MethodInfo methodInfo3 = typeof(Pawn_ApparelTracker).GetMethod("TakeWearoutDamageForDay", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo3,
                          prefix: new HarmonyMethod(patchType, nameof(HautsTakeWearoutDamageForDayPrefix)));
            harmony.Patch(AccessTools.Method(typeof(CompDrug), nameof(CompDrug.PostIngested)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsCompDrug_PostIngestedPrefix)));
            harmony.Patch(AccessTools.Method(typeof(CompDrug), nameof(CompDrug.PostIngested)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsCompDrug_PostIngestedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.SpawnThingsFromHediffs)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsApplyOnPawnPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsKillPrefix)));
            //hediff comps
            if (ModsConfig.IdeologyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.SetIdeo)),
                              postfix: new HarmonyMethod(patchType, nameof(HautsSetIdeoPostfix)));
            }
            if (ModsConfig.BiotechActive)
            {
                harmony.Patch(AccessTools.Method(typeof(Gene_Resource), nameof(Gene_Resource.ResetMax)),
                              postfix: new HarmonyMethod(patchType, nameof(HautsResetMaxPostfix)));
                harmony.Patch(AccessTools.Method(typeof(GeneResourceDrainUtility), nameof(GeneResourceDrainUtility.OffsetResource)),
                              prefix: new HarmonyMethod(patchType, nameof(HautsOffsetResourcePrefix)));
                HautsUtility.mechlinkDefs = new List<HediffDef>();
                foreach (HediffDef h in DefDatabase<HediffDef>.AllDefsListForReading)
                {
                    if (h.HasComp(typeof(HediffComp_MCR_Storage)))
                    {
                        HautsUtility.mechlinkDefs.Add(h);
                    }
                }
            }
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PreApplyDamage)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsFramework_PreApplyDamagePostfix)));
            harmony.Patch(AccessTools.Method(typeof(AttachableThing), nameof(AttachableThing.AttachTo)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_AddAttachmentPrefix)));
            /* missing a transpiler patch that would hit up VerbTracker.Command_VerbTarget's CreateVerbTargetCommand function, doing something similar to FirstApparelPreventingShooting
             * harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.ApparelPreventsShooting)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsApparelPreventsShootingPostfix)));*/
            harmony.Patch(AccessTools.Method(typeof(Pawn_PsychicEntropyTracker), nameof(Pawn_PsychicEntropyTracker.OffsetPsyfocusDirectly)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_OffsetPsyfocusDirectlyPrefix)));
            harmony.Patch(AccessTools.Method(typeof(HediffComp_ReactOnDamage), nameof(HediffComp_ReactOnDamage.Notify_PawnPostApplyDamage)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsPawnPostApplyDamagePrefix)));
            harmony.Patch(AccessTools.Method(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsStunNotifyDamageAppliedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(PlayDataLoader), nameof(PlayDataLoader.HotReloadDefs)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsHotReloadPostfix)));
            HautsUtility.ApplyAllDamageFactorGroupDefs();
            harmony.Patch(AccessTools.Method(typeof(Gene_Resource), nameof(Gene_Resource.Reset)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsResetMaxPostfix)));
            //abilities
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Ability), nameof(RimWorld.Ability.Activate), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsActivatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Ability), nameof(RimWorld.Ability.Activate), new[] { typeof(GlobalTargetInfo) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsActivatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(VEF.Abilities.Ability), nameof(VEF.Abilities.Ability.GetCooldownForPawn)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGetCooldownForPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.RemoveAbility)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsRemoveAbilityPostfix)));
            /*harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.CreateCombatLog)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsMelee_CreateCombatLogPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.WarmupComplete)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsWarmupCompletePostfix)));*/
            harmony.Patch(AccessTools.Method(typeof(RitualBehaviorWorker), nameof(RitualBehaviorWorker.TryExecuteOn)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsRitualBehaviorWorker_TryExecuteOnPostfix)));
            /*if (ModsConfig.RoyaltyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(RitualOutcomeEffectWorker_Speech), nameof(RitualOutcomeEffectWorker_Speech.Apply)),
                              postfix: new HarmonyMethod(patchType, nameof(HautsRitualOutcomeEffectWorker_Speech_ApplyPostfix)));
            }*/
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.CanApplyPsycastTo)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsCanApplyPsycastToPostfix)));
            //delayed resurrection vs shambler
            harmony.Patch(AccessTools.Method(typeof(MutantUtility), nameof(MutantUtility.CanResurrectAsShambler)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsCanResurrectAsShamblerPostfix)));
            //verbs
            harmony.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.EffectiveMinRange), new[] { typeof(bool) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsEffectiveMinRangePostfix)));
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Ability), nameof(RimWorld.Ability.GetJob)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGetJobPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Caravan), nameof(Caravan.GetGizmos)),
                          postfix: new HarmonyMethod(patchType, nameof(Hauts_Settlement_GetGizmosPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Settlement), nameof(Settlement.GetFloatMenuOptions)),
                          postfix: new HarmonyMethod(patchType, nameof(Hauts_Settlement_GetFloatMenuOptionsPostfix)));
            //resurrection in unusual places
            harmony.Patch(AccessTools.Method(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsResurrectionPrefix_Interred)));
            harmony.Patch(AccessTools.Method(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsResurrectionPrefix_Caravan)));
            harmony.Patch(AccessTools.Method(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsResurrectionPostfix_Caravan)));
            /*harmony.Patch(AccessTools.Method(typeof(ShotReport), nameof(ShotReport.HitFactorFromShooter), new[] { typeof(Thing), typeof(float) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsHitFactorFromShooterPostfix)));*/
            //ability, incident, and trait categories
            List<TraitDef> ConceitedTraits = GetTypeField(typeof(RoyalTitleUtility), "ConceitedTraits") as List<TraitDef>;
            foreach (IncidentDef i in DefDatabase<IncidentDef>.AllDefs)
            {
                if (i.HasModExtension<BelongsToEventPool>())
                {
                    if (i.GetModExtension<BelongsToEventPool>().good)
                    {
                        HautsUtility.AddGoodEvent(i);
                    }
                    if (i.GetModExtension<BelongsToEventPool>().bad)
                    {
                        HautsUtility.AddBadEvent(i);
                    }
                }
            } 
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefs)
            {
                if (t.HasModExtension<ExciseTraitExempt>())
                {
                    HautsUtility.AddExciseTraitExemption(t);
                }
                if (t.HasModExtension<ConceitedTrait>())
                {
                    ConceitedTraits.Add(t);
                }
            }
            //ai checker
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Ability), nameof(RimWorld.Ability.AICanTargetNow)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsAICanTargetNowPrefix)));
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect), nameof(CompAbilityEffect.AICanTargetNow)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsAICanTargetNowPostfix)));
            //ignore natural goodwill's influence on the amount of goodwill gained or lost by a particular HED
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.TryAffectGoodwillWith)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTryAffectGoodwillWithPrefix)));
            HautsUtility.isHighFantasy = ModsConfig.IsActive("MrSamuelStreamer.RPGAdventureFlavour.DEV");
            HautsUtility.combatIsExtended = ModsConfig.IsActive("CETeam.CombatExtended");
            Log.Message("Hauts_Initialize".Translate().CapitalizeFirst());
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        internal static object GetTypeField(Type type, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(type);
        }
        //tweaks
        /*public static void HautsMassUtility_CapacityPostfix(ref float __result, Pawn p)
        {
            __result *= (p.GetStatValue(StatDefOf.CarryingCapacity) / 75f);
        }*/
        public static void HautsVFEAbility_CastPostfix(VEF.Abilities.Ability __instance, GlobalTargetInfo[] targets)
        {
            if (__instance.pawn != null)
            {
                LocalTargetInfo needATarget = targets.Any<GlobalTargetInfo>() ? ((targets[0].Thing != null) ? new LocalTargetInfo(targets[0].Thing) : new LocalTargetInfo(targets[0].Cell)) : default(LocalTargetInfo);
                foreach (Hediff h in __instance.pawn.health.hediffSet.hediffs)
                {
                    h.Notify_PawnUsedVerb(__instance.GetVerb, needATarget);
                }
            }
        }
        public static void Hauts_GeneratePawnPostfix(ref Pawn __result)
        {
            if (__result.equipment != null && __result.equipment.Primary != null && __result.story != null)
            {
                List<Trait> traitsToRemove = new List<Trait>();
                foreach (Trait t in __result.story.traits.allTraits)
                {
                    if ((t.def.disabledWorkTags & WorkTags.Violent) > WorkTags.None)
                    {
                        Log.Warning("Hauts_PacifistWWeapon".Translate().CapitalizeFirst().Formatted(__result.Named("PAWN")).AdjustedFor(__result, "PAWN", true).Resolve());
                        traitsToRemove.Add(t);
                    }
                }
                foreach (Trait t in traitsToRemove) {
                    __result.story.traits.RemoveTrait(t);
                }
            }
        }
        public static void Hauts_OpenPermitTabPostfix(Pawn_RoyaltyTracker __instance)
        {
            if (!__instance.AllTitlesInEffectForReading.NullOrEmpty())
            {
                Faction f = __instance.MostSeniorTitle.faction;
                foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
                {
                    if (__instance.GetPermitPoints(faction) > __instance.GetPermitPoints(f))
                    {
                        f = faction;
                    } else if (__instance.GetPermitPoints(faction) == __instance.GetPermitPoints(f)) {
                        RoyalTitle rtFac = __instance.GetCurrentTitleInFaction(faction), rtF = __instance.GetCurrentTitleInFaction(f);
                        if (rtFac != null && rtF != null && rtFac.def.seniority > rtF.def.seniority)
                        {
                            f = faction;
                        }
                    }
                }
                if (f != null)
                {
                    PermitsCardUtility.selectedFaction = f;
                }
            }
        }
        /*public static void Hauts_TryFindCastPositionPrefix(ref CastPositionRequest newReq)
        {
            if (newReq.caster != null)
            {
                newReq.maxRangeFromTarget += Mathf.Max(0f, newReq.caster.GetStatValue(VFEDefOf.VEF_MeleeWeaponRange) - VFEDefOf.VEF_MeleeWeaponRange.defaultBaseValue);
            }
        }
        public static void Hauts_BestAttackTargetPrefix(IAttackTargetSearcher searcher, ref float maxDist)
        {
            if (searcher is Pawn p)
            {
                maxDist += Mathf.Max(0f, p.GetStatValue(VFEDefOf.VEF_MeleeWeaponRange) - VFEDefOf.VEF_MeleeWeaponRange.defaultBaseValue);
            }
        }
        public static void Hauts_IsVanillaMeleeAttackPostfix(Verb verb, ref bool __result)
        {
            if (verb.Caster is Pawn p && p.GetStatValue(VFEDefOf.VEF_MeleeWeaponRange) - VFEDefOf.VEF_MeleeWeaponRange.defaultBaseValue > 0f)
            {
                __result = false;
            }
        }*/
        //faction comps. second method here makes the example comp, "spy points", work
        public static void HautsFactionManager_AddPostfix(Faction faction)
        {
            WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
            if (WCFC != null)
            {
                bool shouldAdd = true;
                foreach (Hauts_FactionCompHolder fch in WCFC.factions)
                {
                    if (fch.factionLoadID == faction.loadID)
                    {
                        shouldAdd = false;
                        break;
                    }
                }
                if (shouldAdd)
                {
                    Hauts_FactionCompHolder newFCH = new Hauts_FactionCompHolder(faction);
                    newFCH.PostMake();
                    WCFC.factions.Add(newFCH);
                }
            }
        }
        public static void HautsNotify_FactionRemovedPostfix(Faction faction)
        {
            WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
            if (WCFC != null)
            {
                Hauts_FactionCompHolder fch = WCFC.FindCompsFor(faction);
                if (fch != null)
                {
                    WCFC.factions.Remove(fch);
                }
            }
        }
        public static void HautsAdjustedRaidPointsPostfix(ref float __result, Faction faction)
        {
            WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
            Hauts_FactionCompHolder fch = WCFC.FindCompsFor(faction);
            if (fch != null)
            {
                HautsFactionComp_SpyPoints spyPoints = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                if (spyPoints != null)
                {
                    float bonusPoints = Math.Min(__result, spyPoints.spyPoints);
                    __result += bonusPoints;
                    spyPoints.spyPoints = Math.Max(0,spyPoints.spyPoints - (int)bonusPoints);
                }
            }
        }
        //stats, though IngestedPostfix also does a hediffcomp function
        public static void HautsAdjustedArmorPenetrationPostfix(ref float __result, VerbProperties __instance, Pawn attacker)
        {
            if (__instance.IsMeleeAttack)
            {
                __result *= attacker.GetStatValue(HautsDefOf.Hauts_MeleeArmorPenetration);
            } else {
                __result *= attacker.GetStatValue(HautsDefOf.Hauts_RangedArmorPenetration);
            }
        }
        public static void HautsArmorPenetrationPostfix(ref float __result, Projectile __instance)
        {
            Pawn pawn = __instance.Launcher as Pawn;
            if (pawn != null)
            {
                __result *= pawn.GetStatValue(HautsDefOf.Hauts_RangedArmorPenetration);
            }
        }
        public static void HautsDamageWorker_ApplyPrefix(ref DamageInfo dinfo, Thing victim)
        {
            if (dinfo.Instigator != null && dinfo.Instigator is Pawn p && victim.def.category == ThingCategory.Building && victim.def.useHitPoints)
            {
                DamageInfo dinfo2 = new DamageInfo(dinfo.Def, dinfo.Amount * p.GetStatValue(HautsDefOf.Hauts_BreachDamageFactor), dinfo.ArmorPenetrationInt, -dinfo.Angle, dinfo.Instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.IntendedTarget, dinfo.InstigatorGuilty, dinfo.SpawnFilth);
                dinfo = dinfo2;
            }
        }
        public static void HautsDamageWorker_AddInjury_ApplyPrefix(ref DamageInfo dinfo, Thing thing)
        {
            if (dinfo.Instigator != null && dinfo.Instigator is Pawn p)
            {
                if (thing is Pawn p2 && (p2.RaceProps.IsAnomalyEntity || p2.IsMutant))
                {
                    DamageInfo dinfo2 = new DamageInfo(dinfo.Def, dinfo.Amount * p.GetStatValue(HautsDefOf.Hauts_EntityDamageFactor), dinfo.ArmorPenetrationInt, -dinfo.Angle, dinfo.Instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.IntendedTarget, dinfo.InstigatorGuilty, dinfo.SpawnFilth);
                    dinfo = dinfo2;
                }
            }
        }
        public static void Hauts_AdjustedRangePostfix(ref float __result, Verb ownerVerb, Pawn attacker)
        {
            if (attacker != null)
            {
                if (ownerVerb is RimWorld.Verb_CastAbility verb && __result > 0)
                {
                    if (HautsUtility.IsSkipAbility(verb.ability.def))
                    {
                        __result *= attacker.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor);
                    }
                    if (HautsUtility.IsSpewAbility(verb.ability.def))
                    {
                        __result *= attacker.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor);
                    }
                }
            }
        }
        public static bool Hauts_CAE_WD_CanHitTargetPrefix(ref bool __result, CompAbilityEffect_WithDest __instance, LocalTargetInfo target)
        {
            if (__instance.parent.pawn != null)
            {
                if (HautsUtility.IsSkipAbility(__instance.parent.def) && __instance.parent.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor) != 1f)
                {
                    LocalTargetInfo selectedTarget = (LocalTargetInfo)GetInstanceField(typeof(CompAbilityEffect_WithDest), __instance, "selectedTarget");
                    __result = target.IsValid && (__instance.Props.range <= 0f || target.Cell.DistanceTo(selectedTarget.Cell) <= __instance.Props.range * __instance.parent.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor) && (!__instance.Props.requiresLineOfSight || GenSight.LineOfSight(selectedTarget.Cell, target.Cell, __instance.parent.pawn.Map, false, null, 0, 0)));
                    return false;
                }
                if (HautsUtility.IsSpewAbility(__instance.parent.def) && __instance.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) != 1f)
                {
                    LocalTargetInfo selectedTarget = (LocalTargetInfo)GetInstanceField(typeof(CompAbilityEffect_WithDest), __instance, "selectedTarget");
                    __result = target.IsValid && (__instance.Props.range <= 0f || target.Cell.DistanceTo(selectedTarget.Cell) <= __instance.Props.range * __instance.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) && (!__instance.Props.requiresLineOfSight || GenSight.LineOfSight(selectedTarget.Cell, target.Cell, __instance.parent.pawn.Map, false, null, 0, 0)));
                    return false;
                }
            }
            return true;
        }
        public static bool Hauts_VCA_DH_DrawHighlightPrefix(RimWorld.Verb_CastAbility __instance, LocalTargetInfo target)
        {
            if (__instance.CasterIsPawn)
            {
                if (HautsUtility.IsSkipAbility(__instance.ability.def) && __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor) != 1f)
                {
                    HautsUtility.DrawBoostedAbilityRange(__instance, target, HautsDefOf.Hauts_SkipcastRangeFactor);
                    return false;
                }
                if (HautsUtility.IsSpewAbility(__instance.ability.def) && __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) != 1f)
                {
                    HautsUtility.DrawBoostedAbilityRange(__instance, target, HautsDefOf.Hauts_SpewRangeFactor);
                    return false;
                }
            }
            return true;
        }
        public static void HautsCaravanVisibilityPostfix(ref float __result, List<Pawn> pawns, bool caravanMovingNow, StringBuilder explanation = null)
        {
            float num = __result;
            if (!caravanMovingNow)
            {
                num /= 0.3f;
            }
            SimpleCurve BodySizeSumToVisibility = new SimpleCurve {
                {
                    new CurvePoint(0f, 0f),
                    true
                },
                {
                    new CurvePoint(1f, 0.2f),
                    true
                },
                {
                    new CurvePoint(6f, 1f),
                    true
                },
                {
                    new CurvePoint(12f, 1.12f),
                    true
                }
            };
            num = BodySizeSumToVisibility.EvaluateInverted(num);
            for (int i = 0; i < pawns.Count; i++)
            {
                num += pawns[i].GetStatValue(HautsDefOf.Hauts_CaravanVisibilityOffset);
                num -= pawns[i].BodySize;
                num += pawns[i].BodySize * pawns[i].GetStatValue(HautsDefOf.Hauts_PersonalCaravanVisibilityFactor);
            }
            if (num < 0f)
            {
                num = 0f;
            }
            __result = CaravanVisibilityCalculator.Visibility(num, caravanMovingNow, explanation);
        }
        public static void HautsCheckMeditationScheduleTeachOpportunityPostfix(Pawn pawn)
        {
            pawn.psychicEntropy.OffsetPsyfocusDirectly((pawn.GetStatValue(HautsDefOf.Hauts_PsyfocusRegenRate)+Pawn_PsychicEntropyTracker.FallRatePerPsyfocusBand[pawn.psychicEntropy.PsyfocusBand]) / 400f);
        }
        public static bool HautsGainFilthPrefix(Pawn_FilthTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_FilthTracker), __instance, "pawn") as Pawn;
            if (Rand.Value < pawn.GetStatValue(HautsDefOf.Hauts_TrackSize))
            {
                return true;
            }
            return false;
        }
        public static void HautsDropCarriedFilthPostfix(Pawn_FilthTracker __instance, Filth f)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_FilthTracker), __instance, "pawn") as Pawn;
            float trackSize = pawn.GetStatValue(HautsDefOf.Hauts_TrackSize) - 1f;
            while (trackSize > 0f)
            {
                if (Rand.Value <= trackSize)
                {
                    FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, f.def, f.sources, (pawn.Faction != null || !pawn.RaceProps.Animal) ? FilthSourceFlags.Unnatural : FilthSourceFlags.Natural);
                }
                trackSize -= 1f;
            }
        }
        public static void Hauts_GetRangeForPawnPostfix(ref float __result, VEF.Abilities.Ability __instance)
        {
            if (__instance.pawn != null && __result > 1f)
            {
                if (HautsUtility.IsLeapAbility(__instance.def))
                {
                    __result *= __instance.pawn.GetStatValue(HautsDefOf.Hauts_JumpRangeFactor);
                }
                if (HautsUtility.IsSkipAbility(__instance.def))
                {
                    __result *= __instance.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor);
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
        public static void HautsMaxDryadsPostfix(ref int __result, CompTreeConnection __instance)
        {
            if (__instance.Connected)
            {
                __result = (int)Math.Floor(__result*__instance.ConnectedPawn.GetStatValue(HautsDefOf.Hauts_MaxDryadFactor));
            }
        }
        public static void HautsPsycast_ActivatePostfix(Psycast __instance)
        {
            if (__instance.pawn != null)
            {
                float totalRefund = HautsUtility.TotalPsyfocusRefund(__instance.pawn, __instance.def.PsyfocusCost, __instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("WordOf"), __instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("Skip"));
                if (__instance.def.level == 1 && __instance.pawn.GetStatValue(HautsDefOf.Hauts_TierOnePsycastCostOffset) < 0f)
                {
                    totalRefund -= __instance.pawn.GetStatValue(HautsDefOf.Hauts_TierOnePsycastCostOffset);
                }
                __instance.pawn.psychicEntropy.OffsetPsyfocusDirectly(Math.Min(__instance.def.PsyfocusCost,totalRefund));
            }
        }
        public static void HautsIngestedPostfix(float __result, Pawn ingester)
        {
            foreach (Hediff h in ingester.health.hediffSet.hediffs)
            {
                if (h is HediffWithComps hwc)
                {
                    HediffComp_ChangeSeverityOnIngestion csoi = hwc.TryGetComp<HediffComp_ChangeSeverityOnIngestion>();
                    if (csoi != null && h.ageTicks >= csoi.Props.minAgeTicksToFunction)
                    {
                        h.Severity += __result*csoi.Props.severityPerNutritionIngested.RandomInRange;
                    }
                }
            }
            if (ModsConfig.RoyaltyActive)
            {
                Pawn_PsychicEntropyTracker psychicEntropy = ingester.psychicEntropy;
                if (psychicEntropy != null && ingester.GetStatValue(HautsDefOf.Hauts_PsyfocusFromFood) != 0f)
                {
                    psychicEntropy.OffsetPsyfocusDirectly(__result * ingester.GetStatValue(HautsDefOf.Hauts_PsyfocusFromFood));
                }
            }
        }
        public static void HautsOnBookReadTickPostfix(Pawn pawn)
        {
            if (pawn.skills != null)
            {
                pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f * (pawn.GetStatValue(HautsDefOf.Hauts_SkillGainFromRecreation) - 1), false, false);
            }
        }
        public static void HautsJoyTickCheckEndPostfix(Pawn pawn)
        {
            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.def.joySkill != null && pawn.skills != null)
            {
                pawn.skills.GetSkill(curJob.def.joySkill).Learn(curJob.def.joyXpPerTick * (pawn.GetStatValue(HautsDefOf.Hauts_SkillGainFromRecreation) - 1), false);
            }
        }
        public static void HautsJoyToleranceSet_NeedIntervalPostfix(JoyToleranceSet __instance, Pawn pawn)
        {
            DefMap<JoyKindDef, float> tolerances = GetInstanceField(typeof(JoyToleranceSet), __instance, "tolerances") as DefMap<JoyKindDef, float>;
            DefMap<JoyKindDef, bool> bored = GetInstanceField(typeof(JoyToleranceSet), __instance, "bored") as DefMap<JoyKindDef, bool>;
            for (int i = 0; i < tolerances.Count; i++)
            {
                float num2 = tolerances[i];
                num2 -= (pawn.GetStatValue(HautsDefOf.Hauts_BoredomDropPerDay)-ExpectationsUtility.CurrentExpectationFor(pawn).joyToleranceDropPerDay) * 150f / 60000f;
                if (num2 < 0f)
                {
                    num2 = 0f;
                }
                tolerances[i] = num2;
                if (bored[i] && num2 < 0.3f)
                {
                    bored[i] = false;
                }
            }
        }
        public static void HautsLearningRateFactorPostfix(ref float __result, Pawn pawn)
        {
            Job curJob = pawn.CurJob;
            if (curJob.def == JobDefOf.Lessontaking && curJob.targetB.Thing != null)
            {
                Pawn teacher = (Pawn)curJob.targetB.Thing;
                if (teacher != null)
                {
                    __result *= teacher.GetStatValue(HautsDefOf.Hauts_InstructiveAbility);
                }
            }
            else if (curJob.def == DefDatabase<JobDef>.GetNamed("Workwatching") && curJob.targetA.Thing != null)
            {
                Pawn teacher = (Pawn)curJob.targetB.Thing;
                if (teacher != null)
                {
                    __result *= teacher.GetStatValue(HautsDefOf.Hauts_InstructiveAbility);
                }
            }
        }
        public static bool Hauts_DrawCommandRadiusPrefix(Pawn_MechanitorTracker __instance)
        {
            if (__instance.Pawn.Spawned && __instance.AnySelectedDraftedMechs)
            {
                Hediff mechlink = HautsUtility.GetStrongestMechlink(__instance.Pawn);
                if (mechlink != null)
                {
                    HediffComp_MCR_Storage hcmcrs = mechlink.TryGetComp<HediffComp_MCR_Storage>();
                    if (hcmcrs != null)
                    {
                        float mcr = hcmcrs.mechCommandRadius;
                        GenDraw.DrawRadiusRing(__instance.Pawn.Position, mcr, Color.white, (IntVec3 c) => c.InBounds(__instance.Pawn.MapHeld));
                        return false;
                    }
                }
            }
            return true;
        }
        public static void HautsCanCommandToPostfix(ref bool __result, Pawn_MechanitorTracker __instance, LocalTargetInfo target)
        {
            __result = target.Cell.InBounds(__instance.Pawn.MapHeld) && (float)__instance.Pawn.Position.DistanceToSquared(target.Cell) < (__instance.Pawn.GetStatValue(HautsDefOf.Hauts_MechCommandRange) * __instance.Pawn.GetStatValue(HautsDefOf.Hauts_MechCommandRange));
        }
        public static void HautsMoodOffsetOfGroupPostfix(ref float __result, Thought group)
        {
            ModifyingTraits mt = group.def.GetModExtension<ModifyingTraits>();
            if (mt != null)
            {
                int sign = 0;
                if (group.pawn.story != null)
                {
                    foreach (TraitDef t in mt.multiplierTraits.Keys)
                    {
                        if (group.pawn.story.traits.HasTrait(t))
                        {
                            __result *= mt.multiplierTraits.TryGetValue(t);
                        }
                    }
                    if (mt.forcePositive != null)
                    {
                        foreach (TraitDef t in mt.forcePositive) {
                            if (group.pawn.story.traits.HasTrait(t))
                            {
                                sign++;
                            }
                        }
                    }
                    if (mt.forceNegative != null)
                    {
                        foreach (TraitDef t in mt.forceNegative)
                        {
                            if (group.pawn.story.traits.HasTrait(t))
                            {
                                sign--;
                            }
                        }
                    }
                }
                if (ModsConfig.BiotechActive && group.pawn.genes != null)
                {
                    foreach (GeneDef g in mt.multiplierGenes.Keys)
                    {
                        if (group.pawn.genes.HasActiveGene(g))
                        {
                            __result *= mt.multiplierGenes.TryGetValue(g);
                        }
                    }
                    if (mt.forcePositiveG != null)
                    {
                        foreach (GeneDef g in mt.forcePositiveG)
                        {
                            if (group.pawn.genes.HasActiveGene(g))
                            {
                                sign++;
                            }
                        }
                    }
                    if (mt.forceNegativeG != null)
                    {
                        foreach (GeneDef g in mt.forceNegativeG)
                        {
                            if (group.pawn.genes.HasActiveGene(g))
                            {
                                sign--;
                            }
                        }
                    }
                }
                if ((sign > 0 && __result < 0f) || (sign < 0 && __result > 0f))
                {
                    __result *= -1f;
                }
            }
            if (ModsConfig.IdeologyActive && group.pawn.Ideo != null)
            {
                if (group.sourcePrecept != null || group.def == ThoughtDefOf.Counselled_MoodBoost || group.pawn.Ideo.cachedPossibleSituationalThoughts.Contains(group.def))
                {
                    __result *= group.pawn.GetStatValue(HautsDefOf.Hauts_IdeoThoughtFactor);
                    return;
                }
            }
        }
        public static void HautsNotify_PawnKilledPostfix(Pawn killed, Pawn killer)
        {
            if (ModsConfig.RoyaltyActive && killer.psychicEntropy != null)
            {
                Pawn_PsychicEntropyTracker psychicEntropy = killer.psychicEntropy;
                float psyfocus = killer.GetStatValue(HautsDefOf.Hauts_PsyfocusGainOnKill) * killed.GetStatValue(StatDefOf.PsychicSensitivity);
                if (killed.RaceProps != null)
                {
                    if (killed.RaceProps.intelligence == Intelligence.Animal)
                    {
                        psyfocus *= 0.5f;
                    }
                    else if (killed.RaceProps.intelligence == Intelligence.ToolUser)
                    {
                        psyfocus *= 0.75f;
                    }
                }
                psychicEntropy.OffsetPsyfocusDirectly(psyfocus);
            }
        }
        public static void HautsOpinionOffsetOfGroupPostfix(ref int __result, ThoughtHandler __instance, ISocialThought group)
        {
            Thought thought = (Thought)group;
            ModifyingTraits mt = thought.def.GetModExtension<ModifyingTraits>();
            if (mt != null)
            {
                int sign = 0;
                if (__instance.pawn.story != null)
                {
                    foreach (TraitDef t in mt.multiplierTraits.Keys)
                    {
                        if (__instance.pawn.story.traits.HasTrait(t))
                        {
                            __result = (int)(__result * mt.multiplierTraits.TryGetValue(t));
                        }
                    }
                    if (mt.forcePositive != null)
                    {
                        foreach (TraitDef t in mt.forcePositive)
                        {
                            if (__instance.pawn.story.traits.HasTrait(t))
                            {
                                sign++;
                            }
                        }
                    }
                    if (mt.forceNegative != null)
                    {
                        foreach (TraitDef t in mt.forceNegative)
                        {
                            if (__instance.pawn.story.traits.HasTrait(t))
                            {
                                sign--;
                            }
                        }
                    }
                }
                if (ModsConfig.BiotechActive && __instance.pawn.genes != null)
                {
                    foreach (GeneDef g in mt.multiplierGenes.Keys)
                    {
                        if (__instance.pawn.genes.HasActiveGene(g))
                        {
                            __result = (int)(__result * mt.multiplierGenes.TryGetValue(g));
                        }
                    }
                    if (mt.forcePositiveG != null)
                    {
                        foreach (GeneDef g in mt.forcePositiveG)
                        {
                            if (__instance.pawn.genes.HasActiveGene(g))
                            {
                                sign++;
                            }
                        }
                    }
                    if (mt.forceNegativeG != null)
                    {
                        foreach (GeneDef g in mt.forceNegativeG)
                        {
                            if (__instance.pawn.genes.HasActiveGene(g))
                            {
                                sign--;
                            }
                        }
                    }
                }
                if ((sign > 0 && __result < 0f) || (sign < 0 && __result > 0f))
                {
                    __result *= -1;
                }
            }
            if (ModsConfig.IdeologyActive && __instance.pawn.Ideo != null)
            {
                if (thought.sourcePrecept != null || __instance.pawn.Ideo.cachedPossibleSituationalThoughts.Contains(thought.def))
                {
                    __result = (int)(__result*__instance.pawn.GetStatValue(HautsDefOf.Hauts_IdeoThoughtFactor));
                    return;
                }
            }
        }
        public static void Hauts_VCAJ_EffectiveRangePostfix(ref float __result, Verb_CastAbilityJump __instance)
        {
            if (__instance.CasterPawn != null)
            {
                __result *= __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_JumpRangeFactor);
            }
        }
        public static void Hauts_VJ_EffectiveRangePostfix(ref float __result, Verb_Jump __instance)
        {
            if (__instance.CasterPawn != null)
            {
                __result *= __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_JumpRangeFactor);
            }
        }
        public static void Hauts_HemogenGainBloodlossFactorPostfix(ref float __result, Pawn pawn)
        {
            __result *= pawn.GetStatValue(HautsDefOf.Hauts_HemogenContentFactor);
        }
        //trait defmodextension functionalities
        public static void HautsGainTraitPostfix(TraitSet __instance, Trait trait, bool suppressConflicts)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (!HautsUtility.ShouldNotGrantTraitStuff(pawn, __instance, trait))
            {
                HautsUtility.AddTraitGrantedStuff(true, trait, pawn);
                if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && suppressConflicts)
                {
                    for (int k = 0; k < __instance.allTraits.Count; k++)
                    {
                        if (__instance.allTraits[k].def != trait.def && trait.def.CanSuppress(__instance.allTraits[k]) && __instance.allTraits[k].def.canBeSuppressed)
                        {
                            HautsUtility.RemoveTraitGrantedStuff(__instance.allTraits[k], pawn);
                        }
                    }
                }
            }
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                HediffComp_GiveTrait hediffComp_GiveTrait = h.TryGetComp<HediffComp_GiveTrait>();
                if (hediffComp_GiveTrait != null && hediffComp_GiveTrait.Props.traitDef == trait.def && hediffComp_GiveTrait.Props.traitDegree == trait.Degree)
                {
                    hediffComp_GiveTrait.removeTraitOnRemoval = false;
                }
            }
            if ((trait.def.disabledWorkTags & WorkTags.Violent) != WorkTags.None)
            {
                Pawn_EquipmentTracker equipment = pawn.equipment;
                if (((equipment != null) ? equipment.Primary : null) != null)
                {
                    if (pawn.PositionHeld.IsValid && pawn.MapHeld != null)
                    {
                        ThingWithComps thingWithComps;
                        pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.PositionHeld, false);
                    } else {
                        pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
                    }
                }
            }
            if (pawn.needs != null)
            {
                pawn.needs.AddOrRemoveNeedsAsAppropriate();
            }
            pawn.health.hediffSet.DirtyCache();
        }
        public static void HautsRemoveTraitPostfix(TraitSet __instance, Trait trait, bool unsuppressConflicts)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            HautsUtility.RemoveTraitGrantedStuff(trait, pawn);
            if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && unsuppressConflicts)
            {
                for (int k = 0; k < __instance.allTraits.Count; k++)
                {
                    if (__instance.allTraits[k].def != trait.def && trait.def.CanSuppress(__instance.allTraits[k]))
                    {
                        HautsUtility.AddTraitGrantedStuff(true, __instance.allTraits[k], pawn);
                    }
                }
            }
            if (pawn.needs != null)
            {
                pawn.needs.AddOrRemoveNeedsAsAppropriate();
            }
        }
        public static IEnumerable<Gizmo> HautsGetGizmosPostfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos && __instance.story != null)
            {
                yield return (new Command_Action
                {
                    defaultLabel = "Hauts_CharEditFixer".Translate(),
                    defaultDesc = "Hauts_CharEditFixerDesc".Translate(),
                    action = delegate ()
                    {
                        if (ModsConfig.IdeologyActive && __instance.story != null && __instance.story.favoriteColor == null)
                        {
                            Pawn_StoryTracker pst = __instance.story;
                            if (pst.favoriteColor == null)
                            {
                                pst.favoriteColor = DefDatabase<ColorDef>.AllDefs.Where(delegate (ColorDef x)
                                {
                                    ColorType colorType = x.colorType;
                                    return colorType == ColorType.Ideo || colorType == ColorType.Misc;
                                }).RandomElement<ColorDef>();
                            }
                        }
                        HautsUtility.TraitGrantedStuffRegeneration(__instance);
                    }
                });
            }
        }
        public static void Hauts_SetGuestStatusPostfix(Pawn_GuestTracker __instance, GuestStatus guestStatus)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_GuestTracker), __instance, "pawn") as Pawn;
            if (guestStatus == GuestStatus.Prisoner && pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (t.def.HasModExtension<TraitGrantedStuff>() && t.def.GetModExtension<TraitGrantedStuff>().prisonerResolveFactor != null)
                    {
                        __instance.resistance *= t.def.GetModExtension<TraitGrantedStuff>().prisonerResolveFactor.TryGetValue(t.Degree);
                        __instance.will *= t.def.GetModExtension<TraitGrantedStuff>().prisonerResolveFactor.TryGetValue(t.Degree);
                    }
                }
            }
        }
        public static bool HautsApplyOnPawnPrefix(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (t.def.HasModExtension<CannotRemoveBionicsFrom>())
                    {
                        Messages.Message("Hauts_CannotRemoveBionicsFrom".Translate(pawn.Name.ToStringShort, t.Label), pawn, MessageTypeDefOf.RejectInput, false);
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool HautsKillPrefix(Pawn __instance)
        {
            if (HautsUtility.TryVanishPawn(__instance))
            {
                return false;
            }
            return true;
        }
        public static void HautsStatPart_GlowActiveForPostfix(ref bool __result, StatPart_Glow __instance, Thing t)
        {
            if (__result && (bool)__instance.GetType().GetField("ignoreIfPrefersDarkness", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))
            {
                if (t is Pawn p && p.story != null)
                {
                    foreach (Trait trait in p.story.traits.TraitsSorted)
                    {
                        if (trait.def.HasModExtension<UnaffectedByDarkness>())
                        {
                            __result = false;
                            break;
                        }
                    }
                }
            }
        }
        public static void HautsNotify_LifeStageStartedPostfix(Pawn pawn)
        {
            if (pawn.story != null && pawn.story.bodyType != null && (pawn.def == ThingDefOf.Human || (ModsConfig.AnomalyActive && pawn.def == ThingDefOf.CreepJoiner)))
            {
                foreach (Trait t in pawn.story.traits.TraitsSorted)
                {
                    TraitGrantedStuff tgs = t.def.GetModExtension<TraitGrantedStuff>();
                    if (tgs != null)
                    {
                        if (tgs.forcedBodyTypes != null)
                        {
                            if (tgs.forcedBodyTypes.Keys.Contains(pawn.story.bodyType))
                            {
                                pawn.story.bodyType = tgs.forcedBodyTypes.TryGetValue(pawn.story.bodyType);
                                pawn.Drawer.renderer.SetAllGraphicsDirty();
                            }
                        }
                    }
                }
            }
        }
        public static bool HautsTakeWearoutDamageForDayPrefix(Pawn_ApparelTracker __instance, Thing ap)
        {
            Pawn pawn = __instance.pawn;
            int num = (int)(GenMath.RoundRandom(ap.def.apparel.wearPerDay) * pawn.GetStatValue(HautsDefOf.Hauts_ApparelWearRateFactor));
            if (num > 0)
            {
                ap.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, (float)num, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
            }
            if (ap.Destroyed && PawnUtility.ShouldSendNotificationAbout(pawn) && !pawn.Dead)
            {
                Messages.Message("MessageWornApparelDeterioratedAway".Translate(GenLabel.ThingLabel(ap.def, ap.Stuff, 1), pawn).CapitalizeFirst(), pawn, MessageTypeDefOf.NegativeEvent, true);
            }
            return false;
        }
        public static void HautsCompDrug_PostIngestedPrefix(CompDrug __instance, Pawn ingester, ref List<float> __state)
        {
            float ods = ingester.GetStatValue(HautsDefOf.Hauts_OverdoseSusceptibility);
            __state = new List<float>
            {
                __instance.Props.largeOverdoseChance,
                __instance.Props.overdoseSeverityOffset.min,
                __instance.Props.overdoseSeverityOffset.max
            };
            if (ods != 1f)
            {
                __instance.Props.largeOverdoseChance *= ods;
                float newMin = __instance.Props.overdoseSeverityOffset.min * ods;
                float newMax = __instance.Props.overdoseSeverityOffset.max * ods;
                __instance.Props.overdoseSeverityOffset = new FloatRange(newMin, newMax);
            }
        }
        public static void HautsCompDrug_PostIngestedPostfix(CompDrug __instance, List<float> __state)
        {
            if (!__state.NullOrEmpty())
            {
                __instance.Props.largeOverdoseChance = __state[0];
                float newMin = __state[1];
                float newMax = __state[2];
                __instance.Props.overdoseSeverityOffset = new FloatRange(newMin, newMax);
            }
        }
        //hediff comp functionalities
        public static void HautsSetIdeoPostfix(Pawn_IdeoTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_IdeoTracker), __instance, "pawn") as Pawn;
            if (pawn.ideo != null && pawn.ideo.PreviousIdeos.Count != 0)
            {
                List<Hediff> tmpHediffs = new List<Hediff>();
                tmpHediffs.AddRange(pawn.health.hediffSet.hediffs);
                foreach (Hediff hediff in tmpHediffs)
                {
                    HediffComp_IdeoCertaintySeverity hediffComp_ICS = hediff.TryGetComp<HediffComp_IdeoCertaintySeverity>();
                    if (hediffComp_ICS != null)
                    {
                        if (hediffComp_ICS.Props.removeOnApostasy)
                        {
                            pawn.health.RemoveHediff(hediff);
                        } else if (hediffComp_ICS.Props.changesToThisOnApostasy != null) {
                            Hediff toAdd = HediffMaker.MakeHediff(hediffComp_ICS.Props.changesToThisOnApostasy, pawn, null);
                            pawn.health.AddHediff(toAdd, null, null, null);
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
                }
            }
        }
        public static void HautsResetMaxPostfix(Gene_Resource __instance)
        {
            HautsUtility.ModifyGeneResourceMax(__instance.pawn, __instance);
        }
        public static void HautsOffsetResourcePrefix(IGeneResourceDrain drain, ref float amnt)
        {
            float netGRM = 1f;
            foreach (Hediff h in drain.Pawn.health.hediffSet.hediffs)
            {
                if (h is HediffWithComps hwc)
                {
                    foreach (HediffComp hc in hwc.comps)
                    {
                        if (hc is HediffComp_GeneticResourceModifiers grm && grm.Props.drainRateFactors.ContainsKey(drain.Resource.ResourceLabel))
                        {
                            netGRM += grm.Props.drainRateFactors.TryGetValue(drain.Resource.ResourceLabel);
                        }
                    }
                }
            }
            if (netGRM != 1f && amnt < 0)
            {
                amnt /= Math.Max(0.001f,netGRM);
            }
        }
        public static void HautsFramework_PreApplyDamagePostfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (!absorbed)
            {
                List<HediffComp_PreDamageModification> hcdns = new List<HediffComp_PreDamageModification>();
                foreach (Hediff h in __instance.health.hediffSet.hediffs)
                {
                    if (h is Hediff_PreDamageModification)
                    {
                        HediffComp_PreDamageModification hcdn = h.TryGetComp<HediffComp_PreDamageModification>();
                        if (hcdn != null)
                        {
                            hcdns.Add(hcdn);
                        }
                    }
                }
                if (hcdns.Count > 0)
                {
                    List<HediffComp_PreDamageModification> hcdns2 = hcdns.OrderBy(x => x.Props.priority).ToList();
                    for (int i = hcdns2.Count - 1; i >= 0; i--)
                    {
                        hcdns2[i].TryDoModification(ref dinfo, ref absorbed);
                        if (absorbed)
                        {
                            return;
                        }
                    }
                }
            }
        }
        public static bool Hauts_AddAttachmentPrefix(AttachableThing __instance, Thing newParent)
        {
            if (__instance.def.HasModExtension<BadAttachable>() && newParent is Pawn p)
            {
                if (p.TryGetComp<CompAttachBase>(out CompAttachBase cab))
                {
                    foreach (Hediff h in p.health.hediffSet.hediffs)
                    {
                        HediffComp_DamageNegation hcdn = h.TryGetComp<HediffComp_DamageNegation>();
                        if (hcdn != null && hcdn.Props.removeBadAttachables && hcdn.ShouldPreventAttachment(__instance))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        /*public static void HautsApparelPreventsShootingPostfix(ref bool __result, Verb __instance)
        {
            if (__instance.CasterIsPawn)
            {
                foreach (Hediff h in __instance.CasterPawn.health.hediffSet.hediffs)
                {
                    if (h is Hediff_PreDamageModification)
                    {
                        HediffComp_DamageNegationShield hcdns = h.TryGetComp<HediffComp_DamageNegationShield>();
                        if (hcdns != null && hcdns.Props.blocksRangedWeapons)
                        {
                            __result = true;
                        }
                    }
                }
            }
        }*/
        public static void Hauts_OffsetPsyfocusDirectlyPrefix(Pawn_PsychicEntropyTracker __instance, ref float offset)
        {
            if (offset < 0)
            {
                Pawn pawn = GetInstanceField(typeof(Pawn_PsychicEntropyTracker), __instance, "pawn") as Pawn;
                foreach (Hediff h in pawn.health.hediffSet.hediffs)
                {
                    if (h is HediffWithComps hwc)
                    {
                        HediffComp_PsyfocusSpentTracker pst = hwc.TryGetComp<HediffComp_PsyfocusSpentTracker>();
                        if (pst != null)
                        {
                            pst.UpdatePsyfocusExpenditure(offset);
                        }
                    }
                }
            }
        }
        public static bool HautsPawnPostApplyDamagePrefix(HediffComp_ReactOnDamage __instance, DamageInfo dinfo)
        {
            if (dinfo.Def == DamageDefOf.EMP && !__instance.parent.pawn.Dead && !__instance.parent.pawn.Destroyed)
            {
                foreach (Hediff h in __instance.parent.pawn.health.hediffSet.hediffs)
                {
                    if (h.def.HasModExtension<NoEMPReaction>())
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static void HautsStunNotifyDamageAppliedPostfix(StunHandler __instance, DamageInfo dinfo)
        {
            if (dinfo.Def == DamageDefOf.EMP && __instance.parent is Pawn p && !p.Dead && !p.Destroyed)
            {
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    if (h.def.HasModExtension<NoEMPReaction>())
                    {
                        __instance.StopStun();
                        return;
                    }
                }
            }
        }
        public static void HautsHotReloadPostfix()
        {
            HautsUtility.ApplyAllDamageFactorGroupDefs();
        }
        //ability comps and AbilityCooldownModifier
        public static void HautsActivatePostfix(RimWorld.Ability __instance)
        {
            if ((__instance.def.cooldownTicksRange.min != 0 && __instance.def.cooldownTicksRange.max != 0) || __instance.def.groupDef != null)
            {
                float cooldownModifier = HautsUtility.GetCooldownModifier(__instance);
                int newCooldown = (int)(__instance.CooldownTicksRemaining / Math.Max(0.001f, cooldownModifier));
                HautsUtility.SetNewCooldown(__instance, newCooldown);
            }
        }
        public static void HautsGetCooldownForPawnPostfix(ref int __result, VEF.Abilities.Ability __instance)
        {
            __result /= Mathf.RoundToInt(HautsUtility.GetCooldownModifier(__instance));
        }
        public static void HautsRemoveAbilityPostfix(Pawn_AbilityTracker __instance, RimWorld.AbilityDef def)
        {
            foreach (AbilityCompProperties comp in def.comps)
            {
                CompProperties_AbilityForcedByOtherProperty cRTG = comp as CompProperties_AbilityForcedByOtherProperty;
                if (cRTG != null && __instance.pawn != null)
                {
                    if (__instance.pawn.story != null && cRTG.forcingTraits != null && cRTG.forcingTraits.Count > 0)
                    {
                        foreach (TraitDef td in cRTG.forcingTraits)
                        {
                            if (__instance.pawn.story.traits.HasTrait(td))
                            {
                                __instance.GainAbility(def);
                                return;
                            }
                        }
                    } else if (ModsConfig.BiotechActive && __instance.pawn.genes != null && cRTG.forcingGenes != null && cRTG.forcingGenes.Count > 0) {
                        foreach (GeneDef gd in cRTG.forcingGenes)
                        {
                            if (HautsUtility.AnalogHasActiveGene(__instance.pawn.genes,gd))
                            {
                                __instance.GainAbility(def);
                                return;
                            }
                        }
                    }
                }
            }
        }
        public static void HautsRitualBehaviorWorker_TryExecuteOnPostfix(Precept_Ritual ritual, RitualRoleAssignments assignments)
        {
            AbilityGroupDef useCooldownFromAbilityGroupDef = ritual.def.useCooldownFromAbilityGroupDef;
            if (useCooldownFromAbilityGroupDef != null && useCooldownFromAbilityGroupDef.cooldownTicks > 0 && !useCooldownFromAbilityGroupDef.ritualRoleIds.NullOrEmpty<string>())
            {
                foreach (string roleId in useCooldownFromAbilityGroupDef.ritualRoleIds)
                {
                    if (assignments.AnyPawnAssigned(roleId))
                    {
                        foreach (Pawn pawn3 in assignments.AssignedPawns(roleId))
                        {
                            foreach (RimWorld.Ability ability in pawn3.abilities.AllAbilitiesForReading)
                            {
                                if (ability.def.groupDef != null && ability.def.groupDef == useCooldownFromAbilityGroupDef)
                                {
                                    float cooldownModifier = HautsUtility.GetCooldownModifier(ability);
                                    int newCooldown = (int)(ability.CooldownTicksRemaining / Math.Max(0.001f, cooldownModifier));
                                    HautsUtility.SetNewCooldown(ability, newCooldown);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void HautsRitualOutcomeEffectWorker_Speech_ApplyPostfix(LordJob_Ritual jobRitual)
        {
            if (jobRitual.Ritual.def == PreceptDefOf.ThroneSpeech)
            {
                Pawn organizer = jobRitual.Organizer;
                RimWorld.Ability ability = organizer.abilities.GetAbility(AbilityDefOf.Speech, true);
                RoyalTitle mostSeniorTitle = organizer.royalty.MostSeniorTitle;
                if (ability != null && mostSeniorTitle != null)
                {
                    float cooldownModifier = HautsUtility.GetCooldownModifier(ability);
                    int newCooldown = (int)(ability.CooldownTicksRemaining / Math.Max(0.001f, cooldownModifier));
                    HautsUtility.SetNewCooldown(ability, newCooldown);
                }
            }
        }
        //psycasts that can target psychically deaf pawns
        public static void HautsCanApplyPsycastToPostfix(ref bool __result, Psycast __instance, LocalTargetInfo target)
        {
            if (target.Thing != null && target.Thing is Pawn p && __instance.def.HasModExtension<PsycastCanTargetDeaf>())
            {
                if (p.Faction != null && p.Faction == Faction.OfMechanoids)
                {
                    if (__instance.EffectComps.Any((CompAbilityEffect e) => !e.Props.applicableToMechs))
                    {
                        __result = false;
                    }
                }
                __result = true;
            }
        }
        //melee shot verb
        public static void HautsEffectiveMinRangePostfix(ref float __result, VerbProperties __instance)
        {
            if (typeof(Verb_MeleeShot).IsAssignableFrom(__instance.verbClass))
            {
                __result = __instance.minRange;
            }
        }
        //delayed resurrection that can prevent shambling
        public static void HautsCanResurrectAsShamblerPostfix(ref bool __result, Corpse corpse)
        {
            WorldComponent_HautsDelayedResurrections WCDR = (WorldComponent_HautsDelayedResurrections)Find.World.GetComponent(typeof(WorldComponent_HautsDelayedResurrections));
            foreach (Hauts_DelayedResurrection HDR in WCDR.pawns)
            {
                if (HDR.preventRisingAsShambler && HDR.corpse != null && HDR.corpse == corpse)
                {
                    __result = false;
                    break;
                }
            }
        }
        //for self combat buff verbs
        public static void HautsGetJobPostfix(ref Job __result, RimWorld.Ability __instance)
        {
            if (__instance.verb != null && __instance.verb.verbProps.verbClass == typeof(Verb_CastAbilityCombatSelfBuff))
            {
                __result.targetA = new LocalTargetInfo(__instance.pawn);
            }
        }
        //burgle
        public static IEnumerable<Gizmo> Hauts_Settlement_GetGizmosPostfix(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            if (Find.WorldObjects.AnySettlementAt(__instance.Tile) && HautsUtility.HasAnyBurglars(__instance))
            {
                Settlement settlement = Find.WorldObjects.SettlementAt(__instance.Tile);
                if (settlement.Faction != __instance.Faction && settlement.trader != null)
                {
                    yield return (new Command_Action
                    {
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Trade", true),
                        defaultLabel = "Hauts_BurgleIcon".Translate() + " (" + HautsDefOf.Hauts_PawnAlertLevel.LabelForFullStatList + " " + HautsUtility.SettlementAlertLevel(settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")",
                        defaultDesc = "Hauts_BurgleTooltip".Translate(),
                        action = delegate ()
                        {
                            HautsUtility.Burgle(__instance, settlement);
                        }
                    });
                }
            }
        }
        public static IEnumerable<FloatMenuOption> Hauts_Settlement_GetFloatMenuOptionsPostfix(IEnumerable<FloatMenuOption> __result, Settlement __instance, Caravan caravan)
        {
            foreach (FloatMenuOption floatMenuOption in __result)
            {
                yield return floatMenuOption;
            }
            if (__instance.Faction != null && __instance.Faction != Faction.OfPlayer)
            {

                foreach (FloatMenuOption fmo in CaravanArrivalAction_BurgleSettlement.GetFloatMenuOptions(caravan, __instance))
                {
                    yield return fmo;
                }
            }
        }
        //resurrection in unusual circumstances
        public static void HautsResurrectionPrefix_Interred(Pawn pawn)
        {
            Corpse corpse = pawn.Corpse;
            if (corpse != null && corpse.SpawnedOrAnyParentSpawned && corpse.ParentHolder != null)
            {
                if (corpse.ParentHolder != null)
                {
                    if (corpse.ParentHolder is Thing holder)
                    {
                        if (holder is Building_Casket casket)
                        {
                            casket.EjectContents();
                        } else {
                            Thing newThing;
                            corpse.holdingOwner.TryDrop(corpse, ThingPlaceMode.Near, 1, out newThing, null, null);
                        }
                    }
                }
            }
        }
        public static void HautsResurrectionPrefix_Caravan(Pawn pawn, ref Caravan __state)
        {
            __state = null;
            if (pawn.Dead && !pawn.Discarded)
            {
                Corpse corpse = pawn.Corpse;
                if (corpse != null)
                {
                    foreach (Caravan c in Find.WorldObjects.Caravans)
                    {
                        if (CaravanInventoryUtility.AllInventoryItems(c).Contains(corpse))
                        {
                            __state = c;
                        }
                    }
                }
            }
        }
        public static void HautsResurrectionPostfix_Caravan(bool __result, Pawn pawn, Caravan __state)
        {
            if (__result && !pawn.Dead)
            {
                if (__state != null)
                {
                    __state.AddPawn(pawn, true);
                }
                if (pawn.story != null && pawn.story.traits != null)
                {
                    HautsUtility.TraitGrantedStuffRegeneration(pawn);
                }
            }
        }
        //ai ability use stimulators
        public static void HautsAICanTargetNowPrefix(RimWorld.Ability __instance, LocalTargetInfo target)
        {
            List<CompAbilityEffect> effectComps = __instance.EffectComps;
        }
        public static void HautsAICanTargetNowPostfix(CompAbilityEffect __instance, ref bool __result)
        {
            __result = true;
        }
        //history event def dme
        public static bool HautsTryAffectGoodwillWithPrefix(ref bool __result, Faction __instance, Faction other, int goodwillChange, bool canSendMessage, bool canSendHostilityLetter, HistoryEventDef reason, GlobalTargetInfo? lookTarget)
        {
            if (Current.ProgramState != ProgramState.Playing || reason == null || !reason.HasModExtension<IgnoresNaturalGoodwill>() || goodwillChange == 0)
            {
                return true;
            }
            if (!__instance.CanChangeGoodwillFor(other, goodwillChange))
            {
                __result = false;
            } else {
                int num = __instance.GoodwillWith(other);
                int num2 = __instance.BaseGoodwillWith(other);
                int num3 = Mathf.Clamp(num2 + goodwillChange, -100, 100);
                if (num2 == num3)
                {
                    return true;
                }
                if (reason != null && (__instance.IsPlayer || other.IsPlayer))
                {
                    Faction faction = (__instance.IsPlayer ? other : __instance);
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(reason, faction.Named(HistoryEventArgsNames.AffectedFaction), goodwillChange.Named(HistoryEventArgsNames.CustomGoodwill)), true);
                }
                FactionRelation factionRelation = __instance.RelationWith(other, false);
                factionRelation.baseGoodwill = num3;
                bool flag;
                factionRelation.CheckKindThresholds(__instance, canSendHostilityLetter, (reason != null) ? reason.LabelCap : null, lookTarget ?? GlobalTargetInfo.Invalid, out flag);
                FactionRelation factionRelation2 = other.RelationWith(__instance, false);
                FactionRelationKind kind = factionRelation2.kind;
                factionRelation2.baseGoodwill = factionRelation.baseGoodwill;
                factionRelation2.kind = factionRelation.kind;
                bool flag2;
                if (kind != factionRelation2.kind)
                {
                    other.Notify_RelationKindChanged(__instance, kind, canSendHostilityLetter, (reason != null) ? reason.LabelCap : null, lookTarget ?? GlobalTargetInfo.Invalid, out flag2);
                }
                else
                {
                    flag2 = false;
                }
                int num4 = __instance.GoodwillWith(other);
                if (canSendMessage && num != num4 && !flag && !flag2 && Current.ProgramState == ProgramState.Playing && (__instance.IsPlayer || other.IsPlayer))
                {
                    Faction faction2 = (__instance.IsPlayer ? other : __instance);
                    string name = (string)GetInstanceField(typeof(Faction), faction2, "name");
                    string text;
                    if (reason != null)
                    {
                        text = "MessageGoodwillChangedWithReason".Translate(name, num.ToString("F0"), num4.ToString("F0"), reason.label);
                    } else {
                        text = "MessageGoodwillChanged".Translate(name, num.ToString("F0"), num4.ToString("F0"));
                    }
                    Messages.Message(text, lookTarget ?? GlobalTargetInfo.Invalid, ((float)goodwillChange > 0f) ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent, true);
                }
                __result = true;
            }
            return false;
        }
    }
    [DefOf]
    public static class HautsDefOf
    {
        static HautsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HautsDefOf));
        }
        public static DamageDef Hauts_SkipFrag;
        public static EffecterDef Hauts_ToxThornsMist;

        public static Hauts_FactionCompDef Hauts_FCHolder;

        public static IncidentDef Hauts_InvestmentReturn;

        public static StatDef Hauts_ApparelWearRateFactor;
        public static StatDef Hauts_OverdoseSusceptibility;
        public static StatDef Hauts_BoredomDropPerDay;
        public static StatDef Hauts_PilferingStealth;
        public static StatDef Hauts_MaxPilferingValue;
        public static StatDef Hauts_PawnAlertLevel;
        public static StatDef Hauts_SkillGainFromRecreation;
        public static StatDef Hauts_CaravanVisibilityOffset;
        public static StatDef Hauts_PersonalCaravanVisibilityFactor;
        public static StatDef Hauts_TrackSize;
        public static StatDef Hauts_JumpRangeFactor;
        [MayRequireIdeology]
        public static StatDef Hauts_IdeoAbilityDurationSelf;
        [MayRequireIdeology]
        public static StatDef Hauts_IdeoThoughtFactor;
        [MayRequireIdeology]
        public static StatDef Hauts_MaxDryadFactor;
        [MayRequireBiotech]
        public static StatDef Hauts_InstructiveAbility;
        [MayRequireBiotech]
        public static StatDef Hauts_MechCommandRange;
        [MayRequireBiotech]
        public static StatDef Hauts_SpewRangeFactor;
        [MayRequireBiotech]
        public static StatDef Hauts_HemogenContentFactor;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsycastFocusRefund;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsyfocusFromFood;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsyfocusGainOnKill;
        [MayRequireRoyalty]
        public static StatDef Hauts_PsyfocusRegenRate;
        [MayRequireRoyalty]
        public static StatDef Hauts_TierOnePsycastCostOffset;
        [MayRequireRoyalty]
        public static StatDef Hauts_SkipcastRangeFactor;
        public static StatDef Hauts_BreachDamageFactor;
        [MayRequireAnomaly]
        public static StatDef Hauts_EntityDamageFactor;
        public static StatDef Hauts_MeleeArmorPenetration;
        public static StatDef Hauts_RangedArmorPenetration;

        public static ThingDef Hauts_DefaultAuraGraphic;

        public static HediffDef HVT_Spy;
        public static HediffDef Hauts_PsycastLoopBreaker;
        public static HediffDef Hauts_RaisedAlertLevel;

        public static JobDef Hauts_Pickpocket;
    }
    //traits
    public class TraitGrantedStuff : DefModExtension
    {
        public TraitGrantedStuff()
        {

        }
        public Dictionary<int,List<HediffDef>> grantedHediffs;
        public Dictionary<int,List<HediffDef>> otherHediffsToRemoveOnRemoval;
        public bool hediffsToBrain = false;
        public Dictionary<int,float> prisonerResolveFactor;
        public Dictionary<int,List<RimWorld.AbilityDef>> grantedAbilities;
        public Dictionary<int,List<VEF.Abilities.AbilityDef>> grantedVEFAbilities;
        public Dictionary<BodyTypeDef, BodyTypeDef> forcedBodyTypes;
    }
    public class CannotRemoveBionicsFrom : DefModExtension
    {

    }
    public class ExciseTraitExemption : DefModExtension
    {

    }
    public class VanishOnDeath : DefModExtension
    {
        public VanishOnDeath()
        {

        }
        public SoundDef sound;
        public ThingDef thingLeftBehind;
        public bool triggerOnRemoval;
        public bool skipgateOut;
    }
    public class Hediff_VanishOnDownedToo : HediffWithComps
    {
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.pawn.Downed)
            {
                HautsUtility.TryVanishPawn(this.pawn);
            }
        }
    }
    public class ConceitedTrait : DefModExtension
    {

    }
    public class UnaffectedByDarkness : DefModExtension
    {

    }
    //faction 'comps'
    public class WorldComponent_HautsFactionComps : WorldComponent
    {
        public WorldComponent_HautsFactionComps(World world) : base(world)
        {
            this.world = world;
        }
        public bool TryGetCompsFor(Faction faction)
        {
            foreach (Hauts_FactionCompHolder item in this.factions)
            {
                if (item.factionLoadID == faction.loadID)
                {
                    return true;
                }
            }
            return false;
        }
        public Hauts_FactionCompHolder FindCompsFor(Faction faction)
        {
            Hauts_FactionCompHolder fch = null;
            foreach (Hauts_FactionCompHolder item in this.factions)
            {
                if (item.factionLoadID == faction.loadID)
                {
                    fch = item;
                    break;
                }
            }
            return fch;
        }
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            foreach (Faction f in this.world.factionManager.AllFactionsListForReading)
            {
                bool shouldAdd = true;
                foreach (Hauts_FactionCompHolder fch in this.factions)
                {
                    if (fch.factionLoadID == f.loadID)
                    {
                        shouldAdd = false;
                        break;
                    }
                }
                if (shouldAdd)
                {
                    Hauts_FactionCompHolder newFCH = new Hauts_FactionCompHolder(f);
                    newFCH.PostMake();
                    this.factions.Add(newFCH);
                }
            }
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (GenTicks.TicksGame == 3)
            {
                HautsUtility.TraitGrantedStuffLoadCheck(Find.WorldPawns.AllPawnsAlive);
                for (int i = 0; i <Find.Maps.Count; i++)
                {
                    HautsUtility.TraitGrantedStuffLoadCheck(Find.Maps[i].mapPawns.AllPawns);
                }
                this.ThirdTickEffects();
            }
            for (int j = this.factions.Count - 1; j >= 0; j--)
            {
                this.factions[j].PostTick();
            }
        }
        public void ThirdTickEffects()
        {
            //you put Harmony patches in here to also go off at the same time as the initial traitgrantedstuff load check
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Hauts_FactionCompHolder>(ref this.factions, "factions", LookMode.Deep, Array.Empty<object>());
        }
        public List<Hauts_FactionCompHolder> factions = new List<Hauts_FactionCompHolder>();
    }
    public class Hauts_FactionCompHolder : IExposable
    {
        public Hauts_FactionCompHolder()
        {
            this.def = HautsDefOf.Hauts_FCHolder;
        }
        public Hauts_FactionCompHolder(Faction faction)
        {
            this.factionLoadID = faction.loadID;
            this.def = HautsDefOf.Hauts_FCHolder;
        }
        public void PostMake()
        {
            this.InitializeComps();
            for (int i = this.comps.Count - 1; i >= 0; i--)
            {
                try
                {
                    this.comps[i].CompPostMake();
                }
                catch (Exception arg)
                {
                    Log.Error("Error in HautsFactionComp.CompPostMake(): " + arg);
                    this.comps.RemoveAt(i);
                }
            }
        }
        private void InitializeComps()
        {
            if (this.def.comps != null)
            {
                this.comps = new List<HautsFactionComp>();
                for (int i = 0; i < this.def.comps.Count; i++)
                {
                    HautsFactionComp hautsFactionComp = null;
                    try
                    {
                        hautsFactionComp = (HautsFactionComp)Activator.CreateInstance(this.def.comps[i].compClass);
                        hautsFactionComp.props = this.def.comps[i];
                        hautsFactionComp.parent = this;
                        this.comps.Add(hautsFactionComp);
                    } catch (Exception arg) {
                        Log.Error("Could not instantiate or initialize a HautsFactionComp: " + arg);
                        this.comps.Remove(hautsFactionComp);
                    }
                }
            }
        }
        public T TryGetComp<T>() where T : HautsFactionComp
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    T t = this.comps[i] as T;
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            return default(T);
        }
        public void PostTick()
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    this.comps[i].CompPostTick();
                }
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.factionLoadID, "factionLoadID", 0);
            Scribe_Defs.Look<Hauts_FactionCompDef>(ref this.def, "def");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.InitializeComps();
            }
            for (int i = 0; i < this.comps.Count; i++)
            {
                this.comps[i].CompExposeData();
            }
        }
        public int factionLoadID;
        public Hauts_FactionCompDef def;
        public List<HautsFactionComp> comps = new List<HautsFactionComp>();
    }
    public class Hauts_FactionCompDef : Def
    {
        public bool HasComp(Type compClass)
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    if (this.comps[i].compClass == compClass)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public HautsFactionCompProperties CompPropsFor(Type compClass)
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    if (this.comps[i].compClass == compClass)
                    {
                        return this.comps[i];
                    }
                }
            }
            return null;
        }
        public T CompProps<T>() where T : HautsFactionCompProperties
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    T t = this.comps[i] as T;
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            return default(T);
        }
        public override void ResolveReferences()
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    this.comps[i].ResolveReferences(this);
                }
            }
        }
        public List<HautsFactionCompProperties> comps;
    }
    public class HautsFactionComp
    {
        public int factionLoadID
        {
            get
            {
                return this.parent.factionLoadID;
            }
        }
        public Faction ThisFaction
        {
            get
            {
                foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                {
                    if (f.loadID == this.factionLoadID)
                    {
                        return f;
                    }
                }
                return null;
            }
        }
        public Hauts_FactionCompDef Def
        {
            get
            {
                return this.parent.def;
            }
        }
        public virtual void CompPostMake()
        {
        }
        public virtual void CompPostTick()
        {
        }
        public virtual void CompExposeData()
        {
        }
        //may add other functions, like Notify_FactionDestroyed, or leader died, or whatever
        public Hauts_FactionCompHolder parent;
        public HautsFactionCompProperties props;
    }
    public class HautsFactionCompProperties
    {
        public virtual void PostLoad()
        {
        }
        public virtual void ResolveReferences(Hauts_FactionCompDef parent)
        {
        }
        public virtual IEnumerable<string> ConfigErrors(Hauts_FactionCompDef parentDef)
        {
            if (this.compClass == null)
            {
                yield return "compClass is null";
            }
            int num;
            for (int i = 0; i < parentDef.comps.Count; i = num + 1)
            {
                if (parentDef.comps[i] != this && parentDef.comps[i].compClass == this.compClass)
                {
                    yield return "two comps with same compClass: " + this.compClass;
                }
                num = i;
            }
            yield break;
        }
        [TranslationHandle]
        public Type compClass;
    }
    public class HautsFactionCompProperties_SpyPoints : HautsFactionCompProperties
    {
        public HautsFactionCompProperties_SpyPoints()
        {
            this.compClass = typeof(HautsFactionComp_SpyPoints);
        }
        public int spyPoints;
    }
    public class HautsFactionComp_SpyPoints : HautsFactionComp
    {
        public HautsFactionCompProperties_SpyPoints Props
        {
            get
            {
                return (HautsFactionCompProperties_SpyPoints)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.spyPoints = this.Props.spyPoints;
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.spyPoints, "spyPoints", 0, false);
        }
        public int spyPoints;
    }
    public class HautsFactionCompProperties_BurglaryResponse : HautsFactionCompProperties
    {
        public HautsFactionCompProperties_BurglaryResponse()
        {
            this.compClass = typeof(HautsFactionComp_BurglaryResponse);
        }
        public float initialAlertLevel;
        public Dictionary<FactionDef, float> specificFactionMinAlertLevels;
        public Dictionary<TechLevel,float> minAlertLevelPerTechLevel;
        public float alertDecayPerDay;
        public float advancedDecayThreshold;
        public float advancedDecayPerDayPct;
        public float minAlertGainFromBurgle;
        public float alertGainPerMarketValueStolen;
    }
    public class HautsFactionComp_BurglaryResponse : HautsFactionComp
    {
        public HautsFactionCompProperties_BurglaryResponse Props
        {
            get
            {
                return (HautsFactionCompProperties_BurglaryResponse)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.currentAlertLevel = this.Props.initialAlertLevel;
        }
        public override void CompPostTick()
        {
            base.CompPostTick();
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                if (this.currentAlertLevel > this.Props.advancedDecayThreshold)
                {
                    this.currentAlertLevel = Math.Max(this.currentAlertLevel-this.Props.alertDecayPerDay,Math.Max(this.Props.advancedDecayThreshold,this.currentAlertLevel-(this.currentAlertLevel*(this.Props.advancedDecayPerDayPct / 24f))));
                } else {
                    this.currentAlertLevel -= this.Props.alertDecayPerDay / 24f;
                }
                if (this.currentAlertLevel < 0f)
                {
                    this.currentAlertLevel = 0f;
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.currentAlertLevel, "currentAlertLevel", 0f, false);
        }
        public float currentAlertLevel;
    }
    [Obsolete]
    public class Hauts_SpyHediff : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.spyingOnFaction == null)
            {
                this.spyingOnFaction = Faction.OfPlayerSilentFail;
            }
            if (this.spyingForFaction == null)
            {
                this.spyingForFaction = this.pawn.Faction;
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.Faction == this.spyingOnFaction)
            {
                this.Severity = -1f;
            } else if (this.pawn.IsWorldPawn() && !this.pawn.Dead) {
                if (!this.pawn.IsPrisonerOfColony)
                {
                    WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(this.spyingForFaction);
                    if (fch != null)
                    {
                        HautsFactionComp_SpyPoints spyPoints = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                        if (spyPoints != null)
                        {
                            int addedSpyPoints = (int)(153.5 * this.pawn.skills.GetSkill(SkillDefOf.Intellectual).Level * this.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight));
                            spyPoints.spyPoints += addedSpyPoints + 2;
                        }
                    }
                    this.Severity = -1f;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.spyingOnFaction, "spyingOnFaction", false);
            Scribe_References.Look<Faction>(ref this.spyingForFaction, "spyingForFaction", false);
        }
        public Faction spyingOnFaction;
        public Faction spyingForFaction;
    }
    public enum SpyPointAttribution : byte
    {
        OwnFaction,
        AllPermaHostile,
        AllHostile,
        RandomHostileFactions,
        All
    }
    public class HediffCompProperties_Espionage : HediffCompProperties
    {
        public HediffCompProperties_Espionage()
        {
            this.compClass = typeof(HediffComp_Espionage);
        }
        public float baseSpyPoints;
        public int unscalableFlatSpyPoints;
        public List<SkillDef> relevantSkills;
        public float fallbackIfNoSkillLevel = 1f;
        public List<PawnCapacityDef> relevantCapacities;
        public int randomFactionCount;
        public SpyPointAttribution spyPointAttribution = SpyPointAttribution.OwnFaction;
    }
    public class HediffComp_Espionage : HediffComp
    {
        public HediffCompProperties_Espionage Props
        {
            get
            {
                return (HediffCompProperties_Espionage)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.spyingOnFaction == null)
            {
                this.spyingOnFaction = Faction.OfPlayerSilentFail;
            }
            if (this.spyingForFaction == null)
            {
                this.spyingForFaction = this.Pawn.Faction;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.Faction == this.spyingOnFaction)
            {
                this.Pawn.health.RemoveHediff(this.parent);
                return;
            }
            if (this.Pawn.IsWorldPawn() && !this.Pawn.Dead) {
                if (!this.Pawn.IsPrisonerOfColony)
                {
                    int spyPointsToGain = this.SpyPointsToGain();
                    WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                    Faction playerF = Faction.OfPlayerSilentFail;
                    switch (this.Props.spyPointAttribution)
                    {
                        case SpyPointAttribution.OwnFaction:
                            this.GrantSpyPoints(WCFC,this.spyingForFaction);
                            break;
                        case SpyPointAttribution.AllPermaHostile:
                            foreach (Faction f in Find.FactionManager.AllFactions)
                            {
                                if (f != playerF && f.def.PermanentlyHostileTo(playerF.def) && f.HostileTo(playerF))
                                {
                                    this.GrantSpyPoints(WCFC, f);
                                }
                            }
                            break;
                        case SpyPointAttribution.AllHostile:
                            foreach (Faction f in Find.FactionManager.AllFactions)
                            {
                                if (f != playerF && f.HostileTo(playerF))
                                {
                                    this.GrantSpyPoints(WCFC, f);
                                }
                            }
                            break;
                        case SpyPointAttribution.RandomHostileFactions:
                            int randomCount = Math.Max(1,this.Props.randomFactionCount);
                            foreach (Faction f in Find.FactionManager.AllFactions.InRandomOrder())
                            {
                                if (f != playerF)
                                {
                                    this.GrantSpyPoints(WCFC, f);
                                }
                                randomCount--;
                                if (randomCount <= 0)
                                {
                                    break;
                                }
                            }
                            break;
                        case SpyPointAttribution.All:
                            foreach (Faction f in Find.FactionManager.AllFactions)
                            {
                                this.GrantSpyPoints(WCFC, f);
                            }
                            break;
                        default:
                            this.GrantSpyPoints(WCFC, this.spyingForFaction);
                            break;
                    }
                    this.Pawn.health.RemoveHediff(this.parent);
                }
            }
        }
        public virtual int SpyPointsToGain()
        {
            float addedSpyPoints = this.Props.baseSpyPoints;
            if (!this.Props.relevantSkills.NullOrEmpty())
            {
                float sumSkilllevel = this.Props.fallbackIfNoSkillLevel;
                if (this.Pawn.skills != null)
                {
                    foreach (SkillDef sd in this.Props.relevantSkills)
                    {
                        sumSkilllevel += this.Pawn.skills.GetSkill(sd).Level;
                    }
                }
                addedSpyPoints *= sumSkilllevel;
            }
            if (!this.Props.relevantCapacities.NullOrEmpty())
            {
                float sumCapLevel = 0f;
                foreach (PawnCapacityDef pcd in this.Props.relevantCapacities)
                {
                    sumCapLevel += this.Pawn.health.capacities.GetLevel(pcd);
                }
                addedSpyPoints *= sumCapLevel;
            }
            return (int)addedSpyPoints + this.Props.unscalableFlatSpyPoints;
        }
        public virtual void GrantSpyPoints(WorldComponent_HautsFactionComps WCFC, Faction f)
        {
            Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
            if (fch != null)
            {
                HautsFactionComp_SpyPoints spyPoints = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                if (spyPoints != null)
                {
                    spyPoints.spyPoints += this.SpyPointsToGain();
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Faction>(ref this.spyingOnFaction, "spyingOnFaction", false);
            Scribe_References.Look<Faction>(ref this.spyingForFaction, "spyingForFaction", false);
        }
        public Faction spyingOnFaction;
        public Faction spyingForFaction;
    }
    //stats
    public class AbilityStatEffecters : DefModExtension
    {
        public AbilityStatEffecters()
        {
        }
        public bool skip = false;
        public bool leap = false;
    }
    public class SkillNeed_BaseBonusAWRF : SkillNeed_BaseBonus
    {
        public override float ValueFor(Pawn pawn)
        {
            if (Hauts_Mod.settings.apparelWearRateCrafting)
            {
                return base.ValueFor(pawn);
            }
            return 0f;
        }
    }
    public class SkillNeed_BaseBonusBDF : SkillNeed_BaseBonus
    {
        public override float ValueFor(Pawn pawn)
        {
            if (Hauts_Mod.settings.breachDamageConstruction)
            {
                return base.ValueFor(pawn);
            }
            return 0f;
        }
    }
    public class StatPart_BoredomExpectationBand : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            val += ExpectationsUtility.CurrentExpectationFor(pawn).joyToleranceDropPerDay;
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return null;
            }
            return "Hauts_StatWorkerExpectationLevel".Translate() + ": " + (ExpectationsUtility.CurrentExpectationFor(pawn).joyToleranceDropPerDay).ToStringPercent();
        }
    }
    public class SkillNeed_BaseBonusOS : SkillNeed_BaseBonus
    {
        public override float ValueFor(Pawn pawn)
        {
            if (Hauts_Mod.settings.overdoseSusceptibilityMedicine)
            {
                return base.ValueFor(pawn);
            }
            return 0f;
        }
    }
    public class SkillNeed_BaseBonusPS : SkillNeed_BaseBonus
    {
        public override float ValueFor(Pawn pawn)
        {
            if (Hauts_Mod.settings.pilferingStealthSocial)
            {
                return base.ValueFor(pawn);
            }
            return 1f;
        }
    }
    public class StatPart_PilferingStealth : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            if (this.multiplyByLackOf != null)
            {
                foreach (StatDef sd in this.multiplyByLackOf)
                {
                    val *= this.LackOfFactor(pawn, sd);
                }
            }
            if (val < this.invisibilityMinimum && pawn.IsPsychologicallyInvisible())
            {
                val = this.invisibilityMinimum;
            }
            if (this.TryGetBodySize(req, out float num))
            {
                val /= Math.Max(0.01f,num);
            }
        }
        public float LackOfFactor(Pawn p, StatDef sd)
        {
            return Math.Max(this.minimumLackOf, 1f - p.GetStatValue(sd));
        }
        private bool TryGetBodySize(StatRequest req, out float bodySize)
        {
            return PawnOrCorpseStatUtility.TryGetPawnOrCorpseStat(req, (Pawn x) => x.BodySize, (ThingDef x) => x.race.baseBodySize, out bodySize);
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return null;
            }
            string descKey = "";
            if (this.TryGetBodySize(req, out float num))
            {
                descKey += "StatsReport_BodySize".Translate(num.ToString("F2")) + ": /" + num.ToStringPercent() + "\n";
            }
            if (this.multiplyByLackOf != null)
            {
                foreach (StatDef sd in this.multiplyByLackOf)
                {
                    descKey += "Hauts_StatWorkerLackOfFactor".Translate(sd.LabelCap,this.minimumLackOf.ToStringByStyle(ToStringStyle.FloatTwo)) + ": " + this.LackOfFactor(pawn,sd).ToStringByStyle(ToStringStyle.FloatTwo) + "\n";
                }
                if (pawn.IsPsychologicallyInvisible())
                {
                    descKey += "Hauts_StatWorkerIsInvisible".Translate(this.invisibilityMinimum);
                }
                return descKey;
            }
            return null;
        }
        public List<StatDef> multiplyByLackOf;
        public float minimumLackOf;
        public float invisibilityMinimum;
    }
    public class StatPart_PilferingYield : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            val /= this.divideBy;
            float meleeDmgFactor = 0f;
            val *= pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) + (pawn.GetStatValue(StatDefOf.MoveSpeed) / 4.6f) * (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) + (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) / 2)) * meleeDmgFactor;
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return null;
            }
            if (this.divideBy != 1f)
            {
                return "/ " + this.divideBy + "\n";
            }
            return null;
        }
        public override bool ForceShow(StatRequest req)
        {
            if (req.Thing != null && req.Thing is Pawn p)
            {
                return p.GetStatValue(HautsDefOf.Hauts_PilferingStealth) > 0f;
            }
            return false;
        }
        public float divideBy;
    }
    public class StatPart_PawnAlertLevel : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            val *= 1f +pawn.GetStatValue(HautsDefOf.Hauts_PilferingStealth);
        }
        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }
    }
    public class StatPart_PsyfocusBand : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || pawn.psychicEntropy == null || pawn.GetPsylinkLevel() == 0 || pawn.psychicEntropy.IsCurrentlyMeditating)
            {
                return;
            }
            val -= Pawn_PsychicEntropyTracker.FallRatePerPsyfocusBand[pawn.psychicEntropy.PsyfocusBand];
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || pawn.psychicEntropy == null || pawn.GetPsylinkLevel() == 0 || pawn.psychicEntropy.IsCurrentlyMeditating)
            {
                return null;
            }
            return "Hauts_StatWorkerPsyfocusBand".Translate() + ": " + (-1f * Pawn_PsychicEntropyTracker.FallRatePerPsyfocusBand[pawn.psychicEntropy.PsyfocusBand]).ToStringPercent();
        }
    }
    public class StatPart_OwnStatOffset : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            val += pawn.GetStatValue(this.stat);
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || pawn.psychicEntropy == null)
            {
                return null;
            }
            return this.label + ": +" + pawn.GetStatValue(this.stat).ToStringPercent();
        }
        private readonly StatDef stat;
        [MustTranslate]
        private readonly string label;
    }
    public class CompProperties_AbilityFireSpewScalable : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityFireSpewScalable()
        {
            this.compClass = typeof(CompAbilityEffect_FireSpewScalable);
        }
        public float range;
        public float lineWidthEnd;
        public ThingDef filthDef;
        public float filthChance = 1f;
        public int damAmount = -1;
        public EffecterDef effecterDef;
        public bool canHitFilledCells;
    }
    public class CompAbilityEffect_FireSpewScalable : CompAbilityEffect
    {
        private new CompProperties_AbilityFireSpewScalable Props
        {
            get
            {
                return (CompProperties_AbilityFireSpewScalable)this.props;
            }
        }
        private Pawn Pawn
        {
            get
            {
                return this.parent.pawn;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            IntVec3 cell = target.Cell;
            Map mapHeld = this.parent.pawn.MapHeld;
            float num = 0f;
            DamageDef flame = DamageDefOf.Flame;
            Thing pawn = this.Pawn;
            GenExplosion.DoExplosion(cell, mapHeld, num, flame, pawn, this.Props.damAmount, -1f, null, null, null, null, this.Props.filthDef, this.Props.filthChance, 1, null, null, 255, false, null, 0f, 1, 1f, false, null, null, null, false, 0.6f, 0f, false, null, 1f, this.parent.verb.verbProps.flammabilityAttachFireChanceCurve, this.AffectedCells(target), null, null);
            base.Apply(target, dest);
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (this.Props.effecterDef != null)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                    {
                        this.parent.AddEffecterToMaintain(this.Props.effecterDef.Spawn(this.parent.pawn.Position, a.Cell, this.parent.pawn.Map, 1f), this.Pawn.Position, a.Cell, 17, this.Pawn.MapHeld);
                    },
                    ticksAwayFromCast = 17
                };
            }
            yield break;
        }
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(this.AffectedCells(target));
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (this.Pawn.Faction != null)
            {
                foreach (IntVec3 intVec in this.AffectedCells(target))
                {
                    List<Thing> thingList = intVec.GetThingList(this.Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == this.Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }
        private float Range
        {
            get
            {
                return this.Props.range * ((this.parent.def.HasModExtension<Hauts_SpewAbility>() && ModsConfig.BiotechActive) ? this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) : 1f);
            }
        }
        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            this.tmpCells.Clear();
            Vector3 vector = this.Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 intVec = target.Cell.ClampInsideMap(this.Pawn.Map);
            if (this.Pawn.Position == intVec)
            {
                return this.tmpCells;
            }
            float lengthHorizontal = (intVec - this.Pawn.Position).LengthHorizontal;
            float num = (float)(intVec.x - this.Pawn.Position.x) / lengthHorizontal;
            float num2 = (float)(intVec.z - this.Pawn.Position.z) / lengthHorizontal;
            intVec.x = Mathf.RoundToInt((float)this.Pawn.Position.x + num * this.Range);
            intVec.z = Mathf.RoundToInt((float)this.Pawn.Position.z + num2 * this.Range);
            float num3 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
            float num4 = this.Props.lineWidthEnd * ((this.parent.def.HasModExtension<Hauts_SpewAbility>() && ModsConfig.BiotechActive) ? (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) / (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) > 1.5f ? 1.5f : 1f)) : 1f) / 2f;
            float num5 = Mathf.Sqrt(Mathf.Pow((intVec - this.Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num4, 2f));
            float num6 = 57.29578f * Mathf.Asin(num4 / num5);
            int num7 = GenRadial.NumCellsInRadius(this.Range);
            for (int i = 0; i < num7; i++)
            {
                IntVec3 intVec2 = this.Pawn.Position + GenRadial.RadialPattern[i];
                if (this.CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), num3)) <= num6)
                {
                    this.tmpCells.Add(intVec2);
                }
            }
            List<IntVec3> list = GenSight.BresenhamCellsBetween(this.Pawn.Position, intVec);
            for (int j = 0; j < list.Count; j++)
            {
                IntVec3 intVec3 = list[j];
                if (!this.tmpCells.Contains(intVec3) && this.CanUseCell(intVec3))
                {
                    this.tmpCells.Add(intVec3);
                }
            }
            return this.tmpCells;
        }
        [CompilerGenerated]
        private bool CanUseCell(IntVec3 c)
        {
            ShootLine shootLine;
            return c.InBounds(this.Pawn.Map) && !(c == this.Pawn.Position) && (this.Props.canHitFilledCells || !c.Filled(this.Pawn.Map)) && c.InHorDistOf(this.Pawn.Position, this.Range) && this.parent.verb.TryFindShootLineFromTo(this.parent.pawn.Position, c, out shootLine, false);
        }

        // Token: 0x04004219 RID: 16921
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();
    }
    public class Hauts_SpewAbility : DefModExtension
    {
        public Hauts_SpewAbility()
        {
        }
    }
    public class Recipe_ExtractHemogenStatScalable : Recipe_ExtractHemogen
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (!ModLister.CheckBiotech("Hemogen extraction"))
            {
                return;
            }
            if (!this.PawnHasEnoughBloodForExtraction(pawn))
            {
                Messages.Message("MessagePawnHadNotEnoughBloodToProduceHemogenPack".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NeutralEvent, true);
                return;
            }
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn, null);
            hediff.Severity = 0.45f/pawn.GetStatValue(HautsDefOf.Hauts_HemogenContentFactor);
            pawn.health.AddHediff(hediff, null, null, null);
            this.OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
            if (this.IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                base.ReportViolation(pawn, billDoer, pawn.HomeFaction, -1, HistoryEventDefOf.ExtractedHemogenPack);
            }
        }
        private bool PawnHasEnoughBloodForExtraction(Pawn pawn)
        {
            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss, false);
            return firstHediffOfDef == null || firstHediffOfDef.Severity < 0.45f;
        }
    }
    public class HediffCompProperties_MCR_Storage : HediffCompProperties
    {
        public HediffCompProperties_MCR_Storage()
        {
            this.compClass = typeof(HediffComp_MCR_Storage);
        }
    }
    public class HediffComp_MCR_Storage : HediffComp
    {
        public HediffCompProperties_MCR_Storage Props
        {
            get
            {
                return (HediffCompProperties_MCR_Storage)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.RedetermineMCR();
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250,delta))
            {
                this.RedetermineMCR();
            }
        }
        public void RedetermineMCR()
        {
            if (ModsConfig.BiotechActive)
            {
                this.mechCommandRadius = this.Pawn.GetStatValue(HautsDefOf.Hauts_MechCommandRange);
            } else {
                this.mechCommandRadius = 25f;
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
        }
        public float mechCommandRadius = 25f;
    }
    //hediffcomps misc.
    public class Hediff_PsycastLoopBreaker : Hediff
    {
        public override void PostTick()
        {
            base.PostTick();
            this.pawn.health.RemoveHediff(this);
        }
    }
    public class HediffCompProperties_Aura : HediffCompProperties
    {
        public HediffCompProperties_Aura()
        {
            this.compClass = typeof(HediffComp_Aura);
        }
        public float range;
        public float bonusRangePerSeverity;
        public int tickPeriodicity = 15;
        public StatDef rangeModifier = null;
        public float maxRangeModifier = 1f;
        public float minRangeModifier = 1f;
        public Color color;
        public bool affectsSelf = true;
        public bool affectsHostiles;
        public bool affectsAllies;
        public bool affectsMechs = true;
        public bool affectsDrones = true;
        public bool affectsFleshies = true;
        public bool affectsEntities = true;
        public bool mutantsAreEntities = true;
        public bool affectsOthersInCaravan = true;
        public bool disappearsWhileDowned = true;
        public FloatRange functionalSeverity = new FloatRange(-999f, 99999f);
        public bool scanByPawnListerNotByGrid = true;
        public ThingDef mote;
        public bool canToggleVisualization;
        public string visIcon = "Other/ShieldBubble";
        public string visLabel;
        public string visTooltip;
        public string visTooltipFantasy;
    }
    public class HediffComp_Aura : HediffComp
    {
        public HediffCompProperties_Aura Props
        {
            get
            {
                return (HediffCompProperties_Aura)this.props;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (this.Props.canToggleVisualization)
            {
                if (this.uiIcon == null)
                {
                    this.uiIcon = ContentFinder<Texture2D>.Get(this.Props.visIcon, true);
                    this.buttonLabel = this.Props.visLabel;
                    this.buttonTooltip = (HautsUtility.IsHighFantasy() ? this.Props.visTooltipFantasy : this.Props.visTooltip);
                }
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = this.buttonLabel;
                command_Action.defaultDesc = this.buttonTooltip;
                command_Action.icon = this.uiIcon;
                if (Find.Selector.NumSelected > 1)
                {
                    Command_Action command_Action2 = command_Action;
                    command_Action2.defaultLabel = command_Action2.defaultLabel + " (" + this.Pawn.LabelShort + ")";
                }
                command_Action.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    Action action0 = delegate
                    {
                        this.visSetting = AuraVisSetting.Enabled;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting0".Translate(), action0, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Action action1 = delegate
                    {
                        this.visSetting = AuraVisSetting.WhileDrafted;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting1".Translate(), action1, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Action action2 = delegate
                    {
                        this.visSetting = AuraVisSetting.WhileSelected;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting2".Translate(), action2, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Action action3 = delegate
                    {
                        this.visSetting = AuraVisSetting.Disabled;
                    };
                    list.Add(new FloatMenuOption("Hauts_AuraVisSetting3".Translate(), action3, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                yield return command_Action;
                yield break;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                return "Hauts_AuraTooltip".Translate(Mathf.RoundToInt(this.FunctionalRange));
            }
        }
        public virtual float FunctionalRange
        {
            get
            {
                if (this.Props.rangeModifier != null)
                {
                    return (this.Props.range + (this.Props.bonusRangePerSeverity*this.parent.Severity)) * Math.Min(this.Props.maxRangeModifier,Math.Max(this.Props.minRangeModifier,this.parent.pawn.GetStatValue(this.Props.rangeModifier)));
                }
                return this.Props.range + (this.Props.bonusRangePerSeverity * this.parent.Severity);
            }
        }
        public virtual bool ShouldBeActive
        {
            get
            {
                return !this.Pawn.Downed || !this.Props.disappearsWhileDowned || !this.Props.functionalSeverity.Includes(this.parent.Severity);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Props.mote != null)
            {
                if (this.Pawn.Spawned)
                {
                    if (this.ShouldBeActive && (this.visSetting == AuraVisSetting.Enabled || (this.Pawn.Drafted && this.visSetting == AuraVisSetting.WhileDrafted) || (Find.Selector.SelectedPawns.Contains(this.Pawn) && this.visSetting == AuraVisSetting.WhileSelected)))
                    {
                        if (this.mote == null || this.mote.Destroyed)
                        {
                            this.mote = (MoteThrownAttached_Aura)MoteMaker.MakeAttachedOverlay(base.Pawn, this.Props.mote, Vector3.zero, 2 * this.FunctionalRange, -1f);
                            this.mote.instanceColor = this.Props.color;
                            this.mote.range = this.FunctionalRange;
                            if (this.Pawn.IsHashIntervalTick(10))
                            {
                                this.mote.link1.UpdateDrawPos();
                            }
                        } else {
                            this.mote.range = this.FunctionalRange;
                            this.mote.Maintain();
                        }
                    }
                } else if ((this.mote != null && !this.mote.Destroyed)) {
                    this.mote.Destroy(DestroyMode.Vanish);
                }
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn pawn = this.parent.pawn;
            if (!pawn.IsHashIntervalTick(this.Props.tickPeriodicity, delta) || !this.ShouldBeActive)
            {
                return;
            }
            if (pawn.Spawned && !pawn.IsPlayerControlled)
            {
                this.visSetting = AuraVisSetting.Enabled;
            }
            if (this.Props.affectsSelf)
            {
                this.AffectSelf();
            }
            if (pawn.Spawned)
            {
                List<Pawn> pawns = new List<Pawn>();
                if (this.Props.scanByPawnListerNotByGrid)
                {
                    pawns = (List<Pawn>)pawn.Map.mapPawns.AllPawnsSpawned;
                } else {
                    pawns = GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.FunctionalRange, true).OfType<Pawn>().Distinct<Pawn>().ToList();
                }
                this.AffectPawns(pawn, pawns);
                return;
            }
            Caravan caravan = pawn.GetCaravan();
            if (caravan != null)
            {
                this.AffectPawns(pawn, caravan.pawns.InnerListForReading, true);
            }
        }
        protected virtual void AffectPawns(Pawn p, List<Pawn> pawns, bool inCaravan = false)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && this.ValidatePawn(p, pawn, inCaravan))
                {
                    AffectPawn(p, pawn);
                }
            }
        }
        public virtual void AffectSelf() { }
        public virtual void AffectPawn(Pawn self, Pawn pawn) { }
        public virtual bool ValidatePawn(Pawn self, Pawn p, bool inCaravan)
        {
            if (p == self)
            {
                return false;
            }
            if (!this.Props.affectsMechs && p.RaceProps.IsMechanoid)
            {
                return false;
            }
            if (!this.Props.affectsDrones && p.RaceProps.IsDrone)
            {
                return false;
            }
            if (!this.Props.affectsFleshies && p.RaceProps.IsFlesh)
            {
                return false;
            }
            if (!this.Props.affectsEntities && (p.RaceProps.IsAnomalyEntity || (this.Props.mutantsAreEntities && p.IsMutant)))
            {
                return false;
            }
            if (p.HostileTo(self) || self.HostileTo(p))
            {
                if (!this.Props.affectsHostiles)
                {
                    return false;
                }
            } else if (!this.Props.affectsAllies) {
                return false;
            }
            if (inCaravan) {
                if (!this.Props.affectsOthersInCaravan)
                {
                    return false;
                }
            } else if (self.Spawned && p.Position.DistanceTo(self.Position) > this.FunctionalRange) {
                return false;
            }
            return true;
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (this.mote != null && !this.mote.Destroyed)
            {
                this.mote.Destroy(DestroyMode.Vanish);
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            if (this.Props.canToggleVisualization)
            {
                Scribe_Values.Look<AuraVisSetting>(ref this.visSetting, "AuraVisSetting", AuraVisSetting.Enabled, false);
                Scribe_Values.Look<string>(ref this.buttonLabel, "buttonLabel", this.Props.visLabel, false);
                Scribe_Values.Look<string>(ref this.buttonTooltip, "buttonTooltip", this.Props.visTooltip, false);
            }
        }
        public MoteThrownAttached_Aura mote;
        public AuraVisSetting visSetting = AuraVisSetting.Enabled;
        Texture2D uiIcon;
        string buttonLabel;
        string buttonTooltip;
    }
    public enum AuraVisSetting : short
    {
        Enabled,
        WhileDrafted = 8,
        WhileSelected = 16,
        Disabled = 24
    }
    public class HediffCompProperties_AuraHediff : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraHediff()
        {
            this.compClass = typeof(HediffComp_AuraHediff);
        }
        public List<HediffDef> hediffs;
    }
    public class HediffComp_AuraHediff : HediffComp_Aura
    {
        public new HediffCompProperties_AuraHediff Props
        {
            get
            {
                return (HediffCompProperties_AuraHediff)this.props;
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            foreach (HediffDef h in this.Props.hediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(h, this.Pawn, null);
                hediff.Severity = this.HediffSeverity(this.Pawn, h);
                this.parent.pawn.health.AddHediff(hediff, null);
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            foreach (HediffDef h in this.Props.hediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(h, pawn, null);
                hediff.Severity = this.HediffSeverity(pawn,h);
                pawn.health.AddHediff(hediff, null);
            }
        }
        public virtual float HediffSeverity(Pawn p, HediffDef h)
        {
            return h.initialSeverity;
        }
    }
    public class HediffCompProperties_AuraThought : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraThought()
        {
            this.compClass = typeof(HediffComp_AuraThought);
        }
        public List<ThoughtDef> thoughts;
    }
    public class HediffComp_AuraThought : HediffComp_Aura
    {
        public new HediffCompProperties_AuraThought Props
        {
            get
            {
                return (HediffCompProperties_AuraThought) this.props;
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            if (this.parent.pawn.needs.mood != null && this.parent.pawn.needs.mood.thoughts != null)
            {
                foreach (ThoughtDef t in this.Props.thoughts)
                {
                    Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(t);
                    if (!thought.def.IsSocial)
                    {
                        this.parent.pawn.needs.mood.thoughts.memories.TryGainMemory(thought, null);
                    }
                }
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (pawn.needs.mood != null && pawn.needs.mood.thoughts != null)
            {
                foreach (ThoughtDef t in this.Props.thoughts)
                {
                    Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(t);
                    if (thought.def.IsSocial)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(thought, self);
                    } else {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(thought, null);
                    }
                }
            }
        }
    }
    public class MoteThrownAttached_Aura : MoteThrown
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.link1.Linked)
            {
                drawVal = 2 * this.range;
                this.attacheeLastPosition = this.link1.LastDrawPos;
            }
            this.exactPosition += this.def.mote.attachedDrawOffset;
        }
        protected override void TimeInterval(float deltaTime)
        {
            deltaTime = 0.01f;
            switch (Find.TickManager.CurTimeSpeed)
            {
                case TimeSpeed.Fast:
                    deltaTime /= 3f;
                    break;
                case TimeSpeed.Superfast:
                    deltaTime /= 6f;
                    break;
                case TimeSpeed.Ultrafast:
                    deltaTime /= 15f;
                    break;
                default:
                    break;
            }
            base.TimeInterval(deltaTime);
            if (Rand.Value <= 0.99f)
            {
                if (dilating)
                {
                    drawVal += deltaTime;
                }
                else
                {
                    drawVal -= deltaTime;
                }
            }
            else
            {
                if (dilating)
                {
                    dilating = false;
                }
                else
                {
                    dilating = true;
                }
            }
            if (drawVal > (2 * this.range) + 0.5f)
            {
                drawVal = (2 * this.range) + 0.5f;
            }
            else if (drawVal < (2 * this.range) - 0.5f)
            {
                drawVal = (2 * this.range) - 0.5f;
            }
            this.Scale = drawVal;
        }
        protected override Vector3 NextExactPosition(float deltaTime)
        {
            Vector3 vector = base.NextExactPosition(deltaTime);
            if (this.link1.Linked)
            {
                bool flag = this.detachAfterTicks == -1 || Find.TickManager.TicksGame - this.spawnTick < this.detachAfterTicks;
                if (!this.link1.Target.ThingDestroyed && flag)
                {
                    this.link1.UpdateDrawPos();
                }
                Vector3 b = this.link1.LastDrawPos - this.attacheeLastPosition;
                vector += b;
                vector.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                this.attacheeLastPosition = this.link1.LastDrawPos;
            }
            return vector;
        }
        public float drawVal = 0.1f;
        public float range = 1f;
        private bool dilating = false;
        private Vector3 attacheeLastPosition = new Vector3(-1000f, -1000f, -1000f);
    }
    public class Hediff_PreDamageModification : HediffWithComps
    {

    }
    public class HediffCompProperties_PreDamageModification : HediffCompProperties
    {
        public HediffCompProperties_PreDamageModification()
        {
            this.compClass = typeof(HediffComp_PreDamageModification);
        }
        public float minSeverityToWork = 0f;
        public float minDmgToTrigger = 0f;
        public List<DamageDef> unaffectedDamageTypes;
        public List<DamageDef> affectedDamageTypes;
        public bool harmfulDamageTypesOnly = true;
        public float chance = 1f;
        public StatDef chanceScalar;
        public float maxChance = 1f;
        public bool shouldUseIncomingDamageFactor = true;
        public float severityOnHit = 0f;
        public bool severityChangesEvenOnFail = false;
        public bool damageScalesSeverityLoss = false;
        public bool noCostIfInvincible = true;
        public int priority = 100;
        public bool reactsToRanged = true;
        public bool reactsToExplosive = true;
        public bool reactsToShieldBypassers = true;
        public bool reactsToOther = true;
    }
    public class HediffComp_PreDamageModification : HediffComp
    {
        public HediffCompProperties_PreDamageModification Props
        {
            get
            {
                return (HediffCompProperties_PreDamageModification)this.props;
            }
        }
        public virtual bool ShouldDoEffect(DamageInfo dinfo)
        {
            return this.parent.Severity >= this.Props.minSeverityToWork && (!this.Props.harmfulDamageTypesOnly || dinfo.Def.harmsHealth) && (dinfo.Def.isRanged ? this.Props.reactsToRanged : (dinfo.Def.isExplosive ? this.Props.reactsToExplosive : this.Props.reactsToOther)) && (this.Props.affectedDamageTypes == null || this.Props.affectedDamageTypes.Contains(dinfo.Def)) && (!dinfo.Def.ignoreShields || this.Props.reactsToShieldBypassers) && (this.Props.unaffectedDamageTypes == null || !this.Props.unaffectedDamageTypes.Contains(dinfo.Def));
        }
        public virtual bool ShouldDoModificationInner(DamageInfo dinfo)
        {
            return true;
        }
        public virtual bool ShouldPayCostOfHit(DamageInfo dinfo, bool absorbed)
        {
            if (this.Props.noCostIfInvincible && this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) <= float.Epsilon)
            {
                return false;
            }
            return true;
        }
        public virtual void PayCostOfHit(float damageAmount)
        {
            this.parent.Severity += this.Props.severityOnHit * (this.Props.damageScalesSeverityLoss ? damageAmount : 1f) * (this.Props.shouldUseIncomingDamageFactor ? this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) : 1f);
        }
        public virtual void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
        }
        public bool ChanceCapped()
        {
            return Rand.Chance(Math.Min(this.Props.maxChance, this.Props.chance * (this.Props.chanceScalar != null ? this.Pawn.GetStatValue(this.Props.chanceScalar) : 1f)));
        }
        public virtual void TryDoModification(ref DamageInfo dinfo, ref bool absorbed)
        {
            if (this.ShouldDoEffect(dinfo))
            {
                if (dinfo.Amount >= this.Props.minDmgToTrigger && this.ChanceCapped())
                {
                    float amount = dinfo.Amount;
                    if (this.ShouldPayCostOfHit(dinfo, absorbed))
                    {
                        this.PayCostOfHit(amount);
                    }
                    if (this.ShouldDoModificationInner(dinfo))
                    {
                        this.DoModificationInner(ref dinfo, ref absorbed, amount);
                    }
                } else if (this.Props.severityChangesEvenOnFail && this.ShouldPayCostOfHit(dinfo, absorbed)) {
                    this.PayCostOfHit(dinfo.Amount);
                }
            }
        }
    }
    public class HediffCompProperties_DamageNegation : HediffCompProperties_PreDamageModification
    {
        public HediffCompProperties_DamageNegation()
        {
            this.compClass = typeof(HediffComp_DamageNegation);
        }
        public FloatRange damageAdded = new FloatRange(0f);
        public float damageMultiplier = 0f;
        public SoundDef soundOnBlock;
        public FleckDef fleckOnBlock;
        public bool fleckScaleWithDamage = true;
        public bool centerFleckOnCharacter;
        public float minFleckSize = 10f;
        public bool throwDustPuffsOnBlock = true;
        public bool onlyDoGraphicsOnFullNegation = true;
        public bool throwText;
        public string textToThrow;
        public bool removeBadAttachables;
    }
    public class HediffComp_DamageNegation : HediffComp_PreDamageModification
    {
        public new HediffCompProperties_DamageNegation Props
        {
            get
            {
                return (HediffCompProperties_DamageNegation)this.props;
            }
        }
        public virtual bool ShouldPreventAttachment(Thing attachment)
        {
                return this.parent.Severity >= this.Props.minSeverityToWork && this.ChanceCapped();
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            base.DoModificationInner(ref dinfo, ref absorbed, amount);
            dinfo.SetAmount(Math.Max(0f, ((dinfo.Amount * this.Props.damageMultiplier) + this.Props.damageAdded.RandomInRange) * (this.Props.shouldUseIncomingDamageFactor ? this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) : 1f)));
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                if (dinfo.Amount == 0f)
                {
                    absorbed = true;
                    this.DoGraphics(dinfo, amount);
                } else if (!this.Props.onlyDoGraphicsOnFullNegation) {
                    this.DoGraphics(dinfo, Math.Min(0f, amount - dinfo.Amount));
                }
            }
        }
        public virtual void DoGraphics(DamageInfo dinfo, float amount)
        {
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                if (this.Props.soundOnBlock != null)
                {
                    this.Props.soundOnBlock.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                }
                if (amount > 0)
                {
                    Vector3 loc = this.Pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle).RotatedBy(180f) * 0.5f;
                    float num = Mathf.Min(this.Props.minFleckSize, 2f + amount / 10f);
                    if (this.Props.fleckOnBlock != null)
                    {
                        if (this.Props.centerFleckOnCharacter)
                        {
                            FleckMaker.Static(this.Pawn.TrueCenter(), this.Pawn.Map, this.Props.fleckOnBlock, this.Props.fleckScaleWithDamage ? num : this.Props.minFleckSize);
                        } else {
                            FleckMaker.Static(loc, this.Pawn.Map, this.Props.fleckOnBlock, this.Props.fleckScaleWithDamage ? num : this.Props.minFleckSize);
                        }
                    }
                    if (this.Props.throwText)
                    {
                        Vector3 locText = new Vector3((float)this.Pawn.Position.x + 1f, (float)this.Pawn.Position.y, (float)this.Pawn.Position.z + 1f);
                        string text = dinfo.Def.adaptedText ?? this.Props.textToThrow.Translate();
                        MoteMaker.ThrowText(locText, this.Pawn.Map, text, Color.white, -1f);
                    }
                    if (this.Props.throwDustPuffsOnBlock)
                    {
                        int num2 = (int)num;
                        for (int i = 0; i < num2; i++)
                        {
                            FleckMaker.ThrowDustPuff(loc, this.Pawn.Map, Rand.Range(0.8f, 1.2f));
                        }
                    }
                }
            }
        }
    }
    public class HediffCompProperties_DamageNegationShield : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_DamageNegationShield()
        {
            this.compClass = typeof(HediffComp_DamageNegationShield);
        }
        public DamageDef instantlyOverwhelmedBy;
        public bool destroyIfOverwhelmed;
        public bool blocksRangedWeapons;
        public int baseStartingTicksToReset = 1;
        public float energyOnReset = 1;
        public float baseEnergyRechargeRate = 1;
        public float baseMaxEnergy = 1;
        public StatDef rechargeRateScalar;
        public StatDef maxEnergyScalar;
        public EffecterDef breakEffect;
        public FloatRange visualRange;
        public SoundDef resetSound;
        public bool lightningGlowOnReset = true;
    }
    public class BadAttachable : DefModExtension
    {
        public BadAttachable(){}
        public DamageDef extinguishingDamageDef;
    }
    public class HediffComp_DamageNegationShield : HediffComp_DamageNegation
    {
        public new HediffCompProperties_DamageNegationShield Props
        {
            get
            {
                return (HediffCompProperties_DamageNegationShield)this.props;
            }
        }
        public virtual float EnergyGainPerTick
        {
            get
            {
                return this.energyGainPerTickCached;
            }
            set
            {
                this.energyGainPerTickCached = value;
            }
        }
        public virtual int ResetDelayTicks
        {
            get
            {
                return this.resetDelayTicksCached;
            }
            set
            {
                this.resetDelayTicksCached = value;
            }
        }
        public virtual float MaxEnergy
        {
            get
            {
                return this.maxEnergyCached;
            }
            set
            {
                this.maxEnergyCached = value;
            }
        }
        public float Energy
        {
            get
            {
                return this.parent.Severity - this.Props.minSeverityToWork;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.RedetermineAllStats();
            this.ResetShield();
        }
        public virtual void RedetermineAllStats()
        {
            this.EnergyGainPerTick = this.Props.baseEnergyRechargeRate * (this.Props.rechargeRateScalar != null ? this.Pawn.GetStatValue(this.Props.rechargeRateScalar) : 1f) / 60f;
            this.ResetDelayTicks = this.Props.baseStartingTicksToReset;
            this.MaxEnergy = (this.Props.baseMaxEnergy * (this.Props.maxEnergyScalar != null ? this.Pawn.GetStatValue(this.Props.maxEnergyScalar) : 1f)) + this.Props.minSeverityToWork;
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            return (this.parent.Severity >= this.Props.minSeverityToWork && this.Props.instantlyOverwhelmedBy != null && dinfo.Def == this.Props.instantlyOverwhelmedBy) || base.ShouldDoEffect(dinfo);
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            if (this.Props.instantlyOverwhelmedBy != null && dinfo.Def == this.Props.instantlyOverwhelmedBy)
            {
                this.PayCostOfHit(this.parent.Severity*2f);
            }
            base.DoModificationInner(ref dinfo, ref absorbed, amount);
        }
        public override void PayCostOfHit(float damageAmount)
        {
            base.PayCostOfHit(damageAmount);
            if (this.Energy < 0)
            {
                this.BreakShield();
            }
        }
        public virtual void ResetShield()
        {
            this.parent.Severity = this.Props.minSeverityToWork + this.Props.energyOnReset;
            if (this.Pawn.Spawned)
            {
                if (this.Props.resetSound != null)
                {
                    this.Props.resetSound.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                }
                if (this.Props.lightningGlowOnReset)
                {
                    FleckMaker.ThrowLightningGlow(this.Pawn.TrueCenter(), this.Pawn.Map, 3f);
                }
            }
        }
        public virtual void BreakShield()
        {
            this.ticksToReset = this.ResetDelayTicks;
            if (this.Pawn.Spawned)
            {
                float num = Mathf.Lerp(this.Props.visualRange.min, this.Props.visualRange.max, this.parent.Severity);
                if (this.Props.breakEffect != null)
                {
                    this.Props.breakEffect.SpawnAttached(this.Pawn, this.Pawn.MapHeld, num);
                }
                if (this.Props.fleckOnBlock != null)
                {
                    FleckMaker.Static(this.Pawn.TrueCenter(), this.Pawn.Map, this.Props.fleckOnBlock, this.Props.minFleckSize * 1.2f);
                }
                if (this.Props.throwDustPuffsOnBlock)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        FleckMaker.ThrowDustPuff(this.Pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle((float)Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), this.Pawn.Map, Rand.Range(0.8f, 1.2f));
                    }
                }
            }
            if (this.Props.destroyIfOverwhelmed)
            {
                this.Pawn.health.RemoveHediff(this.parent);
                return;
            } else {
                this.parent.Severity = this.Props.minSeverityToWork / 2f;
            }
        }
        public override string CompLabelInBracketsExtra {
            get
            {
                if (this.ticksToReset <= 0)
                {
                    return (this.parent.Severity - this.Props.minSeverityToWork).ToStringByStyle(ToStringStyle.FloatOne) + "/" + this.MaxEnergy.ToStringByStyle(ToStringStyle.FloatOne);
                }
                return "Hauts_ShieldRecharge".Translate((this.ticksToReset/60));
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.ticksToReset > 0)
            {
                this.ticksToReset -= delta;
                if (this.ticksToReset <= 0)
                {
                    if (this.parent.Severity < this.Props.minSeverityToWork)
                    {
                        this.ResetShield();
                    }
                    this.parent.Severity = Math.Min((this.EnergyGainPerTick*delta) + this.parent.Severity, this.MaxEnergy);
                if (this.Energy < 0)
                {
                    this.BreakShield();
                }
                }
            } else if (this.parent.Severity < this.Props.minSeverityToWork) {
                this.ticksToReset = this.ResetDelayTicks;
            } else {
                this.parent.Severity = Math.Min((this.EnergyGainPerTick * delta) + this.parent.Severity, this.MaxEnergy);
                if (this.Energy < 0)
                {
                    this.BreakShield();
                }
            }
            if (this.Pawn.IsHashIntervalTick(60, delta))
            {
                this.RedetermineAllStats();
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksToReset, "ticksToReset", -1, false);
            Scribe_Values.Look<float>(ref this.maxEnergyCached, "maxEnergyCached", 1, false);
            Scribe_Values.Look<float>(ref this.energyGainPerTickCached, "energyGainPerTickCached", 1, false);
            Scribe_Values.Look<int>(ref this.resetDelayTicksCached, "resetDelayTicksCached", 1, false);
        }
        public int ticksToReset = -1;
        public float maxEnergyCached;
        public float energyGainPerTickCached;
        public int resetDelayTicksCached;
    }
    public class HediffCompProperties_DamageRetaliation : HediffCompProperties_PreDamageModification
    {
        public HediffCompProperties_DamageRetaliation()
        {
            this.compClass = typeof(HediffComp_DamageRetaliation);
        }
        public float range = 0f;
        public bool onlyRetaliateVsInstigator = false;
        public bool hitInstigatorRegardlessOfRange;
        public bool canAffectAnimals = true;
        public bool canAffectMechs = true;
        public bool canAffectDrones = true;
        public bool canAffectHumanlikes = true;
        public bool canAffectEntities = true;
        public bool canAffectMutants = true;
        public bool friendlyFire;
        public float chanceToInflictHediff = 1f;
        public float baseHediffSeverity;
        public HediffDef hediff;
        public StatDef hediffResistStat;
        public bool hediffScaleWithDamage;
        public bool hediffScaleWithBodySize;
        public float chanceToDoDamage = 1f;
        public float baseRetaliationDamage;
        public DamageDef retaliationDamageDef;
        public StatDef baseRetaliationDamageFactor;
        public bool damageScaleWithDamage;
        public bool damageScaleWithBodySize;
        public EffecterDef visualEffect;
        public int vfxCooldownTicks;
    }
    public class HediffComp_DamageRetaliation : HediffComp_PreDamageModification
    {
        public new HediffCompProperties_DamageRetaliation Props
        {
            get
            {
                return (HediffCompProperties_DamageRetaliation)this.props;
            }
        }
        public virtual void RetaliateAgainst(Pawn p, float amount)
        {
            if (this.Props.shouldUseIncomingDamageFactor)
            {
                amount *= this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor);
            }
            if (this.Props.hediff != null && Rand.Chance(this.Props.chanceToInflictHediff))
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediff, p);
                hediff.Severity = this.Props.baseHediffSeverity * (this.Props.hediffScaleWithDamage ? amount : 1f) * (this.Props.hediffResistStat != null ? Mathf.Max(1f - p.GetStatValue(this.Props.hediffResistStat), 0f) : 1f) / (this.Props.hediffScaleWithBodySize ? p.BodySize : 1f);
                p.health.AddHediff(hediff);
            }
            if (this.Props.retaliationDamageDef != null && Rand.Chance(this.Props.chanceToDoDamage))
            {
                DamageInfo dinfo2 = new DamageInfo(this.Props.retaliationDamageDef, this.Props.baseRetaliationDamage * (this.Props.damageScaleWithDamage ? amount : 1f) * (this.Props.baseRetaliationDamageFactor != null ? this.Pawn.GetStatValue(this.Props.baseRetaliationDamageFactor) : 1f) / (this.Props.hediffScaleWithBodySize ? p.BodySize : 1f), 2f, -1f, null, p.health.hediffSet.GetRandomNotMissingPart(this.Props.retaliationDamageDef), null, DamageInfo.SourceCategory.ThingOrUnknown);
                p.TakeDamage(dinfo2);
            }
        }
        public virtual float RetaliationRange
        {
            get
            {
                return this.Props.range;
            }
        }
        public virtual bool CanHit(Pawn pawn, float amount)
        {
            return (this.Props.friendlyFire || pawn.HostileTo(this.Pawn) || this.Pawn.HostileTo(pawn)) && (pawn.IsMutant ? this.Props.canAffectMutants : ((this.Props.canAffectAnimals || !pawn.RaceProps.Animal) && (this.Props.canAffectMechs || !pawn.RaceProps.IsMechanoid) && (this.Props.canAffectDrones || !pawn.RaceProps.IsDrone) && (this.Props.canAffectHumanlikes || !pawn.RaceProps.Humanlike) && (this.Props.canAffectEntities || !pawn.RaceProps.IsAnomalyEntity)));
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            base.DoModificationInner(ref dinfo, ref absorbed, amount);
            Pawn instigator = dinfo.Instigator as Pawn;
            if (instigator != null && this.CanHit(instigator,amount) && (this.Props.hitInstigatorRegardlessOfRange || this.Pawn.Position.DistanceTo(instigator.Position) <= this.RetaliationRange))
            {
                this.RetaliateAgainst(instigator,amount);
            }
            if (!this.Props.onlyRetaliateVsInstigator && this.Pawn.SpawnedOrAnyParentSpawned && this.RetaliationRange > 0f)
            {
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.RetaliationRange, true).OfType<Pawn>().Distinct<Pawn>())
                {
                    if (this.CanHit(p,amount) && (!this.Props.hitInstigatorRegardlessOfRange || p != instigator))
                    {
                        this.RetaliateAgainst(p,amount);
                    }
                }
                if (this.graphicCooldown <= 0 && this.Props.visualEffect != null)
                {
                    this.Props.visualEffect.SpawnMaintained(this.Pawn.PositionHeld, this.Pawn.MapHeld, 1f);
                    this.graphicCooldown = this.Props.vfxCooldownTicks;
                }
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (graphicCooldown > 0)
            {
                graphicCooldown--;
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.graphicCooldown, "graphicCooldown", 0, false);
        }
        private int graphicCooldown = 0;
    }
    public class Hediff_HasExtraOnHitEffects : HediffWithComps
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsUtility.DoExtraOnHitEffects(this,thing,dinfo,result);
        }
    }
    public class Hediff_ImplantHasExtraOnHitEffects : Hediff_Implant
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
    public class Hediff_AddedPartHasExtraOnHitEffects : Hediff_AddedPart
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
    public class Hediff_PreDamageModificationHasExtraOnHitEffects : Hediff_PreDamageModification
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
    public class HediffCompProperties_ExtraOnHitEffects : HediffCompProperties
    {
        public HediffCompProperties_ExtraOnHitEffects()
        {
            this.compClass = typeof(HediffComp_ExtraOnHitEffects);
        }
        public bool damageScaling = false;
        public bool entropyCostScaling = false;
        public bool psyfocusCostScaling = false;
        public bool showTooltip = true;
        public IntRange tickCooldown = new IntRange(0, 0);
        public float severityChangeOnHit;
        public float cellRange = 1f;
        public float worldTileRange = 0f;
        public float minDmgToTrigger = 0.01f;
        public bool canAffectAnimals = true;
        public bool canAffectMechs = true;
        public bool canAffectDrones = true;
        public bool canAffectHumanlikes = true;
        public bool canAffectEntities = true;
        public bool canAffectMutants = true;
        public bool canAffectHostiles = true;
        public bool canAffectFriendlies = true;
        public bool canAffectBuildings = true;
        public bool canAffectPlants = true;
        public bool canAffectItems = true;
        public bool appliedViaAttacks;
        public bool appliedViaPsycasts;
        public StatDef attackerScalar;
        public StatDef victimScalar;
        public bool victimBodySizeInverseScaling = false;
        public float chance = 1f;
        public StatDef attackerChanceScalar;
        public StatDef victimChanceScalar;
        public float chanceCap = 1f;
        public bool triggersPyroThought;
    }
    public class HediffComp_ExtraOnHitEffects : HediffComp
    {
        public HediffCompProperties_ExtraOnHitEffects Props
        {
            get
            {
                return (HediffCompProperties_ExtraOnHitEffects)this.props;
            }
        }
        public virtual string FXTooltip()
        {
            return "";
        }
        public virtual float ChanceForVictim(Pawn victim)
        {
            return Math.Min(this.Props.chanceCap, this.Props.chance * (this.Props.attackerChanceScalar != null ? this.Pawn.GetStatValue(this.Props.attackerChanceScalar) : 1f) * (this.Props.victimChanceScalar != null ? victim.GetStatValue(this.Props.victimChanceScalar) : 1f));
        }
        public virtual void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            this.parent.Severity += this.Props.severityChangeOnHit;
        }
        public virtual float ScaledValue(Pawn victim, float basicEffectValue, float valueToScale)
        {
            return basicEffectValue * valueToScale * (this.Props.attackerScalar != null ? this.Pawn.GetStatValue(this.Props.attackerScalar) : 1f) * (this.Props.victimScalar != null ? victim.GetStatValue(this.Props.victimScalar) : 1f) / (this.Props.victimBodySizeInverseScaling ? victim.BodySize : 1f);
        }
        public virtual float ChanceForVictimThing(Thing victim)
        {
            return Math.Min(this.Props.chanceCap, this.Props.chance * (this.Props.attackerChanceScalar != null ? this.Pawn.GetStatValue(this.Props.attackerChanceScalar) : 1f) * (this.Props.victimChanceScalar != null ? victim.GetStatValue(this.Props.victimChanceScalar) : 1f));
        }
        public virtual void DoExtraEffectsThing(Thing victim, float valueToScale)
        {
            this.parent.Severity += this.Props.severityChangeOnHit;
        }
        public virtual float ScaledValueThing(Thing victim, float basicEffectValue, float valueToScale)
        {
            return basicEffectValue * valueToScale * (this.Props.attackerScalar != null ? this.Pawn.GetStatValue(this.Props.attackerScalar) : 1f) * (this.Props.victimScalar != null ? victim.GetStatValue(this.Props.victimScalar) : 1f);
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showTooltip)
                {
                    string result = "";
                    if (this.Props.chance <= 1f)
                    {
                        result += "Hauts_ExtraHitFXPrefixChance".Translate(this.Props.chance.ToStringPercent(), this.Props.minDmgToTrigger);
                    } else {
                        result += "Hauts_ExtraHitFXPrefixAlways".Translate(this.Props.minDmgToTrigger.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                    }
                    if (this.Props.cellRange <= 255f)
                    {
                        result += "Hauts_ExtraHitFXRange".Translate(Mathf.RoundToInt(this.Props.cellRange));
                    }
                    if (this.Props.tickCooldown.max > 0)
                    {
                        if (this.Props.tickCooldown.min != this.Props.tickCooldown.max)
                        {
                            result += "Hauts_ExtraHitFXPrefixCDVariable".Translate(this.Props.tickCooldown.min, this.Props.tickCooldown.max);
                        } else {
                            result += "Hauts_ExtraHitFXPrefixCD".Translate(this.Props.tickCooldown.min);
                        }
                    } else {
                        result += "Hauts_ExtraHitFXPrefixNoCD".Translate();
                    }
                    result += this.FXTooltip();
                    if (this.Props.damageScaling || this.Props.attackerScalar != null || this.Props.victimScalar != null)
                    {
                        result += "Hauts_ExtraHitFXScalars".Translate();
                        bool prev = false;
                        if (this.Props.damageScaling)
                        {
                            result += "Hauts_ExtraHitFXScaleDmgDealt".Translate();
                            prev = true;
                        }
                        if (this.Props.attackerScalar != null)
                        {
                            if (prev)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXScaleAttacker".Translate(this.Props.attackerScalar.label);
                        }
                        if (this.Props.victimScalar != null)
                        {
                            if (prev)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXScaleVictim".Translate(this.Props.victimScalar.label);
                        }
                    }
                    if (!this.Props.canAffectAnimals || !this.Props.canAffectFriendlies || !this.Props.canAffectHostiles || !this.Props.canAffectHumanlikes || !this.Props.canAffectMechs || !this.Props.canAffectDrones)
                    {
                        result += "\n";
                        result += "Hauts_ExtraHitFXSuffix".Translate();
                        bool prev2 = false;
                        if (!this.Props.canAffectAnimals)
                        {
                            result += "Hauts_ExtraHitFXSuffix2A".Translate();
                            prev2 = true;
                        }
                        if (!this.Props.canAffectFriendlies)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffixF".Translate();
                        }
                        if (!this.Props.canAffectHostiles)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffixH".Translate();
                        }
                        if (!this.Props.canAffectHumanlikes)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2H".Translate();
                        }
                        if (!this.Props.canAffectMechs)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2M".Translate();
                        }
                        if (!this.Props.canAffectDrones)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2D".Translate();
                        }
                        if (!this.Props.canAffectMutants)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2Mu".Translate();
                        }
                        if (!this.Props.canAffectEntities)
                        {
                            if (prev2)
                            {
                                result += ",";
                            }
                            result += "Hauts_ExtraHitFXSuffix2E".Translate();
                        }
                    }
                    return result.CapitalizeFirst();
                }
                return null;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.cooldown = Find.TickManager.TicksGame;
        }
        public virtual bool CanAffectTarget(Pawn pawn)
        {
            return Rand.Chance(this.ChanceForVictim(pawn)) && (this.Pawn.HostileTo(pawn) ? this.Props.canAffectHostiles : this.Props.canAffectFriendlies) && (pawn.IsMutant ? this.Props.canAffectMutants : ((this.Props.canAffectAnimals || !pawn.RaceProps.Animal) && (this.Props.canAffectHumanlikes || !pawn.RaceProps.Humanlike) && (this.Props.canAffectMechs || !pawn.RaceProps.IsMechanoid) && (this.Props.canAffectDrones || !pawn.RaceProps.IsDrone) && (this.Props.canAffectEntities || !pawn.RaceProps.IsAnomalyEntity)));
        }
        public virtual bool CanAffectTargetThing(Thing thing)
        {
            return Rand.Chance(this.ChanceForVictimThing(thing)) && (this.Pawn.HostileTo(thing) ? this.Props.canAffectHostiles : this.Props.canAffectFriendlies) && ((thing is Building && this.Props.canAffectBuildings) || (thing is Plant && this.Props.canAffectPlants) || (thing.def.category == ThingCategory.Item));
        }
        public virtual bool RangeCheck(Thing thing, DamageInfo dinfo)
        {
            if (thing.Tile != this.Pawn.Tile)
            {
                return Find.WorldGrid.TraversalDistanceBetween(thing.Tile, this.Pawn.Tile, true) <= this.Props.worldTileRange;
            }
            if (thing.SpawnedOrAnyParentSpawned && this.Pawn.SpawnedOrAnyParentSpawned)
            {
                float cellDist = thing.Position.DistanceTo(this.Pawn.Position) - Math.Max(1.42f,this.Props.cellRange);
                if (cellDist > 0 && dinfo.Weapon != null && dinfo.Weapon.StatBaseDefined(VEFDefOf.VEF_MeleeWeaponRange))
                {
                    cellDist -= dinfo.Weapon.GetStatValueAbstract(VEFDefOf.VEF_MeleeWeaponRange);
                }
                return cellDist <= 0f;
            }
            return false;
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (ModsConfig.RoyaltyActive && this.Props.appliedViaPsycasts && target != null && this.cooldown <= Find.TickManager.TicksGame)
            {
                if (verb is RimWorld.Verb_CastAbility vca && vca.ability is Psycast psycast)
                {
                    List<LocalTargetInfo> targets = vca.ability.GetAffectedTargets(target).ToList();
                    foreach (LocalTargetInfo lti in targets)
                    {
                        if (lti.Thing != null)
                        {
                            if (lti.Pawn != null && this.CanAffectTarget(lti.Pawn))
                            {
                                this.DoExtraEffects(lti.Pawn, (this.Props.psyfocusCostScaling ? 100f * psycast.FinalPsyfocusCost(target) : 1f) * (this.Props.entropyCostScaling ? psycast.def.EntropyGain : 1f), null);
                            } else if (this.CanAffectTargetThing(lti.Thing)) {
                                this.DoExtraEffectsThing(lti.Thing, (this.Props.psyfocusCostScaling ? 100f * psycast.FinalPsyfocusCost(target) : 1f) * (this.Props.entropyCostScaling ? psycast.def.EntropyGain : 1f));
                            }
                        }
                    }
                }
                if (verb is VEF.Abilities.Verb_CastAbility vcavfe && vcavfe.Caster != null && vcavfe.CasterIsPawn && HautsUtility.IsVPEPsycast(vcavfe.ability))
                {
                    GlobalTargetInfo[] targets = new GlobalTargetInfo[]
                    {
                        target.ToGlobalTargetInfo(vcavfe.Caster.Map)
                    };
                    vcavfe.ability.ModifyTargets(ref targets);
                    foreach (LocalTargetInfo lti in targets)
                    {
                        if (lti.Thing != null)
                        {
                            if (lti.Pawn != null && this.CanAffectTarget(lti.Pawn))
                            {
                                this.DoExtraEffects(lti.Pawn, (this.Props.psyfocusCostScaling ? 100f * HautsUtility.GetVPEPsyfocusCost(vcavfe.ability) : 1f) * (this.Props.entropyCostScaling ? HautsUtility.GetVPEEntropyCost(vcavfe.ability) : 1f), null);
                            } else if (this.CanAffectTargetThing(lti.Thing)) {
                                this.DoExtraEffectsThing(lti.Thing, (this.Props.psyfocusCostScaling ? 100f * HautsUtility.GetVPEPsyfocusCost(vcavfe.ability) : 1f) * (this.Props.entropyCostScaling ? HautsUtility.GetVPEEntropyCost(vcavfe.ability) : 1f));
                            }
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.cooldown, "cooldown", 0, false);
        }
        public int cooldown;
    }
    public class HediffCompProperties_CureHediffsOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_CureHediffsOnHit()
        {
            this.compClass = typeof(HediffComp_CureHediffsOnHit);
        }
        public List<HediffDef> hediffsToCure;
        public FloatRange severityLostOnCure = new FloatRange(99999f, 99999f);
        public bool totallyRemoveOnCure = true;
        public int maxHediffsCuredPerHit = 99999;
        public bool onlyOnHitPart = false;
    }
    public class HediffComp_CureHediffsOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_CureHediffsOnHit Props
        {
            get
            {
                return (HediffCompProperties_CureHediffsOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.hediffsToCure != null)
            {
                if (this.Props.totallyRemoveOnCure)
                {
                    foreach (HediffDef hed in this.Props.hediffsToCure)
                    {
                        result += "Hauts_ExtraHitFXPurge".Translate(hed.LabelCap);
                    }
                } else {
                    foreach (HediffDef hed in this.Props.hediffsToCure)
                    {
                        if (this.Props.severityLostOnCure.max > 0)
                        {
                            if (this.Props.severityLostOnCure.min != this.Props.severityLostOnCure.max)
                            {
                                result += "Hauts_ExtraHitFXPurgePartialVariable".Translate(this.Props.severityLostOnCure.min.ToStringByStyle(ToStringStyle.FloatTwo), this.Props.severityLostOnCure.max.ToStringByStyle(ToStringStyle.FloatTwo), hed.LabelCap);
                            } else {
                                result += "Hauts_ExtraHitFXPurgePartial".Translate(this.Props.severityLostOnCure.max.ToStringByStyle(ToStringStyle.FloatTwo), hed.LabelCap);
                            }
                        }
                    }
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.hediffsToCure != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                int curesRemaining = this.Props.maxHediffsCuredPerHit;
                for (int i = victim.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff h = victim.health.hediffSet.hediffs[i];
                    if (curesRemaining <= 0)
                    {
                        break;
                    }
                    if (this.Props.hediffsToCure.Contains(h.def) && (hitPart == null || (this.Props.onlyOnHitPart && h.Part == hitPart)))
                    {
                        if (this.Props.totallyRemoveOnCure)
                        {
                            victim.health.RemoveHediff(h);
                            curesRemaining--;
                        }
                        else
                        {
                            h.Severity -= this.ScaledValue(victim, this.Props.severityLostOnCure.RandomInRange, valueToScale);
                            curesRemaining--;
                        }
                    }
                }
            }
        }
    }
    public class HediffCompProperties_ExtraDamageOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_ExtraDamageOnHit()
        {
            this.compClass = typeof(HediffComp_ExtraDamageOnHit);
        }
        public List<ExtraDamage> extraDamages;
    }
    public class HediffComp_ExtraDamageOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_ExtraDamageOnHit Props
        {
            get
            {
                return (HediffCompProperties_ExtraDamageOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.extraDamages != null)
            {
                foreach (ExtraDamage ed in this.Props.extraDamages)
                {
                    if (ed.chance <1f)
                    {
                        result += "Hauts_ExtraHitFXMoreDmgChance".Translate(ed.chance.ToStringPercent(),ed.amount,ed.def.label);
                    } else {
                        result += "Hauts_ExtraHitFXMoreDmg".Translate(ed.amount, ed.def.label);
                    }
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.extraDamages != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                foreach (ExtraDamage extraDamage in this.Props.extraDamages)
                {
                    if (Rand.Chance(extraDamage.chance))
                    {
                        DamageInfo dinfo2 = new DamageInfo(extraDamage.def, this.ScaledValue(victim, extraDamage.amount, valueToScale), extraDamage.AdjustedArmorPenetration(), -1f, HautsUtility.CombatIsExtended() ? null : this.Pawn, hitPart != null ? hitPart : victim.health.hediffSet.GetRandomNotMissingPart(extraDamage.def), null, DamageInfo.SourceCategory.ThingOrUnknown);
                        dinfo2.SetWeaponHediff(this.parent.def);
                        victim.TakeDamage(dinfo2);
                    }
                }
            }
        }
        public override void DoExtraEffectsThing(Thing victim, float valueToScale)
        {
            base.DoExtraEffectsThing(victim, valueToScale);
            if (this.Props.extraDamages != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                foreach (ExtraDamage extraDamage in this.Props.extraDamages)
                {
                    if (Rand.Chance(extraDamage.chance))
                    {
                        DamageInfo dinfo2 = new DamageInfo(extraDamage.def, this.ScaledValueThing(victim, extraDamage.amount, valueToScale), extraDamage.AdjustedArmorPenetration(), -1f, HautsUtility.CombatIsExtended()?null:this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                        dinfo2.SetWeaponHediff(this.parent.def);
                        victim.TakeDamage(dinfo2);
                    }
                }
            }
        }
    }
    public class HediffCompProperties_InflictHediffOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_InflictHediffOnHit()
        {
            this.compClass = typeof(HediffComp_InflictHediffOnHit);
        }
        public HediffDef hediff;
        public float baseSeverity = 1f;
        public float canOnlyIncreaseSeverityUpTo = -999f;
        public bool localizedToHitPart = true;
    }
    public class HediffComp_InflictHediffOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_InflictHediffOnHit Props
        {
            get
            {
                return (HediffCompProperties_InflictHediffOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.hediff != null)
            {
                result += "Hauts_ExtraHitFXDebuff".Translate(this.Props.baseSeverity, this.Props.hediff);
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.hediff != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                float severity = this.ScaledValue(victim, this.Props.baseSeverity, valueToScale);
                Hediff alreadyExtant = victim.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff);
                if (alreadyExtant != null && (!this.Props.localizedToHitPart || alreadyExtant.Part == hitPart))
                {
                    severity += alreadyExtant.Severity;
                    if (this.Props.canOnlyIncreaseSeverityUpTo > 0f)
                    {
                        severity = Math.Min(severity, this.Props.canOnlyIncreaseSeverityUpTo);
                    }
                    alreadyExtant.Severity = severity;
                } else {
                    BodyPartRecord whereToAdd = this.Props.localizedToHitPart ? hitPart : null;
                    Hediff toAdd = HediffMaker.MakeHediff(this.Props.hediff, victim, whereToAdd);
                    if (this.Props.canOnlyIncreaseSeverityUpTo > 0f)
                    {
                        severity = Math.Min(severity, this.Props.canOnlyIncreaseSeverityUpTo);
                    }
                    toAdd.Severity = severity;
                    victim.health.AddHediff(toAdd, whereToAdd, null, null);
                }
            }
        }
    }
    public class HediffCompProperties_InspireOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_InspireOnHit()
        {
            this.compClass = typeof(HediffComp_InspireOnHit);
        }
        public Dictionary<InspirationDef, float> inspirationList;
    }
    public class HediffComp_InspireOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_InspireOnHit Props
        {
            get
            {
                return (HediffCompProperties_InspireOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.inspirationList == null)
            {
                result += "Hauts_ExtraHitFXInspireAny".Translate();
            }
            else
            {
                result += "Hauts_ExtraHitFXInspireList".Translate();
                bool subsequentListing = false;
                foreach (InspirationDef id in this.Props.inspirationList.Keys)
                {
                    if (subsequentListing)
                    {
                        result += ", ";
                    }
                    result += "Hauts_ExtraHitFXListicle".Translate(id.LabelCap, this.Props.inspirationList.TryGetValue(id).ToStringByStyle(ToStringStyle.FloatMaxOne));
                    subsequentListing = true;
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (victim.mindState.inspirationHandler != null)
            {
                InspirationDef id = victim.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                if (id != null)
                {
                    if (this.Props.inspirationList == null)
                    {
                        victim.mindState.inspirationHandler.TryStartInspiration(id, "Hauts_GotInspiredOnHit".Translate(victim.Named("PAWN"), this.parent.Label), true);
                    }
                    else
                    {
                        int tries = 100;
                        while (tries > 0)
                        {
                            this.Props.inspirationList.Keys.TryRandomElementByWeight((InspirationDef d) => this.Props.inspirationList.TryGetValue(d), out id);
                            if (victim.mindState.inspirationHandler.TryStartInspiration(id, "Hauts_GotInspiredOnHit".Translate(victim.Name.ToStringShort, this.parent.Label), true))
                            {
                                break;
                            }
                            tries--;
                        }
                    }
                }
            }
        }
    }
    public class HediffCompProperties_MentalStateOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_MentalStateOnHit()
        {
            this.compClass = typeof(HediffComp_MentalStateOnHit);
        }
        public Dictionary<MentalStateDef, float> mbList;
        public bool forceWake = true;
    }
    public class HediffComp_MentalStateOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_MentalStateOnHit Props
        {
            get
            {
                return (HediffCompProperties_MentalStateOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.mbList == null)
            {
                result += "Hauts_ExtraHitFXMBAny".Translate();
            }
            else
            {
                result += "Hauts_ExtraHitFXMBList".Translate();
                bool subsequentListing = false;
                foreach (MentalStateDef mb in this.Props.mbList.Keys)
                {
                    if (subsequentListing)
                    {
                        result += ", ";
                    }
                    result += "Hauts_ExtraHitFXListicle".Translate(mb.LabelCap, this.Props.mbList.TryGetValue(mb).ToStringByStyle(ToStringStyle.FloatMaxOne));
                    subsequentListing = true;
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            MentalStateDef mb;
            int tries = 100;
            if (this.Props.mbList == null)
            {
                while (tries > 0)
                {
                    mb = DefDatabase<MentalStateDef>.GetRandom();
                    if (mb.Worker.StateCanOccur(victim))
                    {
                        victim.mindState.mentalStateHandler.TryStartMentalState(mb, "Hauts_GotMBOnHit".Translate(this.parent.Label), false, this.Props.forceWake);
                        break;
                    }
                    tries--;
                }
            } else {
                while (tries > 0)
                {
                    this.Props.mbList.Keys.TryRandomElementByWeight((MentalStateDef d) => this.Props.mbList.TryGetValue(d), out mb);
                    if (mb.Worker.StateCanOccur(victim))
                    {
                        victim.mindState.mentalStateHandler.TryStartMentalState(mb, "Hauts_GotMBOnHit".Translate(this.parent.Label), false, this.Props.forceWake);
                        break;
                    }
                    tries--;
                }
            }
        }
    }
    public class HediffCompProperties_StunOnHit : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_StunOnHit()
        {
            this.compClass = typeof(HediffComp_StunOnHit);
        }
        public IntRange stunTicksRange = new IntRange(-1, -1);
    }
    public class HediffComp_StunOnHit : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_StunOnHit Props
        {
            get
            {
                return (HediffCompProperties_StunOnHit)this.props;
            }
        }
        public override string FXTooltip()
        {
            string result = base.FXTooltip();
            if (this.Props.stunTicksRange.max > 0)
            {
                if (this.Props.stunTicksRange.min != this.Props.stunTicksRange.max)
                {
                    result += "Hauts_ExtraHitFXStunVariable".Translate(this.Props.stunTicksRange.min, this.Props.stunTicksRange.max);
                }
                else
                {
                    result += "Hauts_ExtraHitFXStun".Translate(this.Props.stunTicksRange.min);
                }
            }
            return result;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (this.Props.stunTicksRange.min > 0 && this.Props.stunTicksRange.max > 0 && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
            {
                victim.stances.stunner.StunFor((int)this.ScaledValue(victim, (float)this.Props.stunTicksRange.RandomInRange, valueToScale), this.parent.pawn, false);
            }
        }
    }
    public class DamageFactorGroupDef : Def
    {
        public List<DamageDef> damageDefs;
        public List<DFG_HediffTarget> applyToHediffs;
    }
    public class DFG_HediffTarget
    {
        public DFG_HediffTarget(){}
        public HediffDef hediff;
        public int stageIndex;
        public float factor;
    }
    public class HediffCompProperties_AbilityCooldownModifier : HediffCompProperties
    {
        public HediffCompProperties_AbilityCooldownModifier()
        {
            this.compClass = typeof(HediffComp_AbilityCooldownModifier);
        }
        public float increasedCooldownRecovery = 0f;
        public List<RimWorld.AbilityDef> affectedAbilities = new List<RimWorld.AbilityDef>();
        public List<VEF.Abilities.AbilityDef> affectedVEFAbilities = new List<VEF.Abilities.AbilityDef>();
        public List<DefModExtension> affectedDMEs = new List<DefModExtension>();
        public WorkTags abilitiesUsingThisWorkTag = WorkTags.None;
        public bool multiplyBySeverity = false;
        public StatDef multiplyByStat = null;
        public bool affectsAllBionicAbilities = false;
        public bool affectsAllGeneticAbilities = false;
        public bool affectsAllAbilities = false;
    }
    public class HediffComp_AbilityCooldownModifier : HediffComp
    {
        public HediffCompProperties_AbilityCooldownModifier Props
        {
            get
            {
                return (HediffCompProperties_AbilityCooldownModifier)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.multiplyBySeverity)
                {
                    return "Hauts_ACMtooltip".Translate((this.parent.Severity*this.Props.increasedCooldownRecovery).ToStringPercent());
                }
                return "Hauts_ACMtooltip".Translate(this.Props.increasedCooldownRecovery.ToStringPercent());
            }
        }
    }
    public class CooldownModifier_WorkTags : DefModExtension
    {
        public CooldownModifier_WorkTags()
        {

        }
        public WorkTags affectedByAnyACMwithThisWorkTag = WorkTags.None;
    }
    public class HediffCompProperties_BoredomAdjustment : HediffCompProperties
    {
        public HediffCompProperties_BoredomAdjustment()
        {
            this.compClass = typeof(HediffComp_BoredomAdjustment);
        }
        public int ticks = 2500;
        public Dictionary<JoyKindDef, float> boredoms;
    }
    public class HediffComp_BoredomAdjustment : HediffComp
    {
        public HediffCompProperties_BoredomAdjustment Props
        {
            get
            {
                return (HediffCompProperties_BoredomAdjustment)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.ticks, delta) && this.Pawn.needs.joy != null && this.Props.boredoms != null)
            {
                DefMap<JoyKindDef, float> tolerances = HautsFramework.GetInstanceField(typeof(JoyToleranceSet), this.Pawn.needs.joy.tolerances, "tolerances") as DefMap<JoyKindDef, float>;
                DefMap<JoyKindDef, bool> bored = HautsFramework.GetInstanceField(typeof(JoyToleranceSet), this.Pawn.needs.joy.tolerances, "bored") as DefMap<JoyKindDef, bool>;
                foreach (JoyKindDef jkd in this.Props.boredoms.Keys)
                {
                    float num2 = tolerances[jkd];
                    num2 = Math.Min(1f, Math.Max(0f, num2 - this.Props.boredoms.TryGetValue(jkd)));
                    tolerances[jkd] = num2;
                    if (bored[jkd] && num2 < 0.3f)
                    {
                        bored[jkd] = false;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_ChangesBasedOnStat : HediffCompProperties
    {
        public HediffCompProperties_ChangesBasedOnStat()
        {
            this.compClass = typeof(HediffComp_ChangesBasedOnStat);
        }
        public HediffDef turnInto;
        public StatDef whenStat;
        public float goesBelow;
        public float goesAbove = 999f;
    }
    public class HediffComp_ChangesBasedOnStat : HediffComp
    {
        public HediffCompProperties_ChangesBasedOnStat Props
        {
            get
            {
                return (HediffCompProperties_ChangesBasedOnStat)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(2500, delta))
            {
                float statVal = this.Pawn.GetStatValue(this.Props.whenStat);
                if (statVal <= this.Props.goesBelow || statVal > this.Props.goesAbove)
                {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.turnInto, this.Pawn, this.parent.Part ?? null);
                    this.Pawn.health.AddHediff(hediff, this.parent.Part ?? null);
                    this.Pawn.health.RemoveHediff(this.parent);
                }
            }
        }
    }
    public class HediffCompProperties_DelayedResurrection : HediffCompProperties
    {
        public HediffCompProperties_DelayedResurrection()
        {
            this.compClass = typeof(HediffComp_DelayedResurrection);
        }
        public float chance = 1f;
        public IntRange rareTickDelay;
        public string onDeathMessage;
        public string onRezMessage;
        public bool shouldTranslateOnDeath = true;
        public bool shouldTranslateOnRez = true;
        public bool shouldSendMessage = true;
        public bool preventRisingAsShambler = true;
        public TraitDef requiredTrait = null;
        public HediffDef potentialMutation;
        public float mutationChance;
        public FloatRange mutationSeverity;
    }
    public class HediffComp_DelayedResurrection : HediffComp
    {
        public HediffCompProperties_DelayedResurrection Props
        {
            get
            {
                return (HediffCompProperties_DelayedResurrection)this.props;
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (Rand.Chance(this.Props.chance) && (this.Pawn.story == null || this.Props.requiredTrait == null || this.Pawn.story.traits.HasTrait(this.Props.requiredTrait)))
            {
                if (this.Props.shouldSendMessage)
                {
                    Messages.Message((this.Props.shouldTranslateOnDeath ? this.Props.onDeathMessage.Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve() : this.Props.onDeathMessage), this.Pawn.Corpse != null ? this.Pawn.Corpse : null, MessageTypeDefOf.NeutralEvent, true);
                }
                HediffDef mut = null;
                float mutSeverity = this.Props.mutationSeverity.RandomInRange;
                if (Rand.Chance(this.Props.mutationChance))
                {
                    mut = this.Props.potentialMutation;
                }
                HautsUtility.StartDelayedResurrection(this.Pawn, this.Props.rareTickDelay, this.Props.onRezMessage, this.Props.shouldSendMessage, this.Props.shouldTranslateOnRez, this.Props.preventRisingAsShambler, mut ?? null, mutSeverity);
            }
        }
    }
    public class HediffCompProperties_DisappearsWhileDowned : HediffCompProperties
    {
        public HediffCompProperties_DisappearsWhileDowned()
        {
            this.compClass = typeof(HediffComp_DisappearsWhileDowned);
        }
        public int ticksSpentDownedToStop;
    }
    public class HediffComp_DisappearsWhileDowned : HediffComp
    {
        public HediffCompProperties_DisappearsWhileDowned Props
        {
            get
            {
                return (HediffCompProperties_DisappearsWhileDowned)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.Downed)
            {
                this.ticksSpentDowned++;
                if (this.ticksSpentDowned >= this.Props.ticksSpentDownedToStop)
                {
                    this.Pawn.health.RemoveHediff(this.parent);
                    return;
                }
            } else {
                this.ticksSpentDowned = 0;
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksSpentDowned, "ticksSpentDowned", this.Props.ticksSpentDownedToStop, false);
        }
        int ticksSpentDowned;
    }
    public class HediffCompProperties_ExpelsHediffsWithTag : HediffCompProperties
    {
        public HediffCompProperties_ExpelsHediffsWithTag()
        {
            this.compClass = typeof(HediffComp_ExpelsHediffsWithTag);
        }
        public string hediffTag;
        public bool onlyExpelItemizableHediffs;
        public bool onlySameBodyPart = true;
    }
    public class HediffComp_ExpelsHediffsWithTag : HediffComp
    {
        public HediffCompProperties_ExpelsHediffsWithTag Props
        {
            get
            {
                return (HediffCompProperties_ExpelsHediffsWithTag)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            List<Hediff> hediffs = this.Pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (this.parent != hediffs[i] && hediffs[i].def.tags != null && hediffs[i].def.tags.Contains(this.Props.hediffTag) && (!this.Props.onlySameBodyPart || this.parent.Part == null || (hediffs[i].Part != null && hediffs[i].Part == this.parent.Part)))
                {
                    if (hediffs[i].def.spawnThingOnRemoved != null)
                    {
                        if (this.Pawn.SpawnedOrAnyParentSpawned)
                        {
                            GenSpawn.Spawn(hediffs[i].def.spawnThingOnRemoved, this.Pawn.PositionHeld, this.Pawn.MapHeld, WipeMode.Vanish);
                        } else {
                            this.Pawn.inventory.innerContainer.TryAdd(ThingMaker.MakeThing(hediffs[i].def.spawnThingOnRemoved, null));
                        }
                        this.Pawn.health.RemoveHediff(hediffs[i]);
                    } else if (!this.Props.onlyExpelItemizableHediffs) {
                        this.Pawn.health.RemoveHediff(hediffs[i]);
                    }
                }
            }
        }
    }
    public class HediffCompProperties_ExitMentalStateOnRemoval : HediffCompProperties
    {
        public HediffCompProperties_ExitMentalStateOnRemoval()
        {
            this.compClass = typeof(HediffComp_ExitMentalStateOnRemoval);
        }
        public bool anyMentalState;
        public List<MentalStateDef> mentalStates;
        public bool sendNotification;
        public bool canRemoveFleeing;
        public bool canRemoveAggro = true;
        public bool canRemoveMalicious = true;
        public bool removeEarlyIfNotInMentalState;
        public string recoveryText;
    }
    public class HediffComp_ExitMentalStateOnRemoval : HediffComp
    {
        public HediffCompProperties_ExitMentalStateOnRemoval Props
        {
            get
            {
                return (HediffCompProperties_ExitMentalStateOnRemoval)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!this.Pawn.InMentalState && this.Props.removeEarlyIfNotInMentalState)
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (PawnUtility.ShouldSendNotificationAbout(this.Pawn) && this.Pawn.InMentalState && (this.Pawn.MentalStateDef != MentalStateDefOf.PanicFlee || this.Props.canRemoveFleeing) && (!this.Pawn.MentalStateDef.IsAggro || this.Props.canRemoveAggro) && (this.Pawn.MentalStateDef.category != MentalStateCategory.Malicious || this.Props.canRemoveMalicious) && (this.Props.anyMentalState || this.Props.mentalStates.Contains(this.Pawn.MentalStateDef)))
            {
                this.parent.pawn.MentalState.RecoverFromState();
                if (this.Props.sendNotification)
                {
                    TaggedString message = this.Props.recoveryText.Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve();
                    Messages.Message(message, this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    public class HediffCompProperties_ForcedByOtherProperty : HediffCompProperties
    {
        public HediffCompProperties_ForcedByOtherProperty()
        {
            this.compClass = typeof(HediffComp_ForcedByOtherProperty);
        }
        public List<TraitDef> forcingTraits;
        public List<GeneDef> forcingGenes;
        public List<HediffDef> alternativeHediffs;
        public bool requiresAForcingProperty = true;
        public HediffDef returnAs;
    }
    public class HediffComp_ForcedByOtherProperty : HediffComp
    {
        public HediffCompProperties_ForcedByOtherProperty Props
        {
            get
            {
                return (HediffCompProperties_ForcedByOtherProperty)this.props;
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!this.Pawn.Dead && !this.Pawn.Destroyed)
            {
                if (this.Props.alternativeHediffs != null)
                {
                    foreach (HediffDef h in this.Props.alternativeHediffs)
                    {
                        if (this.Pawn.health.hediffSet.HasHediff(h))
                        {
                            return;
                        }
                    }
                }
                if (this.Props.forcingTraits != null && this.Pawn.story != null)
                {
                    foreach (TraitDef t in this.Props.forcingTraits)
                    {
                        if (this.Pawn.story.traits.HasTrait(t))
                        {
                            this.RecreateHediff();
                            return;
                        }
                    }
                }
                if (ModsConfig.BiotechActive && this.Props.forcingGenes != null && this.Pawn.genes != null)
                {
                    foreach (GeneDef g in this.Props.forcingGenes)
                    {
                        if (HautsUtility.AnalogHasActiveGene(this.Pawn.genes, g))
                        {
                            this.RecreateHediff();
                            return;
                        }
                    }
                }
            }
        }
        private void RecreateHediff()
        {
            Hediff hediff = HediffMaker.MakeHediff(this.Props.returnAs != null ? this.Props.returnAs : this.parent.def, this.Pawn, null);
            this.Pawn.health.AddHediff(hediff, null, null, null);
        }
    }
    public class HediffCompProperties_GeneticResourceModifiers : HediffCompProperties
    {
        public HediffCompProperties_GeneticResourceModifiers()
        {
            this.compClass = typeof(HediffComp_GeneticResourceModifiers);
        }
        public Dictionary<string, float> maxResourceOffsets = new Dictionary<string, float>();
        public Dictionary<string, float> drainRateFactors = new Dictionary<string, float>();
    }
    public class HediffComp_GeneticResourceModifiers : HediffComp
    {
        public HediffCompProperties_GeneticResourceModifiers Props
        {
            get
            {
                return (HediffCompProperties_GeneticResourceModifiers)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                string result = "";
                foreach (string s in this.Props.maxResourceOffsets.Keys)
                {
                    result += "Hauts_GRMtooltip".Translate(100f * this.Props.maxResourceOffsets.TryGetValue(s), s) + "\n";
                }
                foreach (string s in this.Props.drainRateFactors.Keys)
                {
                    result += "Hauts_GRMtooltip2".Translate(s.CapitalizeFirst(), this.Props.drainRateFactors.TryGetValue(s).ToStringPercent()) + "\n";
                }
                return result;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (ModsConfig.BiotechActive && this.Pawn.genes != null)
            {
                foreach (Gene g in this.Pawn.genes.GenesListForReading)
                {
                    if (g is Gene_Resource gr && this.Props.maxResourceOffsets.ContainsKey(gr.ResourceLabel))
                    {
                        gr.SetMax(gr.Max + this.Props.maxResourceOffsets.TryGetValue(gr.ResourceLabel));
                    }
                }
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (ModsConfig.BiotechActive && this.Pawn.genes != null)
            {
                foreach (Gene g in this.Pawn.genes.GenesListForReading)
                {
                    if (g is Gene_Resource gr && this.Props.maxResourceOffsets.ContainsKey(gr.ResourceLabel))
                    {
                        gr.SetMax(gr.Max - this.Props.maxResourceOffsets.TryGetValue(gr.ResourceLabel));
                    }
                }
            }
        }
    }
    public class HediffCompProperties_GiveScalingDurationHediff : HediffCompProperties
    {
        public HediffCompProperties_GiveScalingDurationHediff()
        {
            this.compClass = typeof(HediffComp_GiveScalingDurationHediff);
        }
        public HediffDef hediffDef;
        public float durationScalar;
        public bool skipIfAlreadyExists;
    }
    public class HediffComp_GiveScalingDurationHediff : HediffComp
    {
        public HediffCompProperties_GiveScalingDurationHediff Props
        {
            get
            {
                return (HediffCompProperties_GiveScalingDurationHediff)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            if (this.Props.skipIfAlreadyExists && this.parent.pawn.health.hediffSet.HasHediff(this.Props.hediffDef, false))
            {
                return;
            }
            Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffDef, this.parent.pawn, null);
            if (hediff is HediffWithComps hediffWithComps)
            {
                foreach (HediffComp hediffComp in hediffWithComps.comps)
                {
                    HediffComp_Disappears thisDisappears = this.parent.TryGetComp<HediffComp_Disappears>();
                    if (hediffComp is HediffComp_Disappears hediffComp_Disappears && thisDisappears != null)
                    {
                        hediffComp_Disappears.ticksToDisappear = (int)(this.Props.durationScalar * thisDisappears.ticksToDisappear);
                    }
                }
            }
            this.parent.pawn.health.AddHediff(hediff, null, null, null);
        }
    }
    public class HediffCompProperties_GiveThoughtsRandomly : HediffCompProperties
    {
        public HediffCompProperties_GiveThoughtsRandomly()
        {
            this.compClass = typeof(HediffComp_GiveThoughtsRandomly);
        }
        public float mtbDays;
        public float mtbLossPerExtraSeverity;
        public float mtbLossSeverityCap;
        public List<ThoughtDef> thoughtDefs;
        public bool showInTooltip = false;
    }
    public class HediffComp_GiveThoughtsRandomly : HediffComp
    {
        public HediffCompProperties_GiveThoughtsRandomly Props
        {
            get
            {
                return (HediffCompProperties_GiveThoughtsRandomly)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (!this.Props.showInTooltip)
                {
                    return base.CompLabelInBracketsExtra;
                }
                return "Hauts_GTRtooltip".Translate(this.Props.mtbDays);
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta) && this.Pawn.needs.mood != null && Rand.MTBEventOccurs(Math.Max(this.Props.mtbDays - Math.Min(this.Props.mtbLossPerExtraSeverity * this.parent.Severity, this.Props.mtbLossSeverityCap), 0.001f), 60000f, 150f))
            {
                Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(this.Props.thoughtDefs.RandomElement<ThoughtDef>());
                this.Pawn.needs.mood.thoughts.memories.TryGainMemory(thought, null);
            }
        }
    }
    public class HediffCompProperties_GiveTrait : HediffCompProperties
    {
        public HediffCompProperties_GiveTrait()
        {
            this.compClass = typeof(HediffComp_GiveTrait);
        }
        public TraitDef traitDef;
        public int traitDegree = 0;
    }
    public class HediffComp_GiveTrait : HediffComp
    {
        public HediffCompProperties_GiveTrait Props
        {
            get
            {
                return (HediffCompProperties_GiveTrait)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (!this.removeTraitOnRemoval)
                {
                    return base.CompLabelInBracketsExtra;
                }
                return "Hauts_GivesTraitTooltip".Translate(this.Props.traitDef.DataAtDegree(this.Props.traitDegree).GetLabelFor(this.parent.pawn));
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            if (this.parent.pawn.story != null && this.parent.pawn.story.traits != null)
            {
                foreach (Trait t in this.parent.pawn.story.traits.allTraits)
                {
                    if (t.def == this.Props.traitDef && t.Degree == this.Props.traitDegree)
                    {
                        this.removeTraitOnRemoval = false;
                        if (t.suppressedByTrait)
                        {
                            t.suppressedByTrait = false;
                            this.AdjustSuppression();
                        }
                        return;
                    }
                }
                this.AdjustSuppression();
                Trait toGain = new Trait(this.Props.traitDef, this.Props.traitDegree);
                this.parent.pawn.story.traits.GainTrait(toGain);
                this.removeTraitOnRemoval = true;
                this.parent.pawn.story.traits.RecalculateSuppression();
            }
        }
        public void AdjustSuppression()
        {
            foreach (Trait tt in this.parent.pawn.story.traits.allTraits)
            {
                if ((tt.def != this.Props.traitDef && tt.def.ConflictsWith(this.Props.traitDef) && tt.def.canBeSuppressed) || (tt.def == this.Props.traitDef && tt.Degree != this.Props.traitDegree))
                {
                    tt.suppressedByTrait = true;
                }
            }
        }
        public override void CompPostPostRemoved()
        {
            if (this.parent.pawn.story != null && this.parent.pawn.story.traits != null)
            {
                if (this.removeTraitOnRemoval)
                {
                    List<Trait> toRemove = new List<Trait>();
                    foreach (Trait t in this.parent.pawn.story.traits.allTraits)
                    {
                        if (t.def == this.Props.traitDef && t.Degree == this.Props.traitDegree)
                        {
                            toRemove.Add(t);
                        }
                    }
                    foreach (Trait t in toRemove)
                    {
                        this.parent.pawn.story.traits.RemoveTrait(t);
                        foreach (Trait tt in this.parent.pawn.story.traits.allTraits)
                        {
                            if (tt.suppressedByTrait && ((tt.def.ConflictsWith(this.Props.traitDef)) || (tt.def == this.Props.traitDef && tt.Degree != this.Props.traitDegree)))
                            {
                                bool flag = true;
                                foreach (Trait ttt in this.parent.pawn.story.traits.allTraits)
                                {
                                    if (ttt != tt && ttt.def.ConflictsWith(tt.def))
                                    {
                                        flag = false;
                                    }
                                }
                                if (flag)
                                {
                                    tt.suppressedByTrait = false;
                                }
                            }
                        }
                    }
                }
            }
            this.parent.pawn.story.traits.RecalculateSuppression();
            base.CompPostPostRemoved();
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.removeTraitOnRemoval, "removeTraitOnRemoval", true, false);
        }
        public bool removeTraitOnRemoval;
    }
    public class HediffCompProperties_MoteConditional : HediffCompProperties
    {
        public HediffCompProperties_MoteConditional()
        {
            this.compClass = typeof(HediffComp_MoteConditional);
        }
        public ThingDef mote;
        public float scale;
        public FloatRange validRange = new FloatRange(-1f);
        public bool scaleWithBodySize = true;
    }
    public class HediffComp_MoteConditional : HediffComp
    {
        public HediffCompProperties_MoteConditional Props
        {
            get
            {
                return this.props as HediffCompProperties_MoteConditional;
            }
        }
        public virtual bool DisableMote()
        {
            return false;
        }
        public virtual float Scale
        {
            get
            {
                return this.Props.scale * (this.Props.scaleWithBodySize ? this.Pawn.BodySize : 1f);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.Spawned && (this.Props.validRange.max < 0f || (this.parent.Severity >= this.Props.validRange.min && this.parent.Severity <= this.Props.validRange.max)))
            {
                if (!this.DisableMote())
                {
                    if (this.mote == null || this.mote.Destroyed)
                    {
                        this.mote = MoteMaker.MakeAttachedOverlay(base.Pawn, this.Props.mote, Vector3.zero, this.Scale, -1f);
                        if (this.mote is MoteConditionalText mct)
                        {
                            mct.UpdateText();
                        }
                        if (this.Pawn.IsHashIntervalTick(10))
                        {
                            this.mote.link1.UpdateDrawPos();
                        }
                    } else {
                        this.mote.Maintain();
                    }
                    if (this.Pawn.IsHashIntervalTick(250))
                    {
                        this.mote.Scale = this.Scale;
                    }
                } else if (this.mote != null && !this.mote.Destroyed) {
                    this.mote.Destroy();
                }
            } else if (this.mote != null && !this.mote.Destroyed) {
                this.mote.Destroy(DestroyMode.Vanish);
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (this.mote != null && !this.mote.Destroyed)
            {
                this.mote.Destroy(DestroyMode.Vanish);
            }
        }
        public Mote mote;
    }
    public class HediffCompProperties_MoteConditionalShield : HediffCompProperties_MoteConditional
    {
        public HediffCompProperties_MoteConditionalShield()
        {
            this.compClass = typeof(HediffComp_MoteConditionalShield);
        }
        public float minDrawFactor = 1.2f;
        public float maxDrawFactor = 1.55f;
        public bool randomRotation = true;
    }
    public class HediffComp_MoteConditionalShield : HediffComp_MoteConditional
    {
        public new HediffCompProperties_MoteConditionalShield Props
        {
            get
            {
                return this.props as HediffCompProperties_MoteConditionalShield;
            }
        }
        public float MaxEnergy
        {
            get
            {
                HediffComp_DamageNegationShield hcdns = this.parent.TryGetComp<HediffComp_DamageNegationShield>();
                if (hcdns != null)
                {
                    return hcdns.MaxEnergy;
                }
                return 1f;
            }
        }
        public override float Scale {
            get
            {
                return base.Scale*Mathf.Lerp(this.Props.minDrawFactor, this.Props.maxDrawFactor, (this.parent.Severity-this.Props.validRange.min) /this.MaxEnergy);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.mote != null)
            {
                this.mote.Scale = this.Scale;
                if (this.Props.randomRotation)
                {
                    this.mote.exactRotation = (float)Rand.Range(0, 360);
                }
            }
        }
    }
    public class MoteConditionalText : MoteAttached
    {
        public override void DrawGUIOverlay()
        {
            Color color = new Color(this.def.graphicData.color.r, this.def.graphicData.color.g, this.def.graphicData.color.b);
            GenMapUI.DrawText(new Vector2(this.exactPosition.x, this.exactPosition.z), this.text, color);
        }
        public void UpdateText()
        {
            this.text = this.TextString;
        }
        public virtual string TextString
        {
            get
            {
                return " ";
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.text, "text", "1_1", false);
        }
        public string text = " ";
    }
    public class HediffCompProperties_MultiLink : HediffCompProperties
    {
        public HediffCompProperties_MultiLink()
        {
            this.compClass = typeof(HediffComp_MultiLink);
        }
        public bool showName = true;
        public float maxDistance = -1f;
        public ThingDef customMote;
    }
    public class HediffComp_MultiLink : HediffComp
    {
        public HediffCompProperties_MultiLink Props
        {
            get
            {
                return (HediffCompProperties_MultiLink)this.props;
            }
        }
        public override bool CompShouldRemove
        {
            get
            {
                if (base.CompShouldRemove)
                {
                    return true;
                }
                if (this.others == null || this.others.Count == 0)
                {
                    return true;
                }
                if (!this.parent.pawn.SpawnedOrAnyParentSpawned)
                {
                    return true;
                }
                bool anyOtherInRange = false;
                foreach (Thing t in this.others)
                {
                    if (t.SpawnedOrAnyParentSpawned && (this.Props.maxDistance <= 0f || this.parent.pawn.PositionHeld.InHorDistOf(t.PositionHeld,this.Props.maxDistance)))
                    {
                        anyOtherInRange = true;
                        break;
                    }
                }
                if (!anyOtherInRange)
                {
                    return true;
                }
                return false;
            }
        }
        public virtual void DoToDistanceBrokenLink(Thing other)
        {

        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.drawConnection && this.others != null)
            {
                if (this.motes == null)
                {
                    this.motes = new List<MoteDualAttached>();
                }
                int countDiff = this.others.Count - this.motes.Count;
                while (countDiff > 0)
                {
                    this.motes.Add(null);
                    countDiff--;
                }
                for (int i = this.others.Count - 1; i >= 0; i--)
                {
                    if (this.others[i].MapHeld == this.Pawn.MapHeld)
                    {
                        if (this.Props.maxDistance > 0f && !this.Pawn.PositionHeld.InHorDistOf(this.others[i].PositionHeld, this.Props.maxDistance))
                        {
                            this.DoToDistanceBrokenLink(this.others[i]);
                            bool letPairedHediffRemoveItself = false;
                            if (this.others[i] is Pawn p2)
                            {
                                HediffComp_PairedHediff hcph = this.parent.TryGetComp<HediffComp_PairedHediff>();
                                if (hcph != null)
                                {
                                    for (int j = hcph.hediffs.Count - 1; j >= 0; j--)
                                    {
                                        if (hcph.hediffs[j].pawn == p2)
                                        {
                                            letPairedHediffRemoveItself = true;
                                            p2.health.RemoveHediff(hcph.hediffs[j]);
                                        }
                                    }
                                }
                            }
                            if (!letPairedHediffRemoveItself)
                            {
                                this.others.Remove(this.others[i]);
                                if (this.motes.Count >= i + 1)
                                {
                                    this.motes.Remove(this.motes[i]);
                                }
                            }
                            continue;
                        }
                        if (this.others[i] is Pawn p)
                        {
                            if (p.Dead || p.Destroyed || !p.Spawned)
                            {
                                this.others.Remove(this.others[i]);
                                if (this.motes.Count >= i + 1)
                                {
                                    this.motes.Remove(this.motes[i]);
                                }
                                continue;
                            }
                            if (p == this.Pawn)
                            {
                                continue;
                            }
                        }
                        ThingDef thingDef = this.Props.customMote ?? ThingDefOf.Mote_PsychicLinkLine;
                        if (this.motes[i] == null || this.motes[i].Destroyed)
                        {
                            this.motes[i] = MoteMaker.MakeInteractionOverlay(thingDef, this.Pawn, this.others[i]);
                        }
                        this.motes[i].Maintain();
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look<Thing>(ref this.others, "others", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.drawConnection, "drawConnection", false, false);
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.others != null && this.others.Count > 0)
                {
                    string pairedOthers = "Hauts_PairPrefix".Translate();
                    foreach (Thing t in this.others)
                    {
                        pairedOthers += "Hauts_PairedWithOther".Translate(t.Label);
                    }
                    return pairedOthers;
                }
                return base.CompLabelInBracketsExtra;
            }
        }
        public List<Thing> others;
        public List<MoteDualAttached> motes;
        public bool drawConnection;
    }
    public class HediffCompProperties_PairedHediff : HediffCompProperties
    {
        public HediffCompProperties_PairedHediff()
        {
            this.compClass = typeof(HediffComp_PairedHediff);
        }
        public bool invalidateLinksIfSuspended = false;
        public bool removeLinkedHediffOnRemoval = true;
        public float addedSeverityToLinkedHediffOnRemoval = 0f;
        public float addSeverityOnLostHediff;
    }
    public class HediffComp_PairedHediff : HediffComp
    {
        public HediffCompProperties_PairedHediff Props
        {
            get
            {
                return (HediffCompProperties_PairedHediff)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.hediffs != null && this.hediffs.Count > 0)
                {
                    string pairedHediffs = "Hauts_PairPrefix".Translate();
                    foreach (Hediff h in this.hediffs)
                    {
                        if (h != null && h.pawn != null)
                        {
                            pairedHediffs += "Hauts_PairedWithHediff".Translate(h.Label, h.pawn.Name != null ? h.pawn.Name.ToStringShort : h.pawn.Label);
                        }
                    }
                    return pairedHediffs;
                }
                return base.CompLabelInBracketsExtra;
            }
        }
        public virtual void SynchronizePairedHediffDurations()
        {
            HediffComp_Disappears hcd = this.parent.TryGetComp<HediffComp_Disappears>();
            if (hcd != null)
            {
                foreach (Hediff h in this.hediffs)
                {
                    if (h is HediffWithComps)
                    {
                        HediffComp_Disappears hcd2 = h.TryGetComp<HediffComp_Disappears>();
                        if (hcd2 != null)
                        {
                            hcd2.ticksToDisappear = hcd.ticksToDisappear;
                        }
                    }
                }
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.hediffs = new List<Hediff>();
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(15))
            {
                for (int i = this.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff h = this.hediffs[i];
                    Pawn p = h.pawn;
                    if (p.DestroyedOrNull() || (p.IsWorldPawn() && !p.IsCaravanMember() && !PawnUtility.IsTravelingInTransportPodWorldObject(p) && !p.IsBorrowedByAnyFaction()))
                    {
                        this.hediffs.RemoveAt(i);
                        this.parent.Severity += this.Props.addSeverityOnLostHediff;
                    } else if (this.Props.invalidateLinksIfSuspended && p.Suspended) {
                        this.hediffs.RemoveAt(i);
                        HediffComp_PairedHediff ph = h.TryGetComp<HediffComp_PairedHediff>();
                        if (ph != null)
                        {
                            ph.hediffs.Remove(this.parent);
                        }
                        this.parent.Severity += this.Props.addSeverityOnLostHediff;
                    }
                }
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            this.RemoveLinksOnRemoval();
        }
        public virtual void RemoveLinksOnRemoval()
        {
            foreach (Hediff h in hediffs)
            {
                if (h != null && h.pawn != null)
                {
                    if (h is HediffWithComps)
                    {
                        HediffComp_PairedHediff ph = h.TryGetComp<HediffComp_PairedHediff>();
                        if (ph != null)
                        {
                            ph.hediffs.Remove(this.parent);
                        }
                    }
                    if (this.Props.removeLinkedHediffOnRemoval)
                    {
                        h.pawn.health.RemoveHediff(h);
                    } else {
                        h.Severity += this.Props.addedSeverityToLinkedHediffOnRemoval;
                        HediffComp_MultiLink hcml = h.TryGetComp<HediffComp_MultiLink>();
                        if (hcml != null && hcml.others != null)
                        {
                            for (int i = hcml.others.Count - 1; i >= 0; i--)
                            {
                                if (hcml.others[i] is Pawn p && p == this.Pawn)
                                {
                                    hcml.others.RemoveAt(i);
                                    hcml.motes.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look<Hediff>(ref this.hediffs, "hediffs", LookMode.Reference, Array.Empty<object>());
        }
        public List<Hediff> hediffs;
    }
    public class HediffCompProperties_PhylumMorphsHediff : HediffCompProperties
    {
        public HediffCompProperties_PhylumMorphsHediff()
        {
            this.compClass = typeof(HediffComp_PhylumMorphsHediff);
        }
        public HediffDef hediffIfOrganic;
        public HediffDef hediffIfInorganic;
        public HediffDef hediffIfAnimal;
        public HediffDef hediffIfDryad;
        public HediffDef hediffIfEntity;
        public HediffDef hediffIfHumanlike;
        public HediffDef hediffIfMech;
        public HediffDef hediffIfDrone;
        public HediffDef hediffIfMutant;
    }
    public class HediffComp_PhylumMorphsHediff : HediffComp
    {
        public HediffCompProperties_PhylumMorphsHediff Props
        {
            get
            {
                return (HediffCompProperties_PhylumMorphsHediff)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.Pawn.RaceProps.Animal && this.Props.hediffIfAnimal != null && (this.Props.hediffIfDryad == null || !this.Pawn.RaceProps.Dryad))
            {
                this.ReplaceHediff(this.Props.hediffIfAnimal);
            } else if (this.Pawn.RaceProps.Dryad && this.Props.hediffIfDryad != null) {
                this.ReplaceHediff(this.Props.hediffIfDryad);
            } else if (this.Pawn.RaceProps.IsAnomalyEntity && this.Props.hediffIfEntity != null) {
                this.ReplaceHediff(this.Props.hediffIfEntity);
            } else if (this.Pawn.RaceProps.Humanlike && this.Props.hediffIfHumanlike != null) {
                this.ReplaceHediff(this.Props.hediffIfHumanlike);
            } else if (this.Pawn.RaceProps.IsMechanoid && this.Props.hediffIfMech != null) {
                this.ReplaceHediff(this.Props.hediffIfMech);
            } else if (this.Pawn.RaceProps.IsDrone && this.Props.hediffIfDrone != null) {
                this.ReplaceHediff(this.Props.hediffIfDrone);
            } else if (this.Pawn.IsMutant && this.Props.hediffIfMutant != null) {
                this.ReplaceHediff(this.Props.hediffIfMutant);
            } else if (this.Pawn.RaceProps.IsFlesh) {
                if (this.Props.hediffIfOrganic != null)
                {
                    this.ReplaceHediff(this.Props.hediffIfOrganic);
                }
            } else if (this.Props.hediffIfInorganic != null) {
                this.ReplaceHediff(this.Props.hediffIfInorganic);
                return;
            }
        }
        public void ReplaceHediff(HediffDef newHediff)
        {
            Hediff hediff = HediffMaker.MakeHediff(newHediff, this.Pawn);
            HediffComp_Disappears hcd = this.parent.TryGetComp<HediffComp_Disappears>();
            HediffComp_Disappears hcd2 = hediff.TryGetComp<HediffComp_Disappears>();
            if (hcd != null && hcd2 != null)
            {
                hcd2.ticksToDisappear = hcd.ticksToDisappear;
            }
            this.Pawn.health.AddHediff(hediff, this.parent.Part);
            this.Pawn.health.RemoveHediff(this.parent);
        }
    }
    public class HediffCompProperties_SatisfiesNeeds : HediffCompProperties
    {
        public HediffCompProperties_SatisfiesNeeds()
        {
            this.compClass = typeof(HediffComp_SatisfiesNeeds);
        }
        public int periodicity;
        public Dictionary<NeedDef, float> needsSatisfied;
        public bool satisfiesDrugAddictions;
        public float drugAddictionSatisfaction;
    }
    public class HediffComp_SatisfiesNeeds : HediffComp
    {
        public HediffCompProperties_SatisfiesNeeds Props
        {
            get
            {
                return (HediffCompProperties_SatisfiesNeeds)this.props;
            }
        }
        public virtual bool ConditionsMetToSatisfyNeeds
        {
            get
            {
                return true;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity,delta) && this.ConditionsMetToSatisfyNeeds)
            {
                foreach (Need n in this.Pawn.needs.AllNeeds)
                {
                    if (this.Props.needsSatisfied.ContainsKey(n.def))
                    {
                        n.CurLevel += this.Props.needsSatisfied.TryGetValue(n.def);
                    } else if (this.Props.satisfiesDrugAddictions && n.def.needClass == typeof(Need_Chemical)) {
                        n.CurLevel += this.Props.drugAddictionSatisfaction;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_SkillAdjustment : HediffCompProperties
    {
        public HediffCompProperties_SkillAdjustment()
        {
            this.compClass = typeof(HediffComp_SkillAdjustment);
        }
        public int ticks = 2500;
        public int minLevel = 0;
        public int maxLevel = int.MaxValue;
        public float skillAdjustment = -2000;
        public bool affectsAptitudes = false;
        public List<SkillDef> affectedSkills;
        public bool showInTooltip = true;
        public List<TraitDef> nullifyingTraits;
        public StatDef statMultiplier;
        public StatDef statResistor;
    }
    public class HediffComp_SkillAdjustment : HediffComp
    {
        public HediffCompProperties_SkillAdjustment Props
        {
            get
            {
                return (HediffCompProperties_SkillAdjustment)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (!this.Props.showInTooltip)
                {
                    return base.CompLabelInBracketsExtra;
                }
                string result = "";
                foreach (SkillDef s in this.Props.affectedSkills)
                {
                    result += "Hauts_SkillAdjustmentTooltip".Translate(Mathf.RoundToInt(60000f/(this.Props.skillAdjustment*this.Props.ticks)), s.LabelCap) + "\n";
                }
                return result;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.affectsAptitudes = this.Props.affectsAptitudes;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.ticks,delta))
            {
                if (this.Props.nullifyingTraits != null && this.Pawn.story != null)
                {
                    foreach (TraitDef td in this.Props.nullifyingTraits)
                    {
                        if (this.Pawn.story.traits.HasTrait(td))
                        {
                            return;
                        }
                    }
                }
                float skillAdjustment = this.Props.skillAdjustment * (this.Props.statMultiplier != null ? this.Pawn.GetStatValue(this.Props.statMultiplier) : 1f) / (this.Props.statResistor != null ? Math.Max(0.001f, this.Pawn.GetStatValue(this.Props.statResistor)) : 1f);
                foreach (SkillRecord s in this.Pawn.skills.skills)
                {
                    if (this.Props.affectedSkills.Contains(s.def) && s.GetLevel(this.affectsAptitudes) >= this.Props.minLevel && s.GetLevel(this.affectsAptitudes) < this.Props.maxLevel)
                    {
                        float skillAdjustment2 = skillAdjustment;
                        if (skillAdjustment2 < 0f)
                        {
                            if (this.Pawn.story != null && this.Pawn.story.traits.HasTrait(TraitDefOf.GreatMemory))
                            {
                                skillAdjustment2 *= 0.5f;
                            }
                            skillAdjustment2 *= this.ForgettingSpeed(s);
                        }
                        s.Learn(skillAdjustment2, true);
                        if (s.Level < 0)
                        {
                            s.Level = 0;
                        }
                    }
                }
            }
        }
        public float ForgettingSpeed(SkillRecord skill)
        {
            return 1f;
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.affectsAptitudes, "affectsAptitudes", false, false);
        }
        private bool affectsAptitudes;
    }
    //hediffcomps whose effects are based on severity
    public class HediffCompProperties_ChangeAboveSeverity : HediffCompProperties
    {
        public HediffCompProperties_ChangeAboveSeverity()
        {
            this.compClass = typeof(HediffComp_ChangeAboveSeverity);
        }
        public float aboveThisSeverity = 1f;
        public HediffDef alternativeHediff;
        public bool addToBrain = false;
    }
    public class HediffComp_ChangeAboveSeverity : HediffComp
    {
        public HediffCompProperties_ChangeAboveSeverity Props
        {
            get
            {
                return (HediffCompProperties_ChangeAboveSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity > this.Props.aboveThisSeverity)
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.alternativeHediff, this.Pawn);
                if (this.Props.addToBrain)
                {
                    this.Pawn.health.AddHediff(hediff, this.Pawn.health.hediffSet.GetBrain());
                } else {
                    this.Pawn.health.AddHediff(hediff,this.parent.Part);
                }
                hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
    }
    public class HediffCompProperties_ChangeBelowSeverity : HediffCompProperties
    {
        public HediffCompProperties_ChangeBelowSeverity()
        {
            this.compClass = typeof(HediffComp_ChangeBelowSeverity);
        }
        public float atOrBelowThisSeverity = 1f;
        public HediffDef alternativeHediff;
        public bool addToBrain = false;
    }
    public class HediffComp_ChangeBelowSeverity : HediffComp
    {
        public HediffCompProperties_ChangeBelowSeverity Props
        {
            get
            {
                return (HediffCompProperties_ChangeBelowSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity <= this.Props.atOrBelowThisSeverity)
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.alternativeHediff, this.Pawn, null);
                if (this.Props.addToBrain)
                {
                    this.Pawn.health.AddHediff(hediff, this.Pawn.health.hediffSet.GetBrain());
                } else {
                    this.Pawn.health.AddHediff(hediff,this.parent.Part);
                }
                hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
    }
    public class HediffCompProperties_ChangeIfSeverityVsHitPoints : HediffCompProperties
    {
        public HediffCompProperties_ChangeIfSeverityVsHitPoints()
        {
            this.compClass = typeof(HediffComp_ChangeIfSeverityVsHitPoints);
        }
        public HediffDef alternativeHediff;
        public bool addToBrain = false;
        public bool ifAbove = true;
        public bool ifBelow = false;
        public bool showSeverityOverHitPoints = true;
    }
    public class HediffComp_ChangeIfSeverityVsHitPoints : HediffComp
    {
        public HediffCompProperties_ChangeIfSeverityVsHitPoints Props
        {
            get
            {
                return (HediffCompProperties_ChangeIfSeverityVsHitPoints)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showSeverityOverHitPoints)
                {
                    return base.CompTipStringExtra + "\n" + (int)this.parent.Severity + "/" + this.CurrentHitPoints;
                }
                return base.CompTipStringExtra;
            }
        }
        public virtual bool ShouldTransform()
        {
            return (this.Props.ifAbove && this.parent.Severity > this.CurrentHitPoints) || (this.Props.ifBelow && this.parent.Severity < this.CurrentHitPoints);
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.ShouldTransform())
            {
                this.TransformThis();
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        protected virtual void TransformThis()
        {
            Hediff hediff = HediffMaker.MakeHediff(this.Props.alternativeHediff, this.Pawn);
            if (this.Props.addToBrain)
            {
                this.Pawn.health.AddHediff(hediff, this.Pawn.health.hediffSet.GetBrain());
            } else {
                this.Pawn.health.AddHediff(hediff, this.parent.Part);
            }
        }
        protected float CurrentHitPoints
        {
            get
            {
                return HautsUtility.HitPointTotalFor(this.Pawn);
            }
        }
    }
    public class HediffCompProperties_KillBelowSeverity : HediffCompProperties
    {
        public HediffCompProperties_KillBelowSeverity()
        {
            this.compClass = typeof(HediffComp_KillBelowSeverity);
        }
        public float killBelow = 0.002f;
    }
    public class HediffComp_KillBelowSeverity : HediffComp
    {
        public HediffCompProperties_KillBelowSeverity Props
        {
            get
            {
                return (HediffCompProperties_KillBelowSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity < this.Props.killBelow)
            {
                this.Pawn.Kill(null, null);
            }
        }
    }
    public class HediffCompProperties_SeverityDeterminesVisibility : HediffCompProperties
    {
        public HediffCompProperties_SeverityDeterminesVisibility()
        {
            this.compClass = typeof(HediffComp_SeverityDeterminesVisibility);
        }
        public FloatRange invisibleWithin = new FloatRange(-999f,-998f);
        public FloatRange visibleWithin = new FloatRange(-999f, -998f);
    }
    public class HediffComp_SeverityDeterminesVisibility : HediffComp
    {
        public HediffCompProperties_SeverityDeterminesVisibility Props
        {
            get
            {
                return (HediffCompProperties_SeverityDeterminesVisibility)this.props;
            }
        }
        public override bool CompDisallowVisible()
        {
            if (this.parent.Severity >= this.Props.invisibleWithin.min && this.parent.Severity < this.Props.invisibleWithin.max)
            {
                return true;
            }
            if (this.parent.Severity >= this.Props.visibleWithin.min && this.parent.Severity < this.Props.visibleWithin.max)
            {
                return false;
            }
            return false;
        }
    }
    public class HediffCompProperties_CreateHediffBySpendingSeverity : HediffCompProperties
    {
        public HediffCompProperties_CreateHediffBySpendingSeverity()
        {
            this.compClass = typeof(HediffComp_CreateHediffBySpendingSeverity);
        }
        public float severityToTrigger;
        public HediffDef hediffGiven;
        public float maxSeverityOfCreatedHediff = -1f;
        public float severityToGive;
        public bool showProgressInTooltip;
    }
    public class HediffComp_CreateHediffBySpendingSeverity : HediffComp
    {
        public HediffCompProperties_CreateHediffBySpendingSeverity Props
        {
            get
            {
                return (HediffCompProperties_CreateHediffBySpendingSeverity)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawn".Translate(this.parent.Severity.ToStringByStyle(ToStringStyle.FloatMaxTwo), this.Props.severityToTrigger.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta) && this.parent.Severity >= this.Props.severityToTrigger)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffGiven);
                if (hediff != null)
                {
                    if (hediff.Severity < this.Props.maxSeverityOfCreatedHediff || this.Props.maxSeverityOfCreatedHediff <= 0f)
                    {
                        hediff.Severity = Math.Min(hediff.Severity + this.Props.severityToGive, this.Props.maxSeverityOfCreatedHediff);
                        this.parent.Severity -= this.Props.severityToTrigger;
                    }
                } else {
                    Hediff hediff2 = HediffMaker.MakeHediff(this.Props.hediffGiven, this.Pawn);
                    this.Pawn.health.AddHediff(hediff2);
                    this.parent.Severity -= this.Props.severityToTrigger;
                }
            }
        }
    }
    public class HediffCompProperties_CreateHediffPeriodically : HediffCompProperties
    {
        public HediffCompProperties_CreateHediffPeriodically()
        {
            this.compClass = typeof(HediffComp_CreateHediffPeriodically);
        }
        public IntRange ticksToNextSpawn;
        public HediffDef hediffGiven;
        public float maxSeverityOfCreatedHediff = -1f;
        public int maxStoredCharges = 1;
        public bool maxChargesScaleWithSeverity = false;
        public int startingCharges = 1;
        public float severityToGive;
        public bool showProgressInTooltip;
    }
    public class HediffComp_CreateHediffPeriodically : HediffComp
    {
        public HediffCompProperties_CreateHediffPeriodically Props
        {
            get
            {
                return (HediffCompProperties_CreateHediffPeriodically)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawnCountdown".Translate((this.ticksRemaining/2500f).ToStringByStyle(ToStringStyle.FloatMaxOne)) + "Hauts_ChargesStored".Translate(this.charges,this.maxCharges);
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.charges = this.Props.startingCharges;
            this.maxCharges = this.Props.maxStoredCharges * (int)(this.Props.maxChargesScaleWithSeverity ? Math.Max(this.parent.Severity,1f) : 1f);
            if (this.charges < this.maxCharges)
            {
                this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
            } else {
                this.ticksRemaining = 0;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                this.maxCharges = this.Props.maxStoredCharges * (int)(this.Props.maxChargesScaleWithSeverity ? Math.Max(this.parent.Severity, 1f) : 1f);
            }
            if (this.ticksRemaining <= 0)
            {
                if (this.charges < this.maxCharges)
                {
                    this.charges++;
                    if (this.charges < this.maxCharges)
                    {
                        this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
                    }
                }
            } else {
                this.ticksRemaining -= delta;
            }
            if (this.charges > 0)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffGiven);
                if (hediff != null)
                {
                    if (hediff.Severity < this.Props.maxSeverityOfCreatedHediff || this.Props.maxSeverityOfCreatedHediff < 0f)
                    {
                        hediff.Severity = hediff.Severity + this.Props.severityToGive;
                        this.charges--;
                        this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
                    }
                } else {
                    Hediff hediff2 = HediffMaker.MakeHediff(this.Props.hediffGiven, this.Pawn);
                    this.Pawn.health.AddHediff(hediff2);
                    this.charges--;
                    this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining", 0, false);
            Scribe_Values.Look<int>(ref this.charges, "charges", 0, false);
            Scribe_Values.Look<int>(ref this.maxCharges, "maxCharges", 1, false);
        }
        public int ticksRemaining;
        public int charges;
        public int maxCharges;
    }
    public class HediffCompProperties_CreateThingsBySpendingSeverity : HediffCompProperties
    {
        public HediffCompProperties_CreateThingsBySpendingSeverity()
        {
            this.compClass = typeof(HediffComp_CreateThingsBySpendingSeverity);
        }
        public float severityToTrigger;
        public Dictionary<ThingDef, FloatRange> spawnedThingAndCountPerTrigger;
        public bool minify = true;
        public bool setToOwnFaction = true;
        public bool spawnInOwnInventory = true;
        public float spawnRadius;
        public bool showProgressInTooltip = true;
        public bool fullyRandomizeSpawns = false;
        public FleckDef spawnFleck1;
        public FleckDef spawnFleck2;
        public SoundDef spawnSound;
    }
    public class HediffComp_CreateThingsBySpendingSeverity : HediffComp
    {
        public HediffCompProperties_CreateThingsBySpendingSeverity Props
        {
            get
            {
                return (HediffCompProperties_CreateThingsBySpendingSeverity)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawn".Translate(this.parent.Severity.ToStringByStyle(ToStringStyle.FloatMaxTwo), this.Props.severityToTrigger.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta) && this.parent.Severity >= this.Props.severityToTrigger && (this.Pawn.Spawned || this.Props.spawnInOwnInventory))
            {
                this.SpawnThings();
            }
        }
        public virtual KeyValuePair<ThingDef, FloatRange> GetThingToSpawn()
        {
            return this.Props.spawnedThingAndCountPerTrigger.RandomElement();
        }
        public void SpawnThings()
        {
            KeyValuePair<ThingDef, FloatRange> toSpawn = this.GetThingToSpawn();
            ThingDef def = toSpawn.Key;
            float countPerTrigger = toSpawn.Value.RandomInRange;
            if (this.Props.spawnInOwnInventory && !def.EverHaulable)
            {
                Log.Error("Hauts_CantSpawnUnminified".Translate(def.LabelCap, this.Pawn.Name.ToStringFull));
                return;
            }
            this.SpawningBlock(def, countPerTrigger);
        }
        public void SpawningBlock(ThingDef def, float countPerTrigger)
        {
            this.FinalizeThingBeforePlacing(def, countPerTrigger, out Thing thing);
            while (this.parent.Severity >= this.Props.severityToTrigger)
            {
                if (this.Props.spawnInOwnInventory)
                {
                    this.AddToInventory(thing);
                } else {
                    if (this.Pawn.Spawned)
                    {
                        this.SpawnInRadius(thing);
                    } else if (this.Pawn.IsCaravanMember()) {
                        this.AddToInventory(thing);
                    }
                }
                this.parent.Severity -= this.Props.severityToTrigger;
                if (!this.Props.fullyRandomizeSpawns && this.parent.Severity >= this.Props.severityToTrigger)
                {
                    this.FinalizeThingBeforePlacing(def, countPerTrigger, out thing);
                }
            }
            if (this.Props.spawnSound != null && this.Pawn.Spawned)
            {
                this.Props.spawnSound.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
            }
        }
        public void FinalizeThingBeforePlacing(ThingDef def, float countPerTrigger, out Thing thing)
        {
            ThingDef stuff = GenStuff.RandomStuffFor(def);
            thing = ThingMaker.MakeThing(def, stuff);
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Colony);
            }
            if (thing.def.Minifiable && (this.Props.minify || this.Props.spawnInOwnInventory))
            {
                thing = thing.MakeMinified();
            }
            if (thing.def.CanHaveFaction)
            {
                if (this.Props.setToOwnFaction)
                {
                    thing.SetFaction(this.Pawn.Faction, null);
                }
            }
            thing.stackCount = Math.Min((int)Math.Floor(countPerTrigger), def.stackLimit);
        }
        public void AddToInventory(Thing thing)
        {
            this.Pawn.inventory.innerContainer.TryAdd(thing, true);
        }
        public virtual void SpawnInRadius(Thing thing)
        {
            IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 6, null);
            if (thing.def.category == ThingCategory.Plant && this.Pawn.Map.wildPlantSpawner.CurrentWholeMapNumNonZeroFertilityCells > 0)
            {
                List<IntVec3> fertileCells = new List<IntVec3>();
                CellRect cellRect = CellRect.WholeMap(this.Pawn.Map);
                using (CellRect.Enumerator enumerator = cellRect.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.GetFertility(this.Pawn.Map) >= thing.def.plant.fertilityMin)
                        {
                            fertileCells.Add(enumerator.Current);
                        }
                    }
                }
                if (fertileCells.Count > 0)
                {
                    bool foundFertileLoc = false;
                    int tries = 1000;
                    while (!foundFertileLoc && tries > 0)
                    {
                        loc = fertileCells.RandomElement();
                        if (loc.Standable(this.Pawn.Map) && !loc.Fogged(this.Pawn.Map))
                        {
                            Plant plant = loc.GetPlant(this.Pawn.Map);
                            if ((plant == null || plant.def.plant.growDays <= 10f) && !loc.GetTerrain(this.Pawn.Map).avoidWander && (!loc.Roofed(this.Pawn.Map) || !thing.def.plant.interferesWithRoof))
                            {
                                foundFertileLoc = true;
                            }
                        }
                        tries--;
                    }
                }
                Plant plant2 = loc.GetPlant(this.Pawn.Map);
                if (plant2 != null)
                {
                    plant2.Destroy(DestroyMode.Vanish);
                }
                this.parent.Severity -= this.Props.severityToTrigger;
                GenSpawn.Spawn(thing, loc, this.Pawn.Map, Rot4.North, WipeMode.Vanish, false);
                ((Plant)thing).Growth = Rand.Value * 0.1f;
            } else {
                loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, (int)Math.Ceiling(this.Props.spawnRadius), null);
                GenPlace.TryPlaceThing(thing, loc, this.Pawn.Map, ThingPlaceMode.Near, null, null, default);
            }
            thing.Notify_DebugSpawned();
            if (thing.SpawnedOrAnyParentSpawned && thing.Map != null && this.Props.spawnFleck1 != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, this.Props.spawnFleck1, 1f);
                dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                thing.Map.flecks.CreateFleck(dataStatic);
                if (this.Props.spawnFleck2 != null)
                {
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, this.Props.spawnFleck2, 1f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    thing.Map.flecks.CreateFleck(dataStatic2);
                }
            }
        }
    }
    public class HediffCompProperties_CreateThingsPeriodically : HediffCompProperties
    {
        public HediffCompProperties_CreateThingsPeriodically()
        {
            this.compClass = typeof(HediffComp_CreateThingsPeriodically);
        }
        public IntRange ticksToNextSpawn;
        public Dictionary<ThingDef, FloatRange> spawnedThingAndCountPerTrigger;
        public bool minify = true;
        public bool setToOwnFaction = true;
        public bool spawnInOwnInventory = true;
        public float spawnRadius;
        public bool showProgressInTooltip = true;
        public bool fullyRandomizeSpawns = false;
        public FleckDef spawnFleck1;
        public FleckDef spawnFleck2;
        public SoundDef spawnSound;
    }
    public class HediffComp_CreateThingsPeriodically : HediffComp
    {
        public HediffCompProperties_CreateThingsPeriodically Props
        {
            get
            {
                return (HediffCompProperties_CreateThingsPeriodically)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawnCountdown".Translate((this.ticksRemaining / 2500f).ToStringByStyle(ToStringStyle.FloatMaxOne));
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.ticksRemaining <= 0 && (this.Pawn.Spawned || this.Props.spawnInOwnInventory))
            {
                this.SpawnThings();
            } else {
                this.ticksRemaining -= delta;
            }
        }
        public virtual KeyValuePair<ThingDef, FloatRange> GetThingToSpawn()
        {
            return this.Props.spawnedThingAndCountPerTrigger.RandomElement();
        }
        public virtual void SpawnThings()
        {
            KeyValuePair<ThingDef, FloatRange> toSpawn = this.GetThingToSpawn();
            ThingDef def = toSpawn.Key;
            float countPerTrigger = toSpawn.Value.RandomInRange;
            if (this.Props.spawnInOwnInventory && !def.EverHaulable)
            {
                Log.Error("Hauts_CantSpawnUnminified".Translate(def.LabelCap, this.Pawn.Name.ToStringFull));
                return;
            }
            this.SpawningBlock(def, countPerTrigger);
        }
        public void SpawningBlock(ThingDef def, float countPerTrigger)
        {
            this.FinalizeThingBeforePlacing(def, countPerTrigger, out Thing thing);
            if (this.Props.spawnInOwnInventory)
            {
                this.AddToInventory(thing);
            }
            else
            {
                if (this.Pawn.Spawned)
                {
                    this.SpawnInRadius(thing);
                }
                else if (this.Pawn.IsCaravanMember())
                {
                    this.AddToInventory(thing);
                }
            }
            this.ticksRemaining = this.Props.ticksToNextSpawn.RandomInRange;
            if (this.Props.spawnSound != null && this.Pawn.Spawned)
            {
                this.Props.spawnSound.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
            }
        }
        public void FinalizeThingBeforePlacing(ThingDef def, float countPerTrigger, out Thing thing)
        {
            ThingDef stuff = GenStuff.RandomStuffFor(def);
            thing = ThingMaker.MakeThing(def, stuff);
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Colony);
            }
            if (thing.def.Minifiable && (this.Props.minify || this.Props.spawnInOwnInventory))
            {
                thing = thing.MakeMinified();
            }
            if (this.Props.setToOwnFaction && thing.def.CanHaveFaction)
            {
                thing.SetFaction(this.Pawn.Faction, null);
            }
            thing.stackCount = Math.Min((int)Math.Floor(countPerTrigger), def.stackLimit);
        }
        public void AddToInventory(Thing thing)
        {
            this.Pawn.inventory.innerContainer.TryAdd(thing, true);
        }
        public virtual void SpawnInRadius(Thing thing)
        {
            IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 6, null);
            if (thing.def.category == ThingCategory.Plant && this.Pawn.Map.wildPlantSpawner.CurrentWholeMapNumNonZeroFertilityCells > 0)
            {
                List<IntVec3> fertileCells = new List<IntVec3>();
                CellRect cellRect = CellRect.WholeMap(this.Pawn.Map);
                using (CellRect.Enumerator enumerator = cellRect.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.GetFertility(this.Pawn.Map) >= thing.def.plant.fertilityMin)
                        {
                            fertileCells.Add(enumerator.Current);
                        }
                    }
                }
                if (fertileCells.Count > 0)
                {
                    bool foundFertileLoc = false;
                    int tries = 1000;
                    while (!foundFertileLoc && tries > 0)
                    {
                        loc = fertileCells.RandomElement();
                        if (loc.Standable(this.Pawn.Map) && !loc.Fogged(this.Pawn.Map))
                        {
                            Plant plant = loc.GetPlant(this.Pawn.Map);
                            if ((plant == null || plant.def.plant.growDays <= 10f) && !loc.GetTerrain(this.Pawn.Map).avoidWander && (!loc.Roofed(this.Pawn.Map) || !thing.def.plant.interferesWithRoof))
                            {
                                foundFertileLoc = true;
                            }
                        }
                        tries--;
                    }
                }
                Plant plant2 = loc.GetPlant(this.Pawn.Map);
                if (plant2 != null)
                {
                    plant2.Destroy(DestroyMode.Vanish);
                }
                GenSpawn.Spawn(thing, loc, this.Pawn.Map, Rot4.North, WipeMode.Vanish, false);
                ((Plant)thing).Growth = Rand.Value * 0.1f;
            }
            else
            {
                loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, (int)Math.Ceiling(this.Props.spawnRadius), null);
                GenPlace.TryPlaceThing(thing, loc, this.Pawn.Map, ThingPlaceMode.Near, null, null, default);
            }
            thing.Notify_DebugSpawned();
            if (thing.SpawnedOrAnyParentSpawned && thing.Map != null && this.Props.spawnFleck1 != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, this.Props.spawnFleck1, 1f);
                dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                thing.Map.flecks.CreateFleck(dataStatic);
                if (this.Props.spawnFleck2 != null)
                {
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, this.Props.spawnFleck2, 1f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    thing.Map.flecks.CreateFleck(dataStatic2);
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining", 0, false);
        }
        public int ticksRemaining;
    }
    //hediffcomps that change severity
    public class HediffCompProperties_AbilityCooldownSeverity : HediffCompProperties
    {
        public HediffCompProperties_AbilityCooldownSeverity()
        {
            this.compClass = typeof(HediffComp_AbilityCooldownSeverity);
        }
        public float sevBonusPerGrantedAbilityOnCD;
    }
    public class HediffComp_AbilityCooldownSeverity : HediffComp
    {
        public HediffCompProperties_AbilityCooldownSeverity Props
        {
            get
            {
                return (HediffCompProperties_AbilityCooldownSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            float severity = this.parent.def.initialSeverity;
            if (this.parent.AllAbilitiesForReading != null && this.parent.AllAbilitiesForReading.Count > 0)
            {
                foreach (RimWorld.Ability a in this.parent.AllAbilitiesForReading)
                {
                    if (a.OnCooldown)
                    {
                        severity += this.Props.sevBonusPerGrantedAbilityOnCD;
                    }
                }
            }
            this.parent.Severity = severity;
        }
    }
    public class HediffCompProperties_BreakRiskSeverity : HediffCompProperties
    {
        public HediffCompProperties_BreakRiskSeverity()
        {
            this.compClass = typeof(HediffComp_BreakRiskSeverity);
        }
        public bool activeDuringMentalStates = true;
        public float severityIfInMentalState = 0.001f;
        public bool removeFromMoodless = false;
        public float severityForMoodless = 0.001f;
        public float extremeMBseverity = 1f;
        public float majorMBseverity = 2f;
        public float minorMBseverity = 3f;
        public float neutralSeverity = 4f;
        public float contentSeverity = 5f;
        public float happySeverity = 6f;
    }
    public class HediffComp_BreakRiskSeverity : HediffComp
    {
        public HediffCompProperties_BreakRiskSeverity Props
        {
            get
            {
                return (HediffCompProperties_BreakRiskSeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.Pawn.needs.mood == null && this.Props.removeFromMoodless)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                if (this.Pawn.needs.mood != null)
                {
                    if (this.Pawn.MentalState == null || this.Props.activeDuringMentalStates)
                    {
                        Need_Mood mood = this.Pawn.needs.mood;
                        if (mood != null)
                        {
                            if (mood.CurLevel < this.Pawn.mindState.mentalBreaker.BreakThresholdExtreme)
                            {
                                this.parent.Severity = this.Props.extremeMBseverity;
                            } else if (mood.CurLevel < this.Pawn.mindState.mentalBreaker.BreakThresholdMajor) {
                                this.parent.Severity = this.Props.majorMBseverity;
                            } else if (mood.CurLevel < this.Pawn.mindState.mentalBreaker.BreakThresholdMinor) {
                                this.parent.Severity = this.Props.minorMBseverity;
                            } else if (mood.CurLevel < 0.65f) {
                                this.parent.Severity = this.Props.neutralSeverity;
                            } else if (mood.CurLevel < 0.9f) {
                                this.parent.Severity = this.Props.contentSeverity;
                            } else {
                                this.parent.Severity = this.Props.happySeverity;
                            }
                        }
                    } else {
                        this.parent.Severity = this.Props.severityIfInMentalState;
                    }
                } else {
                    this.parent.Severity = this.Props.severityForMoodless;
                }
            }
        }
    }
    public class HediffCompProperties_CapacitySeverity : HediffCompProperties
    {
        public HediffCompProperties_CapacitySeverity()
        {
            this.compClass = typeof(HediffComp_CapacitySeverity);
        }
        public PawnCapacityDef capacity;
        public bool plusInitialSeverity;
    }
    public class HediffComp_CapacitySeverity : HediffComp
    {
        public HediffCompProperties_CapacitySeverity Props
        {
            get
            {
                return (HediffCompProperties_CapacitySeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(250, delta))
            {
                this.parent.Severity = this.Pawn.health.capacities.GetLevel(this.Props.capacity) + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    public class HediffCompProperties_ChangeSeverityOnIngestion : HediffCompProperties
    {
        public HediffCompProperties_ChangeSeverityOnIngestion()
        {
            this.compClass = typeof(HediffComp_ChangeSeverityOnIngestion);
        }
        public FloatRange severityPerNutritionIngested;
        public int minAgeTicksToFunction = 1;
    }
    public class HediffComp_ChangeSeverityOnIngestion : HediffComp
    {
        public HediffCompProperties_ChangeSeverityOnIngestion Props
        {
            get
            {
                return (HediffCompProperties_ChangeSeverityOnIngestion)this.props;
            }
        }
    }
    public class HediffCompProperties_ChangeSeverityOnVerbUse : HediffCompProperties
    {
        public HediffCompProperties_ChangeSeverityOnVerbUse()
        {
            this.compClass = typeof(HediffComp_ChangeSeverityOnVerbUse);
        }
        public float severityGainedOnUse = 0f;
        public float setSeverity = -1f;
        public int minAgeTicksToFunction = 1;
        public bool pilferingCountsAsVerb;
        [TranslationHandle]
        public Type specificVerbType = typeof(Verb);
    }
    public class HediffComp_ChangeSeverityOnVerbUse : HediffComp
    {
        public HediffCompProperties_ChangeSeverityOnVerbUse Props
        {
            get
            {
                return (HediffCompProperties_ChangeSeverityOnVerbUse)this.props;
            }
        }
        public virtual void AdjustSeverity()
        {
            if (this.Props.setSeverity != -1f)
            {
                this.parent.Severity = this.Props.setSeverity;
            }
            if (this.Props.severityGainedOnUse != 0f)
            {
                this.parent.Severity += this.Props.severityGainedOnUse;
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (this.parent.ageTicks >= this.Props.minAgeTicksToFunction)
            {
                if (this.Props.specificVerbType == null || this.Props.specificVerbType == typeof(Verb) || verb.GetType().IsAssignableFrom(this.Props.specificVerbType))
                {
                    this.AdjustSeverity();
                }
            }
        }
    }
    public class HediffCompProperties_ExpectationSeverity : HediffCompProperties
    {
        public HediffCompProperties_ExpectationSeverity()
        {
            this.compClass = typeof(HediffComp_ExpectationSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_ExpectationSeverity : HediffComp
    {
        public HediffCompProperties_ExpectationSeverity Props
        {
            get
            {
                return (HediffCompProperties_ExpectationSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(250, delta))
            {
                this.parent.Severity = ExpectationsUtility.CurrentExpectationFor(this.parent.pawn).order + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    public class HediffCompProperties_GasSeverity : HediffCompProperties
    {
        public HediffCompProperties_GasSeverity()
        {
            this.compClass = typeof(HediffComp_GasSeverity);
        }
        public float whileNotInGas = -999f;
        public float perTickNoGas = 0f;
        public float whileInGas = -999f;
        public float perTickInGas = 0f;
        public List<GasType> gasTypes;
    }
    public class HediffComp_GasSeverity : HediffComp
    {
        public HediffCompProperties_GasSeverity Props
        {
            get
            {
                return (HediffCompProperties_GasSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15,delta) && this.Pawn.Spawned)
            {
                bool inGas = false;
                foreach (GasType g in this.Props.gasTypes)
                {
                    if (this.Pawn.Position.AnyGas(this.Pawn.Map, g))
                    {
                        inGas = true;
                        if (this.Props.whileInGas != -999f)
                        {
                            this.parent.Severity = this.Props.whileInGas;
                            break;
                        }
                        else
                        {
                            this.parent.Severity += this.Props.perTickInGas * 15f;
                        }
                    }
                }
                if (!inGas)
                {
                    if (this.Props.whileNotInGas != -999f)
                    {
                        this.parent.Severity = this.Props.whileNotInGas;
                    }
                    else
                    {
                        this.parent.Severity += this.Props.perTickNoGas * 15f;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_HasTraitSeverity : HediffCompProperties
    {
        public HediffCompProperties_HasTraitSeverity()
        {
            this.compClass = typeof(HediffComp_GasSeverity);
        }
        public List<TraitDef> traits;
        public float severityIfHas = -999f;
        public float severityIfLacks = -999f;
    }
    public class HediffComp_HasTraitSeverity : HediffComp
    {
        public HediffCompProperties_HasTraitSeverity Props
        {
            get
            {
                return (HediffCompProperties_HasTraitSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(200, delta) && this.parent.pawn.story != null)
            {
                if (this.Props.severityIfLacks != -999f)
                {
                    bool hasAnySuchTrait = false;
                    foreach (TraitDef t in this.Props.traits)
                    {
                        if (this.parent.pawn.story.traits.HasTrait(t))
                        {
                            hasAnySuchTrait = true;
                        }
                    }
                    if (!hasAnySuchTrait)
                    {
                        this.parent.Severity = this.Props.severityIfLacks;
                    }
                }
                else if (this.Props.severityIfHas != -999f)
                {
                    foreach (TraitDef t in this.Props.traits)
                    {
                        if (this.parent.pawn.story.traits.HasTrait(t))
                        {
                            this.parent.Severity = this.Props.severityIfHas;
                        }
                        break;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_IsRestingSeverity : HediffCompProperties
    {
        public HediffCompProperties_IsRestingSeverity()
        {
            this.compClass = typeof(HediffComp_IsRestingSeverity);
        }
        public float whileAwake = -999f;
        public float perTickAwake = 0f;
        public float whileResting = -999f;
        public float perTickResting = 0f;
    }
    public class HediffComp_IsRestingSeverity : HediffComp
    {
        public HediffCompProperties_IsRestingSeverity Props
        {
            get
            {
                return (HediffCompProperties_IsRestingSeverity) this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn pawn = this.Pawn;
            if (pawn.IsHashIntervalTick(15, delta) && pawn.needs != null && pawn.needs.rest != null)
            {
                if (pawn.needs.rest.Resting)
                {
                    if (this.Props.whileResting != -999f)
                    {
                        this.parent.Severity = this.Props.whileResting;
                    } else {
                        this.parent.Severity += this.Props.perTickResting * 15f;
                    }
                } else if (this.Props.whileAwake != -999f) {
                    this.parent.Severity = this.Props.whileAwake;
                } else {
                    this.parent.Severity += this.Props.perTickAwake * 15f;
                }
            }
        }
    }
    public class HediffCompProperties_LightingSeverity : HediffCompProperties
    {
        public HediffCompProperties_LightingSeverity()
        {
            this.compClass = typeof(HediffComp_LightingSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_LightingSeverity : HediffComp
    {
        public HediffCompProperties_LightingSeverity Props
        {
            get
            {
                return (HediffCompProperties_LightingSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(250, delta))
            {
                if (this.parent.pawn.Map != null && this.parent.pawn.Spawned)
                {
                    this.parent.Severity = this.parent.pawn.Map.glowGrid.GroundGlowAt(this.parent.pawn.Position) + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
                else if (this.parent.pawn.Tile != -1)
                {
                    this.parent.Severity = GenCelestial.CelestialSunGlow(this.parent.pawn.Tile, Find.TickManager.TicksAbs) + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
            }
        }
    }
    public class HediffCompProperties_OnCaravanSeverity : HediffCompProperties
    {
        public HediffCompProperties_OnCaravanSeverity()
        {
            this.compClass = typeof(HediffComp_OnCaravanSeverity);
        }
        public float perOnCaravanTick = 0f;
        public float whileOnCaravan = -999f;
        public float perInMapTick = 0f;
        public float whileInMap = -999f;
        public bool respectMinSeverity = false;
    }
    public class HediffComp_OnCaravanSeverity : HediffComp
    {
        public HediffCompProperties_OnCaravanSeverity Props
        {
            get
            {
                return (HediffCompProperties_OnCaravanSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15,delta))
            {
                if (this.Pawn.IsCaravanMember())
                {
                    if (this.Props.whileOnCaravan != -999f)
                    {
                        this.parent.Severity = this.Props.whileOnCaravan;
                    } else {
                        this.parent.Severity += this.Props.perOnCaravanTick * 15f;
                    }
                } else {
                    if (this.Props.whileInMap != -999f)
                    {
                        this.parent.Severity = this.Props.whileInMap;
                    } else {
                        this.parent.Severity += this.Props.perInMapTick * 15f;
                    }
                }
                if (this.parent.Severity < this.parent.def.minSeverity && this.Props.respectMinSeverity)
                {
                    this.parent.Severity = this.parent.def.minSeverity;
                }
            }
        }
    }
    public class HediffCompProperties_RelationDependentSeverity : HediffCompProperties
    {
        public HediffCompProperties_RelationDependentSeverity()
        {
            this.compClass = typeof(HediffComp_RelationDependentSeverity);
        }
        public int periodicity = 2500;
        public bool countPositiveRelations = true;
        public bool countNegativeRelations = true;
        public bool positiveRelationsPositiveSeverity = true;
        public bool plusInitialSeverity;

    }
    public class HediffComp_RelationDependentSeverity : HediffComp
    {
        public HediffCompProperties_RelationDependentSeverity Props
        {
            get
            {
                return (HediffCompProperties_RelationDependentSeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            RecalculateRDS();
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(this.Props.periodicity, delta))
            {
                RecalculateRDS();
            }
        }
        private void RecalculateRDS()
        {
            List<Pawn> list = SocialCardUtility.PawnsForSocialInfo(this.parent.pawn);
            float relevantRelations = 0f;
            foreach (Pawn p in list)
            {
                int opinion = this.parent.pawn.relations.OpinionOf(p);
                if ((opinion > 0 && this.Props.countPositiveRelations) || (opinion < 0 && this.Props.countNegativeRelations))
                {
                    relevantRelations += opinion;
                }
            }
            if (!this.Props.positiveRelationsPositiveSeverity)
            {
                relevantRelations *= -1;
            }
            this.parent.Severity = relevantRelations + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
        }
    }
    public class HediffCompProperties_SeverityDuringSpecificMentalStates : HediffCompProperties
    {
        public HediffCompProperties_SeverityDuringSpecificMentalStates()
        {
            this.compClass = typeof(HediffComp_SeverityDuringSpecificMentalStates);
        }
        public bool anyMentalState = true;
        public List<MentalStateDef> mentalStates;
        public float severityInState = -999f;
        public float severityPerTickInState;
        public float severityOtherwise = -999f;
        public float severityPerTickOtherwise;
    }
    public class HediffComp_SeverityDuringSpecificMentalStates : HediffComp
    {
        public HediffCompProperties_SeverityDuringSpecificMentalStates Props
        {
            get
            {
                return (HediffCompProperties_SeverityDuringSpecificMentalStates)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.pawn.MentalStateDef != null && (this.Props.anyMentalState || this.Props.mentalStates.Contains(this.parent.pawn.MentalStateDef)))
            {
                if (this.Props.severityInState != -999f)
                {
                    this.parent.Severity = this.Props.severityInState;
                } else {
                    this.parent.Severity += this.Props.severityPerTickInState;
                }
            } else if (this.Props.severityOtherwise != -999f) {
                this.parent.Severity = this.Props.severityOtherwise;
            } else {
                this.parent.Severity += this.Props.severityPerTickOtherwise;
            }
        }
    }
    public class HediffCompProperties_SkillLevelSeverity : HediffCompProperties
    {
        public HediffCompProperties_SkillLevelSeverity()
        {
            this.compClass = typeof(HediffComp_SkillLevelSeverity);
        }
        public List<SkillDef> skills;
        public bool plusInitialSeverity;
    }
    public class HediffComp_SkillLevelSeverity : HediffComp
    {
        public HediffCompProperties_SkillLevelSeverity Props
        {
            get
            {
                return (HediffCompProperties_SkillLevelSeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.Pawn.skills == null)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                float netSkill = 0f;
                foreach (SkillDef s in this.Props.skills)
                {
                    netSkill += this.parent.pawn.skills.GetSkill(s).Level;
                }
                this.parent.Severity = netSkill + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    public class HediffCompProperties_StatScalingSeverityPerDay : HediffCompProperties
    {
        public HediffCompProperties_StatScalingSeverityPerDay()
        {
            this.compClass = typeof(HediffComp_StatScalingSeverityPerDay);
        }
        public float baseSeverityPerDay;
        public int periodicity;
        public StatDef stat;
    }
    public class HediffComp_StatScalingSeverityPerDay : HediffComp
    {
        public HediffCompProperties_StatScalingSeverityPerDay Props
        {
            get
            {
                return (HediffCompProperties_StatScalingSeverityPerDay)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity,delta))
            {

                this.parent.Severity += this.Props.baseSeverityPerDay * this.Pawn.GetStatValue(this.Props.stat) *this.Props.periodicity/ 60000f;
            }
        }
    }
    public class HediffCompProperties_StatScalingSeverityWithMin : HediffCompProperties
    {
        public HediffCompProperties_StatScalingSeverityWithMin()
        {
            this.compClass = typeof(HediffComp_StatScalingSeverityWithMin);
        }
        public float baseSeverity = 1f;
        public StatDef statScalar;
        public float minStatToScale = 1f;
    }
    public class HediffComp_StatScalingSeverityWithMin : HediffComp
    {
        public HediffCompProperties_StatScalingSeverityWithMin Props
        {
            get
            {
                return (HediffCompProperties_StatScalingSeverityWithMin)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.parent.pawn.GetStatValue(this.Props.statScalar) >= this.Props.minStatToScale)
            {
                this.parent.Severity = this.Props.baseSeverity * this.parent.pawn.GetStatValue(this.Props.statScalar);
            } else {
                this.parent.Severity = this.Props.baseSeverity;
            }
        }
    }
    public class HediffCompProperties_TemperatureLevelSeverity : HediffCompProperties
    {
        public HediffCompProperties_TemperatureLevelSeverity()
        {
            this.compClass = typeof(HediffComp_TemperatureLevelSeverity);
        }
        public FloatRange zeroSeverityAt;
        public float perTempAbove;
        public float perTempBelow;
        public float changePerTick = 999f;
    }
    public class HediffComp_TemperatureLevelSeverity : HediffComp
    {
        public HediffCompProperties_TemperatureLevelSeverity Props
        {
            get
            {
                return (HediffCompProperties_TemperatureLevelSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15,delta))
            {

                float temp = this.Pawn.AmbientTemperature;
                if (this.Props.zeroSeverityAt.Includes(temp))
                {
                    if (this.parent.Severity > 0f)
                    {
                        this.parent.Severity = Math.Max(0f, this.parent.Severity - (this.Props.changePerTick*15f));
                    } else {
                        this.parent.Severity = Math.Min(0f, this.parent.Severity + (this.Props.changePerTick*15f));
                    }
                } else if (temp > this.Props.zeroSeverityAt.max) {
                    float desiredTemp = this.Props.perTempAbove * (temp - this.Props.zeroSeverityAt.max);
                    if (this.parent.Severity > desiredTemp)
                    {
                        this.parent.Severity = Math.Max(desiredTemp, this.parent.Severity - (this.Props.changePerTick * 15f));
                    } else {
                        this.parent.Severity = Math.Min(desiredTemp, this.parent.Severity + (this.Props.changePerTick * 15f));
                    }
                } else {
                    float desiredTemp = this.Props.perTempBelow * (this.Props.zeroSeverityAt.min - temp);
                    if (this.parent.Severity > desiredTemp)
                    {
                        this.parent.Severity = Math.Max(desiredTemp, this.parent.Severity - (this.Props.changePerTick * 15f));
                    } else {
                        this.parent.Severity = Math.Min(desiredTemp, this.parent.Severity + (this.Props.changePerTick * 15f));
                    }
                }
            }
        }
    }
    public class HediffCompProperties_WaterImmersionSeverity : HediffCompProperties
    {
        public HediffCompProperties_WaterImmersionSeverity()
        {
            this.compClass = typeof(HediffComp_WaterImmersionSeverity);
        }
        public float baseSeverity = 0.001f;
        public float rainCountsFor = 0.051f;
        public float baseSeverityCaravan = 0.001f;
        public SimpleCurve severityPerCaravanRiverSize = new SimpleCurve(new CurvePoint[]
        {
            new CurvePoint(0f, 0f)
        });
        public float caravanWaterTileSeverity = 3f;
        public bool disabledIfNotSlowedInWater;
    }
    public class HediffComp_WaterImmersionSeverity : HediffComp
    {
        public HediffCompProperties_WaterImmersionSeverity Props
        {
            get
            {
                return (HediffCompProperties_WaterImmersionSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15,delta))
            {
                this.parent.Severity = this.Props.baseSeverity;
                if (this.Pawn.Spawned && this.Pawn.PositionHeld.GetTerrain(this.Pawn.MapHeld) != null)
                {
                    if (this.Props.disabledIfNotSlowedInWater)
                    {
                        if (VEF.AnimalBehaviours.StaticCollectionsClass.floating_animals.Contains(this.Pawn))
                        {
                            return;
                        }
                        int? wcc = this.Pawn.WaterCellCost;
                        if (wcc != null && wcc <= 1)
                        {
                            return;
                        }
                    }
                    if (this.Pawn.PositionHeld.GetTerrain(this.Pawn.MapHeld).IsWater)
                    {
                        this.parent.Severity += ((this.Pawn.PositionHeld.GetTerrain(this.Pawn.MapHeld).pathCost) / 100f);
                    }
                    if (this.Pawn.PositionHeld.GetRoof(this.Pawn.MapHeld) == null)
                    {
                        this.parent.Severity += this.Pawn.MapHeld.weatherManager.RainRate * this.Props.rainCountsFor;
                    }
                } else if (this.Pawn.Tile != -1 && this.Pawn.GetCaravan() != null) {
                    this.parent.Severity = this.Props.baseSeverityCaravan;
                    if (Find.WorldGrid[this.Pawn.Tile].WaterCovered)
                    {
                        this.parent.Severity += this.Props.caravanWaterTileSeverity;
                    } else if (this.Pawn.Tile.Tile is SurfaceTile st && !st.Rivers.NullOrEmpty<SurfaceTile.RiverLink>()) {
                        float riverWidth = 0f;
                        foreach (SurfaceTile.RiverLink rl in st.Rivers)
                        {
                            if (rl.river.widthOnMap > riverWidth)
                            {
                                riverWidth = rl.river.widthOnMap;
                            }
                        }
                        this.parent.Severity += this.Props.severityPerCaravanRiverSize.Evaluate(riverWidth);
                    }
                }
            }
        }
    }
    public class HediffCompProperties_WindLevelSeverity : HediffCompProperties
    {
        public HediffCompProperties_WindLevelSeverity()
        {
            this.compClass = typeof(HediffComp_WindLevelSeverity);
        }
        public bool worksEnclosedSpace = false;
        public bool worksUnderThinRoof = true;
        public bool worksUnderThickRoof = true;
        public float worldMapValue = 0.001f;
        public bool plusInitialSeverity;
    }
    public class HediffComp_WindLevelSeverity : HediffComp
    {
        public HediffCompProperties_WindLevelSeverity Props
        {
            get
            {
                return (HediffCompProperties_WindLevelSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(60,delta))
            {
                if (this.parent.pawn.Map != null)
                {
                    if (this.parent.pawn.Spawned)
                    {
                        if (this.parent.pawn.Position.GetRoof(this.parent.pawn.Map) != null)
                        {
                            if (!this.Props.worksEnclosedSpace && !this.parent.pawn.Position.UsesOutdoorTemperature(this.parent.pawn.Map))
                            {
                                this.parent.Severity = 0f;
                                return;
                            }
                            else if (!this.Props.worksUnderThinRoof && !this.parent.pawn.Position.GetRoof(this.parent.pawn.Map).isThickRoof)
                            {
                                this.parent.Severity = 0f;
                                return;
                            }
                            else if (!this.Props.worksUnderThickRoof && this.parent.pawn.Position.GetRoof(this.parent.pawn.Map).isThickRoof)
                            {
                                this.parent.Severity = 0f;
                                return;
                            }
                        }
                        this.parent.Severity = this.Pawn.Map.windManager.WindSpeed + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                    }
                }
                else
                {
                    this.parent.Severity = this.Props.worldMapValue + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
            }
        }
    }
    public class HediffCompProperties_IdeoCertaintySeverity : HediffCompProperties
    {
        public HediffCompProperties_IdeoCertaintySeverity()
        {
            this.compClass = typeof(HediffComp_IdeoCertaintySeverity);
        }
        public bool removeOnApostasy;
        public HediffDef changesToThisOnApostasy;
        public float severityIfNoIdeo = 0.001f;
    }
    public class HediffComp_IdeoCertaintySeverity : HediffComp
    {
        public HediffCompProperties_IdeoCertaintySeverity Props
        {
            get
            {
                return (HediffCompProperties_IdeoCertaintySeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (!ModsConfig.IdeologyActive || this.Pawn.Ideo == null)
            {
                this.parent.Severity = this.Props.severityIfNoIdeo;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta))
            {
                if (ModsConfig.IdeologyActive && this.Pawn.Ideo != null)
                {
                    this.parent.Severity = Math.Max(this.Pawn.ideo.Certainty, 0.001f);
                }
            }
        }
    }
    public class HediffCompProperties_MeditationSeverity : HediffCompProperties
    {
        public HediffCompProperties_MeditationSeverity()
        {
            this.compClass = typeof(HediffComp_MeditationSeverity);
        }
        public float perMeditationTick = 0f;
        public float perNotMedTick = 0f;
        public bool medFocusSpeedMatters = false;
        public float whileMeditating = -999f;
        public float whileNotMeditating = -999f;
    }
    public class HediffComp_MeditationSeverity : HediffComp
    {
        public HediffCompProperties_MeditationSeverity Props
        {
            get
            {
                return (HediffCompProperties_MeditationSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                if (this.Pawn.CurJob != null && (this.Pawn.CurJobDef == JobDefOf.Meditate || this.Pawn.CurJobDef == JobDefOf.Reign) && this.Pawn.jobs.curDriver != null && this.Pawn.jobs.curDriver.OnLastToil)
                {
                    if (this.Props.whileMeditating != -999f)
                    {
                        this.parent.Severity = this.Props.whileMeditating;
                    } else if (this.Props.medFocusSpeedMatters) {
                        this.parent.Severity += this.Props.perMeditationTick * this.Pawn.GetStatValue(StatDefOf.MeditationFocusGain) * 15f;
                    } else {
                        this.parent.Severity += this.Props.perMeditationTick * 15f;
                    }
                } else {
                    if (this.Props.whileNotMeditating != -999f)
                    {
                        this.parent.Severity = this.Props.whileNotMeditating;
                    } else {
                        this.parent.Severity += this.Props.perNotMedTick * 15f;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_PsyfocusSeverity : HediffCompProperties
    {
        public HediffCompProperties_PsyfocusSeverity()
        {
            this.compClass = typeof(HediffComp_PsyfocusSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_PsyfocusSeverity : HediffComp
    {
        public HediffCompProperties_PsyfocusSeverity Props
        {
            get
            {
                return (HediffCompProperties_PsyfocusSeverity)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.pawn.psychicEntropy != null)
            {
                this.parent.Severity = this.Pawn.psychicEntropy.CurrentPsyfocus + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
            }
        }
    }
    public class HediffCompProperties_PsyfocusSpentTracker : HediffCompProperties
    {
        public HediffCompProperties_PsyfocusSpentTracker()
        {
            this.compClass = typeof(HediffComp_PsyfocusSpentTracker);
        }
        public FloatRange severityPerPsyfocus = new FloatRange(100f);
    }
    public class HediffComp_PsyfocusSpentTracker : HediffComp
    {
        public HediffCompProperties_PsyfocusSpentTracker Props
        {
            get
            {
                return (HediffCompProperties_PsyfocusSpentTracker)this.props;
            }
        }
        public virtual void UpdatePsyfocusExpenditure(float offset)
        {
            this.parent.Severity -= offset * this.Props.severityPerPsyfocus.RandomInRange;
        }
    }
    public class HediffCompProperties_PsylinkSeverity : HediffCompProperties
    {
        public HediffCompProperties_PsylinkSeverity()
        {
            this.compClass = typeof(HediffComp_PsylinkSeverity);
        }
        public bool plusInitialSeverity;
    }
    public class HediffComp_PsylinkSeverity : HediffComp
    {
        public HediffCompProperties_PsylinkSeverity Props
        {
            get
            {
                return (HediffCompProperties_PsylinkSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                {
                    Hediff_Level psylink = (Hediff_Level)this.Pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                    if (psylink == null)
                    {
                        this.parent.Severity = psylink.level;
                        return;
                    }
                } else {
                    this.parent.Severity = this.Pawn.GetPsylinkLevel() + (this.Props.plusInitialSeverity ? this.parent.def.initialSeverity : 0f);
                }
            }
        }
    }
    public class HediffCompProperties_AnomalousActivitySeverity : HediffCompProperties
    {
        public HediffCompProperties_AnomalousActivitySeverity()
        {
            this.compClass = typeof(HediffComp_AnomalousActivitySeverity);
        }
        public Dictionary<int, float> severityAtEachLevel;
        public float defaultSeverity;
        public float defaultSeverityAmbientHorror = 2f;
    }
    public class HediffComp_AnomalousActivitySeverity : HediffComp
    {
        public HediffCompProperties_AnomalousActivitySeverity Props
        {
            get
            {
                return (HediffCompProperties_AnomalousActivitySeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(2500, delta) && ModsConfig.AnomalyActive)
            {
                if (Find.Storyteller.difficulty.AnomalyPlaystyleDef == DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror"))
                {
                    this.parent.Severity = this.Props.defaultSeverityAmbientHorror;
                    return;
                }
                this.parent.Severity = this.Props.severityAtEachLevel.TryGetValue(Find.Anomaly.Level, this.Props.defaultSeverity);
            }
        }
    }
    public class HediffCompProperties_SeverityPerDayPerAnomalousActivity : HediffCompProperties
    {
        public HediffCompProperties_SeverityPerDayPerAnomalousActivity()
        {
            this.compClass = typeof(HediffComp_SeverityPerDayPerAnomalousActivity);
        }
        public float CalculateSeverityPerDay()
        {
            if (ModsConfig.AnomalyActive)
            {
                float num = (Find.Storyteller.difficulty.AnomalyPlaystyleDef == DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror") ? this.defaultSeverityPerDayAmbientHorror : this.severityPerDayAtEachLevel.TryGetValue(Find.Anomaly.Level, this.defaultSeverityPerDay)) + this.severityPerDayRange.RandomInRange;
                if (Rand.Chance(this.reverseSeverityChangeChance))
                {
                    num *= -1f;
                }
                return num;
            }
            return 0f;
        }
        public Dictionary<int, float> severityPerDayAtEachLevel;
        public float defaultSeverityPerDay;
        public float defaultSeverityPerDayAmbientHorror;
        public bool showDaysToRecover;
        public bool showHoursToRecover;
        public float mechanitorFactor = 1f;
        public float reverseSeverityChangeChance;
        public FloatRange severityPerDayRange = FloatRange.Zero;
        public float minAge;
    }
    public class HediffComp_SeverityPerDayPerAnomalousActivity : HediffComp_SeverityModifierBase
    {
        private HediffCompProperties_SeverityPerDayPerAnomalousActivity Props
        {
            get
            {
                return (HediffCompProperties_SeverityPerDayPerAnomalousActivity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.severityPerDay = this.Props.CalculateSeverityPerDay();
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.severityPerDay, "severityPerDay", 0f, false);
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                this.severityPerDay = this.Props.CalculateSeverityPerDay();
            }
        }
        public override float SeverityChangePerDay()
        {
            if (base.Pawn.ageTracker.AgeBiologicalYearsFloat < this.Props.minAge)
            {
                return 0f;
            }
            float num = this.severityPerDay;
            HediffStage curStage = this.parent.CurStage;
            float num2 = num * ((curStage != null) ? curStage.severityGainFactor : 1f);
            if (ModsConfig.BiotechActive && MechanitorUtility.IsMechanitor(base.Pawn))
            {
                num2 *= this.Props.mechanitorFactor;
            }
            return num2;
        }
        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (this.Props.showHoursToRecover && this.SeverityChangePerDay() < 0f)
                {
                    return Mathf.RoundToInt(this.parent.Severity / Mathf.Abs(this.SeverityChangePerDay()) * 24f) + "LetterHour".Translate();
                }
                return null;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showDaysToRecover && this.SeverityChangePerDay() < 0f)
                {
                    return "DaysToRecover".Translate((this.parent.Severity / Mathf.Abs(this.SeverityChangePerDay())).ToString("0.0")).Resolve();
                }
                return null;
            }
        }
        public float severityPerDay;
    }
    public class HediffCompProperties_PlanetLayerSeverity : HediffCompProperties
    {
        public HediffCompProperties_PlanetLayerSeverity()
        {
            this.compClass = typeof(HediffComp_PlanetLayerSeverity);
        }
        public float defaultSeverity;
        public int periodicity = 250;
        public Dictionary<PlanetLayerDef, float> setToInLayer;
        public Dictionary<PlanetLayerDef, float> incrementInLayer;
    }
    public class HediffComp_PlanetLayerSeverity : HediffComp
    {
        public HediffCompProperties_PlanetLayerSeverity Props
        {
            get
            {
                return (HediffCompProperties_PlanetLayerSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity, delta) && this.Pawn.Tile != null && this.Pawn.Tile.Layer != null)
            {
                if (!this.Props.setToInLayer.NullOrEmpty() && this.Props.setToInLayer.TryGetValue(this.Pawn.Tile.LayerDef, out float value))
                {
                    this.parent.Severity = value;
                    return;
                }
                if (!this.Props.incrementInLayer.NullOrEmpty() && this.Props.incrementInLayer.TryGetValue(this.Pawn.Tile.LayerDef, out value))
                {
                    this.parent.Severity += value;
                    return;
                }
                this.parent.Severity = this.Props.defaultSeverity;
            }
        }
    }
    public class HediffCompProperties_VacuumSeverity : HediffCompProperties
    {
        public HediffCompProperties_VacuumSeverity()
        {
            this.compClass = typeof(HediffComp_VacuumSeverity);
        }
        public float vacuumThreshold = 0.5f;
        public float perTickInVacuum;
        public float perTickNotInVacuum;
        public float whileInVacuum = -999f;
        public float whileNotInVacuum = -999f;
        public bool onlyInVacuumIfHarmedByIt = true;
        public bool freezeAtVacuumImmunityInVacuum;
    }
    public class HediffComp_VacuumSeverity : HediffComp
    {
        public HediffCompProperties_VacuumSeverity Props
        {
            get
            {
                return (HediffCompProperties_VacuumSeverity)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                Pawn pawn = this.Pawn;
                if (pawn.Spawned && pawn.Map.Biome.inVacuum && pawn.Position.GetVacuum(pawn.Map) >= this.Props.vacuumThreshold)
                {
                    if (!pawn.HarmedByVacuum)
                    {
                        if (this.Props.freezeAtVacuumImmunityInVacuum)
                        {
                            return;
                        }
                        if (this.Props.onlyInVacuumIfHarmedByIt)
                        {
                            this.NonVacuumEffects();
                        } else {
                            this.VacuumEffects();
                        }
                    } else {
                        this.VacuumEffects();
                    }
                } else {
                    this.NonVacuumEffects();
                }
            }
        }
        public void VacuumEffects()
        {
            if (this.Props.whileInVacuum != -999f)
            {
                this.parent.Severity = this.Props.whileInVacuum;
            } else if (this.Props.perTickInVacuum != 0f) {
                this.parent.Severity += this.Props.perTickInVacuum * 15f;
            }
        }
        public void NonVacuumEffects()
        {
            if (this.Props.whileNotInVacuum != -999f)
            {
                this.parent.Severity = this.Props.whileNotInVacuum;
            } else {
                this.parent.Severity += this.Props.perTickNotInVacuum * 15f;
            }
        }
    }
    //ability comps
    public class CompProperties_AbilityAiTargetingDistanceRange : CompProperties_AbilityEffect
    {
        public FloatRange distanceRange;
        public bool mustBeMelee;
    }
    public class CompAbilityEffect_AiTargetingDistanceRange : CompAbilityEffect
    {
        public new CompProperties_AbilityAiTargetingDistanceRange Props
        {
            get
            {
                return (CompProperties_AbilityAiTargetingDistanceRange)this.props;
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (this.parent.pawn.SpawnedOrAnyParentSpawned && (!this.Props.mustBeMelee || this.parent.pawn.equipment.Primary == null || !this.parent.pawn.equipment.Primary.def.IsRangedWeapon) && this.parent.pawn.PositionHeld.DistanceTo(target.Cell) > this.Props.distanceRange.min && this.parent.pawn.PositionHeld.DistanceTo(target.Cell) <= this.Props.distanceRange.max)
            {
                return true;
            }
            return false;
        }
    }
    public class CompProperties_AbilityAiAppliesToSelf : CompProperties_AbilityEffect
    {
        public int periodicity = 250;
    }
    public class CompAbilityEffect_AiAppliesToSelf : CompAbilityEffect
    {
        public new CompProperties_AbilityAiAppliesToSelf Props
        {
            get
            {
                return (CompProperties_AbilityAiAppliesToSelf)this.props;
            }
        }
        public virtual int Periodicity
        {
            get
            {
                return this.Props.periodicity;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.pawn.IsHashIntervalTick(this.Periodicity) && this.parent.pawn.Spawned && !this.parent.pawn.IsColonistPlayerControlled && !this.parent.GizmoDisabled(out string text) && this.parent.def.aiCanUse && (!this.parent.pawn.InMentalState || this.parent.pawn.InAggroMentalState) && this.parent.CanCast && (this.parent.pawn.CurJob == null || (this.parent.pawn.CurJob.ability == null && !(this.parent.pawn.CurJob.verbToUse is VEF.Abilities.Verb_CastAbility))) && this.AdditionalQualifiers())
            {
                this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(this.parent.pawn), null), JobCondition.InterruptForced);
            }
        }
        public virtual bool AdditionalQualifiers()
        {
            return true;
        }
    }
    public class CompProperties_AbilityAiScansForTargets : CompProperties_AbilityEffect
    {
        public int periodicity = 250;
        public bool scanForPawnsOnly = false;
        public bool onlyHostiles = false;
        public bool usableInMentalStates = false;
    }
    public class CompAbilityEffect_AiScansForTargets : CompAbilityEffect
    {
        public new CompProperties_AbilityAiScansForTargets Props
        {
            get
            {
                return (CompProperties_AbilityAiScansForTargets)this.props;
            }
        }
        public virtual float Range
        {
            get
            {
                return this.parent.verb.verbProps.range * (this.parent.verb.verbProps.rangeStat != null ? this.parent.pawn.GetStatValue(this.parent.verb.verbProps.rangeStat) : 1f);
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.pawn.IsHashIntervalTick(this.Props.periodicity) && this.parent.pawn.Spawned && !this.parent.pawn.IsColonistPlayerControlled && !this.parent.GizmoDisabled(out string text) && this.parent.def.aiCanUse && this.parent.CanCast && (this.Props.usableInMentalStates || !this.parent.pawn.InMentalState) && (this.parent.pawn.CurJob == null || (this.parent.pawn.CurJob.ability == null && (this.parent.pawn.CurJob.verbToUse == null || !(this.parent.pawn.CurJob.verbToUse is VEF.Abilities.Verb_CastAbility)))))
            {
                if (this.Props.scanForPawnsOnly)
                {
                    foreach (Pawn p in (List<Pawn>)this.parent.pawn.Map.mapPawns.AllPawnsSpawned)
                    {
                        if (p.Position.DistanceTo(this.parent.pawn.Position) <= this.Range)
                        {
                            if ((!this.Props.onlyHostiles || this.parent.pawn.HostileTo(p)) && this.VetPotentialTarget(p))
                            {
                                return;
                            }
                        }
                    }
                } else {
                    foreach (Thing thing in GenRadial.RadialDistinctThingsAround(this.parent.pawn.Position, this.parent.pawn.Map, this.Range, true))
                    {
                        if ((!this.Props.onlyHostiles || (thing.Faction != null && this.parent.pawn.HostileTo(thing.Faction))) && this.VetPotentialTarget(thing))
                        {
                            return;
                        }
                    }
                }
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (target.Thing != null)
            {
                return this.AdditionalQualifiers(target.Thing);
            }
            return base.AICanTargetNow(target);
        }
        protected bool VetPotentialTarget(Thing thing)
        {
            if (this.AdditionalQualifiers(thing))
            {
                this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(thing), null), JobCondition.InterruptForced);
                return true;
            }
            return false;
        }
        public virtual bool AdditionalQualifiers(Thing thing)
        {
            return true;
        }
    }
    public class CompProperties_AbilityAiUsesToRetreat : CompProperties_AbilityEffect
    {
        public float hpThreshold;
        public bool mustBeRanged;
        public int maxTicksSinceDamage = 120;
        public float minDangerRange = 2;
        public int minDangerCount = 1;
    }
    public class CompAbilityEffect_AiUsesToRetreat : CompAbilityEffect
    {
        public new CompProperties_AbilityAiUsesToRetreat Props
        {
            get
            {
                return (CompProperties_AbilityAiUsesToRetreat)this.props;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = this.parent.pawn;
            if (pawn.IsHashIntervalTick(250) && pawn.Spawned && !this.parent.pawn.IsColonistPlayerControlled && !this.parent.GizmoDisabled(out string text) && (!this.Props.mustBeRanged || (this.parent.pawn.equipment.Primary != null && this.parent.pawn.equipment.Primary.def.IsRangedWeapon)) && this.parent.def.aiCanUse && !this.parent.pawn.InAggroMentalState && this.parent.CanCast && !this.parent.Casting && this.parent.pawn.mindState.lastHarmTick > 0 && Find.TickManager.TicksGame < this.parent.pawn.mindState.lastHarmTick + this.Props.maxTicksSinceDamage && this.HpCheck)
            {
                if (PawnUtility.EnemiesAreNearby(this.parent.pawn, 9, true, this.Props.minDangerRange, this.Props.minDangerCount))
                {
                    if (this.EscapePosition(out IntVec3 c, this.Range))
                    {
                        this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(c), null), JobCondition.InterruptForced);
                    }
                }
            }
        }
        private float Range
        {
            get
            {
                float result = this.parent.verb.EffectiveRange;
                if (ModsConfig.RoyaltyActive && this.parent.def.category == DefDatabase<AbilityCategoryDef>.GetNamed("Skip"))
                {
                    result *= this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor);
                }
                return result;
            }
        }
        private bool HpCheck
        {
            get {
                return HautsUtility.MissingHitPointPercentageFor(this.parent.pawn) > this.Props.hpThreshold;
            }
        }
        private bool EscapePosition(out IntVec3 relocatePosition, float maxDistance)
        {
            this.tmpHostiles.Clear();
            this.tmpHostiles.AddRange(from a in this.parent.pawn.Map.attackTargetsCache.GetPotentialTargetsFor(this.parent.pawn) where !a.ThreatDisabled(this.parent.pawn) select a.Thing);
            relocatePosition = CellFinderLoose.GetFallbackDest(this.parent.pawn, this.tmpHostiles, maxDistance, 5f, 5f, 20, (IntVec3 c) => this.parent.verb.ValidateTarget(c, false));
            this.tmpHostiles.Clear();
            return relocatePosition.IsValid;
        }
        private List<Thing> tmpHostiles = new List<Thing>();
    }
    public class CompAbilityEffect_AvoidTargetingStunnedPawns : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect Props
        {
            get
            {
                return (CompProperties_AbilityEffect)this.props;
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return target.Pawn == null || !target.Pawn.stances.stunner.Stunned;
        }
    }
    public class CompProperties_AbilityCooldownStatScaling : CompProperties_AbilityEffect
    {
        public StatDef stat;
    }
    public class CompAbilityEffect_CooldownStatScaling : CompAbilityEffect
    {
        public new CompProperties_AbilityCooldownStatScaling Props
        {
            get
            {
                return (CompProperties_AbilityCooldownStatScaling)this.props;
            }
        }
    }
    public class CompProperties_AbilityForcedByOtherProperty : CompProperties_AbilityEffect
    {
        public List<TraitDef> forcingTraits;
        public List<GeneDef> forcingGenes;
        public bool requiresAForcingProperty = true;
    }
    public class CompAbilityEffect_ForcedByOtherProperty : CompAbilityEffect
    {
        public new CompProperties_AbilityForcedByOtherProperty Props
        {
            get
            {
                return (CompProperties_AbilityForcedByOtherProperty)this.props;
            }
        }
    }
    public class CompProperties_AbilityGiveHediffCasterStatScalingSeverity : CompProperties_AbilityGiveHediff
    {
        public StatDef casterStatToScaleFrom;
        public bool replacesLessSevereHediff;
        public bool refreshesMoreSevereHediff;
    }
    public class CompAbilityEffect_GiveHediffCasterStatScalingSeverity : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilityGiveHediffCasterStatScalingSeverity Props
        {
            get
            {
                return (CompProperties_AbilityGiveHediffCasterStatScalingSeverity)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (this.Props.ignoreSelf && target.Pawn == this.parent.pawn)
            {
                return;
            }
            if (!this.Props.onlyApplyToSelf && this.Props.applyToTarget)
            {
                this.ApplyInner(target.Pawn, this.parent.pawn);
            }
            if (this.Props.applyToSelf || this.Props.onlyApplyToSelf)
            {
                this.ApplyInner(this.parent.pawn, target.Pawn);
            }
        }
        protected void ApplyInner(Pawn target, Pawn other)
        {
            if (target != null)
            {
                if (this.TryResist(target))
                {
                    MoteMaker.ThrowText(target.DrawPos, target.Map, "Resisted".Translate(), -1f);
                    return;
                }
                float severity = this.parent.pawn.GetStatValue(this.Props.casterStatToScaleFrom) * (this.Props.severity >= 0f ? this.Props.severity: 1f);
                Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef, false);
                if (firstHediffOfDef != null)
                {
                    if (this.Props.replaceExisting || (this.Props.replacesLessSevereHediff && firstHediffOfDef.Severity < severity))
                    {
                        this.ReplaceExistingHediff(target,firstHediffOfDef);
                    } else if (this.Props.refreshesMoreSevereHediff) {
                        this.RefreshMoreSevereHediff(target,firstHediffOfDef);
                        return;
                    } else {
                        this.DontReplaceDontRefresh(target);
                        return;
                    }
                }
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffDef, target, this.Props.onlyBrain ? target.health.hediffSet.GetBrain() : null);
                HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
                if (hediffComp_Disappears != null)
                {
                    hediffComp_Disappears.ticksToDisappear = base.GetDurationSeconds(target).SecondsToTicks();
                }
                hediff.Severity = severity;
                HediffComp_Link hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
                if (hediffComp_Link != null)
                {
                    hediffComp_Link.other = other;
                    hediffComp_Link.drawConnection = (target == this.parent.pawn);
                }
                HediffComp_MultiLink hcml = hediff.TryGetComp<HediffComp_MultiLink>();
                if (hcml != null)
                {
                    if (hcml.others == null)
                    {
                        hcml.others = new List<Thing>();
                    }
                    hcml.others.Add(other);
                    if (hcml.motes == null)
                    {
                        hcml.motes = new List<MoteDualAttached>();
                    }
                    hcml.motes.Add(null);
                    hcml.drawConnection = true;
                }
                this.ModifyCreatedHediff(target,hediff);
                target.health.AddHediff(hediff, null, null, null);
            }
        }
        protected virtual void ReplaceExistingHediff(Pawn target, Hediff firstHediffOfDef)
        {
            target.health.RemoveHediff(firstHediffOfDef);
        }
        protected virtual void RefreshMoreSevereHediff(Pawn target, Hediff firstHediffOfDef)
        {
            HediffComp_Disappears hcd = firstHediffOfDef.TryGetComp<HediffComp_Disappears>();
            if (hcd != null)
            {
                hcd.ticksToDisappear = base.GetDurationSeconds(target).SecondsToTicks();
            }
        }
        protected virtual void DontReplaceDontRefresh(Pawn target)
        {

        }
        protected virtual void ModifyCreatedHediff(Pawn target, Hediff h)
        {

        }
        protected virtual bool TryResist(Pawn pawn)
        {
            return false;
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return this.parent.pawn.Faction != Faction.OfPlayer && target.Pawn != null;
        }
    }
    public class CompProperties_AbilityGiveHediffPaired : CompProperties_EffectWithDest
    {
        public CompProperties_AbilityGiveHediffPaired()
        {
            this.compClass = typeof(CompAbilityEffect_GiveHediffPaired);
        }
        public bool applyToTarget = true;
        public HediffDef hediffToTarg;
        public BodyPartTagDef bodyPartTarg;
        public bool replaceExistingTarg;
        public float severityTarg = -1f;
        public bool overridePreviousSeverityTarg;
        public bool overridePreviousDurationTarg;
        public bool overridePreviousLinkTargetTarg;
        public bool linkHediffTarg = true;
        public bool canStackTarg;
        //
        public bool applyToDest;
        public HediffDef hediffToDest;
        public BodyPartTagDef bodyPartDest;
        public bool replaceExistingDest;
        public float severityDest = -1f;
        public bool overridePreviousSeverityDest;
        public bool overridePreviousDurationDest;
        public bool overridePreviousLinkTargetDest;
        public bool linkHediffDest = true;
        public bool canStackDest;
        //
        public StatDef durationMultiplier;
        public FloatRange durationSecondsOverride = FloatRange.Zero;
    }
    public class CompAbilityEffect_GiveHediffPaired : CompAbilityEffect_WithDest
    {
        public new CompProperties_AbilityGiveHediffPaired Props
        {
            get
            {
                return (CompProperties_AbilityGiveHediffPaired)this.props;
            }
        }
        public override bool HideTargetPawnTooltip
        {
            get
            {
                return true;
            }
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!this.Props.replaceExistingTarg && !this.Props.canStackTarg && target.Thing != null && target.Thing is Pawn p)
            {
                if (p.health.hediffSet.HasHediff(this.Props.hediffToTarg))
                {
                    if (throwMessages)
                    {
                        Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "Hauts_TargetAlreadyHasPairedHediff".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                }
            }
            return base.Valid(target, throwMessages);
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!this.Props.replaceExistingDest && !this.Props.canStackDest && target.Thing != null && target.Thing is Pawn p)
            {
                if (p.health.hediffSet.HasHediff(this.Props.hediffToDest))
                {
                    if (showMessages)
                    {
                        Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "Hauts_TargetAlreadyHasPairedHediff".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                }
            }
            return base.ValidateTarget(target, showMessages);
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (this.Props.destination == AbilityEffectDestination.Caster)
            {
                dest = new LocalTargetInfo(this.parent.pawn);
            }
            if (this.Props.applyToTarget && target.Thing != null && target.Thing is Pawn)
            {
                if (dest.Thing != null)
                {
                    if (this.Props.applyToDest && dest.Thing is Pawn)
                    {
                        this.ApplyTwoPawns(target.Pawn, dest.Pawn);
                    } else {
                        this.ApplyPawnToThing(target.Pawn, dest.Thing, this.Props.hediffToTarg, this.Props.bodyPartTarg, this.Props.severityTarg,this.Props.replaceExistingTarg,this.Props.overridePreviousSeverityTarg,this.Props.overridePreviousDurationTarg,this.Props.overridePreviousLinkTargetTarg,this.Props.linkHediffTarg);
                    }
                }
            } else if (this.Props.applyToDest && dest.Thing != null && dest.Thing is Pawn) {
                this.ApplyPawnToThing(dest.Pawn, target.Thing, this.Props.hediffToDest, this.Props.bodyPartDest, this.Props.severityDest, this.Props.replaceExistingDest,this.Props.overridePreviousSeverityDest, this.Props.overridePreviousDurationDest, this.Props.overridePreviousLinkTargetDest, this.Props.linkHediffDest);
            }
        }
        public float GetDurationSeconds(Pawn target)
        {
            if (this.Props.durationSecondsOverride != FloatRange.Zero)
            {
                return this.Props.durationSecondsOverride.RandomInRange;
            }
            float num = this.parent.def.GetStatValueAbstract(StatDefOf.Ability_Duration, this.parent.pawn);
            if (this.Props.durationMultiplier != null)
            {
                num *= target.GetStatValue(this.Props.durationMultiplier, true, -1);
            }
            return num;
        }
        public BodyPartRecord GetPartToApplyTo(Pawn p, BodyPartTagDef bptd)
        {
            if (bptd != null)
            {
                foreach (BodyPartRecord bodyPartRecord in p.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null))
                {
                    if (bodyPartRecord.def.tags.Contains(bptd))
                    {
                        return bodyPartRecord;
                    }
                }
            }
            return null;
        }
        public virtual void ModifyHediffPreAdd(Hediff h, Pawn toApplyTo, Thing toAttachTo)
        {

        }
        public Hediff CreateHediffInner(Pawn toApplyTo, Thing toAttachTo, HediffDef hediffDef, BodyPartTagDef bptd, float severity, bool establishLink)
        {

            Hediff hediff = HediffMaker.MakeHediff(hediffDef, toApplyTo, this.GetPartToApplyTo(toApplyTo, bptd) ?? null);
            this.ModifyHediffPreAdd(hediff, toApplyTo, toAttachTo);
            if (severity > 0f)
            {
                hediff.Severity = severity;
            }
            HediffComp_Disappears hcd = hediff.TryGetComp<HediffComp_Disappears>();
            if (hcd != null)
            {
                hcd.ticksToDisappear = this.GetDurationSeconds(toApplyTo).SecondsToTicks();
            }
            if (establishLink)
            {
                HediffComp_Link hcl = hediff.TryGetComp<HediffComp_Link>();
                if (hcl != null)
                {
                    hcl.other = toAttachTo;
                    hcl.drawConnection = true;
                }
                HediffComp_MultiLink hcml = hediff.TryGetComp<HediffComp_MultiLink>();
                if (hcml != null)
                {
                    if (hcml.others == null)
                    {
                        hcml.others = new List<Thing>();
                    }
                    hcml.others.Add(toAttachTo);
                    if (hcml.motes == null)
                    {
                        hcml.motes = new List<MoteDualAttached>();
                    }
                    hcml.motes.Add(null);
                    hcml.drawConnection = true;
                }
            }
            toApplyTo.health.AddHediff(hediff, null, null, null);
            return hediff;
        }
        public Hediff CreateHediff(Pawn toApplyTo, Thing toAttachTo, HediffDef hediffDef, BodyPartTagDef bptd, float severity, bool replaceExisting, bool overridePreviousSeverity, bool overridePreviousDuration, bool overridePreviousLinkTarget, bool establishLink)
        {
            Hediff firstHediffOfDef = toApplyTo.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
            if (firstHediffOfDef != null)
            {
                if (replaceExisting)
                {
                    toApplyTo.health.RemoveHediff(firstHediffOfDef);
                    return this.CreateHediffInner(toApplyTo, toAttachTo, hediffDef, bptd, severity, establishLink);
                } else {
                    if (severity > 0f)
                    {
                        if (overridePreviousSeverity)
                        {
                            firstHediffOfDef.Severity = severity;
                        } else {
                            firstHediffOfDef.Severity += severity;
                        }
                    }
                    if (overridePreviousDuration)
                    {
                        HediffComp_Disappears hcd = firstHediffOfDef.TryGetComp<HediffComp_Disappears>();
                        if (hcd != null)
                        {
                            hcd.ticksToDisappear = this.GetDurationSeconds(toApplyTo).SecondsToTicks();
                        }
                    }
                    if (overridePreviousLinkTarget && establishLink)
                    {
                        HediffComp_Link hcl = firstHediffOfDef.TryGetComp<HediffComp_Link>();
                        if (hcl != null)
                        {
                            hcl.other = toAttachTo;
                            hcl.drawConnection = true;
                        }
                        HediffComp_MultiLink hcml = firstHediffOfDef.TryGetComp<HediffComp_MultiLink>();
                        if (hcml != null)
                        {
                            if (hcml.others == null)
                            {
                                hcml.others = new List<Thing>();
                            }
                            hcml.others.Add(toAttachTo);
                            if (hcml.motes == null)
                            {
                                hcml.motes = new List<MoteDualAttached>();
                            }
                            hcml.motes.Add(null);
                            hcml.drawConnection = true;
                        }
                    }
                    return firstHediffOfDef;
                }
            } else {
                return this.CreateHediffInner(toApplyTo, toAttachTo, hediffDef, bptd, severity, establishLink);
            }
        }
        protected void ApplyPawnToThing(Pawn toApplyTo, Thing toAttachTo, HediffDef hediff, BodyPartTagDef bptd, float severity, bool replaceExisting, bool overridePreviousSeverity, bool overridePreviousDuration, bool overridePreviousLinkTarget, bool linkToOther)
        {
            if (toAttachTo != null)
            {
                this.CreateHediff(toApplyTo, toAttachTo, hediff, bptd, severity, replaceExisting, overridePreviousSeverity, overridePreviousDuration, overridePreviousLinkTarget, linkToOther);
            }
        }
        protected void ApplyTwoPawns(Pawn target, Pawn dest)
        {
            if (target != null)
            {
                if (this.TryResist(target))
                {
                    MoteMaker.ThrowText(target.DrawPos, target.Map, "Resisted".Translate(), -1f);
                    return;
                }
                Hediff hediffToTarg = null;
                Hediff hediffToDest = null;
                if (this.Props.hediffToTarg != null)
                {
                    hediffToTarg = this.CreateHediff(target, dest, this.Props.hediffToTarg, this.Props.bodyPartTarg, this.Props.severityTarg, this.Props.replaceExistingTarg, this.Props.overridePreviousSeverityTarg, this.Props.overridePreviousDurationTarg, this.Props.overridePreviousLinkTargetTarg, this.Props.linkHediffTarg);
                }
                if (this.Props.hediffToDest != null)
                {
                    hediffToDest = this.CreateHediff(dest, target, this.Props.hediffToDest, this.Props.bodyPartDest, this.Props.severityDest, this.Props.replaceExistingDest, this.Props.overridePreviousSeverityDest, this.Props.overridePreviousDurationDest, this.Props.overridePreviousLinkTargetDest, this.Props.linkHediffDest);
                    if (hediffToTarg != null)
                    {
                        HediffComp_PairedHediff hcph = hediffToTarg.TryGetComp<HediffComp_PairedHediff>();
                        if (hcph != null)
                        {
                            hcph.hediffs.Add(hediffToDest);
                        }
                        HediffComp_PairedHediff hcph2 = hediffToDest.TryGetComp<HediffComp_PairedHediff>();
                        if (hcph2 != null)
                        {
                            hcph2.hediffs.Add(hediffToTarg);
                        }
                    }
                }
            }
        }
        protected virtual bool TryResist(Pawn pawn)
        {
            return false;
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return this.parent.pawn.Faction != Faction.OfPlayer && target.Pawn != null;
        }
    }
    public class CompProperties_AbilityGiveHediffFromMenu : CompProperties_AbilityEffectWithDuration
    {
        public List<HediffDef> hediffs;
        public bool onlyBrain;
        public bool applyToSelf;
        public bool onlyApplyToSelf;
        public bool applyToTarget = true;
        public float severity = -1f;
        public bool ignoreSelf;
        public bool autoSelectIfAI = true;
        public string menuString;
    }
    public class CompAbilityEffect_GiveHediffFromMenu : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilityGiveHediffFromMenu Props
        {
            get
            {
                return (CompProperties_AbilityGiveHediffFromMenu)this.props;
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return false;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (this.Props.ignoreSelf && target.Pawn == this.parent.pawn)
            {
                return;
            }
            if (!this.Props.onlyApplyToSelf && this.Props.applyToTarget)
            {
                this.ApplyInner(target.Pawn, this.parent.pawn);
            }
            if (this.Props.applyToSelf || this.Props.onlyApplyToSelf)
            {
                this.ApplyInner(this.parent.pawn, target.Pawn);
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.Props.autoSelectIfAI && this.parent.pawn.Faction != Faction.OfPlayer)
            {
                this.Apply(this.parent.pawn, this.parent.pawn);
            }
        }
        protected void ApplyInner(Pawn target, Pawn other)
        {
            if (target != null)
            {
                if (this.parent.pawn != null && this.parent.pawn.Faction == Faction.OfPlayer)
                {
                    Find.WindowStack.Add(new Dialog_GiveHediffFromMenu(this, target, other, this.parent.pawn, this.Props.hediffs, this.Props.menuString));
                } else {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffs.RandomElement<HediffDef>(), target, null);
                    target.health.AddHediff(hediff, this.Props.onlyBrain ? target.health.hediffSet.GetBrain() : null, null, null);
                    HautsUtility.AddHediffFromMenu(this.Props.hediffs.RandomElement<HediffDef>(), this.parent.pawn, this, this.parent.pawn, this.parent.pawn);
                    CompAbilityEffect_RemoveHediff caerh = this.parent.CompOfType<CompAbilityEffect_RemoveHediff>();
                    if (caerh != null)
                    {
                        Hediff h = this.parent.pawn.health.hediffSet.GetFirstHediffOfDef(caerh.Props.hediffDef);
                        if (h != null)
                        {
                            this.parent.pawn.health.RemoveHediff(h);
                        }
                    }
                }
            }
        }
    }
    public class Dialog_GiveHediffFromMenu : Window
    {
        public Dialog_GiveHediffFromMenu(CompAbilityEffect_GiveHediffFromMenu ability, Pawn pawn, Pawn other, Pawn caster, List<HediffDef> hediffs, string menuLabel)
        {
            this.pawn = pawn;
            this.other = other;
            this.caster = caster;
            this.ability = ability;
            this.forcePause = true;
            this.doCloseButton = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.closeOnAccept = false;
            this.closeOnCancel = true;
            this.optionalTitle = menuLabel.Translate(this.pawn.Name.ToStringShort);
            this.possibleHediffs = hediffs;
        }
        private float Height
        {
            get
            {
                return (CharacterCardUtility.PawnCardSize(this.pawn).y + Window.CloseButSize.y + 4f + this.Margin * 2f) / 1.25f;
            }
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, this.Height);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= 4f + Window.CloseButSize.y;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width * 0.7f, this.scrollHeight);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            num += 14f;
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            if (pawn.story != null)
            {
                foreach (HediffDef h in this.possibleHediffs)
                {
                    bool flag = this.chosenHediff == h;
                    bool flag2 = flag;
                    listing_Standard.CheckboxLabeled(h.LabelCap, ref flag, 15f);
                    if (flag != flag2)
                    {
                        if (flag)
                        {
                            this.chosenHediff = h;
                        }
                    }
                }
            }
            listing_Standard.End();
            num += listing_Standard.CurHeight + 10f + 4f;
            if (Event.current.type == EventType.Layout)
            {
                this.scrollHeight = Mathf.Max(num, inRect.height);
            }
            Widgets.EndScrollView();
            Rect rect2 = new Rect(0f, inRect.yMax + 4f, inRect.width, Window.CloseButSize.y);
            AcceptanceReport acceptanceReport = this.CanClose();
            if (!acceptanceReport.Accepted)
            {
                TextAnchor anchor = Text.Anchor;
                GameFont font = Text.Font;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                Rect rect3 = rect;
                rect3.xMax = rect2.xMin - 4f;
                Widgets.Label(rect3, acceptanceReport.Reason.Colorize(ColoredText.WarningColor));
                Text.Font = font;
                Text.Anchor = anchor;
            }
            if (Widgets.ButtonText(rect2, "OK".Translate(), true, true, true, null))
            {
                if (acceptanceReport.Accepted)
                {
                    HautsUtility.AddHediffFromMenu(this.chosenHediff, this.pawn, this.ability, this.other, this.caster);
                    this.Close(true);
                } else {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            if (this.chosenHediff == null)
            {
                return "Hauts_HediffMenuException".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }
        private readonly List<HediffDef> possibleHediffs;
        private CompAbilityEffect_GiveHediffFromMenu ability;
        private HediffDef chosenHediff;
        private float scrollHeight;
        private Pawn pawn;
        private Pawn other;
        private Pawn caster;
        private Vector2 scrollPosition;
    }
    public class CompProperties_AbilityGivesThought : CompProperties_AbilityEffect
    {
        public IntRange periodicity;
        public ThoughtDef thought;
        public bool duringCooldown = true;
        public bool whileReady = true;
        public bool clearsThisThoughtOnActivation = true;
    }
    public class CompAbilityEffect_GivesThought : CompAbilityEffect
    {
        public new CompProperties_AbilityGivesThought Props
        {
            get
            {
                return (CompProperties_AbilityGivesThought)this.props;
            }
        }
        public override void Apply(GlobalTargetInfo target)
        {
            this.ClearThoughtOnActivation();
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            this.ClearThoughtOnActivation();
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.nextPeriod <= 0 && this.parent.pawn != null && !this.parent.pawn.Dead && this.parent.pawn.needs.mood != null && ((this.Props.duringCooldown && this.parent.OnCooldown) || (this.Props.whileReady && !this.parent.OnCooldown)))
            {
                this.parent.pawn.needs.mood.thoughts.memories.TryGainMemory(this.Props.thought);
                this.nextPeriod = this.Props.periodicity.RandomInRange;
            }
            if (this.nextPeriod > 0)
            {
                this.nextPeriod --;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.nextPeriod, "nextPeriod", 0);
        }
        public void ClearThoughtOnActivation()
        {
            if (this.Props.clearsThisThoughtOnActivation && this.parent.pawn != null && this.parent.pawn.needs.mood != null)
            {
                this.parent.pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(this.Props.thought);
            }
        }
        private int nextPeriod = 0;
    }
    public class CompProperties_AbilityMeditationCooldown : CompProperties_AbilityEffect
    {
        public bool stopsWhileNotMeditating;
        public int bonusTicksWhileMeditating;
    }
    public class CompAbilityEffect_MeditationCooldown : CompAbilityEffect
    {
        public new CompProperties_AbilityMeditationCooldown Props
        {
            get
            {
                return (CompProperties_AbilityMeditationCooldown)this.props;
            }
        }
        public override void Initialize(AbilityCompProperties props)
        {
            base.Initialize(props);
            HautsUtility.CheckIfAbilityHasRequiredPsylink(this.parent.pawn, this.parent);
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.CooldownTicksRemaining > 0 && this.parent.pawn.psychicEntropy != null)
            {
                if (!this.parent.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    if (this.Props.stopsWhileNotMeditating)
                    {
                        HautsUtility.SetNewCooldown(this.parent, this.parent.CooldownTicksRemaining + 1);
                    }
                } else {
                    HautsUtility.SetNewCooldown(this.parent, this.parent.CooldownTicksRemaining - this.Props.bonusTicksWhileMeditating);
                }
            }
        }
    }
    public class CompProperties_AbilityNova : CompProperties_AbilityAiAppliesToSelf
    {
        public PawnCapacityDef radiusScalarCapacity = null;
        public StatDef radiusScalarStat = null;
        public float maxRadius;
        public float baseRadius;
        public int aiMinEnemyPawns = 1;
        public bool aiDislikesFriendlyFire = true;
    }
    public class CompAbilityEffect_Nova : CompAbilityEffect_AiAppliesToSelf
    {
        public new CompProperties_AbilityNova Props
        {
            get
            {
                return (CompProperties_AbilityNova)this.props;
            }
        }
        public virtual float Radius
        {
            get
            {
                float radius = this.Props.baseRadius;
                if (this.Props.radiusScalarCapacity != null)
                {
                    radius *= this.parent.pawn.health.capacities.GetLevel(this.Props.radiusScalarCapacity);
                }
                if (this.Props.radiusScalarStat != null)
                {
                    radius *= this.parent.pawn.GetStatValue(this.Props.radiusScalarStat);
                }
                return Math.Min(this.Props.maxRadius, radius);
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (this.parent.pawn.Spawned)
            {
                List<Pawn> pawns = (List<Pawn>)this.parent.pawn.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i] == this.parent.pawn)
                    {
                        this.AffectSelf();
                    } else if (pawns[i].Position.DistanceTo(this.parent.pawn.Position) <= this.Radius) {
                        this.AffectPawn(pawns[i]);
                    }
                }
            }
        }
        public override bool AdditionalQualifiers()
        {
            return this.VictimCounter();
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return this.VictimCounter();
        }
        public virtual bool VictimCounter()
        {
            int victimCounter = 0;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(this.parent.pawn.Position, this.parent.pawn.Map, this.Radius, true))
            {
                if (thing is Pawn p)
                {
                    victimCounter += this.VictimValue(p);
                }
            }
            return this.parent.pawn.Spawned && victimCounter >= this.Props.aiMinEnemyPawns;
        }
        public virtual int VictimValue(Pawn p)
        {
            if (p != this.parent.pawn && p.Map.attackTargetsCache.GetPotentialTargetsFor(this.parent.pawn).Contains(p))
            {
                if (p.HostileTo(this.parent.pawn) && !p.ThreatDisabled(this.parent.pawn))
                {
                    return 1;
                }
                if (this.Props.aiDislikesFriendlyFire && p.Faction != null)
                {
                    return -1;
                }
            }
            return 0;
        }
        public override void OnGizmoUpdate()
        {
            base.OnGizmoUpdate();
            GenDraw.DrawRadiusRing(this.parent.pawn.Position, this.Radius);
        }
        public virtual void AffectSelf()
        {

        }
        public virtual void AffectPawn(Pawn pawn)
        {

        }
    }
    public class CompProperties_AbilityOffsetNeeds : CompProperties_AbilityEffect
    {
        public Dictionary<NeedDef, float> needsToAffect = new Dictionary<NeedDef, float>();
        public bool targetMustHaveAffectedNeeds;
    }
    public class CompAbilityEffect_OffsetNeeds : CompAbilityEffect
    {
        public new CompProperties_AbilityOffsetNeeds Props
        {
            get
            {
                return (CompProperties_AbilityOffsetNeeds)this.props;
            }
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (this.Props.targetMustHaveAffectedNeeds && target.Thing != null && target.Thing is Pawn p)
            {
                if (p.needs == null)
                {
                    if (throwMessages)
                    {
                        Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "Hauts_LacksAnyAffectedNeed".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                } else {
                    bool hasAnyAffectedNeed = false;
                    foreach (NeedDef n in this.Props.needsToAffect.Keys)
                    {
                        if (p.needs.TryGetNeed(n) != null)
                        {
                            hasAnyAffectedNeed = true;
                            break;
                        }
                    }
                    if (!hasAnyAffectedNeed)
                    {
                        if (throwMessages)
                        {
                            Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "Hauts_LacksAnyAffectedNeed".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                        }
                        return false;
                    }
                }
            }
            return base.Valid(target, throwMessages);
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target.Thing != null && target.Thing is Pawn p)
            {
                foreach (NeedDef n in this.Props.needsToAffect.Keys)
                {
                    if (p.needs.TryGetNeed(n) != null)
                    {
                        p.needs.TryGetNeed(n).CurLevel += this.Props.needsToAffect.TryGetValue(n);
                    }
                }
            }
        }
    }
    public class CompProperties_AbilityRequiresMinimumStat : CompProperties_AbilityEffect
    {
        public Dictionary<StatDef, float> minStats = new Dictionary<StatDef, float>();
        public bool hideIfDisabled;
    }
    public class CompAbilityEffect_RequiresMinimumStat : CompAbilityEffect
    {
        public new CompProperties_AbilityRequiresMinimumStat Props
        {
            get
            {
                return (CompProperties_AbilityRequiresMinimumStat)this.props;
            }
        }
        public override bool ShouldHideGizmo
        {
            get
            {
                if (this.Props.hideIfDisabled)
                {
                    foreach (StatDef stat in this.Props.minStats.Keys)
                    {
                        if (this.parent.pawn.GetStatValue(stat) < this.Props.minStats.TryGetValue(stat))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        public override bool CanCast
        {
            get
            {
                return !this.ShouldHideGizmo;
            }
        }
        public override bool GizmoDisabled(out string reason)
        {
            foreach (StatDef stat in this.Props.minStats.Keys)
            {
                if (this.parent.pawn.GetStatValue(stat) < this.Props.minStats.TryGetValue(stat))
                {
                    reason = "Hauts_AbilityNeedsHigherStatToUse".Translate(this.Props.minStats.TryGetValue(stat), stat.label);
                    return true;
                }
            }
            return base.GizmoDisabled(out reason);
        }
    }
    public class CompProperties_AbilityTargetMarketValueSetsCooldown : CompProperties_AbilityEffect
    {
        public float minMarketValueToScale;
        public float marketValueScalar = 10f;
    }
    public class CompAbilityEffect_TargetMarketValueSetsCooldown : CompAbilityEffect
    {
        public new CompProperties_AbilityTargetMarketValueSetsCooldown Props
        {
            get
            {
                return (CompProperties_AbilityTargetMarketValueSetsCooldown)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            float effectiveMarketValue = Math.Max(this.Props.marketValueScalar * (target.Thing.MarketValue - this.Props.minMarketValueToScale), 0f);
            int modCD = Math.Min((int)(this.parent.def.cooldownTicksRange.min + effectiveMarketValue),this.parent.def.cooldownTicksRange.max);
            HautsUtility.SetNewCooldown(this.parent,modCD);
        }
    }
    public class CompProperties_AbilityTargetPsylinkLevelSetsCooldown : CompProperties_AbilityEffect
    {
        public float addedPerLevel = 0;
        public float baseForExponent = 1;//this value is raised to the psylink level power
        public int capsOutAtLevel = 6;
        public bool minCooldownIfNoPsylink = true;
    }
    public class CompAbilityEffect_TargetPsylinkLevelSetsCooldown : CompAbilityEffect
    {
        public new CompProperties_AbilityTargetPsylinkLevelSetsCooldown Props
        {
            get
            {
                return (CompProperties_AbilityTargetPsylinkLevelSetsCooldown)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target.Thing is Pawn pawn)
            {
                int psylinkLevel = Math.Min(pawn.GetPsylinkLevel(), this.Props.capsOutAtLevel);
                if (psylinkLevel >0 || this.Props.minCooldownIfNoPsylink)
                {
                    int modCD = (int)Math.Min(Math.Ceiling((this.parent.def.cooldownTicksRange.min + (this.Props.addedPerLevel * psylinkLevel)) * Math.Pow(this.Props.baseForExponent, psylinkLevel - 1)), this.parent.def.cooldownTicksRange.max);
                    this.parent.StartCooldown(modCD);
                    if (this.parent.def.groupDef != null)
                    {
                        foreach (RimWorld.Ability ability in this.parent.pawn.abilities.AllAbilitiesForReading)
                        {
                            if (ability.def.groupDef != null && ability.def.groupDef == this.parent.def.groupDef && ability != this.parent)
                            {
                                ability.StartCooldown(modCD);
                            }
                        }
                    }
                }
            }
        }
    }
    public class CompProperties_AbilityTeleportSelf : CompProperties_AbilityEffect
    {
        public bool requiresLineOfSight;
        public IntRange stunTicks;
        public ClamorDef destClamorType;
        public int destClamorRadius;
    }
    public class CompAbilityEffect_TeleportSelf : CompAbilityEffect
    {
        public new CompProperties_AbilityTeleportSelf Props
        {
            get
            {
                return (CompProperties_AbilityTeleportSelf)this.props;
            }
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(this.parent.pawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                        dataAttachedOverlay.link.detachAfterTicks = 5;
                        this.parent.pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                        FleckMaker.Static(t.Cell, this.parent.pawn.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    }
                    if (!this.parent.def.HasAreaOfEffect)
                    {
                        SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(this.parent.pawn.Position, this.parent.pawn.Map, false));
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(t.Cell, this.parent.pawn.Map, false));
                    }
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target.IsValid && this.Valid(target))
            {
                Pawn pawn = this.parent.pawn;
                if (!this.parent.def.HasAreaOfEffect)
                {
                    this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, null);
                } else {
                    this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_EntryNoDelay.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, null);
                }
                this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Exit.Spawn(target.Cell, pawn.Map, 1f), target.Cell, 60, null);
                CompCanBeDormant compCanBeDormant = pawn.TryGetComp<CompCanBeDormant>();
                if (compCanBeDormant != null)
                {
                    compCanBeDormant.WakeUp();
                }
                pawn.Position = target.Cell;
                if ((pawn.Faction == Faction.OfPlayer || pawn.IsPlayerControlled) && pawn.Position.Fogged(pawn.Map))
                {
                    FloodFillerFog.FloodUnfog(pawn.Position, pawn.Map);
                }
                pawn.stances.stunner.StunFor(this.Props.stunTicks.RandomInRange, this.parent.pawn, false, false);
                pawn.Notify_Teleported(true, true);
                CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn.Position, pawn);
                if (this.Props.destClamorType != null)
                {
                    GenClamor.DoClamor(pawn, target.Cell, (float)this.Props.destClamorRadius, this.Props.destClamorType);
                }
            }
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return this.parent.pawn.Spawned && !this.parent.pawn.kindDef.skipResistant && target.IsValid && (this.parent.verb.verbProps.range <= 0f || target.Cell.DistanceTo(target.Cell) <= this.parent.verb.verbProps.range) && (!this.Props.requiresLineOfSight || GenSight.LineOfSight(target.Cell, this.parent.pawn.Position, this.parent.pawn.Map, false, null, 0, 0)) && this.CanPlaceSelectedTargetAt(target);
        }
        public bool CanPlaceSelectedTargetAt(LocalTargetInfo target)
        {
            Pawn pawn = this.parent.pawn;
            Building_Door door = target.Cell.GetDoor(pawn.Map);
            return (door == null || door.CanPhysicallyPass(pawn)) && (pawn.Spawned && !target.Cell.Impassable(this.parent.pawn.Map)) && target.Cell.WalkableBy(this.parent.pawn.Map, pawn);
        }
    }
    //ability gizmos
    public class Command_AbilityCanBuffSelfOnCaravan : RimWorld.Command_Ability
    {
        public Command_AbilityCanBuffSelfOnCaravan(RimWorld.Ability ability, Pawn pawn) : base(ability, pawn)
        {
        }
        public override void ProcessInput(Event ev)
        {
            if (this.Pawn.IsCaravanMember())
            {
                this.Ability.Activate(this.Pawn);
                foreach (CompAbilityEffect compAbilityEffect in this.Ability.EffectComps)
                {
                    if (!(compAbilityEffect is CompAbilityEffect_FleckOnTarget))
                    {
                        compAbilityEffect.Apply(this.Pawn, null);
                    }
                }
            }
            base.ProcessInput(ev);
        }
    }
    public class Psycast_CanBuffSelfOnCaravan : RimWorld.Psycast
    {
        public Psycast_CanBuffSelfOnCaravan(Pawn pawn)
            : base(pawn)
        {
        }
        public Psycast_CanBuffSelfOnCaravan(Pawn pawn, RimWorld.AbilityDef def)
            : base(pawn, def)
        {
        }
        public override IEnumerable<Command> GetGizmos()
        {
            if (!ModLister.RoyaltyInstalled)
            {
                yield break;
            }
            if (this.gizmo == null)
            {
                this.gizmo = new Command_PsycastCanBuffSelfOnCaravan(this, this.pawn);
            }
            yield return this.gizmo;
            yield break;
        }
    }
    public class Command_PsycastCanBuffSelfOnCaravan : Command_Psycast
    {
        public Command_PsycastCanBuffSelfOnCaravan(Psycast ability, Pawn pawn) : base(ability, pawn)
        {
        }
        public override void ProcessInput(Event ev)
        {
            if (this.Pawn.IsCaravanMember())
            {
                this.Ability.Activate(this.Pawn);
                foreach (CompAbilityEffect compAbilityEffect in this.Ability.EffectComps)
                {
                    if (!(compAbilityEffect is CompAbilityEffect_FleckOnTarget))
                    {
                        compAbilityEffect.Apply(this.Pawn, null);
                    }
                }
            }
            base.ProcessInput(ev);
        }
    }
    //permits
    //permit mechanics: DMEs
    public class PermitMoreEffects : DefModExtension
    {
        public PermitMoreEffects() { }
        public float GetIncidentPoints(Pawn caller)
        {
            if (this.defaultIncidentPointFactor != null)
            {
                float factor = this.defaultIncidentPointFactor.RandomInRange;
                if (caller.Map != null && caller.Map.IsPlayerHome)
                {
                    return factor * StorytellerUtility.DefaultThreatPointsNow(caller.Map);
                } else if (Find.AnyPlayerHomeMap != null) {
                    return factor * StorytellerUtility.DefaultThreatPointsNow(Find.RandomPlayerHomeMap);
                }
            }
            if (this.incidentPoints != null)
            {
                return this.incidentPoints.RandomInRange;
            }
            if (caller.Map != null && caller.Map.IsPlayerHome)
            {
                return StorytellerUtility.DefaultThreatPointsNow(caller.Map);
            } else if (Find.AnyPlayerHomeMap != null) {
                return StorytellerUtility.DefaultThreatPointsNow(Find.RandomPlayerHomeMap);
            }
            return 100f;
        }
        //giving hediffs
        public List<HediffDef> hediffs;
        //spawning stuff
        public int phenomenonCount;
        //making books
        public ThingDef bookDef;
        public RulePackDef bookTitlePack;
        public RulePackDef bookDescPack;
        public long bookFixedPubDate = -1;
        public float bookSuperQualityChance;
        //making things from sets
        public List<ThingCategoryDef> thingCategories;
        public List<ThingCategoryDef> forbiddenThingCategories;
        public List<string> tradeTags;
        public List<string> forbiddenTradeTags;
        public TechLevel minTechLevelInCategory = TechLevel.Undefined;
        public TechLevel maxTechLevelInCategory = TechLevel.Archotech;
        public IntRange marketValueLimits = new IntRange(0,9999999);
        public IntRange numFromCategory;
        public bool allRandomOutcomesMustBeSamePerUse;
        //making quests
        public List<QuestScriptDef> questScriptDefs;
        public List<IncidentDef> incidentDefs;
        public int questCount = 1;
        public IntRange incidentPoints;
        public FloatRange defaultIncidentPointFactor;
        public IntRange incidentDelay;
        public bool incidentUsesPermitFaction = true;
        //causing conditions
        public List<GameConditionDef> conditionDefs;
        public IntRange conditionDuration;
        //feedback
        public bool screenShake;
        public SoundDef soundDef;
        public string onUseMessage;
        //other effects
        public FloatRange extraNumber;
        //thing-targeting
        public List<ThingDef> targetableThings;
        public string invalidTargetMessage;
        //drop pawns
        public List<PawnKindDef> allowedPawnKinds;
        public bool allowMechs;
        public bool allowDrones;
        public bool allowDryads;
        public bool allowInsectoids;
        public bool allowEntities;
        public bool allowAnimals;
        public bool allowHumanlikes;
        public bool allowAnyFlesh;
        public bool allowAnyNonflesh;
        public bool needsPen;
        public bool mustBePredator;
        public float maxWildness = 0.6f;
        public float minPetness = -1f;
        public FloatRange bodySizeCapRange = new FloatRange(-1f,999f);
        public List<PawnKindDef> disallowedPawnKinds;
        public bool startsTamed;
        //drop stuff
        public FloatRange gambaFactorRange;
        public bool gambaDropPodSoNotInstant = false;
        public IntRange gambaReturnDelay;
        public string returnMessage;
        public string extraString;
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropBook : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallResources(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.BeginCallResources(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_DropBook.CommandTex,
                action = delegate
                {
                    Caravan caravan = pawn.GetCaravan();
                    float num = caravan.MassUsage;
                    PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
                    if (pme != null && pme.bookDef != null)
                    {
                        for (int i = 0; i < this.ItemStackCount(pme, caller); i++)
                        {
                            num += pme.bookDef.BaseMass;
                        }
                        if (num > caravan.MassCapacity)
                        {
                            WindowStack windowStack = Find.WindowStack;
                            TaggedString taggedString = "DropResourcesOverweightConfirm".Translate();
                            Action action = delegate
                            {
                                this.CallResourcesToCaravan(pawn, faction, this.free);
                            };
                            windowStack.Add(Dialog_MessageBox.CreateConfirmation(taggedString, action, true, null, WindowLayer.Dialog));
                            return;
                        }
                        this.CallResourcesToCaravan(pawn, faction, this.free);
                    }
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallResources(IntVec3 cell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.bookDef != null)
            {
                List<Thing> list = new List<Thing>();
                for (int i = 0; i < this.ItemStackCount(pme, this.caller); i++)
                {
                    Thing thing = this.MakeBook(pme);
                    list.Add(thing);
                }
                if (list.Any<Thing>())
                {
                    ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                    activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                    DropPodUtility.MakeDropPodAt(cell, this.map, activeTransporterInfo, null);
                    Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
                    this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                    if (!this.free)
                    {
                        this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                    }
                    this.DoOtherEffect(this.caller, this.faction);
                }
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        public virtual int ItemStackCount(PermitMoreEffects pme, Pawn caller)
        {
            return pme.phenomenonCount;
        }
        private void CallResourcesToCaravan(Pawn caller, Faction faction, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.bookDef != null)
            {
                Caravan caravan = caller.GetCaravan();
                for (int i = 0; i < this.ItemStackCount(pme,caller); i++)
                {
                    Thing thing = this.MakeBook(pme);
                    CaravanInventoryUtility.GiveThing(caravan, thing);
                }
                Messages.Message("MessagePermitTransportDropCaravan".Translate(faction.Named("FACTION"), caller.Named("PAWN")), caravan, MessageTypeDefOf.NeutralEvent, true);
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(caller, faction);
            }
        }
        private Book MakeBook(PermitMoreEffects pme)
        {
            CompProperties_Book cpb = pme.bookDef.GetCompProperties<CompProperties_Book>();
            if (cpb != null)
            {
                foreach (ReadingOutcomeProperties rop in cpb.doers)
                {
                    if (rop is BookOutcomeProperties_GainResearch bopgr)
                    {
                        List<ResearchTabDef> tabs = new List<ResearchTabDef>();
                        if (bopgr.tab != null)
                        {
                            tabs.Add(bopgr.tab);
                        }
                        if (bopgr.tabs != null)
                        {
                            foreach (BookOutcomeProperties_GainResearch.BookTabItem bti in bopgr.tabs)
                            {
                                if (!tabs.Contains(bti.tab))
                                {
                                    tabs.Add(bti.tab);
                                }
                            }
                        }
                        bool doNovel = true;
                        foreach (ResearchProjectDef rpd in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                        {
                            if (!rpd.IsFinished && tabs.Contains(rpd.tab) && rpd.techprintCount == 0 && (bopgr.exclude.Count == 0 || !bopgr.exclude.ContainsAny((BookOutcomeProperties_GainResearch.BookResearchItem i) => i.project == rpd)))
                            {
                                doNovel = false;
                                break;
                            }
                        }
                        if (doNovel)
                        {
                            return this.MakeBookInner(pme, false);
                        }
                        break;
                    }
                }
            }
            return this.MakeBookInner(pme, false);
        }
        public virtual void ExtraBookModification(Book book, PermitMoreEffects pme)
        {

        }
        public virtual void ExtraTitleGrammarRules(Book book, ref GrammarRequest grammarRequest)
        {

        }
        public virtual void ExtraDescGrammarRules(Book book, ref GrammarRequest grammarRequest)
        {

        }
        private Book MakeBookInner(PermitMoreEffects pme, bool doNovelInstead = false)
        {
            Book book = (Book)ThingMaker.MakeThing(doNovelInstead ? ThingDefOf.Novel : pme.bookDef);
            CompQuality compQuality = book.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory q = Rand.Chance(pme.bookSuperQualityChance) ? QualityUtility.GenerateQuality(QualityGenerator.Super) : QualityUtility.GenerateQuality(QualityGenerator.Trader);
                compQuality.SetQuality(q, null);
            }
            this.ExtraBookModification(book,pme);
            //custom naming schema
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, true);
            RoyalTitlePermitWorker_DropBook.subjects.Clear();
            GrammarRequest grammarRequest = default(GrammarRequest);
            long num = pme.bookFixedPubDate >= 0 ? pme.bookFixedPubDate : ((long)GenTicks.TicksAbs - (long)(book.BookComp.Props.ageYearsRange.RandomInRange * 3600000f));
            grammarRequest.Rules.Add(new Rule_String("date", GenDate.DateFullStringAt(num, Vector2.zero)));
            grammarRequest.Rules.Add(new Rule_String("date_season", GenDate.DateMonthYearStringAt(num, Vector2.zero)));
            if (compQuality != null)
            {
                grammarRequest.Constants.Add("quality", ((int)book.GetComp<CompQuality>().Quality).ToString());
            }
            foreach (Verse.Grammar.Rule rule in (TaleData_Pawn.GenerateRandom(true)).GetRules("ANYPAWN", grammarRequest.Constants))
            {
                grammarRequest.Rules.Add(rule);
            }
            foreach (BookOutcomeDoer bookOutcomeDoer in book.BookComp.Doers)
            {
                bookOutcomeDoer.Reset();
                bookOutcomeDoer.OnBookGenerated(null);
				IEnumerable<RulePack> topicRulePacks = bookOutcomeDoer.GetTopicRulePacks();
                if (topicRulePacks != null)
                {
                    foreach (RulePack rulePack in topicRulePacks)
                    {
                        GrammarRequest grammarRequestX = grammarRequest;
                        grammarRequestX.IncludesBare.Add(rulePack);
                        List<ValueTuple<string, string>> list = new List<ValueTuple<string, string>>();
                        foreach (Verse.Grammar.Rule rule in rulePack.Rules)
                        {
                            if (rule.keyword.StartsWith("subject_"))
                            {
                                list.Add(new ValueTuple<string, string>(rule.keyword.Substring("subject_".Length), GrammarResolver.Resolve(rule.keyword, grammarRequestX, null, false, null, null, null, false)));
                            }
                        }
                        RoyalTitlePermitWorker_DropBook.subjects.Add(new HautsUtility.BookSubjectSymbol
                        {
                            keyword = GrammarResolver.Resolve("subject", grammarRequestX, null, false, null, null, null, false),
                            subSymbols = list
                        });
                    }
                }
            }
            RoyalTitlePermitWorker_DropBook.AppendRulesForSubject(RoyalTitlePermitWorker_DropBook.subjects, grammarRequest.Rules, grammarRequest.Constants, "primary", 0);
            RoyalTitlePermitWorker_DropBook.AppendRulesForSubject(RoyalTitlePermitWorker_DropBook.subjects, grammarRequest.Rules, grammarRequest.Constants, "secondary", 1);
            RoyalTitlePermitWorker_DropBook.AppendRulesForSubject(RoyalTitlePermitWorker_DropBook.subjects, grammarRequest.Rules, grammarRequest.Constants, "tertiary", 2);
            GrammarRequest grammarRequest2 = grammarRequest;
            this.ExtraTitleGrammarRules(book, ref grammarRequest2);
            if (pme.bookTitlePack != null)
            {
                grammarRequest2.Includes.Add(pme.bookTitlePack??book.BookComp.Props.nameMaker);
                typeof(Book).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, GenText.CapitalizeAsTitle(GrammarResolver.Resolve("title", grammarRequest2, null, false, null, null, null, true)).StripTags());
            }
            GrammarRequest grammarRequest3 = grammarRequest;
            grammarRequest3.Includes.Add(pme.bookDescPack??book.BookComp.Props.descriptionMaker);
            this.ExtraDescGrammarRules(book, ref grammarRequest3);
            grammarRequest3.Includes.Add(RulePackDefOf.TalelessImages);
            grammarRequest3.Includes.Add(RulePackDefOf.ArtDescriptionRoot_Taleless);
            grammarRequest3.Includes.Add(RulePackDefOf.ArtDescriptionUtility_Global);
            typeof(Book).GetField("descriptionFlavor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, GrammarResolver.Resolve("desc", grammarRequest3, null, false, null, null, null, true).StripTags());
            typeof(Book).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, this.GenerateFullDescription(book));
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, false);
            return book;
        }
        private static void AppendRulesForSubject(List<HautsUtility.BookSubjectSymbol> subjects, List<Verse.Grammar.Rule> rules, Dictionary<string, string> constants, string postfix, int i)
        {
            if (i < subjects.Count)
            {
                rules.Add(new Rule_String("subject_" + postfix, subjects[i].keyword));
                constants.Add("length_subject_" + postfix, subjects[i].keyword.Length.ToString());
                constants.Add("has_subject_" + postfix, "true");
                using (List<ValueTuple<string, string>>.Enumerator enumerator = subjects[i].subSymbols.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        ValueTuple<string, string> valueTuple = enumerator.Current;
                        rules.Add(new Rule_String("subject_" + postfix + "_" + valueTuple.Item1, valueTuple.Item2));
                    }
                    return;
                }
            }
            constants.Add("has_subject_" + postfix, "false");
        }
        private string GenerateFullDescription(Book book)
        {
            StringBuilder stringBuilder = new StringBuilder();
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, false);
            string title = (string)typeof(Book).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(book);
            string descriptionFlavor = (string)typeof(Book).GetField("descriptionFlavor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(book);
            stringBuilder.AppendLine(title.Colorize(ColoredText.TipSectionTitleColor) + GenLabel.LabelExtras(book, false, true) + "\n");
            stringBuilder.AppendLine(descriptionFlavor + "\n");
            if (book.MentalBreakChancePerHour > 0f)
            {
                stringBuilder.AppendLine(string.Format(" - {0}: {1}", "BookMentalBreak".Translate(), "PerHour".Translate(book.MentalBreakChancePerHour.ToStringPercent("0.0"))));
            }
            foreach (BookOutcomeDoer bookOutcomeDoer in book.BookComp.Doers)
            {
                string benefitsString = bookOutcomeDoer.GetBenefitsString(null);
                if (!string.IsNullOrEmpty(benefitsString))
                {
                    if (bookOutcomeDoer.BenefitDetailsCanChange(null))
                    {
                        typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, true);
                    }
                    stringBuilder.AppendLine(benefitsString);
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
        public Faction faction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
        private static List<HautsUtility.BookSubjectSymbol> subjects = new List<HautsUtility.BookSubjectSymbol>();
        
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropResourcesStuff : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallResources(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.BeginCallResources(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_DropResourcesStuff.CommandTex,
                action = delegate
                {
                    Caravan caravan = pawn.GetCaravan();
                    float num = caravan.MassUsage;
                    List<ThingDefCountClass> itemsToDrop = this.def.royalAid.itemsToDrop;
                    for (int i = 0; i < itemsToDrop.Count; i++)
                    {
                        num += itemsToDrop[i].thingDef.BaseMass * (float)this.ItemStackCount(itemsToDrop[i], null, pawn);
                    }
                    if (num > caravan.MassCapacity)
                    {
                        WindowStack windowStack = Find.WindowStack;
                        TaggedString taggedString = "DropResourcesOverweightConfirm".Translate();
                        Action action = delegate
                        {
                            this.CallResourcesToCaravan(pawn, faction, this.free);
                        };
                        windowStack.Add(Dialog_MessageBox.CreateConfirmation(taggedString, action, true, null, WindowLayer.Dialog));
                        return;
                    }
                    this.CallResourcesToCaravan(pawn, faction, this.free);
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallResources(IntVec3 cell)
        {
            List<Thing> list = new List<Thing>();
            for (int i = 0; i < this.def.royalAid.itemsToDrop.Count; i++)
            {
                PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
                Thing thing = ThingMaker.MakeThing(this.def.royalAid.itemsToDrop[i].thingDef, this.def.royalAid.itemsToDrop[i].stuff);
                thing.stackCount = this.ItemStackCount(this.def.royalAid.itemsToDrop[i], pme, this.caller);
                if (thing.TryGetComp(out CompQuality compQuality))
                {
                    compQuality.SetQuality(this.def.royalAid.itemsToDrop[i].quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                }
                if (thing.TryGetComp(out CompPowerBattery compBattery))
                {
                    compBattery.SetStoredEnergyPct(1f);
                }
                if (thing.def.Minifiable)
                {
                    MinifiedThing minifiedThing = thing.MakeMinified();
                    list.Add(minifiedThing);
                } else {
                    list.Add(thing);
                }
            }
            if (list.Any<Thing>())
            {
                ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                DropPodUtility.MakeDropPodAt(cell, this.map, activeTransporterInfo, null);
                Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(this.caller,this.faction);
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        public virtual int ItemStackCount(ThingDefCountClass tdcc, PermitMoreEffects pme, Pawn caller)
        {
            return tdcc.count;
        }
        private void CallResourcesToCaravan(Pawn caller, Faction faction, bool free)
        {
            Caravan caravan = caller.GetCaravan();
            for (int i = 0; i < this.def.royalAid.itemsToDrop.Count; i++)
            {
                PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
                Thing thing = ThingMaker.MakeThing(this.def.royalAid.itemsToDrop[i].thingDef, this.def.royalAid.itemsToDrop[i].stuff);
                thing.stackCount = this.ItemStackCount(this.def.royalAid.itemsToDrop[i], pme, caller);
                if (thing.TryGetComp(out CompQuality compQuality))
                {
                    compQuality.SetQuality(this.def.royalAid.itemsToDrop[i].quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                }
                if (thing.def.Minifiable)
                {
                    MinifiedThing minifiedThing = thing.MakeMinified();
                    CaravanInventoryUtility.GiveThing(caravan, minifiedThing);
                } else {
                    CaravanInventoryUtility.GiveThing(caravan, thing);
                }
            }
            Messages.Message("MessagePermitTransportDropCaravan".Translate(faction.Named("FACTION"), caller.Named("PAWN")), caravan, MessageTypeDefOf.NeutralEvent, true);
            caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!free)
            {
                caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
            this.DoOtherEffect(caller, faction);
        }
        public Faction faction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropResourcesOfCategory : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallResources(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.BeginCallResources(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_DropResourcesOfCategory.CommandTex,
                action = delegate
                {
                    Caravan caravan = pawn.GetCaravan();
                    float num = caravan.MassUsage;
                    List<ThingDefCountClass> itemsToDrop = this.def.royalAid.itemsToDrop;
                    for (int i = 0; i < itemsToDrop.Count; i++)
                    {
                        num += itemsToDrop[i].thingDef.BaseMass * (float)itemsToDrop[i].count;
                    }
                    if (num > caravan.MassCapacity)
                    {
                        WindowStack windowStack = Find.WindowStack;
                        TaggedString taggedString = "DropResourcesOverweightConfirm".Translate();
                        Action action = delegate
                        {
                            this.CallResourcesToCaravan(pawn, faction, this.free);
                        };
                        windowStack.Add(Dialog_MessageBox.CreateConfirmation(taggedString, action, true, null, WindowLayer.Dialog));
                        return;
                    }
                    this.CallResourcesToCaravan(pawn, faction, this.free);
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallResources(IntVec3 cell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                List<Thing> list = new List<Thing>();
                ThingDef oneThing = null;
                if (pme.allRandomOutcomesMustBeSamePerUse)
                {
                    if (!pme.targetableThings.NullOrEmpty())
                    {
                        oneThing = pme.targetableThings.RandomElement();
                    } else {
                        DefDatabase<ThingDef>.AllDefsListForReading.TryRandomElement((ThingDef x) => this.IsValidItemOption(x, pme), out oneThing);
                    }
                }
                for (int i = 0; i < this.ItemStackCount(pme, this.caller); i++)
                {
                    ThingDef randomThing;
                    if (oneThing != null)
                    {
                        randomThing = oneThing;
                    } else if (!pme.targetableThings.NullOrEmpty()) {
                        randomThing = pme.targetableThings.RandomElement();
                    } else {
                        DefDatabase<ThingDef>.AllDefsListForReading.TryRandomElement((ThingDef x) => this.IsValidItemOption(x, pme), out randomThing);
                    }
                    if (randomThing != null)
                    {
                        Thing thing = ThingMaker.MakeThing(randomThing, GenStuff.RandomStuffFor(randomThing));
                        if (thing.TryGetComp(out CompQuality compQuality) && pme.extraNumber != null)
                        {
                            compQuality.SetQuality((QualityCategory)pme.extraNumber.RandomInRange, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                        }
                        if (thing.def.Minifiable)
                        {
                            MinifiedThing minifiedThing = thing.MakeMinified();
                            list.Add(minifiedThing);
                        } else {
                            list.Add(thing);
                        }
                    }
                }
                if (list.Any<Thing>())
                {
                    ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                    activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                    DropPodUtility.MakeDropPodAt(cell, this.map, activeTransporterInfo, null);
                    Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
                    this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                    if (!this.free)
                    {
                        this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                    }
                    this.DoOtherEffect(this.caller,this.faction);
                }
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        public bool IsValidItemOption(ThingDef x, PermitMoreEffects pme)
        {
            return x.techLevel <= pme.maxTechLevelInCategory && x.techLevel >= pme.minTechLevelInCategory && x.BaseMarketValue <= pme.marketValueLimits.max && x.BaseMarketValue >= pme.marketValueLimits.min && (pme.thingCategories.NullOrEmpty() || (!x.thingCategories.NullOrEmpty() && x.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd)))) && (pme.forbiddenThingCategories.NullOrEmpty() || x.thingCategories.NullOrEmpty() || !x.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))) && (pme.tradeTags.NullOrEmpty() || (!x.tradeTags.NullOrEmpty() && x.tradeTags.ContainsAny((string tt) => pme.tradeTags.Contains(tt)))) && (pme.forbiddenTradeTags.NullOrEmpty() || (x.tradeTags.NullOrEmpty() && !x.tradeTags.ContainsAny((string tt) => pme.tradeTags.Contains(tt))));
        }
        public virtual int ItemStackCount(PermitMoreEffects pme, Pawn caller)
        {
            return pme.numFromCategory.RandomInRange;
        }
        private void CallResourcesToCaravan(Pawn caller, Faction faction, bool free)
        {
            Caravan caravan = caller.GetCaravan();
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                List<Thing> list = new List<Thing>();
                for (int i = 0; i < this.ItemStackCount(pme,caller); i++)
                {
                    ThingDef randomThing;
                    if (!pme.targetableThings.NullOrEmpty())
                    {
                        randomThing = pme.targetableThings.RandomElement();
                    } else {
                        DefDatabase<ThingDef>.AllDefsListForReading.TryRandomElement((ThingDef x) => this.IsValidItemOption(x, pme), out randomThing);
                    }
                    if (randomThing != null)
                    {
                        Thing thing = ThingMaker.MakeThing(randomThing, GenStuff.RandomStuffFor(randomThing));
                        if (thing.TryGetComp(out CompQuality compQuality))
                        {
                            compQuality.SetQuality(this.def.royalAid.itemsToDrop[i].quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                        }
                        if (thing.def.Minifiable)
                        {
                            MinifiedThing minifiedThing = thing.MakeMinified();
                            CaravanInventoryUtility.GiveThing(caravan, minifiedThing);
                        } else {
                            CaravanInventoryUtility.GiveThing(caravan, thing);
                        }
                        Messages.Message("MessagePermitTransportDropCaravan".Translate(faction.Named("FACTION"), caller.Named("PAWN")), caravan, MessageTypeDefOf.NeutralEvent, true);
                        caller.royalty.GetPermit(this.def, faction).Notify_Used();
                        if (!free)
                        {
                            caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                        }
                        this.DoOtherEffect(this.caller,this.faction);
                    }
                }
            }
        }
        public Faction faction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CauseCondition : RoyalTitlePermitWorker_Targeted
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.MakeCondition(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        protected virtual void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.conditionDefs != null && caller.MapHeld != null)
            {
                GameConditionManager gameConditionManager = caller.MapHeld.GameConditionManager;
                GameCondition gameCondition = GameConditionMaker.MakeCondition(pme.conditionDefs.RandomElement(), pme.conditionDuration.RandomInRange);
                gameConditionManager.RegisterCondition(gameCondition);
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                if (pme.screenShake && caller.MapHeld == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(caller.PositionHeld, caller.MapHeld, false));
                }
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(caller,faction);
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
    }
    public class LumpQuest : DefModExtension
    {
        public LumpQuest() { }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_GenerateQuest : RoyalTitlePermitWorker_Targeted
    {
        public virtual bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return !f.IsPlayer && !f.defeated && !f.temporary;
        }
        public IEnumerable<Faction> CandidateFactions(Map map, bool desperate = false)
        {
            return Find.FactionManager.AllFactions.Where((Faction f) => this.FactionCanBeGroupSource(f, map, desperate));
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (!this.CandidateFactions(map, false).Any<Faction>())
            {
                yield return new FloatMenuOption("Hauts_NoFactionCanFieldIncident".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_GenerateQuest.CommandTex,
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                }
            };
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        public virtual int NumQuestsToGenerate(PermitMoreEffects pme, Pawn caller, Faction faction)
        {
            return pme.questCount;
        }
        protected void GiveQuest(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && (pme.questScriptDefs != null || pme.incidentDefs != null))
            {
                bool done = false;
                for (int i = 0; i < this.NumQuestsToGenerate(pme,caller,faction); i++)
                {
                    int questCount = pme.questScriptDefs != null ? pme.questScriptDefs.Count : 0;
                    int incidentCount = pme.incidentDefs != null ? pme.incidentDefs.Count : 0;
                    bool questNotIncident = Rand.Chance((float)questCount / Math.Max(1f, questCount + incidentCount));
                    parms.points = pme.GetIncidentPoints(caller);
                    if (questNotIncident)
                    {
                        QuestScriptDef questDef = pme.questScriptDefs.RandomElement();
                        Slate slate = new Slate();
                        slate.Set<TaggedString>("discoveryMethod", "Hauts_QuestDiscoveredByPermit".Translate(caller.Named("PERMITUSER")), false);
                        slate.Set<float>("points", parms.points, false);
                        if (questDef.HasModExtension<LumpQuest>())
                        {
                            List<ThingDef> mineables = ((GenStep_PreciousLump)GenStepDefOf.PreciousLump.genStep).mineables;
                            ThingDef targetMineable = mineables.RandomElement();
                            slate.Set<ThingDef>("targetMineable", targetMineable, false);
                            slate.Set<ThingDef>("targetMineableThing", targetMineable.building.mineableThing, false);
                            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                            Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest, null, null, 0, true);
                        } else {
                            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                            if (!quest.hidden && questDef.sendAvailableLetter)
                            {
                                QuestUtility.SendLetterQuestAvailable(quest);
                            }
                        }
                        done = true;
                    } else {
                        IncidentDef incidentDef = pme.incidentDefs.RandomElement();
                        Faction funcFaction = pme.incidentUsesPermitFaction ? faction : this.CandidateFactions(caller.Map??null, false).RandomElement();
                        Faction raidFaction = Find.FactionManager.AllFactionsListForReading.Where((Faction f) => !f.IsPlayer && f.HostileTo(Faction.OfPlayerSilentFail) && !f.defeated && !f.temporary && (caller.Map != null || (f.def.allowedArrivalTemperatureRange.Includes(caller.Map.mapTemperature.OutdoorTemp) && f.def.allowedArrivalTemperatureRange.Includes(caller.Map.mapTemperature.SeasonalTemp)))).RandomElement();
                        IncidentParms incidentParms = new IncidentParms
                        {
                            forced = true,
                            points = parms.points,
                            faction = funcFaction,
                            raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                            raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                            traderKind = (funcFaction != null && !funcFaction.IsPlayer && !funcFaction.def.caravanTraderKinds.NullOrEmpty() && funcFaction.def.caravanTraderKinds.ContainsAny((TraderKindDef tkd2) => tkd2.requestable) && funcFaction.AllyOrNeutralTo(Faction.OfPlayerSilentFail)) ? funcFaction.def.caravanTraderKinds.Where((TraderKindDef tkd)=>tkd.requestable).RandomElement() : null
                        };
                        if (caller.Map != null)
                        {
                            incidentParms.target = caller.Map;
                        } else if (Find.AnyPlayerHomeMap != null) {
                            incidentParms.target = Find.AnyPlayerHomeMap;
                        } else if (Find.WorldObjects.Caravans.Count > 0) {
                            incidentParms.target = Find.WorldObjects.Caravans.RandomElement();
                        } else {
                            incidentParms.target = Find.World;
                        }
                        if (pme.incidentDelay != null)
                        {
                            Find.Storyteller.incidentQueue.Add(incidentDef, Find.TickManager.TicksGame + pme.incidentDelay.RandomInRange, incidentParms, 240000);
                        } else if (incidentDef.Worker.CanFireNow(incidentParms)) {
                            incidentDef.Worker.TryExecute(incidentParms);
                        }
                        done = true;
                    }
                }
                if (done)
                {
                    caller.royalty.GetPermit(this.def, faction).Notify_Used();
                    if (!free)
                    {
                        caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                    }
                    this.DoOtherEffect(caller,faction);
                }
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    public class RoyalTitlePermitWorker_MultiplyItemStack : RoyalTitlePermitWorker_Targeted
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    if (pme.targetableThings != null)
                    {
                        foreach (Thing t in lti.Cell.GetThingList(this.caller.Map))
                        {
                            if (pme.targetableThings.Contains(t.def))
                            {
                                return AcceptanceReport.WasAccepted;
                            }
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.targetableThings != null)
            {
                foreach (Thing t in target.Cell.GetThingList(this.caller.Map))
                {
                    if (pme.targetableThings.Contains(t.def))
                    {
                        this.Invest(t, this.calledFaction);
                        break;
                    }
                }
            }
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (this.IsFactionHostileToPlayer(faction,pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.BeginInvest(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        private void BeginInvest(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        private void Invest(Thing thing, Faction faction)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                int amountTaken = Math.Min(thing.stackCount, (int)pme.extraNumber.max);
                if (pme.gambaDropPodSoNotInstant)
                {
                    IncidentParms parms = new IncidentParms();
                    parms.target = thing.Map;
                    if (amountTaken < thing.stackCount)
                    {
                        thing.stackCount -= amountTaken;
                        Thing thing2 = ThingMaker.MakeThing(thing.def,thing.Stuff);
                        thing2.stackCount = amountTaken;
                        parms.gifts = new List<Thing>
                        {
                            thing2
                        };
                    } else {
                        parms.gifts = new List<Thing>
                        {
                            thing
                        };
                        thing.DeSpawn();
                    }
                    //this.calledFaction.leader.inventory.TryAddAndUnforbid(thing);
                    parms.controllerPawn = this.caller;
                    parms.biocodeWeaponsChance = pme.gambaFactorRange.min;
                    parms.biocodeApparelChance = pme.gambaFactorRange.max;
                    parms.customLetterText = pme.returnMessage;
                    parms.faction = faction;
                    Find.Storyteller.incidentQueue.Add(HautsDefOf.Hauts_InvestmentReturn, Find.TickManager.TicksGame + pme.gambaReturnDelay.RandomInRange, parms, 600);
                } else {
                    float comeOn = amountTaken * pme.gambaFactorRange.RandomInRange;
                    thing.stackCount -= amountTaken;
                    thing.stackCount += (int)comeOn;
                    if (thing.stackCount <= 0)
                    {
                        thing.Destroy();
                    } else {
                        while (thing.stackCount > thing.def.stackLimit)
                        {
                            Thing thing2 = thing.SplitOff(thing.stackCount - thing.def.stackLimit);
                            GenDrop.TryDropSpawn(thing2, thing.PositionHeld, map, ThingPlaceMode.Near, out Thing resultingThing, null, null, true);
                        }
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(this.caller,this.calledFaction);
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        private Faction calledFaction;
    }
    public class IncidentWorker_MultiplyItemStackDelay : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (parms.target != null && parms.gifts != null)
            {
                Map map = (Map)parms.target;
                if (map == null || map.Disposed)
                {
                    if (parms.controllerPawn != null && !parms.controllerPawn.Dead && parms.controllerPawn.Map != null)
                    {
                        map = parms.controllerPawn.Map;
                    }
                }
                if (map != null)
                {
                    IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
                    if (dropSpot.IsValid && parms.gifts.Count > 0)
                    {
                        foreach (Thing t in parms.gifts)
                        {
                            float comeOn = t.stackCount * Rand.Range(parms.biocodeWeaponsChance, parms.biocodeApparelChance);
                            t.stackCount = (int)comeOn;
                            TradeUtility.SpawnDropPod(dropSpot, map, t);
                        }
                        Messages.Message(parms.customLetterText.Translate(parms.faction.Named("FACTION")), new LookTargets(dropSpot, map), MessageTypeDefOf.NeutralEvent, true);
                        return true;
                    }
                }
            }
            return false;
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_TargetPawn : RoyalTitlePermitWorker_Targeted
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    foreach (Thing t in lti.Cell.GetThingList(this.caller.Map))
                    {
                        if (t is Pawn p && this.IsGoodPawn(p))
                        {
                            return AcceptanceReport.WasAccepted;
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
        public virtual bool IsGoodPawn(Pawn pawn)
        {
            return true;
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                foreach (Thing t in target.Cell.GetThingList(this.caller.Map))
                {
                    if (t is Pawn p && this.IsGoodPawn(p))
                    {
                        this.AffectPawn(p,this.calledFaction);
                        break;
                    }
                }
            }
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_TargetPawn.CommandTex,
                action = delegate
                {
                    this.caller = pawn;
                    this.GiveHediffInCaravan(pawn, faction, this.free);
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        private void GiveHediffInCaravan(Pawn caller, Faction faction, bool free)
        {
            Caravan caravan = caller.GetCaravan();
            this.GiveHediffInCaravanInner(caller,faction,free,caravan);
        }
        public virtual void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            this.AffectPawn(caller, faction);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.BeginAffectPawn(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        private void BeginAffectPawn(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        protected void AffectPawn(Pawn pawn, Faction faction)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                if (pme.onUseMessage != null)
                {
                    Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION"), pawn.Named("PAWN")), pawn, MessageTypeDefOf.NeutralEvent, true);
                }
                if (pme.soundDef != null && pawn.SpawnedOrAnyParentSpawned)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(pawn.PositionHeld, pawn.MapHeld, false));
                }
                this.AffectPawnInner(pme,pawn,faction);
                this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(this.caller,faction);
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        public Faction CalledFaction { 
            get {
                return this.CalledFaction;
            } 
        }
        public virtual void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
        }
        private Faction calledFaction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    public class RoyalTitlePermitWorker_GiveHediffs : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            return HautsUtility.AllowCheckPMEs(pme,pawn.kindDef);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            foreach (HediffDef hd in pme.hediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(hd, pawn);
                pawn.health.AddHediff(hediff);
            }
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropPawns : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallPawns(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (this.OverridableFillAidOption(pawn,faction,ref text,out free))
            {
                action = delegate
                {
                    this.BeginCallPawns(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public virtual bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        public virtual bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return base.FillAidOption(pawn, faction, ref text, out free);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_DropPawns.CommandTex,
                action = delegate
                {
                    this.CallResourcesToCaravan(pawn, faction, this.free);
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        private void BeginCallPawns(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallPawns(IntVec3 cell)
        {
            List<Pawn> list = new List<Pawn>();
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                int pawnSpawnCount = pme.numFromCategory != null ? pme.numFromCategory.RandomInRange : pme.phenomenonCount;
                PawnKindDef onePkd = pme.allRandomOutcomesMustBeSamePerUse ? this.ChoosePawnKindToDrop(pme) : null;
                for (int i = 0; i < pawnSpawnCount; i++)
                {
                    PawnKindDef pkd = onePkd??this.ChoosePawnKindToDrop(pme);
                    if (pkd != null)
                    {
                        Pawn p = PawnGenerator.GeneratePawn(pkd, pme.startsTamed ? this.caller.Faction : null);
                        if (pme.hediffs != null)
                        {
                            foreach (HediffDef hd in pme.hediffs)
                            {
                                p.health.AddHediff(hd);
                            }
                        }
                        list.Add(p);
                    }
                }
            }
            if (list.Any<Pawn>())
            {
                ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                DropPodUtility.MakeDropPodAt(cell, this.map, activeTransporterInfo, null);
                Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(this.caller,this.faction);
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        private void CallResourcesToCaravan(Pawn caller, Faction faction, bool free)
        {
            Caravan caravan = caller.GetCaravan();
            List<Pawn> list = new List<Pawn>();
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                int pawnSpawnCount = pme.numFromCategory != null ? pme.numFromCategory.RandomInRange : pme.phenomenonCount;
                for (int i = 0; i < pawnSpawnCount; i++)
                {
                    PawnKindDef pkd = this.ChoosePawnKindToDrop(pme);
                    if (pkd != null)
                    {
                        Pawn p = PawnGenerator.GeneratePawn(pkd, pme.startsTamed ? this.caller.Faction : null);
                        if (pme.hediffs != null)
                        {
                            foreach (HediffDef hd in pme.hediffs)
                            {
                                p.health.AddHediff(hd);
                            }
                        }
                        list.Add(p);
                    }
                }
            }
            if (!list.NullOrEmpty())
            {
                foreach (Pawn p in list)
                {
                    Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
                    caravan.AddPawn(p,true);
                    p.SetFaction(pme.startsTamed ? caller.Faction : null);
                }
            }
            Messages.Message("MessagePermitTransportDropCaravan".Translate(faction.Named("FACTION"), caller.Named("PAWN")), caravan, MessageTypeDefOf.NeutralEvent, true);
            caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!free)
            {
                caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
            this.DoOtherEffect(caller,faction);
        }
        private PawnKindDef ChoosePawnKindToDrop(PermitMoreEffects pme)
        {
            PawnKindDef pawnToMake = PawnKindDefOf.WildMan;
            if (!pme.allowedPawnKinds.NullOrEmpty())
            {
                pawnToMake = pme.allowedPawnKinds.RandomElement();
            } else {
                List<PawnKindDef> possiblePawnsFromAllowBools = DefDatabase<PawnKindDef>.AllDefsListForReading.Where((PawnKindDef p) => (!pme.needsPen || p.RaceProps.Roamer) && (pme.maxWildness < 0f || p.race.GetStatValueAbstract(StatDefOf.Wildness) <= pme.maxWildness) && p.RaceProps.petness >= pme.minPetness && (!pme.mustBePredator || p.RaceProps.predator) && (pme.disallowedPawnKinds ==null || !pme.disallowedPawnKinds.Contains(p)) && (pme.marketValueLimits == null || (pme.marketValueLimits.min <= p.race.GetStatValueAbstract(StatDefOf.MarketValue) && pme.marketValueLimits.max >= p.race.GetStatValueAbstract(StatDefOf.MarketValue))) && (pme.bodySizeCapRange == null || pme.bodySizeCapRange.Includes(p.RaceProps.baseBodySize)) && HautsUtility.AllowCheckPMEs(pme, p)).ToList<PawnKindDef>();
                if (!possiblePawnsFromAllowBools.NullOrEmpty())
                {
                    pawnToMake = possiblePawnsFromAllowBools.RandomElement();
                }
            }
                return pawnToMake;
        }
        private Faction faction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    //verbs and damage
    public class Verb_MeleeShot : Verse.Verb_Shoot
    {
    }
    public class DamageWorker_AddInjurySkip : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (thing is Pawn pawn)
            {
                dinfo.SetAmount(dinfo.Amount / Math.Max(0.01f, pawn.GetStatValue(StatDefOf.IncomingDamageFactor)));
            }
            return base.Apply(dinfo, thing);
        }
    }
    public class Verb_CastAbilityCombatSelfBuff : RimWorld.Verb_CastAbility
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Pawn != null || target.Thing is Building_Turret)
            {
                return true;
            }
            return false;
        }
    }
    public class PsycastCanTargetDeaf : DefModExtension
    {
        public PsycastCanTargetDeaf()
        {
        }
    }
    //delayed resurrection
    public class WorldComponent_HautsDelayedResurrections : WorldComponent
    {
        public WorldComponent_HautsDelayedResurrections(World world) : base(world)
        {
            this.world = world;
            this.pawns = new List<Hauts_DelayedResurrection>();
        }
        public void StartDelayedResurrection(Corpse corpse, IntRange rareTickRange, string explanationKey, bool shouldSendMessage, bool shouldTranslateMessage, bool preventRisingAsShambler, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            int initialRareTicks = rareTickRange.RandomInRange;
            bool alreadyHasDelayedRes = false;
            foreach (Hauts_DelayedResurrection hdr in this.pawns)
            {
                if (hdr.corpse == corpse && hdr.rareTicksRemaining > initialRareTicks)
                {
                    alreadyHasDelayedRes = true;
                    hdr.rareTicksRemaining = initialRareTicks;
                    hdr.explanationKey = explanationKey;
                    hdr.shouldSendMessage = shouldSendMessage;
                    hdr.shouldTranslateMessage = shouldTranslateMessage;
                    hdr.preventRisingAsShambler = preventRisingAsShambler;
                    hdr.mutation = mutation;
                    hdr.mutationSeverity = mutationSeverity;
                    break;
                }
            }
            if (!alreadyHasDelayedRes)
            {
                this.pawns.Add(new Hauts_DelayedResurrection(corpse,initialRareTicks,explanationKey,shouldSendMessage,shouldTranslateMessage,preventRisingAsShambler,mutation,mutationSeverity));
            }
        }
        public void RemoveFromRoster(Corpse corpse)
        {
            for (int i = this.pawns.Count - 1; i >= 0; i--)
            {
                if (this.pawns[i].corpse == corpse && this.pawns[i].rareTicksRemaining > 0)
                {
                    this.pawns.Remove(this.pawns[i]);
                    break;
                }
            }
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                for (int i = this.pawns.Count - 1; i >= 0; i--)
                {
                    if (this.pawns[i].corpse == null || this.pawns[i].corpse.Destroyed || this.pawns[i].corpse.InnerPawn == null)
                    {
                        this.pawns.Remove(this.pawns[i]);
                        continue;
                    }
                    this.pawns[i].rareTicksRemaining--;
                    if (this.pawns[i].rareTicksRemaining <= 0)
                    {
                        Pawn pawn = this.pawns[i].corpse.InnerPawn;
                        if (ResurrectionUtility.TryResurrect(pawn))
                        {
                            if (ModsConfig.AnomalyActive && pawn.IsShambler)
                            {
                                pawn.mutant.Revert();
                            }
                            if (this.pawns[i].mutation != null)
                            {
                                if (pawn.health.hediffSet.HasHediff(this.pawns[i].mutation))
                                {
                                    pawn.health.hediffSet.GetFirstHediffOfDef(this.pawns[i].mutation).Severity += this.pawns[i].mutationSeverity;
                                } else {
                                    Hediff hediff = HediffMaker.MakeHediff(this.pawns[i].mutation, pawn);
                                    pawn.health.AddHediff(hediff, null);
                                    hediff.Severity = this.pawns[i].mutationSeverity;
                                }
                            }
                            if (this.pawns[i].shouldSendMessage && (PawnUtility.ShouldSendNotificationAbout(pawn) || (pawn.Faction != null && pawn.Faction == Faction.OfPlayer) || pawn.SpawnedOrAnyParentSpawned || (pawn.IsCaravanMember() && pawn.GetCaravan().IsPlayerControlled)))
                            {
                                LookTargets target = null;
                                if (pawn.Spawned)
                                {
                                    target = pawn;
                                }
                                Messages.Message((this.pawns[i].shouldTranslateMessage ? this.pawns[i].explanationKey.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : this.pawns[i].explanationKey), target, MessageTypeDefOf.PositiveEvent, true);
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Hauts_DelayedResurrection>(ref this.pawns, "pawns", LookMode.Deep, Array.Empty<object>());
        }
        public List<Hauts_DelayedResurrection> pawns = new List<Hauts_DelayedResurrection>();
    }
    public class Hauts_DelayedResurrection : IExposable
    {
        public Hauts_DelayedResurrection()
        {
        }
        public Hauts_DelayedResurrection (Corpse corpse, IntRange rareTickRange, string explanationKey, bool shouldSendMessage = true, bool shouldTranslateMessage = true, bool preventRisingAsShambler = true, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            this.corpse = corpse;
            this.rareTicksRemaining = rareTickRange.RandomInRange;
            this.explanationKey = explanationKey;
            this.shouldSendMessage = shouldSendMessage;
            this.shouldTranslateMessage = shouldTranslateMessage;
            this.preventRisingAsShambler = preventRisingAsShambler;
            this.mutation = mutation;
            this.mutationSeverity = mutationSeverity;
        }
        public Hauts_DelayedResurrection(Corpse corpse, int initialRareTicks, string explanationKey, bool shouldSendMessage = true, bool shouldTranslateMessage = true, bool preventRisingAsShambler = true, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            this.corpse = corpse;
            this.rareTicksRemaining = initialRareTicks;
            this.explanationKey = explanationKey;
            this.shouldSendMessage = shouldSendMessage;
            this.shouldTranslateMessage = shouldTranslateMessage;
            this.preventRisingAsShambler = preventRisingAsShambler;
            this.mutation = mutation;
            this.mutationSeverity = mutationSeverity;
        }
        public void ExposeData()
        {
            Scribe_References.Look<Corpse>(ref this.corpse, "corpse", false);
            Scribe_Values.Look<int>(ref this.rareTicksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look<string>(ref this.explanationKey, "explanationKey", "");
            Scribe_Values.Look<bool>(ref this.shouldSendMessage, "shouldSendMessage", true);
            Scribe_Values.Look<bool>(ref this.shouldTranslateMessage, "shouldTranslateMessage", true);
            Scribe_Values.Look<bool>(ref this.preventRisingAsShambler, "preventRisingAsShambler", true);
        }
        public Corpse corpse;
        public int rareTicksRemaining;
        public string explanationKey = "";
        public bool shouldSendMessage;
        public bool shouldTranslateMessage;
        public bool preventRisingAsShambler;
        public HediffDef mutation;
        public float mutationSeverity;
    }
    //thing comp: aura emitter
    public class CompProperties_AuraEmitter : CompProperties
    {
        public CompProperties_AuraEmitter()
        {
            this.compClass = typeof(CompAuraEmitter);
        }
        public float range;
        public int periodicity = 150;
        public StatDef rangeModifier = null;
        public float maxRangeModifier = 1f;
        public float minRangeModifier = 1f;
        public bool affectEnemies = true;
        public bool affectNeutrals = true;
        public bool affectOwnFaction = true;
        public bool affectCreator = true;
        public StatDef requiredStat = null;
        public float minStat = 1E-45f;
        public bool scanByPawnListerNotByGrid = true;
        public Color color;
    }
    public class CompAuraEmitter : ThingComp
    {
        public CompProperties_AuraEmitter Props
        {
            get
            {
                return (CompProperties_AuraEmitter)this.props;
            }
        }
        public virtual float FunctionalRange
        {
            get
            {
                if (this.Props.rangeModifier != null)
                {
                    return this.Props.range * Math.Min(this.Props.maxRangeModifier, Math.Max(this.Props.minRangeModifier, this.parent.GetStatValue(this.Props.rangeModifier)));
                }
                return this.Props.range * this.radiusScalar;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (this.creator != null)
            {
                this.faction = this.creator.Faction;
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(this.Props.periodicity, delta) && this.parent.SpawnedOrAnyParentSpawned)
            {
                this.DoOnTrigger();
                if (this.Props.scanByPawnListerNotByGrid)
                {
                    foreach (Pawn p in this.parent.MapHeld.mapPawns.AllPawnsSpawned)
                    {
                        if (p.Position.DistanceTo(this.parent.PositionHeld) <= this.FunctionalRange && this.ShouldAffectPawn(p))
                        {
                            this.AffectPawn(p);
                        }
                    }
                } else {
                    foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.parent.PositionHeld, this.parent.MapHeld, this.FunctionalRange, true).OfType<Pawn>().Distinct<Pawn>())
                    {
                        if (this.ShouldAffectPawn(p))
                        {
                            this.AffectPawn(p);
                        }
                    }
                }
            }
        }
        public virtual void DoOnTrigger()
        {

        }
        public virtual bool ShouldAffectPawn(Pawn pawn)
        {
            if ((this.Props.requiredStat == null || pawn.GetStatValue(this.Props.requiredStat) >= this.Props.minStat))
            {
                if (pawn == this.creator && this.Props.affectCreator)
                {
                    return true;
                }
                if (this.faction == null)
                {
                    return true;
                } else if (pawn.Faction == null && this.Props.affectNeutrals) {
                    return true;
                } else if (this.Props.affectEnemies && pawn.Faction.HostileTo(this.faction) || (this.Props.affectOwnFaction && pawn.Faction == this.faction)) {
                    return true;
                }
            }
            return false;
        }
        public virtual void AffectPawn(Pawn pawn) { }
        public override void PostDraw()
        {
            base.PostDraw();
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(this.parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverheadLow), Quaternion.AngleAxis(0f, Vector3.up), Vector3.one * this.FunctionalRange * 2f), MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, this.Props.color), 0);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.radiusScalar, "radiusScalar", 1f, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_References.Look<Pawn>(ref this.creator, "creator", false);
        }
        public float radiusScalar = 1f;
        public Faction faction;
        public Pawn creator;
    }
    public class CompProperties_AuraEmitterHediff : CompProperties_AuraEmitter
    {
        public CompProperties_AuraEmitterHediff()
        {
            this.compClass = typeof(CompAuraEmitterHediff);
        }
        public List<HediffDef> hediffs;
    }
    public class CompAuraEmitterHediff : CompAuraEmitter
    {
        public new CompProperties_AuraEmitterHediff Props
        {
            get
            {
                return (CompProperties_AuraEmitterHediff)this.props;
            }
        }
        public override void AffectPawn(Pawn pawn)
        {
            base.AffectPawn(pawn);
            foreach (HediffDef h in this.Props.hediffs)
            {
                Hediff hediff = HediffMaker.MakeHediff(h, pawn, null);
                pawn.health.AddHediff(hediff, null);
            }
        }
    }
    //thing comp: team color
    public class CompProperties_FactionColored : CompProperties
    {
        public CompProperties_FactionColored()
        {
            this.compClass = typeof(CompFactionColored);
        }
        public float colorFactor = -1f;
    }
    public class CompFactionColored : CompColorable
    {
        public CompProperties_FactionColored Props
        {
            get
            {
                return (CompProperties_FactionColored)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.SetTeamColor();
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(15, delta))
            {
                this.SetTeamColor();
            }
        }
        public void SetTeamColor()
        {
            Color newColor = new Color(1f,1f,1f);
            if (this.parent.Faction != null)
            {
                newColor = this.parent.Faction.Color;   
            }
            if (this.Props.colorFactor >= 0f)
            {
                newColor.r *= this.Props.colorFactor;
                newColor.g *= this.Props.colorFactor;
                newColor.b *= this.Props.colorFactor;
            }
            this.teamColor = newColor;
            this.SetColor(this.teamColor);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<Color>(ref this.teamColor, "teamColor", Color.white, false);
        }
        public Color teamColor;
    }
    //trait categories
    public class ExciseTraitExempt : DefModExtension
    {
        public ExciseTraitExempt()
        {
        }
    }
    /*thought modifiers - this is for performance's sake. Instead of patching EVERY thought EVERY thought-tick on EVERY pawn to say "hey, is your id this, or this, or this, or... or this?" 
        we can instead patch it to just be "hey, do you have THIS ONE defmodextension?" which is so much simpler*/
    public class ModifyingTraits : DefModExtension
    {
        public ModifyingTraits()
        {
        }
        public Dictionary<TraitDef,float> multiplierTraits = new Dictionary<TraitDef, float>();
        public List<TraitDef> forcePositive;
        public List<TraitDef> forceNegative;
        public Dictionary<GeneDef, float> multiplierGenes = new Dictionary<GeneDef, float>();
        public List<GeneDef> forcePositiveG;
        public List<GeneDef> forceNegativeG;
    }
    //EMP immunity
    public class NoEMPReaction : DefModExtension
    {
        public NoEMPReaction()
        {
        }
    }
    //natural-goodwill ignoring history event defs
    public class IgnoresNaturalGoodwill : DefModExtension
    {
        public IgnoresNaturalGoodwill(){}
    }
    //misc thought worker
    public class ThoughtWorker_OfSameFaction : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!RelationsUtility.PawnsKnowEachOther(pawn, other) || (this.def.hediff != null && !pawn.health.hediffSet.HasHediff(this.def.hediff)) || other.Faction == null || pawn.Faction == null || other.Faction != pawn.Faction)
            {
                return false;
            }
            return true;
        }
    }
    //with 1.5 non-apparel items apparently can't have charges. so uh, fixed because I'm not compromising on the persona neuroformatter's design
    public class CompProperties_ItemCharged : CompProperties
    {
        public NamedArgument ChargeNounArgument
        {
            get
            {
                return this.chargeNoun.Named("CHARGENOUN");
            }
        }
        public CompProperties_ItemCharged()
        {
            this.compClass = typeof(Comp_ItemCharged);
        }
        public int maxCharges;
        public bool destroyOnEmpty;
        [MustTranslate]
        public string chargeNoun = "charge";
        public KeyBindingDef hotKey;
        public bool displayGizmoWhileUndrafted = true;
        public bool displayGizmoWhileDrafted = true;
        public bool displayChargesInLabel = true;
        public bool priceScalesByRemainingCharges = true;
    }
    public class Comp_ItemCharged : ThingComp
    {
        public CompProperties_ItemCharged Props
        {
            get
            {
                return this.props as CompProperties_ItemCharged;
            }
        }
        public int RemainingCharges
        {
            get
            {
                return this.remainingCharges;
            }
        }
        public int MaxCharges
        {
            get
            {
                return this.Props.maxCharges;
            }
        }
        public string LabelRemaining
        {
            get
            {
                return string.Format("{0} / {1}", this.RemainingCharges, this.MaxCharges);
            }
        }
        public virtual string GizmoExtraLabel
        {
            get
            {
                return this.LabelRemaining;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.remainingCharges = this.InitialCharges();
        }
        public virtual int InitialCharges()
        {
            return this.MaxCharges;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.remainingCharges, "remainingCharges", -999, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.remainingCharges == -999)
            {
                this.remainingCharges = this.MaxCharges;
            }
        }
        public override string TransformLabel(string label)
        {
            if (this.Props.displayChargesInLabel)
            {
                return base.TransformLabel(label) + " (" + this.remainingCharges + "/" + this.Props.maxCharges + ")";
            }
            return base.TransformLabel(label);
        }
        public override string CompInspectStringExtra()
        {
            return "ChargesRemaining".Translate(this.Props.ChargeNounArgument) + ": " + this.LabelRemaining;
        }
        public virtual void UsedOnce()
        {
            if (this.remainingCharges > 0)
            {
                this.remainingCharges--;
            }
            if (this.Props.destroyOnEmpty && this.remainingCharges == 0 && !this.parent.Destroyed)
            {
                this.parent.Destroy(DestroyMode.Vanish);
            }
        }
        public virtual bool CanBeUsed(out string reason)
        {
            reason = "";
            if (this.parent.MapHeld == null)
            {
                return false;
            }
            return true;
        }
        protected int remainingCharges;
    }
    public class StatPart_ItemCharged : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            StatPart_ItemCharged.TransformAndExplain(req, ref val, null);
        }
        public override string ExplanationPart(StatRequest req)
        {
            float num = 1f;
            StringBuilder stringBuilder = new StringBuilder();
            StatPart_ItemCharged.TransformAndExplain(req, ref num, stringBuilder);
            return stringBuilder.ToString().TrimEndNewlines();
        }
        private static void TransformAndExplain(StatRequest req, ref float val, StringBuilder explanation)
        {
            Thing thing = req.Thing;
            if (thing != null)
            {
                Comp_ItemCharged cic = thing.TryGetComp<Comp_ItemCharged>();
                if (cic != null && cic.Props.priceScalesByRemainingCharges)
                {
                    float num3 = ((float)cic.RemainingCharges / (float)cic.MaxCharges);
                    if (explanation != null)
                    {
                        explanation.AppendLine("StatsReport_ReloadRemainingChargesMultipler".Translate(cic.Props.ChargeNounArgument, cic.LabelRemaining) + ": x" + num3.ToStringPercent());
                    }
                    val *= num3;
                }
            }
            if (val < 0f)
            {
                val = 0f;
            }
        }
    }
    //burglary and pickpocketing
    public class CaravanArrivalAction_BurgleSettlement : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "Hauts_BurgleIcon".Translate() + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " " + HautsUtility.SettlementAlertLevel(this.settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")";
            }
        }
        public override string ReportString
        {
            get
            {
                return "Hauts_ActivityPilfering".Translate(this.settlement.Label + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " " + HautsUtility.SettlementAlertLevel(this.settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")");
            }
        }
        public CaravanArrivalAction_BurgleSettlement()
        {
        }
        public CaravanArrivalAction_BurgleSettlement(Settlement settlement)
        {
            this.settlement = settlement;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (this.settlement != null && this.settlement.Tile != destinationTile)
            {
                return false;
            }
            return CaravanArrivalAction_BurgleSettlement.CanVisit(caravan, this.settlement);
        }
        public override void Arrived(Caravan caravan)
        {
            if (caravan.IsPlayerControlled)
            {
                HautsUtility.Burgle(caravan, this.settlement);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Settlement>(ref this.settlement, "settlement", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, Settlement settlement)
        {
            return settlement != null && settlement.Spawned && settlement.Visitable && HautsUtility.HasAnyBurglars(caravan);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, Settlement settlement)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_BurgleSettlement>(() => CaravanArrivalAction_BurgleSettlement.CanVisit(caravan, settlement), () => new CaravanArrivalAction_BurgleSettlement(settlement), "Hauts_BurgleIcon".Translate() + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " " + HautsUtility.SettlementAlertLevel(settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")", caravan, settlement.Tile, settlement, null);
        }

        // Token: 0x0400EE43 RID: 60995
        private Settlement settlement;
    }
    public class BurgleWindow : Window
    {
        public BurgleWindow(Caravan caravan, List<Pawn> burglars, Settlement settlement, float burglaryMaxValue, float burglaryMaxWeight, float successChance)
        {
            this.burglars.Clear();
            this.thingsStolen.Clear();
            this.targetedThingCategories.Clear();
            this.categories.Clear();
            this.goodsList.Clear();
            this.caravan = caravan;
            this.burglars = burglars;
            this.forcePause = true;
            this.settlement = settlement;
            this.burglaryMaxValue = burglaryMaxValue;
            this.burglaryMaxWeight = burglaryMaxWeight;
            this.valueRemaining = burglaryMaxValue;
            this.weightRemaining = burglaryMaxWeight;
            this.successChance = successChance;
            this.goodsList = this.settlement.Goods.ToList<Thing>();
            foreach (Thing t in goodsList)
            {
                if (t.def.thingCategories != null)
                {
                    foreach (ThingCategoryDef d in t.def.thingCategories)
                    {
                        if (!this.categories.Contains(d))
                        {
                            this.categories.Add(d);
                        }
                    }
                }
            }
        }
        private float Height
        {
            get
            {
                return 459f + Window.CloseButSize.y + this.Margin * 2f;
            }
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1000f, this.Height);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= 4f + Window.CloseButSize.y;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width * 0.8f, this.scrollHeight);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            Widgets.Label(0f, ref num, viewRect.width, "Hauts_BurgleWindow1".Translate((int)this.burglaryMaxValue, this.settlement.Name, this.burglaryMaxWeight, (this.successChance * 100f)), default(TipSignal));
            num += 14f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(0f, ref num, viewRect.width, "Hauts_BurgleWindow2".Translate(), default(TipSignal));
            Text.Font = GameFont.Small;
            Widgets.Label(0f, ref num, viewRect.width, "Hauts_BurgleWindow3".Translate(), default(TipSignal));
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            foreach (ThingCategoryDef t in this.categories)
            {
                bool flag = this.targetedThingCategories.Contains(t);
                bool flag2 = flag;
                listing_Standard.CheckboxLabeled("Hauts_BurgleWindowCategories".Translate(t.label), ref flag, 15f);
                if (flag != flag2)
                {
                    if (flag)
                    {
                        this.targetedThingCategories.Add(t);
                    }
                    else
                    {
                        this.targetedThingCategories.Remove(t);
                    }
                }
            }
            listing_Standard.End();
            num += listing_Standard.CurHeight + 10f + 4f;
            if (Event.current.type == EventType.Layout)
            {
                this.scrollHeight = Mathf.Max(num, inRect.height);
            }
            Widgets.EndScrollView();
            Rect rect2 = new Rect(0f, inRect.yMax + 4f, inRect.width, Window.CloseButSize.y);
            AcceptanceReport acceptanceReport = this.CanClose();
            if (Widgets.ButtonText(rect2, "OK".Translate(), true, true, true, null))
            {
                if (acceptanceReport.Accepted)
                {
                    List<Thing> settlementGoods = new List<Thing>();
                    foreach (Thing t in this.goodsList)
                    {
                        if (this.targetedThingCategories.Any<ThingCategoryDef>())
                        {
                            foreach (ThingCategoryDef d in this.targetedThingCategories)
                            {
                                if (t.HasThingCategory(d))
                                {
                                    settlementGoods.Add(t);
                                }
                            }
                        } else {
                            settlementGoods = this.settlement.Goods.ToList<Thing>();
                        }
                    }
                    while (this.weightRemaining > 0f && this.valueRemaining > 0f && settlementGoods.Count > 0)
                    {
                        int triesRemaining = 30;
                        while (triesRemaining > 0)
                        {
                            triesRemaining--;
                            Thing t = settlementGoods.RandomElement<Thing>();
                            if (t != null)
                            {
                                int mostYouCouldGetValue = (int)Math.Floor(this.valueRemaining / t.MarketValue);
                                int mostYouCouldGet = Math.Min(mostYouCouldGetValue, (int)Math.Floor(this.weightRemaining / t.GetStatValue(StatDefOf.Mass)));
                                int lowerBoundStack = Math.Min(t.def.stackLimit, t.stackCount);
                                int trueLowest = Math.Min(mostYouCouldGet, lowerBoundStack);
                                float stackMarketValue = trueLowest * t.MarketValue;
                                float stackMass = trueLowest * t.GetStatValue(StatDefOf.Mass);
                                if (stackMarketValue <= this.valueRemaining && trueLowest > 0 && stackMass <= this.weightRemaining)
                                {
                                    this.valueRemaining -= stackMarketValue;
                                    this.weightRemaining -= stackMass;
                                    if (trueLowest < t.stackCount)
                                    {
                                        this.thingsStolen.Add(t.SplitOff(trueLowest));
                                    }
                                    else
                                    {
                                        this.thingsStolen.Add(t);
                                        settlementGoods.Remove(t);
                                    }
                                    break;
                                }
                            }
                        }
                        if (triesRemaining <= 0)
                        {
                            break;
                        }
                    }
                    Faction f = this.settlement.Faction;
                    WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                    if (fch != null)
                    {
                        HautsFactionComp_BurglaryResponse br = fch.TryGetComp<HautsFactionComp_BurglaryResponse>();
                        if (br != null)
                        {
                            float alertRaise = 0f, minAlertRaise = 0f;
                            minAlertRaise += br.Props.minAlertGainFromBurgle;
                            this.Close(true);
                            if (Rand.Value <= this.successChance)
                            {
                                if (this.thingsStolen.Count == 0)
                                {
                                    TaggedString message = "Hauts_BurgleOutcome1".Translate();
                                    LookTargets toLook = new LookTargets(this.caravan);
                                    ChoiceLetter tieLetter = LetterMaker.MakeLetter("Hauts_PilferLetter1".Translate(), message, LetterDefOf.NeutralEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(tieLetter, null);
                                } else {
                                    TaggedString message = "Hauts_BurgleOutcome2".Translate();
                                    foreach (Thing t in this.thingsStolen)
                                    {
                                        this.settlement.trader.GetDirectlyHeldThings().Remove(t);
                                        t.PreTraded(TradeAction.PlayerBuys, this.burglars.RandomElement(), this.settlement);
                                        if (t is Pawn pawnoff)
                                        {
                                            this.caravan.AddPawn(pawnoff, true);
                                        } else {
                                            CaravanInventoryUtility.GiveThing(this.caravan, t);
                                        }
                                        alertRaise += t.MarketValue * br.Props.alertGainPerMarketValueStolen;
                                    }
                                    LookTargets toLook = new LookTargets(this.caravan);
                                    ChoiceLetter winLetter = LetterMaker.MakeLetter("Hauts_PilferLetter2".Translate(), message, LetterDefOf.PositiveEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(winLetter, null);
                                }
                            } else {
                                int lostGoodwill = -1 * (int)((this.burglaryMaxValue - this.valueRemaining) / 40f);
                                TaggedString message = "Hauts_BurgleOutcome3".Translate(this.settlement.Faction, lostGoodwill);
                                LookTargets toLook = new LookTargets(this.caravan);
                                ChoiceLetter sadLetter = LetterMaker.MakeLetter("Hauts_PilferLetter3".Translate(), message, LetterDefOf.NegativeEvent, toLook, null, null, null);
                                Find.LetterStack.ReceiveLetter(sadLetter, null);
                                this.caravan.Faction.TryAffectGoodwillWith(this.settlement.Faction, lostGoodwill);
                            }
                            br.currentAlertLevel += Math.Max(minAlertRaise, alertRaise);
                        }
                    }
                    this.Close(true);
                    this.thingsStolen.Clear();
                    this.targetedThingCategories.Clear();
                    this.burglars.Clear();
                    this.categories.Clear();
                    this.goodsList.Clear();
                } else {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            return AcceptanceReport.WasAccepted;
        }
        private Caravan caravan;
        private List<Pawn> burglars = new List<Pawn>();
        private Settlement settlement;
        private float scrollHeight;
        private float burglaryMaxWeight;
        private float burglaryMaxValue;
        private float successChance;
        private float weightRemaining;
        private float valueRemaining;
        private List<ThingCategoryDef> targetedThingCategories = new List<ThingCategoryDef>();
        private List<Thing> thingsStolen = new List<Thing>();
        List<ThingCategoryDef> categories = new List<ThingCategoryDef>();
        List<Thing> goodsList = new List<Thing>();
        private Vector2 scrollPosition;
    }
    public class FloatMenuOptionProvider_Pickpocket : FloatMenuOptionProvider
    {
        protected override bool Drafted
        {
            get
            {
                return true;
            }
        }
        protected override bool Undrafted
        {
            get
            {
                return true;
            }
        }
        protected override bool Multiselect
        {
            get
            {
                return false;
            }
        }
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (clickedPawn == null || clickedPawn.inventory == null || clickedPawn.Faction == Faction.OfPlayerSilentFail || clickedPawn.inventory.FirstUnloadableThing == default(ThingCount) || clickedPawn.InAggroMentalState || clickedPawn.HostileTo(context.FirstSelectedPawn) || context.FirstSelectedPawn.HomeFaction == clickedPawn.Faction || context.FirstSelectedPawn.GetStatValue(HautsDefOf.Hauts_PilferingStealth) <= float.Epsilon)
            {
                yield break;
            }
            if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                yield return new FloatMenuOption("Hauts_PilfererErrorPrefix".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = delegate
            {
                Job job = JobMaker.MakeJob(HautsDefOf.Hauts_Pickpocket, clickedPawn);
                job.playerForced = true;
                context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            };
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Hauts_PickpocketLabel".Translate(clickedPawn.LabelShort + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " "+ clickedPawn.GetStatValue(HautsDefOf.Hauts_PawnAlertLevel).ToStringByStyle(ToStringStyle.FloatOne) + ")"), action, MenuOptionPriority.InitiateSocial, null, clickedPawn, 0f, null, null, true, 0), context.FirstSelectedPawn, clickedPawn, "ReservedBy", null);
            yield break;
        }
    }
    public class JobDriver_Pickpocket : JobDriver
    {
        private Pawn Victim
        {
            get
            {
                return (Pawn)base.TargetThingA;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Victim, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Pawn victim = this.Victim;
            if (victim != null)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOn(() => !this.CanPickpocket(victim, this.pawn));
                Toil trade = ToilMaker.MakeToil("MakeNewToils");
                trade.initAction = delegate
                {
                    Pawn actor = trade.actor;
                    if (this.CanPickpocket(victim,actor))
                    {
                        HautsUtility.AdjustPickpocketSensitiveHediffs(actor);
                        float burglaryMaxWeight = actor.GetStatValue(StatDefOf.CarryingCapacity);
                        float burglaryMaxValue = actor.GetStatValue(HautsDefOf.Hauts_MaxPilferingValue);
                        float successChance = actor.GetStatValue(HautsDefOf.Hauts_PilferingStealth);
                        float alertLevel = victim.Downed?0f:victim.GetStatValue(HautsDefOf.Hauts_PawnAlertLevel);
                        bool thingsStolen = false;
                        successChance -= alertLevel;
                        if (burglaryMaxWeight <= 0f)
                        {
                            TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererNoCarryCap".Translate();
                            Messages.Message(message, actor, MessageTypeDefOf.RejectInput, true);
                            return;
                        } else if (burglaryMaxValue <= 0f) {
                            TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooWeak".Translate();
                            Messages.Message(message, actor, MessageTypeDefOf.RejectInput, true);
                            return;
                        } else if (successChance <= 0f) {
                            TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooConspicuous".Translate();
                            Messages.Message(message, actor, MessageTypeDefOf.RejectInput, true);
                            return;
                        } else {
                            float alertRaise = 0f, minAlertRaise = 0f;
                            minAlertRaise += HautsDefOf.Hauts_RaisedAlertLevel.initialSeverity;
                            if (Rand.Value <= successChance)
                            {
                                while (burglaryMaxWeight > 0f && burglaryMaxValue > 0f && victim.inventory.innerContainer.Count > 0)
                                {
                                    int triesRemaining = 30;
                                    while (triesRemaining > 0 && victim.inventory.innerContainer.Count > 0)
                                    {
                                        triesRemaining--;
                                        Thing toSteal = victim.inventory.innerContainer.Where((Thing t) => t.MarketValue < burglaryMaxValue).RandomElement();
                                        if (toSteal != null)
                                        {
                                            int mostYouCouldGetValue = (int)Math.Floor(burglaryMaxValue / toSteal.MarketValue);
                                            int mostYouCouldGetCount = (int)Math.Min(actor.carryTracker.AvailableStackSpace(toSteal.def), Math.Min(toSteal.def.stackLimit, toSteal.stackCount));
                                            int trueLowest = Math.Min(mostYouCouldGetValue, mostYouCouldGetCount);
                                            float stackMarketValue = trueLowest * toSteal.MarketValue;
                                            if (stackMarketValue <= burglaryMaxValue && trueLowest > 0 && trueLowest <= burglaryMaxWeight)
                                            {
                                                burglaryMaxValue -= stackMarketValue;
                                                burglaryMaxWeight -= trueLowest;
                                                if (toSteal.stackCount > toSteal.def.stackLimit)
                                                {
                                                    victim.inventory.TryAddAndUnforbid(toSteal.SplitOff(toSteal.stackCount - toSteal.def.stackLimit));
                                                }
                                                victim.inventory.RemoveCount(toSteal.def, toSteal.stackCount, false);
                                                actor.inventory.TryAddAndUnforbid(toSteal);
                                                thingsStolen = true;
                                                alertRaise += toSteal.MarketValue * trueLowest * 0.02f;
                                                break;
                                            }
                                        }
                                    }
                                    if (triesRemaining <= 0)
                                    {
                                        break;
                                    }
                                }
                                if (!thingsStolen)
                                {
                                    TaggedString message = "Hauts_PickpocketOutcome1".Translate(actor.Name.ToStringShort);
                                    LookTargets toLook = new LookTargets(actor);
                                    ChoiceLetter tieLetter = LetterMaker.MakeLetter("Hauts_PilferLetter1".Translate(), message, LetterDefOf.NeutralEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(tieLetter, null);
                                    HautsUtility.IncreaseAlertLevel(victim,minAlertRaise);
                                } else {
                                    TaggedString message = "Hauts_PickpocketOutcome2".Translate(actor.Name.ToStringShort, victim.Faction.NameColored);
                                    LookTargets toLook = new LookTargets(actor);
                                    ChoiceLetter winLetter = LetterMaker.MakeLetter("Hauts_PilferLetter2".Translate(), message, LetterDefOf.PositiveEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(winLetter, null);
                                    actor.Faction.TryAffectGoodwillWith(victim.Faction, -5);
                                    float raiseAlertBy = Math.Max(alertRaise, minAlertRaise);
                                    if (victim.Faction != null)
                                    {
                                        foreach (Pawn p in victim.MapHeld.mapPawns.PawnsInFaction(victim.Faction))
                                        {
                                            HautsUtility.IncreaseAlertLevel(p, raiseAlertBy);
                                        }
                                    } else {
                                        HautsUtility.IncreaseAlertLevel(victim,raiseAlertBy);
                                    }
                                }
                            } else {
                                float raiseAlertBy = Math.Max(alertRaise,minAlertRaise);
                                if (victim.Faction != null)
                                {
                                    TaggedString message = "Hauts_PickpocketOutcome3".Translate(actor.Name.ToStringShort,victim.Faction.NameColored);
                                    LookTargets toLook = new LookTargets(actor);
                                    ChoiceLetter sadLetter = LetterMaker.MakeLetter("Hauts_PilferLetter3".Translate(), message, LetterDefOf.NegativeEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(sadLetter, null);
                                    actor.Faction.TryAffectGoodwillWith(victim.Faction, actor.IsPsychologicallyInvisible()?-5:-15);
                                    /*if (victim.lord != null)
                                    {
                                        Pawn trader = TraderCaravanUtility.FindTrader(victim.lord);
                                        if (trader != null)
                                        {
                                            trader.mindState.traderDismissed = true;
                                        }
                                    }*/
                                    foreach (Pawn p in victim.MapHeld.mapPawns.PawnsInFaction(victim.Faction))
                                    {
                                        HautsUtility.IncreaseAlertLevel(p, raiseAlertBy);
                                    }
                                } else {
                                    TaggedString message = "Hauts_PickpocketOutcome3_Factionless".Translate(actor.Name.ToStringShort, victim.Name.ToStringShort);
                                    LookTargets toLook = new LookTargets(actor);
                                    ChoiceLetter sadLetter = LetterMaker.MakeLetter("Hauts_PilferLetter3".Translate(), message, LetterDefOf.NegativeEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(sadLetter, null);
                                    if (victim.InMentalState)
                                    {
                                        victim.MentalState.RecoverFromState();
                                    }
                                    victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, false, true, false, null, false, false, false);
                                    HautsUtility.IncreaseAlertLevel(victim, raiseAlertBy);
                                }
                            }
                        }
                    }
                };
                yield return trade;
            }
            yield break;
        }
        public bool CanPickpocket(Pawn victim, Pawn thief)
        {
            return victim.Faction != Faction.OfPlayerSilentFail && victim.inventory != null && victim.inventory.FirstUnloadableThing != default(ThingCount) && !victim.InAggroMentalState && thief.CanReach(victim, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn) && thief.HomeFaction != victim.Faction && thief.GetStatValue(HautsDefOf.Hauts_PilferingStealth) > float.Epsilon;
        }
    }
    //event pools
    public class BelongsToEventPool : DefModExtension
    {
        public BelongsToEventPool()
        {
        }
        public bool good = false;
        public bool bad = false;
    }
    //toolkit
    public static class HautsUtility
    {
        //cooldown manipulation
        public static bool ShouldLowerCooldown (RimWorld.Ability ability, HediffComp_AbilityCooldownModifier acm)
        {
            return false;
        }
        public static bool ShouldLowerCooldown(VEF.Abilities.Ability ability, HediffComp_AbilityCooldownModifier acm)
        {
            return false;
        }
        public static float GetCooldownModifier (VEF.Abilities.Ability ability)
        {
            float netACM = 1f;
            foreach (Hediff h in ability.pawn.health.hediffSet.hediffs)
            {
                if (h is HediffWithComps hwc)
                {
                    foreach (HediffComp hc in hwc.comps)
                    {
                        if (hc is HediffComp_AbilityCooldownModifier acm)
                        {
                            bool shouldLowerCooldown = acm.Props.affectsAllAbilities;
                            if (acm.Props.affectedVEFAbilities.Contains(ability.def))
                            {
                                shouldLowerCooldown = true;
                            } else if (acm.Props.affectedDMEs != null && acm.Props.affectedDMEs.Count > 0 && ability.def.modExtensions != null) {
                                foreach (DefModExtension dme in acm.Props.affectedDMEs)
                                {
                                    foreach (DefModExtension dme2 in ability.def.modExtensions)
                                    {
                                        if (dme2.GetType() == dme.GetType())
                                        {
                                            shouldLowerCooldown = true;
                                            break;
                                        }
                                    }
                                    if (shouldLowerCooldown)
                                    {
                                        break;
                                    }
                                }
                            } else if (acm.Props.abilitiesUsingThisWorkTag != 0 && acm.Props.abilitiesUsingThisWorkTag.GetAllSelectedItems<WorkTags>().Contains(WorkTags.Social)) {
                                for (int i = 0; i < ability.AbilityModExtensions.Count; i++)
                                {
                                    if (ability.AbilityModExtensions[i] is AbilityExtension_SocialInteraction)
                                    {
                                        shouldLowerCooldown = true;
                                        break;
                                    }
                                }
                            }
                            if (!shouldLowerCooldown && HautsUtility.ShouldLowerCooldown(ability,acm)) {
                                shouldLowerCooldown = true;
                            }
                            if (shouldLowerCooldown)
                            {
                                netACM += acm.Props.increasedCooldownRecovery * (acm.Props.multiplyBySeverity ? hwc.Severity : 1f) * (acm.Props.multiplyByStat != null ? ability.pawn.GetStatValue(acm.Props.multiplyByStat): 1f);
                                break;
                            }
                        }
                    }
                }
            }
            return Math.Max(netACM,0.001f);
        }
        public static float GetCooldownModifier (RimWorld.Ability ability)
        {
            foreach (CompAbilityEffect acomp in ability.EffectComps)
            {
                if (acomp is CompAbilityEffect_CooldownStatScaling cdSS)
                {
                    if (ability.pawn.GetStatValue(cdSS.Props.stat) > 0f)
                    {
                        ability.StartCooldown((int)(ability.CooldownTicksRemaining / ability.pawn.GetStatValue(cdSS.Props.stat)));
                    }
                }
            }
            float netACM = 1f;
            List<WorkTags> workTags = new List<WorkTags>();
            if (ability.def.groupDef != null)
            {
                CooldownModifier_WorkTags cmwt = ability.def.groupDef.GetModExtension<CooldownModifier_WorkTags>();
                if (cmwt != null)
                {
                    workTags.Add(cmwt.affectedByAnyACMwithThisWorkTag);
                }
            }
            CooldownModifier_WorkTags cmwt2 = ability.def.GetModExtension<CooldownModifier_WorkTags>();
            if (cmwt2 != null)
            {
                workTags.Add(cmwt2.affectedByAnyACMwithThisWorkTag);
            }
            foreach (Hediff h in ability.pawn.health.hediffSet.hediffs)
            {
                if (h is HediffWithComps hwc)
                {
                    foreach (HediffComp hc in hwc.comps)
                    {
                        if (hc is HediffComp_AbilityCooldownModifier acm)
                        {
                            bool shouldLowerCooldown = acm.Props.affectsAllAbilities;
                            if (acm.Props.affectedAbilities != null && acm.Props.affectedAbilities.Contains(ability.def))
                            {
                                shouldLowerCooldown = true;
                            } else if (acm.Props.affectedDMEs != null && acm.Props.affectedDMEs.Count > 0 && ability.def.modExtensions != null) {
                                foreach (DefModExtension dme in acm.Props.affectedDMEs)
                                {
                                    foreach (DefModExtension dme2 in ability.def.modExtensions)
                                    {
                                        if (dme2.GetType() == dme.GetType())
                                        {
                                            shouldLowerCooldown = true;
                                            break;
                                        }
                                    }
                                    if (shouldLowerCooldown)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (!shouldLowerCooldown && acm.Props.abilitiesUsingThisWorkTag != 0) {
                                if (!ability.comps.NullOrEmpty<AbilityComp>())
                                {
                                    for (int i = 0; i < ability.comps.Count; i++)
                                    {
                                        if (ability.comps[i] is CompAbilityEffect_MustBeCapableOf mbco)
                                        {
                                            foreach (WorkTags wt in mbco.Props.workTags.GetAllSelectedItems<WorkTags>())
                                            {
                                                if (!workTags.Contains(wt))
                                                {
                                                    workTags.Add(wt);
                                                }
                                            }
                                        } else if (ability.comps[i] is CompAbilityEffect_SocialInteraction || ability.comps[i] is CompAbilityEffect_StartRitual) {
                                            workTags.Add(WorkTags.Social);
                                        } else if (ability.comps[i] is CompAbilityEffect_StopManhunter) {
                                            workTags.Add(WorkTags.Animals);
                                        }
                                    }
                                }
                                if (!workTags.NullOrEmpty())
                                {
                                    foreach (WorkTags wt in workTags)
                                    {
                                        if (acm.Props.abilitiesUsingThisWorkTag.GetAllSelectedItems<WorkTags>().Contains(wt))
                                        {
                                            shouldLowerCooldown = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!shouldLowerCooldown && acm.Props.affectsAllBionicAbilities) {
                                foreach (Hediff h2 in ability.pawn.health.hediffSet.hediffs)
                                {
                                    bool breakout = false;
                                    if (h2.def.countsAsAddedPartOrImplant)
                                    {
                                        if (h2.def.abilities != null && h2.def.abilities.Contains(ability.def))
                                        {
                                            shouldLowerCooldown = true;
                                        } else if (h2 is HediffWithComps hwc2 && hwc2.def.comps != null) {
                                            foreach (HediffCompProperties hcp in hwc2.def.comps)
                                            {
                                                if (hcp is HediffCompProperties_GiveAbility ga)
                                                {
                                                    if ((ga.abilityDef != null && ga.abilityDef == ability.def) || (ga.abilityDefs != null && ga.abilityDefs.Contains(ability.def)))
                                                    {
                                                        shouldLowerCooldown = true;
                                                        breakout = true;
                                                        break;
                                                    }
                                                } else if (hcp is VEF.AnimalBehaviours.HediffCompProperties_Ability abhcpa) {
                                                    if (abhcpa.ability != null && abhcpa.ability == ability.def)
                                                    {
                                                        shouldLowerCooldown = true;
                                                        breakout = true;
                                                        break;
                                                    }
                                                } else if (HautsUtility.AthenaAbilityCooldownPatch(ability, hcp)) {
                                                    shouldLowerCooldown = true;
                                                    breakout = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    if (breakout)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (!shouldLowerCooldown && acm.Props.affectsAllGeneticAbilities && ModsConfig.BiotechActive && ability.pawn.genes != null)
                            {
                                foreach (Gene g in ability.pawn.genes.GenesListForReading)
                                {
                                    if (g.def.abilities != null && g.def.abilities.Contains(ability.def))
                                    {
                                        shouldLowerCooldown = true;
                                        break;
                                    }
                                }
                            }
                            if (!shouldLowerCooldown && HautsUtility.ShouldLowerCooldown(ability,acm)) {
                                shouldLowerCooldown = true;
                            }
                            if (shouldLowerCooldown)
                            {
                                netACM += acm.Props.increasedCooldownRecovery * (acm.Props.multiplyBySeverity ? hwc.Severity : 1f) * (acm.Props.multiplyByStat != null ? ability.pawn.GetStatValue(acm.Props.multiplyByStat) : 1f);
                                break;
                            }
                        }
                    }
                }
            }
            return netACM;
        }
        public static void SetNewCooldown(RimWorld.Ability ability, int newCooldown, bool cantGoAboveMaxCD = true, bool affectsWholeAbilityGroup = true)
        {
            if (ability.def.groupDef != null)
            {
                foreach (RimWorld.Ability ab in ability.pawn.abilities.AllAbilitiesForReading)
                {
                    if (ab.def.groupDef != null && ab.def.groupDef == ability.def.groupDef)
                    {
                        HautsUtility.SetNewCooldownInner(ab, cantGoAboveMaxCD ? Math.Min(newCooldown, ability.def.groupDef.cooldownTicks) : newCooldown);
                    }
                }
            } else {
                HautsUtility.SetNewCooldownInner(ability, cantGoAboveMaxCD ? Math.Min(newCooldown, ability.def.cooldownTicksRange.max) : newCooldown);
            }
        }
        private static void SetNewCooldownInner(RimWorld.Ability ability, int newCooldown)
        {
            if (ability is Psycast)
            {
                ability.StartCooldown(newCooldown);
            } else {
                if (ability.GetType().GetField("cooldownDuration", BindingFlags.NonPublic | BindingFlags.Instance) != null && ability.GetType().GetField("cooldownEndTick", BindingFlags.NonPublic | BindingFlags.Instance) != null)
                {
                    if (newCooldown > ability.CooldownTicksTotal)
                    {
                        ability.GetType().GetField("cooldownDuration", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ability, Math.Max(newCooldown, 0));
                    }
                    ability.GetType().GetField("cooldownEndTick", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ability, GenTicks.TicksGame + Math.Max(newCooldown, 0));
                }
            }
        }
        public static bool AthenaAbilityCooldownPatch(RimWorld.Ability ability, HediffCompProperties hcp)
        {
            return false;
        }
        //resurrection and healing
        public static void StartDelayedResurrection(Pawn pawn, IntRange initialRareTicks, string explanationKey, bool shouldSendMessage = true, bool shouldTranslateMessage = true, bool preventRisingAsShambler = true, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            if (pawn.Corpse != null)
            {
                WorldComponent_HautsDelayedResurrections WCDR = (WorldComponent_HautsDelayedResurrections)Find.World.GetComponent(typeof(WorldComponent_HautsDelayedResurrections));
                WCDR.StartDelayedResurrection(pawn.Corpse, initialRareTicks, explanationKey, shouldSendMessage, shouldTranslateMessage, preventRisingAsShambler, mutation, mutationSeverity);
            }
        }
        public static void StatScalingHeal(float baseHeal, StatDef statDef, Pawn toHeal, Pawn whoseStatMatters)
        {
            List<Hediff_Injury> source = new List<Hediff_Injury>();
            toHeal.health.hediffSet.GetHediffs<Hediff_Injury>(ref source, (Hediff_Injury x) => x.CanHealNaturally() || x.CanHealFromTending());
            Hediff_Injury hediff_Injury;
            if (source.TryRandomElement(out hediff_Injury))
            {
                hediff_Injury.Heal(baseHeal*whoseStatMatters.GetStatValue(statDef));
            }
        }
        //trait granted stuff management
        public static void TraitGrantedStuffLoadCheck(List<Pawn> pawns)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && pawns[i].story.traits != null)
                {
                    TraitGrantedStuffRegeneration(pawns[i]);
                }
            }
        }
        public static void TraitGrantedStuffRegeneration(Pawn p)
        {
            List<RimWorld.AbilityDef> abilitiesToRemove = new List<RimWorld.AbilityDef>();
            if (p.abilities != null)
            {
                foreach (RimWorld.Ability a in p.abilities.abilities)
                {
                    foreach (CompAbilityEffect comp in a.EffectComps)
                    {
                        CompAbilityEffect_ForcedByOtherProperty cRTG = comp as CompAbilityEffect_ForcedByOtherProperty;
                        if (cRTG != null && cRTG.Props.requiresAForcingProperty)
                        {
                            bool shouldDelete = true;
                            if (cRTG.Props.forcingTraits != null && cRTG.Props.forcingTraits.Count > 0 && p.story != null)
                            {
                                foreach (TraitDef td in cRTG.Props.forcingTraits)
                                {
                                    if (p.story.traits.HasTrait(td))
                                    {
                                        shouldDelete = false;
                                        break;
                                    }
                                }
                            } else if (cRTG.Props.forcingGenes != null && cRTG.Props.forcingGenes.Count > 0 && ModsConfig.BiotechActive && p.genes != null) {
                                foreach (GeneDef gd in cRTG.Props.forcingGenes)
                                {
                                    if (HautsUtility.AnalogHasActiveGene(p.genes, gd))
                                    {
                                        shouldDelete = false;
                                        break;
                                    }
                                }
                            }
                            if (shouldDelete)
                            {
                                abilitiesToRemove.Add(a.def);
                            }
                            break;
                        }
                    }
                }
                foreach (RimWorld.AbilityDef ad in abilitiesToRemove)
                {
                    p.abilities.RemoveAbility(ad);
                }
            }
            List<Hediff> hediffsToRemove = new List<Hediff>();
            if (p.health.hediffSet.hediffs != null)
            {
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    HediffComp_ForcedByOtherProperty comp = h.TryGetComp<HediffComp_ForcedByOtherProperty>();
                    if (comp != null && comp.Props.requiresAForcingProperty)
                    {
                        bool shouldDelete = true;
                        if (comp.Props.forcingTraits != null && comp.Props.forcingTraits.Count > 0 && p.story != null)
                        {
                            foreach (TraitDef td in comp.Props.forcingTraits)
                            {
                                if (p.story.traits.HasTrait(td))
                                {
                                    shouldDelete = false;
                                    break;
                                }
                            }
                        } else if (ModsConfig.BiotechActive && comp.Props.forcingGenes != null && comp.Props.forcingGenes.Count > 0 && p.genes != null) {
                            foreach (GeneDef gd in comp.Props.forcingGenes)
                            {
                                if (HautsUtility.AnalogHasActiveGene(p.genes, gd))
                                {
                                    shouldDelete = false;
                                    break;
                                }
                            }
                        }
                        if (shouldDelete)
                        {
                            hediffsToRemove.Add(h);
                        }
                    }
                }
                foreach (Hediff h in hediffsToRemove)
                {
                    p.health.RemoveHediff(h);
                }
            }
            foreach (Trait t in p.story.traits.allTraits)
            {
                if (t.def.HasModExtension<TraitGrantedStuff>())
                {
                    bool shouldAddHediffs = true;
                    if (t.def.GetModExtension<TraitGrantedStuff>().grantedHediffs != null)
                    {
                        foreach (HediffDef h in t.def.GetModExtension<TraitGrantedStuff>().grantedHediffs.TryGetValue(t.Degree))
                        {
                            if (p.health.hediffSet.HasHediff(h))
                            {
                                shouldAddHediffs = false;
                            }
                        }
                        if (shouldAddHediffs && t.def.GetModExtension<TraitGrantedStuff>().otherHediffsToRemoveOnRemoval != null)
                        {
                            foreach (HediffDef h in t.def.GetModExtension<TraitGrantedStuff>().otherHediffsToRemoveOnRemoval.TryGetValue(t.Degree))
                            {
                                if (p.health.hediffSet.HasHediff(h))
                                {
                                    shouldAddHediffs = false;
                                }
                            }
                        }
                    }
                    HautsUtility.AddTraitGrantedStuff(shouldAddHediffs,t,p);
                }
            }
            CharacterEditorCompat(p);
        }
        public static void CharacterEditorCompat(Pawn p)
        {

        }
        public static void AddTraitGrantedStuff(bool shouldAddHediffs, Trait t, Pawn pawn)
        {
            TraitGrantedStuff tgs = t.def.GetModExtension<TraitGrantedStuff>();
            if (tgs != null)
            {
                if (shouldAddHediffs)
                {
                    HautsUtility.AddHediffsFromTrait(t, tgs, pawn);
                }
                HautsUtility.AddAbilitiesFromTrait(t, tgs, pawn);
                if (pawn.GuestStatus == GuestStatus.Prisoner && tgs.prisonerResolveFactor != null)
                {
                    pawn.guest.resistance *= tgs.prisonerResolveFactor.TryGetValue(t.Degree);
                    pawn.guest.will *= tgs.prisonerResolveFactor.TryGetValue(t.Degree);
                }
                if ((pawn.def == ThingDefOf.Human || (ModsConfig.AnomalyActive && pawn.def == ThingDefOf.CreepJoiner)) && pawn.story.bodyType != null && tgs.forcedBodyTypes != null)
                {
                    if (tgs.forcedBodyTypes.Keys.Contains(pawn.story.bodyType))
                    {
                        pawn.story.bodyType = tgs.forcedBodyTypes.TryGetValue(pawn.story.bodyType);
                        pawn.Drawer.renderer.SetAllGraphicsDirty();
                    }
                }
            }
        }
        public static void AddHediffsFromTrait(Trait t, TraitGrantedStuff tgs, Pawn pawn)
        {
            if (tgs.grantedHediffs != null)
            {
                if (tgs.grantedHediffs.ContainsKey(t.Degree))
                {
                    for (int i = 0; i < tgs.grantedHediffs.TryGetValue(t.Degree).Count; i++)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(tgs.grantedHediffs.TryGetValue(t.Degree)[i], pawn, null);
                        if (tgs.hediffsToBrain)
                        {
                            pawn.health.AddHediff(hediff, pawn.health.hediffSet.GetBrain(), null, null);
                        } else {
                            pawn.health.AddHediff(hediff, null, null, null);
                        }
                    }
                }
            }
        }
        public static void AddAbilitiesFromTrait(Trait t, TraitGrantedStuff tgs, Pawn pawn)
        {
            if (tgs.grantedAbilities != null)
            {
                if (tgs.grantedAbilities.ContainsKey(t.Degree))
                {
                    for (int i = 0; i < tgs.grantedAbilities.TryGetValue(t.Degree).Count; i++)
                    {
                        pawn.abilities.GainAbility(tgs.grantedAbilities.TryGetValue(t.Degree)[i]);
                    }
                }
            }
            VEF.Abilities.CompAbilities comp = pawn.GetComp<VEF.Abilities.CompAbilities>();
            if (tgs.grantedVEFAbilities != null && comp != null)
            {
                if (tgs.grantedVEFAbilities.ContainsKey(t.Degree)) {
                    for (int i = 0; i < tgs.grantedVEFAbilities.TryGetValue(t.Degree).Count; i++)
                    {
                        comp.GiveAbility(tgs.grantedVEFAbilities.TryGetValue(t.Degree)[i]);
                    }
                }
            }
        }
        public static void RemoveTraitGrantedStuff(Trait t, Pawn pawn)
        {
            VanishOnDeath vod = t.def.GetModExtension<VanishOnDeath>();
            if (vod != null && vod.triggerOnRemoval)
            {
                HautsUtility.VanishPawnInner(pawn, vod.thingLeftBehind, vod.sound, vod.skipgateOut);
                return;
            }
            TraitGrantedStuff tgs = t.def.GetModExtension<TraitGrantedStuff>();
            if (tgs != null)
            {
                HautsUtility.RemoveHediffsFromTrait(t, tgs, pawn);
                HautsUtility.RemoveAbilitiesFromTrait(t, tgs, pawn);
                if (pawn.GuestStatus == GuestStatus.Prisoner && tgs.prisonerResolveFactor != null)
                {
                    pawn.guest.resistance /= tgs.prisonerResolveFactor.TryGetValue(t.Degree);
                    pawn.guest.will /= tgs.prisonerResolveFactor.TryGetValue(t.Degree);
                }
            }
        }
        public static void RemoveHediffsFromTrait(Trait t, TraitGrantedStuff tgs, Pawn pawn)
        {
            if (tgs.grantedHediffs != null)
            {
                if (tgs.grantedHediffs.ContainsKey(t.Degree))
                {
                    for (int i = 0; i < tgs.grantedHediffs.TryGetValue(t.Degree).Count; i++)
                    {
                        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(tgs.grantedHediffs.TryGetValue(t.Degree)[i], false);
                        if (hediff != null)
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
                }
            }
            if (tgs.otherHediffsToRemoveOnRemoval != null)
            {
                if (tgs.otherHediffsToRemoveOnRemoval.ContainsKey(t.Degree))
                {
                    for (int i = 0; i < tgs.otherHediffsToRemoveOnRemoval.Count; i++)
                    {
                        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(tgs.otherHediffsToRemoveOnRemoval.TryGetValue(t.Degree)[i], false);
                        if (hediff != null)
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
                }
            }
        }
        public static void RemoveAbilitiesFromTrait(Trait t, TraitGrantedStuff tgs, Pawn pawn)
        {
            if (tgs.grantedAbilities != null)
            {
                if (tgs.grantedAbilities.ContainsKey(t.Degree))
                {
                    for (int i = 0; i < tgs.grantedAbilities.TryGetValue(t.Degree).Count; i++)
                    {
                        pawn.abilities.RemoveAbility(tgs.grantedAbilities.TryGetValue(t.Degree)[i]);
                    }
                }
            }
            VEF.Abilities.CompAbilities comp2 = pawn.GetComp<VEF.Abilities.CompAbilities>();
            if (tgs.grantedVEFAbilities != null && comp2 != null)
            {
                if (tgs.grantedVEFAbilities.ContainsKey(t.Degree))
                {
                    List<VEF.Abilities.Ability> learnedAbilities = HautsFramework.GetInstanceField(typeof(VEF.Abilities.CompAbilities), comp2, "learnedAbilities") as List<VEF.Abilities.Ability>;
                    for (int i = 0; i < tgs.grantedVEFAbilities.TryGetValue(t.Degree).Count; i++)
                    {
                        VEF.Abilities.Ability ability = learnedAbilities.FirstOrDefault((VEF.Abilities.Ability x) => x.def == tgs.grantedVEFAbilities.TryGetValue(t.Degree)[i]);
                        if (ability != null)
                        {
                            learnedAbilities.Remove(ability);
                        }
                    }
                }
            }
        }
        public static bool TryVanishPawn(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.TraitsSorted)
                {
                    VanishOnDeath vod = t.def.GetModExtension<VanishOnDeath>();
                    if (vod != null)
                    {
                        HautsUtility.VanishPawnInner(pawn, vod.thingLeftBehind, vod.sound, vod.skipgateOut);
                        return true;
                    }
                }
            }
            return false;
        }
        public static void VanishPawnInner(Pawn pawn, ThingDef thingLeftBehind, SoundDef sound, bool skipgateOut)
        {
            if (pawn.Spawned)
            {
                Map map = pawn.Map;
                if (sound != null)
                {
                    sound.PlayOneShot(new TargetInfo(pawn.Position, map, false));
                } else if (skipgateOut && ModsConfig.RoyaltyActive) {
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, map, false));
                }
                if (thingLeftBehind != null)
                {
                    Thing thing = ThingMaker.MakeThing(thingLeftBehind);
                    GenPlace.TryPlaceThing(thing, pawn.Position, map, ThingPlaceMode.Near, null, null, default);
                }
                if (skipgateOut)
                {
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(pawn.Position.ToVector3Shifted(), map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    map.flecks.CreateFleck(dataStatic);
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(pawn.Position.ToVector3Shifted(), map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    map.flecks.CreateFleck(dataStatic2);
                    pawn.teleporting = true;
                }
            }
            pawn.ExitMap(false, Rot4.Invalid);
            pawn.Destroy(DestroyMode.Vanish);
        }
        //ability range mod tools
        public static bool IsSkipAbility(RimWorld.AbilityDef ability)
        {
            return ModsConfig.RoyaltyActive && ability.category == DefDatabase<AbilityCategoryDef>.GetNamed("Skip");
        }
        public static bool IsSpewAbility(RimWorld.AbilityDef ability)
        {
            return ability.HasModExtension<Hauts_SpewAbility>();
        }
        public static bool IsLeapVerb(Verb verb)
        {
            return verb is Verb_CastAbilityJump || verb is Verb_Jump;
        }
        public static void DrawBoostedAbilityRange(RimWorld.Verb_CastAbility ability, LocalTargetInfo target, StatDef stat)
        {

            if (ability.verbProps.range > 0f && Find.CurrentMap != null && !ability.verbProps.IsMeleeAttack && ability.verbProps.targetable)
            {
                VerbProperties verbProps = ability.verbProps;
                float num = verbProps.EffectiveMinRange(true);
                if (num > 0f && num < GenRadial.MaxRadialPatternRadius)
                {
                    GenDraw.DrawRadiusRing(ability.caster.Position, num);
                }
                float newRange = verbProps.range * ability.CasterPawn.GetStatValue(stat);
                if (newRange < (float)(Find.CurrentMap.Size.x + Find.CurrentMap.Size.z) && newRange < GenRadial.MaxRadialPatternRadius)
                {
                    Func<IntVec3, bool> predicate = null;
                    if (verbProps.drawHighlightWithLineOfSight)
                    {
                        predicate = (IntVec3 c) => GenSight.LineOfSight(ability.caster.Position, c, Find.CurrentMap);
                    }
                    GenDraw.DrawRadiusRing(ability.caster.Position, newRange, Color.white, predicate);
                }
            }
            if (ability.CanHitTarget(target) && ability.IsApplicableTo(target, false))
            {
                if (ability.ability.def.HasAreaOfEffect)
                {
                    if (target.IsValid)
                    {
                        GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
                        GenDraw.DrawRadiusRing(target.Cell, ability.ability.def.EffectRadius, RimWorld.Verb_CastAbility.RadiusHighlightColor, null);
                    }
                } else {
                    GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
                }
            }
            if (target.IsValid)
            {
                ability.ability.DrawEffectPreviews(target);
            }
        }
        //extra on hit effects
        public static void DoExtraOnHitEffects(HediffWithComps hediff, Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (!thing.DestroyedOrNull())
            {
                Pawn attacker = hediff.pawn;
                HediffDef hd = dinfo.WeaponLinkedHediff;
                if (hd != null)
                {
                    foreach (HediffCompProperties hcp in hd.comps)
                    {
                        if (hcp is HediffCompProperties_ExtraOnHitEffects)
                        {
                            return;
                        }
                    }
                }
                foreach (HediffComp hc in hediff.comps)
                {
                    if (hc is HediffComp_ExtraOnHitEffects hoH && hoH != null && hoH.Props.appliedViaAttacks && hoH.cooldown <= Find.TickManager.TicksGame && result.totalDamageDealt >= hoH.Props.minDmgToTrigger && hoH.RangeCheck(thing,dinfo))
                    {
                        if (thing is Pawn p)
                        {
                            if (hoH.CanAffectTarget(p))
                            {
                                hoH.DoExtraEffects(p, hoH.Props.damageScaling ? result.totalDamageDealt : 1f, dinfo.HitPart);
                                if (hoH.Props.triggersPyroThought && attacker.story != null && attacker.story.traits.HasTrait(TraitDefOf.Pyromaniac) && !p.Downed && !p.IsPsychologicallyInvisible() && !p.Fogged() && (attacker.Faction == null || p.HostileTo(attacker.Faction)))
                                {
                                    Pawn_NeedsTracker pnt = attacker.needs;
                                    if (pnt != null && pnt.mood != null && pnt.mood.thoughts != null && pnt.mood.thoughts.memories != null)
                                    {
                                        pnt.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.PyroUsed, null, null);
                                    }
                                }
                            }
                        } else if (hoH.CanAffectTargetThing(thing)) {
                            hoH.DoExtraEffectsThing(thing, hoH.Props.damageScaling ? result.totalDamageDealt : 1f);
                        }
                        hoH.cooldown = Find.TickManager.TicksGame + hoH.Props.tickCooldown.RandomInRange;
                    }
                }
            }
        }
        //damage factor group defs
        public static void ApplyAllDamageFactorGroupDefs()
        {
            foreach (DamageFactorGroupDef dfg in DefDatabase<DamageFactorGroupDef>.AllDefs)
            {
                if (!dfg.applyToHediffs.NullOrEmpty() && !dfg.damageDefs.NullOrEmpty())
                {
                    foreach (DFG_HediffTarget ht in dfg.applyToHediffs)
                    {
                        HediffDef hd = ht.hediff;
                        if (hd.stages != null && hd.stages.Count > ht.stageIndex)
                        {
                            List<DamageDef> extantDamageFactors = new List<DamageDef>();
                            if (hd.stages[ht.stageIndex].damageFactors == null)
                            {
                                hd.stages[ht.stageIndex].damageFactors = new List<DamageFactor>();
                            } else {
                                foreach (DamageFactor dfac in hd.stages[ht.stageIndex].damageFactors)
                                {
                                    extantDamageFactors.Add(dfac.damageDef);
                                }
                            }
                            foreach (DamageDef d in dfg.damageDefs)
                            {
                                if (!extantDamageFactors.Contains(d))
                                {
                                    DamageFactor df = new DamageFactor();
                                    df.damageDef = d;
                                    df.factor = ht.factor;
                                    hd.stages[ht.stageIndex].damageFactors.Add(df);
                                }
                            }
                        }
                    }
                }
            }
        }
        //misc
        public static bool AnalogHasActiveGene(Pawn_GeneTracker pgt, GeneDef geneDef)
        {
            if (geneDef == null)
            {
                return false;
            }
            List<Gene> genesListForReading = pgt.GenesListForReading;
            for (int i = 0; i < genesListForReading.Count; i++)
            {
                if (genesListForReading[i].def == geneDef && genesListForReading[i].Active)
                {
                    return true;
                }
            }
            return false;
        }
        public static void ModifyGeneResourceMax(Pawn pawn, Gene_Resource gr)
        {
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is HediffWithComps hwc)
                {
                    foreach (HediffComp hc in hwc.comps)
                    {
                        if (hc is HediffComp_GeneticResourceModifiers mgrm && mgrm.Props.maxResourceOffsets.ContainsKey(gr.ResourceLabel))
                        {
                            gr.SetMax(gr.Max + mgrm.Props.maxResourceOffsets.TryGetValue(gr.ResourceLabel));
                        }
                    }
                }
            }
        }
        public static float TotalPsyfocusRefund(Pawn pawn, float psyfocusCost, bool isWord = false, bool isSkip = false)
        {
            if (pawn.psychicEntropy != null)
            {
                return Math.Max(-pawn.psychicEntropy.CurrentPsyfocus, psyfocusCost * pawn.GetStatValue(HautsDefOf.Hauts_PsycastFocusRefund));
            }
            return 0f;
        }
        public static List<TerrainDef> FertilityTerrainDefs(Map map, bool requiresBedrock = false)
        {
            if (requiresBedrock && !map.Biome.hasBedrock)
            {
                return null;
            }
            List<TerrainDef> terrainDefList = new List<TerrainDef>();
            terrainDefList.Add(TerrainDefOf.Gravel);
            terrainDefList.Add(TerrainDefOf.Soil);
            foreach (TerrainThreshold terrainThreshold in map.Biome.terrainsByFertility)
            {
                bool flag3 = !terrainDefList.Contains(terrainThreshold.terrain);
                if (flag3)
                {
                    terrainDefList.Add(terrainThreshold.terrain);
                }
            }
            foreach (TerrainPatchMaker terrainPatchMaker in map.Biome.terrainPatchMakers)
            {
                foreach (TerrainThreshold threshold in terrainPatchMaker.thresholds)
                {
                    bool flag4 = !terrainDefList.Contains(threshold.terrain);
                    if (flag4)
                    {
                        terrainDefList.Add(threshold.terrain);
                    }
                }
            }
            return terrainDefList;
        }
        public static void AddHediffFromMenu(HediffDef chosenHediff, Pawn pawn, CompAbilityEffect_GiveHediffFromMenu ability, Pawn other, Pawn caster)
        {
            Hediff hediff = HediffMaker.MakeHediff(chosenHediff, pawn, ability.Props.onlyBrain ? pawn.health.hediffSet.GetBrain() : null);
            HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (hediffComp_Disappears != null)
            {
                hediffComp_Disappears.ticksToDisappear = ability.GetDurationSeconds(pawn).SecondsToTicks();
            }
            if (ability.Props.severity >= 0f)
            {
                hediff.Severity = ability.Props.severity;
            }
            HediffComp_Link hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
            if (hediffComp_Link != null)
            {
                hediffComp_Link.other = other;
                hediffComp_Link.drawConnection = (pawn == caster);
            }
            HediffComp_MultiLink hcml = hediff.TryGetComp<HediffComp_MultiLink>();
            if (hcml != null)
            {
                if (hcml.others == null)
                {
                    hcml.others = new List<Thing>();
                }
                hcml.others.Add(other);
                if (hcml.motes == null)
                {
                    hcml.motes = new List<MoteDualAttached>();
                }
                hcml.motes.Add(null);
                hcml.drawConnection = true;
            }
            pawn.health.AddHediff(hediff, null, null, null);
        }
        public static void DoRandomDiseaseOutbreak(Thing thing)
        {
            BiomeDef biome = (thing.MapHeld.Tile.Valid ? Find.WorldGrid[thing.MapHeld.Tile].PrimaryBiome : DefDatabase<BiomeDef>.GetRandom());
            IncidentDef incidentDef = DefDatabase<IncidentDef>.AllDefs.Where((IncidentDef d) => d.category == IncidentCategoryDefOf.DiseaseHuman).RandomElementByWeightWithFallback((IncidentDef d) => biome.CommonalityOfDisease(d), null);
            Map map = thing.MapHeld;
            if (map == null)
            {
                List<Map> maps = Find.Maps.Where((Map x) => x.IsPlayerHome).ToList();
                if (maps != null)
                {
                    map = maps.RandomElement();
                }
            }
            if (incidentDef != null && map != null)
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.DiseaseHuman, map);
                incidentDef.Worker.TryExecute(parms);
            }
        }
        //vgravshipe integration
        public static bool AddGravdata(Pawn researcher, float power)
        {
            return false;
        }
        //vpe integration
        public static bool IsVPEPsycast(VEF.Abilities.Ability ability)
        {
            return false;
        }
        public static int GetVPEPsycastLevel(VEF.Abilities.Ability ability)
        {
            return 0;
        }
        public static float GetVPEEntropyCost(VEF.Abilities.Ability ability)
        {
            return 0f;
        }
        public static float GetVPEPsyfocusCost(VEF.Abilities.Ability ability)
        {
            return 0f;
        }
        public static void VPEUnlockAbility(Pawn pawn, VEF.Abilities.AbilityDef abilityDef)
        {

        }
        public static void VPESetSkillPointsAndExperienceTo(Pawn setFor, Pawn copyFrom)
        {

        }
        //rim languages integration
        public static void LearnLanguage(Pawn pawn, Pawn other, float power)
        {

        }
        //permits
        public static bool AllowCheckPMEs(PermitMoreEffects pme, PawnKindDef p)
        {
            if (!pme.allowedPawnKinds.NullOrEmpty())
            {
                return pme.allowedPawnKinds.Contains(p);
            }
            if (!pme.disallowedPawnKinds.NullOrEmpty() && pme.disallowedPawnKinds.Contains(p))
            {
                return false;
            }
            return (pme.allowAnyFlesh && p.RaceProps.IsFlesh) || (pme.allowAnyNonflesh && !p.RaceProps.IsFlesh) || ((pme.allowDryads || !p.RaceProps.Dryad) && (pme.allowEntities || !p.RaceProps.IsAnomalyEntity) && (pme.allowInsectoids || !p.RaceProps.Insect) && (pme.allowMechs || !p.RaceProps.IsMechanoid) && (pme.allowDrones || !p.RaceProps.IsDrone) && (pme.allowAnimals || !p.RaceProps.Animal) && (pme.allowHumanlikes || !p.RaceProps.Humanlike));
        }
        //burgle
        public static bool HasAnyBurglars(Caravan caravan)
        {
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (p.Faction == Faction.OfPlayerSilentFail && p.GetStatValue(HautsDefOf.Hauts_PilferingStealth) > 0f)
                {
                    return true;
                }
            }
            return false;
        }
        public static void Burgle(Caravan caravan, Settlement settlement)
        {
            if (settlement.trader == null)
            {
                TaggedString message = "Hauts_NotBurglable".Translate();
                Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                return;
            } else {
                float burglaryMaxWeight = caravan.MassCapacity;
                float burglaryMaxValue = 0f;
                float successChance = 0f;
                List<Pawn> skulkersInCaravan = new List<Pawn>();
                foreach (Pawn p in caravan.PawnsListForReading)
                {
                    if (p.GetStatValue(HautsDefOf.Hauts_PilferingStealth) > 0f)
                    {
                        skulkersInCaravan.Add(p);
                    }
                }
                float alertLevel = HautsUtility.SettlementAlertLevel(settlement);
                foreach (Pawn p in skulkersInCaravan)
                {
                    burglaryMaxValue += p.GetStatValue(HautsDefOf.Hauts_MaxPilferingValue);
                    successChance += p.GetStatValue(HautsDefOf.Hauts_PilferingStealth);
                }
                successChance /= skulkersInCaravan.Count;
                successChance -= alertLevel;
                if (skulkersInCaravan.Count == 0)
                {
                    TaggedString message = "Hauts_NoPilferers".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                }
                if (burglaryMaxWeight <= 0f)
                {
                    TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererNoCarryCap".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                } else if (burglaryMaxValue <= 0f) {
                    TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooWeak".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                } else if (successChance <= 0f) {
                    TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooConspicuous".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                } else {
                    for (int i = skulkersInCaravan.Count - 1; i >= 0; i--)
                    {
                        HautsUtility.AdjustPickpocketSensitiveHediffs(skulkersInCaravan[i]);
                    }
                    Find.WindowStack.Add(new BurgleWindow(caravan, skulkersInCaravan, settlement, burglaryMaxValue, burglaryMaxWeight, successChance));
                }
            }
        }
        public static float SettlementAlertLevel(Settlement settlement)
        {
            float alertLevel = 0f;
            Faction f = settlement.Faction;
            if (f != null)
            {
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                if (fch != null)
                {
                    HautsFactionComp_BurglaryResponse br = fch.TryGetComp<HautsFactionComp_BurglaryResponse>();
                    if (br != null)
                    {
                        alertLevel = br.currentAlertLevel;
                        if (br.Props.specificFactionMinAlertLevels != null && br.Props.specificFactionMinAlertLevels.ContainsKey(f.def))
                        {
                            br.Props.specificFactionMinAlertLevels.TryGetValue(f.def, out float minAlertLevel);
                            alertLevel += minAlertLevel;
                        } else {
                            TechLevel tl = f.def.techLevel;
                            if (br.Props.minAlertLevelPerTechLevel != null && br.Props.minAlertLevelPerTechLevel.ContainsKey(tl))
                            {
                                br.Props.minAlertLevelPerTechLevel.TryGetValue(tl, out float minAlertLevel);
                                alertLevel += minAlertLevel;
                            }
                        }
                    }
                }
            }
            return alertLevel;
        }
        public static void AdjustPickpocketSensitiveHediffs(Pawn pilferer)
        {
            for (int i = pilferer.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
            {
                HediffComp_ChangeSeverityOnVerbUse csovu = pilferer.health.hediffSet.hediffs[i].TryGetComp<HediffComp_ChangeSeverityOnVerbUse>();
                if (csovu != null && csovu.Props.pilferingCountsAsVerb)
                {
                    csovu.AdjustSeverity();
                }
            }
        }
        public static void IncreaseAlertLevel(Pawn victim, float value)
        {
            Hediff existingHediff = victim.health.hediffSet.GetFirstHediffOfDef(HautsDefOf.Hauts_RaisedAlertLevel);
            if (existingHediff != null)
            {
                existingHediff.Severity += value;
            }
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(HautsDefOf.Hauts_RaisedAlertLevel, victim, null);
                victim.health.AddHediff(newHediff);
                newHediff.Severity = value;
            }
        }
        //checkers and lists
        public static bool CanBeHitByAirToSurface(IntVec3 iv3, Map map, bool blockedByThinRoofs)
        {
            RoofDef roof = iv3.GetRoof(map);
            if (roof != null && (blockedByThinRoofs || roof.isThickRoof || !roof.canCollapse))
            {
                return false;
            }
            List<Thing> highShields = map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < highShields.Count; i++)
            {
                CompProjectileInterceptor cpi = highShields[i].TryGetComp<CompProjectileInterceptor>();
                if (cpi != null && cpi.Active && cpi.Props.interceptAirProjectiles && iv3.InHorDistOf(highShields[i].PositionHeld, cpi.Props.radius))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsntCastingAbility(Pawn pawn)
        {
            return (pawn.CurJob == null || (pawn.CurJob.ability == null && !(pawn.CurJob.verbToUse is VEF.Abilities.Verb_CastAbility)));
        }
        public static float DamageFactorFor(DamageDef def, Thing t)
        {
            float damageFactor = 1f;
            DamageInfo dinfo = new DamageInfo(def, 0f, 0f);
            if (t is Pawn p)
            {
                damageFactor *= p.health.hediffSet.FactorForDamage(dinfo) * ((ModsConfig.BiotechActive && p.genes != null) ? p.genes.FactorForDamage(dinfo) : 1f);
            }
            if (t.def.damageMultipliers != null)
            {
                foreach (DamageMultiplier dm in t.def.damageMultipliers)
                {
                    if (dm.damageDef == def)
                    {
                        damageFactor *= dm.multiplier;
                    }
                }
            }
            return damageFactor;
        }
        public static float HitPointTotalFor(Pawn p)
        {
            float result = 0f;
            foreach (BodyPartRecord bpr in p.RaceProps.body.AllParts)
            {
                result += p.health.hediffSet.GetPartHealth(bpr);
            }
            return result;
        }
        public static float MissingHitPointPercentageFor(Pawn p)
        {
            HediffSet hediffSet = p.health.hediffSet;
            float num = 0f;
            for (int i = 0; i < hediffSet.hediffs.Count; i++)
            {
                if (hediffSet.hediffs[i] is Hediff_Injury)
                {
                    num += hediffSet.hediffs[i].Severity;
                }
            }
            return num / p.health.LethalDamageThreshold;
        }
        public static bool ReactsToEMP(Pawn p)
        {
            foreach (Hediff h in p.health.hediffSet.hediffs)
            {
                if (h.def.HasModExtension<NoEMPReaction>())
                {
                    return false;
                }
            }
            if (!p.stances.stunner.Stunned)
            {
                MethodInfo EMPstunCheck = typeof(StunHandler).GetMethod("CanBeStunnedByDamage", BindingFlags.NonPublic | BindingFlags.Instance);
                if ((bool)EMPstunCheck.Invoke(p.stances.stunner, new object[] { DamageDefOf.EMP }))
                {
                    return true;
                }
            }
            foreach (Hediff h in p.health.hediffSet.hediffs)
            {
                HediffComp_ReactOnDamage rod = h.TryGetComp<HediffComp_ReactOnDamage>();
                if (rod != null && rod.Props.damageDefIncoming == DamageDefOf.EMP)
                {
                    return true;
                }
                HediffComp_DamageNegationShield dns = h.TryGetComp<HediffComp_DamageNegationShield>();
                if (dns != null && dns.Props.instantlyOverwhelmedBy != null && dns.Props.instantlyOverwhelmedBy == DamageDefOf.EMP && dns.Energy > 0f)
                {
                    return true;
                }
            }
            if (p.apparel != null)
            {
                foreach (Apparel a in p.apparel.WornApparel)
                {
                    RimWorld.CompShield cs = a.TryGetComp<RimWorld.CompShield>();
                    if (cs != null && cs.ShieldState == ShieldState.Active)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static int TotalPsycastLevel(Pawn p)
        {
            int totalPsycastPower = 0;
            if (p.abilities != null)
            {
                for (int i = 0; i < p.abilities.abilities.Count; i++)
                {
                    if (p.abilities.abilities[i].def.IsPsycast)
                    {
                        totalPsycastPower += p.abilities.abilities[i].def.level;
                    }
                }
            }
            return totalPsycastPower;
        }
        public static bool ShouldNotGrantTraitStuff(Pawn pawn, TraitSet traitSet, Trait trait)
        {
            return false;
        }
        public static bool IsOtherDisallowedTrait(TraitDef t)
        {
            return false;
        }
        public static bool IsOtherDisallowedTrait(TraitDef t, out string reason)
        {
            reason = "";
            return false;
        }
        public static void AddExciseTraitExemption(TraitDef def)
        {
            HautsUtility.exciseTraitExemptions.Add(def);
        }
        public static bool IsExciseTraitExempt(TraitDef def, bool includeSexualities = false)
        {
            if (HautsUtility.exciseTraitExemptions.Contains(def) || def.HasModExtension<ExciseTraitExemption>())
            {
                return true;
            }
            if (includeSexualities && def.exclusionTags.Contains("SexualOrientation"))
            {
                return true;
            }
            return false;
        }
        public static void CheckIfAbilityHasRequiredPsylink(Pawn pawn, RimWorld.Ability parent)
        {
            if (!ModsConfig.RoyaltyActive)
            {
                pawn.abilities.RemoveAbility(parent.def);
                return;
            } else if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE")) {
                Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                if (psylink == null)
                {
                    pawn.abilities.RemoveAbility(parent.def);
                    return;
                }
            } else if (pawn.GetPsylinkLevel() <= 0) {
                pawn.abilities.RemoveAbility(parent.def);
                return;
            }
        }
        public static bool IsLeapAbility(VEF.Abilities.AbilityDef abilityDef)
        {
            if (abilityDef.HasModExtension<AbilityStatEffecters>() && abilityDef.GetModExtension<AbilityStatEffecters>().leap)
            {
                return true;
            }
            return false;
        }
        public static bool IsSkipAbility(VEF.Abilities.AbilityDef abilityDef)
        {
            if (abilityDef.HasModExtension<AbilityStatEffecters>() && abilityDef.GetModExtension<AbilityStatEffecters>().skip)
            {
                return true;
            }
            return false;
        }
        public static void AddGoodEvent(IncidentDef def)
        {
            goodEventPool.Add(def);
        }
        public static void AddBadEvent(IncidentDef def)
        {
            badEventPool.Add(def);
        }
        public static void MakeGoodEvent(Pawn p)
        {
            List<IncidentDef> incidents = HautsUtility.goodEventPool;
            if (incidents.Count > 0)
            {
                bool incidentFired = false;
                int tries = 0;
                while (!incidentFired && tries <= 50)
                {
                    IncidentDef toTryFiring = incidents.RandomElement<IncidentDef>();
                    IncidentParms parms = null;
                    if (toTryFiring.TargetAllowed(Find.World))
                    {
                        parms = new IncidentParms
                        {
                            target = Find.World
                        };
                    }
                    else if (Find.Maps.Count > 0)
                    {
                        Map mapToHit = Find.Maps.RandomElement<Map>();
                        if (Find.AnyPlayerHomeMap != null && Rand.Value <= 0.5f)
                        {
                            mapToHit = Find.AnyPlayerHomeMap;
                        }
                        parms = new IncidentParms
                        {
                            target = mapToHit
                        };
                    }
                    if (parms != null)
                    {
                        IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(toTryFiring.category, parms.target);
                        incidentParms.forced = true;
                        if (toTryFiring.Worker.CanFireNow(parms))
                        {
                            incidentFired = true;
                            toTryFiring.Worker.TryExecute(parms);
                            break;
                        }
                    }
                    tries++;
                }
            }
        }
        public static void MakeBadEvent(Pawn p)
        {
            List<IncidentDef> incidents = HautsUtility.badEventPool;
            if (incidents.Count > 0)
            {
                bool incidentFired = false;
                int tries = 0;
                while (!incidentFired && tries <= 50)
                {
                    IncidentDef toTryFiring = incidents.RandomElement<IncidentDef>();
                    IncidentParms parms = null;
                    if (toTryFiring.TargetAllowed(Find.World))
                    {
                        parms = new IncidentParms
                        {
                            target = Find.World
                        };
                    }
                    else if (Find.Maps.Count > 0)
                    {
                        Map mapToHit = Find.Maps.RandomElement<Map>();
                        if (Find.AnyPlayerHomeMap != null && Rand.Value <= 0.5f)
                        {
                            mapToHit = Find.AnyPlayerHomeMap;
                        }
                        parms = new IncidentParms
                        {
                            target = mapToHit
                        };
                    }
                    if (parms != null)
                    {
                        IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(toTryFiring.category, parms.target);
                        incidentParms.forced = true;
                        if (toTryFiring.Worker.CanFireNow(parms))
                        {
                            incidentFired = true;
                            toTryFiring.Worker.TryExecute(parms);
                            break;
                        }
                    }
                    tries++;
                }
            }
        }
        public static bool COaNN_TraitReset_ShouldDoBonusEffect(TraitDef def)
        {
            return false;
        }
        public static void COaNN_TraitReset_BonusEffects(Pawn user, List<TraitDef> defs)
        {

        }
        public static bool IsHighFantasy()
        {
            return HautsUtility.isHighFantasy;
        }
        public static bool CombatIsExtended()
        {
            return HautsUtility.combatIsExtended;
        }
        public static Hediff GetStrongestMechlink(Pawn p)
        {
            float bestCommandRange = 0f;
            Hediff result = null;
            foreach (Hediff h in p.health.hediffSet.hediffs)
            {
                if (HautsUtility.IsMechlink(h))
                {
                    HediffStage cs = h.CurStage;
                    if (cs != null)
                    {
                        float cr = cs.statOffsets.GetStatOffsetFromList(HautsDefOf.Hauts_MechCommandRange);
                        if (cr > bestCommandRange)
                        {
                            result = h;
                            bestCommandRange = cr;
                        }
                    }
                }
            }
            return result;
        }
        public static bool IsMechlink(Hediff h)
        {
            return HautsUtility.mechlinkDefs.Contains(h.def);
        }
        public static bool IsAwakenedPsychic(Pawn pawn)
        {
            return false;
        }
        private static readonly List<TraitDef> exciseTraitExemptions = new List<TraitDef>() { };
        public static readonly List<IncidentDef> goodEventPool = new List<IncidentDef>() { };
        public static readonly List<IncidentDef> badEventPool = new List<IncidentDef>() { };
        public static bool isHighFantasy;
        public static bool combatIsExtended;
        public static List<HediffDef> mechlinkDefs;
        public struct BookSubjectSymbol
        {
            public string keyword;
            public List<ValueTuple<string, string>> subSymbols;
        }
    }
    //mod settings
    public class Hauts_Settings : ModSettings
    {
        public bool apparelWearRateCrafting = false;
        public bool breachDamageConstruction = false;
        public bool overdoseSusceptibilityMedicine = false;
        public bool pilferingStealthSocial = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref apparelWearRateCrafting, "apparelWearRateCrafting", false);
            Scribe_Values.Look(ref breachDamageConstruction, "breachDamageConstruction", false);
            Scribe_Values.Look(ref overdoseSusceptibilityMedicine, "overdoseSusceptibilityMedicine", false);
            Scribe_Values.Look(ref pilferingStealthSocial, "pilferingStealthSocial", false);
            base.ExposeData();
        }
    }
    public class Hauts_Mod : Mod
    {
        public Hauts_Mod(ModContentPack content) : base(content)
        {
            Hauts_Mod.settings = GetSettings<Hauts_Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Hauts_SettingAWRFC".Translate(), ref settings.apparelWearRateCrafting, "Hauts_TooltipAWRFC".Translate());
            listingStandard.CheckboxLabeled("Hauts_SettingBDFC".Translate(), ref settings.breachDamageConstruction, "Hauts_TooltipBDFC".Translate());
            listingStandard.CheckboxLabeled("Hauts_SettingOSC".Translate(), ref settings.overdoseSusceptibilityMedicine, "Hauts_TooltipOSC".Translate());
            listingStandard.CheckboxLabeled("Hauts_SettingPSC".Translate(), ref settings.pilferingStealthSocial, "Hauts_TooltipPSC".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return "Hauts' Framework";
        }
        public static Hauts_Settings settings;
    }
}
