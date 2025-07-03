using FactionLanguages;
using FactionLanguages.Utilities;
using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HautsF_RimLangs
{
    [StaticConstructorOnStartup]
    public class HautsF_RimLangs
    {
        private static readonly Type patchType = typeof(HautsF_RimLangs);
        static HautsF_RimLangs()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.rimlanguages");
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.LearnLanguage)),
                           postfix: new HarmonyMethod(patchType, nameof(Hauts_LearnLanguagePostfix)));
        }
        public static void Hauts_LearnLanguagePostfix(Pawn pawn, Pawn other, float power)
        {
            if (pawn.Faction != Faction.OfPlayerSilentFail)
            {
                return;
            }
            Dialect dialect;
            if (other == null)
            {
                float lla = StatDefOfLocal.LanguageLearningAbility.Worker.IsDisabledFor(pawn) ? 0.1f : pawn.GetStatValue(StatDefOfLocal.LanguageLearningAbility);
                IEnumerable<Dialect> unlearnedDialects = GameComponent_FactionLanguages.CurrentGame.LanguageDatabase.AllDialects.Where((Dialect d) => !d.IsLearned);
                if (unlearnedDialects.Count() > 0)
                {
                    dialect = unlearnedDialects.RandomElement();
                    if (dialect != null)
                    {
                        if (!dialect.Parent.IsLearned)
                        {
                            float learnXp = dialect.Parent.Def.GetBaseCost() * power * lla/(LanguageUtility.Settings.languageLearnDifficulty* dialect.Def.GetDifficulty(dialect.TechLevel) * dialect.Parent.Def.GetDifficulty());
                            if (learnXp > 0f)
                            {
                                dialect.Parent.AddXp(learnXp, pawn);
                            }
                        } else {
                            if (!dialect.IsLearned)
                            {
                                float learnXp = dialect.Def.GetBaseCost() * power * lla/(LanguageUtility.Settings.languageLearnDifficulty* dialect.Def.GetDifficulty(dialect.TechLevel) * dialect.Parent.Def.GetDifficulty());
                                if (learnXp > 0f)
                                {
                                    dialect.AddXp(learnXp, pawn);
                                }
                            }
                        }
                    }
                }
                return;
            }
            if (GameComponent_FactionLanguages.CurrentGame.LanguageDatabase.TryGetLanguage(other.Faction, out dialect))
            {
                float factor = 0.1f;
                if (!StatDefOfLocal.LanguageLearningAbility.Worker.IsDisabledFor(pawn))
                {
                    factor = LanguageUtility.CalculateLanguageLearnFactor(pawn, other);
                }
                if (!dialect.Parent.IsLearned)
                {
                    float learnXp = dialect.Parent.Def.GetBaseCost() * power * factor;
                    if (learnXp > 0f)
                    {
                        dialect.Parent.AddXp(learnXp, pawn);
                        if (other.Spawned)
                        {
                            MoteMaker.ThrowText(other.Position.ToVector3Shifted(), other.Map, "FactionLanguages.JobDriver_LearnPrisonerLanguage.AddXpForLanguage".Translate(dialect.Parent.Name, (int)learnXp), -1f);
                        }
                    }
                } else {
                    if (!dialect.IsLearned)
                    {
                        float learnXp = dialect.Def.GetBaseCost() * power * factor;
                        if (learnXp > 0f)
                        {
                            dialect.AddXp(learnXp, pawn);
                            if (other.Spawned)
                            {
                                MoteMaker.ThrowText(other.Position.ToVector3Shifted(), other.Map, "FactionLanguages.JobDriver_LearnPrisonerLanguage.AddXpForDialect".Translate(dialect.Name, (int)learnXp), -1f);
                            }
                        }
                    }
                }
            }
        }
    }
}
