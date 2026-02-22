using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 相簿控制器 —— 管理图鉴的展示
/// </summary>
public class AlbumController : MonoBehaviour
{
    [Header("UI")]
    public Transform itemContainer;
    public GameObject albumItemPrefab; // 可以为null，运行时创建
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public Button backButton;

    // 图鉴数据
    private static readonly string[] AllAlbumItems = {
        "木塔模型摆件", "佛光寺老照片", "南禅寺禅意图鉴", "侍女明信片"
    };
    private static readonly string[] AlbumDescriptions = {
        "应县木塔的斗拱结构模型，展现了千年榫卯技艺的巅峰。",
        "1937年梁思成拍摄的佛光寺东大殿珍贵照片。",
        "南禅寺大殿的彩塑写生，记录修复前的古朴之美。",
        "晋祠圣母殿侍女像的临摹明信片，每一尊表情各异。"
    };

    private readonly Color _unlockedColor = new Color(0.2f, 0.8f, 0.6f);
    private readonly Color _lockedColor = new Color(0.4f, 0.4f, 0.4f);

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClick);
        RefreshAlbum();
    }

    void OnEnable()
    {
        EventBus.Subscribe<string>(GameEvent.OnAlbumItemUnlocked, OnItemUnlocked);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<string>(GameEvent.OnAlbumItemUnlocked, OnItemUnlocked);
    }

    private void OnItemUnlocked(string itemName)
    {
        RefreshAlbum();
    }

    public void RefreshAlbum()
    {
        if (itemContainer == null) return;

        // 清空现有项
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < AllAlbumItems.Length; i++)
        {
            bool unlocked = GameManager.Instance != null && GameManager.Instance.HasAlbumItem(AllAlbumItems[i]);
            CreateAlbumItemUI(i, unlocked);
        }

        if (titleText != null) titleText.text = "相簿";
        if (descText != null)
        {
            int count = GameManager.Instance != null ? GameManager.Instance.AlbumItems.Count : 0;
            descText.text = $"已收集: {count}/{AllAlbumItems.Length}";
        }
    }

    private void CreateAlbumItemUI(int index, bool unlocked)
    {
        GameObject item = new GameObject($"AlbumItem_{index}");
        RectTransform rt = item.AddComponent<RectTransform>();
        rt.SetParent(itemContainer, false);
        rt.sizeDelta = new Vector2(300, 80);

        // 背景
        Image bg = item.AddComponent<Image>();
        bg.color = unlocked ? _unlockedColor : _lockedColor;

        // 文字
        GameObject textObj = new GameObject("Text");
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 5);
        textRt.offsetMax = new Vector2(-10, -5);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = unlocked ? $"{AllAlbumItems[index]}\n{AlbumDescriptions[index]}" : "??? 未解锁";
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void OnBackClick()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }
}
