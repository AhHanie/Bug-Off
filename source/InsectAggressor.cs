using RimWorld;
using System;
using Verse;

namespace SK_Bug_Off
{
    public class InsectAggressor : IExposable, IEquatable<InsectAggressor>
    {
        private Faction faction;
        private Pawn pawn;
        private int engagementStartTick;

        public Pawn Pawn { get => pawn; set => pawn = value; }
        public Faction Faction { get => faction; set => faction = value; }
        public int EngagementStartTick { get => engagementStartTick; set => engagementStartTick = value; }

        public string Name { get => pawn != null ? pawn.LabelCap : faction.Name; }

        // Parameterless constructor for loading
        public InsectAggressor()
        {
        }

        public InsectAggressor(Pawn pawn)
        {
            this.pawn = pawn;
            engagementStartTick = Find.TickManager.TicksGame;
        }

        public InsectAggressor(Faction faction)
        {
            this.faction = faction;
            engagementStartTick = Find.TickManager.TicksGame;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref engagementStartTick, "engagementStartTick", 0);
        }

        public bool DestoryedOrNull()
        {
            // Check if pawn is destroyed/null, or if faction is null when we only have faction reference
            if (pawn != null)
            {
                // Consider pawn as "destroyed" if they are dead, destroyed, or downed
                return pawn.DestroyedOrNull() || pawn.Dead || pawn.Downed;
            }

            return faction == null;
        }

        public bool IsDefeated(Map map)
        {
            // More specific check for whether this aggressor is defeated
            if (pawn != null)
            {
                return pawn.DestroyedOrNull() || pawn.DeadOrDowned;
            }

            // For factions, check if all members of the faction on the map are defeated
            if (faction == null)
                return true;

            // Check if there are any living, non-downed pawns of this faction on the map
            return !HasAliveFactionMembersOnMap(map);
        }

        private bool HasAliveFactionMembersOnMap(Map map)
        {
            if (faction == null || map == null)
            {
                return false;
            }
                

            // Check all pawns on the map
            foreach (Pawn mapPawn in map.mapPawns.PawnsInFaction(faction))
            {
                if (mapPawn.Faction == faction && !mapPawn.DestroyedOrNull() && !mapPawn.DeadOrDowned)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Equals(InsectAggressor other)
        {
            if (other == null)
                return false;

            // Equal if same pawn (and both have pawns) or same faction (and both have factions)
            if (this.pawn != null && other.pawn != null)
                return this.pawn == other.pawn;

            if (this.faction != null && other.faction != null)
                return this.faction == other.faction;

            return false;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as InsectAggressor);
        }

        public override int GetHashCode()
        {
            if (pawn != null)
                return pawn.GetHashCode();

            return faction?.GetHashCode() ?? 0;
        }
    }
}