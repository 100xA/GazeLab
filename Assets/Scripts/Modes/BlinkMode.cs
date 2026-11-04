// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>
    /// Aim, then close both eyes a little longer than a natural blink. Closures shorter than
    /// <see cref="GameSettings.blinkMinSeconds"/> (natural blinks) and longer than
    /// <see cref="GameSettings.blinkMaxSeconds"/> are ignored.
    /// </summary>
    public sealed class BlinkMode : ShotMode
    {
        public override string Name { get { return "Aim + Blink"; } }
        public override string Description(GameSettings settings)
        {
            return "Look at a target, then close both eyes a little longer than a normal blink.\nSimulation: hold B";
        }
        public override string LockHint { get { return "BLINK"; } }
        public override bool ShowsEyesClosedTimer { get { return true; } }

        public override string Instruction(GameSettings settings)
        {
            return "Aim, then close both eyes " + Format.Number(settings.blinkMinSeconds, "0.0#") + "–" +
                   Format.Number(settings.blinkMaxSeconds, "0.0#") + " s";
        }

        public override bool ToleratesGazeLoss(GazeInput gaze, GameSettings settings)
        {
            return gaze.Blink.EyesClosed && gaze.Blink.ClosedSeconds <= settings.blinkMaxSeconds;
        }

        public override ShotDecision Decide(ShotContext context)
        {
            BlinkDetector blink = context.Gaze.Blink;
            if (!blink.Ended) return ShotDecision.None;
            float closed = blink.LastClosedSeconds;
            if (closed < context.Settings.blinkMinSeconds)
                return ShotDecision.Ignore("Blink " + Format.Number(closed, "0.00") + " s - too short, ignored");
            if (closed > context.Settings.blinkMaxSeconds)
                return ShotDecision.Ignore("Eyes closed " + Format.Number(closed, "0.0") + " s - too long, ignored");
            // The lock must survive the closure itself.
            return ShotDecision.FireLocked("Blink " + Format.Number(closed, "0.00") + " s", context.Settings.lockGraceSeconds + closed);
        }
    }
}
