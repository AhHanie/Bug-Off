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
        private const int CLEANUP_INTERVAL = 3600; // 1 minute in ticks
        private const int FORGET_AGGRESSOR_TICKS = 10800; // 2 minutes in ticks

        public InsectMemoryMapComp(Map map)
            : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Convert dictionary to lists for saving
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var insects = originalAggressors.Keys.ToList();
                var aggressorLists = originalAggressors.Values.ToList();

                Scribe_Collections.Look(ref insects, "insects", LookMode.Reference);
                Scribe_Collections.Look(ref aggressorLists, "aggressorLists", LookMode.Deep);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<Pawn> insects = null;
                List<List<InsectAggressor>> aggressorLists = null;

                Scribe_Collections.Look(ref insects, "insects", LookMode.Reference);
                Scribe_Collections.Look(ref aggressorLists, "aggressorLists", LookMode.Deep);

                originalAggressors.Clear();
                if (insects != null && aggressorLists != null)
                {
                    for (int i = 0; i < insects.Count && i < aggressorLists.Count; i++)
                    {
                        if (insects[i] != null && aggressorLists[i] != null)
                        {
                            originalAggressors[insects[i]] = aggressorLists[i];
                        }
                    }
                }
            }
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

            InsectAggressor existingAggressor = originalAggressors[insect].Find(agg => agg.Equals(aggressor));
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

        public bool HasValidAggressors(Pawn insect)
        {
            var aggressors = GetOriginalAggressors(insect);
            return aggressors.Any(a => !ShouldForgetAggressor(a));
        }

        public bool ShouldForgetAggressor(InsectAggressor aggressor)
        {
            // First check if it's defeated (includes faction check)
            if (aggressor.IsDefeated(map))
            {
                return true;
            }
                
            // Then check time-based forgetting
            return Find.TickManager.TicksGame - aggressor.EngagementStartTick > FORGET_AGGRESSOR_TICKS;
        }

        // NEW METHOD: Set all insects on the map to assault colony
        public void SetAllInsectsToAssaultColony()
        {
            // Get all insect lords on this map
            var insectLords = map.lordManager.lords.Where(lord =>
                lord.ownedPawns.Any(pawn => Utils.IsInsect(pawn))).ToList();

            foreach (var lord in insectLords)
            {
                UpdateLordToAssaultColony(lord);
            }
        }

        // NEW METHOD: Update a specific lord to assault colony
        private void UpdateLordToAssaultColony(Lord lord)
        {
            if (lord?.ownedPawns == null)
                return;

            // Update all insect duties to AssaultColony
            foreach (var insect in lord.ownedPawns.Where(Utils.IsInsect))
            {
                if (insect.mindState != null)
                {
                    var duty = new PawnDuty(DutyDefOf.AssaultColony);
                    if (duty != null)
                    {
                        insect.mindState.duty = duty;
                    }
                }
            }
        }

        public void CheckAndUpdateLordDuties(Pawn potentialAggressor)
        {
            // First, instantly clean up this specific aggressor if they're defeated
            bool wasAggressor = CleanupSpecificAggressor(potentialAggressor);

            if (wasAggressor)
            {
                CheckAllLordDuties();
            }
        }

        public bool CleanupSpecificAggressor(Pawn defeatedPawn)
        {
            bool wasAggressor = false;
            var toUpdate = new Dictionary<Pawn, List<InsectAggressor>>();
            var keysToRemove = new List<Pawn>();

            foreach (var kvp in originalAggressors)
            {
                var insect = kvp.Key;
                var aggressors = kvp.Value;
                var originalCount = aggressors.Count;

                // Remove this specific pawn and any faction aggressors related to it
                var remainingAggressors = aggressors.Where(a =>
                {
                    bool shouldRemove = false;

                    // Remove if this is the specific pawn aggressor
                    if (a.Pawn == defeatedPawn)
                    {
                        shouldRemove = true;
                        wasAggressor = true;
                    }
                    // Remove faction aggressor if this was the last alive member
                    else if (a.Faction != null && defeatedPawn.Faction == a.Faction && a.Pawn == null)
                    {
                        // Check if there are any other alive members of this faction on the map
                        bool hasOtherAliveMembers = map.mapPawns.AllPawnsSpawned.Any(p =>
                            p != defeatedPawn &&
                            p.Faction == a.Faction &&
                            !p.Dead &&
                            !p.Downed &&
                            !p.DestroyedOrNull());

                        if (!hasOtherAliveMembers)
                        {
                            shouldRemove = true;
                            wasAggressor = true;
                        }
                    }

                    return !shouldRemove;
                }).ToList();

                if (remainingAggressors.Count == 0)
                {
                    keysToRemove.Add(insect);
                }
                else if (remainingAggressors.Count != originalCount)
                {
                    toUpdate.Add(insect, remainingAggressors);
                }
            }

            // Apply the updates
            foreach (var key in keysToRemove)
            {
                originalAggressors.Remove(key);
            }

            foreach (var kvp in toUpdate)
            {
                originalAggressors[kvp.Key] = kvp.Value;
            }

            return wasAggressor;
        }

        private void CleanupOldAggressors()
        {
            // This method now primarily handles time-based cleanup since defeated aggressors
            // are cleaned up instantly via CleanupSpecificAggressor()
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

                // Remove old aggressors (time-based cleanup and any missed defeated ones)
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

            // Remove entries with no valid aggressors
            foreach (var key in keysToRemove)
            {
                originalAggressors.Remove(key);
            }

            foreach (var kvp in toUpdate)
            {
                originalAggressors[kvp.Key] = kvp.Value;
            }

            // Check if we need to update lord duties after cleanup
            if (keysToRemove.Count > 0 || toUpdate.Count > 0)
            {
                CheckAllLordDuties();
            }
        }

        public void CleanupInsect(Pawn insect)
        {
            originalAggressors.Remove(insect);
        }

        private void CheckAllLordDuties()
        {
            // Get all insect lords on this map
            var insectLords = map.lordManager.lords.Where(lord =>
                lord.ownedPawns.Any(pawn => Utils.IsInsect(pawn))).ToList();

            foreach (var lord in insectLords)
            {
                CheckAndUpdateLordDuty(lord);
            }
        }

        private void CheckAndUpdateLordDuty(Lord lord)
        {
            if (lord?.ownedPawns == null || lord.ownedPawns.Count == 0)
                return;

            // Check if any insects in this lord still have valid aggressors
            bool hasValidAggressors = false;

            foreach (var insect in lord.ownedPawns.Where(Utils.IsInsect))
            {
                if (HasValidAggressors(insect))
                {
                    hasValidAggressors = true;
                }
            }

            // If no valid aggressors remain, switch back to DefendAndExpandHive
            if (!hasValidAggressors)
            {
                // Check if lord is currently in assault mode
                if (lord.CurLordToil != null && IsAssaultDuty(lord))
                {
                    UpdateLordToDefendAndExpandHive(lord);
                }
            }
        }

        private bool IsAssaultDuty(Lord lord)
        {
            // Check if any pawn in the lord has an assault-type duty
            return lord.ownedPawns.Any(pawn =>
                pawn.mindState?.duty?.def == DutyDefOf.AssaultColony);
        }

        private void UpdateLordToDefendAndExpandHive(Lord lord)
        {
            if (lord?.ownedPawns == null)
                return;

            // Update all insect duties to DefendAndExpandHive
            foreach (var insect in lord.ownedPawns.Where(Utils.IsInsect))
            {
                if (insect.mindState != null)
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