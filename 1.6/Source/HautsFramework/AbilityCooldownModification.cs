using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VEF.Abilities;
using Verse;

namespace HautsFramework
{
    /*when the hediff comp's pawn casts an ability that is within the specified ability categories or list, its cooldown is reduced by a certain magnitude
     the increasedCooldownRecovery of all the pawn's ACMs applicable to the ability are added together. This sum is added to 1, and this becomes a denominator for the ability's cooldown. Ergo
    * 100% ACM = 1/(1+1) = 50% cooldown reduction
    * 300% ACM = 1/(1+3) = 75% cooldown reduction
    etc.*/
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
        public bool affectsAllIdeoRoleAbilities = false;
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
                    return "Hauts_ACMtooltip".Translate((this.parent.Severity * this.Props.increasedCooldownRecovery).ToStringPercent());
                }
                return "Hauts_ACMtooltip".Translate(this.Props.increasedCooldownRecovery.ToStringPercent());
            }
        }
    }
    //some abilities don't require a WorkTag, but I feel like ACMs that affect abilities that DO should also affect such an ability. In such a case, this DME is applied. VIEMS compatibility in ModPatches folder contains an example.
    public class CooldownModifier_WorkTags : DefModExtension
    {
        public CooldownModifier_WorkTags()
        {

        }
        public WorkTags affectedByAnyACMwithThisWorkTag = WorkTags.None;
    }
    //cooldown reduced in proportion to how much of the specified stat the caster has
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
    /*each tick spent meditating removes bonusTicksWhileMeditating additional ticks from the cooldown
     * stopsWhileNotMeditating: provided it's on cooldown, adds 1 tick to the cooldown each tick that the ability owner is not meditating*/
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
            AbilityCooldownModifierUtility.CheckIfAbilityHasRequiredPsylink(this.parent.pawn, this.parent);
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
                        AbilityCooldownModifierUtility.SetNewCooldown(this.parent, this.parent.CooldownTicksRemaining + 1);
                    }
                }
                else
                {
                    AbilityCooldownModifierUtility.SetNewCooldown(this.parent, this.parent.CooldownTicksRemaining - this.Props.bonusTicksWhileMeditating);
                }
            }
        }
    }
    /* cooldown is immediately set to its cooldownTicksRange minimum value + (marketValueScalar * (target’s base market value - minMarketValueToScale)). Can’t go outside the bounds of the ability’s cooldownTicksRange*/
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
            int modCD = Math.Min((int)(this.parent.def.cooldownTicksRange.min + effectiveMarketValue), this.parent.def.cooldownTicksRange.max);
            AbilityCooldownModifierUtility.SetNewCooldown(this.parent, modCD);
        }
    }
    /* whenever the ability is cast, its cooldown is immediately set to (its cooldownTicksRange minimum value + (addedPerLevel*target’s psylink level) * (baseForExponent^target’s psylink level). Can’t go outside the bounds of the ability’s cooldownTicksRange
     capsOutAtLevel: psylink levels exceeding this value are treated as this value for the cooldown calculation*/
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
                if (psylinkLevel > 0 || this.Props.minCooldownIfNoPsylink)
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
    public static class AbilityCooldownModifierUtility
    {
        //you can use Harmony patches on these methods to inject additional conditions in which an ACM should apply to an ability, without resorting to transpilers (I don't know how to write transpilers and I hear bad things about them)
        public static bool ShouldLowerCooldown(RimWorld.Ability ability, HediffComp_AbilityCooldownModifier acm)
        {
            return false;
        }
        public static bool ShouldLowerCooldown(VEF.Abilities.Ability ability, HediffComp_AbilityCooldownModifier acm)
        {
            return false;
        }
        //these apply  all cooldown-modifying affects from this mod to the given ability
        public static float GetCooldownModifier(VEF.Abilities.Ability ability)
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
                            if (!shouldLowerCooldown && AbilityCooldownModifierUtility.ShouldLowerCooldown(ability, acm))
                            {
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
            return Math.Max(netACM, 0.001f);
        }
        public static float GetCooldownModifier(RimWorld.Ability ability)
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
                            if (!shouldLowerCooldown && acm.Props.abilitiesUsingThisWorkTag != 0)
                            {
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
                            if (!shouldLowerCooldown && acm.Props.affectsAllBionicAbilities)
                            {
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
                                                } else if (AbilityCooldownModifierUtility.AthenaAbilityCooldownPatch(ability, hcp)) {
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
                            if (!shouldLowerCooldown && acm.Props.affectsAllIdeoRoleAbilities && ModsConfig.IdeologyActive && ability.pawn.Ideo != null)
                            {
                                Precept_Role precept_Role = ability.pawn.Ideo.GetRole(ability.pawn);
                                if (precept_Role != null && precept_Role.Active && !precept_Role.AbilitiesFor(ability.pawn).NullOrEmpty<RimWorld.Ability>())
                                {
                                    foreach (RimWorld.Ability iab in precept_Role.AbilitiesFor(ability.pawn))
                                    {
                                        if (ability == iab)
                                        {
                                            shouldLowerCooldown = true;
                                            break;
                                        }
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
                            if (!shouldLowerCooldown && AbilityCooldownModifierUtility.ShouldLowerCooldown(ability, acm))
                            {
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
        public static void SetNewCooldown(RimWorld.Ability ability, int newCooldown, bool cantGoAboveMaxCD = true)
        {
            if (ability.def.groupDef != null)
            {
                foreach (RimWorld.Ability ab in ability.pawn.abilities.AllAbilitiesForReading)
                {
                    if (ab.def.groupDef != null && ab.def.groupDef == ability.def.groupDef)
                    {
                        AbilityCooldownModifierUtility.SetNewCooldownInner(ab, cantGoAboveMaxCD ? Math.Min(newCooldown, ability.def.groupDef.cooldownTicks) : newCooldown);
                    }
                }
            } else {
                AbilityCooldownModifierUtility.SetNewCooldownInner(ability, cantGoAboveMaxCD ? Math.Min(newCooldown, ability.def.cooldownTicksRange.max) : newCooldown);
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
        //maybe this should be in ModCompatibilityUtilities? it IS only relevant here, though. Possibly foldable into a patch for ShouldLowerCooldown
        public static bool AthenaAbilityCooldownPatch(RimWorld.Ability ability, HediffCompProperties hcp)
        {
            return false;
        }
        //while this could theoretically be used for something else, I've only ended up using it for MeditationCooldown
        public static void CheckIfAbilityHasRequiredPsylink(Pawn pawn, RimWorld.Ability parent)
        {
            if (!ModsConfig.RoyaltyActive)
            {
                pawn.abilities.RemoveAbility(parent.def);
                return;
            }
            else if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
            {
                Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                if (psylink == null)
                {
                    pawn.abilities.RemoveAbility(parent.def);
                    return;
                }
            }
            else if (pawn.GetPsylinkLevel() <= 0)
            {
                pawn.abilities.RemoveAbility(parent.def);
                return;
            }
        }
    }
}
