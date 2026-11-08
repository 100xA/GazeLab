// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
namespace GazeLab
{
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
