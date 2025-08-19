using HarmonyLib;
using RimWorld;
using Verse.AI.Group;

[HarmonyPatch(typeof(LordJob_DefendAndExpandHive), "CreateGraph")]
public static class LordJob_DefendAndExpandHive_CreateGraph_Patch
{
    public static void Postfix(ref StateGraph __result)
    {
        // Find the LordToil_DefendHiveAggressively to use as replacement target
        LordToil_DefendHiveAggressively defendHiveAggressively = null;
        foreach (var toil in __result.lordToils)
        {
            if (toil is LordToil_DefendHiveAggressively)
            {
                defendHiveAggressively = toil as LordToil_DefendHiveAggressively;
                break;
            }
        }

        // Find and modify the specific transition that goes to LordToil_AssaultColony
        foreach (Transition transition in __result.transitions)
        {
            if (ShouldModifyTransition(transition))
            {
                transition.target = defendHiveAggressively;
            }
        }
    }

    private static bool ShouldModifyTransition(Transition transition)
    {
        // Check if target is LordToil_AssaultColony
        if (transition.target.GetType() != typeof(LordToil_AssaultColony))
            return false;

        // Check if source contains LordToil_DefendAndExpandHive
        bool hasDefendAndExpandSource = false;
        foreach (var source in transition.sources)
        {
            if (source.GetType() == typeof(LordToil_DefendAndExpandHive))
            {
                hasDefendAndExpandSource = true;
                break;
            }
        }
        if (!hasDefendAndExpandSource)
            return false;

        // Check if it has the specific trigger pattern we want to modify
        // The transition we want to modify has:
        // - 1 Trigger_PawnHarmed with requireInstigatorWithFaction: true
        // - 1 Trigger_PawnLostViolently
        // - 4 Trigger_Memo triggers
        // - 1 TransitionAction_EndAllJobs post action
        return HasSpecificTriggerPattern(transition);
    }

    private static bool HasSpecificTriggerPattern(Transition transition)
    {
        // Count trigger types
        int pawnHarmedCount = 0;
        int pawnLostViolentlyCount = 0;
        int memoCount = 0;
        bool hasPawnHarmedWithInstigatorFaction = false;

        foreach (var trigger in transition.triggers)
        {
            if (trigger is Trigger_PawnHarmed)
            {
                pawnHarmedCount++;
                // Check if this is the specific PawnHarmed trigger with requireInstigatorWithFaction: true
                // We can use reflection to check the field, or just assume if there's only one PawnHarmed trigger
                // and it's part of a transition with the right pattern, it's the one we want
                if (pawnHarmedCount == 1) // First (and should be only) PawnHarmed trigger
                    hasPawnHarmedWithInstigatorFaction = true;
            }
            else if (trigger is Trigger_PawnLostViolently)
            {
                pawnLostViolentlyCount++;
            }
            else if (trigger is Trigger_Memo)
            {
                memoCount++;
            }
        }

        // Check if it has the expected post action
        bool hasEndAllJobsAction = false;
        foreach (var action in transition.postActions)
        {
            if (action is TransitionAction_EndAllJobs)
            {
                hasEndAllJobsAction = true;
                break;
            }
        }

        // The specific transition pattern:
        // - Exactly 1 PawnHarmed trigger (with instigator faction requirement)
        // - Exactly 1 PawnLostViolently trigger  
        // - Exactly 4 Memo triggers
        // - Has TransitionAction_EndAllJobs post action
        return pawnHarmedCount == 1 &&
               pawnLostViolentlyCount == 1 &&
               memoCount == 4 &&
               hasPawnHarmedWithInstigatorFaction &&
               hasEndAllJobsAction;
    }
}