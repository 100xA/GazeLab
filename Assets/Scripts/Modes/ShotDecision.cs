// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>What a <see cref="ShotMode"/> decided this frame: nothing, fire, or an ignored attempt.</summary>
    public struct ShotDecision
    {
        public readonly bool Fire;
        public readonly bool IsIgnored;
        /// <summary>What triggered the decision, e.g. "Blink 0.42 s", shown as feedback.</summary>
        public readonly string Message;
        /// <summary>How old the target lock may be (the eyes may have moved during a blink or nod).</summary>
        public readonly float LockGraceSeconds;

        ShotDecision(bool fire, bool ignored, string message, float lockGraceSeconds)
        {
            Fire = fire;
            IsIgnored = ignored;
            Message = message ?? "";
            LockGraceSeconds = lockGraceSeconds;
        }

        public static ShotDecision None { get { return default(ShotDecision); } }
        public static ShotDecision FireLocked(string message, float lockGraceSeconds) { return new ShotDecision(true, false, message, lockGraceSeconds); }
        public static ShotDecision Ignore(string reason) { return new ShotDecision(false, true, reason, 0f); }
    }
}
