// Student: Jan Biernacki | Matriculation number: TO BE PROVIDED
using UnityEngine;
#if GAZELAB_TOBII && (UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR))
using Tobii.Gaming;
#endif

/// <summary>Real gaze is the default; simulation is explicitly selected and labelled.</summary>
public class GazeInput : MonoBehaviour
{
    public enum GazeSource { TobiiSdk, Talon }
#if UNITY_EDITOR_OSX || (UNITY_STANDALONE_OSX && !UNITY_EDITOR)
    public GazeSource source = GazeSource.Talon;
#else
    public GazeSource source = GazeSource.TobiiSdk;
#endif
    public bool simulateMouse;
    TalonGazeReceiver talon;
    int lastInputMode = -1;
    public bool IsTestData { get { return !simulateMouse && source == GazeSource.Talon && talon != null && talon.IsTestData; } }
    [Range(0.02f, 0.5f)] public float maximumSampleAge = 0.1f;
    public bool HasValidGaze { get; private set; }
    public Vector2 ScreenPoint { get; private set; }
    public string StatusText { get; private set; }
    public bool IsSimulation { get { return simulateMouse; } }
    public bool IsOverControls { get { return GazeHud.ControlsContain(ScreenPoint); } }

    void Awake()
    {
        StatusText = "Waiting for gaze";
        talon = GetComponent<TalonGazeReceiver>();
        if (talon == null) talon = gameObject.AddComponent<TalonGazeReceiver>();
        talon.enabled = source == GazeSource.Talon && !simulateMouse;
        if (GetComponent<GazeHud>() == null) gameObject.AddComponent<GazeHud>();
    }

    public void SetSimulation(bool enabled)
    {
        simulateMouse = enabled;
        HasValidGaze = false;
        CancelSelection();
        if (talon != null) talon.enabled = !enabled && source == GazeSource.Talon;
    }

    public void SelectSource(GazeSource selected)
    {
        source = selected;
        simulateMouse = false;
        HasValidGaze = false;
        CancelSelection();
        if (talon != null) talon.enabled = selected == GazeSource.Talon;
    }

    public void SetTalonViewportBounds(Vector2 topLeft, Vector2 bottomRight)
    {
        if (talon != null) talon.SetViewportBounds(topLeft, bottomRight);
    }

    void CancelSelection()
    {
        GameController game = GetComponent<GameController>();
        if (game != null) game.CancelSelection();
    }

    void Update()
    {
        HasValidGaze = false;
        int mode = simulateMouse ? 2 : (int)source;
        if (mode != lastInputMode) { CancelSelection(); lastInputMode = mode; }
        if (talon != null)
        {
            talon.maximumSampleAge = maximumSampleAge;
            talon.enabled = !simulateMouse && source == GazeSource.Talon;
        }
        if (!Application.isFocused)
        {
            StatusText = "Game window inactive - paused";
            return;
        }
        if (simulateMouse)
        {
            ScreenPoint = Input.mousePosition;
            HasValidGaze = InBounds(ScreenPoint);
            StatusText = HasValidGaze ? "MOUSE SIMULATION - no eye tracker" : "Move pointer into the game";
            return;
        }
        if (source == GazeSource.Talon)
        {
            Vector2 point = Vector2.zero;
            HasValidGaze = talon != null && talon.TryGetScreenPoint(out point);
            if (HasValidGaze) ScreenPoint = point;
            StatusText = talon == null ? "Talon receiver unavailable" : talon.StatusText;
            return;
        }
#if GAZELAB_TOBII && (UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR))
#if UNITY_EDITOR_WIN
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget != UnityEditor.BuildTarget.StandaloneWindows64 &&
            UnityEditor.EditorUserBuildSettings.activeBuildTarget != UnityEditor.BuildTarget.StandaloneWindows)
        {
            StatusText = "Select Windows in File > Build Settings - paused";
            return;
        }
#endif
        if (!Tobii.Gaming.Internal.TobiiEulaFile.IsEulaAccepted())
        {
            StatusText = "Accept the Tobii SDK license in Unity - paused";
            return;
        }
        if (!TobiiAPI.IsConnected)
        {
            StatusText = "Tobii disconnected - paused";
            return;
        }
        var sample = TobiiAPI.GetGazePoint();
        float age = Time.unscaledTime - sample.Timestamp;
        if (!sample.IsValid || age < 0f || age > maximumSampleAge)
        {
            StatusText = "Gaze lost or stale - paused";
            return;
        }
        ScreenPoint = sample.Screen; // Already Unity bottom-left coordinates. Do not invert Y.
        HasValidGaze = InBounds(ScreenPoint);
        StatusText = HasValidGaze ? "Tobii gaze active" : "Gaze outside game - paused";
#elif GAZELAB_TOBII
        StatusText = "Tobii 4C requires the Windows lab PC - paused";
#else
        StatusText = "Import course SDK, then Tools > Gaze Lab > Enable Tobii SDK";
#endif
    }

    static bool InBounds(Vector2 point)
    {
        return !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
               !float.IsInfinity(point.x) && !float.IsInfinity(point.y) &&
               point.x >= 0 && point.y >= 0 && point.x < Screen.width && point.y < Screen.height;
    }
}
