// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System;
using UnityEngine;

namespace GazeLab
{
    /// <summary>All gameplay tuning values in one place, editable in the Inspector of the GameController.</summary>
    [Serializable]
    public class GameSettings
    {
        [Header("Shooting modes")]
        [Tooltip("Dwell mode: how long a target must be looked at.")]
        public float dwellSeconds = 0.6f;
        [Tooltip("Blink mode: shortest eye closure that counts (natural blinks are shorter).")]
        public float blinkMinSeconds = 0.15f;
        [Tooltip("Blink mode: longest eye closure that counts; longer closures pause the game.")]
        public float blinkMaxSeconds = 1.5f;
        [Tooltip("Blink mode: the eyes must reopen this close to where they closed (fraction of the " +
                 "screen height); looking away and back is not a blink.")]
        public float blinkMaxGazeShift = 0.15f;
        [Tooltip("How long a target stays locked after the gaze left it (blink, nod, button).")]
        public float lockGraceSeconds = 0.35f;

        [Header("Gaze targeting")]
        [Tooltip("A gaze point this close to a target (world units, ~65 px at 1080p) still selects it.")]
        public float gazeToleranceWorld = 0.6f;
        [Tooltip("Aim progress survives gaze jitter that leaves the target for at most this long.")]
        public float aimLossToleranceSeconds = 0.15f;

        [Header("Round")]
        public float spawnIntervalSeconds = 4f;
        public int maxShipHits = 4;
        public int rockPoints = 20;
        public int satellitePoints = -100;
        [Tooltip("Duration of the laser shot animation.")]
        public float beamVisibleSeconds = 0.3f;
        [Tooltip("Volume of the sound effects (0 = muted).")]
        [Range(0f, 1f)] public float soundVolume = 0.8f;

        [Header("Spawning")]
        public float spawnX = 13f;
        public float rockSpawnHalfHeight = 3.35f;
        public float satelliteSpawnHalfHeight = 3.7f;
        public float firstRockY = 2.4f;
        public float firstSatelliteY = -2.4f;
    }
}
