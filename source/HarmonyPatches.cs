using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace SK_Bug_Off
{
    public class HarmonyPatches
    {
        private static bool isInsectSelectingTargets = false;

        [HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        public static class Patch_JobGiver_AIGotoNearestHostile_TryGiveJob
        {
            public static void Prefix(Pawn pawn)
            {
                if (Utils.IsInsect(pawn))
                {
                    isInsectSelectingTargets = true;
                }
            }

            public static void Postfix(Pawn pawn)
            {
                isInsectSelectingTargets = false;
            }
        }

        [HarmonyPatch(typeof(AttackTargetsCache), "GetPotentialTargetsFor")]
        public static class Patch_AttackTargetsCache_GetPotentialTargetsFor
        {
            public static void Postfix(IAttackTargetSearcher th, ref List<IAttackTarget> __result)
            {
                if (!isInsectSelectingTargets || __result == null || __result.Count == 0)
                    return;

                if (!(th.Thing is Pawn insectPawn) || !Utils.IsInsect(insectPawn))
                    return;

                InsectMemoryMapComp memoryComp = insectPawn.Map?.GetComponent<InsectMemoryMapComp>();

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

                if (IsOriginalAggressor(memoryComp, insect, targetThing))
                {
                    return true;
                }

                float distance = (insect.Position - targetThing.Position).LengthHorizontal;

                if (distance <= Settings.aggroRadius)
                {
                    return true;
                }

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
                        insectMemoryComp.AddOriginalAggressor(pawn, new InsectAggressor(attacker));

                        if (attacker.Faction != null)
                        {
                            insectMemoryComp.AddOriginalAggressor(pawn, new InsectAggressor(attacker.Faction));
                        }

                        if (Settings.enableAllInsectsAssault)
                        {
                            insectMemoryComp.SetInsectsToAssaultColonyInRadius(pawn.Position);
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
                    insectMemoryComp.CleanupInsect(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(TrashUtility), "ShouldTrashBuilding", new Type[] { typeof(Pawn), typeof(Building), typeof(bool) })]
        public static class Patch_TrashUtility_ShouldTrashBuilding
        {
            public static void Postfix(Pawn pawn, Building b, bool attackAllInert, ref bool __result)
            {
                if (!Utils.IsInsect(pawn) || !__result)
                    return;

                InsectMemoryMapComp memoryComp = pawn.Map?.GetComponent<InsectMemoryMapComp>();

                if (!IsPlayerFactionAggressorToAnyInsect(memoryComp, pawn.Map))
                {
                    __result = false;
                }
            }

            private static bool IsPlayerFactionAggressorToAnyInsect(InsectMemoryMapComp memoryComp, Map map)
            {
                var allInsects = map.mapPawns.AllPawnsSpawned.Where(p => Utils.IsInsect(p));

                foreach (var insect in allInsects)
                {
                    var aggressors = memoryComp.GetOriginalAggressors(insect);

                    foreach (var aggressor in aggressors)
                    {
                        if (aggressor.IsDefeated(map) || memoryComp.ShouldForgetAggressor(aggressor))
                            continue;

                        if ((aggressor.Pawn != null && aggressor.Pawn.Faction == Faction.OfPlayer) ||
                            (aggressor.Faction != null && aggressor.Faction == Faction.OfPlayer))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}