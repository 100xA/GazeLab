// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

namespace GazeLab
{
    /// <summary>Gaze-operated overlays: mode selection at start, Restart / Change mode after game over.</summary>
    public sealed class MenuView
    {
        // One colour per mode tile, taken from the 3D models.
        static readonly Color[] TileColors = { HudTheme.CockpitTeal, HudTheme.ShipYellow, HudTheme.RockOrange, HudTheme.SatelliteBlue };

        readonly GameController game;
        readonly GazeButtons buttons = new GazeButtons();
        // Buttons are rebuilt only when what they show or the screen size changes.
        GameState builtState;
        ShotMode builtMode;
        int builtWidth = -1, builtHeight = -1;

        public MenuView(GameController game) { this.game = game; }

        public GazeButtons Buttons { get { return buttons; } }
        public bool IsVisible { get { return game.State != GameState.Playing; } }

        public void Refresh()
        {
            if (builtState == game.State && builtMode == game.Mode && builtWidth == Screen.width && builtHeight == Screen.height) return;
            builtState = game.State;
            builtMode = game.Mode;
            builtWidth = Screen.width;
            builtHeight = Screen.height;
            buttons.Set(Build());
        }

        IEnumerable<GazeButtons.Button> Build()
        {
            float vw = HudTheme.Width, vh = HudTheme.Height;
            if (game.State == GameState.Menu)
            {
                const float Gap = 24f, Height = 150f;
                float width = Mathf.Min(420f, (vw - 3 * Gap) / 2f);
                float top = Mathf.Max(StatusCard.Area.yMax / HudTheme.Scale + 110f, vh * 0.5f - Height + 18f);
                float left = vw * 0.5f - width - Gap * 0.5f;
                for (int i = 0; i < ShotModes.All.Length; i++)
                {
                    ShotMode mode = ShotModes.All[i];
                    yield return new GazeButtons.Button
                    {
                        Rect = HudTheme.R(left + (i % 2) * (width + Gap), top + (i / 2) * (Height + Gap), width, Height),
                        Number = (i + 1).ToString(),
                        Accent = TileColors[i % TileColors.Length],
                        Title = mode.Name,
                        Body = mode.Description(game.settings),
                        Action = () => game.StartMode(mode)
                    };
                }
            }
            else if (game.State == GameState.GameOver)
            {
                const float Width = 280f, Height = 112f, Gap = 24f;
                float y = vh * 0.5f + 2f;
                yield return new GazeButtons.Button
                {
                    Rect = HudTheme.R(vw * 0.5f - Width - Gap * 0.5f, y, Width, Height),
                    Title = "Restart", Body = game.Mode.Name, Accent = HudTheme.ShipYellow, Action = game.RestartRound
                };
                yield return new GazeButtons.Button
                {
                    Rect = HudTheme.R(vw * 0.5f + Gap * 0.5f, y, Width, Height),
                    Title = "Change mode", Body = "Back to the mode menu", Accent = HudTheme.CockpitTeal, Action = game.ShowMenu
                };
            }
        }

        public void Draw(HudTheme theme)
        {
            if (game.State == GameState.Menu) DrawMenu(theme);
            else if (game.State == GameState.GameOver) DrawGameOver(theme);
        }

        void DrawMenu(HudTheme theme)
        {
            HudTheme.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.45f));
            if (buttons.Items.Count == 0) return;
            float top = buttons.Items[0].Rect.y / HudTheme.Scale;
            HudTheme.Shadowed(HudTheme.R(0, top - 112, HudTheme.Width, 60), "Choose your shooting mode", theme.Title, HudTheme.ShipYellow, HudTheme.ShipRed, 5f);
            HudTheme.Write(HudTheme.R(0, top - 46, HudTheme.Width, 26), "Look at a mode for one second", theme.Subtitle, HudTheme.Foreground);
            DrawTiles(theme);
        }

        void DrawGameOver(HudTheme theme)
        {
            float vw = HudTheme.Width, vh = HudTheme.Height;
            theme.Plate(HudTheme.R(vw * 0.5f - 330, vh * 0.5f - 120, 660, 270), HudTheme.Panel, 7f);
            Rect banner = HudTheme.R(vw * 0.5f - 230, vh * 0.5f - 150, 460, 74);
            theme.Plate(banner, HudTheme.ShipRed, 6f);
            HudTheme.Shadowed(banner, "GAME OVER", theme.Banner, HudTheme.ShipYellow, HudTheme.Shade(HudTheme.ShipRed, 0.45f), 4f);
            HudTheme.Shadowed(HudTheme.R(vw * 0.5f - 330, vh * 0.5f - 62, 660, 50), "Score " + game.Score, theme.Callout, HudTheme.Foreground, HudTheme.Ink, 2f);
            DrawTiles(theme);
        }

        void DrawTiles(HudTheme theme)
        {
            float s = HudTheme.Scale;
            for (int i = 0; i < buttons.Items.Count; i++)
            {
                GazeButtons.Button button = buttons.Items[i];
                bool hovered = i == buttons.Hovered;
                Rect r = button.Rect;
                theme.Plate(r, hovered ? HudTheme.PanelLight : HudTheme.Panel, 6f);
                if (hovered) theme.Outline(r, button.Accent);

                float textLeft = 22 * s;
                if (!string.IsNullOrEmpty(button.Number))
                {
                    Rect badge = new Rect(r.x + 18 * s, r.y + 18 * s, 44 * s, 44 * s);
                    theme.Circle(new Rect(badge.x, badge.y + 4 * s, badge.width, badge.height), HudTheme.Shade(button.Accent));
                    theme.Circle(badge, button.Accent);
                    HudTheme.Write(badge, button.Number, theme.Badge, HudTheme.Ink);
                    textLeft = 76 * s;
                }
                HudTheme.Write(new Rect(r.x + textLeft, r.y + 20 * s, r.width - textLeft - 18 * s, 36 * s), button.Title, theme.TileTitle,
                    hovered ? button.Accent : HudTheme.Foreground);
                HudTheme.Write(new Rect(r.x + textLeft, r.y + 58 * s, r.width - textLeft - 18 * s, r.height - 72 * s), button.Body, theme.TileBody, HudTheme.Muted);

                if (!hovered) continue;
                Rect bar = new Rect(r.x + 16 * s, r.yMax - 22 * s, r.width - 32 * s, 10 * s);
                theme.Box(bar, HudTheme.Track);
                float progress = Mathf.Clamp01(buttons.Progress);
                if (progress > 0.02f) theme.Box(new Rect(bar.x, bar.y, Mathf.Max(bar.height, bar.width * progress), bar.height), button.Accent);
            }
        }
    }
}
