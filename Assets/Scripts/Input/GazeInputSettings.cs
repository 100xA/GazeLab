// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System;
using UnityEngine;

namespace GazeLab
{
    /// <summary>Tuning values of the gaze input, editable in the Inspector of <see cref="GazeInput"/>.</summary>
    [Serializable]
    public class GazeInputSettings
    {
        [Tooltip("Tobii samples older than this count as lost gaze (eyes closed).")]
        [Range(0.02f, 0.5f)] public float maximumSampleAge = 0.1f;

        [Tooltip("Smoothing of small gaze jitter (0 = raw gaze). Saccades are never smoothed.")]
        [Range(0f, 0.95f)] public float smoothing = 0.75f;

        [Tooltip("Gaze jumps larger than this (pixels) are saccades and are followed immediately.")]
        public float saccadeThresholdPixels = 90f;

        [Tooltip("Undo the Tobii mapping error under Windows display scaling (e.g. 150 %).")]
        public bool compensateDisplayScaling = true;

        [Tooltip("Head poses older than this are ignored.")]
        public float maximumHeadPoseAge = 0.25f;
    }
}
