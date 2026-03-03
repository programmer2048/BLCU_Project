using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class mainUISceneController : MonoBehaviour
{
    [Header("--- 资源展示 ---")]
    public TextMeshProUGUI moneyText;

    [Header("--- 章节选择控制 ---")]
    public List<ChapterSlot> chapterSlots;
    public List<ChapterConfig> allChapterConfigs;

    [Header("--- 功能按钮 ---")]
    public Button workSystemButton;
    public GameObject workPanel;
    public Button btnRestaurant;
    public Button btnGuesthouse;
    public Button btnAlbum;

    [Header("--- 场景名称 ---")]
    public string storySceneName = "04_StoryScenes";
    private string restaurantSceneName = "02_PartTimeJobs";
    private string guesthouseSceneName = "RhythmGame";
    private string albumSceneName = "Album";

    private GameData currentData;

    [System.Serializable]
    public class ChapterSlot
    {
        public int chapterId;
        public Button button;
        public GameObject lockIcon;      // 未解锁图标 (锁)
        public GameObject checkMarkIcon; // 已完成图标 (勾)
        public TextMeshProUGUI costText; // 费用文本
    }

    void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
            currentData = SaveManager.Instance.CurrentGameData;
        else
            currentData = new GameData("test_save");

        SaveManager.Instance.SaveCurrentGame();

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
        int playerProgress = currentData.currentChapter;

        foreach (var slot in chapterSlots)
        {
            ChapterConfig config = allChapterConfigs.Find(c => c.chapterId == slot.chapterId);
            int cost = config != null ? config.unlockCost : 0;

            // 这是一个临时变量，用于闭包，防止点击事件里的ID出错
            int targetId = slot.chapterId;

            slot.button.onClick.RemoveAllListeners();

            // 状态 1：未解锁 (章节号 > 当前进度)
            if (slot.chapterId > playerProgress)
            {
                slot.button.interactable = false;
                if (slot.lockIcon) slot.lockIcon.SetActive(true);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(false);
                if (slot.costText) slot.costText.text = "未解锁";
            }
            // 状态 2：当前进度 (章节号 == 当前进度)
            else if (slot.chapterId == playerProgress)
            {
                slot.button.interactable = true;
                if (slot.lockIcon) slot.lockIcon.SetActive(false);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(false);
                if (slot.costText) slot.costText.text = $"费用: {cost}";

                // 【修改点】传入 targetId
                slot.button.onClick.AddListener(() => OnCurrentChapterClicked(cost, targetId));
            }
            // 状态 3：已通过 (章节号 < 当前进度)
            else
            {
                slot.button.interactable = true;
                if (slot.lockIcon) slot.lockIcon.SetActive(false);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(true);
                if (slot.costText) slot.costText.text = "重玩"; // 提示文字改为重玩

                // 【修改点】调用专门的回顾方法，传入 targetId
                slot.button.onClick.AddListener(() => OnReplayChapterClicked(targetId));
            }
        }
    }

    // 处理当前新章节的点击（扣钱 + 记录ID + 跳转）
    private void OnCurrentChapterClicked(int cost, int chapterId)
    {
        if (currentData.money >= cost)
        {
            currentData.money -= cost;
            SaveManager.Instance.SaveCurrentGame();
            RefreshResourceUI();

            // 【关键】记录玩家选择的章节ID，告诉下一个场景该播哪个剧情
            PlayerPrefs.SetInt("SelectedChapterId", chapterId);

            LoadTargetScene(storySceneName);
        }
        else
        {
            Debug.LogWarning("旅费不足，请先打工！");
        }
    }

    // 处理旧章节的点击（不扣钱 + 记录ID + 跳转）
    private void OnReplayChapterClicked(int chapterId)
    {
        // 【关键】记录玩家选择的章节ID
        PlayerPrefs.SetInt("SelectedChapterId", chapterId);

        LoadTargetScene(storySceneName);
    }

    private void LoadTargetScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}