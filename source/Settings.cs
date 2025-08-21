using Verse;

namespace SK_Bug_Off
{
    public class Settings : ModSettings
    {
        public static bool enableAllInsectsAssault = false;
        public static int forgetAggressorMinutes = 2;
        public static int cleanupIntervalSeconds = 60;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableAllInsectsAssault, "enableAllInsectsAssault", false);
            Scribe_Values.Look(ref forgetAggressorMinutes, "forgetAggressorMinutes", 2);
            Scribe_Values.Look(ref cleanupIntervalSeconds, "cleanupIntervalSeconds", 60);
        }

        // Convert minutes to ticks for internal use
        public static int ForgetAggressorTicks => forgetAggressorMinutes * 3600;

        // Convert seconds to ticks for internal use  
        public static int CleanupIntervalTicks => cleanupIntervalSeconds * 60;
    }
}