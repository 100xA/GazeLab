// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Tobii.Gaming;
#endif

/// <summary>
/// Gaze input from the Tobii 4C (Tobii Unity SDK) or a clearly labelled mouse simulation,
/// plus the two non-gaze confirmation signals used by the shooting modes: deliberate
/// blinks and head nods.
/// </summary>
public class GazeInput : MonoBehaviour
{
    // The Tobii native library can crash the Unity Editor when no tracker/runtime is present.
    // It is therefore only loaded after the player explicitly selects "Tobii eye tracker".
    public bool simulateMouse = true;
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

    void Awake()
    {
        StatusText = "Waiting for gaze";
        HeadStatus = "";
        lastSimulation = simulateMouse;
        if (GetComponent<GazeHud>() == null) gameObject.AddComponent<GazeHud>();
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
            StatusText = HasValidGaze ? "MOUSE SIMULATION - no eye tracker" : "Move pointer into the game";
            return false;
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
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
        ScreenPoint = sample.Screen; // Already Unity bottom-left coordinates. Do not invert Y.
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
