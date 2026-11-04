// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// In-game feedback: crosshair gaze marker with progress ring or lock hint, target corner
    /// brackets, eyes-closed timer, shot feedback and HIT.
    /// </summary>
    public sealed class GameOverlay
    {
        /// <summary>Elements drawn below the HUD: target highlight and the eyes-closed timer.</summary>
        public void DrawBehindHud(HudTheme theme, GameController game, GazeInput gaze)
        {
            if (game.State != GameState.Playing) return;
            DrawTargetHighlight(game);
            DrawEyesClosed(theme, game, gaze);
        }

        public void DrawGazeMarker(HudTheme theme, GameController game, GazeInput gaze)
        {
            if (!gaze.HasValidGaze) return;
            float s = HudTheme.Scale;
            Vector2 point = HudTheme.ToGui(gaze.ScreenPoint);
            DrawCrosshair(theme, point, s);
            if (game.State != GameState.Playing) return;

            string hint = game.Mode.LockHint;
            if (hint == null)
            {
                // Dwell: dark track with the yellow progress on top.
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                HudTheme.Ring(point, 33 * s, 1f, 6 * s);
                GUI.color = HudTheme.ShipYellow;
                HudTheme.Ring(point, 33 * s, game.ModeProgress, 6 * s);
                return;
            }
            if (game.Targeting.AimTarget == null) return;
            GUI.color = HudTheme.ShipYellow;
            HudTheme.Ring(point, 33 * s, 1f, 5 * s);
            Rect pill = new Rect(point.x + 44 * s, point.y - 14 * s, (30 + hint.Length * 9) * s, 28 * s);
            theme.Plate(pill, HudTheme.ShipYellow, 3f);
            HudTheme.Write(pill, hint, theme.Chip, HudTheme.Ink);
        }

        /// <summary>Four yellow arms with a dark outline and a gap in the middle, plus a centre dot.</summary>
        static void DrawCrosshair(HudTheme theme, Vector2 center, float s)
        {
            const float Gap = 7f, Arm = 24f;
            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            GUI.color = HudTheme.Ink;
            foreach (Vector2 d in directions) HudTheme.Line(center + d * (Gap - 1.5f) * s, center + d * (Arm + 1.5f) * s, 7 * s);
            GUI.color = HudTheme.ShipYellow;
            foreach (Vector2 d in directions) HudTheme.Line(center + d * Gap * s, center + d * Arm * s, 3.5f * s);
            theme.Circle(new Rect(center.x - 3.5f * s, center.y - 3.5f * s, 7 * s, 7 * s), HudTheme.Ink);
            theme.Circle(new Rect(center.x - 2f * s, center.y - 2f * s, 4 * s, 4 * s), HudTheme.ShipYellow);
        }

        public void DrawShotFeedback(HudTheme theme, GameController game, float now)
        {
            if (game.State == GameState.Menu) return;
            ShotFeedback feedback = game.LastFeedback;
            if (now - feedback.Time < 1.2f && feedback.Text.Length > 0)
            {
                Color color = feedback.Fired ? HudTheme.Good : HudTheme.Warn;
                Rect pill = HudTheme.R(HudTheme.Width * 0.5f - 240, HudTheme.Height - 92, 480, 40);
                theme.Plate(pill, color, 4f);
                HudTheme.Write(pill, feedback.Text, theme.Button, HudTheme.Ink);
            }
            if (now - game.Cannon.LastShotTime < 0.25f)
                HudTheme.Shadowed(HudTheme.R(HudTheme.Width * 0.5f - 150, 20, 300, 80), "HIT!", theme.Hit, HudTheme.ShipYellow, HudTheme.ShipRed, 5f);
        }

        static void DrawTargetHighlight(GameController game)
        {
            Collider2D target = game.Targeting.AimTarget;
            Camera cam = Camera.main;
            if (target == null || cam == null) return;
            Vector2 lo = cam.WorldToScreenPoint(target.bounds.min);
            Vector2 hi = cam.WorldToScreenPoint(target.bounds.max);
            float s = HudTheme.Scale, pad = 8 * s;
            Rect rect = Rect.MinMaxRect(lo.x - pad, Screen.height - hi.y - pad, hi.x + pad, Screen.height - lo.y + pad);
            float length = Mathf.Min(rect.width, rect.height) * 0.22f;
            GUI.color = HudTheme.Ink;
            HudTheme.Corners(new Rect(rect.x, rect.y + 3 * s, rect.width, rect.height), 6 * s, length);
            GUI.color = HudTheme.ShipYellow;
            HudTheme.Corners(rect, 6 * s, length);
        }

        static void DrawEyesClosed(HudTheme theme, GameController game, GazeInput gaze)
        {
            if (!game.Mode.ShowsEyesClosedTimer || !gaze.Blink.EyesClosed) return;
            float t = gaze.Blink.ClosedSeconds;
            bool inWindow = t >= game.settings.blinkMinSeconds && t <= game.settings.blinkMaxSeconds;
            Rect pill = HudTheme.R(HudTheme.Width * 0.5f - 170, 96, 340, 56);
            theme.Plate(pill, inWindow ? HudTheme.Good : HudTheme.Warn, 5f);
            HudTheme.Write(pill, "Eyes closed " + Format.Number(t, "0.00") + " s", theme.Callout, HudTheme.Ink);
        }
    }
}
