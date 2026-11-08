namespace GazeLab
{
    public sealed class NodMode : ShotMode
    {
        public override string Name { get { return "Aim + Nod"; } }
        public override string Description(GameSettings settings) { return "Look at a target, then nod your head once.\nSimulation: press N"; }
        public override string LockHint { get { return "NOD"; } }
        public override bool UsesHeadPose { get { return true; } }

        public override string Instruction(GameSettings settings) { return "Aim, then nod once"; }

        public override ShotDecision Decide(ShotContext context)
        {
            return context.Gaze.NodDetected ? ShotDecision.FireLocked("Nod", context.Settings.lockGraceSeconds) : ShotDecision.None;
        }
    }
}
