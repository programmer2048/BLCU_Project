using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class mainUISceneController : MonoBehaviour
{
    [Header("--- 资源展示 ---")]
    public TextMeshProUGUI moneyText;

    [Header("--- 章节与配置库 ---")]
    public List<ChapterSlot> chapterSlots;
    public List<ChapterConfig> allChapterConfigs;

    [Header("--- 交互按钮 ---")]
    public Button workSystemButton;
    public GameObject workPanel;
    public Button btnRestaurant;
    public Button btnGuesthouse;
    public Button btnAlbum;

    [Header("--- 场景名称 ---")]
    public string storySceneName = "04_StoryScenes";
    private string restaurantSceneName = "02_PartTimeJobs";
    private string guesthouseSceneName = "PhythmGame";
    private string albumSceneName = "Album";

    private GameData currentData;

    [System.Serializable]
    public class ChapterSlot
    {
        public int chapterId;
        public Button button;
        public GameObject lockIcon;      // 未解锁图标 (锁)
        public GameObject checkMarkIcon; // 已完成图标 (打勾)
        public TextMeshProUGUI costText; // 旅费文本
    }

    void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
            currentData = SaveManager.Instance.CurrentGameData;
        else
            currentData = new GameData("test_save");

        RefreshResourceUI();
        RefreshChapterState();

        if (workPanel != null) workPanel.SetActive(false);
        BindButtonEvents();
    }

    private void BindButtonEvents()
    {
        if (workSystemButton) workSystemButton.onClick.AddListener(() => workPanel.SetActive(!workPanel.activeSelf));
        if (btnRestaurant) btnRestaurant.onClick.AddListener(() => LoadTargetScene(restaurantSceneName));
        if (btnGuesthouse) btnGuesthouse.onClick.AddListener(() => LoadTargetScene(guesthouseSceneName));
        if (btnAlbum) btnAlbum.onClick.AddListener(() => LoadTargetScene(albumSceneName));
    }

    public void RefreshResourceUI()
    {
        if (currentData == null) return;
        if (moneyText != null) moneyText.text = currentData.money.ToString();
    }

    private void RefreshChapterState()
    {
        int playerProgress = currentData.currentChapter; // 假设新档为 1

        foreach (var slot in chapterSlots)
        {
            ChapterConfig config = allChapterConfigs.Find(c => c.chapterId == slot.chapterId);
            int cost = config != null ? config.unlockCost : 0;

            slot.button.onClick.RemoveAllListeners();

            // 状态 1：未解锁 (章节号 > 当前进度)
            if (slot.chapterId > playerProgress)
            {
                slot.button.interactable = false; // 无响应
                if (slot.lockIcon) slot.lockIcon.SetActive(true);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(false);
                if (slot.costText) slot.costText.text = "未解锁";
            }
            // 状态 2：当前可玩 (章节号 == 当前进度)
            else if (slot.chapterId == playerProgress)
            {
                slot.button.interactable = true;
                if (slot.lockIcon) slot.lockIcon.SetActive(false);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(false);
                if (slot.costText) slot.costText.text = $"旅费: {cost}";

                slot.button.onClick.AddListener(() => OnCurrentChapterClicked(cost));
            }
            // 状态 3：已经完成 (章节号 < 当前进度)
            else
            {
                slot.button.interactable = true; // 允许免费回顾剧情
                if (slot.lockIcon) slot.lockIcon.SetActive(false);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(true); // 显示打勾
                if (slot.costText) slot.costText.text = "已通关";

                slot.button.onClick.AddListener(() => LoadTargetScene(storySceneName));
            }
        }
    }

    private void OnCurrentChapterClicked(int cost)
    {
        if (currentData.money >= cost)
        {
            currentData.money -= cost;
            SaveManager.Instance.SaveCurrentGame();
            RefreshResourceUI();
            LoadTargetScene(storySceneName);
        }
        else
        {
            Debug.LogWarning("旅费不足！请先打工。");
        }
    }

    private void LoadTargetScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}