using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 结局判断系统
/// </summary>
public class EndingJudgeSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject endingPanel;
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI endingContentText;
    public Button restartButton;

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    void OnEnable()
    {
        EventBus.Subscribe(GameEvent.OnEndingTriggered, ShowEnding);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe(GameEvent.OnEndingTriggered, ShowEnding);
    }

    private void ShowEnding()
    {
        if (endingPanel != null) endingPanel.SetActive(true);

        bool endingA = GameManager.Instance != null && GameManager.Instance.CanGetEndingA();

        if (endingA)
        {
            Debug.Log("[Ending] 结局A: 檐下星河 —— 留守传承");
            if (endingTitleText != null) endingTitleText.text = "结局A：檐下星河";
            if (endingContentText != null)
                endingContentText.text = "陈默决定留在山西，与林晓一起记录和保护这些古建筑。\n" +
                    "在漫天星河下，他们并肩站在古老的檐角旁，\n" +
                    "终于找到了属于自己的方向。\n\n" +
                    "\"有些风景，值得用一生去守候。\"";
        }
        else
        {
            Debug.Log("[Ending] 结局B: 山河远望 —— 带着感悟回归");
            if (endingTitleText != null) endingTitleText.text = "结局B：山河远望";
            if (endingContentText != null)
                endingContentText.text = "陈默带着旅途的感悟回到了城市。\n" +
                    "虽然和林晓未能深交，但这趟旅行让他重新审视了自己。\n" +
                    "他开始用建筑师的身份，将古建美学融入现代设计。\n\n" +
                    "\"每一座建筑，都是一代人的心意。\"";
        }

    }

    private void RestartGame()
    {
        if (endingPanel != null) endingPanel.SetActive(false);
        SaveManager.Instance?.DeleteSave();
        GameManager.Instance?.InitializeData();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ChangeState(GameState.Boot);
    }
}
