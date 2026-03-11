using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsFramework
{
    /*You can add comps to factions via xpath. The xpath target’s def should be “HautsFramework.Hauts_FactionCompDef”, and the defName should be “Hauts_FCHolder”.
     * Obviously, you will still have to define what those comps do yourself*/
    public class WorldComponent_HautsFactionComps : WorldComponent
    {
        public WorldComponent_HautsFactionComps(World world) : base(world)
        {
            this.world = world;
        }
        public bool TryGetCompsFor(Faction faction)
        {
            foreach (Hauts_FactionCompHolder item in this.factions)
            {
                if (item.factionLoadID == faction.loadID)
                {
                    return true;
                }
            }
            return false;
        }
        public Hauts_FactionCompHolder FindCompsFor(Faction faction)
        {
            Hauts_FactionCompHolder fch = null;
            foreach (Hauts_FactionCompHolder item in this.factions)
            {
                if (item.factionLoadID == faction.loadID)
                {
                    fch = item;
                    break;
                }
            }
            return fch;
        }
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            foreach (Faction f in this.world.factionManager.AllFactionsListForReading)
            {
                bool shouldAdd = true;
                foreach (Hauts_FactionCompHolder fch in this.factions)
                {
                    if (fch.factionLoadID == f.loadID)
                    {
                        shouldAdd = false;
                        break;
                    }
                }
                if (shouldAdd)
                {
                    Hauts_FactionCompHolder newFCH = new Hauts_FactionCompHolder(f);
                    newFCH.PostMake();
                    this.factions.Add(newFCH);
                }
            }
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (GenTicks.TicksGame == 3)
            {
                TraitModExtensionUtility.TraitGrantedStuffLoadCheck(Find.WorldPawns.AllPawnsAlive);
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    TraitModExtensionUtility.TraitGrantedStuffLoadCheck(Find.Maps[i].mapPawns.AllPawns);
                }
                this.ThirdTickEffects();
            }
            for (int j = this.factions.Count - 1; j >= 0; j--)
            {
                this.factions[j].PostTick();
            }
        }
        public void ThirdTickEffects()
        {
            //you put Harmony patches in here to also go off at the same time as the initial traitgrantedstuff load check
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Hauts_FactionCompHolder>(ref this.factions, "factions", LookMode.Deep, Array.Empty<object>());
        }
        public List<Hauts_FactionCompHolder> factions = new List<Hauts_FactionCompHolder>();
    }
    public class Hauts_FactionCompHolder : IExposable
    {
        public Hauts_FactionCompHolder()
        {
            this.def = HautsDefOf.Hauts_FCHolder;
        }
        public Hauts_FactionCompHolder(Faction faction)
        {
            this.factionLoadID = faction.loadID;
            this.def = HautsDefOf.Hauts_FCHolder;
        }
        public void PostMake()
        {
            this.InitializeComps();
            for (int i = this.comps.Count - 1; i >= 0; i--)
            {
                try
                {
                    this.comps[i].CompPostMake();
                }
                catch (Exception arg)
                {
                    Log.Error("Error in HautsFactionComp.CompPostMake(): " + arg);
                    this.comps.RemoveAt(i);
                }
            }
        }
        private void InitializeComps()
        {
            if (this.def.comps != null)
            {
                this.comps = new List<HautsFactionComp>();
                for (int i = 0; i < this.def.comps.Count; i++)
                {
                    HautsFactionComp hautsFactionComp = null;
                    try
                    {
                        hautsFactionComp = (HautsFactionComp)Activator.CreateInstance(this.def.comps[i].compClass);
                        hautsFactionComp.props = this.def.comps[i];
                        hautsFactionComp.parent = this;
                        this.comps.Add(hautsFactionComp);
                    }
                    catch (Exception arg)
                    {
                        Log.Error("Could not instantiate or initialize a HautsFactionComp: " + arg);
                        this.comps.Remove(hautsFactionComp);
                    }
                }
            }
        }
        public T TryGetComp<T>() where T : HautsFactionComp
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    T t = this.comps[i] as T;
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            return default(T);
        }
        public void PostTick()
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    this.comps[i].CompPostTick();
                }
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.factionLoadID, "factionLoadID", 0);
            Scribe_Defs.Look<Hauts_FactionCompDef>(ref this.def, "def");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.InitializeComps();
            }
            for (int i = 0; i < this.comps.Count; i++)
            {
                this.comps[i].CompExposeData();
            }
        }
        public int factionLoadID;
        public Hauts_FactionCompDef def;
        public List<HautsFactionComp> comps = new List<HautsFactionComp>();
    }
    public class Hauts_FactionCompDef : Def
    {
        public bool HasComp(Type compClass)
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    if (this.comps[i].compClass == compClass)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public HautsFactionCompProperties CompPropsFor(Type compClass)
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    if (this.comps[i].compClass == compClass)
                    {
                        return this.comps[i];
                    }
                }
            }
            return null;
        }
        public T CompProps<T>() where T : HautsFactionCompProperties
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    T t = this.comps[i] as T;
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            return default(T);
        }
        public override void ResolveReferences()
        {
            if (this.comps != null)
            {
                for (int i = 0; i < this.comps.Count; i++)
                {
                    this.comps[i].ResolveReferences(this);
                }
            }
        }
        public List<HautsFactionCompProperties> comps;
    }
    public class HautsFactionComp
    {
        public int factionLoadID
        {
            get
            {
                return this.parent.factionLoadID;
            }
        }
        public Faction ThisFaction
        {
            get
            {
                foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                {
                    if (f.loadID == this.factionLoadID)
                    {
                        return f;
                    }
                }
                return null;
            }
        }
        public Hauts_FactionCompDef Def
        {
            get
            {
                return this.parent.def;
            }
        }
        public virtual void CompPostMake()
        {
        }
        public virtual void CompPostTick()
        {
        }
        public virtual void CompExposeData()
        {
        }
        //may add other functions, like Notify_FactionDestroyed, or leader died, or whatever
        public Hauts_FactionCompHolder parent;
        public HautsFactionCompProperties props;
    }
    /*example comp, "SpyPoints". Pawns with an Espionage hediff comp will add spy points to whichever faction(s) the hediff is configured to let them give points to.
     * spy points are spent whenever that faction issues a raid, increasing the raid's incident points by however many were spent (up to +1x)
     * HAT and HEMP use this comp, if you want to see it in invisible 'action'*/
    public class HautsFactionCompProperties
    {
        public virtual void PostLoad()
        {
        }
        public virtual void ResolveReferences(Hauts_FactionCompDef parent)
        {
        }
        public virtual IEnumerable<string> ConfigErrors(Hauts_FactionCompDef parentDef)
        {
            if (this.compClass == null)
            {
                yield return "compClass is null";
            }
            int num;
            for (int i = 0; i < parentDef.comps.Count; i = num + 1)
            {
                if (parentDef.comps[i] != this && parentDef.comps[i].compClass == this.compClass)
                {
                    yield return "two comps with same compClass: " + this.compClass;
                }
                num = i;
            }
            yield break;
        }
        [TranslationHandle]
        public Type compClass;
    }
    public class HautsFactionCompProperties_SpyPoints : HautsFactionCompProperties
    {
        public HautsFactionCompProperties_SpyPoints()
        {
            this.compClass = typeof(HautsFactionComp_SpyPoints);
        }
        public int spyPoints;
    }
    public class HautsFactionComp_SpyPoints : HautsFactionComp
    {
        public HautsFactionCompProperties_SpyPoints Props
        {
            get
            {
                return (HautsFactionCompProperties_SpyPoints)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.spyPoints = this.Props.spyPoints;
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.spyPoints, "spyPoints", 0, false);
        }
        public int spyPoints;
    }
    [Obsolete]
    public class Hauts_SpyHediff : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.spyingOnFaction == null)
            {
                this.spyingOnFaction = Faction.OfPlayerSilentFail;
            }
            if (this.spyingForFaction == null)
            {
                this.spyingForFaction = this.pawn.Faction;
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.Faction == this.spyingOnFaction)
            {
                this.Severity = -1f;
            }
            else if (this.pawn.IsWorldPawn() && !this.pawn.Dead)
            {
                if (!this.pawn.IsPrisonerOfColony)
                {
                    WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(this.spyingForFaction);
                    if (fch != null)
                    {
                        HautsFactionComp_SpyPoints spyPoints = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                        if (spyPoints != null)
                        {
                            int addedSpyPoints = (int)(153.5 * this.pawn.skills.GetSkill(SkillDefOf.Intellectual).Level * this.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight));
                            spyPoints.spyPoints += addedSpyPoints + 2;
                        }
                    }
                    this.Severity = -1f;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.spyingOnFaction, "spyingOnFaction", false);
            Scribe_References.Look<Faction>(ref this.spyingForFaction, "spyingForFaction", false);
        }
        public Faction spyingOnFaction;
        public Faction spyingForFaction;
    }
    public enum SpyPointAttribution : byte
    {
        OwnFaction,
        AllPermaHostile,
        AllHostile,
        RandomHostileFactions,
        All
    }
    /*spy points gained = baseSpyPoints * (fallbackIfNoSkillLevel + sum level of pawn's skills in relevantSkills) * (sum of all pawn's capacity levels in relevantCapacities), plus unscalableFlatSpyPoints
     * spyPointAttribution: whom these points are given to. OwnFaction is the pawn's own faction, AllPermaHostile is self-explanatory, so is AllHostile, All is literally all factions other than the player...
     * randomFactionCount: ...or up to this many non-player factions if RandomHostileFactions is the spyPointAttribution. */
    public class HediffCompProperties_Espionage : HediffCompProperties
    {
        public HediffCompProperties_Espionage()
        {
            this.compClass = typeof(HediffComp_Espionage);
        }
        public float baseSpyPoints;
        public int unscalableFlatSpyPoints;
        public List<SkillDef> relevantSkills;
        public float fallbackIfNoSkillLevel = 1f;
        public List<PawnCapacityDef> relevantCapacities;
        public int randomFactionCount;
        public SpyPointAttribution spyPointAttribution = SpyPointAttribution.OwnFaction;
    }
    public class HediffComp_Espionage : HediffComp
    {
        public HediffCompProperties_Espionage Props
        {
            get
            {
                return (HediffCompProperties_Espionage)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (this.spyingOnFaction == null)
            {
                this.spyingOnFaction = Faction.OfPlayerSilentFail;
            }
            if (this.spyingForFaction == null)
            {
                this.spyingForFaction = this.Pawn.Faction;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.Faction == this.spyingOnFaction)
            {
                this.Pawn.health.RemoveHediff(this.parent);
                return;
            }
            if (this.Pawn.IsWorldPawn() && !this.Pawn.Dead)
            {
                if (!this.Pawn.IsPrisonerOfColony)
                {
                    int spyPointsToGain = this.SpyPointsToGain();
                    WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                    Faction playerF = Faction.OfPlayerSilentFail;
                    switch (this.Props.spyPointAttribution)
                    {
                        case SpyPointAttribution.OwnFaction:
                            this.GrantSpyPoints(WCFC, this.spyingForFaction);
                            break;
                        case SpyPointAttribution.AllPermaHostile:
                            foreach (Faction f in Find.FactionManager.AllFactions)
                            {
                                if (f != playerF && f.def.PermanentlyHostileTo(playerF.def) && f.HostileTo(playerF))
                                {
                                    this.GrantSpyPoints(WCFC, f);
                                }
                            }
                            break;
                        case SpyPointAttribution.AllHostile:
                            foreach (Faction f in Find.FactionManager.AllFactions)
                            {
                                if (f != playerF && f.HostileTo(playerF))
                                {
                                    this.GrantSpyPoints(WCFC, f);
                                }
                            }
                            break;
                        case SpyPointAttribution.RandomHostileFactions:
                            List<Faction> hostileFactions = Find.FactionManager.AllFactions.Where((Faction f)=> f!= Faction.OfPlayerSilentFail && f.HostileTo(Faction.OfPlayerSilentFail)).ToList();
                            if (hostileFactions.Count > 0)
                            {
                                int randomCount = Math.Min(Math.Max(1, this.Props.randomFactionCount), hostileFactions.Count());
                                foreach (Faction f in hostileFactions.InRandomOrder())
                                {
                                    if (f != playerF)
                                    {
                                        this.GrantSpyPoints(WCFC, f);
                                    }
                                    randomCount--;
                                    if (randomCount <= 0)
                                    {
                                        break;
                                    }
                                }
                            }
                            break;
                        case SpyPointAttribution.All:
                            foreach (Faction f in Find.FactionManager.AllFactions)
                            {
                                this.GrantSpyPoints(WCFC, f);
                            }
                            break;
                        default:
                            this.GrantSpyPoints(WCFC, this.spyingForFaction);
                            break;
                    }
                    this.Pawn.health.RemoveHediff(this.parent);
                }
            }
        }
        public virtual int SpyPointsToGain()
        {
            float addedSpyPoints = this.Props.baseSpyPoints;
            if (!this.Props.relevantSkills.NullOrEmpty())
            {
                float sumSkilllevel = this.Props.fallbackIfNoSkillLevel;
                if (this.Pawn.skills != null)
                {
                    foreach (SkillDef sd in this.Props.relevantSkills)
                    {
                        sumSkilllevel += this.Pawn.skills.GetSkill(sd).Level;
                    }
                }
                addedSpyPoints *= sumSkilllevel;
            }
            if (!this.Props.relevantCapacities.NullOrEmpty())
            {
                float sumCapLevel = 0f;
                foreach (PawnCapacityDef pcd in this.Props.relevantCapacities)
                {
                    sumCapLevel += this.Pawn.health.capacities.GetLevel(pcd);
                }
                addedSpyPoints *= sumCapLevel;
            }
            return (int)addedSpyPoints + this.Props.unscalableFlatSpyPoints;
        }
        public virtual void GrantSpyPoints(WorldComponent_HautsFactionComps WCFC, Faction f)
        {
            Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
            if (fch != null)
            {
                HautsFactionComp_SpyPoints spyPoints = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                if (spyPoints != null)
                {
                    spyPoints.spyPoints += this.SpyPointsToGain();
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Faction>(ref this.spyingOnFaction, "spyingOnFaction", false);
            Scribe_References.Look<Faction>(ref this.spyingForFaction, "spyingForFaction", false);
        }
        public Faction spyingOnFaction;
        public Faction spyingForFaction;
    }
}
