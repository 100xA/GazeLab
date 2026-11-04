// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Top-left status card (mode, score, hull, tracking state), input switch and team footer.</summary>
    public sealed class StatusCard
    {
        const string TeamLine = "Team: Jan Biernacki · Kürsat · Paul · Malte   |   Matriculation numbers: TO BE PROVIDED";

        static readonly Rect Card = new Rect(16, 16, 384, 150);   // design units
        static readonly Rect Controls = new Rect(16, 174, 384, 28);

        /// <summary>Screen area covered by card and controls (GUI pixels); gaze there never aims.</summary>
        public static Rect Area { get { return HudTheme.R(Card.x, Card.y, Card.width, Controls.yMax - Card.y); } }

        public void Draw(HudTheme theme, GameController game, GazeInput gaze)
        {
            DrawCard(theme, game, gaze);
            DrawControls(theme, gaze);
            HudTheme.Text(HudTheme.R(16, HudTheme.Height - 28, 700, 20), TeamLine, theme.Small,
                new Color(HudTheme.Muted.r, HudTheme.Muted.g, HudTheme.Muted.b, 0.85f));
        }

        void DrawCard(HudTheme theme, GameController game, GazeInput gaze)
        {
            Rect card = HudTheme.R(Card.x, Card.y, Card.width, Card.height);
            theme.Box(card, HudTheme.Panel);
            theme.Outline(card, HudTheme.Edge);
            float x = Card.x + 18, y = Card.y;
            bool inMenu = game.State == GameState.Menu;

            HudTheme.Text(HudTheme.R(x, y + 14, 120, 16), "GAZE LAB", theme.Caption, HudTheme.Accent);
            string mode = inMenu ? "CHOOSE MODE" : game.Mode.Name.ToUpperInvariant();
            float chipWidth = Mathf.Min(220f, 26f + mode.Length * 7.6f);
            Rect chip = HudTheme.R(Card.xMax - 16 - chipWidth, y + 10, chipWidth, 22);
            theme.Box(chip, new Color(HudTheme.Accent.r, HudTheme.Accent.g, HudTheme.Accent.b, 0.16f));
            HudTheme.Text(chip, mode, theme.Chip, HudTheme.Accent);

            HudTheme.Text(HudTheme.R(x, y + 40, 150, 14), "SCORE", theme.Caption, HudTheme.Muted);
            HudTheme.Text(HudTheme.R(x, y + 52, 170, 42), game.Score.ToString(), theme.Value, Color.white);
            DrawHull(theme, game, x + 186, y);

            Color tracking = gaze.IsSimulation ? HudTheme.Warn : gaze.HasValidGaze ? HudTheme.Good : HudTheme.Bad;
            theme.Circle(HudTheme.R(x, y + 103, 10, 10), tracking);
            HudTheme.Text(HudTheme.R(x + 18, y + 98, Card.width - 52, 20), gaze.StatusText, theme.Body, Color.white);
            HudTheme.Text(HudTheme.R(x, y + 122, Card.width - 36, 20), Hint(game, gaze), theme.BodyMuted, HudTheme.Muted);
        }

        static void DrawHull(HudTheme theme, GameController game, float x, float y)
        {
            int segments = Mathf.Max(1, game.settings.maxShipHits);
            int left = Mathf.Clamp(segments - game.ShipHits, 0, segments);
            HudTheme.Text(HudTheme.R(x, y + 40, 150, 14), "HULL  " + game.HullPercent + " %", theme.Caption, HudTheme.Muted);
            Color color = left * 4 >= segments * 3 ? HudTheme.Good : left * 2 >= segments ? HudTheme.Warn : HudTheme.Bad;
            float width = 176f / segments;
            for (int i = 0; i < segments; i++)
                theme.Box(HudTheme.R(x + i * width, y + 62, width - 5, 12), i < left ? color : HudTheme.Track);
        }

        static string Hint(GameController game, GazeInput gaze)
        {
            if (game.State == GameState.Menu)
                return "Look at a mode for 1 s to start.  Rocks " + Signed(game.settings.rockPoints) +
                       "  ·  Satellites " + Signed(game.settings.satellitePoints);
            string hint = game.Mode.Instruction(game.settings);
            if (game.Mode.UsesHeadPose && game.State == GameState.Playing)
                hint += gaze.Head.IsAvailable ? "  ·  head pitch " + Format.Number(gaze.HeadPitchDeviation, "+0;-0") + "°" : "  ·  " + gaze.Head.Status;
            return hint;
        }

        static string Signed(int points) { return points > 0 ? "+" + points : points < 0 ? "−" + (-points) : "0"; }

        static void DrawControls(HudTheme theme, GazeInput gaze)
        {
            float x = Controls.x, y = Controls.y, h = Controls.height;
            theme.Box(HudTheme.R(x, y, 230, h), HudTheme.Panel);
            if (Segment(theme, HudTheme.R(x + 3, y + 3, 112, h - 6), "Eye tracker", !gaze.IsSimulation)) gaze.UseTobii();
            if (Segment(theme, HudTheme.R(x + 115, y + 3, 112, h - 6), "Mouse", gaze.IsSimulation)) gaze.UseMouse();

            if (gaze.IsSimulation || gaze.DisplayScale <= 1.01f) return;
            bool on = gaze.settings.compensateDisplayScaling;
            Color color = on ? HudTheme.Good : HudTheme.Warn;
            Rect pill = HudTheme.R(x + 238, y, 146, h);
            theme.Box(pill, HudTheme.Panel);
            theme.Outline(pill, new Color(color.r, color.g, color.b, 0.55f));
            HudTheme.Text(pill, "Scaling " + Mathf.RoundToInt(gaze.DisplayScale * 100) + " %: fix " + (on ? "on" : "off"), theme.Chip, color);
            if (HudTheme.Clicked(pill)) gaze.settings.compensateDisplayScaling = !on;
        }

        static bool Segment(HudTheme theme, Rect rect, string label, bool selected)
        {
            if (selected) theme.Box(rect, HudTheme.Accent);
            HudTheme.Text(rect, label, theme.Segment, selected ? HudTheme.OnAccent : HudTheme.Muted);
            return HudTheme.Clicked(rect) && !selected;
        }
    }
}
