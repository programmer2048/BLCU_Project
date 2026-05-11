using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        M3_GameManager.Instance.OnScoreChanged += UpdateScore;
        M3_GameManager.Instance.OnHealthChanged += UpdateHealth;
        M3_GameManager.Instance.OnGameOver += ShowGameOver;

        // 隐藏结束面板
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (M3_GameManager.Instance != null)
        {
            M3_GameManager.Instance.OnScoreChanged -= UpdateScore;
            M3_GameManager.Instance.OnHealthChanged -= UpdateHealth;
            M3_GameManager.Instance.OnGameOver -= ShowGameOver;
        }
    }

    void Update()
    {
        if (M3_GameManager.Instance.state == M3_GameState.Playing)
        {
            UpdateTimer(M3_GameManager.Instance.currentLevelTime);
        }
    }

    // --- 回调函数 ---

    void UpdateScore(int newScore)
    {
        scoreText.text = $"￥{newScore}";
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