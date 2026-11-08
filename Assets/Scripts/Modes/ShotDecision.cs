// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
namespace GazeLab
{
    public struct ShotDecision
    {
        public readonly bool Fire;
        public readonly bool IsIgnored;
        public readonly string Message;
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
