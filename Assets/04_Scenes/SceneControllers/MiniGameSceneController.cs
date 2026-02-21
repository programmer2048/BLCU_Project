using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 小游戏场景控制器 —— 根据章节索引激活对应小游戏
/// </summary>
public class MiniGameSceneController : MonoBehaviour
{
    [Header("小游戏面板")]
    public GameObject mortiseTenonPanel;
    public GameObject findDifferencesPanel;
    public GameObject timeRestorerPanel;
    public GameObject cardMatchingPanel;

    [Header("控制器引用")]
    public MortiseTenon mortiseTenon;
    public FindDifferences findDifferences;
    public TimeRestorer timeRestorer;
    public CardMatching cardMatching;

    [Header("通用UI")]
    public TextMeshProUGUI titleText;
    public Button backButton;

    private readonly string[] GameTitles = {
        "榫卯大师 —— 应县木塔",
        "时光寻宝 —— 佛光寺",
        "时光修复师 —— 南禅寺",
        "侍女心语 —— 晋祠"
    };

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(BackToMainUI);

        HideAll();
        StartMiniGame();
    }

    void OnEnable()
    {
        EventBus.Subscribe(GameEvent.OnMiniGameEnd, OnMiniGameEnd);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(GameEvent.OnMiniGameEnd, OnMiniGameEnd);
    }

    private void StartMiniGame()
    {
        int chapter = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentChapterIndex : 0;
        chapter = Mathf.Clamp(chapter, 0, 3);

        if (titleText != null) titleText.text = GameTitles[chapter];
        Debug.Log($"[MiniGame] 开始小游戏: {GameTitles[chapter]}");

        switch (chapter)
        {
            case 0:
                if (mortiseTenonPanel != null) mortiseTenonPanel.SetActive(true);
                if (mortiseTenon != null) mortiseTenon.StartGame();
                break;
            case 1:
                if (findDifferencesPanel != null) findDifferencesPanel.SetActive(true);
                if (findDifferences != null) findDifferences.StartGame();
                break;
            case 2:
                if (timeRestorerPanel != null) timeRestorerPanel.SetActive(true);
                if (timeRestorer != null) timeRestorer.StartGame();
                break;
            case 3:
                if (cardMatchingPanel != null) cardMatchingPanel.SetActive(true);
                if (cardMatching != null) cardMatching.StartGame();
                break;
        }

        EventBus.Publish(GameEvent.OnMiniGameStart);
    }

    private void OnMiniGameEnd()
    {
        Debug.Log("[MiniGame] 小游戏结束，3秒后返回主界面");
        Invoke(nameof(BackToMainUI), 3f);
    }

    private void HideAll()
    {
        if (mortiseTenonPanel != null) mortiseTenonPanel.SetActive(false);
        if (findDifferencesPanel != null) findDifferencesPanel.SetActive(false);
        if (timeRestorerPanel != null) timeRestorerPanel.SetActive(false);
        if (cardMatchingPanel != null) cardMatchingPanel.SetActive(false);
    }

    private void BackToMainUI()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }
}
