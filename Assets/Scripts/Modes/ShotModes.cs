// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
namespace GazeLab
{
    public static class ShotModes
    {
        public static readonly ShotMode Dwell = new DwellMode();
        public static readonly ShotMode Blink = new BlinkMode();
        public static readonly ShotMode Nod = new NodMode();
        public static readonly ShotMode Button = new ButtonMode();

        public static readonly ShotMode[] All = { Dwell, Blink, Nod, Button };
    }
}
