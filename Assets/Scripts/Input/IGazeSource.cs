namespace GazeLab
{
    public interface IGazeSource
    {
        bool IsSimulation { get; }
        void Activate();
        void Deactivate();
        GazeSample ReadGaze(float now);
        HeadReading ReadHead(float now);
    }
}
