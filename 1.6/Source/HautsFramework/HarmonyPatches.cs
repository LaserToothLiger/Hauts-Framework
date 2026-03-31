using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HautsFramework
{
    [StaticConstructorOnStartup]
    public static class HautsFramework
    {
        private static readonly Type patchType = typeof(HautsFramework);
        static HautsFramework()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.main");
            //tweak - VEF Abilities notify of verb use. I don't know if this is necessary anymore, actually
            harmony.Patch(AccessTools.Method(typeof(VEF.Abilities.Ability), nameof(VEF.Abilities.Ability.Cast), new[] { typeof(GlobalTargetInfo[]) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsVFEAbility_CastPostfix)));
            //tweak - pawn with a violence-disabling trait that is generated with a weapon loses the violence-disabling trait
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnKindDef), typeof(Faction), typeof(PlanetTile) }),
                          postfix: new HarmonyMethod(patchType, nameof(Hauts_GeneratePawnPostfix)));
            //tweak - permit selection menu preferentially snaps to the faction you have the highest title in. this isn't always what you want, but it's almost always better than the default when you have non-Empire permit-granting factions.
            harmony.Patch(AccessTools.Method(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.OpenPermitWindow)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_OpenPermitTabPostfix)));
            //faction comps - ensure they are given to new factions
            harmony.Patch(AccessTools.Method(typeof(FactionManager), nameof(FactionManager.Add)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsFactionManager_AddPostfix)));
            //faction comps - a removed faction's comps should be removed too. could theoretically be handled by the ticking of faction comps, but factions are destroyed so infrequently this patch is more performant.
            harmony.Patch(AccessTools.Method(typeof(TaleManager), nameof(TaleManager.Notify_FactionRemoved)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsNotify_FactionRemovedPostfix)));
            //faction comps - this is for the example comp, SpyPoints. Adds a faction's spypoints to the adjusted raid points of any raid they send, losing those points in the process. can't add more than 1x the original ARP
            harmony.Patch(AccessTools.Method(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.AdjustedRaidPoints)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsAdjustedRaidPointsPostfix)));
            //stats - i wonder which stats these ones are for
            harmony.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedArmorPenetration), new[] { typeof(Verb), typeof(Pawn) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsAdjustedArmorPenetrationPostfix)));
            harmony.Patch(AccessTools.Property(typeof(Projectile), nameof(Projectile.ArmorPenetration)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HautsArmorPenetrationPostfix)));
            //stats - breach damage factor increases damage done to buildings
            harmony.Patch(AccessTools.Method(typeof(DamageWorker), nameof(DamageWorker.Apply)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsDamageWorker_ApplyPrefix)));
            //stats - anti entity damage factor vs Anomaly entities
            harmony.Patch(AccessTools.Method(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.Apply)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsDamageWorker_AddInjury_ApplyPrefix)));
            //stats - skip and spew range factor adjust ability ranges where applicable. Jumps are handled separately
            harmony.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedRange)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_AdjustedRangePostfix)));
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_WithDest), nameof(CompAbilityEffect_WithDest.CanHitTarget)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_CAE_WD_CanHitTargetPrefix)));
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Verb_CastAbility), nameof(RimWorld.Verb_CastAbility.DrawHighlight)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_VCA_DH_DrawHighlightPrefix)));
            //stats - the two caravan visibility stats apply here
            harmony.Patch(AccessTools.Method(typeof(CaravanVisibilityCalculator), nameof(CaravanVisibilityCalculator.Visibility), new[] { typeof(List<Pawn>), typeof(bool), typeof(StringBuilder) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsCaravanVisibilityPostfix)));
            //stats - track size <1 makes you less likely to pick up filth from walking over a cell with filth-imparting terrain
            harmony.Patch(AccessTools.Method(typeof(Pawn_FilthTracker), nameof(Pawn_FilthTracker.GainFilth), new[] { typeof(ThingDef) }),
                          prefix: new HarmonyMethod(patchType, nameof(HautsGainFilthPrefix)));
            //stats - track size >1 provides a chance to drop ADDITIONAL filths whenever you'd drop one
            MethodInfo methodInfo = typeof(Pawn_FilthTracker).GetMethod("DropCarriedFilth", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo,
                          postfix: new HarmonyMethod(patchType, nameof(HautsDropCarriedFilthPostfix)));
            //stats - VEF leap and skip abilities affected by jump/skip range factor
            harmony.Patch(AccessTools.Method(typeof(VEF.Abilities.Ability), nameof(VEF.Abilities.Ability.GetRangeForPawn)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_GetRangeForPawnPostfix)));
            //stats - skill gain from recreation applies to the Intellectual gain from books, as well as to conventional joy sources
            harmony.Patch(AccessTools.Method(typeof(Book), nameof(Book.OnBookReadTick)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsOnBookReadTickPostfix)));
            harmony.Patch(AccessTools.Method(typeof(JoyUtility), nameof(JoyUtility.JoyTickCheckEnd)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsJoyTickCheckEndPostfix)));
            //stats - boredom decay operates here. because boredom decay adds the non-modded amount of recreation tolerance loss to itself for representational purposes, we have to subtract that out when doing this calculation
            harmony.Patch(AccessTools.Method(typeof(JoyToleranceSet), nameof(JoyToleranceSet.NeedInterval)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsJoyToleranceSet_NeedIntervalPostfix)));
            //stats - makes the learning from workwatching and lessontaking affected by InstructivePower
            harmony.Patch(AccessTools.Method(typeof(LearningUtility), nameof(LearningUtility.LearningRateFactor)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsLearningRateFactorPostfix)));
            /*ModifyingTraits DME for thought defs works here. See ThoughtMechanics.cs.
            Oh, also this handles the IdeoligiousThoughtFactor stat's multiplication of ideoligious thought magnitudes*/
            harmony.Patch(AccessTools.Method(typeof(ThoughtHandler), nameof(ThoughtHandler.MoodOffsetOfGroup)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsMoodOffsetOfGroupPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ThoughtHandler), nameof(ThoughtHandler.OpinionOffsetOfGroup)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsOpinionOffsetOfGroupPostfix)));
            //stats - jump range factor alters range of (conventional) jump abilities
            harmony.Patch(AccessTools.Property(typeof(Verb_CastAbilityJump), nameof(Verb_CastAbilityJump.EffectiveRange)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_VCAJ_EffectiveRangePostfix)));
            harmony.Patch(AccessTools.Property(typeof(Verb_Jump), nameof(Verb_Jump.EffectiveRange)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_VJ_EffectiveRangePostfix)));
            if (ModsConfig.BiotechActive)
            {
                //stats - hemogen yield increases hemogen gain from (conventional) bloodfeeding. Second patch handles the Last Epoch-like chance to spawn bonus hemogen packs when extracted from
                harmony.Patch(AccessTools.Method(typeof(SanguophageUtility), nameof(SanguophageUtility.HemogenGainBloodlossFactor)),
                               postfix: new HarmonyMethod(patchType, nameof(Hauts_HemogenGainBloodlossFactorPostfix)));
                MethodInfo methodInfoHemogen = typeof(Recipe_ExtractHemogen).GetMethod("OnSurgerySuccess", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfoHemogen,
                              prefix: new HarmonyMethod(patchType, nameof(Hauts_Recipe_ExtractHemogen_OnSurgerySuccessPostfix)));
            }
            if (!ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
            {
                //stats - the two stats that refund your psyfocus when you psycast should do that. one patch for LTI psycasts (most of them), one for basically just Farskip
                harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                               postfix: new HarmonyMethod(patchType, nameof(HautsPsycast_ActivatePostfix)));
                harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(GlobalTargetInfo) }),
                               postfix: new HarmonyMethod(patchType, nameof(HautsPsycast_ActivatePostfix)));
            }
            //stats - psyfocus regen. Since it incorporates the normal decay rate at current psyfocus band for representational purposes, we have to subtract that out from the stat when actually doing calculations
            harmony.Patch(AccessTools.Method(typeof(MeditationUtility), nameof(MeditationUtility.CheckMeditationScheduleTeachOpportunity)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsCheckMeditationScheduleTeachOpportunityPostfix)));
            harmony.Patch(AccessTools.Method(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsNotify_PawnKilledPostfix)));
            //apparel wear rate factor works by modifying the decay rate of the apparel right as it is experiencing daily wear, then setting it back to its OG value.
            MethodInfo methodInfo3 = typeof(Pawn_ApparelTracker).GetMethod("TakeWearoutDamageForDay", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo3,
                          prefix: new HarmonyMethod(patchType, nameof(HautsTakeWearoutDamageForDayPrefix)));
            harmony.Patch(methodInfo3,
                          postfix: new HarmonyMethod(patchType, nameof(HautsTakeWearoutDamageForDayPostfix)));
            //overdose susceptibility works by modifying the overdose stats of the drug right as you are ingesting them, and then setting them back to their OG values.
            harmony.Patch(AccessTools.Method(typeof(CompDrug), nameof(CompDrug.PostIngested)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsCompDrug_PostIngestedPrefix)));
            harmony.Patch(AccessTools.Method(typeof(CompDrug), nameof(CompDrug.PostIngested)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsCompDrug_PostIngestedPostfix)));
            //TraitGrantedStuff. grant the stuff. and also remove it when the trait is removed.
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGainTraitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.RemoveTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsRemoveTraitPostfix)));
            //specifically implements the effects of prisonerResolveFactor. a simple one-time modifier to your will and resistance at time of capture works fine, as opposed to having to alter the effectiveness of recruit/enslave attempts when they occur
            harmony.Patch(AccessTools.Method(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_SetGuestStatusPostfix)));
            //pink God Mode button you can press to inflict TraitGrantedStuff (and remove ForcedByOtherProperty abilities and hediffs that wish to be removed if there are no present forcing properties)
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGetGizmosPostfix)));
            //makes CannotRemoveBionicsFrom work. as you can see, it is not theoretically perfect. Other mods' Recipe classes that remove an artificial body part completely bypass it. That's fine and I don't care enough to arms race on this front, not the way I do with ExciseTraitExemptions
            harmony.Patch(AccessTools.Method(typeof(Recipe_RemoveBodyPart), nameof(Recipe_RemoveBodyPart.ApplyOnPawn)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsApplyOnPawnPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Recipe_RemoveImplant), nameof(Recipe_RemoveImplant.ApplyOnPawn)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsApplyOnPawnPrefix)));
            harmony.Patch(AccessTools.Method(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.SpawnThingsFromHediffs)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsApplyOnPawnPrefix)));
            //when you are unaffected by darkness (trait DME), you are unaffected by darkness. waow
            MethodInfo methodInfo2 = typeof(StatPart_Glow).GetMethod("ActiveFor", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo2,
                          postfix: new HarmonyMethod(patchType, nameof(HautsStatPart_GlowActiveForPostfix)));
            harmony.Patch(AccessTools.Method(typeof(LifeStageWorker_HumanlikeAdult), nameof(LifeStageWorker_HumanlikeAdult.Notify_LifeStageStarted)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsNotify_LifeStageStartedPostfix)));
            //trait DME that makes you vanish right before you die. This is used for temporary pawns whom I don't want to trigger on-death thoughts, or to leave behind any loot
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
            }
            //    -note that this hediff comp handler also does the stat function for PsyfocusFromFood
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsIngestedPostfix)));
            //    -also note that this one also handles SpecificDamageFactorStats
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PreApplyDamage)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsFramework_PreApplyDamagePostfix)));
            harmony.Patch(AccessTools.Method(typeof(AttachableThing), nameof(AttachableThing.AttachTo)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_AddAttachmentPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_PsychicEntropyTracker), nameof(Pawn_PsychicEntropyTracker.OffsetPsyfocusDirectly)),
                           prefix: new HarmonyMethod(patchType, nameof(Hauts_OffsetPsyfocusDirectlyPrefix)));
            harmony.Patch(AccessTools.Method(typeof(HediffComp_ReactOnDamage), nameof(HediffComp_ReactOnDamage.Notify_PawnPostApplyDamage)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsPawnPostApplyDamagePrefix)));
            harmony.Patch(AccessTools.Method(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsStunNotifyDamageAppliedPostfix)));
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
            harmony.Patch(AccessTools.Method(typeof(RitualBehaviorWorker), nameof(RitualBehaviorWorker.TryExecuteOn)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsRitualBehaviorWorker_TryExecuteOnPostfix)));
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
            //ability, incident, and trait categories
            List<TraitDef> ConceitedTraits = GetTypeField(typeof(RoyalTitleUtility), "ConceitedTraits") as List<TraitDef>;
            foreach (IncidentDef i in DefDatabase<IncidentDef>.AllDefs)
            {
                if (i.HasModExtension<BelongsToEventPool>())
                {
                    if (i.GetModExtension<BelongsToEventPool>().good)
                    {
                        GoodAndBadIncidentsUtility.goodEventPool.Add(i);
                    }
                    if (i.GetModExtension<BelongsToEventPool>().bad)
                    {
                        GoodAndBadIncidentsUtility.badEventPool.Add(i);
                    }
                }
            } 
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefs)
            {
                if (t.HasModExtension<ExciseTraitExempt>())
                {
                    TraitModExtensionUtility.AddExciseTraitExemption(t);
                }
                if (t.HasModExtension<ConceitedTrait>())
                {
                    ConceitedTraits.Add(t);
                }
            }
            //ignore natural goodwill's influence on the amount of goodwill gained or lost by a particular HED
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.TryAffectGoodwillWith)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTryAffectGoodwillWithPrefix)));
            ModCompatibilityUtility.isHighFantasy = ModsConfig.IsActive("MrSamuelStreamer.RPGAdventureFlavour.DEV");
            if (!ModCompatibilityUtility.isHighFantasy)
            {
                ModCompatibilityUtility.isHighFantasy = ModsConfig.IsActive("Joe.RPGAdventureFlavour.Fork");
            }
            //Hauts_HeatDamageFactor is hardcoded to apply to Flame, instead of being handled in an xpath patch, to avoid AcidBurn and ElectricalBurn inheriting its SDFS
            Dictionary<StatDef, float> flameSDFS = new Dictionary<StatDef, float>
            {
                { HautsDefOf.Hauts_HeatDamageFactor, 1f }
            };
            if (DamageDefOf.Flame.modExtensions == null)
            {
                DamageDefOf.Flame.modExtensions = new List<DefModExtension>();
            }
            SpecificDamageFactorStats sdfs = DamageDefOf.Flame.GetModExtension<SpecificDamageFactorStats>();
            if (sdfs != null)
            {
                sdfs.factorStats.Add(HautsDefOf.Hauts_HeatDamageFactor, 1f);
            } else {
                DamageDefOf.Flame.modExtensions.Add(new SpecificDamageFactorStats(flameSDFS));
            }
            ModCompatibilityUtility.combatIsExtended = ModsConfig.IsActive("CETeam.CombatExtended");
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
        //faction comps. third method here makes the example comp, "spy points", work
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
            if (dinfo.Instigator != null && dinfo.Instigator is Pawn p && victim.def.category == ThingCategory.Building && victim.def.useHitPoints && dinfo.Def != DamageDefOf.Mining)
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
                    if (Stats_AbilityRangesUtility.IsSkipAbility(verb.ability.def))
                    {
                        __result *= attacker.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor);
                    }
                    if (Stats_AbilityRangesUtility.IsSpewAbility(verb.ability.def))
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
                if (Stats_AbilityRangesUtility.IsSkipAbility(__instance.parent.def) && __instance.parent.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor) != 1f)
                {
                    LocalTargetInfo selectedTarget = (LocalTargetInfo)GetInstanceField(typeof(CompAbilityEffect_WithDest), __instance, "selectedTarget");
                    __result = target.IsValid && (__instance.Props.range <= 0f || target.Cell.DistanceTo(selectedTarget.Cell) <= __instance.Props.range * __instance.parent.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor) && (!__instance.Props.requiresLineOfSight || GenSight.LineOfSight(selectedTarget.Cell, target.Cell, __instance.parent.pawn.Map, false, null, 0, 0)));
                    return false;
                }
                if (Stats_AbilityRangesUtility.IsSpewAbility(__instance.parent.def) && __instance.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) != 1f)
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
                if (Stats_AbilityRangesUtility.IsSkipAbility(__instance.ability.def) && __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor) != 1f)
                {
                    Stats_AbilityRangesUtility.DrawBoostedAbilityRange(__instance, target, HautsDefOf.Hauts_SkipcastRangeFactor);
                    return false;
                }
                if (Stats_AbilityRangesUtility.IsSpewAbility(__instance.ability.def) && __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) != 1f)
                {
                    Stats_AbilityRangesUtility.DrawBoostedAbilityRange(__instance, target, HautsDefOf.Hauts_SpewRangeFactor);
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
                if (Stats_AbilityRangesUtility.IsLeapAbility(__instance.def))
                {
                    __result *= __instance.pawn.GetStatValue(HautsDefOf.Hauts_JumpRangeFactor);
                }
                if (Stats_AbilityRangesUtility.IsSkipAbility(__instance.def))
                {
                    __result *= __instance.pawn.GetStatValue(HautsDefOf.Hauts_SkipcastRangeFactor);
                }
            }
        }
        public static void HautsPsycast_ActivatePostfix(Psycast __instance)
        {
            if (__instance.pawn != null)
            {
                float totalRefund = HautsMiscUtility.TotalPsyfocusRefund(__instance.pawn, __instance.def.PsyfocusCost, __instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("WordOf"), __instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("Skip"));
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
        public static void Hauts_Recipe_ExtractHemogen_OnSurgerySuccessPostfix(Pawn pawn)
        {
            float bonusPacks = pawn.GetStatValue(HautsDefOf.Hauts_HemogenContentFactor) - 1f;
            while (bonusPacks > 0)
            {
                if (Rand.Chance(bonusPacks))
                {
                    if (!GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.HemogenPack, null), pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near, null, null, null, 1))
                    {
                        Log.Error("Could not drop hemogen pack near " + pawn.PositionHeld.ToString());
                    }
                }
                bonusPacks -= 1f;
            }
        }
        public static void HautsTakeWearoutDamageForDayPrefix(Pawn_ApparelTracker __instance, Thing ap, ref float __state)
        {
            __state = ap.def.apparel.wearPerDay;
            Pawn pawn = __instance.pawn;
            if (pawn != null)
            {
                ap.def.apparel.wearPerDay *= pawn.GetStatValue(HautsDefOf.Hauts_ApparelWearRateFactor);
            }
        }
        public static void HautsTakeWearoutDamageForDayPostfix(Pawn_ApparelTracker __instance, Thing ap, float __state)
        {
            ap.def.apparel.wearPerDay = __state;
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
        //trait defmodextension functionalities
        public static void HautsGainTraitPostfix(TraitSet __instance, Trait trait, bool suppressConflicts)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (!TraitModExtensionUtility.ShouldNotGrantTraitStuff(pawn, __instance, trait))
            {
                TraitModExtensionUtility.AddTraitGrantedStuff(true, trait, pawn);
                if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && suppressConflicts)
                {
                    for (int k = 0; k < __instance.allTraits.Count; k++)
                    {
                        if (__instance.allTraits[k].def != trait.def && trait.def.CanSuppress(__instance.allTraits[k]) && __instance.allTraits[k].def.canBeSuppressed)
                        {
                            TraitModExtensionUtility.RemoveTraitGrantedStuff(__instance.allTraits[k], pawn);
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
            TraitModExtensionUtility.RemoveTraitGrantedStuff(trait, pawn);
            if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && unsuppressConflicts)
            {
                for (int k = 0; k < __instance.allTraits.Count; k++)
                {
                    if (__instance.allTraits[k].def != trait.def && trait.def.CanSuppress(__instance.allTraits[k]))
                    {
                        TraitModExtensionUtility.AddTraitGrantedStuff(true, __instance.allTraits[k], pawn);
                    }
                }
            }
            pawn.needs?.AddOrRemoveNeedsAsAppropriate();
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
                        TraitModExtensionUtility.TraitGrantedStuffRegeneration(__instance);
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
            if (TraitModExtensionUtility.TryVanishPawn(__instance))
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
            if (pawn.story != null && TraitModExtensionUtility.CanApplyForcedBodyTypes(pawn))
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
        //hediff comps - IdeoCertaintySeverity removeOnApostasy
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
        //hediff comps - GeneticResourceModifiers
        public static void HautsResetMaxPostfix(Gene_Resource __instance)
        {
            HautsMiscUtility.ModifyGeneResourceMax(__instance.pawn, __instance);
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
        /*hediff comps - when a pawn takes damage, get all its PreDamageModifications, put them in order of highest to lowest priority, and run through their effects
         * this also applies all SpecificDamageFactorStats of the damage def, which happens before the PDMs*/
        public static void HautsFramework_PreApplyDamagePostfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (!absorbed)
            {
                if (dinfo.Def != null)
                {
                    SpecificDamageFactorStats sdfs = dinfo.Def.GetModExtension<SpecificDamageFactorStats>();
                    if (sdfs != null && !sdfs.factorStats.NullOrEmpty())
                    {
                        foreach (KeyValuePair<StatDef, float> kvp in sdfs.factorStats)
                        {
                            dinfo.SetAmount(dinfo.Amount * Math.Max(0f, ((__instance.GetStatValue(kvp.Key) - 1f) * kvp.Value) + 1f));
                        }
                    }
                }
                if (dinfo.Amount > 0f)
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
        }
        //hediff comps - some PDMs can prevent the attachment of "BadAttachments" (fire, VGE astrofire, VQE Cryptoforge cryptofreeze)
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
        //hediff comps - psyfocus spent tracker gains severity when psyfocus is lost (not just spent on psycasts).
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
        //hediff... ok, this one is a DME. NoEMPReaction prevents brain shocking, vomiting, etc. that EMP does thru ReactOnDamage
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
        //NoEMPReaction also doesn't want you to be stunned when you get EMP'd. Granted that it does this by invoking StopStun, so you can shoot an EMP at a NoEMPReaction-having pawn to clear other forms of stun, I think? Pretty rare idea though.
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
        //ability comps and AbilityCooldownModifier
        public static void HautsActivatePostfix(RimWorld.Ability __instance)
        {
            if ((__instance.def.cooldownTicksRange.min != 0 && __instance.def.cooldownTicksRange.max != 0) || __instance.def.groupDef != null)
            {
                float cooldownModifier = AbilityCooldownModifierUtility.GetCooldownModifier(__instance);
                int newCooldown = (int)(__instance.CooldownTicksRemaining / Math.Max(0.001f, cooldownModifier));
                AbilityCooldownModifierUtility.SetNewCooldown(__instance, newCooldown);
            }
        }
        public static void HautsGetCooldownForPawnPostfix(ref int __result, VEF.Abilities.Ability __instance)
        {
            __result /= Mathf.RoundToInt(AbilityCooldownModifierUtility.GetCooldownModifier(__instance));
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
                            if (HautsMiscUtility.AnalogHasActiveGene(__instance.pawn.genes,gd))
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
                                    float cooldownModifier = AbilityCooldownModifierUtility.GetCooldownModifier(ability);
                                    int newCooldown = (int)(ability.CooldownTicksRemaining / Math.Max(0.001f, cooldownModifier));
                                    AbilityCooldownModifierUtility.SetNewCooldown(ability, newCooldown);
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
                    float cooldownModifier = AbilityCooldownModifierUtility.GetCooldownModifier(ability);
                    int newCooldown = (int)(ability.CooldownTicksRemaining / Math.Max(0.001f, cooldownModifier));
                    AbilityCooldownModifierUtility.SetNewCooldown(ability, newCooldown);
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
        //player commands to Burglarize settlements
        public static IEnumerable<Gizmo> Hauts_Settlement_GetGizmosPostfix(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            if (Find.WorldObjects.AnySettlementAt(__instance.Tile) && PilferingSystemUtility.HasAnyBurglars(__instance))
            {
                Settlement settlement = Find.WorldObjects.SettlementAt(__instance.Tile);
                if (settlement.Faction != __instance.Faction && settlement.trader != null)
                {
                    yield return (new Command_Action
                    {
                        icon = ContentFinder<Texture2D>.Get("UI/Hauts_Burglarize", true),
                        defaultLabel = "Hauts_BurgleIcon".Translate() + " (" + HautsDefOf.Hauts_PawnAlertLevel.LabelForFullStatList + " " + PilferingSystemUtility.SettlementAlertLevel(settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")",
                        defaultDesc = "Hauts_BurgleTooltip".Translate(),
                        action = delegate ()
                        {
                            PilferingSystemUtility.Burgle(__instance, settlement);
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
        //resurrection in unusual circumstances should not delete a pawn!!!!! Now it does not!!!!!!
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
                    TraitModExtensionUtility.TraitGrantedStuffRegeneration(pawn);
                }
            }
        }
        //ai ability use stimulators
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
}
