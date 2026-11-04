// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Non-physics overlay: gaze trail, selection feedback, status card and the
/// gaze-only menus (mode selection at start, restart / change mode after game over).
/// All layout values are given for a 1080p screen and scaled with the Game view height,
/// so the HUD keeps its size on 4K / high-DPI displays.
/// </summary>
public class GazeHud : MonoBehaviour
{
    struct TracePoint { public Vector2 point; public float time; }
    struct GazeButton { public Rect rect; public string number, title, body; public System.Action action; }

    const float MenuDwellSeconds = 1f;
    const float ButtonTolerance = 40f;             // design px: gaze slightly outside a button still counts
    const float ButtonLossToleranceSeconds = 0.3f; // blinks / jitter keep the progress frozen
    static readonly GameController.SelectionMode[] MenuModes =
    {
        GameController.SelectionMode.Dwell, GameController.SelectionMode.Blink,
        GameController.SelectionMode.Nod, GameController.SelectionMode.Button
    };

    // Palette
    static readonly Color Panel = new Color(0.035f, 0.06f, 0.12f, 0.88f);
    static readonly Color PanelSolid = new Color(0.035f, 0.06f, 0.12f, 0.97f);
    static readonly Color Edge = new Color(0.35f, 0.85f, 1f, 0.28f);
    static readonly Color Accent = new Color(0.35f, 0.88f, 1f);
    static readonly Color Good = new Color(0.32f, 1f, 0.58f);
    static readonly Color Warn = new Color(1f, 0.74f, 0.25f);
    static readonly Color Bad = new Color(1f, 0.38f, 0.38f);
    static readonly Color Muted = new Color(0.62f, 0.7f, 0.82f);
    static readonly Color Track = new Color(1f, 1f, 1f, 0.1f);

    readonly List<TracePoint> trace = new List<TracePoint>();
    readonly List<GazeButton> buttons = new List<GazeButton>();
    GameController game;
    GazeInput input;
    Texture2D dot, round, ring;
    GUIStyle roundStyle, ringStyle, caption, value, body, bodyMuted, small, chip, center, huge, tileTitle, tileBody, badge, segment;
    float stylesForScale = -1f;
    int hoveredButton = -1;
    float buttonDwell, lastButtonHit;
    bool previousSimulation;

    /// <summary>HUD scale: 1 at 1080p, larger on 4K Game views.</summary>
    static float Ui { get { return Mathf.Clamp(Screen.height / 1080f, 0.75f, 3f); } }
    static float VirtualWidth { get { return Screen.width / Ui; } }
    static float VirtualHeight { get { return Screen.height / Ui; } }
    static Rect R(float x, float y, float w, float h) { float s = Ui; return new Rect(x * s, y * s, w * s, h * s); }

    // Design-space layout of the status card and the input row below it.
    static readonly Rect CardDesign = new Rect(16, 16, 384, 150);
    static readonly Rect ControlsDesign = new Rect(16, 174, 384, 28);
    static Rect PanelArea { get { return R(16, 16, 384, 186); } }

    public static bool ControlsContain(Vector2 screenPoint)
    {
        Vector2 guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
        if (PanelArea.Contains(guiPoint)) return true;
        GameController controller = FindObjectOfType<GameController>();
        if (controller == null || !(controller.InMenu || controller.GameOver)) return false;
        foreach (Rect rect in OverlayRects(controller)) if (rect.Contains(guiPoint)) return true;
        return false;
    }

    /// <summary>GUI-space (pixel) rectangles of the gaze buttons for the current overlay.</summary>
    static List<Rect> OverlayRects(GameController controller)
    {
        var rects = new List<Rect>();
        float vw = VirtualWidth, vh = VirtualHeight;
        if (controller.InMenu)
        {
            float gap = 24f;
            float width = Mathf.Min(420f, (vw - 3 * gap) / 2f);
            float height = 150f;
            float top = Mathf.Max(CardDesign.yMax + 110f, vh * 0.5f - height - gap * 0.5f + 30f);
            float left = vw * 0.5f - width - gap * 0.5f;
            for (int i = 0; i < 4; i++)
                rects.Add(R(left + (i % 2) * (width + gap), top + (i / 2) * (height + gap), width, height));
        }
        else if (controller.GameOver)
        {
            float width = 280f, height = 104f, gap = 24f;
            float y = vh * 0.5f + 10f;
            rects.Add(R(vw * 0.5f - width - gap * 0.5f, y, width, height));
            rects.Add(R(vw * 0.5f + gap * 0.5f, y, width, height));
        }
        return rects;
    }

    void Awake()
    {
        game = GetComponent<GameController>();
        input = GetComponent<GazeInput>();
        dot = MakeRound(32, 15.5f);
    }

    static Texture2D MakeRound(int size, float radius, float thickness = 0f)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float alpha = Mathf.Clamp01(radius - d + 0.5f);
            if (thickness > 0f) alpha *= Mathf.Clamp01(d - (radius - thickness) + 0.5f); // ring only
            tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        tex.Apply();
        return tex;
    }

    /// <summary>Rounded-rectangle 9-slice texture; corner radius scales with the HUD.</summary>
    void BuildStyles()
    {
        float s = Ui;
        stylesForScale = s;
        int radius = Mathf.Max(4, Mathf.RoundToInt(10 * s));
        if (round != null) Destroy(round);
        if (ring != null) Destroy(ring);
        round = MakeRound(radius * 2 + 2, radius);
        ring = MakeRound(radius * 2 + 2, radius, Mathf.Max(1f, Mathf.Round(1.2f * s)));
        roundStyle = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
        roundStyle.normal.background = round;
        ringStyle = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
        ringStyle.normal.background = ring;

        System.Func<int, FontStyle, TextAnchor, GUIStyle> make = (size, fontStyle, anchor) =>
        {
            var st = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(size * s), fontStyle = fontStyle, alignment = anchor,
                wordWrap = true, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0)
            };
            st.normal.textColor = Color.white;
            return st;
        };
        caption = make(11, FontStyle.Bold, TextAnchor.UpperLeft);
        value = make(34, FontStyle.Bold, TextAnchor.UpperLeft);
        body = make(14, FontStyle.Normal, TextAnchor.MiddleLeft);
        bodyMuted = make(13, FontStyle.Normal, TextAnchor.MiddleLeft);
        small = make(12, FontStyle.Normal, TextAnchor.MiddleLeft);
        chip = make(12, FontStyle.Bold, TextAnchor.MiddleCenter);
        segment = make(13, FontStyle.Bold, TextAnchor.MiddleCenter);
        center = make(20, FontStyle.Bold, TextAnchor.MiddleCenter);
        huge = make(34, FontStyle.Bold, TextAnchor.MiddleCenter);
        tileTitle = make(24, FontStyle.Bold, TextAnchor.UpperLeft);
        tileBody = make(15, FontStyle.Normal, TextAnchor.UpperLeft);
        badge = make(18, FontStyle.Bold, TextAnchor.MiddleCenter);
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
                    rect = rects[i], number = (i + 1).ToString(),
                    title = GameController.ModeName(mode), body = ModeDescription(mode),
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
        float bestDistance = ButtonTolerance * Ui;
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
        if (roundStyle == null || !Mathf.Approximately(stylesForScale, Ui)) BuildStyles();
        Color previous = GUI.color;
        if (buttons.Count == 0) RebuildButtons();

        if (game.InMenu) DrawMenu();
        else
        {
            DrawTarget();
            DrawBlinkIndicator();
        }
        if (game.GameOver) DrawGameOver();
        DrawCard();
        DrawControls();
        DrawFooter();
        DrawGazeMarker();
        DrawFeedback();
        GUI.color = previous;
    }

    // ---------- status card ----------

    void DrawCard()
    {
        Rect card = R(CardDesign.x, CardDesign.y, CardDesign.width, CardDesign.height);
        Box(card, Panel);
        Outline(card, Edge);
        float x = CardDesign.x + 18, y = CardDesign.y;

        Text(R(x, y + 14, 120, 16), "GAZE LAB", caption, Accent);
        string modeText = game.InMenu ? "CHOOSE MODE" : GameController.ModeName(game.Mode).ToUpperInvariant();
        float chipWidth = Mathf.Min(220f, 26f + modeText.Length * 7.6f);
        Rect chipRect = R(CardDesign.xMax - 16 - chipWidth, y + 10, chipWidth, 22);
        Box(chipRect, new Color(Accent.r, Accent.g, Accent.b, 0.16f));
        Text(chipRect, modeText, chip, Accent);

        Text(R(x, y + 40, 150, 14), "SCORE", caption, Muted);
        Text(R(x, y + 52, 170, 42), game.points.ToString(), value, Color.white);

        int hull = Mathf.Clamp(4 - game.damage, 0, 4);
        Text(R(x + 186, y + 40, 150, 14), "HULL  " + hull * 25 + " %", caption, Muted);
        Color hullColor = hull >= 3 ? Good : hull == 2 ? Warn : Bad;
        for (int i = 0; i < 4; i++)
            Box(R(x + 186 + i * 44, y + 62, 39, 12), i < hull ? hullColor : Track);

        Color statusColor = input.IsSimulation ? Warn : input.HasValidGaze ? Good : Bad;
        GUI.color = statusColor;
        GUI.DrawTexture(R(x, y + 103, 10, 10), dot);
        Text(R(x + 18, y + 98, CardDesign.width - 52, 20), input.StatusText, body, Color.white);

        string hint = game.InMenu ? "Look at a mode for 1 s to start.  Rocks +20  ·  Satellites −100" : ModeInstruction(game);
        if (!game.InMenu && game.Mode == GameController.SelectionMode.Nod && input.HeadStatus.Length > 0)
            hint += "  ·  " + input.HeadStatus;
        Text(R(x, y + 122, CardDesign.width - 36, 20), hint, bodyMuted, Muted);
    }

    void DrawControls()
    {
        float x = ControlsDesign.x, y = ControlsDesign.y, h = ControlsDesign.height;
        Rect group = R(x, y, 230, h);
        Box(group, Panel);
        if (Segment(R(x + 3, y + 3, 112, h - 6), "Eye tracker", !input.IsSimulation)) input.ChooseSource(false);
        if (Segment(R(x + 115, y + 3, 112, h - 6), "Mouse", input.IsSimulation)) input.ChooseSource(true);

        if (!input.IsSimulation && input.DisplayScale > 1.01f)
        {
            string label = "Scaling " + Mathf.RoundToInt(input.DisplayScale * 100) + " %: fix " + (input.compensateDisplayScaling ? "on" : "off");
            Rect pill = R(x + 238, y, 146, h);
            Box(pill, Panel);
            Outline(pill, input.compensateDisplayScaling ? new Color(Good.r, Good.g, Good.b, 0.5f) : new Color(Warn.r, Warn.g, Warn.b, 0.6f));
            Text(pill, label, chip, input.compensateDisplayScaling ? Good : Warn);
            if (GUI.Button(pill, GUIContent.none, GUIStyle.none)) input.compensateDisplayScaling = !input.compensateDisplayScaling;
        }
    }

    bool Segment(Rect rect, string label, bool selected)
    {
        if (selected) Box(rect, Accent);
        Text(rect, label, segment, selected ? new Color(0.03f, 0.06f, 0.12f) : Muted);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    void DrawFooter()
    {
        Text(R(16, VirtualHeight - 28, 700, 20), "Team: Jan Biernacki · Kürsat · Paul · Malte   |   Matriculation numbers: TO BE PROVIDED",
            small, new Color(Muted.r, Muted.g, Muted.b, 0.85f));
    }

    // ---------- menus ----------

    void DrawMenu()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        if (buttons.Count == 0) return;
        Rect first = buttons[0].rect;
        float top = first.y / Ui;
        Text(R(0, top - 92, VirtualWidth, 44), "Choose a shooting mode", huge, Color.white);
        Text(R(0, top - 48, VirtualWidth, 24), "Look at a mode for one second", center, Muted);
        for (int i = 0; i < buttons.Count; i++) DrawTile(i);
    }

    void DrawGameOver()
    {
        float vw = VirtualWidth, vh = VirtualHeight;
        Rect card = R(vw * 0.5f - 330, vh * 0.5f - 120, 660, 268);
        Box(card, PanelSolid);
        Outline(card, Edge);
        Text(R(vw * 0.5f - 330, vh * 0.5f - 104, 660, 40), "GAME OVER", huge, Color.white);
        Text(R(vw * 0.5f - 330, vh * 0.5f - 58, 660, 30), "Score " + game.points, center, Accent);
        for (int i = 0; i < buttons.Count; i++) DrawTile(i);
    }

    void DrawTile(int index)
    {
        GazeButton button = buttons[index];
        bool hovered = index == hoveredButton;
        Rect r = button.rect;
        float s = Ui;
        Box(r, hovered ? new Color(0.07f, 0.2f, 0.3f, 0.97f) : PanelSolid);
        Outline(r, hovered ? Good : Edge);

        float textLeft = 20 * s;
        if (!string.IsNullOrEmpty(button.number))
        {
            Rect badgeRect = new Rect(r.x + 18 * s, r.y + 18 * s, 36 * s, 36 * s);
            GUI.color = hovered ? Good : new Color(Accent.r, Accent.g, Accent.b, 0.2f);
            GUI.DrawTexture(badgeRect, dot);
            Text(badgeRect, button.number, badge, hovered ? new Color(0.03f, 0.06f, 0.12f) : Accent);
            textLeft = 66 * s;
        }
        Text(new Rect(r.x + textLeft, r.y + 20 * s, r.width - textLeft - 18 * s, 32 * s), button.title, tileTitle, Color.white);
        Text(new Rect(r.x + textLeft, r.y + 56 * s, r.width - textLeft - 18 * s, r.height - 70 * s), button.body, tileBody, Muted);

        // Dwell progress along the bottom edge.
        Rect bar = new Rect(r.x + 14 * s, r.yMax - 14 * s, r.width - 28 * s, 5 * s);
        if (hovered)
        {
            GUI.color = Track;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Good;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(buttonDwell / MenuDwellSeconds), bar.height), Texture2D.whiteTexture);
        }
    }

    // ---------- in-game feedback ----------

    void DrawFeedback()
    {
        if (game.InMenu) return;
        if (Time.unscaledTime - game.LastConfirmTime < 1.2f && game.LastConfirmText.Length > 0)
        {
            bool fired = game.LastConfirmText.EndsWith("fired");
            float w = 460f;
            Rect pill = R(VirtualWidth * 0.5f - w * 0.5f, VirtualHeight - 84, w, 36);
            Box(pill, Panel);
            Outline(pill, fired ? Good : Warn);
            Text(pill, game.LastConfirmText, segment, fired ? Good : Warn);
        }
        if (Time.unscaledTime - game.LastShotTime < 0.2f)
            Text(R(VirtualWidth * 0.5f - 100, 24, 200, 44), "HIT", huge, Good);
    }

    void DrawGazeMarker()
    {
        float s = Ui;
        for (int i = 1; i < trace.Count; i++)
        {
            GUI.color = new Color(Accent.r, Accent.g, Accent.b, 0.45f * Mathf.Clamp01(1 - (Time.unscaledTime - trace[i].time) / 0.65f));
            DrawLine(ToGui(trace[i - 1].point), ToGui(trace[i].point), 2 * s);
        }
        if (!input.HasValidGaze) return;
        Vector2 point = ToGui(input.ScreenPoint);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(point.x - 4 * s, point.y - 4 * s, 8 * s, 8 * s), dot);
        if (game.InMenu || game.GameOver) return;
        GUI.color = game.IsLineOfFireClear ? Good : Accent;
        if (game.Mode == GameController.SelectionMode.Dwell) DrawRing(point, 17 * s, game.DwellProgress, 3 * s);
        else if (game.DwellTarget != null)
        {
            // Locked target: full ring and the action that fires it.
            DrawRing(point, 17 * s, 1f, 3 * s);
            string hint = LockHint(game.Mode);
            Rect pill = new Rect(point.x + 26 * s, point.y - 12 * s, (24 + hint.Length * 8) * s, 24 * s);
            Box(pill, Panel);
            Text(pill, hint, chip, Good);
        }
    }

    void DrawBlinkIndicator()
    {
        if (game.Mode != GameController.SelectionMode.Blink || !input.EyesClosedCandidate) return;
        float t = input.EyesClosedSeconds;
        bool inWindow = t >= game.BlinkMinSeconds && t <= game.BlinkMaxSeconds;
        Rect pill = R(VirtualWidth * 0.5f - 160, 80, 320, 52);
        Box(pill, Panel);
        Outline(pill, inWindow ? Good : Warn);
        Text(pill, "Eyes closed " + Num(t, "0.00") + " s", center, inWindow ? Good : Warn);
    }

    void DrawTarget()
    {
        if (game.DwellTarget == null || Camera.main == null) return;
        Bounds bounds = game.DwellTarget.bounds;
        Vector2 lo = Camera.main.WorldToScreenPoint(bounds.min);
        Vector2 hi = Camera.main.WorldToScreenPoint(bounds.max);
        float pad = 6 * Ui;
        Rect rect = Rect.MinMaxRect(lo.x - pad, Screen.height - hi.y - pad, hi.x + pad, Screen.height - lo.y + pad);
        GUI.color = game.IsLineOfFireClear ? Good : Warn;
        DrawCorners(rect, 2.5f * Ui, Mathf.Min(rect.width, rect.height) * 0.25f);
    }

    // ---------- texts ----------

    static string ModeDescription(GameController.SelectionMode mode)
    {
        switch (mode)
        {
            case GameController.SelectionMode.Blink:
                return "Look at a target, then close both eyes a little longer than a normal blink.\nSimulation: hold B";
            case GameController.SelectionMode.Nod:
                return "Look at a target, then nod your head once.\nSimulation: press N";
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
                return "Aim, then close both eyes " + Num(game.BlinkMinSeconds, "0.0#") + "–" + Num(game.BlinkMaxSeconds, "0.0#") + " s";
            case GameController.SelectionMode.Nod: return "Aim, then nod once";
            case GameController.SelectionMode.Button: return "Aim, then press Space / left click";
            default: return "Keep looking at a target for 0.6 s";
        }
    }

    static string Num(float v, string format) { return v.ToString(format, System.Globalization.CultureInfo.InvariantCulture); }

    static string LockHint(GameController.SelectionMode mode)
    {
        switch (mode)
        {
            case GameController.SelectionMode.Blink: return "BLINK";
            case GameController.SelectionMode.Nod: return "NOD";
            default: return "SPACE / CLICK";
        }
    }

    // ---------- drawing helpers ----------

    void Box(Rect rect, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        GUI.color = color;
        roundStyle.Draw(rect, false, false, false, false);
    }

    void Outline(Rect rect, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        GUI.color = color;
        ringStyle.Draw(rect, false, false, false, false);
    }

    static void Text(Rect rect, string text, GUIStyle style, Color color)
    {
        GUI.color = Color.white;
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    static Vector2 ToGui(Vector2 point) { return new Vector2(point.x, Screen.height - point.y); }

    static void DrawCorners(Rect r, float width, float length)
    {
        DrawLine(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + length, r.yMin), width);
        DrawLine(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMin + length), width);
        DrawLine(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax - length, r.yMin), width);
        DrawLine(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + length), width);
        DrawLine(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin + length, r.yMax), width);
        DrawLine(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - length), width);
        DrawLine(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - length, r.yMax), width);
        DrawLine(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMax - length), width);
    }

    static void DrawLine(Vector2 start, Vector2 end, float width)
    {
        Matrix4x4 previous = GUI.matrix;
        Vector2 delta = end - start;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
        GUI.DrawTexture(new Rect(start.x, start.y - width / 2, delta.magnitude, width), Texture2D.whiteTexture);
        GUI.matrix = previous;
    }

    static void DrawRing(Vector2 center, float radius, float progress, float width)
    {
        int segments = Mathf.CeilToInt(Mathf.Clamp01(progress) * 48);
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / 48 - Mathf.PI / 2;
            float b = (i + 1) * Mathf.PI * 2 / 48 - Mathf.PI / 2;
            DrawLine(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                     center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width);
        }
    }

    void OnDestroy()
    {
        if (dot != null) Destroy(dot);
        if (round != null) Destroy(round);
    }
}
