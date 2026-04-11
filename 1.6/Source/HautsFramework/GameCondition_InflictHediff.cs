using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace HautsFramework
{
    /*Psychic Suppression inflicts a hediff which can only exist if the Psychic Suppression condition is active where the pawn is. Wonderful system. Also, hardcoded
     This lets you do something similar: make a game condition that imposes a hediff on any pawns it can affect (provided they meet the qualifications of CheckPawnInner),
    and make a hediff that removes itself if the pawn is not affected by a specified game condition*/
    public class GameCondition_InflictHediff : GameCondition
    {
        public override void Init()
        {
            base.Init();
        }
        public void CheckPawn(Pawn pawn)
        {
            InflictedHediff ih = this.def.GetModExtension<InflictedHediff>();
            if (ih != null && this.CheckPawnInner(pawn, ih))
            {
                this.AddHediff(pawn, ih);
            }
        }
        public virtual bool CheckPawnInner(Pawn pawn, InflictedHediff ih)
        {
            return !pawn.health.hediffSet.HasHediff(ih.hediff, false);
        }
        public virtual void AddHediff(Pawn pawn, InflictedHediff ih)
        {
            if (ih.hediff != null)
            {
                pawn.health.AddHediff(ih.hediff, null, null, null);
            }
        }
        public override void GameConditionTick()
        {
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                foreach (Map map in base.AffectedMaps)
                {
                    List<Pawn> allPawns = map.mapPawns.AllPawns;
                    for (int i = 0; i < allPawns.Count; i++)
                    {
                        this.CheckPawn(allPawns[i]);
                    }
                }
                if (this.gameConditionManager == Find.World.GameConditionManager)
                {
                    foreach (Caravan c in Find.WorldObjects.Caravans)
                    {
                        List<Pawn> allPawns = c.PawnsListForReading;
                        for (int i = 0; i < allPawns.Count; i++)
                        {
                            this.CheckPawn(allPawns[i]);
                        }
                    }
                }
            }
        }
    }
    public class InflictedHediff : DefModExtension
    {
        public InflictedHediff() { }
        public HediffDef hediff;
    }
    public class HediffCompProperties_ReliantOnGameCondition : HediffCompProperties
    {
        public HediffCompProperties_ReliantOnGameCondition()
        {
            this.compClass = typeof(HediffComp_ReliantOnGameCondition);
        }
        public GameConditionDef gameCondition;
        public bool dontAffectAnomalies;
        public bool dontAffectMechs;
    }
    public class HediffComp_ReliantOnGameCondition : HediffComp
    {
        public HediffCompProperties_ReliantOnGameCondition Props
        {
            get
            {
                return (HediffCompProperties_ReliantOnGameCondition)this.props;
            }
        }
        public override bool CompShouldRemove
        {
            get
            {
                if (this.Props.dontAffectMechs && !this.Pawn.RaceProps.IsFlesh)
                {
                    return true;
                }
                if (ModsConfig.AnomalyActive && this.Props.dontAffectAnomalies && (this.Pawn.IsMutant || this.Pawn.IsEntity))
                {
                    return true;
                }
                if (base.Pawn.SpawnedOrAnyParentSpawned)
                {
                    GameCondition gc = this.Pawn.MapHeld.gameConditionManager.GetActiveCondition(this.Props.gameCondition);
                    if (this.MeetsGameConditionQualifiers(gc))
                    {
                        return false;
                    }
                } else {
                    GameCondition gc = Find.World.gameConditionManager.GetActiveCondition(this.Props.gameCondition);
                    if (this.MeetsGameConditionQualifiers(gc))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public virtual bool MeetsGameConditionQualifiers(GameCondition gc)
        {
            return gc != null;
        }
    }
}
