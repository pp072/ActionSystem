namespace ActionSystem
{
    public enum FlowControlResult
    {
        Continue = -1,
        Stop = -2,
        // Positive values = GoTo index
    }

    public interface IFlowControlAction
    {
        int GetNextIndex();
    }
}
