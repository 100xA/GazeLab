// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Non-physics overlay: gaze trail, selection feedback, status panel and the
/// gaze-only menus (mode selection at start, restart / change mode after game over).
/// </summary>
public class GazeHud : MonoBehaviour
{
    struct TracePoint { public Vector2 point; public float time; }
    struct GazeButton { public Rect rect; public string title, body; public System.Action action; }

    const float MenuDwellSeconds = 1f;
    const float ButtonTolerancePixels = 40f;   // gaze slightly outside a button still counts
    const float ButtonLossToleranceSeconds = 0.3f; // blinks / jitter keep the progress frozen
    static readonly GameController.SelectionMode[] MenuModes =
    {
        GameController.SelectionMode.Dwell, GameController.SelectionMode.Blink,
        GameController.SelectionMode.Nod, GameController.SelectionMode.Button
    };

    readonly List<TracePoint> trace = new List<TracePoint>();
    readonly List<GazeButton> buttons = new List<GazeButton>();
    GameController game;
    GazeInput input;
    GazeCalibration calibration;
    Texture2D dot;
    GUIStyle titleStyle, labelStyle, bigStyle, buttonTitleStyle, buttonBodyStyle, centerStyle;
    int hoveredButton = -1;
    float buttonDwell, lastButtonHit;
    bool previousSimulation;

    static Rect Panel { get { return new Rect(12, 12, Mathf.Min(640, Screen.width - 24), 166); } }

    public static bool ControlsContain(Vector2 screenPoint)
    {
        Vector2 guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
        if (Panel.Contains(guiPoint)) return true;
        GameController controller = FindObjectOfType<GameController>();
        if (controller == null || !(controller.InMenu || controller.GameOver)) return false;
        foreach (Rect rect in OverlayRects(controller)) if (rect.Contains(guiPoint)) return true;
        return false;
    }

    /// <summary>GUI-space rectangles of the gaze buttons for the current overlay.</summary>
    static List<Rect> OverlayRects(GameController controller)
    {
        var rects = new List<Rect>();
        if (controller.InMenu)
        {
            float gap = 24f;
            float top = Mathf.Max(Panel.yMax + 70f, Screen.height * 0.5f - 150f);
            float width = Mathf.Min(440f, (Screen.width - 3 * gap) / 2f);
            float height = Mathf.Clamp((Screen.height - top - 2 * gap) / 2f, 80f, 170f);
            float left = Screen.width * 0.5f - width - gap * 0.5f;
            for (int i = 0; i < 4; i++)
                rects.Add(new Rect(left + (i % 2) * (width + gap), top + (i / 2) * (height + gap), width, height));
        }
        else if (controller.GameOver)
        {
            float width = 300f, height = 96f, gap = 30f;
            float y = Screen.height * 0.5f + 20f;
            rects.Add(new Rect(Screen.width * 0.5f - width - gap * 0.5f, y, width, height));
            rects.Add(new Rect(Screen.width * 0.5f + gap * 0.5f, y, width, height));
        }
        return rects;
    }

    void Awake()
    {
        game = GetComponent<GameController>();
        input = GetComponent<GazeInput>();
        calibration = GetComponent<GazeCalibration>();
        dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            dot.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) <= 15 ? Color.white : Color.clear);
        dot.Apply();
    }

    void RebuildButtons()
    {
        buttons.Clear();
        List<Rect> rects = OverlayRects(game);
        if (game.InMenu)
        {
            for (int i = 0; i < MenuModes.Length; i++)
            {
                GameController.SelectionMode mode = MenuModes[i];
                buttons.Add(new GazeButton
                {
                    rect = rects[i],
                    title = (i + 1) + "   " + GameController.ModeName(mode),
                    body = ModeDescription(mode),
                    action = () => game.StartMode(mode)
                });
            }
        }
        else if (game.GameOver)
        {
            buttons.Add(new GazeButton { rect = rects[0], title = "Restart", body = GameController.ModeName(game.Mode), action = game.RestartGame });
            buttons.Add(new GazeButton { rect = rects[1], title = "Change mode", body = "Back to the mode menu", action = game.ShowMenu });
        }
    }

    void LateUpdate()
    {
        if (input == null || game == null) return;
        if (input.IsSimulation != previousSimulation) trace.Clear();
        previousSimulation = input.IsSimulation;
        trace.RemoveAll(p => Time.unscaledTime - p.time > 0.65f);
        RebuildButtons();
        if (calibration != null && calibration.IsRunning)
        {
            trace.Clear();
            hoveredButton = -1;
            buttonDwell = 0;
            return;
        }
        if (hoveredButton >= buttons.Count) { hoveredButton = -1; buttonDwell = 0; }
        if (!input.HasValidGaze)
        {
            trace.Clear();
            KeepOrDropHover();
            return;
        }
        trace.Add(new TracePoint { point = input.ScreenPoint, time = Time.unscaledTime });
        if (trace.Count > 100) trace.RemoveAt(0);

        // Gaze-only buttons: look at one for MenuDwellSeconds to activate it.
        int hovered = ButtonUnderGaze(ToGui(input.ScreenPoint));
        if (hovered < 0) { KeepOrDropHover(); return; }
        lastButtonHit = Time.unscaledTime;
        if (hovered != hoveredButton) { hoveredButton = hovered; buttonDwell = 0; }
        buttonDwell += Time.unscaledDeltaTime;
        if (buttonDwell >= MenuDwellSeconds)
        {
            System.Action action = buttons[hovered].action;
            hoveredButton = -1;
            buttonDwell = 0;
            trace.Clear();
            action();
        }
    }

    /// <summary>Button containing the gaze point, or the nearest one within the tolerance.</summary>
    int ButtonUnderGaze(Vector2 position)
    {
        int best = -1;
        float bestDistance = ButtonTolerancePixels;
        for (int i = 0; i < buttons.Count; i++)
        {
            Rect r = buttons[i].rect;
            float dx = Mathf.Max(r.xMin - position.x, 0f, position.x - r.xMax);
            float dy = Mathf.Max(r.yMin - position.y, 0f, position.y - r.yMax);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            if (distance <= bestDistance) { bestDistance = distance; best = i; }
        }
        return best;
    }

    /// <summary>Short gaze loss (blink, jitter) freezes the button progress instead of resetting it.</summary>
    void KeepOrDropHover()
    {
        if (hoveredButton >= 0 && Time.unscaledTime - lastButtonHit <= ButtonLossToleranceSeconds) return;
        hoveredButton = -1;
        buttonDwell = 0;
    }

    void OnGUI()
    {
        if (game == null || input == null) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            buttonTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            buttonBodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperCenter, wordWrap = true };
        }
        Color previous = GUI.color;
        DrawPanel();
        if (buttons.Count == 0) RebuildButtons();

        if (game.InMenu) DrawMenu();
        else
        {
            DrawTarget();
            DrawBlinkIndicator();
        }
        if (game.GameOver) DrawGameOver();
        DrawGazeMarker();
        if (Time.unscaledTime - game.LastConfirmTime < 1.2f && !game.InMenu)
        {
            GUI.color = game.LastConfirmText.EndsWith("fired") ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.72f, 0.2f);
            GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height - 70, 500, 40), game.LastConfirmText, centerStyle);
        }
        if (Time.unscaledTime - game.LastShotTime < 0.15f && !game.InMenu)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 35, 28, 100, 30), "HIT", titleStyle);
        }
        GUI.color = previous;
    }

    void DrawPanel()
    {
        Rect panel = Panel;
        GUI.color = new Color(0.03f, 0.06f, 0.13f, 0.94f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        string mode = game.InMenu ? "choose a mode" : GameController.ModeName(game.Mode);
        GUI.Label(new Rect(24, 17, panel.width - 20, 28),
            "GAZE LAB   Score " + game.points + "   Hull " + Mathf.Max(0, 100 - game.damage * 25) + "%   Mode: " + mode, titleStyle);
        GUI.color = input.IsSimulation ? new Color(1f, 0.72f, 0.2f) : Color.cyan;
        string status = input.StatusText;
        if (!input.IsSimulation) status += input.IsCorrected ? "  |  gaze correction on" : "  |  not calibrated in game";
        GUI.Label(new Rect(24, 47, panel.width - 20, 23), status, labelStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(24, 70, panel.width - 20, 22), game.InMenu
            ? "Look at a mode for 1 s to start. Rocks +20 / Satellites -100."
            : ModeInstruction(game) + " Rocks +20 / Satellites -100.", labelStyle);
        if (calibration != null && calibration.Message.Length > 0 && Time.unscaledTime - calibration.MessageTime < 10f)
        {
            GUI.color = calibration.Message.StartsWith("Calibration saved") ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.72f, 0.2f);
            GUI.Label(new Rect(24, 92, panel.width - 20, 22), calibration.Message, labelStyle);
            GUI.color = Color.white;
        }
        else if (!game.InMenu && game.Mode == GameController.SelectionMode.Nod && input.HeadStatus.Length > 0)
        {
            GUI.color = input.HeadPoseAvailable ? Color.cyan : new Color(1f, 0.72f, 0.2f);
            GUI.Label(new Rect(24, 92, panel.width - 20, 22), input.HeadStatus, labelStyle);
            GUI.color = Color.white;
        }
        float y = 118;
        if (GUI.Button(new Rect(24, y, 150, 22), "Tobii eye tracker")) input.ChooseSource(false);
        if (GUI.Button(new Rect(184, y, 150, 22), "Mouse simulation")) input.ChooseSource(true);
        if (!input.IsSimulation && calibration != null && !calibration.IsRunning)
        {
            if (GUI.Button(new Rect(344, y, 130, 22), "Calibrate gaze")) calibration.Begin();
            if (input.IsCorrected && GUI.Button(new Rect(484, y, 120, 22), "Reset correction")) input.ClearCorrection();
        }
        GUI.Label(new Rect(24, 144, panel.width - 20, 22), "Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED", labelStyle);
    }

    void DrawMenu()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, Panel.yMax + 8, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        float titleY = buttons.Count > 0 ? buttons[0].rect.y - 60 : Screen.height / 2f - 200;
        GUI.Label(new Rect(0, titleY, Screen.width, 44), "Choose a shooting mode - look at it for 1 second", bigStyle);
        for (int i = 0; i < buttons.Count; i++) DrawGazeButton(i);
    }

    void DrawGameOver()
    {
        GUI.color = new Color(0.03f, 0.06f, 0.13f, 0.96f);
        GUI.DrawTexture(new Rect(Screen.width / 2f - 360, Screen.height / 2f - 70, 720, 210), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width / 2f - 360, Screen.height / 2f - 60, 720, 60),
            "GAME OVER  |  Score " + game.points, bigStyle);
        for (int i = 0; i < buttons.Count; i++) DrawGazeButton(i);
    }

    void DrawGazeButton(int index)
    {
        GazeButton button = buttons[index];
        bool hovered = index == hoveredButton;
        GUI.color = hovered ? new Color(0.1f, 0.3f, 0.45f, 0.97f) : new Color(0.06f, 0.11f, 0.22f, 0.95f);
        GUI.DrawTexture(button.rect, Texture2D.whiteTexture);
        GUI.color = hovered ? new Color(0.3f, 1f, 0.55f) : new Color(0.3f, 0.9f, 1f, 0.8f);
        DrawFrame(button.rect, hovered ? 3 : 2);
        GUI.color = Color.white;
        GUI.Label(new Rect(button.rect.x + 10, button.rect.y + 12, button.rect.width - 20, 34), button.title, buttonTitleStyle);
        GUI.Label(new Rect(button.rect.x + 16, button.rect.y + 48, button.rect.width - 32, button.rect.height - 60), button.body, buttonBodyStyle);
        if (hovered)
        {
            GUI.color = new Color(0.3f, 1f, 0.55f);
            GUI.DrawTexture(new Rect(button.rect.x, button.rect.yMax - 8,
                button.rect.width * Mathf.Clamp01(buttonDwell / MenuDwellSeconds), 8), Texture2D.whiteTexture);
        }
    }

    void DrawGazeMarker()
    {
        for (int i = 1; i < trace.Count; i++)
        {
            GUI.color = new Color(0.3f, 0.9f, 1f, 0.45f * Mathf.Clamp01(1 - (Time.unscaledTime - trace[i].time) / 0.65f));
            DrawLine(ToGui(trace[i - 1].point), ToGui(trace[i].point), 2);
        }
        if (!input.HasValidGaze) return;
        Vector2 point = ToGui(input.ScreenPoint);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(point.x - 4, point.y - 4, 8, 8), dot);
        if (game.InMenu || game.GameOver) return;
        GUI.color = game.IsLineOfFireClear ? new Color(0.3f, 1f, 0.55f) : Color.cyan;
        if (game.Mode == GameController.SelectionMode.Dwell) DrawRing(point, 17, game.DwellProgress);
        else if (game.DwellTarget != null)
        {
            // Locked target: full ring and the action that fires it.
            DrawRing(point, 17, 1f);
            GUI.Label(new Rect(point.x + 22, point.y - 12, 160, 24), LockHint(game.Mode), labelStyle);
        }
    }

    void DrawBlinkIndicator()
    {
        if (game.Mode != GameController.SelectionMode.Blink || !input.EyesClosedCandidate) return;
        float t = input.EyesClosedSeconds;
        bool inWindow = t >= game.BlinkMinSeconds && t <= game.BlinkMaxSeconds;
        GUI.color = inWindow ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.72f, 0.2f);
        GUI.Label(new Rect(Screen.width / 2f - 200, 70, 400, 40), "Eyes closed " + t.ToString("0.00") + " s", bigStyle);
    }

    void DrawTarget()
    {
        if (game.DwellTarget == null || Camera.main == null) return;
        Bounds bounds = game.DwellTarget.bounds;
        Vector2 lo = Camera.main.WorldToScreenPoint(bounds.min);
        Vector2 hi = Camera.main.WorldToScreenPoint(bounds.max);
        Rect rect = Rect.MinMaxRect(lo.x - 5, Screen.height - hi.y - 5, hi.x + 5, Screen.height - lo.y + 5);
        GUI.color = game.IsLineOfFireClear ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.65f, 0.2f);
        DrawFrame(rect, 2);
    }

    static string ModeDescription(GameController.SelectionMode mode)
    {
        switch (mode)
        {
            case GameController.SelectionMode.Blink:
                return "Look at a target, then close your eyes a little longer than a normal blink.\n(Simulation: hold B)";
            case GameController.SelectionMode.Nod:
                return "Look at a target, then nod your head once.\n(Simulation: press N)";
            case GameController.SelectionMode.Button:
                return "Look at a target, then press Space or the left mouse button.";
            default:
                return "Look at a target and keep looking for 0.6 s. Eyes only.";
        }
    }

    static string ModeInstruction(GameController game)
    {
        switch (game.Mode)
        {
            case GameController.SelectionMode.Blink:
                return "Aim, then close eyes " + game.BlinkMinSeconds.ToString("0.0#") + "-" + game.BlinkMaxSeconds.ToString("0.0#") + " s to fire.";
            case GameController.SelectionMode.Nod: return "Aim, then nod once to fire.";
            case GameController.SelectionMode.Button: return "Aim, then press Space / left click to fire.";
            default: return "Dwell 0.6 s on a target to fire.";
        }
    }

    static string LockHint(GameController.SelectionMode mode)
    {
        switch (mode)
        {
            case GameController.SelectionMode.Blink: return "BLINK";
            case GameController.SelectionMode.Nod: return "NOD";
            default: return "SPACE / CLICK";
        }
    }

    static Vector2 ToGui(Vector2 point) { return new Vector2(point.x, Screen.height - point.y); }
    static void DrawFrame(Rect rect, float width)
    {
        DrawLine(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), width);
        DrawLine(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), width);
        DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax), width);
        DrawLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMin), width);
    }
    static void DrawLine(Vector2 start, Vector2 end, float width)
    {
        Matrix4x4 previous = GUI.matrix;
        Vector2 delta = end - start;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
        GUI.DrawTexture(new Rect(start.x, start.y - width / 2, delta.magnitude, width), Texture2D.whiteTexture);
        GUI.matrix = previous;
    }
    static void DrawRing(Vector2 center, float radius, float progress)
    {
        int segments = Mathf.CeilToInt(Mathf.Clamp01(progress) * 48);
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / 48 - Mathf.PI / 2;
            float b = (i + 1) * Mathf.PI * 2 / 48 - Mathf.PI / 2;
            DrawLine(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                     center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, 3);
        }
    }
    void OnDestroy() { if (dot != null) Destroy(dot); }
}
