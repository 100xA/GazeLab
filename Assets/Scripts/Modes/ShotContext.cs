// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using System;
using UnityEngine;

namespace GazeLab
{
    public struct ShotContext
    {
        public readonly GazeInput Gaze;
        public readonly Targeting Targeting;
        public readonly GameSettings Settings;
        public readonly Func<Vector2, bool> IsOverUi;

        public ShotContext(GazeInput gaze, Targeting targeting, GameSettings settings, Func<Vector2, bool> isOverUi)
        {
            Gaze = gaze;
            Targeting = targeting;
            Settings = settings;
            IsOverUi = isOverUi;
        }
    }
}
