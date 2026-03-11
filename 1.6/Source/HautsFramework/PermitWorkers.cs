using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Grammar;
using Verse.Sound;

namespace HautsFramework
{
    /*add this to a permitdef to give it way more XML-specifiable parameters. What those parameters do depend on the permit worker, with examples below.*/
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
                }
                else if (Find.AnyPlayerHomeMap != null)
                {
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
            }
            else if (Find.AnyPlayerHomeMap != null)
            {
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
        public IntRange marketValueLimits = new IntRange(0, 9999999);
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
        public FloatRange bodySizeCapRange = new FloatRange(-1f, 999f);
        public List<PawnKindDef> disallowedPawnKinds;
        public bool startsTamed;
        //drop stuff
        public FloatRange gambaFactorRange;
        public bool gambaDropPodSoNotInstant = false;
        public IntRange gambaReturnDelay;
        public string returnMessage;
        public string extraString;
    }
    /*|||||All of the following permit workers have the following virtual methods:
     * OverridableFillAidOption: this is like base.FillAidOption, and it governs what the permit looks like in the permit float menu. Basically the only difference is that it is NOT protected.
     * IsFactionHostileToPlayer: returns if ‘faction’ is hostile to the player faction by default; governs whether the permit should be disabled due to hostility w the faction that grants it.
     * DoOtherEffect: called when the permit goes on cooldown, but is empty by default.|||||
     * 
     * |||||Where a comment describes a field below (the way I described fields for ability or hediff comps), that field is from the permit's PermitMoreEffects.|||||
     * 
     * re: DropBook, you might be surprised to hear that this permit worker drops a book. Very shocking. I know. A regular DropResources worker creates a book with no title or description. This can give default title/desc, or override that.
     * If you want to drop a book that uses the PromoteIdeo book outcome doer, you should go check out the Ideology-exclusive derivative of this permit worker (over in the Ideology folder).
     *  bookDef: determines what kind of books are dropped.
     *  bookTitlePack: if extant, replaces the usual rules for the book’s title generation with these.
     *  bookDescPack: if extant, replaces the usual rules for the book’s description generation with these.
     *  bookFixedPubDate: if non-negative, the titles/descriptions of books created by DropBook consider the book to have been published on this date.
     *  bookSuperQualityChance: if Rand.Value < this value, books are generated using “Super” quality determination, as opposed to “Trader Stock” quality determination. Super is a vanilla setting for quality generation that trends towards higher qualities, while Trader Stock favors normal/good items.
     *  ExtraBookModification: code injected here takes place after the book is made and its quality is assigned, but before any titling/descripting processes occurs.
     *  ExtraTitle|DescGrammarRules: can be used to add extra grammar rules to book title|description generation.
     *  ItemStackCount: using the permit’s PME and the caller as the Pawn, determines how many books are dropped. By default, it returns the PME’s phenomenonCount.*/
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
                for (int i = 0; i < this.ItemStackCount(pme, caller); i++)
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
            this.ExtraBookModification(book, pme);
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
                        RoyalTitlePermitWorker_DropBook.subjects.Add(new HautsMiscUtility.BookSubjectSymbol
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
                grammarRequest2.Includes.Add(pme.bookTitlePack ?? book.BookComp.Props.nameMaker);
                typeof(Book).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, GenText.CapitalizeAsTitle(GrammarResolver.Resolve("title", grammarRequest2, null, false, null, null, null, true)).StripTags());
            }
            GrammarRequest grammarRequest3 = grammarRequest;
            grammarRequest3.Includes.Add(pme.bookDescPack ?? book.BookComp.Props.descriptionMaker);
            this.ExtraDescGrammarRules(book, ref grammarRequest3);
            grammarRequest3.Includes.Add(RulePackDefOf.TalelessImages);
            grammarRequest3.Includes.Add(RulePackDefOf.ArtDescriptionRoot_Taleless);
            grammarRequest3.Includes.Add(RulePackDefOf.ArtDescriptionUtility_Global);
            typeof(Book).GetField("descriptionFlavor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, GrammarResolver.Resolve("desc", grammarRequest3, null, false, null, null, null, true).StripTags());
            typeof(Book).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, this.GenerateFullDescription(book));
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, false);
            return book;
        }
        private static void AppendRulesForSubject(List<HautsMiscUtility.BookSubjectSymbol> subjects, List<Verse.Grammar.Rule> rules, Dictionary<string, string> constants, string postfix, int i)
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
        private static List<HautsMiscUtility.BookSubjectSymbol> subjects = new List<HautsMiscUtility.BookSubjectSymbol>();

    }
    /*As the vanilla DropResources permit worker, but is capable of assigning specified quality levels and stuffing. If used to drop buildings, they will drop in a minified state. Fully charges any batteries it drops.
     * Does not require PME (although derivatives can be made to interact with one), as this information can all technically be encoded in vanilla permit fields - vanilla permit workers were just never made to use the full scope of their functionality, for some reason
     * ItemStackCount: determines how many things for each ThingDefCountClass are dropped. By default, it returns the count from the TDCC.*/
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
                }
                else
                {
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
                this.DoOtherEffect(this.caller, this.faction);
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
                }
                else
                {
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
    /*As the vanilla DropResources permit worker, but is configured to drop items randomly selected from a specified ThingCategoryDef.
     * extraNumber: items created by this permit have a quality within this range (0 is awful, 2 is normal, 6 is legendary). Note that if you choose it to be a range, the upper end needs to be 1 higher than the desired maximum quality.
     * allRandomOutcomesMustBeSamePerUse: if true, all things created by a single casting of this permit will be of the same valid thing def (as opposed to each one being any of the valid thing defs
     *   The valid thing defs for the permit to drop are defined by the following fields…
     *   targetableThings: if specified, the generated items must be one of these items. Otherwise, the generated items must be selected from those that meet the criteria set forth by all the following:
     *   thingCategories: if specified, the generated items must be from one of these categories…
     *   forbiddenThingCategories: …and NOT one of these.
     *   tradeTags: if specified, the generated items must have one of these trade tags…
     *   forbiddenTradeTags: …and NOT one of these.
     *   max|minTechLevelInCategory: items created by this permit will never be of a tech level above|below this level.
     *   marketValueLimits: items created by this permit must have a market value within this range.
     *   ItemStackCount: determines how many items are dropped. By default, it returns a random number from the PME’s numFromCategory.*/
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
                    }
                    else
                    {
                        DefDatabase<ThingDef>.AllDefsListForReading.TryRandomElement((ThingDef x) => this.IsValidItemOption(x, pme), out oneThing);
                    }
                }
                for (int i = 0; i < this.ItemStackCount(pme, this.caller); i++)
                {
                    ThingDef randomThing;
                    if (oneThing != null)
                    {
                        randomThing = oneThing;
                    }
                    else if (!pme.targetableThings.NullOrEmpty())
                    {
                        randomThing = pme.targetableThings.RandomElement();
                    }
                    else
                    {
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
                        }
                        else
                        {
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
                    this.DoOtherEffect(this.caller, this.faction);
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
                for (int i = 0; i < this.ItemStackCount(pme, caller); i++)
                {
                    ThingDef randomThing;
                    if (!pme.targetableThings.NullOrEmpty())
                    {
                        randomThing = pme.targetableThings.RandomElement();
                    }
                    else
                    {
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
                        }
                        else
                        {
                            CaravanInventoryUtility.GiveThing(caravan, thing);
                        }
                        Messages.Message("MessagePermitTransportDropCaravan".Translate(faction.Named("FACTION"), caller.Named("PAWN")), caravan, MessageTypeDefOf.NeutralEvent, true);
                        caller.royalty.GetPermit(this.def, faction).Notify_Used();
                        if (!free)
                        {
                            caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                        }
                        this.DoOtherEffect(this.caller, this.faction);
                    }
                }
            }
        }
        public Faction faction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    /*Causes a condition randomly drawn from conditionDefs, for a random number of ticks within conditionDuration. It occurs on the permit-holder's map.
     * screenShake: shakes the screen on use
     * soundDef: plays this sound on use
     * onUseMessage: displays this message in the top left corner on use. {FACTION_name} = the permit’s faction's name*/
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
                this.DoOtherEffect(caller, faction);
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
    }
    /*Generates a specified number of quests and/or incidents from the questScriptDefs and/or incidentDefs lists
     * GetIncidentPoints (a PME method - not indigenous to this worker): quests and incidents generated by this permit have this many points.
     * incidentDelay: if specified, created incidents are added to the storyteller’s incident queue with a tick delay randomly chosen from w/in this range.
     * incidentUsesPermitFaction: if false, the generated incident’s faction is the one that has issued the permit; if true, it is instead chosen randomly from a list of factions that meet…
     * FactionCanBeGroupSource: …these criteria. Faction is the evaluated faction, Map is the caller’s map (if any), and bool is whether the faction should be treated as “desperate” (a holdover from the code of pawn-arrival incidents which allows pawn groups to arrive outside of their normal allowed temperature ranges).
     * NumQuestsToGenerate: determines how many quests and/or incidents are generated per use. By default, it returns the PME’s questCount.*/
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
                for (int i = 0; i < this.NumQuestsToGenerate(pme, caller, faction); i++)
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
                        }
                        else
                        {
                            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                            if (!quest.hidden && questDef.sendAvailableLetter)
                            {
                                QuestUtility.SendLetterQuestAvailable(quest);
                            }
                        }
                        done = true;
                    }
                    else
                    {
                        IncidentDef incidentDef = pme.incidentDefs.RandomElement();
                        Faction funcFaction = pme.incidentUsesPermitFaction ? faction : this.CandidateFactions(caller.Map ?? null, false).RandomElement();
                        Faction raidFaction = Find.FactionManager.AllFactionsListForReading.Where((Faction f) => !f.IsPlayer && f.HostileTo(Faction.OfPlayerSilentFail) && !f.defeated && !f.temporary && (caller.Map != null || (f.def.allowedArrivalTemperatureRange.Includes(caller.Map.mapTemperature.OutdoorTemp) && f.def.allowedArrivalTemperatureRange.Includes(caller.Map.mapTemperature.SeasonalTemp)))).RandomElement();
                        IncidentParms incidentParms = new IncidentParms
                        {
                            forced = true,
                            points = parms.points,
                            faction = funcFaction,
                            raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                            raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                            traderKind = (funcFaction != null && !funcFaction.IsPlayer && !funcFaction.def.caravanTraderKinds.NullOrEmpty() && funcFaction.def.caravanTraderKinds.ContainsAny((TraderKindDef tkd2) => tkd2.requestable) && funcFaction.AllyOrNeutralTo(Faction.OfPlayerSilentFail)) ? funcFaction.def.caravanTraderKinds.Where((TraderKindDef tkd) => tkd.requestable).RandomElement() : null
                        };
                        if (caller.Map != null)
                        {
                            incidentParms.target = caller.Map;
                        }
                        else if (Find.AnyPlayerHomeMap != null)
                        {
                            incidentParms.target = Find.AnyPlayerHomeMap;
                        }
                        else if (Find.WorldObjects.Caravans.Count > 0)
                        {
                            incidentParms.target = Find.WorldObjects.Caravans.RandomElement();
                        }
                        else
                        {
                            incidentParms.target = Find.World;
                        }
                        if (pme.incidentDelay != null)
                        {
                            Find.Storyteller.incidentQueue.Add(incidentDef, Find.TickManager.TicksGame + pme.incidentDelay.RandomInRange, incidentParms, 240000);
                        }
                        else if (incidentDef.Worker.CanFireNow(incidentParms))
                        {
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
                    this.DoOtherEffect(caller, faction);
                }
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    /*Alters the stack size of a target item by a random amount w/in a specified range. Can be configured to occur instantly, or else to vanish the item first and then return it later in a drop pod.
     * targetableThings: can only target Things from this list.
     * extraNumber: MUST BE DECLARED. Regardless of the size of the targeted item stack, the permit only affects this many items from the stack. All mentions of the ‘targeted stack’ hereafter refer to just that substack, if applicable. I use this to avoid stupid broken bullshit you could pull off with a mod like OgreStack.
     * gambaFactorRange: multiplies the stack size of the targeted stack by a random value within this range.
     * gambaDropPodSoNotInstant: if false, the effect occurs instantly; otherwise, it vanishes the targeted stack and causes it to return in drop pods after…
     * gambaReturnDelay: …a random number of ticks from within this range. (this is what the Hauts_InvestmentReturn incident def is)
     * invalidTargetMessage: this message is displayed in the top left corner if an invalid target is selected.
     * onUseMessage: displays this message in the top left corner on use. {FACTION_name} refers to the permit’s faction.*/
    public class RoyalTitlePermitWorker_MultiplyItemStack : RoyalTitlePermitWorker_Targeted, ITargetingSource
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
                }
                else
                {
                    if (pme.targetableThings != null)
                    {
                        if (lti.Thing != null && pme.targetableThings.Contains(lti.Thing.def))
                        {
                            return AcceptanceReport.WasAccepted;
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
            if (pme != null && target.Thing != null && pme.targetableThings.Contains(target.Thing.def))
            {
                this.Invest(target.Thing, this.calledFaction);
            }
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
            this.targetingParameters.canTargetLocations = false;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
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
                        Thing thing2 = ThingMaker.MakeThing(thing.def, thing.Stuff);
                        thing2.stackCount = amountTaken;
                        parms.gifts = new List<Thing>
                        {
                            thing2
                        };
                    }
                    else
                    {
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
                }
                else
                {
                    float comeOn = amountTaken * pme.gambaFactorRange.RandomInRange;
                    thing.stackCount -= amountTaken;
                    thing.stackCount += (int)comeOn;
                    if (thing.stackCount <= 0)
                    {
                        thing.Destroy();
                    }
                    else
                    {
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
                this.DoOtherEffect(this.caller, this.calledFaction);
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
    /*Targets a pawn which meets specified criteria. Has no effect by itself; purely exists to be inherited by permit workers that actually do something. GiveHediffs is its example child, which does ewisott.
     * soundDef: plays this sound on use.
     * invalidTargetMessage: this message is displayed in the top left corner if an invalid target is selected.
     * onUseMessage: displays this message in the top left corner on use. Uses {FACTION_name} to mean the permit’s faction, and {PAWN} to refer to the pawn (using the same rules as trait tooltips, so PAWN_nameDef, PAWN_possessive, etc.).
     * CalledFaction: provides the called faction, regardless of what step you’re in of the permit’s execution.
     * IsGoodPawn: determines whether a selected pawn is a valid target.
     * GiveHediffInCaravanInner: handles what happens when used on a caravan. By default, it applies the effect to the caller pawn. Bool is whether or not the permit is free. Despite the name of this method, it does not have to grant a hediff, it's just that I originally made GiveHediffs and then later went back to make this general pawn targeter.
     * AffectPawnInner: contains nothing, but this is where to write the on-use effect of the permit.*/
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_TargetPawn : RoyalTitlePermitWorker_Targeted, ITargetingSource
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
                }
                else
                {
                    if (lti.Pawn != null && this.IsGoodPawn(lti.Pawn))
                    {
                        return AcceptanceReport.WasAccepted;
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
                if (target.Pawn != null && this.IsGoodPawn(target.Pawn))
                {
                    this.AffectPawn(target.Pawn, this.calledFaction);
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
            this.GiveHediffInCaravanInner(caller, faction, free, caravan);
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
            this.targetingParameters.canTargetLocations = false;
            this.targetingParameters.canTargetSelf = true;
            this.targetingParameters.canTargetPawns = true;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = false;
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
                this.AffectPawnInner(pme, pawn, faction);
                this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(this.caller, faction);
            }
        }
        public virtual void DoOtherEffect(Pawn caller, Faction faction)
        {

        }
        public Faction CalledFaction
        {
            get
            {
                return this.CalledFaction;
            }
        }
        public virtual void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
        }
        private Faction calledFaction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    //IsGoodPawn returns true only if AllowCheckPMEs passes (see MiscUtility.cs). Grants all hediffs in the PME's hediffs field.
    public class RoyalTitlePermitWorker_GiveHediffs : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            return HautsMiscUtility.AllowCheckPMEs(pme, pawn.kindDef);
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
    /*As the vanilla DropResources permit worker, but is configured to drop pawns instead.
     * Differs from the base game’s RoyalTitlePermitWorker_CallAid in that the pawns do not spawn as part of a friendly raid (thus lacking morale, the “patrol and attack all nearby enemies” AI, and leaving-after-a-duration behavior)
     * Differs from RoyalTitlePermitWorker_CallLaborers in that the pawns do not spawn as part of a hospitality quest (thus they don’t join your faction temporarily and you aren’t required to keep them safe)
     * Differs from both in that the pawns can be drawn from multiple pawn kind defs (not just one).
     * numFromCategory: the number of pawns dropped is equal to a random number from this range.
     * allRandomOutcomesMustBeSamePerUse: if true, all pawns created by a single casting of this permit will be of the same valid kind def (as opposed to each one being any of the valid kind defs). The valid kind defs for the permit to drop are defined by the following fields…
     * allowedPawnKinds: if specified, each dropped pawn is of a random PawnKindDef from this list. Otherwise, it respects the following fields…
     * allowAnyFlesh, allowAnyNonflesh, allowMechs, allowDryads, allowInsectoids, allowEntities, allowAnimals, and allowHumanlikes as per AllowCheckPMEs (see "MiscUtilities.cs").
     * bodySizeCapRange: each dropped pawn’s kind def must be within this range.
     * minPetness: each dropped pawn’s kind def must have a petness >= this value.
     * maxWildness: if >0, each dropped pawn’s kind def cannot have a wildness >= this value.
     * needsPen: if true, each dropped pawn’s kind def must be flagged as a “Roamer” - or in more colloquial terms it must be a pawn that, if tamed, would need to be kept in a pen to prevent it from wandering away.
     * mustBePredator: if true, each dropped pawn’s kind def must be flagged as a predator.
     * marketValueLimits: if specified, each dropped pawn’s kind def must have a base market value within this range.
     * disallowedPawnKinds: if specified, each dropped pawn’s kind def CANNOT be any of the kind defs in this list.
     *   If, after all of these checks, no pawn kind defs are eligible, the pawn’s kind def will default to wildman.
     * hediffs: grants all hediffs in the list to the dropped pawns.*/
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
            if (this.OverridableFillAidOption(pawn, faction, ref text, out free))
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
                    PawnKindDef pkd = onePkd ?? this.ChoosePawnKindToDrop(pme);
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
                this.DoOtherEffect(this.caller, this.faction);
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
                    caravan.AddPawn(p, true);
                    p.SetFaction(pme.startsTamed ? caller.Faction : null);
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
        private PawnKindDef ChoosePawnKindToDrop(PermitMoreEffects pme)
        {
            PawnKindDef pawnToMake = PawnKindDefOf.WildMan;
            if (!pme.allowedPawnKinds.NullOrEmpty())
            {
                pawnToMake = pme.allowedPawnKinds.RandomElement();
            }
            else
            {
                List<PawnKindDef> possiblePawnsFromAllowBools = DefDatabase<PawnKindDef>.AllDefsListForReading.Where((PawnKindDef p) => (!pme.needsPen || p.RaceProps.Roamer) && (pme.maxWildness < 0f || p.race.GetStatValueAbstract(StatDefOf.Wildness) <= pme.maxWildness) && p.RaceProps.petness >= pme.minPetness && (!pme.mustBePredator || p.RaceProps.predator) && (pme.disallowedPawnKinds == null || !pme.disallowedPawnKinds.Contains(p)) && (pme.marketValueLimits == null || (pme.marketValueLimits.min <= p.race.GetStatValueAbstract(StatDefOf.MarketValue) && pme.marketValueLimits.max >= p.race.GetStatValueAbstract(StatDefOf.MarketValue))) && (pme.bodySizeCapRange == null || pme.bodySizeCapRange.Includes(p.RaceProps.baseBodySize)) && HautsMiscUtility.AllowCheckPMEs(pme, p)).ToList<PawnKindDef>();
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
}
