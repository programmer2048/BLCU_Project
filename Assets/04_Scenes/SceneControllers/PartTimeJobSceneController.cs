using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 打工场景控制器 —— 切换三消/音游
/// </summary>
public class PartTimeJobSceneController : MonoBehaviour
{
    [Header("选择面板")]
    public GameObject selectionPanel;
    public Button match3Button;
    public Button rhythmButton;
    public Button backButton;

    [Header("游戏面板")]
    public GameObject match3Panel;
    public GameObject rhythmPanel;

    //[Header("控制器")]
    //public Match3Controller match3Controller;
    //public RhythmController rhythmController;

    //private Match3Difficulty _pendingDifficulty = Match3Difficulty.Medium;

    void Start()
    {
        if (match3Button != null)
            match3Button.onClick.AddListener(StartMatch3);
        if (rhythmButton != null)
            rhythmButton.onClick.AddListener(StartRhythm);
        if (backButton != null)
            backButton.onClick.AddListener(BackToMainUI);

        ShowSelection();
        ApplyDifficultyLabel();
    }
    
    void Update()
    {
        // 安全检查：如果键盘没连接，直接返回
        if (Keyboard.current == null) return;

        // 直接检测数字键按下
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            // _pendingDifficulty = Match3Difficulty.Easy;
            ApplyDifficultyLabel();
            Debug.Log("难度切换: Easy");
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            // _pendingDifficulty = Match3Difficulty.Medium;
            ApplyDifficultyLabel();
            Debug.Log("难度切换: Medium");
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            // _pendingDifficulty = Match3Difficulty.Hard;
            ApplyDifficultyLabel();
            Debug.Log("难度切换: Hard");
        }

        // 如果你需要空格键作为“确认/开始”键，应该分开写，而不是嵌套：
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("开始游戏 / 确认选择");
        }
    }

void OnEnable()
    {
        EventBus.Subscribe(GameEvent.OnMatch3End, OnGameEnd);
        EventBus.Subscribe(GameEvent.OnRhythmEnd, OnGameEnd);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(GameEvent.OnMatch3End, OnGameEnd);
        EventBus.Unsubscribe(GameEvent.OnRhythmEnd, OnGameEnd);
    }

    private void ShowSelection()
    {
        if (selectionPanel != null) selectionPanel.SetActive(true);
        if (match3Panel != null) match3Panel.SetActive(false);
        if (rhythmPanel != null) rhythmPanel.SetActive(false);
    }

    private void StartMatch3()
    {
        Debug.Log("[PartTimeJob] 选择: 拾光餐厅 (三消)");
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (match3Panel != null) match3Panel.SetActive(true);
        /*
        if (match3Controller != null)
        {
            match3Controller.SetDifficulty(_pendingDifficulty);
            match3Controller.StartGame();
        }
        */
    }

    private void StartRhythm()
    {
        Debug.Log("[PartTimeJob] 选择: 远山民宿 (音游)");
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (rhythmPanel != null) rhythmPanel.SetActive(true);
        //if (rhythmController != null) rhythmController.StartGame();
    }

    private void OnGameEnd()
    {
        ShowSelection();
        ApplyDifficultyLabel();
    }

    private void BackToMainUI()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void ApplyDifficultyLabel()
    {
        if (match3Button == null) return;

        var label = match3Button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;

        /*
        string difficultyText = _pendingDifficulty == Match3Difficulty.Easy
            ? "简单"
            : _pendingDifficulty == Match3Difficulty.Hard ? "困难" : "中等";
        label.text = $"拾光餐厅\n(三消游戏)\n难度: {difficultyText} [1/2/3]";
        */
    }
}
