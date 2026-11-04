// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-game 5-point gaze correction. The player looks at five dots; an affine mapping from the
/// measured gaze to the true dot positions is fitted (least squares) and applied to all later
/// Tobii samples. This removes constant offsets and scaling errors caused by the tracker
/// calibration or by the Editor's Game-view mapping (toolbar, DPI scaling, aspect ratio).
/// </summary>
public class GazeCalibration : MonoBehaviour
{
    /// <summary>Affine mapping in viewport space (0..1, bottom-left origin).</summary>
    [System.Serializable]
    public struct Affine
    {
        public float ax, bx, cx, ay, by, cy;
        public static Affine Identity { get { return new Affine { ax = 1f, by = 1f }; } }
        public Vector2 Apply(Vector2 p) { return new Vector2(ax * p.x + bx * p.y + cx, ay * p.x + by * p.y + cy); }
    }

    // Dots in viewport coordinates: centre and four corners (kept away from the edges).
    public static readonly Vector2[] Targets =
    {
        new Vector2(0.5f, 0.5f), new Vector2(0.12f, 0.85f), new Vector2(0.88f, 0.85f),
        new Vector2(0.88f, 0.15f), new Vector2(0.12f, 0.15f)
    };
    const float SettleSeconds = 0.7f;  // eyes need time to reach and fixate the new dot
    const float CollectSeconds = 0.9f;
    const int MinSamples = 10;

    public bool IsRunning { get; private set; }
    public string Message { get; private set; }
    public float MessageTime { get; private set; }

    GazeInput input;
    GameController game;
    int index, retries;
    float pointTime;
    readonly List<Vector2> samples = new List<Vector2>();
    readonly Vector2[] measured = new Vector2[Targets.Length];
    Texture2D dot;

    void Awake()
    {
        input = GetComponent<GazeInput>();
        game = GetComponent<GameController>();
        Message = "";
        MessageTime = float.NegativeInfinity;
        dot = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            dot.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) <= 31 ? Color.white : Color.clear);
        dot.Apply();
    }

    public void Begin()
    {
        if (input == null || input.IsSimulation) { Say("Select the Tobii eye tracker before calibrating"); return; }
        if (game != null) game.ShowMenu();
        input.SuspendCorrection = true;
        IsRunning = true;
        index = 0;
        retries = 0;
        pointTime = 0f;
        samples.Clear();
        Say("");
    }

    public void Cancel(string reason)
    {
        IsRunning = false;
        if (input != null) input.SuspendCorrection = false;
        Say(reason);
    }

    void Say(string text) { Message = text; MessageTime = Time.unscaledTime; }

    void Update()
    {
        if (!IsRunning) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Cancel("Calibration cancelled"); return; }
        if (input.IsSimulation) { Cancel("Calibration cancelled: mouse simulation selected"); return; }
        pointTime += Time.unscaledDeltaTime;
        if (pointTime >= SettleSeconds && input.HasRawGaze) samples.Add(input.RawViewportPoint);
        if (pointTime < SettleSeconds + CollectSeconds) return;

        if (samples.Count < MinSamples)
        {
            // Not enough valid gaze for this dot (blink, looking away): repeat it a few times.
            if (++retries > 3) { Cancel("Calibration failed: no gaze at dot " + (index + 1) + ". Is the Game view on the tracker's monitor?"); return; }
            pointTime = 0f;
            samples.Clear();
            return;
        }
        measured[index] = Median(samples);
        samples.Clear();
        pointTime = 0f;
        retries = 0;
        if (++index < Targets.Length) return;
        Finish();
    }

    void Finish()
    {
        IsRunning = false;
        input.SuspendCorrection = false;
        Affine fit;
        if (!Fit(measured, Targets, out fit) || !IsPlausible(fit))
        {
            Say("Calibration rejected: gaze does not match the screen. Check monitor and Tobii calibration.");
            return;
        }
        float before = MeanErrorPixels(measured, Targets, Affine.Identity);
        float after = MeanErrorPixels(measured, Targets, fit);
        input.SetCorrection(fit);
        Say("Calibration saved: mean error " + before.ToString("0") + " px -> " + after.ToString("0") + " px");
    }

    static Vector2 Median(List<Vector2> points)
    {
        var xs = new List<float>(points.Count);
        var ys = new List<float>(points.Count);
        foreach (Vector2 p in points) { xs.Add(p.x); ys.Add(p.y); }
        xs.Sort();
        ys.Sort();
        return new Vector2(xs[xs.Count / 2], ys[ys.Count / 2]);
    }

    static float MeanErrorPixels(Vector2[] raw, Vector2[] target, Affine map)
    {
        float sum = 0f;
        Vector2 size = new Vector2(Screen.width, Screen.height);
        for (int i = 0; i < raw.Length; i++) sum += Vector2.Scale(map.Apply(raw[i]) - target[i], size).magnitude;
        return sum / raw.Length;
    }

    /// <summary>Scale between 0.5 and 2 and offsets below half a screen: anything else is a wrong setup.</summary>
    public static bool IsPlausible(Affine a)
    {
        return a.ax > 0.5f && a.ax < 2f && a.by > 0.5f && a.by < 2f
            && Mathf.Abs(a.bx) < 0.3f && Mathf.Abs(a.ay) < 0.3f
            && Mathf.Abs(a.cx) < 0.5f && Mathf.Abs(a.cy) < 0.5f;
    }

    /// <summary>Least-squares affine fit target = A * raw + c (separately for x and y).</summary>
    public static bool Fit(Vector2[] raw, Vector2[] target, out Affine result)
    {
        result = Affine.Identity;
        if (raw == null || target == null || raw.Length != target.Length || raw.Length < 3) return false;
        // Normal equations M * p = v with M = sum [x y 1]^T [x y 1].
        double sxx = 0, sxy = 0, sx = 0, syy = 0, sy = 0, n = raw.Length;
        double tx_x = 0, tx_y = 0, tx_1 = 0, ty_x = 0, ty_y = 0, ty_1 = 0;
        for (int i = 0; i < raw.Length; i++)
        {
            double x = raw[i].x, y = raw[i].y;
            sxx += x * x; sxy += x * y; sx += x; syy += y * y; sy += y;
            tx_x += target[i].x * x; tx_y += target[i].x * y; tx_1 += target[i].x;
            ty_x += target[i].y * x; ty_y += target[i].y * y; ty_1 += target[i].y;
        }
        double[,] m = { { sxx, sxy, sx }, { sxy, syy, sy }, { sx, sy, n } };
        double[] px, py;
        if (!Solve3(m, new[] { tx_x, tx_y, tx_1 }, out px) || !Solve3(m, new[] { ty_x, ty_y, ty_1 }, out py)) return false;
        result = new Affine
        {
            ax = (float)px[0], bx = (float)px[1], cx = (float)px[2],
            ay = (float)py[0], by = (float)py[1], cy = (float)py[2]
        };
        return true;
    }

    static bool Solve3(double[,] m, double[] v, out double[] p)
    {
        p = null;
        double det = Det(m[0, 0], m[0, 1], m[0, 2], m[1, 0], m[1, 1], m[1, 2], m[2, 0], m[2, 1], m[2, 2]);
        if (System.Math.Abs(det) < 1e-12) return false;
        p = new double[3];
        p[0] = Det(v[0], m[0, 1], m[0, 2], v[1], m[1, 1], m[1, 2], v[2], m[2, 1], m[2, 2]) / det;
        p[1] = Det(m[0, 0], v[0], m[0, 2], m[1, 0], v[1], m[1, 2], m[2, 0], v[2], m[2, 2]) / det;
        p[2] = Det(m[0, 0], m[0, 1], v[0], m[1, 0], m[1, 1], v[1], m[2, 0], m[2, 1], v[2]) / det;
        return true;
    }

    static double Det(double a, double b, double c, double d, double e, double f, double g, double h, double i)
    {
        return a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
    }

    void OnGUI()
    {
        if (!IsRunning) return;
        GUI.depth = -100; // draw above the HUD
        Color previous = GUI.color;
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.97f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        Vector2 target = Targets[index];
        Vector2 center = new Vector2(target.x * Screen.width, (1f - target.y) * Screen.height);
        // The dot shrinks while it is being measured to keep the eyes on its centre.
        float t = Mathf.Clamp01(pointTime / (SettleSeconds + CollectSeconds));
        float radius = Mathf.Lerp(26f, 9f, t);
        GUI.color = pointTime >= SettleSeconds ? new Color(0.3f, 1f, 0.55f) : Color.white;
        GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), dot);
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(center.x - 3, center.y - 3, 6, 6), dot);
        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, Screen.height * 0.5f + 60, Screen.width, 40),
            "Gaze calibration " + (index + 1) + "/" + Targets.Length + " - look at the centre of the dot (Esc cancels)", style);
        GUI.color = previous;
    }

    void OnDestroy() { if (dot != null) Destroy(dot); }
}
