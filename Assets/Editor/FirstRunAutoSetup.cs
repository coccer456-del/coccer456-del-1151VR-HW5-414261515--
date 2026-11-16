using UnityEditor;
using UnityEngine;

/// <summary>
/// On the first successful Unity import, create the playable scene automatically.
/// You can always rebuild manually via Tools > Pokemon Capture > 1 - Build and Open Scene.
/// </summary>
[InitializeOnLoad]
public static class FirstRunAutoSetup
{
    private const string ScenePath = "Assets/Scenes/CaptureDemo.unity";
    private const string SessionFlag = "PokemonCapture3D_FirstRunBuildInProgress";

    static FirstRunAutoSetup()
    {
        EditorApplication.delayCall += TryBuildScene;
    }

    private static void TryBuildScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryBuildScene;
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        if (SessionState.GetBool(SessionFlag, false)) return;

        string[] requiredModels = {
            "Assets/Models/Trainer.obj",
            "Assets/Models/TrainerThrowArm.obj",
            "Assets/Models/ElectricBuddy.obj",
            "Assets/Models/PokeBall.obj"
        };

        foreach (string path in requiredModels)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning("[Pokemon Capture] Model not imported yet: " + path +
                    ". After import finishes, use Tools > Pokemon Capture > 1 - Build and Open Scene.");
                return;
            }
        }

        SessionState.SetBool(SessionFlag, true);
        try
        {
            BuildCaptureScene.BuildAndOpen();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Pokemon Capture] Automatic scene build failed. " +
                "Use Tools > Pokemon Capture > 1 - Build and Open Scene after checking Console.\n" + ex);
        }
        finally
        {
            SessionState.EraseBool(SessionFlag);
        }
    }
}
