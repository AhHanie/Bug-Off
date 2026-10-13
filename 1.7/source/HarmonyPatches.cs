using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SK_Bug_Off
{
    public class HarmonyPatches
    {
        [HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        public static class Patch_JobGiver_AIGotoNearestHostile_TryGiveJob
        {
            [HarmonyInfix(typeof(AttackTargetsCache), nameof(AttackTargetsCache.GetPotentialTargetsFor))]
            public static List<IAttackTarget> Postfix(List<IAttackTarget> __result, IAttackTargetSearcher th)
            {
                if (__result == null || __result.Count == 0)
                    return __result;

                if (!(th.Thing is Pawn insectPawn) || !Utils.IsInsect(insectPawn) || insectPawn.Map == null)
                    return __result;

                InsectMemoryMapComp memoryComp = insectPawn.Map.GetComponent<InsectMemoryMapComp>();

                if (memoryComp.AllHivesDestroyed)
                    return __result;

                List<InsectAggressor> aggressors = memoryComp.GetOriginalAggressors(insectPawn);
                var filteredTargets = new List<IAttackTarget>();

                foreach (IAttackTarget target in __result)
                {
                    if (ShouldIncludeTarget(insectPawn, target, aggressors, memoryComp))
                    {
                        filteredTargets.Add(target);
                    }
                }

                return filteredTargets;
            }

            private static bool ShouldIncludeTarget(Pawn insect, IAttackTarget target, List<InsectAggressor> aggressors, InsectMemoryMapComp memoryComp)
            {
                Thing targetThing = target.Thing;

                if (IsOriginalAggressor(aggressors, memoryComp, insect, targetThing))
                {
                    return true;
                }

                float distance = (insect.Position - targetThing.Position).LengthHorizontal;

                return distance <= Settings.aggroRadius;
            }

            private static bool IsOriginalAggressor(List<InsectAggressor> aggressors, InsectMemoryMapComp memoryComp, Pawn insect, Thing target)
            {
                foreach (InsectAggressor aggressor in aggressors)
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

        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        public static class Patch_TakeDamage
        {
            [HarmonyInfix(typeof(Thing), nameof(Thing.PreApplyDamage))]
            public static void Prefix(Thing __instance, DamageInfo dinfo)
            {
                if (!(__instance is Pawn pawn) || !Utils.IsInsect(pawn) || dinfo.Instigator == null || pawn.Map == null)
                    return;

                if (!(dinfo.Instigator is Pawn attacker))
                    return;

                InsectMemoryMapComp insectMemoryComp = pawn.Map.GetComponent<InsectMemoryMapComp>();

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

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Destroy))]
        public static class Patch_PawnDestroy
        {
            public static void Prefix(Pawn __instance)
            {
                if (!Utils.IsInsect(__instance))
                    return;

                __instance.MapHeld?.GetComponent<InsectMemoryMapComp>()?.CleanupInsect(__instance);
            }
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        public static class Patch_Kill
        {
            [HarmonyInfix(typeof(GenLeaving), nameof(GenLeaving.DoLeavingsFor), typeof(Thing), typeof(Map), typeof(DestroyMode), typeof(List<Thing>))]
            public static void Postfix(Thing diedThing, Map map, [HarmonyOuter] DamageInfo? dinfo)
            {
                if (dinfo == null || map == null || !(diedThing is Pawn pawn) || !Utils.IsInsect(pawn))
                    return;

                if (dinfo.Value.Instigator == null)
                    return;

                map.GetComponent<InsectMemoryMapComp>().HandleInsectDeath(pawn);
            }
        }

        [HarmonyPatch(typeof(TrashUtility), nameof(TrashUtility.ShouldTrashBuilding), new Type[] { typeof(Pawn), typeof(Building), typeof(bool) })]
        public static class Patch_TrashUtility_ShouldTrashBuilding
        {
            public static void Postfix(Pawn pawn, ref bool __result)
            {
                if (!__result || !Utils.IsInsect(pawn) || pawn.Map == null)
                    return;

                InsectMemoryMapComp memoryComp = pawn.Map.GetComponent<InsectMemoryMapComp>();

                if (memoryComp.AllHivesDestroyed)
                    return;

                if (!memoryComp.IsPlayerFactionAggressorToAnyInsect())
                {
                    __result = false;
                }
            }
        }
    }
}
