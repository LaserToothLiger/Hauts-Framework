using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HautsFramework
{
    /*|||||Go read the user manual, I'm not typing that all out again. In short, a colonist with >0 pilfering stealth can be told to steal random items from a pawn's inventory (hereafter "pickpocketing")
     * or steal items (either totally random, or of one or more specified categories) from a settlement they are visiting in a caravan (hereafter "burglary").
     * Max pilfering value caps the net market value of items that can be stolen in one go.
     * Pilfering can fail if the target's "alert level" exceeds the pilferer's stealth in a random check. Stealth > 1 + alert level is sufficient to guarantee success, stealth < alert level guarantees failure.
     * Settlements' alert levels are handled via a faction comp (a base level determined by its faction's tech level, plus decaying bonuses incurred whenever that faction gets burgled).
     * Pawns' alert level is a stat. A hidden hediff is added to a pawn that's been pickpocketed from, increasing that stat but decaying over time.
     * Fail badly enough, you can incur goodwill penalties.|||||
     * 
     * Setup for the relevant stats.
     * this skill need only works if the mod option to scale pilfering stealth w/ Social skill is on*/
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
    /*pilfering stealth…
     * …has a minimum value if you're invisible
     * …scales with a lack of the multiplyByLackOf stats (though "1 - that stat" can't go below the minimumLackOf value)
     * Yes, this is less complicated than it used to be, back when this was a hidden function of the Skulker trait rather than a generalized system.*/
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
                val /= Math.Max(0.01f, num);
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
                    descKey += "Hauts_StatWorkerLackOfFactor".Translate(sd.LabelCap, this.minimumLackOf.ToStringByStyle(ToStringStyle.FloatTwo)) + ": " + this.LackOfFactor(pawn, sd).ToStringByStyle(ToStringStyle.FloatTwo) + "\n";
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
    /*max pilfering value is divided by a specified value*/
    public class StatPart_PilferingYield : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            val /= Math.Max(this.divideBy,0.001f);
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
    /*think like a thief to catch one, right? Alert level scales w/ pawns' pilfering stealth, but it can't just be a straight statFactor since pilfering stealth is, by default, 0. That would make 99% of pawns always pickpocketable.*/
    public class StatPart_PawnAlertLevel : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null)
            {
                return;
            }
            val *= 1f + pawn.GetStatValue(HautsDefOf.Hauts_PilferingStealth);
        }
        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }
    }
    /*faction comp that handles settlements' alert level. These mostly do what they say on the tin, in conjunction with the abbreviated explanation I gave up above.
     * if you wanted to, you could specify faction defs in specificFactionMinAlertLevels who would have a unique alert level, overruling what they'd usually get from their tech level.
     * see FactionCompDef.xml for more comments*/
    public class HautsFactionCompProperties_BurglaryResponse : HautsFactionCompProperties
    {
        public HautsFactionCompProperties_BurglaryResponse()
        {
            this.compClass = typeof(HautsFactionComp_BurglaryResponse);
        }
        public float initialAlertLevel;
        public Dictionary<FactionDef, float> specificFactionMinAlertLevels;
        public Dictionary<TechLevel, float> minAlertLevelPerTechLevel;
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
                    this.currentAlertLevel = Math.Max(this.currentAlertLevel - this.Props.alertDecayPerDay, Math.Max(this.Props.advancedDecayThreshold, this.currentAlertLevel - (this.currentAlertLevel * (this.Props.advancedDecayPerDayPct / 24f))));
                }
                else
                {
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
    /*Burglary, how it works. Initiate either with a right-click float action menu option, or by pressing the Burglarize gizmo that appears when a pilfering-capable caravan is at a settlement (handled via Harmony patches)*/
    public class CaravanArrivalAction_BurgleSettlement : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "Hauts_BurgleIcon".Translate() + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " " + PilferingSystemUtility.SettlementAlertLevel(this.settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")";
            }
        }
        public override string ReportString
        {
            get
            {
                return "Hauts_ActivityPilfering".Translate(this.settlement.Label + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " " + PilferingSystemUtility.SettlementAlertLevel(this.settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")");
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
                PilferingSystemUtility.Burgle(caravan, this.settlement);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Settlement>(ref this.settlement, "settlement", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, Settlement settlement)
        {
            return settlement != null && settlement.Spawned && settlement.Visitable && PilferingSystemUtility.HasAnyBurglars(caravan);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, Settlement settlement)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_BurgleSettlement>(() => CaravanArrivalAction_BurgleSettlement.CanVisit(caravan, settlement), () => new CaravanArrivalAction_BurgleSettlement(settlement), "Hauts_BurgleIcon".Translate() + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " " + PilferingSystemUtility.SettlementAlertLevel(settlement).ToStringByStyle(ToStringStyle.FloatOne) + ")", caravan, settlement.Tile, settlement, null);
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
                        }
                        else
                        {
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
                                }
                                else
                                {
                                    TaggedString message = "Hauts_BurgleOutcome2".Translate();
                                    foreach (Thing t in this.thingsStolen)
                                    {
                                        this.settlement.trader.GetDirectlyHeldThings().Remove(t);
                                        t.PreTraded(TradeAction.PlayerBuys, this.burglars.RandomElement(), this.settlement);
                                        if (t is Pawn pawnoff)
                                        {
                                            this.caravan.AddPawn(pawnoff, true);
                                        }
                                        else
                                        {
                                            CaravanInventoryUtility.GiveThing(this.caravan, t);
                                        }
                                        alertRaise += t.MarketValue * br.Props.alertGainPerMarketValueStolen;
                                    }
                                    LookTargets toLook = new LookTargets(this.caravan);
                                    ChoiceLetter winLetter = LetterMaker.MakeLetter("Hauts_PilferLetter2".Translate(), message, LetterDefOf.PositiveEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(winLetter, null);
                                }
                            }
                            else
                            {
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
                }
                else
                {
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
    /*Pickpocketing, how it works. Initiate with RMB FAMO.*/
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
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Hauts_PickpocketLabel".Translate(clickedPawn.LabelShort + " (" + HautsDefOf.Hauts_PawnAlertLevel.label + " " + clickedPawn.GetStatValue(HautsDefOf.Hauts_PawnAlertLevel).ToStringByStyle(ToStringStyle.FloatOne) + ")"), action, MenuOptionPriority.InitiateSocial, null, clickedPawn, 0f, null, null, true, 0), context.FirstSelectedPawn, clickedPawn, "ReservedBy", null);
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
                    if (this.CanPickpocket(victim, actor))
                    {
                        PilferingSystemUtility.AdjustPickpocketSensitiveHediffs(actor);
                        float burglaryMaxWeight = actor.GetStatValue(StatDefOf.CarryingCapacity);
                        float burglaryMaxValue = actor.GetStatValue(HautsDefOf.Hauts_MaxPilferingValue);
                        float successChance = actor.GetStatValue(HautsDefOf.Hauts_PilferingStealth);
                        float alertLevel = victim.Downed ? 0f : victim.GetStatValue(HautsDefOf.Hauts_PawnAlertLevel);
                        bool thingsStolen = false;
                        successChance -= alertLevel;
                        if (burglaryMaxWeight <= 0f)
                        {
                            TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererNoCarryCap".Translate();
                            Messages.Message(message, actor, MessageTypeDefOf.RejectInput, true);
                            return;
                        }
                        else if (burglaryMaxValue <= 0f)
                        {
                            TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooWeak".Translate();
                            Messages.Message(message, actor, MessageTypeDefOf.RejectInput, true);
                            return;
                        }
                        else if (successChance <= 0f)
                        {
                            TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooConspicuous".Translate();
                            Messages.Message(message, actor, MessageTypeDefOf.RejectInput, true);
                            return;
                        }
                        else
                        {
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
                                    PilferingSystemUtility.IncreaseAlertLevel(victim, minAlertRaise);
                                }
                                else
                                {
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
                                            PilferingSystemUtility.IncreaseAlertLevel(p, raiseAlertBy);
                                        }
                                    }
                                    else
                                    {
                                        PilferingSystemUtility.IncreaseAlertLevel(victim, raiseAlertBy);
                                    }
                                }
                            }
                            else
                            {
                                float raiseAlertBy = Math.Max(alertRaise, minAlertRaise);
                                if (victim.Faction != null)
                                {
                                    TaggedString message = "Hauts_PickpocketOutcome3".Translate(actor.Name.ToStringShort, victim.Faction.NameColored);
                                    LookTargets toLook = new LookTargets(actor);
                                    ChoiceLetter sadLetter = LetterMaker.MakeLetter("Hauts_PilferLetter3".Translate(), message, LetterDefOf.NegativeEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(sadLetter, null);
                                    actor.Faction.TryAffectGoodwillWith(victim.Faction, actor.IsPsychologicallyInvisible() ? -5 : -15);
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
                                        PilferingSystemUtility.IncreaseAlertLevel(p, raiseAlertBy);
                                    }
                                }
                                else
                                {
                                    TaggedString message = "Hauts_PickpocketOutcome3_Factionless".Translate(actor.Name.ToStringShort, victim.Name.ToStringShort);
                                    LookTargets toLook = new LookTargets(actor);
                                    ChoiceLetter sadLetter = LetterMaker.MakeLetter("Hauts_PilferLetter3".Translate(), message, LetterDefOf.NegativeEvent, toLook, null, null, null);
                                    Find.LetterStack.ReceiveLetter(sadLetter, null);
                                    if (victim.InMentalState)
                                    {
                                        victim.MentalState.RecoverFromState();
                                    }
                                    victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, false, true, false, null, false, false, false);
                                    PilferingSystemUtility.IncreaseAlertLevel(victim, raiseAlertBy);
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
    public static class PilferingSystemUtility
    {
        //does this caravan have anyone with a non-zero pilfering stealth stat?
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
        //what is the average pilfering stealth of all pawns on this caravan with a non-zero pilfering stealth stat?
        public static float CaravanStealthRating(Caravan caravan)
        {
            float stealthRating = 0f;
            List<Pawn> skulkersInCaravan = new List<Pawn>();
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (p.GetStatValue(HautsDefOf.Hauts_PilferingStealth) > 0f)
                {
                    skulkersInCaravan.Add(p);
                }
            }
            foreach (Pawn p in skulkersInCaravan)
            {
                stealthRating += p.GetStatValue(HautsDefOf.Hauts_PilferingStealth);
            }
            stealthRating /= (float)skulkersInCaravan.Count;
            return stealthRating;
        }
        public static List<Pawn> AllBurglarsInCaravan(Caravan caravan)
        {
            List<Pawn> burglarsInCaravan = new List<Pawn>();
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (p.GetStatValue(HautsDefOf.Hauts_PilferingStealth) > 0f)
                {
                    burglarsInCaravan.Add(p);
                }
            }
            return burglarsInCaravan;
        }
        //there are multiple methods of initiating burglary, but they ultimately share the same code. this is that code.
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
                float successChance = PilferingSystemUtility.CaravanStealthRating(caravan);
                List<Pawn> burglars = PilferingSystemUtility.AllBurglarsInCaravan(caravan);
                float alertLevel = PilferingSystemUtility.SettlementAlertLevel(settlement);
                foreach (Pawn p in burglars)
                {
                    burglaryMaxValue += p.GetStatValue(HautsDefOf.Hauts_MaxPilferingValue);
                }
                successChance -= alertLevel;
                if (burglars.Count == 0)
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
                }
                else if (burglaryMaxValue <= 0f)
                {
                    TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooWeak".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                }
                else if (successChance <= 0f)
                {
                    TaggedString message = "Hauts_PilfererErrorPrefix".Translate() + ": " + "Hauts_PilfererTooConspicuous".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                } else {
                    for (int i = burglars.Count - 1; i >= 0; i--)
                    {
                        PilferingSystemUtility.AdjustPickpocketSensitiveHediffs(burglars[i]);
                    }
                    Find.WindowStack.Add(new BurgleWindow(caravan, burglars, settlement, burglaryMaxValue, burglaryMaxWeight, successChance));
                }
            }
        }
        //determines what alert level this settlement should have by retrieving the value from the FactionComp_BurglaryResponse
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
                        }
                        else
                        {
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
        //pilfering isn't a verb, but ChangeSeverityOnVerbUse hediff comps can be configured to treat it as such. This method is invoked for the performing pawn(s) of any burglary or pilfering
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
        //adds the hidden alert level-increasing hediff to a pawn who has just been alarmed by a pickpocket attempt
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
    }
}
