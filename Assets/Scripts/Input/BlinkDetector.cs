using UnityEngine;

namespace GazeLab
{
    public sealed class BlinkDetector
    {
        bool closed;
        float closedSince;
        Vector2 lastValidPoint, pointBeforeClosure;
        bool hasValidPoint, knewPointBeforeClosure;

        public bool EyesClosed { get; private set; }
        public float ClosedSeconds { get; private set; }
        public bool Ended { get; private set; }
        public float LastClosedSeconds { get; private set; }
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
