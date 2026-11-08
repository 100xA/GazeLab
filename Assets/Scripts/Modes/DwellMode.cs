// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
namespace GazeLab
{
    public sealed class DwellMode : ShotMode
    {
        public override string Name { get { return "Aim + Hold (Dwell)"; } }
        public override string Description(GameSettings settings)
        {
            return "Look at a target and keep looking for " + Format.Number(settings.dwellSeconds, "0.0#") + " s. Eyes only.";
        }

        public override string Instruction(GameSettings settings)
        {
            return "Keep looking at a target for " + Format.Number(settings.dwellSeconds, "0.0#") + " s";
        }

        public override float Progress(Targeting targeting, GameSettings settings)
        {
            if (targeting.AimTarget == null || settings.dwellSeconds <= 0f) return 0f;
            return UnityEngine.Mathf.Clamp01(targeting.AimSeconds / settings.dwellSeconds);
        }

        public override ShotDecision Decide(ShotContext context)
        {
            bool dwelled = context.Targeting.AimTarget != null && context.Targeting.AimSeconds >= context.Settings.dwellSeconds;
            return dwelled ? ShotDecision.FireLocked("Dwell", context.Settings.lockGraceSeconds) : ShotDecision.None;
        }
    }
}
