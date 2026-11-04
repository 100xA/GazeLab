// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>
    /// One solution to the Midas-touch problem: decides when looking at a target becomes a shot.
    /// Texts for the menu and HUD live here too, so a new mode is a single new class
    /// (plus an entry in <see cref="ShotModes.All"/>).
    /// </summary>
    public abstract class ShotMode
    {
        public abstract string Name { get; }
        /// <summary>Text on the menu tile.</summary>
        public abstract string Description(GameSettings settings);
        /// <summary>Short instruction in the status card.</summary>
        public abstract string Instruction(GameSettings settings);
        /// <summary>Label next to the gaze marker while a target is locked (null: progress ring instead).</summary>
        public virtual string LockHint { get { return null; } }
        public virtual bool UsesHeadPose { get { return false; } }
        /// <summary>Show the "Eyes closed x.xx s" timer while the eyes are closed.</summary>
        public virtual bool ShowsEyesClosedTimer { get { return false; } }

        /// <summary>0..1 progress shown in the ring around the gaze marker.</summary>
        public virtual float Progress(Targeting targeting, GameSettings settings) { return 0f; }

        /// <summary>Whether the round keeps running while the gaze is invalid (e.g. eyes closed for a blink).</summary>
        public virtual bool ToleratesGazeLoss(GazeInput gaze, GameSettings settings) { return false; }

        public abstract ShotDecision Decide(ShotContext context);
    }
}
