using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RemoveMissingScriptsInScenes
{
    [MenuItem("Tools/Fix/Remove Missing Scripts In Build Scenes")]
    public static void RemoveInBuildScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        int totalRemoved = 0;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (!scenes[i].enabled) continue;

            var scene = EditorSceneManager.OpenScene(scenes[i].path, OpenSceneMode.Single);
            int removedInScene = RemoveMissingInScene(scene);
            totalRemoved += removedInScene;

            if (removedInScene > 0)
                EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[Fix] Missing scripts removed. Total: {totalRemoved}");
    }

    private static int RemoveMissingInScene(Scene scene)
    {
        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            removed += RemoveMissingInHierarchy(root);
        }
        return removed;
    }

    private static int RemoveMissingInHierarchy(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
        {
            removed += RemoveMissingInHierarchy(child.gameObject);
        }
        return removed;
    }
}
