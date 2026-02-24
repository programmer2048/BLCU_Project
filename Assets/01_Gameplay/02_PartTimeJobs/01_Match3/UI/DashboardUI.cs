using UnityEngine;
using UnityEngine.UI;
using TMPro; // 一定要引用这个，现代Unity都用TMP

public class DashboardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public Slider healthSlider;
    public TextMeshProUGUI timerText;
    public GameObject gameOverPanel; // 游戏结束弹窗

    void Start()
    {
        // 1. 初始化显示
        UpdateScore(0);

        // 设置血条最大值
        healthSlider.maxValue = M3_GameManager.Instance.maxHealth;
        healthSlider.value = M3_GameManager.Instance.maxHealth;

        // 2. 订阅事件 (核心！)
        // 当 M3_GameManager 说 "分数变了"，我们就执行 UpdateScore
        M3_GameManager.Instance.OnScoreChanged += UpdateScore;
        M3_GameManager.Instance.OnHealthChanged += UpdateHealth;
        M3_GameManager.Instance.OnGameOver += ShowGameOver;

        // 隐藏结束面板
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void OnDestroy()
    {
        // 3. 取消订阅 (非常重要！防止报错和内存泄漏)
        if (M3_GameManager.Instance != null)
        {
            M3_GameManager.Instance.OnScoreChanged -= UpdateScore;
            M3_GameManager.Instance.OnHealthChanged -= UpdateHealth;
            M3_GameManager.Instance.OnGameOver -= ShowGameOver;
        }
    }

    void Update()
    {
        // 倒计时通常每帧更新，不需要事件系统
        if (M3_GameManager.Instance.state == M3_GameState.Playing)
        {
            UpdateTimer(M3_GameManager.Instance.currentLevelTime);
        }
    }

    // --- 回调函数 ---

    void UpdateScore(int newScore)
    {
        scoreText.text = $"${newScore}";
    }

    void UpdateHealth(int newHealth)
    {
        healthSlider.value = newHealth;
    }

    void UpdateTimer(float time)
    {
        // 把秒数格式化为 00:00
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }
}