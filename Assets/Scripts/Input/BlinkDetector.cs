// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Measures how long the eyes were closed. "Closed" means the tracker works but sees no gaze;
    /// only a return to valid gaze ends a closure, tracker loss or focus loss discard it.
    /// The tracker reports both eyes together, so closing a single eye is not detected.
    /// </summary>
    public sealed class BlinkDetector
    {
        bool closed;
        float closedSince;
        Vector2 lastValidPoint, pointBeforeClosure;
        bool hasValidPoint, knewPointBeforeClosure;

        public bool EyesClosed { get; private set; }
        public float ClosedSeconds { get; private set; }
        /// <summary>True for exactly one update when the eyes reopen.</summary>
        public bool Ended { get; private set; }
        public float LastClosedSeconds { get; private set; }
        /// <summary>
        /// Screen distance (pixels) between the gaze before and after the last closure, or infinity if
        /// unknown. A real blink reopens the eyes where they were; looking away and back usually does not.
        /// </summary>
        public float LastGazeShiftPixels { get; private set; }

        public void Reset()
        {
            closed = false;
            EyesClosed = false;
            ClosedSeconds = 0f;
            Ended = false;
            hasValidPoint = false;
        }

        public void Update(GazeSample sample, float now)
        {
            Ended = false;
            if (sample.EyesClosed && !sample.IsValid)
            {
                if (!closed)
                {
                    closed = true;
                    closedSince = now;
                    pointBeforeClosure = lastValidPoint;
                    knewPointBeforeClosure = hasValidPoint;
                }
                EyesClosed = true;
                ClosedSeconds = now - closedSince;
                return;
            }
            if (closed && sample.IsValid)
            {
                Ended = true;
                LastClosedSeconds = now - closedSince;
                LastGazeShiftPixels = knewPointBeforeClosure
                    ? Vector2.Distance(pointBeforeClosure, sample.ScreenPoint)
                    : float.PositiveInfinity;
            }
            hasValidPoint = sample.IsValid;
            if (sample.IsValid) lastValidPoint = sample.ScreenPoint;
            closed = false;
            EyesClosed = false;
            ClosedSeconds = 0f;
        }
    }
}
