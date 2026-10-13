using RimWorld;
using Verse;

namespace SK_Bug_Off
{
    public class Utils
    {
        public static bool IsInsect(Pawn pawn)
        {
            return pawn.Faction?.def == FactionDefOf.Insect;
        }
    }
}
