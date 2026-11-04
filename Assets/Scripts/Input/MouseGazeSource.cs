// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Clearly labelled development mode without a tracker: the mouse pointer replaces the gaze,
    /// holding B simulates closed eyes and N simulates a head nod.
    /// </summary>
    public sealed class MouseGazeSource : IGazeSource
    {
        /// <summary>Optional extra text for the status line (e.g. a crash notice).</summary>
        public string Notice { get; set; }

        public bool IsSimulation { get { return true; } }
        public void Activate() { }
        public void Deactivate() { }

        public GazeSample ReadGaze(float now)
        {
            Vector2 point = Input.mousePosition;
            if (Input.GetKey(KeyCode.B)) return new GazeSample(false, point, true, "MOUSE SIMULATION - eyes closed (B held)");
            if (!GazeSample.IsOnScreen(point)) return GazeSample.Invalid("Move the pointer into the game");
            string status = string.IsNullOrEmpty(Notice) ? "MOUSE SIMULATION - no eye tracker" : "MOUSE SIMULATION - " + Notice;
            return GazeSample.Valid(point, status);
        }

        public HeadReading ReadHead(float now)
        {
            return HeadReading.Simulated(Input.GetKeyDown(KeyCode.N), "Simulation: press N to nod");
        }
    }
}
