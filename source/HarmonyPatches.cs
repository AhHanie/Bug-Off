using HarmonyLib;
using Verse;

namespace SK_Bug_Off
{
    public class HarmonyPatches
    {

        [HarmonyPatch(typeof(Thing), "TakeDamage")]
        public static class Patch_TakeDamage
        {
            public static void Postfix(Thing __instance, DamageInfo dinfo)
            {
                if (__instance is Pawn pawn)
                {
                    if (!Utils.IsInsect(pawn) || dinfo.Instigator == null || __instance.Map == null)
                        return;

                    InsectMemoryMapComp insectMemoryComp = __instance.Map.GetComponent<InsectMemoryMapComp>();

                    if (dinfo.Instigator is Pawn attacker)
                    {
                        // Add the attacking pawn as an aggressor
                        insectMemoryComp.AddOriginalAggressor(pawn, new InsectAggressor(attacker));

                        // Also add their faction if they have one (for faction-wide hostility)
                        if (attacker.Faction != null)
                        {
                            insectMemoryComp.AddOriginalAggressor(pawn, new InsectAggressor(attacker.Faction));
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), "Destroy")]
        public static class Patch_PawnDestroy
        {
            public static void Postfix(Pawn __instance)
            {
                Map map = Find.CurrentMap;
                if (map == null)
                {
                    return;
                }
                InsectMemoryMapComp insectMemoryComp = map.GetComponent<InsectMemoryMapComp>();

                if (Utils.IsInsect(__instance))
                {
                    // Clean up the insect itself
                    insectMemoryComp.CleanupInsect(__instance);
                }
                else
                {
                    // Instantly clean up this aggressor and check lord duties
                    insectMemoryComp.CheckAndUpdateLordDuties(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), "Kill")]
        public static class Patch_PawnKill
        {
            public static void Postfix(Pawn __instance)
            {
                if (__instance.Corpse?.Map != null)
                {
                    // Instantly clean up this aggressor and check lord duties
                    InsectMemoryMapComp insectMemoryComp = __instance.Corpse.Map.GetComponent<InsectMemoryMapComp>();
                    insectMemoryComp.CheckAndUpdateLordDuties(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), "Notify_Downed")]
        public static class Patch_PawnNotifyDowned
        {
            public static void Postfix(Pawn __instance)
            {
                if (__instance.Map != null)
                {
                    // Instantly clean up this aggressor and check lord duties
                    InsectMemoryMapComp insectMemoryComp = __instance.Map.GetComponent<InsectMemoryMapComp>();
                    insectMemoryComp.CheckAndUpdateLordDuties(__instance);
                }
            }
        }
    }
}