using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class StoryManager : MonoBehaviour
{
    [Header("--- 章节资源库 ---")]
    public List<ChapterConfig> allChapters;
    private ChapterConfig currentConfig; // 当前正在播放的章节配置

    [Header("--- UI 组件 ---")]
    public Image backgroundDisplay;
    public TextMeshProUGUI chapterTitle;
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    public Image leftCharacter;
    public Image rightCharacter;

    [Header("--- 选项 UI 组件 ---")]
    public GameObject choicePanel;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonContainer;
    private RenderTexture videoRenderTexture;

    [Header("--- 视频 UI 组件 ---")]
    public GameObject videoPanel;
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;

    private int currentNodeIndex = 0;
    private int tempAffection = 0;
    private bool isTyping = false;
    private string currentFullText = "";

    private bool isReplayMode = false;
    private bool wasBgmPlaying = false;

    // 【新增】：剧情播放状态锁，防止在场景过渡期间意外触发点击
    private bool isPlayingStory = false;

    void Awake()
    {
        // 强制开启日志
        Debug.unityLogger.logEnabled = true;
        // 确保过滤器不过滤任何类型
        Debug.unityLogger.filterLogType = LogType.Log;
    }

    void Start()
    {
        if (videoPlayer != null && videoDisplay != null)
        {
            videoRenderTexture = new RenderTexture(1280, 788, 0);
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoDisplay.texture = videoRenderTexture;
        }

        // 初始化UI状态
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
        videoPanel.SetActive(false);
        tempAffection = 0;
        isPlayingStory = false; // 初始锁死

        // 获取目标章节 ID
        int savedProgress = SaveManager.Instance.CurrentGameData.currentChapter;

        // 获取用户点击选择的章节
        int selectedChapterId = PlayerPrefs.GetInt("SelectedChapterId", savedProgress);

        // 判断是否为回顾模式
        if (selectedChapterId < savedProgress)
        {
            isReplayMode = true;
            Debug.Log($"[Story] 回顾模式 (Replay) ID: {selectedChapterId}");
        }
        else
        {
            isReplayMode = false;
        }

        // 清理 Key
        PlayerPrefs.DeleteKey("SelectedChapterId");

        // 配置加载
        currentConfig = allChapters.Find(c => c.chapterId == selectedChapterId);
        if (currentConfig == null || currentConfig.storyNodes == null || currentConfig.storyNodes.Count == 0)
        {
            Debug.LogError($"配置错误：找不到章节 {selectedChapterId}");
            TransitionManager.Instance.SwitchScene("01_MainUI");
            return;
        }

        // 如果不是回顾模式，且当前状态为 2 (小游戏阶段)，直接跳转小游戏
        if (!isReplayMode && SaveManager.Instance.CurrentGameData.chapterSubState >= 2)
        {
            if (!string.IsNullOrEmpty(currentConfig.miniGameSceneName))
            {
                Debug.Log($"[Story] 检测到状态为 2，直接跳转至小游戏：{currentConfig.miniGameSceneName}");

                // 【修复】：安全播放小游戏BGM，避免越界报错
                if (currentConfig.storyNodes[0].bgmClip != null)
                {
                    BGMManager.Instance.bgm.clip = currentConfig.storyNodes[0].bgmClip;
                    BGMManager.Instance.bgm.Play();
                }

                TransitionManager.Instance.SwitchScene(currentConfig.miniGameSceneName);
                return; // 【注意】：这里直接 return，isPlayingStory 保持 false，彻底阻断 Update
            }
            else
            {
                Debug.LogWarning("状态为2但无小游戏配置，自动修正状态");
                EndChapterFlow();
                return;
            }
        }

        // --- 正常开始播放剧情 ---
        isPlayingStory = true; // 【新增】：解锁剧情逻辑
        currentNodeIndex = 0;
        chapterTitle.text = currentConfig.chapterTitle;
        PlayCurrentNode();
    }

    void Update()
    {
        // 【新增】：如果剧情没有在播放（比如正在加载小游戏场景），直接阻断所有的输入检测！
        if (!isPlayingStory) return;

        if (currentConfig == null || currentConfig.storyNodes == null || currentConfig.storyNodes.Count == 0) return;

        if (currentConfig.storyNodes[currentNodeIndex].nodeType == StoryNodeType.Dialog)
        {
            bool isSkipPressed = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                isSkipPressed = true;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                isSkipPressed = true;

            if (isSkipPressed)
            {
                if (isTyping)
                {
                    StopAllCoroutines();
                    dialogText.text = currentFullText;
                    isTyping = false;
                }
                else
                {
                    NextNode();
                }
            }
        }
    }

    private void NextNode()
    {
        currentNodeIndex++;
        if (currentNodeIndex < currentConfig.storyNodes.Count)
            PlayCurrentNode();
        else
            EndStorySegment(); // 剧情部分结束
    }

    private void PlayCurrentNode()
    {
        StoryNode node = currentConfig.storyNodes[currentNodeIndex];
        if (node.backgroundSprite != null)
        {
            if (backgroundDisplay != null)
            {
                backgroundDisplay.sprite = node.backgroundSprite;
                backgroundDisplay.gameObject.SetActive(true);
                backgroundDisplay.color = Color.white;
            }
        }
        if (node.bgmClip != null)
        {
            BGMManager.Instance.bgm.clip = node.bgmClip;
            BGMManager.Instance.bgm.Play();
        }

        switch (node.nodeType)
        {
            case StoryNodeType.Video:
                PlayVideo(node.videoClip);
                break;
            case StoryNodeType.Dialog:
                PlayDialog(node);
                break;
            case StoryNodeType.Choice:
                ShowChoices(node.choices);
                break;
            case StoryNodeType.EndStory:
                EndStorySegment();
                break;
        }
    }

    private void PlayVideo(VideoClip clip)
    {
        if (clip == null) { NextNode(); return; }
        if (BGMManager.Instance != null && BGMManager.Instance.bgm != null)
        {
            wasBgmPlaying = BGMManager.Instance.bgm.isPlaying;
            if (wasBgmPlaying)
            {
                BGMManager.Instance.bgm.Pause();
            }
        }
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
        videoPanel.SetActive(true);
        videoPlayer.clip = clip;
        videoPlayer.Play();
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoEnd;
        videoPanel.SetActive(false);
        if (wasBgmPlaying && BGMManager.Instance != null && BGMManager.Instance.bgm != null)
        {
            BGMManager.Instance.bgm.UnPause();
        }
        NextNode();
    }

    private void PlayDialog(StoryNode node)
    {
        videoPanel.SetActive(false);
        choicePanel.SetActive(false);
        dialogPanel.SetActive(true);
        nameText.text = node.speakerName;
        currentFullText = node.dialogText;

        if (node.leftSprite != null) { leftCharacter.sprite = node.leftSprite; leftCharacter.gameObject.SetActive(true); leftCharacter.color = Color.white; }
        else { leftCharacter.gameObject.SetActive(false); }

        if (node.rightSprite != null) { rightCharacter.sprite = node.rightSprite; rightCharacter.gameObject.SetActive(true); rightCharacter.color = Color.white; }
        else { rightCharacter.gameObject.SetActive(false); }

        StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        dialogText.text = "";
        foreach (char c in currentFullText.ToCharArray()) { dialogText.text += c; yield return new WaitForSeconds(0.03f); }
        isTyping = false;
    }

    private void ShowChoices(List<StoryChoice> choices)
    {
        choicePanel.SetActive(true);
        foreach (Transform child in choiceButtonContainer) Destroy(child.gameObject);

        int currentTotalAffection = SaveManager.Instance.CurrentGameData.affinity + tempAffection;

        foreach (var choice in choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btnObj.SetActive(true);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (currentTotalAffection >= choice.requireAffection)
            {
                btnText.text = choice.choiceText;
                btn.onClick.AddListener(() => OnChoiceSelected(choice));
            }
            else
            {
                btnText.text = choice.choiceText + " <color=red>(好感度不足)</color>";
                btn.interactable = false;
            }
        }
    }

    private void OnChoiceSelected(StoryChoice choice)
    {
        tempAffection += choice.addAffection;
        choicePanel.SetActive(false);

        if (choice.jumpToNodeIndex != -1)
            currentNodeIndex = choice.jumpToNodeIndex - 1;

        NextNode();
    }

    private void EndStorySegment()
    {
        isPlayingStory = false; // 【新增】：剧情结束，立刻锁死输入检测

        // 1. 处理好感度和社交
        if (!isReplayMode)
        {
            if (tempAffection != 0)
                SaveManager.Instance.CurrentGameData.affinity += tempAffection;

            if (currentConfig.socialUpdate != null) InjectSocialUpdate(currentConfig.socialUpdate);
        }

        // 2. 判断是否有小游戏
        if (!string.IsNullOrEmpty(currentConfig.miniGameSceneName))
        {
            if (isReplayMode)
            {
                PlayerPrefs.SetInt("IsReplayMode", 1);
            }
            else
            {
                SaveManager.Instance.CurrentGameData.chapterSubState = 2;
                SaveManager.Instance.SaveCurrentGame();
            }
            TransitionManager.Instance.SwitchScene(currentConfig.miniGameSceneName);
        }
        else
        {
            EndChapterFlow();
        }
    }

    private void InjectSocialUpdate(SocialUpdateBatch batch)
    {
        GameData data = SaveManager.Instance.CurrentGameData;
        foreach (var moment in batch.unlockMomentIds)
        {
            if (!data.unlockedMomentIds.Contains(moment))
                data.unlockedMomentIds.Add(moment);
        }

        foreach (var conv in batch.conversations)
        {
            if (!data.unlockedContactIds.Contains(conv.contactId))
                data.unlockedContactIds.Add(conv.contactId);

            ContactHistoryData history = data.GetOrCreateInfo(conv.contactId);
            foreach (var msgText in conv.messages)
            {
                history.chatLog.Add(new ChatMessage
                {
                    sender = SenderType.NPC,
                    type = MessageType.Text,
                    content = msgText,
                    timeStamp = System.DateTime.Now.ToString("HH:mm")
                });
            }
            history.hasUnread = true;
            if (conv.replyOptions != null && conv.replyOptions.Count > 0)
                history.pendingOptions = new List<ChatReplyOption>(conv.replyOptions);
        }
        Debug.Log("[StoryManager] 社交数据已成功注入存档。");
    }

    private void EndChapterFlow()
    {
        if (!isReplayMode && currentConfig != null)
        {
            SaveManager.Instance.CurrentGameData.currentChapter = currentConfig.nextChapterId;
            SaveManager.Instance.CurrentGameData.chapterSubState = 0;
            SaveManager.Instance.SaveCurrentGame();
            Debug.Log($"[Story] 章节结束，进度更新为: {currentConfig.nextChapterId}, 状态重置为 0");
        }
        else
        {
            Debug.Log("[Story] 回顾模式结束，不更新进度");
        }
        TransitionManager.Instance.SwitchScene("01_MainUI");
    }

    private void OnDestroy()
    {
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
        }
    }
}