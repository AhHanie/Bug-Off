using UnityEngine;
using Verse;

namespace SK_Bug_Off
{
    public class ModSettingsWindow
    {
        public static void Draw(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Title
            Text.Font = GameFont.Medium;
            listingStandard.Label("SKBugOff.Settings.Title".Translate());
            Text.Font = GameFont.Small;

            listingStandard.Gap();

            // Main toggle for all insects assault behavior
            listingStandard.CheckboxLabeled(
                "SKBugOff.Settings.EnableAllInsectsAssault".Translate(),
                ref Settings.enableAllInsectsAssault,
                "SKBugOff.Settings.EnableAllInsectsAssault.Tooltip".Translate());

            listingStandard.Gap();

            // Advanced settings section - now always visible
            listingStandard.Label("SKBugOff.Settings.AdvancedSection".Translate());
            listingStandard.Gap(12f);

            // Aggro radius setting
            Rect aggroRect = listingStandard.GetRect(30f);
            Rect aggroLabelRect = new Rect(aggroRect.x, aggroRect.y, aggroRect.width * 0.6f, aggroRect.height);
            Rect aggroSliderRect = new Rect(aggroRect.x + aggroRect.width * 0.65f, aggroRect.y, aggroRect.width * 0.25f, aggroRect.height);
            Rect aggroValueRect = new Rect(aggroRect.x + aggroRect.width * 0.92f, aggroRect.y, aggroRect.width * 0.08f, aggroRect.height);

            Widgets.Label(aggroLabelRect, "SKBugOff.Settings.AggroRadius".Translate());
            float aggroValue = Settings.aggroRadius;
            aggroValue = Widgets.HorizontalSlider(aggroSliderRect, aggroValue, 1f, 30f, true);
            Settings.aggroRadius = aggroValue;
            Widgets.Label(aggroValueRect, Settings.aggroRadius.ToString("F1"));

            listingStandard.Gap(6f);
            listingStandard.Label("SKBugOff.Settings.AggroRadius.Description".Translate());

            listingStandard.Gap();

            // Assault radius setting
            Rect assaultRect = listingStandard.GetRect(30f);
            Rect assaultLabelRect = new Rect(assaultRect.x, assaultRect.y, assaultRect.width * 0.6f, assaultRect.height);
            Rect assaultSliderRect = new Rect(assaultRect.x + assaultRect.width * 0.65f, assaultRect.y, assaultRect.width * 0.25f, assaultRect.height);
            Rect assaultValueRect = new Rect(assaultRect.x + assaultRect.width * 0.92f, assaultRect.y, assaultRect.width * 0.08f, assaultRect.height);

            Widgets.Label(assaultLabelRect, "SKBugOff.Settings.AssaultRadius".Translate());
            float assaultValue = Settings.assaultRadius;
            assaultValue = Widgets.HorizontalSlider(assaultSliderRect, assaultValue, 5f, 50f, true);
            Settings.assaultRadius = assaultValue;
            Widgets.Label(assaultValueRect, Settings.assaultRadius.ToString("F1"));

            listingStandard.Gap(6f);
            listingStandard.Label("SKBugOff.Settings.AssaultRadius.Description".Translate());

            listingStandard.Gap();

            // Forget aggressor time setting
            Rect forgetTimeRect = listingStandard.GetRect(30f);
            Rect forgetTimeLabelRect = new Rect(forgetTimeRect.x, forgetTimeRect.y, forgetTimeRect.width * 0.6f, forgetTimeRect.height);
            Rect forgetTimeSliderRect = new Rect(forgetTimeRect.x + forgetTimeRect.width * 0.65f, forgetTimeRect.y, forgetTimeRect.width * 0.25f, forgetTimeRect.height);
            Rect forgetTimeValueRect = new Rect(forgetTimeRect.x + forgetTimeRect.width * 0.92f, forgetTimeRect.y, forgetTimeRect.width * 0.08f, forgetTimeRect.height);

            Widgets.Label(forgetTimeLabelRect, "SKBugOff.Settings.ForgetAggressorTime".Translate());
            float forgetValue = Settings.forgetAggressorMinutes;
            forgetValue = Widgets.HorizontalSlider(forgetTimeSliderRect, forgetValue, 1f, 60f, true);
            Settings.forgetAggressorMinutes = (int)forgetValue;
            Widgets.Label(forgetTimeValueRect, Settings.forgetAggressorMinutes.ToString());

            listingStandard.Gap(6f);
            listingStandard.Label("SKBugOff.Settings.ForgetAggressorTime.Description".Translate());

            listingStandard.Gap();

            // Cleanup interval setting
            Rect cleanupRect = listingStandard.GetRect(30f);
            Rect cleanupLabelRect = new Rect(cleanupRect.x, cleanupRect.y, cleanupRect.width * 0.6f, cleanupRect.height);
            Rect cleanupSliderRect = new Rect(cleanupRect.x + cleanupRect.width * 0.65f, cleanupRect.y, cleanupRect.width * 0.25f, cleanupRect.height);
            Rect cleanupValueRect = new Rect(cleanupRect.x + cleanupRect.width * 0.92f, cleanupRect.y, cleanupRect.width * 0.08f, cleanupRect.height);

            Widgets.Label(cleanupLabelRect, "SKBugOff.Settings.CleanupInterval".Translate());
            float cleanupValue = Settings.cleanupIntervalSeconds;
            cleanupValue = Widgets.HorizontalSlider(cleanupSliderRect, cleanupValue, 10f, 300f, true);
            Settings.cleanupIntervalSeconds = (int)cleanupValue;
            Widgets.Label(cleanupValueRect, Settings.cleanupIntervalSeconds.ToString());

            listingStandard.Gap(6f);
            listingStandard.Label("SKBugOff.Settings.CleanupInterval.Description".Translate());

            listingStandard.Gap();

            // Reset to defaults button
            if (listingStandard.ButtonText("SKBugOff.Settings.ResetToDefaults".Translate()))
            {
                Settings.enableAllInsectsAssault = false;
                Settings.forgetAggressorMinutes = 2;
                Settings.cleanupIntervalSeconds = 60;
                Settings.aggroRadius = 10f;
                Settings.assaultRadius = 15f;
            }

            listingStandard.End();
        }
    }
}