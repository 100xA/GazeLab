// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// The Tobii native library can crash Unity when the installed Tobii runtime does not fit the SDK.
    /// A flag in PlayerPrefs marks a running Tobii session; it is only cleared when the session ends
    /// normally. If it is still set at the next start, Unity crashed and the game falls back to the
    /// mouse. A session that ran for a few seconds makes Tobii the preferred source.
    /// </summary>
    public static class TobiiCrashGuard
    {
        const string PrefPreferTobii = "GazeLab.UseTobii";
        const string PrefSessionRunning = "GazeLab.TobiiInitPending";
        const float RememberAfterSeconds = 3f;

        static float sessionStart = -1f;

        public static bool CrashedLastTime { get; private set; }

        /// <summary>Call once at startup. Returns whether the game should start with Tobii.</summary>
        public static bool ShouldStartWithTobii()
        {
            if (PlayerPrefs.GetInt(PrefSessionRunning, 0) == 1)
            {
                CrashedLastTime = true;
                PlayerPrefs.SetInt(PrefSessionRunning, 0);
                PlayerPrefs.SetInt(PrefPreferTobii, 0);
                PlayerPrefs.Save();
                return false;
            }
            return PlayerPrefs.GetInt(PrefPreferTobii, 0) == 1;
        }

        public static void BeginSession(float now)
        {
            if (sessionStart >= 0f) return;
            sessionStart = now;
            CrashedLastTime = false;
            PlayerPrefs.SetInt(PrefSessionRunning, 1);
            PlayerPrefs.Save();
        }

        public static void EndSession(float now)
        {
            if (sessionStart < 0f) return;
            bool usedLongEnough = now - sessionStart >= RememberAfterSeconds;
            sessionStart = -1f;
            PlayerPrefs.SetInt(PrefSessionRunning, 0);
            if (usedLongEnough) PlayerPrefs.SetInt(PrefPreferTobii, 1);
            PlayerPrefs.Save();
        }

        /// <summary>The player chose the mouse: do not start with Tobii next time.</summary>
        public static void ForgetTobii()
        {
            PlayerPrefs.SetInt(PrefPreferTobii, 0);
            PlayerPrefs.Save();
        }
    }
}
