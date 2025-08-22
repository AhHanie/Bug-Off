using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace SK_Bug_Off
{
    public class InsectMemoryMapComp : MapComponent
    {
        private Dictionary<Pawn, List<InsectAggressor>> originalAggressors = new Dictionary<Pawn, List<InsectAggressor>>();
        private int CLEANUP_INTERVAL = Settings.CleanupIntervalTicks;
        private int FORGET_AGGRESSOR_TICKS = Settings.ForgetAggressorTicks;
        private List<Pawn> originalAggressorsPawnList;
        private List<List<InsectAggressor>> originalAggressorsInsectAggList;

        public InsectMemoryMapComp(Map map)
            : base(map)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref originalAggressors, "originalAggressors", LookMode.Reference, LookMode.Deep, ref originalAggressorsPawnList, ref originalAggressorsInsectAggList);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame % CLEANUP_INTERVAL == 0)
            {
                CleanupOldAggressors();
            }
        }

        public void AddOriginalAggressor(Pawn insect, InsectAggressor aggressor)
        {
            if (!Utils.IsInsect(insect))
                return;

            if (!originalAggressors.ContainsKey(insect))
            {
                originalAggressors[insect] = new List<InsectAggressor>();
            }

            InsectAggressor existingAggressor = originalAggressors[insect].Find(agg => agg.IsSame(aggressor));
            if (existingAggressor == null)
            {
                originalAggressors[insect].Add(aggressor);
            }
            else
            {
                existingAggressor.EngagementStartTick = Find.TickManager.TicksGame;
            }
        }

        public List<InsectAggressor> GetOriginalAggressors(Pawn insect)
        {
            return originalAggressors.TryGetValue(insect, out List<InsectAggressor> aggressors) ? aggressors : new List<InsectAggressor>();
        }

        public bool ShouldForgetAggressor(InsectAggressor aggressor)
        {
            if (aggressor.IsDefeated(map))
            {
                return true;
            }

            return Find.TickManager.TicksGame - aggressor.EngagementStartTick > FORGET_AGGRESSOR_TICKS;
        }

        public void SetInsectsToAssaultColonyInRadius(IntVec3 centerPosition)
        {
            var allInsects = map.mapPawns.AllPawnsSpawned.Where(p => Utils.IsInsect(p));

            var insectsInRadius = allInsects.Where(insect =>
                (insect.Position - centerPosition).LengthHorizontal <= Settings.assaultRadius).ToList();

            var insectLords = map.lordManager.lords.Where(lord =>
                lord.ownedPawns.Any(pawn => insectsInRadius.Contains(pawn))).ToList();

            foreach (var lord in insectLords)
            {
                UpdateLordToAssaultColony(lord, insectsInRadius);
            }
        }

        private void UpdateLordToAssaultColony(Lord lord, List<Pawn> insectsInRadius)
        {
            if (lord?.ownedPawns == null)
                return;

            foreach (var insect in lord.ownedPawns.Where(Utils.IsInsect))
            {
                if (insectsInRadius.Contains(insect) && insect.mindState != null)
                {
                    var duty = new PawnDuty(DutyDefOf.AssaultColony);
                    if (duty != null)
                    {
                        insect.mindState.duty = duty;
                    }
                }
            }
        }

        private void CleanupOldAggressors()
        {
            var keysToRemove = new List<Pawn>();
            var toUpdate = new Dictionary<Pawn, List<InsectAggressor>>();

            foreach (var kvp in originalAggressors)
            {
                var insect = kvp.Key;
                var aggressors = kvp.Value;

                if (insect.DestroyedOrNull())
                {
                    keysToRemove.Add(insect);
                    continue;
                }

                var validAggressors = aggressors.Where(a => !ShouldForgetAggressor(a)).ToList();
                var removedAggressors = aggressors.Where(a => ShouldForgetAggressor(a)).ToList();
                if (validAggressors.Count == 0)
                {
                    keysToRemove.Add(insect);
                }
                else if (validAggressors.Count != aggressors.Count)
                {
                    toUpdate.Add(insect, validAggressors);
                }
            }

            foreach (var key in keysToRemove)
            {
                originalAggressors.Remove(key);
            }

            foreach (var kvp in toUpdate)
            {
                originalAggressors[kvp.Key] = kvp.Value;
            }

            if (!HasAnyValidAggressors())
            {
                SetAllAssaultInsectsToDefendAndExpandHive();
            }
        }

        public void CleanupInsect(Pawn insect)
        {
            originalAggressors.Remove(insect);
        }

        private bool HasAnyValidAggressors()
        {
            foreach (var kvp in originalAggressors)
            {
                var aggressors = kvp.Value;
                if (aggressors.Any(a => !ShouldForgetAggressor(a)))
                {
                    return true;
                }
            }
            return false;
        }

        private void SetAllAssaultInsectsToDefendAndExpandHive()
        {
            var insectLords = map.lordManager.lords.Where(lord =>
                lord.ownedPawns.Any(pawn => Utils.IsInsect(pawn))).ToList();

            foreach (var lord in insectLords)
            {
                UpdateLordToDefendAndExpandHive(lord);
            }
        }

        private void UpdateLordToDefendAndExpandHive(Lord lord)
        {
            if (lord?.ownedPawns == null)
                return;

            foreach (var insect in lord.ownedPawns.Where(Utils.IsInsect))
            {
                if (insect.mindState?.duty?.def == DutyDefOf.AssaultColony)
                {
                    var duty = new PawnDuty(DutyDefOf.DefendAndExpandHive);
                    if (duty != null)
                    {
                        insect.mindState.duty = duty;
                    }
                }
            }
        }
    }
}