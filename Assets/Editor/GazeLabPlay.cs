using System;
using UnityEditor;
using UnityEditor.SceneManagement;

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
        EditorApplication.delayCall += FocusGameView;
    }

    static void FocusGameView()
    {
        Type gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameView != null) EditorWindow.FocusWindowIfItsOpen(gameView);
    }
}
