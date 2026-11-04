// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>The available shooting modes, in menu order.</summary>
    public static class ShotModes
    {
        public static readonly ShotMode Dwell = new DwellMode();
        public static readonly ShotMode Blink = new BlinkMode();
        public static readonly ShotMode Nod = new NodMode();
        public static readonly ShotMode Button = new ButtonMode();

        public static readonly ShotMode[] All = { Dwell, Blink, Nod, Button };
    }
}
