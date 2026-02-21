using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 自动测试运行器 —— 全流程游戏测试
/// </summary>
public class AutoTestRunner : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Wait for MainUI to be loaded before starting tests
        StartCoroutine(WaitForMainUIThenTest());
    }

    System.Collections.IEnumerator WaitForMainUIThenTest()
    {
        Debug.Log("[AUTO TEST] 等待进入MainUI场景...");
        while (SceneManager.GetActiveScene().name != "01_MainUI")
        {
            yield return new WaitForSeconds(0.5f);
        }
        Debug.Log("[AUTO TEST] 已进入MainUI，开始测试...");
        yield return new WaitForSeconds(1f);
        StartCoroutine(RunTest());
    }

    System.Collections.IEnumerator RunTest()
    {
        Debug.Log("=== [AUTO TEST] 全流程自动测试开始 ===");

        // Step 1: Check initial state
        Debug.Log("[AUTO TEST] Step 1: 初始状态");
        Debug.Log($"  场景: {SceneManager.GetActiveScene().name}");
        Debug.Log($"  状态: {GameFlowManager.Instance?.CurrentState}");
        Debug.Log($"  旅费: {GameManager.Instance?.TravelFee}");
        yield return new WaitForSeconds(1f);

        // Step 2: Go to PartTimeJob
        Debug.Log("[AUTO TEST] Step 2: 前往打工场景");
        GameFlowManager.Instance?.GoToPartTimeJob();
        yield return new WaitForSeconds(2f);
        LogScene("Step 2 result");

        // Step 3: Back to MainUI
        Debug.Log("[AUTO TEST] Step 3: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        LogScene("Step 3 result");

        // Step 4: Add currency
        Debug.Log("[AUTO TEST] Step 4: 增加500旅费");
        GameManager.Instance?.AddCurrency(500);
        Debug.Log($"  旅费: {GameManager.Instance?.TravelFee}");
        yield return new WaitForSeconds(0.5f);

        // Step 5: Unlock spot 0
        Debug.Log("[AUTO TEST] Step 5: 解锁景点0");
        bool unlocked = GameManager.Instance?.UnlockSpot(0) ?? false;
        Debug.Log($"  解锁结果: {unlocked}, 剩余旅费: {GameManager.Instance?.TravelFee}");
        yield return new WaitForSeconds(0.5f);

        // Step 6: Go to Story
        Debug.Log("[AUTO TEST] Step 6: 前往故事场景");
        GameFlowManager.Instance?.ChangeState(GameState.Story);
        yield return new WaitForSeconds(2f);
        LogScene("Step 6 result");

        // Step 7: Back to MainUI
        Debug.Log("[AUTO TEST] Step 7: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        LogScene("Step 7 result");

        // Step 8: Go to MiniGames
        Debug.Log("[AUTO TEST] Step 8: 前往小游戏场景");
        GameFlowManager.Instance?.ChangeState(GameState.MiniGame);
        yield return new WaitForSeconds(2f);
        LogScene("Step 8 result");

        // Step 9: Back to MainUI
        Debug.Log("[AUTO TEST] Step 9: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        LogScene("Step 9 result");

        // Step 10: Go to Album
        Debug.Log("[AUTO TEST] Step 10: 前往相簿场景");
        GameFlowManager.Instance?.GoToAlbum();
        yield return new WaitForSeconds(2f);
        LogScene("Step 10 result");

        // Step 11: Back to MainUI
        Debug.Log("[AUTO TEST] Step 11: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        LogScene("Step 11 result");

        Debug.Log("=== [AUTO TEST] 全流程自动测试完成 ===");

        // Write results to file
        string results = $"Auto Test Complete\n" +
            $"Final Scene: {SceneManager.GetActiveScene().name}\n" +
            $"Final State: {GameFlowManager.Instance?.CurrentState}\n" +
            $"Travel Fee: {GameManager.Instance?.TravelFee}\n" +
            $"Spot 0 Unlocked: {GameManager.Instance?.IsSpotUnlocked(0)}\n";
        System.IO.File.WriteAllText("F:/Unity/UnityProjects/BLCU_Project/test_results.txt", results);
        Debug.Log("[AUTO TEST] 结果已保存到 test_results.txt");

        Destroy(gameObject);
    }

    void LogScene(string label)
    {
        Debug.Log($"[AUTO TEST] {label}: 场景={SceneManager.GetActiveScene().name}");
    }
}
