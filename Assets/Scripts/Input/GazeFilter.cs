using UnityEngine;

namespace GazeLab
{
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
            float keep = Mathf.Pow(Mathf.Clamp(settings.smoothing, 0f, 0.95f), deltaTime * 60f);
            smoothed = Vector2.Lerp(raw, smoothed, keep);
            return smoothed;
        }
    }
}
