// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// The Tobii native library can crash Unity when the installed Tobii runtime does not fit the SDK.
    /// A flag in PlayerPrefs marks a running Tobii session; it is only cleared when the session ends
    /// normally. If it is still set at the next start, Unity crashed and the game falls back to the
    /// mouse once. Otherwise the game always starts with the eye tracker.
    /// </summary>
    public static class TobiiCrashGuard
    {
        const string PrefSessionRunning = "GazeLab.TobiiInitPending";

        static bool sessionRunning;

        public static bool CrashedLastTime { get; private set; }

        /// <summary>Call once at startup. Returns whether the game should start with Tobii.</summary>
        public static bool ShouldStartWithTobii()
        {
            if (PlayerPrefs.GetInt(PrefSessionRunning, 0) == 1)
            {
                CrashedLastTime = true;
                PlayerPrefs.SetInt(PrefSessionRunning, 0);
                PlayerPrefs.Save();
                return false;
            }
            return true;
        }

        public static void BeginSession()
        {
            if (sessionRunning) return;
            sessionRunning = true;
            CrashedLastTime = false;
            PlayerPrefs.SetInt(PrefSessionRunning, 1);
            PlayerPrefs.Save();
        }

        public static void EndSession()
        {
            if (!sessionRunning) return;
            sessionRunning = false;
            PlayerPrefs.SetInt(PrefSessionRunning, 0);
            PlayerPrefs.Save();
        }
    }
}
