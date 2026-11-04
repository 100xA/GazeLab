using System;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Opens the Main scene and enters Play mode, and gives the Game view keyboard focus whenever
/// Play mode starts (otherwise Space/B/N only work after clicking into the Game view once).
/// Run: Unity.exe -projectPath &lt;project&gt; -executeMethod GazeLabPlay.Run
/// </summary>
[InitializeOnLoad]
public static class GazeLabPlay
{
    static GazeLabPlay()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        FocusGameView();
        EditorApplication.delayCall += FocusGameView; // again once the editor layout has settled
    }

    static void FocusGameView()
    {
        Type gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameView != null) EditorWindow.FocusWindowIfItsOpen(gameView);
    }
}
