// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>One gaze reading in Unity screen pixels (bottom-left origin).</summary>
    public struct GazeSample
    {
        public readonly bool IsValid;
        public readonly Vector2 ScreenPoint;
        /// <summary>The tracker works but currently sees no eyes (both eyes closed). Used for blinks.</summary>
        public readonly bool EyesClosed;
        public readonly string Status;

        public GazeSample(bool isValid, Vector2 screenPoint, bool eyesClosed, string status)
        {
            IsValid = isValid;
            ScreenPoint = screenPoint;
            EyesClosed = eyesClosed;
            Status = status ?? "";
        }

        public static GazeSample Valid(Vector2 screenPoint, string status)
        {
            return new GazeSample(true, screenPoint, false, status);
        }

        public static GazeSample Invalid(string status, bool eyesClosed = false)
        {
            return new GazeSample(false, Vector2.zero, eyesClosed, status);
        }

        public static bool IsOnScreen(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
                   !float.IsInfinity(point.x) && !float.IsInfinity(point.y) &&
                   point.x >= 0 && point.y >= 0 && point.x < Screen.width && point.y < Screen.height;
        }
    }
}
