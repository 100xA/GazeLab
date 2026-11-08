// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
namespace GazeLab
{
    public abstract class ShotMode
    {
        public abstract string Name { get; }
        public abstract string Description(GameSettings settings);
        public abstract string Instruction(GameSettings settings);
        public virtual string LockHint { get { return null; } }
        public virtual bool UsesHeadPose { get { return false; } }
        public virtual bool ShowsEyesClosedTimer { get { return false; } }

        public virtual float Progress(Targeting targeting, GameSettings settings) { return 0f; }

        public virtual bool ToleratesGazeLoss(GazeInput gaze, GameSettings settings) { return false; }

        public abstract ShotDecision Decide(ShotContext context);
    }
}
