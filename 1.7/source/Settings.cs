using Verse;

namespace SK_Bug_Off
{
    public class Settings : ModSettings
    {
        public static bool enableAllInsectsAssault = false;
        public static int forgetAggressorMinutes = 2;
        public static int cleanupIntervalSeconds = 60;
        public static float aggroRadius = 10f;
        public static float assaultRadius = 15f;

        // Death reinforcement settings
        public static bool enableDeathReinforcements = false;
        public static float deathScanRadius = 15f;
        public static IntRange combatantThresholdRange = new IntRange(5, 8);
        public static float hiveReinforcementRadius = 20f;
        public static IntRange reinforcementCountRange = new IntRange(10, 15);

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableAllInsectsAssault, "enableAllInsectsAssault", false);
            Scribe_Values.Look(ref forgetAggressorMinutes, "forgetAggressorMinutes", 2);
            Scribe_Values.Look(ref cleanupIntervalSeconds, "cleanupIntervalSeconds", 60);
            Scribe_Values.Look(ref aggroRadius, "aggroRadius", 10f);
            Scribe_Values.Look(ref assaultRadius, "assaultRadius", 15f);

            Scribe_Values.Look(ref enableDeathReinforcements, "enableDeathReinforcements", false);
            Scribe_Values.Look(ref deathScanRadius, "deathScanRadius", 15f);
            Scribe_Values.Look(ref combatantThresholdRange, "combatantThresholdRange", new IntRange(5, 8));
            Scribe_Values.Look(ref hiveReinforcementRadius, "hiveReinforcementRadius", 20f);
            Scribe_Values.Look(ref reinforcementCountRange, "reinforcementCountRange", new IntRange(10, 15));
        }

        // Convert minutes to ticks for internal use
        public static int ForgetAggressorTicks => forgetAggressorMinutes * 3600;

        // Convert seconds to ticks for internal use  
        public static int CleanupIntervalTicks => cleanupIntervalSeconds * 60;
    }
}