// Student: Jan Biernacki | Matriculation number: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

/// <summary>Non-physics overlay: raw gaze trail, dwell feedback, status and gaze restart.</summary>
public class GazeHud : MonoBehaviour
{
    struct TracePoint { public Vector2 point; public float time; }
    readonly List<TracePoint> trace = new List<TracePoint>();
    GameController game;
    GazeInput input;
    Texture2D dot;
    GUIStyle titleStyle, labelStyle;
    float restartDwell;
    bool previousSimulation;
    GazeInput.GazeSource previousSource;

    static Rect Panel { get { return new Rect(12, 12, Mathf.Min(620, Screen.width - 24), 142); } }
    static Rect RestartArea { get { return new Rect(Screen.width / 2f - 160, Screen.height / 2f + 18, 320, 68); } }

    public static bool ControlsContain(Vector2 screenPoint)
    {
        Vector2 guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
        if (Panel.Contains(guiPoint)) return true;
        GameController controller = FindObjectOfType<GameController>();
        return controller != null && controller.GameOver && RestartArea.Contains(guiPoint);
    }

    void Awake()
    {
        game = GetComponent<GameController>();
        input = GetComponent<GazeInput>();
        dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            dot.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) <= 15 ? Color.white : Color.clear);
        dot.Apply();
    }

    void LateUpdate()
    {
        if (input == null || game == null) return;
        if (input.IsSimulation != previousSimulation || input.source != previousSource) trace.Clear();
        previousSimulation = input.IsSimulation;
        previousSource = input.source;
        trace.RemoveAll(p => Time.unscaledTime - p.time > 0.65f);
        if (!input.HasValidGaze)
        {
            trace.Clear();
            restartDwell = 0;
            return;
        }
        trace.Add(new TracePoint { point = input.ScreenPoint, time = Time.unscaledTime });
        if (trace.Count > 100) trace.RemoveAt(0);
        Vector2 position = new Vector2(input.ScreenPoint.x, Screen.height - input.ScreenPoint.y);
        if (game.GameOver && RestartArea.Contains(position))
        {
            restartDwell += Time.unscaledDeltaTime;
            if (restartDwell >= 1f)
            {
                restartDwell = 0;
                trace.Clear();
                game.RestartGame();
            }
        }
        else restartDwell = 0;
    }

    void OnGUI()
    {
        if (game == null || input == null) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        }
        input.SetTalonViewportBounds(GUIUtility.GUIToScreenPoint(Vector2.zero),
            GUIUtility.GUIToScreenPoint(new Vector2(Screen.width, Screen.height)));
        Color previous = GUI.color;
        Rect panel = Panel;
        GUI.color = new Color(0.03f, 0.06f, 0.13f, 0.94f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(24, 17, panel.width - 20, 28), "GAZE LAB     Score " + game.points + "     Hull " + Mathf.Max(0, 100 - game.damage * 25) + "%", titleStyle);
        GUI.color = input.IsSimulation || input.IsTestData ? new Color(1f, 0.72f, 0.2f) : Color.cyan;
        GUI.Label(new Rect(24, 47, panel.width - 20, 23), input.StatusText, labelStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(24, 70, panel.width - 20, 22), "Dwell 0.6 s to fire. Rocks +20 / Satellites -100. No fire button.", labelStyle);
        if (GUI.Button(new Rect(24, 96, 105, 22), "Talon gaze")) input.SelectSource(GazeInput.GazeSource.Talon);
        if (GUI.Button(new Rect(139, 96, 105, 22), "Tobii SDK")) input.SelectSource(GazeInput.GazeSource.TobiiSdk);
        if (GUI.Button(new Rect(254, 96, 142, 22), "Mouse simulation")) input.SetSimulation(true);
        GUI.Label(new Rect(24, 122, panel.width - 20, 22), "Jan Biernacki | Matriculation: TO BE PROVIDED", labelStyle);

        DrawTarget();
        for (int i = 1; i < trace.Count; i++)
        {
            GUI.color = new Color(0.3f, 0.9f, 1f, 0.45f * Mathf.Clamp01(1 - (Time.unscaledTime - trace[i].time) / 0.65f));
            DrawLine(ToGui(trace[i - 1].point), ToGui(trace[i].point), 2);
        }
        if (input.HasValidGaze)
        {
            Vector2 point = ToGui(input.ScreenPoint);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(point.x - 4, point.y - 4, 8, 8), dot);
            GUI.color = game.IsLineOfFireClear ? new Color(0.3f, 1f, 0.55f) : Color.cyan;
            DrawRing(point, 17, game.DwellProgress);
        }
        if (Time.unscaledTime - game.LastShotTime < 0.15f)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 35, 28, 100, 30), "HIT", titleStyle);
        }
        if (game.GameOver)
        {
            GUI.color = new Color(0.03f, 0.06f, 0.13f, 0.96f);
            GUI.DrawTexture(new Rect(Screen.width / 2f - 195, Screen.height / 2f - 52, 390, 158), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 160, Screen.height / 2f - 35, 340, 40), "GAME OVER  |  Score " + game.points, titleStyle);
            if (GUI.Button(RestartArea, "Look here for 1 second to restart")) game.RestartGame();
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(RestartArea.x, RestartArea.yMax - 6, RestartArea.width * Mathf.Clamp01(restartDwell), 6), Texture2D.whiteTexture);
        }
        GUI.color = previous;
    }

    void DrawTarget()
    {
        if (game.DwellTarget == null || Camera.main == null) return;
        Bounds bounds = game.DwellTarget.bounds;
        Vector2 lo = Camera.main.WorldToScreenPoint(bounds.min);
        Vector2 hi = Camera.main.WorldToScreenPoint(bounds.max);
        Rect rect = Rect.MinMaxRect(lo.x - 5, Screen.height - hi.y - 5, hi.x + 5, Screen.height - lo.y + 5);
        GUI.color = game.IsLineOfFireClear ? new Color(0.3f, 1f, 0.55f) : new Color(1f, 0.65f, 0.2f);
        DrawLine(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), 2);
        DrawLine(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), 2);
        DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax), 2);
        DrawLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMin), 2);
    }

    static Vector2 ToGui(Vector2 point) { return new Vector2(point.x, Screen.height - point.y); }
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
