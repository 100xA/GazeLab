// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System;
using UnityEngine;

namespace GazeLab
{
    /// <summary>Everything a <see cref="ShotMode"/> may look at to decide whether to fire.</summary>
    public struct ShotContext
    {
        public readonly GazeInput Gaze;
        public readonly Targeting Targeting;
        public readonly GameSettings Settings;
        /// <summary>True if a screen point lies on a HUD control (clicks there are not shots).</summary>
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
