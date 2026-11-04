// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Tobii.Gaming;
using Tobii.Gaming.Internal;
#endif

/// <summary>
/// Gaze input from the Tobii 4C (Tobii Unity SDK) or a clearly labelled mouse simulation,
/// plus the two non-gaze confirmation signals used by the shooting modes: deliberate
/// blinks and head nods.
/// </summary>
public class GazeInput : MonoBehaviour
{
    // The Tobii native library can crash Unity when the Tobii runtime does not fit the SDK
    // (e.g. old Tobii Core Software). It is therefore only loaded after the player selected
    // "Tobii eye tracker" once. A crash guard falls back to mouse simulation on the next start.
    public bool simulateMouse = true;
    [Tooltip("Smoothing of small gaze jitter (0 = raw gaze). Large jumps (saccades) are never smoothed.")]
    [Range(0f, 0.95f)] public float gazeSmoothing = 0.75f;
    [Tooltip("Gaze jumps larger than this (pixels) are treated as saccades and followed immediately.")]
    public float saccadeThresholdPixels = 90f;
    [Tooltip("Undo the Tobii SDK mapping error under Windows display scaling (e.g. 150 %): " +
             "without it the gaze is pulled towards the centre of the Game view by the scaling factor.")]
    public bool compensateDisplayScaling = true;
    /// <summary>Windows display scaling of the game window (1 = 100 %, 1.5 = 150 %).</summary>
    public float DisplayScale { get; private set; }
    /// <summary>Set by the GameController while the head-nod shooting mode is active.</summary>
    public bool trackHeadPose;
    [Range(0.02f, 0.5f)] public float maximumSampleAge = 0.1f;
    public bool HasValidGaze { get; private set; }
    public Vector2 ScreenPoint { get; private set; }
    public string StatusText { get; private set; }
    public bool IsSimulation { get { return simulateMouse; } }
    public bool IsOverControls { get { return GazeHud.ControlsContain(ScreenPoint); } }

    // Blink signal: the tracker is working but currently sees no gaze (eyes closed).
    public bool EyesClosedCandidate { get; private set; }
    public float EyesClosedSeconds { get; private set; }
    /// <summary>True for exactly one frame when the eyes reopen after a closed period.</summary>
    public bool BlinkEnded { get; private set; }
    public float LastBlinkSeconds { get; private set; }
    bool eyesClosed;
    float closedSince;

    // Head-nod signal.
    readonly HeadNodDetector nod = new HeadNodDetector();
    /// <summary>True for exactly one frame when a head nod completes.</summary>
    public bool NodDetected { get; private set; }
    public bool HeadPoseAvailable { get; private set; }
    public string HeadStatus { get; private set; }

    bool lastSimulation;
    Vector2 smoothedPoint;
    bool hasSmoothedPoint;

    // Crash guard (stored in PlayerPrefs, survives an Editor crash).
    const string PrefUseTobii = "GazeLab.UseTobii";
    const string PrefTobiiPending = "GazeLab.TobiiInitPending";
    const float TobiiConfirmSeconds = 3f;
    float tobiiArmedAt = -1f;
    /// <summary>True when Unity crashed while the Tobii library was starting last time.</summary>
    public bool TobiiCrashedLastTime { get; private set; }

    void Awake()
    {
        StatusText = "Waiting for gaze";
        HeadStatus = "";
        if (PlayerPrefs.GetInt(PrefTobiiPending, 0) == 1)
        {
            TobiiCrashedLastTime = true;
            PlayerPrefs.SetInt(PrefTobiiPending, 0);
            PlayerPrefs.SetInt(PrefUseTobii, 0);
            PlayerPrefs.Save();
            simulateMouse = true;
        }
        else if (PlayerPrefs.GetInt(PrefUseTobii, 0) == 1) simulateMouse = false;
        lastSimulation = simulateMouse;
        if (GetComponent<GazeHud>() == null) gameObject.AddComponent<GazeHud>();
    }

    /// <summary>Called by the HUD buttons: switches the source and remembers the choice.</summary>
    public void ChooseSource(bool mouseSimulation)
    {
        SetSimulation(mouseSimulation);
        if (mouseSimulation)
        {
            EndTobiiSession();
            PlayerPrefs.SetInt(PrefUseTobii, 0);
            PlayerPrefs.SetInt(PrefTobiiPending, 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>true: mouse simulation, false: Tobii eye tracker. Cancels any pending selection.</summary>
    public void SetSimulation(bool enabled)
    {
        simulateMouse = enabled;
        HasValidGaze = false;
        ResetSignals();
        CancelSelection();
    }

    void CancelSelection()
    {
        GameController game = GetComponent<GameController>();
        if (game != null) game.CancelSelection();
    }

    void ResetSignals()
    {
        hasSmoothedPoint = false;
        eyesClosed = false;
        EyesClosedCandidate = false;
        EyesClosedSeconds = 0f;
        BlinkEnded = false;
        NodDetected = false;
        nod.Reset();
    }

    void Update()
    {
        BlinkEnded = false;
        NodDetected = false;
        if (simulateMouse != lastSimulation) { CancelSelection(); ResetSignals(); lastSimulation = simulateMouse; }
        bool closedCandidate = ReadGaze();
        UpdateBlink(closedCandidate);
        UpdateHeadPose();
    }

    /// <summary>Reads the active gaze source. Returns true when the tracker works but sees no eyes.</summary>
    bool ReadGaze()
    {
        HasValidGaze = false;
        if (!Application.isFocused)
        {
            StatusText = "Game window inactive - paused";
            return false;
        }
        if (simulateMouse)
        {
            ScreenPoint = Input.mousePosition;
            if (Input.GetKey(KeyCode.B))
            {
                StatusText = "MOUSE SIMULATION - eyes closed (B held)";
                return true;
            }
            HasValidGaze = InBounds(ScreenPoint);
            StatusText = TobiiCrashedLastTime
                ? "MOUSE SIMULATION - Tobii crashed Unity last time (install Tobii Experience, see README)"
                : HasValidGaze ? "MOUSE SIMULATION - no eye tracker" : "Move pointer into the game";
            return false;
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        GuardTobiiStart();
        string notReady;
        if (!TobiiReady(out notReady))
        {
            StatusText = notReady;
            return false;
        }
        var sample = TobiiAPI.GetGazePoint();
        float age = Time.unscaledTime - sample.Timestamp;
        if (!sample.IsValid || age < 0f || age > maximumSampleAge)
        {
            StatusText = "Gaze lost or stale (eyes closed?) - paused";
            return true; // tracker connected, but no current gaze: candidate for a blink
        }
        // sample.Viewport/Screen use Unity's bottom-left origin. Do not invert Y.
        Vector2 viewport = sample.Viewport;
        DisplayScale = CurrentDisplayScale();
        if (compensateDisplayScaling && DisplayScale > 1.01f)
        {
            Rect bounds = TobiiHost.GetInstance().GameViewInfo.NormalizedClientAreaBounds;
            viewport = CompensateDisplayScaling(viewport, bounds, DisplayScale);
        }
        ScreenPoint = Smooth(new Vector2(viewport.x * Screen.width, viewport.y * Screen.height));
        HasValidGaze = InBounds(ScreenPoint);
        StatusText = HasValidGaze ? "Tobii gaze active" : "Gaze outside game - paused";
        return false;
#else
        StatusText = "The Tobii 4C requires Windows - paused";
        return false;
#endif
    }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    static bool TobiiReady(out string status)
    {
        status = null;
#if UNITY_EDITOR_WIN
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget != UnityEditor.BuildTarget.StandaloneWindows64 &&
            UnityEditor.EditorUserBuildSettings.activeBuildTarget != UnityEditor.BuildTarget.StandaloneWindows)
        {
            status = "Select Windows in File > Build Settings - paused";
            return false;
        }
#endif
        if (!Tobii.Gaming.Internal.TobiiEulaFile.IsEulaAccepted())
        {
            status = "Accept the Tobii SDK license in Unity - paused";
            return false;
        }
        if (!TobiiAPI.IsConnected)
        {
            status = "Tobii disconnected - paused";
            return false;
        }
        return true;
    }
#endif

    /// <summary>
    /// Marks a Tobii session as running before the native library is touched. The flag is only
    /// cleared when the session ends normally (leaving Play mode, switching to the mouse). If Unity
    /// crashes at any time while Tobii is in use, the flag is still set on the next start and the
    /// game falls back to mouse simulation instead of crashing again.
    /// </summary>
    void GuardTobiiStart()
    {
        if (tobiiArmedAt >= 0f) return;
        tobiiArmedAt = Time.unscaledTime;
        TobiiCrashedLastTime = false;
        PlayerPrefs.SetInt(PrefTobiiPending, 1);
        PlayerPrefs.Save();
    }

    /// <summary>Normal end of a Tobii session: no crash, remember Tobii if it was used for a while.</summary>
    void EndTobiiSession()
    {
        if (tobiiArmedAt < 0f) return;
        bool usedLongEnough = Time.unscaledTime - tobiiArmedAt >= TobiiConfirmSeconds;
        tobiiArmedAt = -1f;
        PlayerPrefs.SetInt(PrefTobiiPending, 0);
        if (usedLongEnough) PlayerPrefs.SetInt(PrefUseTobii, 1);
        PlayerPrefs.Save();
    }

    void OnDisable() { EndTobiiSession(); }

    /// <summary>Damps fixation jitter, but follows saccades (large jumps) immediately.</summary>
    Vector2 Smooth(Vector2 raw)
    {
        if (!hasSmoothedPoint || Vector2.Distance(raw, smoothedPoint) > saccadeThresholdPixels)
        {
            smoothedPoint = raw;
            hasSmoothedPoint = true;
            return raw;
        }
        // Frame-rate independent exponential smoothing (gazeSmoothing is the per-1/60 s retention).
        float keep = Mathf.Pow(Mathf.Clamp(gazeSmoothing, 0f, 0.95f), Time.unscaledDeltaTime * 60f);
        smoothedPoint = Vector2.Lerp(raw, smoothedPoint, keep);
        return smoothedPoint;
    }

    /// <summary>
    /// With Windows display scaling the Tobii runtime reports gaze positions inside the tracked
    /// window shrunk towards the window centre by the scaling factor (measured: looking at the
    /// top-left corner at 150 % lands about one third of the way to the centre). This undoes that:
    /// convert back to window-normalised coordinates, stretch around the window centre, and convert
    /// to Game-view viewport coordinates again (same bounds as Tobii's GazePointDataProvider).
    /// </summary>
    public static Vector2 CompensateDisplayScaling(Vector2 viewport, Rect bounds, float scale)
    {
        if (scale <= 0f || bounds.width < 1e-4f || bounds.height < 1e-4f ||
            float.IsNaN(bounds.x) || float.IsNaN(bounds.y)) return viewport;
        float bottom = 1f - bounds.height - bounds.y; // window-normalised, bottom-up
        float wx = bounds.x + viewport.x * bounds.width;
        float wy = bottom + viewport.y * bounds.height;
        wx = 0.5f + (wx - 0.5f) * scale;
        wy = 0.5f + (wy - 0.5f) * scale;
        return new Vector2((wx - bounds.x) / bounds.width, (wy - bottom) / bounds.height);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern System.IntPtr GetActiveWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetDpiForWindow(System.IntPtr hwnd);

    static float CurrentDisplayScale()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorGUIUtility.pixelsPerPoint;
#else
        try
        {
            uint dpi = GetDpiForWindow(GetActiveWindow());
            if (dpi > 0) return dpi / 96f;
        }
        catch (System.EntryPointNotFoundException) { }
        return Screen.dpi > 0 ? Screen.dpi / 96f : 1f;
#endif
    }

    void UpdateBlink(bool closedCandidate)
    {
        float now = Time.unscaledTime;
        if (closedCandidate && !HasValidGaze)
        {
            if (!eyesClosed) { eyesClosed = true; closedSince = now; }
            EyesClosedCandidate = true;
            EyesClosedSeconds = now - closedSince;
            return;
        }
        // Only a return to valid gaze ends a blink; tracker loss or focus loss discards it.
        if (eyesClosed && HasValidGaze)
        {
            BlinkEnded = true;
            LastBlinkSeconds = now - closedSince;
        }
        eyesClosed = false;
        EyesClosedCandidate = false;
        EyesClosedSeconds = 0f;
    }

    void UpdateHeadPose()
    {
        if (!trackHeadPose)
        {
            nod.Reset();
            HeadPoseAvailable = false;
            HeadStatus = "";
            return;
        }
        if (simulateMouse)
        {
            HeadPoseAvailable = true;
            HeadStatus = "Simulation: press N to nod";
            if (Application.isFocused && Input.GetKeyDown(KeyCode.N)) NodDetected = true;
            return;
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string notReady;
        if (!TobiiReady(out notReady))
        {
            nod.Reset();
            HeadPoseAvailable = false;
            HeadStatus = "Head pose unavailable";
            return;
        }
        HeadPose pose = TobiiAPI.GetHeadPose();
        float age = Time.unscaledTime - pose.Timestamp;
        if (!pose.IsValid || age < 0f || age > 0.25f)
        {
            nod.Reset();
            HeadPoseAvailable = false;
            HeadStatus = "Head pose not detected";
            return;
        }
        Vector3 euler = pose.Rotation.eulerAngles;
        NodDetected = nod.Update(euler.x, euler.y, Time.unscaledTime, Time.unscaledDeltaTime);
        HeadPoseAvailable = true;
        HeadStatus = "Head pitch " + nod.PitchDeviation.ToString("+0;-0") + " deg";
#else
        HeadPoseAvailable = false;
        HeadStatus = "Head pose needs the Tobii 4C on Windows";
#endif
    }

    static bool InBounds(Vector2 point)
    {
        return !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
               !float.IsInfinity(point.x) && !float.IsInfinity(point.y) &&
               point.x >= 0 && point.y >= 0 && point.x < Screen.width && point.y < Screen.height;
    }
}
