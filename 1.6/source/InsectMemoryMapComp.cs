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
        private Dictionary<Pawn, ReinforcementAssignment> reinforcementAssignments = new Dictionary<Pawn, ReinforcementAssignment>();
        private int CLEANUP_INTERVAL = Settings.CleanupIntervalTicks;
        private int FORGET_AGGRESSOR_TICKS = Settings.ForgetAggressorTicks;
        private int REINFORCEMENT_DUTY_TIMEOUT_TICKS = 3600; // 1 minute
        private float REINFORCEMENT_MOVEMENT_THRESHOLD = 10f; // 10 tiles
        private List<Pawn> originalAggressorsPawnList;
        private List<List<InsectAggressor>> originalAggressorsInsectAggList;
        private List<Pawn> reinforcementAssignmentsPawnList;
        private List<ReinforcementAssignment> reinforcementAssignmentsAssignmentList;
        private bool allHivesDestroyed = false;

        public bool AllHivesDestroyed { get => allHivesDestroyed; }
        public List<Pawn> AllReinforcements { get => reinforcementAssignments.Keys.ToList(); }

        public InsectMemoryMapComp(Map map)
            : base(map)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref originalAggressors, "originalAggressors", LookMode.Reference, LookMode.Deep, ref originalAggressorsPawnList, ref originalAggressorsInsectAggList);
            Scribe_Collections.Look(ref reinforcementAssignments, "reinforcementAssignments", LookMode.Reference, LookMode.Deep, ref reinforcementAssignmentsPawnList, ref reinforcementAssignmentsAssignmentList);
            Scribe_Values.Look(ref allHivesDestroyed, "allHivesDestroyed", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (reinforcementAssignments == null)
                {
                    reinforcementAssignments = new Dictionary<Pawn, ReinforcementAssignment>();
                }
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame % CLEANUP_INTERVAL == 0)
            {
                CleanupOldAggressors();
                CleanupOldReinforcementAssignments();
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
            var insectsInRadius = new List<Pawn>();

            foreach (var cell in GenRadial.RadialCellsAround(centerPosition, Settings.assaultRadius, useCenter: true))
            {
                foreach (var thing in map.thingGrid.ThingsAt(cell))
                {
                    if (thing is Pawn pawn && Utils.IsInsect(pawn))
                    {
                        insectsInRadius.Add(pawn);
                    }
                }
            }

            var insectLords = map.lordManager.lords.Where(lord =>
                lord.ownedPawns.Any(pawn => insectsInRadius.Contains(pawn))).ToList();

            foreach (var lord in insectLords)
            {
                UpdateLordToAssaultColony(lord, insectsInRadius);
            }
        }

        public void HandleInsectDeath(Pawn deadInsect)
        {
            if (!Settings.enableDeathReinforcements)
                return;

            if (!Utils.IsInsect(deadInsect))
                return;

            int combatantCount = CountInsectCombatantsInRadius(deadInsect.Position, Settings.deathScanRadius);

            int threshold = Settings.combatantThresholdRange.RandomInRange;

            if (combatantCount < threshold)
            {
                CallReinforcements(deadInsect);
            }
        }

        private int CountInsectCombatantsInRadius(IntVec3 center, float radius)
        {
            int count = 0;

            foreach (var cell in GenRadial.RadialCellsAround(center, radius, useCenter: false))
            {
                foreach (var thing in map.thingGrid.ThingsAt(cell))
                {
                    if (thing is Pawn pawn && !pawn.DeadOrDowned)
                    {
                        if (Utils.IsInsect(pawn))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private void CallReinforcements(Pawn deadInsect)
        {
            var allHives = map.listerThings.ThingsOfDef(ThingDefOf.Hive);
            if (allHives.Count == 0)
            {
                return;
            }

            var reinforcements = new List<Pawn>();
            int targetReinforcementCount = Settings.reinforcementCountRange.RandomInRange;

            foreach (var hive in allHives)
            {
                if (reinforcements.Count >= targetReinforcementCount)
                    break;

                var nearbyInsects = FindInsectsNearHive(hive.Position, Settings.hiveReinforcementRadius);

                foreach (var insect in nearbyInsects)
                {
                    if (reinforcements.Count >= targetReinforcementCount)
                        break;

                    if (!reinforcements.Contains(insect))
                    {
                        reinforcements.Add(insect);
                    }
                }
            }

            if (reinforcements.Count == 0)
                return;

            int currentTick = Find.TickManager.TicksGame;

            foreach (var insect in reinforcements)
            {
                insect.GetLord()?.RemovePawn(insect);
                reinforcementAssignments[insect] = new ReinforcementAssignment(deadInsect.Position, currentTick);
            }

            var assaultJob = new LordJob_AssaultColony(Faction.OfInsects, canKidnap: true, canTimeoutOrFlee: false);
            LordMaker.MakeNewLord(Faction.OfInsects, assaultJob, map, reinforcements);
        }


        private List<Pawn> FindInsectsNearHive(IntVec3 hivePosition, float radius)
        {
            var insects = new List<Pawn>();

            foreach (var cell in GenRadial.RadialCellsAround(hivePosition, radius, useCenter: true))
            {
                foreach (var thing in map.thingGrid.ThingsAt(cell))
                {
                    if (thing is Pawn pawn && Utils.IsInsect(pawn) && !pawn.DeadOrDowned)
                    {
                        insects.Add(pawn);
                    }
                }
            }

            return insects;
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
                    if (duty != null && insect.mindState != null)
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

            if (!HasAnyValidAggressors() && HasAnyHivesOnMap())
            {
                SetAllAssaultInsectsToDefendAndExpandHive();
            }
        }

        private void CleanupOldReinforcementAssignments()
        {
            if (!HasAnyValidAggressors())
            {
                reinforcementAssignments.Clear();
                return;
            }

            var keysToRemove = new List<Pawn>();

            foreach (var kvp in reinforcementAssignments)
            {
                var pawn = kvp.Key;
                var assignment = kvp.Value;

                if (pawn.DestroyedOrNull() || pawn.mindState?.duty?.def != DutyDefOf.AssaultColony)
                {
                    keysToRemove.Add(pawn);
                    continue;
                }

                int currentTick = Find.TickManager.TicksGame;
                bool timeoutReached = (currentTick - assignment.assignmentTick) >= REINFORCEMENT_DUTY_TIMEOUT_TICKS;
                bool movedEnough = (pawn.Position - assignment.assignmentPosition).LengthHorizontal <= REINFORCEMENT_MOVEMENT_THRESHOLD;

                if (timeoutReached || movedEnough)
                {
                    keysToRemove.Add(pawn);
                }
            }

            foreach (var key in keysToRemove)
            {
                reinforcementAssignments.Remove(key);
            }
        }

        public void CleanupInsect(Pawn insect)
        {
            originalAggressors.Remove(insect);
            reinforcementAssignments.Remove(insect);
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
                if (insect.mindState?.duty.def == DutyDefOf.AssaultColony)
                {
                    if (reinforcementAssignments.ContainsKey(insect))
                    {
                        var assignment = reinforcementAssignments[insect];
                        int currentTick = Find.TickManager.TicksGame;
                        bool timeoutReached = (currentTick - assignment.assignmentTick) >= REINFORCEMENT_DUTY_TIMEOUT_TICKS;
                        bool movedEnough = (insect.Position - assignment.assignmentPosition).LengthHorizontal <= REINFORCEMENT_MOVEMENT_THRESHOLD;

                        if (!timeoutReached && !movedEnough)
                        {
                            continue;
                        }
                        else
                        {
                            reinforcementAssignments.Remove(insect);
                        }
                    }

                    var duty = new PawnDuty(DutyDefOf.DefendAndExpandHive);
                    if (duty != null && insect.mindState != null)
                    {
                        insect.mindState.duty = duty;
                    }
                }
            }
        }

        public bool HasAnyHivesOnMap()
        {
            allHivesDestroyed = HiveUtility.TotalSpawnedHivesCount(map) == 0;
            return !allHivesDestroyed;
        }
    }
}