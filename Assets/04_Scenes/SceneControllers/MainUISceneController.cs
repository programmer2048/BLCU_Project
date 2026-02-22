using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主界面场景控制器 —— 地图/导航/旅费显示
/// </summary>
public class MainUISceneController : MonoBehaviour
{
    [Header("顶部状态栏")]
    public TextMeshProUGUI currencyText;
    public Button messageButton;
    public GameObject messageUnreadDot;

    [Header("底部导航")]
    public Button workButton;
    public Button albumButton;

    [Header("地图景点按钮")]
    public Button[] spotButtons;
    public TextMeshProUGUI[] spotLabels;
    public Image[] spotImages;

    [Header("聊天面板")]
    public GameObject chatPanel;
    public SocialUIController socialUI;

    [Header("结局检查")]
    public Button endingButton;

    private bool _hasUnreadMessage = true;

    void Start()
    {
        // 导航按钮
        if (workButton != null)
            workButton.onClick.AddListener(() => {
                Debug.Log("[MainUI] 点击打工按钮");
                if (GameFlowManager.Instance != null)
                    GameFlowManager.Instance.GoToPartTimeJob();
            });

        if (albumButton != null)
            albumButton.onClick.AddListener(() => {
                Debug.Log("[MainUI] 点击相簿按钮");
                if (GameFlowManager.Instance != null)
                    GameFlowManager.Instance.GoToAlbum();
            });

        if (messageButton != null)
            messageButton.onClick.AddListener(() => {
                Debug.Log("[MainUI] 点击消息按钮");
                SetUnreadMessage(false);
                if (GameFlowManager.Instance != null)
                    GameFlowManager.Instance.GoToSocial();
            });

        // 景点按钮
        for (int i = 0; i < 4; i++)
        {
            if (spotButtons != null && i < spotButtons.Length && spotButtons[i] != null)
            {
                int index = i;
                spotButtons[i].onClick.AddListener(() => OnSpotClicked(index));
            }
        }

        // 结局按钮（当所有景点探索完毕时显示）
        if (endingButton != null)
            endingButton.onClick.AddListener(() => {
                if (GameFlowManager.Instance != null && GameManager.Instance != null && GameManager.Instance.AllSpotsExplored())
                    GameFlowManager.Instance.GoToEnding();
            });

        if (chatPanel != null)
            chatPanel.SetActive(false);

        RefreshUI();
        SetUnreadMessage(_hasUnreadMessage);
    }

    void OnEnable()
    {
        EventBus.Subscribe<int>(GameEvent.OnCurrencyChanged, OnCurrencyChanged);
        EventBus.Subscribe<int>(GameEvent.OnSpotUnlocked, OnSpotUnlocked);
        EventBus.Subscribe<int>(GameEvent.OnSpotExplored, OnSpotExplored);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<int>(GameEvent.OnCurrencyChanged, OnCurrencyChanged);
        EventBus.Unsubscribe<int>(GameEvent.OnSpotUnlocked, OnSpotUnlocked);
        EventBus.Unsubscribe<int>(GameEvent.OnSpotExplored, OnSpotExplored);
    }

    private void OnSpotUnlocked(int _)
    {
        RefreshSpots();
        SetUnreadMessage(true);
    }

    private void OnSpotExplored(int _)
    {
        RefreshSpots();
        SetUnreadMessage(true);
    }

    private void OnCurrencyChanged(int amount)
    {
        if (currencyText != null)
            currencyText.text = $"旅费: {amount}";
    }

    private void OnSpotClicked(int index)
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.IsSpotExplored(index))
        {
            Debug.Log($"[MainUI] {GameManager.SpotNames[index]} 已探索完毕");
            return;
        }

        if (GameManager.Instance.IsSpotUnlocked(index))
        {
            Debug.Log($"[MainUI] 进入 {GameManager.SpotNames[index]} 剧情");
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CurrentChapterIndex = index;
                GameFlowManager.Instance.GoToStory();
            }
            return;
        }

        // 检查前一个景点是否已解锁（线性解锁）
        if (index > 0 && !GameManager.Instance.IsSpotUnlocked(index - 1))
        {
            Debug.Log($"[MainUI] 需要先解锁 {GameManager.SpotNames[index - 1]}");
            return;
        }

        bool success = GameManager.Instance.UnlockSpot(index);
        if (success)
        {
            RefreshSpots();
            // 解锁后立即进入剧情
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.CurrentChapterIndex = index;
                GameFlowManager.Instance.GoToStory();
            }
        }
        else
        {
            Debug.Log($"[MainUI] 旅费不足! 需要 {GameManager.SpotCosts[index]}");
        }
    }

    public void RefreshUI()
    {
        if (currencyText != null && GameManager.Instance != null)
            currencyText.text = $"旅费: {GameManager.Instance.TravelFee}";
        RefreshSpots();
        RefreshEndingButton();
    }

    private void RefreshSpots()
    {
        if (GameManager.Instance == null) return;

        for (int i = 0; i < 4; i++)
        {
            bool unlocked = GameManager.Instance.IsSpotUnlocked(i);
            bool explored = GameManager.Instance.IsSpotExplored(i);

            if (spotLabels != null && i < spotLabels.Length && spotLabels[i] != null)
            {
                string status = explored ? " [已探索]" : (unlocked ? " [可探索]" : $" [锁]{GameManager.SpotCosts[i]}");
                spotLabels[i].text = $"{GameManager.SpotNames[i]}{status}";
            }

            if (spotImages != null && i < spotImages.Length && spotImages[i] != null)
            {
                if (explored) spotImages[i].color = new Color(0.2f, 0.6f, 1f);
                else if (unlocked) spotImages[i].color = new Color(0.2f, 0.8f, 0.2f);
                else spotImages[i].color = Color.gray;
            }
        }

        RefreshEndingButton();
    }

    private void RefreshEndingButton()
    {
        if (endingButton != null && GameManager.Instance != null)
        {
            endingButton.gameObject.SetActive(GameManager.Instance.AllSpotsExplored());
        }
    }

    private void SetUnreadMessage(bool hasUnread)
    {
        _hasUnreadMessage = hasUnread;

        if (messageUnreadDot == null && messageButton != null)
        {
            var dot = new GameObject("UnreadDot");
            dot.transform.SetParent(messageButton.transform, false);
            var rt = dot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.78f, 0.72f);
            rt.anchorMax = new Vector2(0.95f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = dot.AddComponent<Image>();
            img.color = new Color(0.95f, 0.2f, 0.2f, 0.95f);
            messageUnreadDot = dot;
        }

        if (messageUnreadDot != null)
            messageUnreadDot.SetActive(_hasUnreadMessage);
    }
}
