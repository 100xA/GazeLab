// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>A provider of gaze (and optionally head) data: Tobii tracker, mouse simulation or a test fake.</summary>
    public interface IGazeSource
    {
        /// <summary>True for the labelled mouse simulation (no smoothing, shown in amber).</summary>
        bool IsSimulation { get; }
        void Activate();
        void Deactivate();
        GazeSample ReadGaze(float now);
        HeadReading ReadHead(float now);
    }
}
