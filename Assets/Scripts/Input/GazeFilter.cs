// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Damps fixation jitter of the tracker, but follows saccades (large jumps) immediately.</summary>
    public sealed class GazeFilter
    {
        readonly GazeInputSettings settings;
        Vector2 smoothed;
        bool hasValue;

        public GazeFilter(GazeInputSettings settings) { this.settings = settings; }

        public void Reset() { hasValue = false; }

        public Vector2 Apply(Vector2 raw, float deltaTime)
        {
            if (!hasValue || Vector2.Distance(raw, smoothed) > settings.saccadeThresholdPixels)
            {
                smoothed = raw;
                hasValue = true;
                return raw;
            }
            // Frame-rate independent exponential smoothing; 'smoothing' is the retention per 1/60 s.
            float keep = Mathf.Pow(Mathf.Clamp(settings.smoothing, 0f, 0.95f), deltaTime * 60f);
            smoothed = Vector2.Lerp(raw, smoothed, keep);
            return smoothed;
        }
    }
}
