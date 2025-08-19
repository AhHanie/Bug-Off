using HarmonyLib;
using Verse;

namespace SK_Bug_Off
{
    public class Mod : Verse.Mod
    {
        public static Harmony instance;

        public Mod(ModContentPack content)
            : base(content)
        {
            instance = new Harmony("rimworld.sk.bugoff");
            LongEventHandler.QueueLongEvent(Init, "Sk.Bug_Off.Init", doAsynchronously: true, null);
        }

        public static void Init()
        {
            instance.PatchAll();
        }
    }
}
