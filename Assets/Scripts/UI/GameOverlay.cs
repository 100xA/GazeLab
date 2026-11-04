// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// In-game feedback: gaze marker with a short trail, progress ring or lock hint, target corner
    /// brackets (green = clear line of fire), eyes-closed timer, shot feedback and HIT.
    /// </summary>
    public sealed class GameOverlay
    {
        const float TrailSeconds = 0.65f;
        const int MaxTrailPoints = 100;

        struct TracePoint { public Vector2 Point; public float Time; }
        readonly List<TracePoint> trail = new List<TracePoint>();

        public void ClearTrail() { trail.Clear(); }

        public void UpdateTrail(GazeInput gaze, float now)
        {
            trail.RemoveAll(p => now - p.Time > TrailSeconds);
            if (!gaze.HasValidGaze) { trail.Clear(); return; }
            trail.Add(new TracePoint { Point = gaze.ScreenPoint, Time = now });
            if (trail.Count > MaxTrailPoints) trail.RemoveAt(0);
        }

        /// <summary>Elements drawn below the HUD: target highlight and the eyes-closed timer.</summary>
        public void DrawBehindHud(HudTheme theme, GameController game, GazeInput gaze)
        {
            if (game.State != GameState.Playing) return;
            DrawTargetHighlight(game);
            DrawEyesClosed(theme, game, gaze);
        }

        public void DrawGazeMarker(HudTheme theme, GameController game, GazeInput gaze, float now)
        {
            float s = HudTheme.Scale;
            for (int i = 1; i < trail.Count; i++)
            {
                float fade = Mathf.Clamp01(1 - (now - trail[i].Time) / TrailSeconds);
                Color c = HudTheme.ShipYellow;
                GUI.color = new Color(c.r, c.g, c.b, 0.5f * fade);
                HudTheme.Line(HudTheme.ToGui(trail[i - 1].Point), HudTheme.ToGui(trail[i].Point), 3 * s);
            }
            if (!gaze.HasValidGaze) return;
            Vector2 point = HudTheme.ToGui(gaze.ScreenPoint);
            theme.Circle(new Rect(point.x - 7 * s, point.y - 7 * s, 14 * s, 14 * s), HudTheme.Ink);
            theme.Circle(new Rect(point.x - 4.5f * s, point.y - 4.5f * s, 9 * s, 9 * s), Color.white);
            if (game.State != GameState.Playing) return;

            bool locked = game.Targeting.AimTarget != null;
            string hint = game.Mode.LockHint;
            if (hint == null)
            {
                // Dwell: dark track with the yellow progress on top.
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                HudTheme.Ring(point, 20 * s, 1f, 6 * s);
                GUI.color = HudTheme.ShipYellow;
                HudTheme.Ring(point, 20 * s, game.ModeProgress, 6 * s);
                return;
            }
            if (!locked) return;
            GUI.color = HudTheme.ShipYellow;
            HudTheme.Ring(point, 20 * s, 1f, 5 * s);
            Rect pill = new Rect(point.x + 30 * s, point.y - 14 * s, (30 + hint.Length * 9) * s, 28 * s);
            theme.Plate(pill, HudTheme.ShipYellow, 3f);
            HudTheme.Write(pill, hint, theme.Chip, HudTheme.Ink);
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
