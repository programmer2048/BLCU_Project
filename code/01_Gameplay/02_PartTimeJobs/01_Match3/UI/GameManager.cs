using UnityEngine;
using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class M3_GameManager : MonoBehaviour
{
    public static M3_GameManager Instance { get; private set; }

    private string mainMenuSceneName = "01_MainUI";

    [Header("Game Settings")]
    public int maxHealth = 100;        // 初始耐心值
    public float levelDuration = 120f; // 关卡最大时长（秒）

    [Header("Runtime Data (Read Only)")]
    public float currentLevelTime = 0f;
    public int currentScore = 0;
    public int currentHealth = 0;
    public M3_GameState state;
    public RectTransform revenueIconTransform;
    public RectTransform patienceHintText;

    // --- 事件系统 ---
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHealthChanged;
    public event Action OnGameOver;
    public event Action OnGameStarted;
    public event Action<bool> OnGamePaused;

    public AudioClip bgmClip;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        Time.timeScale = 1f;

        // 强制开启日志
        Debug.unityLogger.logEnabled = true;
        // 确保过滤器不过滤任何类型
        Debug.unityLogger.filterLogType = LogType.Log;
    }

    void Start()
    {
        StartGame();
        BGMManager.Instance.bgm.clip = bgmClip;
        BGMManager.Instance.bgm.Play();
    }

    void Update()
    {
        // 只有在游戏进行中才计时
        if (state == M3_GameState.Playing)
        {
            // 正计时逻辑：时间累加
            currentLevelTime += Time.deltaTime;
            /*
            (可选) 如果达到了关卡限时，游戏结束
            if (currentLevelTime >= levelDuration)
            {
                currentLevelTime = levelDuration; // 修正最后一帧的溢出
                EndGame();
            }
            */
        }
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            Debug.Log("ESC");
            if (state == M3_GameState.Playing) TogglePause(true);
            else if (state == M3_GameState.Paused) TogglePause(false);
        }
        Debug.Log(M3_GameManager.Instance.currentHealth);
    }

    public Vector3 GetRevenueUIPosition()
    {
        if (revenueIconTransform != null) return revenueIconTransform.position;
        return Vector3.zero; // fallback
    }

    public void StartGame()
    {
        // 1. 重置数值
        currentScore = 0;
        currentHealth = maxHealth;
        currentLevelTime = 0f; // 时间归零

        // 2. 切换状态
        state = M3_GameState.Playing;

        // 3. 通知所有监听者（UI、订单管理器等）
        OnScoreChanged?.Invoke(currentScore);
        OnHealthChanged?.Invoke(currentHealth);
        OnGameStarted?.Invoke(); // 通知大家游戏重新开始了
        
        Debug.Log("Game Started!");
    }
    public void TogglePause(bool isPaused)
    {
        if (state == M3_GameState.GameOver) return;
        if (isPaused)
        {
            state = M3_GameState.Paused;
            Time.timeScale = 0f; // 冻结游戏时间（TrayController 和 动画都会停）
        }
        else
        {
            state = M3_GameState.Playing;
            Time.timeScale = 1f; // 恢复时间
        }
        OnGamePaused?.Invoke(isPaused);
    }
    public void RetryLevel()
    {
        Time.timeScale = 1f; // 必须恢复时间，否则新场景也是停的
        addRevenue();
        //StartGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        addRevenue();
        BGMManager.Instance.bgm.Stop();
        SceneManager.LoadScene(mainMenuSceneName);
    }
    public void AddScore(int amount)
    {
        if (state != M3_GameState.Playing) return;

        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

    public void ModifyHealth(int amount)
    {
        if (state != M3_GameState.Playing) return;

        currentHealth += amount;

        // 限制血量在 0 到 maxHealth 之间
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        if (state == M3_GameState.GameOver) return; // 防止重复调用

        state = M3_GameState.GameOver;
        Debug.Log($"Game Over! Final Score: {currentScore}, Time: {currentLevelTime:F2}s");

        OnGameOver?.Invoke();
    }
    public string GetFormattedTime()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(currentLevelTime);
        return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

    public void addRevenue() // 对旅费的增加逻辑
    {
        GameData gameData = SaveManager.Instance.CurrentGameData;
        int amount = this.currentScore;
        gameData.money += amount;
        var bankHistory = gameData.GetOrCreateInfo("System_Bank");
        bankHistory.chatLog.Add(new ChatMessage { sender = SenderType.System, type = MessageType.SystemAlert, content = $"【拾光餐厅】到账 ${amount}。", timeStamp = "Now" });
        bankHistory.hasUnread = true;
        //RefreshContactList();
        SaveManager.Instance.SaveCurrentGame();
    }
}