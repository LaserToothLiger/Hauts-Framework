using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace HautsFramework
{
    /*so far only used by HAT, Hauts_PsycastLoopBreaker is a hediff that really only needs to exist when instantiated and otherwise should cease to exist.
     * See, in VPE, there's at least one way to trigger an ability's Cast effects multiple times from one ability cast (Group Link), and this is undesirable in HAT, whose transcendent traits (and Awakened Erudite)
     * have effects that end up triggering multiple times when they rly shouldn't. You shouldn't be able to farm giga xp when an Erudite casts Group Link and any Word psycast onto it, for example.
     * This hediff gets added to the caster on the first execution of the Cast's effects, but since the "rider effects" are predicated upon the caster not having this hediff, they won't trigger on subsequent repeats
     * until at least 1 tick has elapsed. I thought this might end up being useful in other mods, but that hasn't happened so far.
     * A little clunky. I'll revisit it later.*/ 
    public class Hediff_PsycastLoopBreaker : Hediff
    {
        public override void PostTick()
        {
            base.PostTick();
            this.pawn.health.RemoveHediff(this);
        }
    }
    //a Harmony patch allows psycasts with this DME to target psydeaf pawns
    public class PsycastCanTargetDeaf : DefModExtension
    {
        public PsycastCanTargetDeaf()
        {
        }
    }
    //hediff DME which confers immunity to stuns or ReactOnDamage effects from EMP damage
    public class NoEMPReaction : DefModExtension
    {
        public NoEMPReaction()
        {
        }
    }
    //the goodwill adjustment from history event defs with this DME is not subject to the multiplicative "pull" that normally occurs if the adjustment is in the direction of the faction's natural goodwill w/ the player
    public class IgnoresNaturalGoodwill : DefModExtension
    {
        public IgnoresNaturalGoodwill() { }
    }
    //toolkit
    public static class HautsMiscUtility
    {
        public static void StatScalingHeal(float baseHeal, StatDef statDef, Pawn toHeal, Pawn whoseStatMatters)
        {
            List<Hediff_Injury> source = new List<Hediff_Injury>();
            toHeal.health.hediffSet.GetHediffs<Hediff_Injury>(ref source, (Hediff_Injury x) => x.CanHealNaturally() || x.CanHealFromTending());
            Hediff_Injury hediff_Injury;
            if (source.TryRandomElement(out hediff_Injury))
            {
                hediff_Injury.Heal(baseHeal * whoseStatMatters.GetStatValue(statDef));
            }
        }
        //this was for some bugfix where HasActiveGene did not work. I think it turned out that person just hadn't updated RimWorld in a really long time. Whatever.
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
        //handles the addition of the pawn's GeneticResourceModifiers' max offsets to a specified Gene_Resource's max
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
        //calculates how much psyfocus you should get back on a psycast. This is a matter of the psycast focus refund stat, but is also patched in HAT (for some transes) and HUB (for the eltex silvertonuge's Word-exclusive refund).
        public static float TotalPsyfocusRefund(Pawn pawn, float psyfocusCost, bool isWord = false, bool isSkip = false)
        {
            if (pawn.psychicEntropy != null)
            {
                return Math.Max(-pawn.psychicEntropy.CurrentPsyfocus, psyfocusCost * pawn.GetStatValue(HautsDefOf.Hauts_PsycastFocusRefund));
            }
            return 0f;
        }
        //gets the list of fertile terrains in the biome afforded by its terrainsByFertility and terrainPatchMakers, as well as Gravel and Soil. Can be told to not return anything if in a non-bedrock biome e.g. sea ice
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
        //handles stuff for GiveHediffFromMenu ability comp
        public static void AddHediffFromMenu(HediffDef chosenHediff, Pawn pawn, CompAbilityEffect_GiveHediffFromMenuBase ability, Pawn other, Pawn caster, List<HediffDef> removeAnyOfTheseHediffsFirst)
        {
            if (!removeAnyOfTheseHediffsFirst.NullOrEmpty())
            {
                for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    if (removeAnyOfTheseHediffsFirst.Contains(pawn.health.hediffSet.hediffs[i].def))
                    {
                        pawn.health.RemoveHediff(pawn.health.hediffSet.hediffs[i]);
                    }
                }
            }
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
        /*basically like how Sickly can randomly assign diseases, except it strikes the colony like a normal disease event instead of being restricted to a specific pawn.
         * It pulls the possible disease list from the specified thing's biome, and doesn't work if the thing isn't on a valid planet tile*/
        public static void DoRandomDiseaseOutbreak(Thing thing)
        {
            if (thing.Tile.Valid)
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
        }
        //checks if the cell is under a mortar-blocking roof or shield (or even just any kind of roof if you want to do that)
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
        //checks ewisott
        public static bool IsntCastingAbility(Pawn pawn)
        {
            return (pawn.CurJob == null || (pawn.CurJob.ability == null && !(pawn.CurJob.verbToUse is VEF.Abilities.Verb_CastAbility)));
        }
        //summarizes the final damage factor the thing has for that damage def (based on hediffs and thingdef damageFactors)
        public static float DamageFactorFor(DamageDef def, Thing t)
        {
            float damageFactor = 1f;
            DamageInfo dinfo = new DamageInfo(def, 0f, 0f);
            if (t is Pawn p)
            {
                damageFactor *= p.health.hediffSet.FactorForDamage(dinfo) * ((ModsConfig.BiotechActive && p.genes != null) ? p.genes.FactorForDamage(dinfo) : 1f);
                SpecificDamageFactorStats sdfs = def.GetModExtension<SpecificDamageFactorStats>();
                if (sdfs != null && !sdfs.factorStats.NullOrEmpty())
                {
                    foreach (KeyValuePair<StatDef, float> kvp in sdfs.factorStats)
                    {
                        dinfo.SetAmount(dinfo.Amount * Math.Max(0f, ((t.GetStatValue(kvp.Key) - 1f) * kvp.Value) + 1f));
                    }
                }
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
        //tells you the sum hit points of the pawn's body parts
        public static float HitPointTotalFor(Pawn p)
        {
            float result = 0f;
            foreach (BodyPartRecord bpr in p.RaceProps.body.AllParts)
            {
                result += p.health.hediffSet.GetPartHealth(bpr);
            }
            return result;
        }
        //tells you the total severity of a pawn's injuries over its lethal damage threshold. Given how the LDT works, this is what in most games would be considered [1 - percentage health remaining]
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
        /*determines if the pawn lacks a NoEMPReaction hediff and then meets at least one of the following criteria
         * isn't adapted to EMPs but can be affected by them
         * has an EMPable shield or DamageNegationShield hediff
         * has a ReactOnDamage triggered by EMPs*/
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
        //sum level of all psycasts known by the pawn. This does not work with VPE psycasts
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
        //used to help generate the titles and/or descriptions of books outside of their native generation process. It is literally a duplicate of the private struct of the same name in the Book class
        public struct BookSubjectSymbol
        {
            public string keyword;
            public List<ValueTuple<string, string>> subSymbols;
        }
        //this is what you should invoke whenever you do delayed rezzes, as it handles all possible arguments
        public static void StartDelayedResurrection(Pawn pawn, IntRange initialRareTicks, string explanationKey, bool shouldSendMessage = true, bool shouldTranslateMessage = true, bool preventRisingAsShambler = true, HediffDef mutation = null, float mutationSeverity = 0f)
        {
            if (pawn.Corpse != null)
            {
                WorldComponent_HautsDelayedResurrections WCDR = (WorldComponent_HautsDelayedResurrections)Find.World.GetComponent(typeof(WorldComponent_HautsDelayedResurrections));
                WCDR.StartDelayedResurrection(pawn.Corpse, initialRareTicks, explanationKey, shouldSendMessage, shouldTranslateMessage, preventRisingAsShambler, mutation, mutationSeverity);
            }
        }
        //apply to the Notify_PawnDamagedThing of any custom hediff class you want to be able to utilize EOHEs
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
                    if (hc is HediffComp_ExtraOnHitEffects hoH && hoH != null && hoH.Props.appliedViaAttacks && hoH.cooldown <= Find.TickManager.TicksGame && result.totalDamageDealt >= hoH.Props.minDmgToTrigger && hoH.RangeCheck(thing, dinfo))
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
        /*Determines if the specified pawnkinddef is "allowed" by the PermitMoreEffects. It must be in the allowedPawnKinds (if any)…
         * …or if that field is null, it must be not in the disallowedPawnKinds (if any)…
         * …or if it's a fleshy race and allowAnyFlesh, it's good to go…
         * …or if it's a non-fleshy race and allowAnyNonflesh, it's good to go…
         * …or it must satisfy the dryad/anomalous entity/insectoid/mechanoid/drone/animal/humanlike qualifiers.
         * Used by DropPawns and GiveHediffs permit workers.*/
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
    }
}
