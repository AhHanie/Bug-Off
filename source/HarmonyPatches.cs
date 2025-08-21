using HarmonyLib;
using Verse;
using RimWorld;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;

namespace SK_Bug_Off
{
    public class HarmonyPatches
    {
        // Static flag to track when insects are selecting targets through JobGiver_AIGotoNearestHostile
        private static bool isInsectSelectingTargets = false;

        [HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        public static class Patch_JobGiver_AIGotoNearestHostile_TryGiveJob
        {
            public static void Prefix(Pawn pawn)
            {
                // Set flag if this is an insect selecting targets
                if (Utils.IsInsect(pawn))
                {
                    isInsectSelectingTargets = true;
                }
            }

            public static void Postfix(Pawn pawn)
            {
                // Always clear the flag when done, regardless of pawn type
                isInsectSelectingTargets = false;
            }
        }

        [HarmonyPatch(typeof(AttackTargetsCache), "GetPotentialTargetsFor")]
        public static class Patch_AttackTargetsCache_GetPotentialTargetsFor
        {
            public static void Postfix(IAttackTargetSearcher th, ref List<IAttackTarget> __result)
            {
                // Only filter if we're in insect target selection mode
                if (!isInsectSelectingTargets || __result == null || __result.Count == 0)
                    return;

                // Only apply to insects
                if (!(th.Thing is Pawn insectPawn) || !Utils.IsInsect(insectPawn))
                    return;

                // Get the insect's memory component
                InsectMemoryMapComp memoryComp = insectPawn.Map?.GetComponent<InsectMemoryMapComp>();

                // Filter the targets
                var filteredTargets = new List<IAttackTarget>();

                foreach (var target in __result)
                {
                    if (ShouldIncludeTarget(insectPawn, target, memoryComp))
                    {
                        filteredTargets.Add(target);
                    }
                }

                __result = filteredTargets;
            }

            private static bool ShouldIncludeTarget(Pawn insect, IAttackTarget target, InsectMemoryMapComp memoryComp)
            {
                Thing targetThing = target.Thing;

                // Always include aggressors
                if (IsOriginalAggressor(memoryComp, insect, targetThing))
                {
                    return true;
                }

                // For non-aggressors, apply distance filtering
                float distance = (insect.Position - targetThing.Position).LengthHorizontal;

                if (distance <= 10f)
                {
                    return true;
                }

                // Block distant non-aggressors
                return false;
            }

            private static bool IsOriginalAggressor(InsectMemoryMapComp memoryComp, Pawn insect, Thing target)
            {
                var aggressors = memoryComp.GetOriginalAggressors(insect);

                foreach (var aggressor in aggressors)
                {
                    if (aggressor.IsDefeated(insect.Map) || memoryComp.ShouldForgetAggressor(aggressor))
                        continue;

                    if ((aggressor.Pawn != null && target == aggressor.Pawn) ||
                        (aggressor.Faction != null && target.Faction == aggressor.Faction))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

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

                        if (Settings.enableAllInsectsAssault)
                        {
                            insectMemoryComp.SetAllInsectsToAssaultColony();
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