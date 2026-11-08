// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    public sealed class MouseGazeSource : IGazeSource
    {
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
