using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class mainUISceneController : MonoBehaviour
{
    [Header("--- 资源展示 ---")]
    public TextMeshProUGUI moneyText;

    [Header("--- 提示信息 (通用) ---")]
    public TextMeshProUGUI notificationText;

    [Header("--- 章节选择器 ---")]
    public List<ChapterSlot> chapterSlots;
    public List<ChapterConfig> allChapterConfigs;

    [Header("--- 功能按钮 ---")]
    public Button workSystemButton;
    public GameObject workPanel;
    public Button btnRestaurant;
    public Button btnGuesthouse;
    public Button btnAlbum;
    public Button btnSocial;
    public Button btnSetting;

    [Header("--- 场景名称 ---")]
    public string storySceneName = "04_StoryScenes";
    private string restaurantSceneName = "02_PartTimeJobs";
    private string guesthouseSceneName = "RhythmGame";
    private string albumSceneName = "Album";
    private string socialSceneName = "social";

    private GameData currentData;
    public AudioClip bgmClip;

    [System.Serializable]
    public class ChapterSlot
    {
        public int chapterId;
        public Button button;
        public GameObject lockIcon;
        public GameObject checkMarkIcon;
        public TextMeshProUGUI costText;
    }

    public Image reddot;

    void Start()
    {
        if (BGMManager.Instance)
        {
            BGMManager.Instance.bgm.clip = bgmClip;
            BGMManager.Instance.bgm.Play();
        }

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
            currentData = SaveManager.Instance.CurrentGameData;
        else
            currentData = new GameData("test_save");

        SaveManager.Instance.SaveCurrentGame();

        RefreshResourceUI();
        RefreshChapterState();

        if (workPanel != null) workPanel.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
        if (reddot != null) reddot.gameObject.SetActive(false);

        BindButtonEvents();
        CheckSocialBlockers();
    }
    private void BindButtonEvents()
    {
        if (workSystemButton) workSystemButton.onClick.AddListener(() => workPanel.SetActive(!workPanel.activeSelf));
        if (btnRestaurant) btnRestaurant.onClick.AddListener(() => LoadTargetScene(restaurantSceneName));
        if (btnGuesthouse) btnGuesthouse.onClick.AddListener(() => LoadTargetScene(guesthouseSceneName));
        if (btnAlbum) btnAlbum.onClick.AddListener(() => LoadTargetScene(albumSceneName));
        if (btnSocial) btnSocial.onClick.AddListener(() => LoadTargetScene(socialSceneName));
        if (btnSetting) btnSetting.onClick.AddListener(() =>SettingsManager.Instance.ToggleSettings());
    }
    public void RefreshResourceUI()
    {
        if (currentData == null) return;
        if (moneyText != null) moneyText.text = currentData.money.ToString();
    }
    private bool CheckSocialBlockers()
    {
        if (currentData == null || currentData.contactHistories == null) return false;

        foreach (var history in currentData.contactHistories)
        {
            if (history.contactId == "System_Bank") continue;
            if (history.hasUnread)
            {
                reddot.gameObject.SetActive(true);
                return true;
            }
            if (history.pendingOptions != null && history.pendingOptions.Count > 0)
            {
                reddot.gameObject.SetActive(true);
                return true;
            }
        }
        reddot.gameObject.SetActive(false);
        return false;
    }

    private void RefreshChapterState()
    {
        int playerProgress = currentData.currentChapter;

        foreach (var slot in chapterSlots)
        {
            ChapterConfig config = allChapterConfigs.Find(c => c.chapterId == slot.chapterId);
            int cost = config != null ? config.unlockCost : 0;
            int targetId = slot.chapterId;

            slot.button.onClick.RemoveAllListeners();

            if (slot.chapterId > playerProgress)
            {
                // 未解锁
                slot.button.interactable = false;
                if (slot.lockIcon) slot.lockIcon.SetActive(true);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(false);
                if (slot.costText) slot.costText.text = "未解锁";
            }
            else if (slot.chapterId == playerProgress)
            {
                // 当前正在进行的章节
                slot.button.interactable = true;
                if (slot.lockIcon) slot.lockIcon.SetActive(false);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(false);

                // --- 修改：根据 subState 显示文本 ---
                if (currentData.chapterSubState == 0)
                {
                    if (slot.costText) slot.costText.text = $"开启: {cost}";
                }
                else
                {
                    if (slot.costText) slot.costText.text = "继续"; // 已经在剧情或小游戏中
                }

                slot.button.onClick.AddListener(() => OnCurrentChapterClicked(cost, targetId));
            }
            else
            {
                // 已通关
                slot.button.interactable = true;
                if (slot.lockIcon) slot.lockIcon.SetActive(false);
                if (slot.checkMarkIcon) slot.checkMarkIcon.SetActive(true);
                if (slot.costText) slot.costText.text = "回顾";

                slot.button.onClick.AddListener(() => OnReplayChapterClicked(targetId));
            }
        }
    }

    private void OnCurrentChapterClicked(int cost, int chapterId)
    {
        // 1. 社交阻塞检查
        if (CheckSocialBlockers())
        {
            ShowNotification("手机里有重要信息未回复，请先查看！");
            return;
        }

        // 2. 状态检查与扣费逻辑
        // 如果 subState > 0，说明已经付过钱了，正在剧情中(1)或小游戏中(2)
        if (currentData.chapterSubState > 0)
        {
            Debug.Log($"继续章节 {chapterId}, 当前状态: {currentData.chapterSubState}");
            PlayerPrefs.SetInt("SelectedChapterId", chapterId);
            LoadTargetScene(storySceneName);
        }
        // 否则是初始状态 0，需要扣费并改为 1
        else if (currentData.money >= cost)
        {
            currentData.money -= cost;

            // --- 核心修改：设置 subState 为 1 (进入剧情) ---
            currentData.chapterSubState = 1;

            SaveManager.Instance.SaveCurrentGame();
            RefreshResourceUI();

            Debug.Log($"开启章节 {chapterId}, 扣除 {cost}, 状态设为 1");
            PlayerPrefs.SetInt("SelectedChapterId", chapterId);
            LoadTargetScene(storySceneName);
        }
        else
        {
            ShowNotification("资金不足，快去打工吧！");
        }
    }

    private void OnReplayChapterClicked(int chapterId)
    {
        if (CheckSocialBlockers())
        {
            ShowNotification("手机里有重要信息未回复！");
            return;
        }

        PlayerPrefs.SetInt("SelectedChapterId", chapterId);
        // 回顾模式不修改 subState
        LoadTargetScene(storySceneName);
    }

    // ... [UI 提示和 LoadTargetScene 保持不变] ...
    private void ShowNotification(string message)
    {
        if (notificationText == null)
        {
            Debug.LogWarning(message);
            return;
        }
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideNotificationDelay(2.0f));
    }

    private IEnumerator HideNotificationDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    private void LoadTargetScene(string sceneName)
    {
        if (BGMManager.Instance) BGMManager.Instance.bgm.Pause();
        TransitionManager.Instance.SwitchScene(sceneName);
    }
}