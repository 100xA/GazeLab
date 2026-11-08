// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using UnityEngine;

namespace GazeLab
{
    public sealed class ButtonMode : ShotMode
    {
        public override string Name { get { return "Aim + Space / Click"; } }
        public override string Description(GameSettings settings) { return "Look at a target, then press Space or the left mouse button."; }
        public override string LockHint { get { return "SPACE / CLICK"; } }

        public override string Instruction(GameSettings settings) { return "Aim, then press Space / left click"; }

        public override ShotDecision Decide(ShotContext context)
        {
            if (!Application.isFocused) return ShotDecision.None;
            if (Input.GetKeyDown(KeyCode.Space)) return ShotDecision.FireLocked("Space", context.Settings.lockGraceSeconds);
            bool clickOnGame = Input.GetMouseButtonDown(0) && (context.IsOverUi == null || !context.IsOverUi(Input.mousePosition));
            return clickOnGame ? ShotDecision.FireLocked("Click", context.Settings.lockGraceSeconds) : ShotDecision.None;
        }
    }
}
