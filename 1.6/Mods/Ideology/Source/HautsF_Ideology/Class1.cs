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
            harmony.Patch(AccessTools.Method(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.SetIdeo)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsSetIdeoPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Book), nameof(Book.GenerateBook)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGenerateBookPostfix)));
        }
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
        public static void HautsSetIdeoPostfix(Pawn_IdeoTracker __instance)
        {
            MethodInfo TryQueueIdeoRemoval = typeof(IdeoManager).GetMethod("TryQueueIdeoRemoval", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!__instance.PreviousIdeos.NullOrEmpty() && Find.IdeoManager.IdeosListForReading.Contains(__instance.PreviousIdeos.Last()))
            {
                TryQueueIdeoRemoval.Invoke(Find.IdeoManager, new object[] { __instance.PreviousIdeos.Last() });
            }
        }
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
    //book comp: ideoligious tract
    public class BookOutcomeProperties_PromoteIdeo : BookOutcomeProperties
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(BookOutcomeDoerPromoteIdeo);
            }
        }
        public float newIdeoChance = 0f;
        //for making new ideos
        public float notForSpecificFactionType = 1f;
        public float chanceForPlayerFaction;
        public FactionDef forcedFactionDef;
        public List<PreceptDef> disallowedPrecepts;
        public List<MemeDef> disallowedMemes;
        public List<MemeDef> forcedMemes;
        public bool forceNoWeaponPreference;
        public List<StyleCategoryDef> styles;
        public bool initiallyHidden;
        public bool requiredPreceptsOnly;
        //book strength
        public SimpleCurve conversionPerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 0.01f),true},
                {new CurvePoint(1f, 0.01f),true},
                {new CurvePoint(2f, 0.01f),true},
                {new CurvePoint(3f, 0.01f),true},
                {new CurvePoint(4f, 0.01f),true},
                {new CurvePoint(5f, 0.01f),true},
                {new CurvePoint(6f, 0.01f),true},
            };
        public SimpleCurve reassurePerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 0.01f),true},
                {new CurvePoint(1f, 0.01f),true},
                {new CurvePoint(2f, 0.01f),true},
                {new CurvePoint(3f, 0.01f),true},
                {new CurvePoint(4f, 0.01f),true},
                {new CurvePoint(5f, 0.01f),true},
                {new CurvePoint(6f, 0.01f),true},
            };
        public ThoughtDef upsetOnFailedConversionThought;
        public SimpleCurve upsetLikelihood = new SimpleCurve
            {
                {new CurvePoint(0f, 0f),true},
                {new CurvePoint(1f, 0f),true},
                {new CurvePoint(2f, 0f),true},
                {new CurvePoint(3f, 0f),true},
                {new CurvePoint(4f, 0f),true},
                {new CurvePoint(5f, 0f),true},
                {new CurvePoint(6f, 0f),true},
            };
    }
    public class BookOutcomeDoerPromoteIdeo : BookOutcomeDoer
    {
        public new BookOutcomeProperties_PromoteIdeo Props
        {
            get
            {
                return (BookOutcomeProperties_PromoteIdeo)this.props;
            }
        }
        public override bool DoesProvidesOutcome(Pawn reader)
        {
            if (reader.Ideo != null)
            {
                return true;
            }
            return false;
        }
        public override void OnBookGenerated(Pawn author = null)
        {
            this.ValidateIdeo(author);
        }
        public void ValidateIdeo(Pawn author = null)
        {
            if (this.ideo == null)
            {
                if (author != null && author.Ideo != null && author.Spawned)
                {
                    this.ideo = author.Ideo;
                } else if (Rand.Chance(this.Props.newIdeoChance)) {
                    this.MakeNewIdeo();
                } else {
                    this.ideo = Find.IdeoManager.IdeosListForReading.RandomElement();
                }
                /*if (this.ideo != null)
                {
                    if (!Find.IdeoManager.IdeosListForReading.Contains(this.ideo))
                    {
                        Find.IdeoManager.Add(this.ideo);
                    }
                }*/
                this.ideoFoundInManager = Find.IdeoManager.IdeosListForReading.Contains(this.ideo);
            }
        }
        public void MakeNewIdeo()
        {
            Ideo ideo;
            IdeoGenerationParms parms = new IdeoGenerationParms(Rand.Chance(this.Props.chanceForPlayerFaction) ? Faction.OfPlayer.def : (this.Props.forcedFactionDef ?? DefDatabase<FactionDef>.GetRandom()), false, this.Props.disallowedPrecepts, this.Props.disallowedMemes, this.Props.forcedMemes, false, this.Props.forceNoWeaponPreference, false, false, "", this.Props.styles, null, this.Props.initiallyHidden, "", this.Props.requiredPreceptsOnly);
            if (parms.fixedIdeo)
            {
                ideo = IdeoGenerator.MakeFixedIdeo(parms);
            } else {
                ideo = IdeoGenerator.GenerateIdeo(parms);
            }
            ideo.primaryFactionColor = new Color(Rand.Value, Rand.Value, Rand.Value, 1f);
            this.ideo = ideo;
            this.ideoFoundInManager = Find.IdeoManager.IdeosListForReading.Contains(this.ideo);
        }
        public override void Reset()
        {
            this.OnBookGenerated(null);
        }
        public float ConversionPerHour
        {
            get
            {
                return this.Props.conversionPerHour.Evaluate((float)this.Quality);
            }
        }
        public float ReassurePerHour
        {
            get
            {
                return this.Props.reassurePerHour.Evaluate((float)this.Quality);
            }
        }
        public float ChanceToUpsetPerHour
        {
            get
            {
                return this.Props.upsetLikelihood.Evaluate((float)this.Quality);
            }
        }
        public float ConversionPowerFromReaderTraits(Pawn reader)
        {
            float num = 1f;
            this.ValidateIdeo();
            if (ModsConfig.IdeologyActive && reader.Ideo != null)
            {
                foreach (MemeDef memeDef in reader.Ideo.memes)
                {
                    if (!memeDef.agreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement in memeDef.agreeableTraits)
                        {
                            if (traitRequirement.HasTrait(reader))
                            {
                                num -= 0.2f;
                            }
                        }
                    }
                    if (!memeDef.disagreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement2 in memeDef.disagreeableTraits)
                        {
                            if (traitRequirement2.HasTrait(reader))
                            {
                                num += 0.2f;
                            }
                        }
                    }
                }
                foreach (MemeDef memeDef in this.ideo.memes)
                {
                    if (!memeDef.agreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement in memeDef.agreeableTraits)
                        {
                            if (traitRequirement.HasTrait(reader))
                            {
                                num += 0.2f;
                            }
                        }
                    }
                    if (!memeDef.disagreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement2 in memeDef.disagreeableTraits)
                        {
                            if (traitRequirement2.HasTrait(reader))
                            {
                                num -= 0.2f;
                            }
                        }
                    }
                }
            }
            return Math.Max(num, 0f);
        }
        public override void OnReadingTick(Pawn reader, float factor)
        {
            if (this.Parent.IsHashIntervalTick(250))
            {
                this.ValidateIdeo();
                if (reader.Ideo != null)
                {
                    float curCertainty = reader.ideo.Certainty;
                    if (reader.Ideo != this.ideo)
                    {
                        if (this.ConversionPerHour != 0f)
                        {
                            Ideo oldIdeo = reader.Ideo;
                            Precept_Role role = oldIdeo.GetRole(reader);
                            if (Rand.Chance(this.ChanceToUpsetPerHour * Math.Min(reader.GetStatValue(StatDefOf.ReadingSpeed), 1f)/10f) && !ThoughtUtility.ThoughtNullified(reader, ThoughtDefOf.FailedConvertIdeoAttemptResentment))
                            {
                                reader.needs.mood.thoughts.memories.TryGainMemory(this.Props.upsetOnFailedConversionThought, null, null);
                                this.ExtraUpsetEffect(reader,curCertainty);
                            } else {
                                if (reader.ideo.IdeoConversionAttempt(this.ConversionPerHour * reader.GetStatValue(StatDefOf.CertaintyLossFactor) * reader.GetStatValue(StatDefOf.ReadingSpeed) * this.ConversionPowerFromReaderTraits(reader) / 10f, this.ideo, true))
                                {
                                    if (this.ideo.hidden)
                                    {
                                        this.ideo.hidden = false;
                                    }
                                    if (PawnUtility.ShouldSendNotificationAbout(reader))
                                    {
                                        string letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                                        string title = this.Parent.Label;
                                        Book book = this.Parent as Book;
                                        if (book != null)
                                        {
                                            title = book.Title;
                                        }
                                        string letterText = "Hauts_LetterConvertIdeoBook_Success".Translate(title, reader.Name.ToStringShort, oldIdeo.name, this.ideo.name).Resolve();
                                        LetterDef letterDef = LetterDefOf.PositiveEvent;
                                        LookTargets lookTargets = new LookTargets(new TargetInfo[] { reader });
                                        if (role != null)
                                        {
                                            letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(reader.Named("PAWN"), role.Named("ROLE"), ideo.Named("OLDIDEO")).Resolve();
                                        }
                                        Find.LetterStack.ReceiveLetter(letterLabel, letterText, letterDef, lookTargets ?? reader, null, null, null, null, 0, true);
                                        if (!Find.IdeoManager.IdeosListForReading.Contains(this.ideo))
                                        {
                                            Find.IdeoManager.Add(this.ideo);
                                        }
                                    }
                                }
                                this.ExtraConversionEffect(reader,curCertainty,oldIdeo);
                            }
                        }
                    } else {
                        reader.ideo.OffsetCertainty(this.ReassurePerHour * reader.GetStatValue(StatDefOf.ReadingSpeed) * this.ConversionPowerFromReaderTraits(reader) / 10f);
                        this.ExtraReasurreEffect(reader,curCertainty);
                    }
                }
            }
        }
        public virtual void ExtraUpsetEffect(Pawn reader, float oldCertainty)
        {

        }
        public virtual void ExtraConversionEffect(Pawn reader, float oldCertainty, Ideo oldIdeo)
        {

        }
        public virtual void ExtraReasurreEffect(Pawn reader, float oldCertainty)
        {

        }
        public override string GetBenefitsString(Pawn reader = null)
        {
            if (this.ideo == null)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder();
            if (this.ConversionPerHour != 0f)
            {
                string text = "Hauts_IdeoBookConversion".Translate(this.ConversionPerHour.ToStringDecimalIfSmall());
                stringBuilder.AppendLine(" - " + text);
            }
            if (this.ReassurePerHour != 0f)
            {
                string text2 = "Hauts_IdeoBookCertainty".Translate(this.ReassurePerHour.ToStringDecimalIfSmall());
                stringBuilder.AppendLine(" - " + text2);
            }
            if (this.ChanceToUpsetPerHour > 0f)
            {
                string text3 = "Hauts_IdeoBookUpsetChance".Translate(this.ChanceToUpsetPerHour.ToStringByStyle(ToStringStyle.PercentTwo));
                stringBuilder.AppendLine(" - " + text3);
            }
            string text4 = "Hauts_IdeoBookMemeList".Translate(this.ideo.name.Colorize(this.ideo.Color), this.ideo.StructureMeme.label);
            if (!this.ideo.memes.NullOrEmpty())
            {
                for (int i = 0; i < this.ideo.memes.Count; i++)
                {
                    if (this.ideo.memes[i].category != MemeCategory.Structure)
                    {
                        text4 += this.ideo.memes[i].label;
                        if (this.ideo.memes.Count > i + 1)
                        {
                            text4 += ", ";
                        }
                    }
                }
            }
            stringBuilder.AppendLine(text4);
            this.ExtraEffectsStrings(ref stringBuilder);
            return stringBuilder.ToString();
        }
        public virtual void ExtraEffectsStrings(ref StringBuilder stringBuilder)
        {

        }
        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.ideoFoundInManager, "ideoFoundInManager", false, false);
            if (this.ideoFoundInManager)
            {
                Scribe_References.Look<Ideo>(ref this.ideo, "ideo", false);
            }
            else
            {
                Scribe_Deep.Look<Ideo>(ref this.ideo, "ideo", new object[] { });
            }
        }
        //title and description nonsense for the books, because Ludeon never anticipated people would want to make custom book-naming rules ig
        public void AppendDoerRules(Book parent, Pawn author, GrammarRequest common)
        {
            foreach (BookOutcomeDoer bookOutcomeDoer in parent.BookComp.Doers)
            {
                bookOutcomeDoer.Reset();
                bookOutcomeDoer.OnBookGenerated(author);
                IEnumerable<RulePack> topicRulePacks = bookOutcomeDoer.GetTopicRulePacks();
                if (topicRulePacks != null)
                {
                    foreach (RulePack rulePack in topicRulePacks)
                    {
                        GrammarRequest grammarRequest = common;
                        grammarRequest.IncludesBare.Add(rulePack);
                        List<ValueTuple<string, string>> list = new List<ValueTuple<string, string>>();
                        foreach (Rule rule in rulePack.Rules)
                        {
                            if (rule.keyword.StartsWith("subject_"))
                            {
                                list.Add(new ValueTuple<string, string>(rule.keyword.Substring("subject_".Length), GrammarResolver.Resolve(rule.keyword, grammarRequest, null, false, null, null, null, false)));
                            }
                        }
                        this.subjects.Add(new HautsUtility.BookSubjectSymbol
                        {
                            keyword = GrammarResolver.Resolve("subject", grammarRequest, null, false, null, null, null, false),
                            subSymbols = list
                        });
                    }
                }
            }
        }
        public void AppendRulesForSubject(List<HautsUtility.BookSubjectSymbol> subjects, List<Rule> rules, Dictionary<string, string> constants, string postfix, int i)
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
        public string GenerateFullDescription(Book parent)
        {
            StringBuilder stringBuilder = new StringBuilder();
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(parent, false);
            string title = (string)typeof(Book).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(parent);
            string descriptionFlavor = (string)typeof(Book).GetField("descriptionFlavor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(parent);
            stringBuilder.AppendLine(title.Colorize(ColoredText.TipSectionTitleColor) + GenLabel.LabelExtras(parent, false, true) + "\n");
            stringBuilder.AppendLine(descriptionFlavor + "\n");
            if (parent.MentalBreakChancePerHour > 0f)
            {
                stringBuilder.AppendLine(string.Format(" - {0}: {1}", "BookMentalBreak".Translate(), "PerHour".Translate(parent.MentalBreakChancePerHour.ToStringPercent("0.0"))));
            }
            foreach (BookOutcomeDoer bookOutcomeDoer in parent.BookComp.Doers)
            {
                string benefitsString = bookOutcomeDoer.GetBenefitsString(null);
                if (!string.IsNullOrEmpty(benefitsString))
                {
                    if (bookOutcomeDoer.BenefitDetailsCanChange(null))
                    {
                        typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(parent, true);
                    }
                    stringBuilder.AppendLine(benefitsString);
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
        public Ideo ideo;
        public bool ideoFoundInManager;
        public List<HautsUtility.BookSubjectSymbol> subjects = new List<HautsUtility.BookSubjectSymbol>();
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropIdeoBookForCaller : RoyalTitlePermitWorker_DropBook
    {
        public override void ExtraBookModification(Book book, PermitMoreEffects pme)
        {
            if (Rand.Chance(pme.extraNumber.RandomInRange))
            {
                foreach (BookOutcomeDoer bod in book.BookComp.Doers)
                {
                    if (bod is BookOutcomeDoerPromoteIdeo bodpi && this.caller != null && this.caller.Ideo != null)
                    {
                        bodpi.ideo = this.caller.Ideo;
                        break;
                    }
                }
            }
        }
        public override void ExtraDescGrammarRules(Book book, ref GrammarRequest grammarRequest)
        {
            book.BookComp.TryGetDoer<BookOutcomeDoerPromoteIdeo>(out BookOutcomeDoerPromoteIdeo bodpi);
            if (bodpi != null)
            {
                List<RulePack> memeRulePack = new List<RulePack>();
                if (bodpi.ideo != null && !bodpi.ideo.memes.NullOrEmpty())
                {
                    memeRulePack.AddRange(bodpi.ideo.memes.Select((MemeDef md) => md.generalRules).ToList<RulePack>());
                }
                grammarRequest.IncludesBare.AddRange(memeRulePack);
            }
        }
        public override void ExtraTitleGrammarRules(Book book, ref GrammarRequest grammarRequest)
        {
            book.BookComp.TryGetDoer<BookOutcomeDoerPromoteIdeo>(out BookOutcomeDoerPromoteIdeo bodpi);
            if (bodpi != null)
            {
                List<RulePack> memeRulePack = new List<RulePack>();
                if (bodpi.ideo != null && !bodpi.ideo.memes.NullOrEmpty())
                {
                    memeRulePack.AddRange(bodpi.ideo.memes.Select((MemeDef md) => md.generalRules).ToList<RulePack>());
                }
                grammarRequest.IncludesBare.AddRange(memeRulePack);
            }
        }
    }
}
