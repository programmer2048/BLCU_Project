using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("控制器")]
    public Match3Controller match3Controller;
    public RhythmController rhythmController;

    private Match3Difficulty _pendingDifficulty = Match3Difficulty.Medium;

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
        if (selectionPanel == null || !selectionPanel.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _pendingDifficulty = Match3Difficulty.Easy;
            ApplyDifficultyLabel();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _pendingDifficulty = Match3Difficulty.Medium;
            ApplyDifficultyLabel();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _pendingDifficulty = Match3Difficulty.Hard;
            ApplyDifficultyLabel();
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
        if (match3Controller != null)
        {
            match3Controller.SetDifficulty(_pendingDifficulty);
            match3Controller.StartGame();
        }
    }

    private void StartRhythm()
    {
        Debug.Log("[PartTimeJob] 选择: 远山民宿 (音游)");
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (rhythmPanel != null) rhythmPanel.SetActive(true);
        if (rhythmController != null) rhythmController.StartGame();
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

        string difficultyText = _pendingDifficulty == Match3Difficulty.Easy
            ? "简单"
            : _pendingDifficulty == Match3Difficulty.Hard ? "困难" : "中等";

        label.text = $"拾光餐厅\n(三消游戏)\n难度: {difficultyText} [1/2/3]";
    }
}
