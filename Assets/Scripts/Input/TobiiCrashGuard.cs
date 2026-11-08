// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    public static class TobiiCrashGuard
    {
        const string PrefSessionRunning = "GazeLab.TobiiInitPending";

        static bool sessionRunning;

        public static bool CrashedLastTime { get; private set; }

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
