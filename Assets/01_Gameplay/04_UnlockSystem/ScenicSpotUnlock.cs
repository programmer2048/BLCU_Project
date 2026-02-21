using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 景点解锁系统 —— 控制景点的解锁/探索逻辑
/// </summary>
public class ScenicSpotUnlock : MonoBehaviour
{
    [Header("景点索引 (0-3)")]
    public int spotIndex;

    [Header("UI引用")]
    public Button unlockButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI statusText;
    public Image spotImage;

    // 状态颜色
    private readonly Color _lockedColor = Color.gray;
    private readonly Color _unlockedColor = new Color(0.2f, 0.8f, 0.2f);
    private readonly Color _exploredColor = new Color(0.2f, 0.6f, 1f);

    void Start()
    {
        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnClickSpot);
        RefreshUI();
    }

    void OnEnable()
    {
        EventBus.Subscribe<int>(GameEvent.OnSpotUnlocked, OnSpotStateChanged);
        EventBus.Subscribe<int>(GameEvent.OnSpotExplored, OnSpotStateChanged);
        EventBus.Subscribe<int>(GameEvent.OnCurrencyChanged, _ => RefreshUI());
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<int>(GameEvent.OnSpotUnlocked, OnSpotStateChanged);
        EventBus.Unsubscribe<int>(GameEvent.OnSpotExplored, OnSpotStateChanged);
        EventBus.Unsubscribe<int>(GameEvent.OnCurrencyChanged, _ => RefreshUI());
    }

    private void OnSpotStateChanged(int index)
    {
        if (index == spotIndex)
            RefreshUI();
    }

    private void OnClickSpot()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.IsSpotExplored(spotIndex))
        {
            Debug.Log($"[景点] {GameManager.SpotNames[spotIndex]} 已探索完毕");
            return;
        }

        if (GameManager.Instance.IsSpotUnlocked(spotIndex))
        {
            // 已解锁，进入剧情/小游戏
            Debug.Log($"[景点] 进入 {GameManager.SpotNames[spotIndex]}");
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.GoToMiniGame(spotIndex);
            }
            return;
        }

        // 尝试解锁
        bool success = GameManager.Instance.UnlockSpot(spotIndex);
        if (success)
        {
            Debug.Log($"[景点] {GameManager.SpotNames[spotIndex]} 解锁成功！");
            RefreshUI();
            // 解锁后触发剧情
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CurrentChapterIndex = spotIndex;
                GameFlowManager.Instance.GoToStory();
            }
        }
        else
        {
            Debug.Log($"[景点] 旅费不足，无法解锁 {GameManager.SpotNames[spotIndex]}");
        }
    }

    public void RefreshUI()
    {
        if (GameManager.Instance == null) return;

        if (nameText != null) nameText.text = GameManager.SpotNames[spotIndex];
        if (costText != null) costText.text = $"费用: {GameManager.SpotCosts[spotIndex]}";

        bool unlocked = GameManager.Instance.IsSpotUnlocked(spotIndex);
        bool explored = GameManager.Instance.IsSpotExplored(spotIndex);

        if (statusText != null)
        {
            if (explored) statusText.text = "[已探索]";
            else if (unlocked) statusText.text = "[可探索]";
            else statusText.text = "[锁] 未解锁";
        }

        if (spotImage != null)
        {
            if (explored) spotImage.color = _exploredColor;
            else if (unlocked) spotImage.color = _unlockedColor;
            else spotImage.color = _lockedColor;
        }
    }
}
