namespace MadMaps.Roads
{
    public interface IOnPostBakeCallback
    {
        int GetPriority();
        void OnPostBake();
    }
}