// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>Result of the last confirmation attempt, shown to the player.</summary>
    public struct ShotFeedback
    {
        public readonly string Text;
        public readonly bool Fired;
        public readonly float Time;

        public ShotFeedback(string text, bool fired, float time)
        {
            Text = text ?? "";
            Fired = fired;
            Time = time;
        }

        public static ShotFeedback None { get { return new ShotFeedback("", false, float.NegativeInfinity); } }
    }
}
