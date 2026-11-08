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
