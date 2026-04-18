using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsFramework
{
    //this ability expends chargeCost severity from the pawn's requisiteHediff in order to be cast. Therefore, it can't be cast if the pawn lacks that hediff or it doesn't have enough severity.
    public class CompProperties_AbilityCostsHediffSeverity : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityCostsHediffSeverity()
        {
            this.compClass = typeof(CompAbilityEffect_CostsHediffSeverity);
        }
        public float severityCost;
        public HediffDef requisiteHediff;
    }
    public class CompAbilityEffect_CostsHediffSeverity : CompAbilityEffect
    {
        public new CompProperties_AbilityCostsHediffSeverity Props
        {
            get
            {
                return (CompProperties_AbilityCostsHediffSeverity)this.props;
            }
        }
        public Hediff RequisiteHediff
        {
            get
            {
                if (this.parent.pawn != null)
                {
                    return this.parent.pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.requisiteHediff);
                }
                return null;
            }
        }
        public override bool CanCast
        {
            get
            {
                Hediff h = this.RequisiteHediff;
                return h != null && h.Severity >= this.Props.severityCost;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Hediff h = this.RequisiteHediff;
            if (h != null)
            {
                h.Severity -= this.Props.severityCost;
            }
        }
    }
    /*the ability can only be removed if the pawn has none of these forcing traits or genes. Otherwise, when it would be removed, it gets re-added instantaneously (resetting it to its initial state)
     * requiresAForcingProperty: causes certain events to remove the ability if the pawn has none of these forcing traits or genes
     * -After the player finishes the Game Creation process and clicks start
     * -On selecting a pawn and clicking the “DEV: FixTraitGrantedStuff” button (only visible if you have God Mode in dev mode enabled)*/
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
    /*As GiveHediff, but the hediff’s severity = "severity" * however much of the "casterStatToScaleFrom" the caster has
     * replacesLessSevereHediff: if the target already has this hediff, and its first instance is less severe than the current cast’s instantiation of the hediff would be, the extant one gets replaced by the new instance
     * refreshesMoreSevereHediff: if the target already has this hediff, and its first instance is more severe than the current cast’s instantiation of the hediff would be, that extant one's HediffComp_Disappears duration is set to whatever the new instance's would've been, and the new instance does not get added to the pawn
     *  ReplaceExistingHediff handles the replacement of a prior instantiation of a hediff
     *  RefreshMoreSevereHediff handles the refreshing of a prior instantiation of a hediff
     *  DontReplaceDontRefresh is only run if there was a prior instantiation of the hediff on the target already but it is neither to be replaced nor refreshed
     *  ModifyCreatedHediff alters the hediff added to the target right before it gets added. This code is not run if Refresh or DontReplaceDontRefresh occurred, as in both cases no hediff is going to be added*/
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
                float severity = this.parent.pawn.GetStatValue(this.Props.casterStatToScaleFrom) * (this.Props.severity >= 0f ? this.Props.severity : 1f);
                Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef, false);
                if (firstHediffOfDef != null)
                {
                    if (this.Props.replaceExisting || (this.Props.replacesLessSevereHediff && firstHediffOfDef.Severity < severity))
                    {
                        this.ReplaceExistingHediff(target, firstHediffOfDef);
                    }
                    else if (this.Props.refreshesMoreSevereHediff)
                    {
                        this.RefreshMoreSevereHediff(target, firstHediffOfDef);
                        return;
                    }
                    else
                    {
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
                this.ModifyCreatedHediff(target, hediff);
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
    /*Opens a menu of hediffs, one of which can be selected to apply to the target
     * menuString: heads the Dialogue menu
     * ignoreSelf: makes the ability unable to affect the caster
     * requiresStory: the menu won't have any options if the the target lacks a StoryTracker component
     * hediffs: the menu options. If cast by a pawn not of the player faction, it will pick a random hediff automatically
     * onlyBrain: adds chosen hediff to the pawn’s brain; otherwise it’s added to the whole body
     * autoSelectIfAI: if the pawn with this ability isn’t of the player faction, it will use the ability on itself whenever it can. This will trigger the hediff adding and the ability’s CompAbilityEffect_RemoveHediff (if any), but not any other effects of the ability, as it is not actually getting cast
     * severity: if >=0, the generated hediff’s severity is set to this value*/
    public class CompProperties_AbilityGiveHediffFromMenu : CompProperties_AbilityEffectWithDuration
    {
        public List<HediffDef> hediffs;
        public bool onlyBrain;
        public float severity = -1f;
        public bool ignoreSelf;
        public bool autoSelectIfAI = true;
        public string menuString;
        public bool requiresStory = true;
        public bool removeExistingOptionsFromPawn;
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
            this.ApplyInner(target.Pawn, this.parent.pawn);
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.Props.autoSelectIfAI && this.parent.CanCast && this.parent.pawn.Faction != Faction.OfPlayer)
            {
                this.parent.Activate(this.parent.pawn, this.parent.pawn);
            }
        }
        protected void ApplyInner(Pawn target, Pawn other)
        {
            if (target != null)
            {
                if (this.parent.pawn != null && this.parent.pawn.Faction == Faction.OfPlayer)
                {
                    Find.WindowStack.Add(new Dialog_GiveHediffFromMenu(this, target, other, this.parent.pawn, this.Props.hediffs, this.Props.menuString, this.Props.requiresStory,this.Props.removeExistingOptionsFromPawn));
                } else {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffs.RandomElement<HediffDef>(), target, null);
                    target.health.AddHediff(hediff, this.Props.onlyBrain ? target.health.hediffSet.GetBrain() : null, null, null);
                    HautsMiscUtility.AddHediffFromMenu(this.Props.hediffs.RandomElement<HediffDef>(), this.parent.pawn, this, this.parent.pawn, this.parent.pawn,this.Props.removeExistingOptionsFromPawn?this.Props.hediffs:null);
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
        public Dialog_GiveHediffFromMenu(CompAbilityEffect_GiveHediffFromMenu ability, Pawn pawn, Pawn other, Pawn caster, List<HediffDef> hediffs, string menuLabel, bool requiresStory = true, bool removesOtherOptionsFromPawn = false)
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
            this.requiresStory = requiresStory;
            this.removesOtherOptionsFromPawn = removesOtherOptionsFromPawn;
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
            if (!this.requiresStory || pawn.story != null)
            {
                foreach (HediffDef h in this.possibleHediffs)
                {
                    bool flag = this.chosenHediff == h;
                    bool flag2 = flag;
                    listing_Standard.CheckboxLabeled(h.LabelCap, ref flag, h.description);
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
                    HautsMiscUtility.AddHediffFromMenu(this.chosenHediff, this.pawn, this.ability, this.other, this.caster,this.removesOtherOptionsFromPawn?this.possibleHediffs:null);
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
        private List<HediffDef> possibleHediffs;
        private CompAbilityEffect_GiveHediffFromMenu ability;
        private HediffDef chosenHediff;
        private float scrollHeight;
        private Pawn pawn;
        private Pawn other;
        private Pawn caster;
        private Vector2 scrollPosition;
        private bool requiresStory;
        private bool removesOtherOptionsFromPawn;
    }
    /*Gives hediffs to the target and/or destination Pawns (can be different hediffs in each case) and “pairs” them together if they have the PairedHediff comp.
     * If a given hediff has the Link comp, it will also link to the other party.
     * Of the target and destination, at least one must be a Pawn; otherwise, nothing will happen. Functions as if it were a derivative of WithDuration and WithDest (assuming no mods that patch those) but is not actually a derivative of either class.
     * NOTE ON DESTINATION: Destination will be null if the destination field is “RandomInRange”. “Caster” and “Selected” function as expected.
     * applyToTarget|Dest: if true and the target|destination is a Pawn, gives…
     * -hediffToTarg|Dest: …this hediff to the target|destination
     * bodyPartTarg|Dest: hediff is added to the first non-missing body part of the target|destination found with this tag
     * replaceExistingTarg|Dest: added hediff replaces the first instance of the same hediff already on the target|destination
     * float severityTarg|Dest: if >0, the newly added hediff’s severity is either set to this value…
     * overridePreviousSeverityTarg|Dest: …or, if this is true and there is a prior instance of the same hediff which is not to be replaced, that prior hediff’s severity is increased by this amount instead
     * overridePreviousDurationTarg|Dest: if this is true and there is a prior instance of the same hediff which is not to be replaced, that prior hediff’s Disappears comp’s ticksToDisappear is redetermined as if just created by this ability.
     * overridePreviousLinkTargetTarg|Dest: if this is true and there is a prior instance of the same hediff which is not to be replaced, that prior hediff’s Link comp’s other is changed to link to the destination|target
     * linkHediffTarg|Dest: links the given hediff’s Link comp (if any) to the destination|target Thing
     * canStackTarg|Dest: required if you want a Pawn that already has an instance of the hediffToTarg|Dest to be a valid target|destination
     * ModifyHediffPreAdd: allows derivatives to inject code that runs between the creation of the given hediff and its actual addition to the Pawn. Intended for any last-second changes that occur in that time*/
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
                    }
                    else
                    {
                        this.ApplyPawnToThing(target.Pawn, dest.Thing, this.Props.hediffToTarg, this.Props.bodyPartTarg, this.Props.severityTarg, this.Props.replaceExistingTarg, this.Props.overridePreviousSeverityTarg, this.Props.overridePreviousDurationTarg, this.Props.overridePreviousLinkTargetTarg, this.Props.linkHediffTarg);
                    }
                }
            }
            else if (this.Props.applyToDest && dest.Thing != null && dest.Thing is Pawn)
            {
                this.ApplyPawnToThing(dest.Pawn, target.Thing, this.Props.hediffToDest, this.Props.bodyPartDest, this.Props.severityDest, this.Props.replaceExistingDest, this.Props.overridePreviousSeverityDest, this.Props.overridePreviousDurationDest, this.Props.overridePreviousLinkTargetDest, this.Props.linkHediffDest);
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
                }
                else
                {
                    if (severity > 0f)
                    {
                        if (overridePreviousSeverity)
                        {
                            firstHediffOfDef.Severity = severity;
                        }
                        else
                        {
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
            }
            else
            {
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
    /*A passive property of the ability (not a modifier to its targeting or its on-cast effects) that periodically inflicts a specified thought on its owner.
     * periodicity: after an amount of ticks equal to a randomly chosen value from this range has elapsed…
     * thought: give this thought to the pawn
     * duringCooldown: required for the thought to be given while the ability is on cooldown
     * whileReady: required for the thought to be given while the ability is ready to use
     * clearsThisThoughtOnActivation: makes casting the ability remove all instances of this thought*/
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
                this.nextPeriod--;
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
    /*When this ability finishes casting, it creates an instant radial effect centered on the caster. A derivative of CompAbilityEffect_AiAppliesToSelf (see AbilityComp_Targeting.cs), so NPCs will use it
     * Does not specify what its effect is; this is just a parent for comps that actually do stuff.
     * baseRadius: to get the final radius, multiply this…
     * radiusScalarCapacity: …by (if specified) however much of this health capacity the caster has…
     * radiusScalarStat: …and by (if specified) however much of this stat the pawn has…
     * maxRadius: …capped at this value. These calculations are handled in virtual float Radius.
     * AffectSelf: determines what effect happens to the caster, if anything. Does nothing by default - define it in a child class.
     * AffectPawn: determines what effect happens to pawns other than the caster caught inside the radius, if anything. Again, define in a child class.
     * VictimCounter: must return true in order for AI-controlled pawns to use this ability. Returns true if the sum VictimValue (1 if pawn is hostile, -1 if non-hostile and has a faction and aiDislikesFriendlyFire, 0 otherwise) of all pawns in this ability’s radius >= aiMinEnemyPawns.*/
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
                    }
                    else if (pawns[i].Position.DistanceTo(this.parent.pawn.Position) <= this.Radius)
                    {
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
    /*does exactly what it says on the tin
     * needsToEffect: adds the corresponding value to each need the target has
     * targetMustHaveAffectedNeeds: if true, the ability will refuse to target anyone who lacks any of the need keys in needsToEffect*/
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
                }
                else
                {
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
    /*Disables ability use if the pawn lacks the specified amount of each specified stat in minStats
     * hideIfDisabled: hides ability’s button if it is disabled by this comp*/
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
}
