// Student: Jan Biernacki | Matriculation number: TO BE PROVIDED
using System.Linq;
using UnityEditor;

public static class GazeLabSetup
{
    [MenuItem("Tools/Gaze Lab/Enable Tobii SDK")]
    public static void EnableTobii()
    {
        bool found = System.AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType("Tobii.Gaming.TobiiAPI") != null);
        if (!found)
        {
            EditorUtility.DisplayDialog("Import SDK first", "Import the course tobii-unity-sdk-4.0.5.unitypackage, then run this command again.", "OK");
            return;
        }
        const string symbol = "GAZELAB_TOBII";
        var group = BuildTargetGroup.Standalone;
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (!symbols.Contains(symbol)) symbols.Add(symbol);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols.ToArray()));
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("Tobii SDK enabled. Use a Windows PC and calibrated tracker; real gaze remains the default input.");
    }
}
