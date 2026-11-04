// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Tobii.Gaming;
using Tobii.Gaming.Internal;
#endif

namespace GazeLab
{
    /// <summary>Gaze and head pose of the Tobii 4C through the course Tobii Unity SDK (Windows only).</summary>
    public sealed class TobiiGazeSource : IGazeSource
    {
        readonly GazeInputSettings settings;

        public TobiiGazeSource(GazeInputSettings settings) { this.settings = settings; }

        /// <summary>Windows display scaling detected at the last sample (1 = 100 %).</summary>
        public float DisplayScale { get; private set; } = 1f;
        public bool IsSimulation { get { return false; } }

        // The crash guard is armed before the native library is touched for the first time.
        public void Activate() { TobiiCrashGuard.BeginSession(); }
        public void Deactivate() { TobiiCrashGuard.EndSession(); }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        public GazeSample ReadGaze(float now)
        {
            string notReady;
            if (!IsReady(out notReady)) return GazeSample.Invalid(notReady);

            GazePoint sample = TobiiAPI.GetGazePoint();
            float age = now - sample.Timestamp;
            if (!sample.IsValid || age < 0f || age > settings.maximumSampleAge)
                return GazeSample.Invalid("Gaze lost (eyes closed?) - paused", eyesClosed: true);

            // Viewport uses Unity's bottom-left origin. Do not invert Y.
            Vector2 viewport = sample.Viewport;
            DisplayScale = DisplayScaling.Current();
            if (settings.compensateDisplayScaling && DisplayScale > 1.01f)
                viewport = DisplayScaling.Compensate(viewport, TobiiHost.GetInstance().GameViewInfo.NormalizedClientAreaBounds, DisplayScale);

            Vector2 point = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
            return GazeSample.IsOnScreen(point)
                ? GazeSample.Valid(point, "Tobii gaze active")
                : GazeSample.Invalid("Gaze outside the game - paused");
        }

        public HeadReading ReadHead(float now)
        {
            string notReady;
            if (!IsReady(out notReady)) return HeadReading.Unavailable("Head pose unavailable");
            HeadPose pose = TobiiAPI.GetHeadPose();
            float age = now - pose.Timestamp;
            if (!pose.IsValid || age < 0f || age > settings.maximumHeadPoseAge) return HeadReading.Unavailable("Head pose not detected");
            Vector3 euler = pose.Rotation.eulerAngles;
            return HeadReading.Angles(euler.x, euler.y);
        }

        static bool IsReady(out string status)
        {
            status = null;
#if UNITY_EDITOR_WIN
            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            if (target != UnityEditor.BuildTarget.StandaloneWindows64 && target != UnityEditor.BuildTarget.StandaloneWindows)
            {
                status = "Select Windows in File > Build Settings - paused";
                return false;
            }
#endif
            if (!TobiiEulaFile.IsEulaAccepted()) { status = "Accept the Tobii SDK license in Unity - paused"; return false; }
            if (!TobiiAPI.IsConnected) { status = "Tobii disconnected - paused"; return false; }
            return true;
        }
#else
        public GazeSample ReadGaze(float now) { return GazeSample.Invalid("The Tobii 4C requires Windows - paused"); }
        public HeadReading ReadHead(float now) { return HeadReading.Unavailable("Head pose needs the Tobii 4C on Windows"); }
#endif
    }
}
