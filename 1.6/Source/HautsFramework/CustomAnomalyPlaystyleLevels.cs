using RimWorld;
using System;
using Verse;

namespace HautsFramework
{
    /* a couple of this framework's HediffComps cause severity adjustments based on the current "Anomalous activity level" i.e. monolith level in Standard playstyle, a flat value in Ambient Horror, and none in Disabled
     * but other mods could add other AnomalyPlaystyleDefs, and if their level would be better represented by something other than the defaults, this allows you to implement a custom function for that APD's "level" at any given time
     * the various fields are in the DME so you can define them in XML, but they are for the Worker to use.
     * maxLevel's default is 4 because that corresponds to the logically highest activity level in Standard playstyle (gleaming/embraced) but the level above that corresponds to Disrupted i.e. no anomaly activity
     * presumably anything you wanted to key off Anomalous activity level would treat Disrupted as being 'lower' than most of the levels it is technically higher than, so.*/
    public class CustomAnomalyPlaystyleActivityLevels : DefModExtension
    {
        public CustomAnomalyPlaystyleActivityLevels() { }
        public AnomalyPlaystyleActivityLevelWorker Worker
        {
            get
            {
                if (this.workerInt == null && this.activityLevelWorker != null)
                {
                    this.workerInt = (AnomalyPlaystyleActivityLevelWorker)Activator.CreateInstance(this.activityLevelWorker);
                }
                return this.workerInt;
            }
        }
        public Type activityLevelWorker;
        public int defaultLevel;
        public float daysToNextLevel;
        public int maxLevel = 4;
        [Unsaved(false)]
        private AnomalyPlaystyleActivityLevelWorker workerInt;
    }
    public class AnomalyPlaystyleActivityLevelWorker
    {
        public virtual int CurrentLevel(CustomAnomalyPlaystyleActivityLevels capal)
        {
            return capal.defaultLevel;
        }
    }
    //each day the gray pall is a world condition, +1 activity level. For use with [Alpha] Anomaly Remix: Gray Pall
    public class ActivityLevelWorker_GrayPallMod : AnomalyPlaystyleActivityLevelWorker
    {
        public override int CurrentLevel(CustomAnomalyPlaystyleActivityLevels capal)
        {
            GameCondition gc = Find.World.GameConditionManager.GetActiveCondition(GameConditionDefOf.GrayPall);
            if (gc != null)
            {
                return Math.Min((int)Math.Floor(gc.TicksPassed / (60000 * capal.daysToNextLevel)), capal.maxLevel);
            }
            return capal.defaultLevel;
        }
    }
}
