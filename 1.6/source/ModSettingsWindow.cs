using UnityEngine;
using Verse;

namespace SK_Bug_Off
{
    public class ModSettingsWindow
    {
        private static Vector2 scrollPosition = Vector2.zero;

        public static void Draw(Rect inRect)
        {
            // Calculate content height dynamically based on what's shown
            float contentHeight = 150f; // Base height for title and main toggle

            if (Settings.enableDeathReinforcements)
            {
                contentHeight += 450f; // Death reinforcement settings (5 settings + spacing)
            }

            contentHeight += 350f; // Advanced settings section (4 settings + spacing)
            contentHeight += 80f;  // Reset button and final padding

            // Create scroll view - ensure minimum height
            contentHeight = Mathf.Max(contentHeight, inRect.height + 50f);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, contentHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(viewRect);

            Text.Font = GameFont.Small;

            listingStandard.Gap();

            // Main toggle for all insects assault behavior
            listingStandard.CheckboxLabeled(
                "SKBugOff.Settings.EnableAllInsectsAssault".Translate(),
                ref Settings.enableAllInsectsAssault,
                "SKBugOff.Settings.EnableAllInsectsAssault.Tooltip".Translate());

            listingStandard.Gap();

            // Death reinforcement toggle
            listingStandard.CheckboxLabeled(
                "SKBugOff.Settings.EnableDeathReinforcements".Translate(),
                ref Settings.enableDeathReinforcements,
                "SKBugOff.Settings.EnableDeathReinforcements.Tooltip".Translate());

            listingStandard.Gap();

            // Death reinforcement settings (only show if enabled)
            if (Settings.enableDeathReinforcements)
            {
                listingStandard.Label("SKBugOff.Settings.DeathReinforcementSettings".Translate());
                listingStandard.Gap(12f);

                // Death scan radius
                DrawSliderSetting(listingStandard, "SKBugOff.Settings.DeathScanRadius".Translate(), ref Settings.deathScanRadius, 5f, 30f,
                    "SKBugOff.Settings.DeathScanRadius.Description".Translate());

                // Combatant threshold range
                DrawIntRangeSlider(listingStandard, "SKBugOff.Settings.CombatantThreshold", ref Settings.combatantThresholdRange, 1, 20,
                    "SKBugOff.Settings.CombatantThreshold.Description".Translate());

                // Hive reinforcement radius
                DrawSliderSetting(listingStandard, "SKBugOff.Settings.HiveReinforcementRadius".Translate(), ref Settings.hiveReinforcementRadius, 10f, 50f,
                    "SKBugOff.Settings.HiveReinforcementRadius.Description".Translate());

                // Reinforcement count range
                DrawIntRangeSlider(listingStandard, "SKBugOff.Settings.ReinforcementCount", ref Settings.reinforcementCountRange, 1, 50,
                    "SKBugOff.Settings.ReinforcementCount.Description".Translate());

                listingStandard.Gap();
            }

            // Advanced settings section
            listingStandard.Label("SKBugOff.Settings.AdvancedSection".Translate());
            listingStandard.Gap(12f);

            // Aggro radius setting
            DrawSliderSetting(listingStandard, "SKBugOff.Settings.AggroRadius".Translate(), ref Settings.aggroRadius, 1f, 30f,
                "SKBugOff.Settings.AggroRadius.Description".Translate());

            // Assault radius setting
            DrawSliderSetting(listingStandard, "SKBugOff.Settings.AssaultRadius".Translate(), ref Settings.assaultRadius, 5f, 50f,
                "SKBugOff.Settings.AssaultRadius.Description".Translate());

            // Forget aggressor time setting
            DrawIntSliderSetting(listingStandard, "SKBugOff.Settings.ForgetAggressorTime".Translate(), ref Settings.forgetAggressorMinutes,
                1, 60, "SKBugOff.Settings.ForgetAggressorTime.Description".Translate());

            // Cleanup interval setting
            DrawIntSliderSetting(listingStandard, "SKBugOff.Settings.CleanupInterval".Translate(), ref Settings.cleanupIntervalSeconds,
                10, 300, "SKBugOff.Settings.CleanupInterval.Description".Translate());

            listingStandard.Gap();

            // Reset to defaults button
            if (listingStandard.ButtonText("SKBugOff.Settings.ResetToDefaults".Translate()))
            {
                Settings.enableAllInsectsAssault = false;
                Settings.forgetAggressorMinutes = 2;
                Settings.cleanupIntervalSeconds = 60;
                Settings.aggroRadius = 10f;
                Settings.assaultRadius = 15f;

                // Reset death reinforcement settings
                Settings.enableDeathReinforcements = false;
                Settings.deathScanRadius = 15f;
                Settings.combatantThresholdRange = new IntRange(5, 8);
                Settings.hiveReinforcementRadius = 20f;
                Settings.reinforcementCountRange = new IntRange(10, 15);
            }

            listingStandard.End();
            Widgets.EndScrollView();
        }

        private static void DrawSliderSetting(Listing_Standard listingStandard, string label, ref float value, float min, float max, string description)
        {
            Rect rect = listingStandard.GetRect(30f);
            Rect labelRect = new Rect(rect.x, rect.y, rect.width * 0.6f, rect.height);
            Rect sliderRect = new Rect(rect.x + rect.width * 0.65f, rect.y, rect.width * 0.25f, rect.height);
            Rect valueRect = new Rect(rect.x + rect.width * 0.92f, rect.y, rect.width * 0.08f, rect.height);

            Widgets.Label(labelRect, label);
            value = Widgets.HorizontalSlider(sliderRect, value, min, max, true);
            Widgets.Label(valueRect, value.ToString("F1"));

            listingStandard.Gap(6f);
            listingStandard.Label(description);
            listingStandard.Gap();
        }

        private static void DrawIntSliderSetting(Listing_Standard listingStandard, string label, ref int value, int min, int max, string description)
        {
            Rect rect = listingStandard.GetRect(30f);
            Rect labelRect = new Rect(rect.x, rect.y, rect.width * 0.6f, rect.height);
            Rect sliderRect = new Rect(rect.x + rect.width * 0.65f, rect.y, rect.width * 0.25f, rect.height);
            Rect valueRect = new Rect(rect.x + rect.width * 0.92f, rect.y, rect.width * 0.08f, rect.height);

            Widgets.Label(labelRect, label);
            float floatValue = value;
            floatValue = Widgets.HorizontalSlider(sliderRect, floatValue, min, max, true);
            value = (int)floatValue;
            Widgets.Label(valueRect, value.ToString());

            listingStandard.Gap(6f);
            listingStandard.Label(description);
            listingStandard.Gap();
        }

        private static void DrawIntRangeSlider(Listing_Standard listingStandard, string label, ref IntRange range, int min, int max, string description)
        {
            // Get a unique ID for this slider
            int id = (label + range.ToString()).GetHashCode();

            // Reserve space for the slider
            Rect rect = listingStandard.GetRect(31f);

            // Draw the range slider using Widgets.IntRange
            Widgets.IntRange(rect, id, ref range, min, max, label);

            listingStandard.Gap(6f);
            listingStandard.Label(description);
            listingStandard.Gap();
        }
    }
}