using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑器测试工具 —— 在Play模式下测试游戏流程
/// </summary>
public class PlayModeTestTools : EditorWindow
{
    [MenuItem("Test/Go To PartTimeJob")]
    static void GoToPartTimeJob()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.GoToPartTimeJob();
        else Debug.LogError("[TestTool] GameFlowManager不存在");
    }

    [MenuItem("Test/Go To MainUI")]
    static void GoToMainUI()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.GoToMainUI();
        else Debug.LogError("[TestTool] GameFlowManager不存在");
    }

    [MenuItem("Test/Go To MiniGames")]
    static void GoToMiniGames()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.ChangeState(GameState.MiniGame);
        else Debug.LogError("[TestTool] GameFlowManager不存在");
    }

    [MenuItem("Test/Go To Story")]
    static void GoToStory()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.ChangeState(GameState.Story);
        else Debug.LogError("[TestTool] GameFlowManager不存在");
    }

    [MenuItem("Test/Go To Album")]
    static void GoToAlbum()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.GoToAlbum();
        else Debug.LogError("[TestTool] GameFlowManager不存在");
    }

    [MenuItem("Test/Add 500 Currency")]
    static void AddCurrency()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCurrency(500);
            Debug.Log($"[TestTool] 旅费: {GameManager.Instance.TravelFee}");
        }
    }

    [MenuItem("Test/Unlock Spot 0")]
    static void UnlockSpot0()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameManager.Instance != null)
        {
            bool result = GameManager.Instance.UnlockSpot(0);
            Debug.Log($"[TestTool] 解锁景点0结果: {result}");
        }
    }

    [MenuItem("Test/Status Report")]
    static void StatusReport()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        if (GameManager.Instance != null)
        {
            var gm = GameManager.Instance;
            Debug.Log($"[TestTool] === 状态报告 ===");
            Debug.Log($"[TestTool] 旅费: {gm.TravelFee}");
            Debug.Log($"[TestTool] 当前状态: {GameFlowManager.Instance?.CurrentState}");
            Debug.Log($"[TestTool] 当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            for (int i = 0; i < 4; i++)
            {
                Debug.Log($"[TestTool] 景点{i} ({GameManager.SpotNames[i]}): 解锁={gm.IsSpotUnlocked(i)}, 探索={gm.IsSpotExplored(i)}");
            }
        }
    }

    [MenuItem("Test/Run Full Auto Test")]
    static void RunFullAutoTest()
    {
        if (!Application.isPlaying) { Debug.Log("[TestTool] 需要在Play模式下执行"); return; }
        
        // Create a temporary test runner
        var runner = new GameObject("AutoTestRunner").AddComponent<AutoTestRunner>();
    }
}
