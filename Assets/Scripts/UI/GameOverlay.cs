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
                GUI.color = new Color(HudTheme.Accent.r, HudTheme.Accent.g, HudTheme.Accent.b, 0.45f * fade);
                HudTheme.Line(HudTheme.ToGui(trail[i - 1].Point), HudTheme.ToGui(trail[i].Point), 2 * s);
            }
            if (!gaze.HasValidGaze) return;
            Vector2 point = HudTheme.ToGui(gaze.ScreenPoint);
            theme.Circle(new Rect(point.x - 4 * s, point.y - 4 * s, 8 * s, 8 * s), Color.white);
            if (game.State != GameState.Playing) return;

            GUI.color = game.Targeting.AimTarget != null ? HudTheme.Good : HudTheme.Accent;
            string hint = game.Mode.LockHint;
            if (hint == null) { HudTheme.Ring(point, 17 * s, game.ModeProgress, 3 * s); return; }
            if (game.Targeting.AimTarget == null) return;
            HudTheme.Ring(point, 17 * s, 1f, 3 * s);
            Rect pill = new Rect(point.x + 26 * s, point.y - 12 * s, (24 + hint.Length * 8) * s, 24 * s);
            theme.Box(pill, HudTheme.Panel);
            HudTheme.Text(pill, hint, theme.Chip, HudTheme.Good);
        }

        public void DrawShotFeedback(HudTheme theme, GameController game, float now)
        {
            if (game.State == GameState.Menu) return;
            ShotFeedback feedback = game.LastFeedback;
            if (now - feedback.Time < 1.2f && feedback.Text.Length > 0)
            {
                Color color = feedback.Fired ? HudTheme.Good : HudTheme.Warn;
                Rect pill = HudTheme.R(HudTheme.Width * 0.5f - 230, HudTheme.Height - 84, 460, 36);
                theme.Box(pill, HudTheme.Panel);
                theme.Outline(pill, color);
                HudTheme.Text(pill, feedback.Text, theme.Segment, color);
            }
            if (now - game.Cannon.LastShotTime < 0.2f)
                HudTheme.Text(HudTheme.R(HudTheme.Width * 0.5f - 100, 24, 200, 44), "HIT", theme.Huge, HudTheme.Good);
        }

        static void DrawTargetHighlight(GameController game)
        {
            Collider2D target = game.Targeting.AimTarget;
            Camera cam = Camera.main;
            if (target == null || cam == null) return;
            Vector2 lo = cam.WorldToScreenPoint(target.bounds.min);
            Vector2 hi = cam.WorldToScreenPoint(target.bounds.max);
            float pad = 6 * HudTheme.Scale;
            Rect rect = Rect.MinMaxRect(lo.x - pad, Screen.height - hi.y - pad, hi.x + pad, Screen.height - lo.y + pad);
            GUI.color = HudTheme.Good;
            HudTheme.Corners(rect, 2.5f * HudTheme.Scale, Mathf.Min(rect.width, rect.height) * 0.25f);
        }

        static void DrawEyesClosed(HudTheme theme, GameController game, GazeInput gaze)
        {
            if (!game.Mode.ShowsEyesClosedTimer || !gaze.Blink.EyesClosed) return;
            float t = gaze.Blink.ClosedSeconds;
            bool inWindow = t >= game.settings.blinkMinSeconds && t <= game.settings.blinkMaxSeconds;
            Color color = inWindow ? HudTheme.Good : HudTheme.Warn;
            Rect pill = HudTheme.R(HudTheme.Width * 0.5f - 160, 80, 320, 52);
            theme.Box(pill, HudTheme.Panel);
            theme.Outline(pill, color);
            HudTheme.Text(pill, "Eyes closed " + Format.Number(t, "0.00") + " s", theme.Center, color);
        }
    }
}
