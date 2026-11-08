// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
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
