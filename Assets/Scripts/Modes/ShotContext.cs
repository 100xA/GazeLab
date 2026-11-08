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
