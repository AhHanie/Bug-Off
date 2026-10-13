using Verse;

public class ReinforcementAssignment : IExposable
{
    public IntVec3 assignmentPosition;
    public int assignmentTick;

    public ReinforcementAssignment()
    {
    }

    public ReinforcementAssignment(IntVec3 position, int tick)
    {
        this.assignmentPosition = position;
        this.assignmentTick = tick;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref assignmentPosition, "assignmentPosition");
        Scribe_Values.Look(ref assignmentTick, "assignmentTick", 0);
    }
}