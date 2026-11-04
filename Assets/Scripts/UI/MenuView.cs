// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

namespace GazeLab
{
    /// <summary>Gaze-operated overlays: mode selection at start, Restart / Change mode after game over.</summary>
    public sealed class MenuView
    {
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
                        Title = mode.Name,
                        Body = mode.Description(game.settings),
                        Action = () => game.StartMode(mode)
                    };
                }
            }
            else if (game.State == GameState.GameOver)
            {
                const float Width = 280f, Height = 104f, Gap = 24f;
                float y = vh * 0.5f + 10f;
                yield return new GazeButtons.Button
                {
                    Rect = HudTheme.R(vw * 0.5f - Width - Gap * 0.5f, y, Width, Height),
                    Title = "Restart", Body = game.Mode.Name, Action = game.RestartRound
                };
                yield return new GazeButtons.Button
                {
                    Rect = HudTheme.R(vw * 0.5f + Gap * 0.5f, y, Width, Height),
                    Title = "Change mode", Body = "Back to the mode menu", Action = game.ShowMenu
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
            HudTheme.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.5f));
            if (buttons.Items.Count == 0) return;
            float top = buttons.Items[0].Rect.y / HudTheme.Scale;
            HudTheme.Text(HudTheme.R(0, top - 92, HudTheme.Width, 44), "Choose a shooting mode", theme.Huge, Color.white);
            HudTheme.Text(HudTheme.R(0, top - 48, HudTheme.Width, 24), "Look at a mode for one second", theme.Center, HudTheme.Muted);
            DrawTiles(theme);
        }

        void DrawGameOver(HudTheme theme)
        {
            float vw = HudTheme.Width, vh = HudTheme.Height;
            Rect card = HudTheme.R(vw * 0.5f - 330, vh * 0.5f - 120, 660, 268);
            theme.Box(card, HudTheme.PanelSolid);
            theme.Outline(card, HudTheme.Edge);
            HudTheme.Text(HudTheme.R(vw * 0.5f - 330, vh * 0.5f - 104, 660, 40), "GAME OVER", theme.Huge, Color.white);
            HudTheme.Text(HudTheme.R(vw * 0.5f - 330, vh * 0.5f - 58, 660, 30), "Score " + game.Score, theme.Center, HudTheme.Accent);
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
                theme.Box(r, hovered ? new Color(0.07f, 0.2f, 0.3f, 0.97f) : HudTheme.PanelSolid);
                theme.Outline(r, hovered ? HudTheme.Good : HudTheme.Edge);

                float textLeft = 20 * s;
                if (!string.IsNullOrEmpty(button.Number))
                {
                    Rect badge = new Rect(r.x + 18 * s, r.y + 18 * s, 36 * s, 36 * s);
                    theme.Circle(badge, hovered ? HudTheme.Good : new Color(HudTheme.Accent.r, HudTheme.Accent.g, HudTheme.Accent.b, 0.2f));
                    HudTheme.Text(badge, button.Number, theme.Badge, hovered ? HudTheme.OnAccent : HudTheme.Accent);
                    textLeft = 66 * s;
                }
                HudTheme.Text(new Rect(r.x + textLeft, r.y + 20 * s, r.width - textLeft - 18 * s, 32 * s), button.Title, theme.TileTitle, Color.white);
                HudTheme.Text(new Rect(r.x + textLeft, r.y + 56 * s, r.width - textLeft - 18 * s, r.height - 70 * s), button.Body, theme.TileBody, HudTheme.Muted);

                if (!hovered) continue;
                Rect bar = new Rect(r.x + 14 * s, r.yMax - 14 * s, r.width - 28 * s, 5 * s);
                HudTheme.Fill(bar, HudTheme.Track);
                HudTheme.Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(buttons.Progress), bar.height), HudTheme.Good);
            }
        }
    }
}
