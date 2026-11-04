// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

/// <summary>
/// Detects one deliberate head nod from a stream of head-pose angles (degrees).
/// A nod is a pitch excursion of at least <see cref="PitchThreshold"/> away from a slowly
/// adapting neutral pose that returns close to neutral within <see cref="MaxNodSeconds"/>.
/// Head shaking (yaw dominated) and slow posture changes are ignored.
/// It never reads a clock itself, so it can be driven deterministically (see GazeLabValidation).
/// </summary>
public sealed class HeadNodDetector
{
    public float PitchThreshold = 9f;    // degrees away from neutral to start a nod
    public float ReturnThreshold = 4f;   // degrees from neutral that count as "back"
    public float MaxNodSeconds = 0.9f;   // whole down-and-back movement must fit in this window
    public float CooldownSeconds = 0.45f;
    public float NeutralFollowRate = 1.5f; // 1/s, how fast neutral adapts while idle

    enum Phase { Unset, Idle, Moving, Cooldown }
    Phase phase = Phase.Unset;
    float neutralPitch, neutralYaw, phaseStart, peakPitch, peakYaw;

    /// <summary>Pitch deviation from the neutral pose, for feedback.</summary>
    public float PitchDeviation { get; private set; }
    public bool IsMoving { get { return phase == Phase.Moving; } }

    public void Reset() { phase = Phase.Unset; PitchDeviation = 0f; }

    /// <summary>Feeds one sample; returns true exactly once per completed nod.</summary>
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
                    // Too slow (posture change) or a head shake: adopt the new pose as neutral.
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

            default: // Cooldown
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
