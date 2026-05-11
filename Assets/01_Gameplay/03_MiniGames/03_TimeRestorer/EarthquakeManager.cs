using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 引用 UI

public class EarthquakeManager : MonoBehaviour
{
    [Header("核心对象")]
    public Rigidbody2D groundRb;
    public Transform ballTransform;
    public TextMeshProUGUI uiText;

    [Header("UI 面板 (请在Inspector拖入)")]
    public GameObject gameplayUI; // 搭建时的UI（包含提示按空格的文本）
    public GameObject winPanel;   // 胜利面板
    public GameObject losePanel;  // 失败面板
    public GameObject pausePanel; // 暂停面板
    public TextMeshProUGUI rewardText; // 胜利面板上的奖励文本

    [Header("场景跳转")]
    public string mainMenuSceneName = "01_MainUI";

    [Header("物理参数")]
    private float initialRequiredHeight = 250f;
    private float winHeight = 200f;
    private float maxHorizontalOffset = 150f;
    public float shakeAmplitudeX = 15f;
    public float shakeAmplitudeY = 5f;

    // --- 状态控制 ---
    private bool isQuaking = false;   // 是否正在震动
    private bool hasStarted = false;  // 是否已经开始测试
    private bool isPaused = false;    // 是否暂停

    void Awake()
    {
        Debug.unityLogger.logEnabled = true;
        Time.timeScale = 1f;

        // 初始化面板状态
        if (gameplayUI) gameplayUI.SetActive(true);
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
    }

    void Update()
    {
        // 1. 检测 ESC 暂停
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            //OnLevelClear(0);
        }

        // 如果暂停或已经在震动/结束，则不响应空格
        if (isPaused || isQuaking || hasStarted) return;

        // 2. 检测空格键：开始测试
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartTest();
        }
    }

    public void StartTest()
    {
        // 检查是否满足初始高度条件
        float currentHeight = ballTransform.position.y - groundRb.transform.position.y;
        if (currentHeight < initialRequiredHeight)
        {
            uiText.text = $"<color=red>高度不足！当前:{currentHeight:F0} / 需:{initialRequiredHeight}</color>";
            // 播放一个拒绝的音效或者动画（可选）
            return;
        }

        // --- 开始流程 ---
        hasStarted = true;
        if (gameplayUI) gameplayUI.SetActive(false); // 隐藏搭建UI
        uiText.text = "地震发生中(横波+纵波)...";

        // 锁定所有方块，赋予物理属性
        PhysicsMaterial2D frictionMat = new PhysicsMaterial2D();
        frictionMat.friction = 0.9f;
        frictionMat.bounciness = 0.0f;

        BlockInteract[] allBlocks = FindObjectsOfType<BlockInteract>();
        foreach (var block in allBlocks)
        {
            block.isPhysicsActive = true; // 禁止鼠标拖拽
            Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
            Collider2D col = block.GetComponent<Collider2D>();

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // 启用物理
                rb.gravityScale = 30f;
                rb.mass = 5f;
                rb.linearDamping = 1f;
                rb.angularDamping = 3f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                // ↓↓↓【新增这一行】解除拖拽阶段附加的旋转锁定，允许方块在地震时倾倒 ↓↓↓
                rb.freezeRotation = false;
            }
            if (col != null) col.sharedMaterial = frictionMat;
        }

        // 小球设置
        Rigidbody2D ballRb = ballTransform.GetComponent<Rigidbody2D>();
        if (ballRb != null)
        {
            ballRb.bodyType = RigidbodyType2D.Dynamic;
            ballRb.gravityScale = 30f;
            ballRb.linearDamping = 1f;
            ballRb.angularDamping = 2f;
            ballRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ballTransform.GetComponent<Collider2D>().sharedMaterial = frictionMat;
        }

        StartCoroutine(EarthquakeRoutine());
    }

    private IEnumerator EarthquakeRoutine()
    {
        isQuaking = true;
        float timer = 0f;
        float duration = 5f;

        Vector2 startPos = groundRb.position;
        float randomOffsetX = Random.Range(0f, 100f);
        float randomOffsetY = Random.Range(0f, 100f);

        while (timer < duration)
        {
            if (!isPaused) // 暂停时不计时，也不震动
            {
                timer += Time.deltaTime;
                float noiseX = (Mathf.PerlinNoise(timer * 25f, randomOffsetX) - 0.5f) * 2f;
                float noiseY = (Mathf.PerlinNoise(timer * 25f, randomOffsetY) - 0.5f) * 2f;
                Vector2 targetPos = startPos + new Vector2(noiseX * shakeAmplitudeX, noiseY * shakeAmplitudeY);
                groundRb.MovePosition(targetPos);
            }
            yield return new WaitForFixedUpdate();
        }

        groundRb.MovePosition(startPos);
        isQuaking = false;
        uiText.text = "等待物理静止...";

        // 等待倒塌完全停止
        yield return new WaitForSeconds(3f);
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        float finalHeight = ballTransform.position.y - groundRb.transform.position.y;
        float offsetX = Mathf.Abs(ballTransform.position.x - groundRb.transform.position.x);
        Rigidbody2D ballRb = ballTransform.GetComponent<Rigidbody2D>();

        // 判定条件：高度足够 + 没有偏离中心太远 + 速度接近静止
        bool isStable = ballRb != null && ballRb.linearVelocity.magnitude < 15f;
        bool isHeightEnough = finalHeight >= winHeight;
        bool isCentered = offsetX <= maxHorizontalOffset;

        if (isHeightEnough && isCentered && isStable)
        {
            OnLevelClear(finalHeight);
        }
        else
        {
            string failReason = "";
            if (!isHeightEnough) failReason = $"高度过低 ({finalHeight:F0}/{winHeight})";
            else if (!isCentered) failReason = "小球滚落/偏离中心";
            else if (!isStable) failReason = "结构不稳定";

            OnLevelFail(failReason);
        }
    }
    private void OnLevelClear(float score)
    {
        Debug.Log("Victory!");
        uiText.text = ""; // 清空顶部提示
        if (winPanel) winPanel.SetActive(true);

        // 计算奖励：基础 50 + 高度奖励
        int reward = 50 + Mathf.FloorToInt(score * 0.5f);
        if (rewardText) rewardText.text = $"建筑牢固！\n获得旅费: ${reward}";

        // 保存数据
        if (SaveManager.Instance != null)
        {
            GameData gameData = SaveManager.Instance.CurrentGameData;
            gameData.money += reward;
            var bankHistory = gameData.GetOrCreateInfo("System_Bank");
            bankHistory.chatLog.Add(new ChatMessage { sender = SenderType.System, type = MessageType.SystemAlert, content = $"【搭建积木】到账 ${reward}。", timeStamp = "Now" });
            bankHistory.hasUnread = true;
            SaveManager.Instance.SaveCurrentGame();
        }
    }

    // --- 失败结算 ---
    private void OnLevelFail(string reason)
    {
        Debug.Log("Failed: " + reason);
        uiText.text = "";
        if (losePanel)
        {
            losePanel.SetActive(true);
            // 可以在失败面板上找个 Text 组件显示 reason，这里简单打印
            TextMeshProUGUI failText = losePanel.GetComponentInChildren<TextMeshProUGUI>();
            if (failText) failText.text = $"挑战失败\n{reason}";
        }
    }

    // --- 暂停系统 ---
    public void TogglePause()
    {
        // 如果已经结算了，ESC不起作用
        if (winPanel.activeSelf || losePanel.activeSelf) return;

        isPaused = !isPaused;
        if (pausePanel) pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // --- UI 按钮绑定方法 (绑定到 Button OnClick) ---

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {
            bool isReplay = PlayerPrefs.GetInt("IsReplayMode", 0) == 1;
            if (!isReplay)
            {
                SaveManager.Instance.CurrentGameData.currentChapter = 4;
                SaveManager.Instance.CurrentGameData.chapterSubState = 0;
                SaveManager.Instance.SaveCurrentGame();
            }
            // 清理 Replay 标记
            PlayerPrefs.DeleteKey("IsReplayMode");
            // 返回主界面
            TransitionManager.Instance.SwitchScene("01_MainUI");
            //SceneManager.LoadScene("01_MainUI");
        }
    }
}