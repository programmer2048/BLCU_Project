using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class M3_UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Slider healthSlider;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    // --- 重点：直接声明按钮引用 ---
    [Header("Buttons (Assign in Inspector)")]
    public Button pauseHUDButton;     // 屏幕右上角的暂停按钮
    public Button resumeBtn;          // 暂停面板里的继续按钮
    public Button restartBtnPause;    // 暂停面板里的重开按钮
    public Button restartBtnGameOver; // 结算面板里的重开按钮
    public Button menuBtnPause;       // 暂停面板里的主页按钮
    public Button menuBtnGameOver;    // 结算面板里的主页按钮

    void Start()
    {
        // 1. 初始化数据
        InitHUD();

        // 2. 隐藏面板
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // 3. --- 代码绑定点击事件 (核心修改) ---
        BindButtonEvents();

        // 4. 注册 GameManager 事件
        RegisterGMEvents();
    }

    private void BindButtonEvents()
    {
        // 使用 AddListener 的好处：逻辑清晰，不用在 Unity 编辑器里到处点

        if (pauseHUDButton)
            pauseHUDButton.onClick.AddListener(() => M3_GameManager.Instance.TogglePause(true));

        if (resumeBtn)
            resumeBtn.onClick.AddListener(() => M3_GameManager.Instance.TogglePause(false));

        if (restartBtnPause)
            restartBtnPause.onClick.AddListener(() => M3_GameManager.Instance.RetryLevel());

        if (restartBtnGameOver)
            restartBtnGameOver.onClick.AddListener(() => M3_GameManager.Instance.RetryLevel());

        if (menuBtnPause)
            menuBtnPause.onClick.AddListener(() => M3_GameManager.Instance.ReturnToMenu());

        if (menuBtnGameOver)
            menuBtnGameOver.onClick.AddListener(() => M3_GameManager.Instance.ReturnToMenu());
    }

    private void RegisterGMEvents()
    {
        var gm = M3_GameManager.Instance;
        if (gm != null)
        {
            gm.OnScoreChanged += UpdateScore;
            gm.OnHealthChanged += UpdateHealth;
            gm.OnGamePaused += OnPauseStateChanged;
            gm.OnGameOver += ShowGameOver;
        }
    }

    // --- 剩下的逻辑保持不变 ---

    private void InitHUD()
    {
        if (scoreText) scoreText.text = "$0";
        if (healthSlider)
        {
            healthSlider.maxValue = M3_GameManager.Instance.maxHealth;
            healthSlider.value = M3_GameManager.Instance.maxHealth;
        }
    }

    void OnDestroy()
    {
        // 记得清理 GameManager 事件
        if (M3_GameManager.Instance != null)
        {
            M3_GameManager.Instance.OnScoreChanged -= UpdateScore;
            M3_GameManager.Instance.OnHealthChanged -= UpdateHealth;
            M3_GameManager.Instance.OnGamePaused -= OnPauseStateChanged;
            M3_GameManager.Instance.OnGameOver -= ShowGameOver;
        }

        // Button 的 AddListener 会随物体销毁自动清理，通常不需要手动 RemoveAllListeners
    }

    void Update()
    {
        if (M3_GameManager.Instance.state == M3_GameState.Playing)
        {
            UpdateTimer(M3_GameManager.Instance.currentLevelTime);
        }
    }

    void UpdateScore(int newScore) => scoreText.text = $"${newScore}";
    void UpdateHealth(int newHealth) => healthSlider.value = newHealth;

    void UpdateTimer(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void OnPauseStateChanged(bool isPaused)
    {
        if (pausePanel) pausePanel.SetActive(isPaused);
    }

    void ShowGameOver()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }
}