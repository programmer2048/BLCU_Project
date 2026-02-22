using UnityEditor;
using UnityEngine;

public class FixBuildSettings
{
    [MenuItem("Tools/Fix Build Settings")]
    public static void Fix()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/04_Scenes/00_Boot.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/01_MainUI.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/02_PartTimeJobs.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/03_MiniGames.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/04_StoryScenes.unity", true),
            new EditorBuildSettingsScene("Assets/04_Scenes/05_Album.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        
        // 验证
        Debug.Log($"[FixBuildSettings] 当前 Build Settings 场景数: {EditorBuildSettings.scenes.Length}");
        foreach (var s in EditorBuildSettings.scenes)
        {
            Debug.Log($"  [{(s.enabled ? "✓" : "✗")}] {s.path}");
        }
    }
}
