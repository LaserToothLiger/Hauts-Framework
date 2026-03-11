using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace HautsFramework
{
    /*Can be applied to a trait to make it grant hediffs, abilities*, and/or VEF abilities. See the XML of HAT for examples
     * grantedHediffs: when a pawn gains/un-suppresses this trait, all hediffs from the List whose int key matches the trait’s degree are added to the pawn. When a pawn loses/suppresses this trait, all such hediffs are lost.
     * otherHediffsToRemoveOnRemoval: when this trait is lost or suppressed, removes all hediffs from the degree-matched List.
     * hediffsToBrain: adds hediffs granted by this trait to the brain; if false, adds them to “whole body”.
     * prisonerResolveFactor: when this trait is gained or unsuppressed, OR when the pawn who has this trait is imprisoned, multiplies the pawn’s resistance and will by this amount.
     * grantedAbilities: when this trait is gained or unsuppressed, adds all abilities from the degree-matched List
     * grantedVEFAbilities: ditto for VEF abilities.
     *   who do not have a gene with the DisablesTGSbodyTypeAdjustment DME.*/
    public class TraitGrantedStuff : DefModExtension
    {
        public TraitGrantedStuff()
        {

        }
        public Dictionary<int, List<HediffDef>> grantedHediffs;
        public Dictionary<int, List<HediffDef>> otherHediffsToRemoveOnRemoval;
        public bool hediffsToBrain = false;
        public Dictionary<int, float> prisonerResolveFactor;
        public Dictionary<int, List<RimWorld.AbilityDef>> grantedAbilities;
        public Dictionary<int, List<VEF.Abilities.AbilityDef>> grantedVEFAbilities;
        //forcedBodyTypes does not do anything - will be removed come RimWorld 1.7 if I cannot think of a decent way to reintroduce it
        public Dictionary<BodyTypeDef, BodyTypeDef> forcedBodyTypes;
    }
    public class DisablesTGSbodyTypeAdjustment : DefModExtension
    {
        public DisablesTGSbodyTypeAdjustment() { }
    }
    /*prevents body parts from being spawned from anyone with this trait (e.g. via surgical extraction of an organ)
     * Recipes will partially or fully bypass this effect if they are custom classes from other mods, so don't expect this to work on everything. As I said in the relevant Harmony patches' comments, total functionality for this DME is not my interest,
     * it just needs to work on conventional surgeries.*/
    public class CannotRemoveBionicsFrom : DefModExtension
    {

    }
    /*Trait DME that causes ewisott. Vanishing prevents any on-death effects from triggering (including the infliction of memories on anyone who knew them) and obviously does not leave a corpse.
     * This can be configured to also play a SoundDef sound, leave behind a ThingDef thingLeftBehind, and/or even overlay a skipgate on the pawn (bool skipgateOut; the skipgate sound will play only if sound is null and you have Royalty installed).
     * It can also be configured to disappear the pawn if the trait is removed (bool triggerOnRemoval).*/
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
    //not with the other hediff comps because of its pertinence to VOD. If the pawn has VOD from a trait, this triggers it while the pawn is downed.
    public class Hediff_VanishOnDownedToo : HediffWithComps
    {
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.pawn.Downed)
            {
                TraitModExtensionUtility.TryVanishPawn(this.pawn);
            }
        }
    }
    //traits w/ this DME are added at game initialization to the list of conceited traits
    public class ConceitedTrait : DefModExtension
    {

    }
    //A pawn with a UBD trait ignores stat maluses from StatPart_Glow for being in the dark (the same vanilla effect as the Dark Vision gene or Darkness meme)
    public class UnaffectedByDarkness : DefModExtension
    {

    }
    /*Can be added to any trait to prevent it from being easily removed by various effects (e.g. the Excise Trait psycast from HAT or the “brainwash” item from Cybernetic Organism and Neural Network).
     * This is intended to be applied to traits that aren’t “really” traits, e.g. VFE Pirates' Warcasket or Shellcasket, or should otherwise not be subject to regular trait rules.
     * Unlike CannotRemoveBionicsFrom, I AM willing to make patches specifically to enforce the sovereignty of ExciseTraitExempt, but I don't know every mod out there that exists, and mods that are neither popular nor interesting to me will probably not be patched.*/
    public class ExciseTraitExempt : DefModExtension
    {
        public ExciseTraitExempt()
        {
        }
    }
    public static class TraitModExtensionUtility
    {
        //can be patched to prevent the acquisition of any TGS when it would normally be gained
        public static bool ShouldNotGrantTraitStuff(Pawn pawn, TraitSet traitSet, Trait trait)
        {
            return false;
        }
        /*right after the game starts, this is called to sort out ALL extant pawns' TGS. This is to deal with Character Editor's... character editorness, basically.
         the idea was to run this every time you open a save file, but my thought is that TGS is rarely accidentally sidestepped in the normal course of play, so there's no real point to incurring such a cost on load.*/
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
        //call this whenever you want to enforce a pawn's TGS. It calls all the following TGS-related methods as necessary
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
                            }
                            else if (cRTG.Props.forcingGenes != null && cRTG.Props.forcingGenes.Count > 0 && ModsConfig.BiotechActive && p.genes != null)
                            {
                                foreach (GeneDef gd in cRTG.Props.forcingGenes)
                                {
                                    if (HautsMiscUtility.AnalogHasActiveGene(p.genes, gd))
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
                        }
                        else if (ModsConfig.BiotechActive && comp.Props.forcingGenes != null && comp.Props.forcingGenes.Count > 0 && p.genes != null)
                        {
                            foreach (GeneDef gd in comp.Props.forcingGenes)
                            {
                                if (HautsMiscUtility.AnalogHasActiveGene(p.genes, gd))
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
                    TraitModExtensionUtility.AddTraitGrantedStuff(shouldAddHediffs, t, p);
                }
            }
        }
        public static void AddTraitGrantedStuff(bool shouldAddHediffs, Trait t, Pawn pawn)
        {
            TraitGrantedStuff tgs = t.def.GetModExtension<TraitGrantedStuff>();
            if (tgs != null)
            {
                if (shouldAddHediffs)
                {
                    TraitModExtensionUtility.AddHediffsFromTrait(t, tgs, pawn);
                }
                TraitModExtensionUtility.AddAbilitiesFromTrait(t, tgs, pawn);
                if (pawn.GuestStatus == GuestStatus.Prisoner && tgs.prisonerResolveFactor != null)
                {
                    pawn.guest.resistance *= tgs.prisonerResolveFactor.TryGetValue(t.Degree);
                    pawn.guest.will *= tgs.prisonerResolveFactor.TryGetValue(t.Degree);
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
                        }
                        else
                        {
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
                if (tgs.grantedVEFAbilities.ContainsKey(t.Degree))
                {
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
                TraitModExtensionUtility.VanishPawnInner(pawn, vod.thingLeftBehind, vod.sound, vod.skipgateOut);
                return;
            }
            TraitGrantedStuff tgs = t.def.GetModExtension<TraitGrantedStuff>();
            if (tgs != null)
            {
                TraitModExtensionUtility.RemoveHediffsFromTrait(t, tgs, pawn);
                TraitModExtensionUtility.RemoveAbilitiesFromTrait(t, tgs, pawn);
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
        //Harmony sews this in right before a pawn with VOD dies, causing them to, well, vanish
        public static bool TryVanishPawn(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.TraitsSorted)
                {
                    VanishOnDeath vod = t.def.GetModExtension<VanishOnDeath>();
                    if (vod != null)
                    {
                        TraitModExtensionUtility.VanishPawnInner(pawn, vod.thingLeftBehind, vod.sound, vod.skipgateOut);
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
                }
                else if (skipgateOut && ModsConfig.RoyaltyActive)
                {
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
        //add to ETE list/check if something is in there
        public static void AddExciseTraitExemption(TraitDef def)
        {
            TraitModExtensionUtility.exciseTraitExemptions.Add(def);
        }
        public static bool IsExciseTraitExempt(TraitDef def, bool includeSexualities = false)
        {
            if (TraitModExtensionUtility.exciseTraitExemptions.Contains(def))
            {
                return true;
            }
            if (includeSexualities && def.exclusionTags.Contains("SexualOrientation"))
            {
                return true;
            }
            return false;
        }
        private static readonly List<TraitDef> exciseTraitExemptions = new List<TraitDef>() { };
    }
}
