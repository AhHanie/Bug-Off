using RimWorld;
using Verse;

namespace SK_Bug_Off
{
    public class InsectAggressor : IExposable
    {
        private Faction faction;
        private Pawn pawn;
        private int engagementStartTick;

        public Pawn Pawn { get => pawn; set => pawn = value; }
        public Faction Faction { get => faction; set => faction = value; }
        public int EngagementStartTick { get => engagementStartTick; set => engagementStartTick = value; }

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
            Scribe_Values.Look(ref engagementStartTick, "engagementStartTick", 0);
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref faction, "faction");
        }

        public bool IsDefeated(Map map)
        {
            if (pawn != null)
            {
                return pawn.DestroyedOrNull() || pawn.DeadOrDowned;
            }

            if (faction == null)
                return true;

            return !HasAliveFactionMembersOnMap(map);
        }

        private bool HasAliveFactionMembersOnMap(Map map)
        {
            if (faction == null || map == null)
            {
                return false;
            }

            foreach (Pawn mapPawn in map.mapPawns.PawnsInFaction(faction))
            {
                if (mapPawn.Faction == faction && !mapPawn.DestroyedOrNull() && !mapPawn.DeadOrDowned)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSame(InsectAggressor other)
        {
            if (other == null)
                return false;

            if (this.pawn != null && other.pawn != null)
                return this.pawn == other.pawn;

            if (this.faction != null && other.faction != null)
                return this.faction == other.faction;

            return false;
        }
    }
}