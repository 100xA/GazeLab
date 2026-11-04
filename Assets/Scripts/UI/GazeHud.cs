// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>Draws the HUD (IMGUI) and runs the gaze-only menu buttons. Set up by the GameController.</summary>
    [DisallowMultipleComponent]
    public class GazeHud : MonoBehaviour
    {
        readonly HudTheme theme = new HudTheme();
        readonly StatusCard statusCard = new StatusCard();
        readonly GameOverlay overlay = new GameOverlay();
        GameController game;
        GazeInput gaze;
        MenuView menu;

        public void Init(GameController owner, GazeInput input)
        {
            game = owner;
            gaze = input;
            menu = new MenuView(owner);
        }

        /// <summary>True if a screen point lies on a HUD control; gaze there neither aims nor fires.</summary>
        public bool BlocksGaze(Vector2 screenPoint)
        {
            Vector2 guiPoint = HudTheme.ToGui(screenPoint);
            if (StatusCard.Area.Contains(guiPoint)) return true;
            if (menu == null || !menu.IsVisible) return false;
            menu.Refresh();
            return menu.Buttons.Contains(guiPoint);
        }

        void LateUpdate()
        {
            if (game == null) return;
            float now = Time.unscaledTime;
            menu.Refresh();
            if (menu.IsVisible)
                menu.Buttons.Update(gaze.HasValidGaze, HudTheme.ToGui(gaze.ScreenPoint), now, Time.unscaledDeltaTime);
        }

        void OnGUI()
        {
            if (game == null) return;
            theme.Prepare();
            Color previous = GUI.color;
            float now = Time.unscaledTime;
            menu.Refresh();

            overlay.DrawBehindHud(theme, game, gaze);
            menu.Draw(theme);
            statusCard.Draw(theme, game, gaze);
            overlay.DrawGazeMarker(theme, game, gaze);
            overlay.DrawShotFeedback(theme, game, now);
            GUI.color = previous;
        }

        void OnDestroy()
        {
            theme.Release();
        }
    }
}
