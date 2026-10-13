using HarmonyLib;
using UnityEngine;
using Verse;

namespace SK_Bug_Off
{
    public class Mod : Verse.Mod
    {
        public static Harmony instance;

        public Mod(ModContentPack content)
            : base(content)
        {
            GetSettings<Settings>();
            instance = new Harmony("rimworld.sk.bugoff");
            instance.PatchAll();
        }

        public override string SettingsCategory()
        {
            return "Bug Off";
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            ModSettingsWindow.Draw(rect);
            base.DoSettingsWindowContents(rect);
        }
    }
}
