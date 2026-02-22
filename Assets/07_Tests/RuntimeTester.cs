using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 运行时测试器 —— 自动测试游戏流程
/// </summary>
public class RuntimeTester : MonoBehaviour
{
    [SerializeField] private bool enableHotkeys = false;
    [SerializeField] private bool enableAutoTestOnMainUILoad = false;
    [SerializeField] private bool verboseLog = false;
    private bool _autoTestStarted = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (verboseLog)
            Debug.Log($"[TEST] 场景加载完成: {scene.name} (buildIndex={scene.buildIndex})");
        if (enableAutoTestOnMainUILoad && scene.name == "01_MainUI" && !_autoTestStarted)
        {
            _autoTestStarted = true;
            Debug.Log("[TEST] MainUI场景已加载，启动自动测试...");
            StartCoroutine(AutoTestCoroutine());
        }
    }

    void Update()
    {
        if (!enableHotkeys) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[TEST] F1: 前往打工场景");
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.GoToPartTimeJob();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[TEST] F2: 返回主界面");
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.GoToMainUI();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[TEST] F3: 前往小游戏场景");
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.ChangeState(GameState.MiniGame);
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("[TEST] F4: 前往故事场景");
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.ChangeState(GameState.Story);
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[TEST] F5: 前往相簿场景");
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.GoToAlbum();
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log("[TEST] F6: 增加100旅费");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCurrency(100);
                Debug.Log($"[TEST] 当前旅费: {GameManager.Instance.TravelFee}");
            }
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log("[TEST] F7: 解锁景点0 (应县木塔)");
            if (GameManager.Instance != null)
            {
                bool result = GameManager.Instance.UnlockSpot(0);
                Debug.Log($"[TEST] 解锁结果: {result}, 旅费剩余: {GameManager.Instance.TravelFee}");
            }
        }
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.Log("[TEST] F8: 探索景点0");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSpotExplored(0);
                Debug.Log("[TEST] 景点0已探索");
            }
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("[TEST] F9: 状态报告");
            if (GameManager.Instance != null)
            {
                var gm = GameManager.Instance;
                Debug.Log($"[TEST] 旅费: {gm.TravelFee}");
                for (int i = 0; i < 4; i++)
                {
                    Debug.Log($"[TEST] 景点{i} ({GameManager.SpotNames[i]}): 解锁={gm.IsSpotUnlocked(i)}, 探索={gm.IsSpotExplored(i)}");
                }
            }
            if (GameFlowManager.Instance != null)
            {
                Debug.Log($"[TEST] 当前状态: {GameFlowManager.Instance.CurrentState}");
            }
        }
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Debug.Log("[TEST] F10: 自动测试全流程");
            StartCoroutine(AutoTestCoroutine());
        }
    }

    System.Collections.IEnumerator AutoTestCoroutine()
    {
        Debug.Log("=== [AUTO TEST] 开始自动测试 ===");

        // 1. 检查初始状态
        Debug.Log("[AUTO TEST] Step 1: 检查初始状态");
        Debug.Log($"  当前状态: {GameFlowManager.Instance?.CurrentState}");
        Debug.Log($"  旅费: {GameManager.Instance?.TravelFee}");
        yield return new WaitForSeconds(0.5f);

        // 2. 增加旅费
        Debug.Log("[AUTO TEST] Step 2: 增加500旅费");
        GameManager.Instance?.AddCurrency(500);
        Debug.Log($"  旅费: {GameManager.Instance?.TravelFee}");
        yield return new WaitForSeconds(0.5f);

        // 3. 前往打工场景
        Debug.Log("[AUTO TEST] Step 3: 前往打工场景");
        GameFlowManager.Instance?.GoToPartTimeJob();
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 4. 返回主界面
        Debug.Log("[AUTO TEST] Step 4: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 5. 解锁景点0
        Debug.Log("[AUTO TEST] Step 5: 解锁景点0");
        bool unlocked = GameManager.Instance?.UnlockSpot(0) ?? false;
        Debug.Log($"  解锁结果: {unlocked}, 剩余旅费: {GameManager.Instance?.TravelFee}");
        yield return new WaitForSeconds(0.5f);

        // 6. 前往故事场景
        Debug.Log("[AUTO TEST] Step 6: 前往故事场景");
        GameFlowManager.Instance?.ChangeState(GameState.Story);
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 7. 返回主界面
        Debug.Log("[AUTO TEST] Step 7: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 8. 前往小游戏场景
        Debug.Log("[AUTO TEST] Step 8: 前往小游戏场景");
        GameFlowManager.Instance?.ChangeState(GameState.MiniGame);
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 9. 返回主界面
        Debug.Log("[AUTO TEST] Step 9: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 10. 前往相簿场景
        Debug.Log("[AUTO TEST] Step 10: 前往相簿场景");
        GameFlowManager.Instance?.GoToAlbum();
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 11. 返回主界面
        Debug.Log("[AUTO TEST] Step 11: 返回主界面");
        GameFlowManager.Instance?.GoToMainUI();
        yield return new WaitForSeconds(2f);
        Debug.Log($"  当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        Debug.Log("=== [AUTO TEST] 自动测试完成 ===");
    }
}
