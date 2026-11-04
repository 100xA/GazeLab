// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Top-left status card (mode, score, hull, tracking state), input switch and team footer.</summary>
    public sealed class StatusCard
    {
        const string TeamLine = "Team: Jan Biernacki · Kürsat · Paul · Malte   |   Matriculation numbers: TO BE PROVIDED";

        static readonly Rect Card = new Rect(16, 16, 384, 156);   // design units
        static readonly Rect Controls = new Rect(16, 186, 384, 34);

        /// <summary>Screen area covered by card and controls (GUI pixels); gaze there never aims.</summary>
        public static Rect Area { get { return HudTheme.R(Card.x, Card.y, Card.width, Controls.yMax + 5 - Card.y); } }

        public void Draw(HudTheme theme, GameController game, GazeInput gaze)
        {
            DrawCard(theme, game, gaze);
            DrawControls(theme, gaze);
            HudTheme.Write(HudTheme.R(16, HudTheme.Height - 30, 760, 22), TeamLine, theme.Small,
                new Color(HudTheme.Muted.r, HudTheme.Muted.g, HudTheme.Muted.b, 0.9f));
        }

        void DrawCard(HudTheme theme, GameController game, GazeInput gaze)
        {
            theme.Plate(HudTheme.R(Card.x, Card.y, Card.width, Card.height), HudTheme.Panel);
            float x = Card.x + 18, y = Card.y;
            bool inMenu = game.State == GameState.Menu;

            HudTheme.Shadowed(HudTheme.R(x, y + 12, 160, 24), "GAZE LAB", theme.LabelStrong, HudTheme.ShipYellow, HudTheme.Ink, 2f);
            string mode = inMenu ? "CHOOSE MODE" : game.Mode.Name.ToUpperInvariant();
            float chipWidth = Mathf.Min(230f, 28f + mode.Length * 8.4f);
            Rect chip = HudTheme.R(Card.xMax - 14 - chipWidth, y + 12, chipWidth, 26);
            theme.Plate(chip, HudTheme.ShipRed, 3f);
            HudTheme.Write(chip, mode, theme.Chip, HudTheme.Foreground);

            HudTheme.Write(HudTheme.R(x, y + 46, 150, 16), "SCORE", theme.Label, HudTheme.Muted);
            HudTheme.Shadowed(HudTheme.R(x, y + 58, 170, 48), game.Score.ToString(), theme.Value, HudTheme.ShipYellow, HudTheme.Ink);
            DrawHull(theme, game, x + 178, y);

            Color tracking = gaze.IsSimulation ? HudTheme.Warn : gaze.HasValidGaze ? HudTheme.Good : HudTheme.Bad;
            theme.Circle(HudTheme.R(x, y + 112, 12, 12), tracking);
            HudTheme.Write(HudTheme.R(x + 20, y + 107, Card.width - 54, 22), gaze.StatusText, theme.Body, HudTheme.Foreground);
            HudTheme.Write(HudTheme.R(x, y + 131, Card.width - 36, 20), Hint(game, gaze), theme.BodyMuted, HudTheme.Muted);
        }

        static void DrawHull(HudTheme theme, GameController game, float x, float y)
        {
            int segments = Mathf.Max(1, game.settings.maxShipHits);
            int left = Mathf.Clamp(segments - game.ShipHits, 0, segments);
            HudTheme.Write(HudTheme.R(x, y + 46, 180, 16), "HULL  " + game.HullPercent + " %", theme.Label, HudTheme.Muted);
            Color color = left * 4 >= segments * 3 ? HudTheme.Good : left * 2 >= segments ? HudTheme.Warn : HudTheme.Bad;
            float width = 188f / segments;
            for (int i = 0; i < segments; i++)
            {
                Rect segment = HudTheme.R(x + i * width, y + 68, width - 7, 18);
                if (i < left) theme.Plate(segment, color, 3f);
                else theme.Box(segment, HudTheme.Track);
            }
        }

        static string Hint(GameController game, GazeInput gaze)
        {
            if (game.State == GameState.Menu)
                return "Look at a mode for 1 s.  Rocks " + Signed(game.settings.rockPoints) +
                       "  ·  Satellites " + Signed(game.settings.satellitePoints);
            string hint = game.Mode.Instruction(game.settings);
            if (game.Mode.UsesHeadPose && game.State == GameState.Playing)
                hint += gaze.Head.IsAvailable ? "  ·  head " + Format.Number(gaze.HeadPitchDeviation, "+0;-0") + "°" : "  ·  " + gaze.Head.Status;
            return hint;
        }

        static string Signed(int points) { return points > 0 ? "+" + points : points < 0 ? "−" + (-points) : "0"; }

        static void DrawControls(HudTheme theme, GazeInput gaze)
        {
            float x = Controls.x, y = Controls.y, h = Controls.height;
            theme.Plate(HudTheme.R(x, y, 236, h), HudTheme.Panel, 4f);
            if (Segment(theme, HudTheme.R(x + 4, y + 4, 112, h - 8), "Eye tracker", !gaze.IsSimulation)) gaze.UseTobii();
            if (Segment(theme, HudTheme.R(x + 120, y + 4, 112, h - 8), "Mouse", gaze.IsSimulation)) gaze.UseMouse();

            if (gaze.IsSimulation || gaze.DisplayScale <= 1.01f) return;
            bool on = gaze.settings.compensateDisplayScaling;
            Rect pill = HudTheme.R(x + 244, y, 140, h);
            theme.Plate(pill, on ? HudTheme.CockpitTeal : HudTheme.Warn, 4f);
            HudTheme.Write(pill, "Scale " + Mathf.RoundToInt(gaze.DisplayScale * 100) + "%: " + (on ? "fixed" : "off"), theme.Chip, HudTheme.Ink);
            if (HudTheme.Clicked(pill)) gaze.settings.compensateDisplayScaling = !on;
        }

        static bool Segment(HudTheme theme, Rect rect, string label, bool selected)
        {
            if (selected) theme.Plate(rect, HudTheme.ShipYellow, 3f);
            HudTheme.Write(rect, label, theme.Button, selected ? HudTheme.Ink : HudTheme.Muted);
            return HudTheme.Clicked(rect) && !selected;
        }
    }
}
