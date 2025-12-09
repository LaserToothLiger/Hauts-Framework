using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using VPE_Neurophage;
using UnityEngine;
using System.Diagnostics.Tracing;
using HautsFramework;

namespace Hauts_Neurophage
{
    [StaticConstructorOnStartup]
    public class Hauts_Neurophage
    {
        private static readonly Type patchType = typeof(Hauts_Neurophage);
        static Hauts_Neurophage()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsVPEneurophage");
            harmony.Patch(AccessTools.Method(typeof(Hediff_NeuralControl), nameof(Hediff_NeuralControl.TransferFromAToB)),
                          prefix: new HarmonyMethod(patchType, nameof(Hauts_TransferFromAToBPrefix)));
        }
        public static bool Hauts_TransferFromAToBPrefix(Pawn source, Pawn target, Hediff_NeuralControl hediff)
        {
            hediff.originalFaction = target.Faction;
            bool ideologyActive = ModsConfig.IdeologyActive;
            Pawn_StoryTracker story2 = source.story;
            Pawn_StoryTracker story = target.story;
            if (ideologyActive)
            {
                hediff.originalFavoriteColor = target.story.favoriteColor;
                if (story.favoriteColor != story2.favoriteColor)
                {
                    story.favoriteColor = story2.favoriteColor;
                }
                hediff.originalIdeo = target.ideo != null ? target.ideo.Ideo : null;
                if (target.Ideo != null && source.Ideo != null && target.Ideo != source.Ideo)
                {
                    target.ideo.SetIdeo(source.Ideo);
                }
            }
            if (target.Faction != source.Faction)
            {
                target.SetFaction(source.Faction, null);
            }
            if (target.skills != null)
            {
                foreach (SkillRecord skillRecord in target.skills.skills)
                {
                    hediff.originalSkillValues.Add(skillRecord.def, skillRecord.Level);
                    hediff.originalSkillPassions.Add(skillRecord.def, skillRecord.passion);
                }
            }
            if (target.story != null && source.story != null)
            {
                foreach (BackstoryDef backstoryDef in target.story.AllBackstories)
                {
                    hediff.originalBackstories.Add(backstoryDef.slot, backstoryDef);
                    if (backstoryDef.slot == BackstorySlot.Childhood)
                    {
                        target.story.Childhood = source.story.Childhood;
                    } else if (backstoryDef.slot == BackstorySlot.Adulthood) {
                        target.story.Adulthood = source.story.Adulthood;
                    }
                }
                for (int i = target.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    if (!HautsUtility.IsExciseTraitExempt(target.story.traits.allTraits[i].def))
                    {
                        hediff.originalTraits.Add(target.story.traits.allTraits[i].def);
                        target.story.traits.RemoveTrait(target.story.traits.allTraits[i]);
                    }
                }
                for (int i = source.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    if (!HautsUtility.IsExciseTraitExempt(source.story.traits.allTraits[i].def))
                    {
                        target.story.traits.GainTrait(source.story.traits.allTraits[i]);
                    }
                }
            }
            if (target.skills != null && source.skills != null)
            {
                target.skills.Notify_SkillDisablesChanged();
                Pawn_SkillTracker skills = source.skills;
                foreach (SkillRecord skillRecord2 in skills.skills)
                {
                    SkillRecord skill = target.skills.GetSkill(skillRecord2.def);
                    skill.Level = skillRecord2.Level;
                    skill.passion = skillRecord2.passion;
                    skill.xpSinceLastLevel = skillRecord2.xpSinceLastLevel;
                    skill.xpSinceMidnight = skillRecord2.xpSinceMidnight;
                }
            }
            if (target.workSettings != null)
            {
                target.workSettings.Notify_DisabledWorkTypesChanged();
            }
            return false;
        }
    }
}
