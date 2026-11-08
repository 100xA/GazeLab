// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    public sealed class HeadNodDetector
    {
        public float PitchThreshold { get; set; } = 9f;
        public float ReturnThreshold { get; set; } = 4f;
        public float MaxNodSeconds { get; set; } = 0.9f;
        public float CooldownSeconds { get; set; } = 0.45f;
        public float NeutralFollowRate { get; set; } = 1.5f;

        enum Phase { Unset, Idle, Moving, Cooldown }
        Phase phase = Phase.Unset;
        float neutralPitch, neutralYaw, phaseStart, peakPitch, peakYaw;

        public float PitchDeviation { get; private set; }
        public bool IsMoving { get { return phase == Phase.Moving; } }

        public void Reset() { phase = Phase.Unset; PitchDeviation = 0f; }

        public bool Update(float pitch, float yaw, float time, float deltaTime)
        {
            if (phase == Phase.Unset)
            {
                neutralPitch = pitch;
                neutralYaw = yaw;
                phase = Phase.Idle;
            }
            float dp = Mathf.DeltaAngle(neutralPitch, pitch);
            float dy = Mathf.DeltaAngle(neutralYaw, yaw);
            PitchDeviation = dp;

            switch (phase)
            {
                case Phase.Idle:
                    if (Mathf.Abs(dp) >= PitchThreshold && Mathf.Abs(dp) > Mathf.Abs(dy))
                    {
                        phase = Phase.Moving;
                        phaseStart = time;
                        peakPitch = dp;
                        peakYaw = dy;
                    }
                    else Follow(pitch, yaw, deltaTime);
                    return false;

                case Phase.Moving:
                    if (Mathf.Abs(dp) > Mathf.Abs(peakPitch)) peakPitch = dp;
                    if (Mathf.Abs(dy) > Mathf.Abs(peakYaw)) peakYaw = dy;
                    if (time - phaseStart > MaxNodSeconds || Mathf.Abs(peakYaw) > Mathf.Abs(peakPitch))
                    {
                        neutralPitch = pitch;
                        neutralYaw = yaw;
                        phase = Phase.Idle;
                        return false;
                    }
                    if (Mathf.Abs(dp) <= ReturnThreshold)
                    {
                        phase = Phase.Cooldown;
                        phaseStart = time;
                        return true;
                    }
                    return false;

                default:
                    Follow(pitch, yaw, deltaTime);
                    if (time - phaseStart >= CooldownSeconds) phase = Phase.Idle;
                    return false;
            }
        }

        void Follow(float pitch, float yaw, float deltaTime)
        {
            float k = Mathf.Clamp01(NeutralFollowRate * deltaTime);
            neutralPitch = Mathf.LerpAngle(neutralPitch, pitch, k);
            neutralYaw = Mathf.LerpAngle(neutralYaw, yaw, k);
        }
    }
}
