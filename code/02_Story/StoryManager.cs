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
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    public Image leftCharacter;
    public Image rightCharacter;

    [Header("--- 选项 UI 组件 ---")]
    public GameObject choicePanel;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonContainer;

    [Header("--- 视频 UI 组件 ---")]
    public GameObject videoPanel;
    public VideoPlayer videoPlayer;

    private int currentNodeIndex = 0;
    private int tempAffection = 0;
    private bool isTyping = false;
    private string currentFullText = "";

    // --- 新增：判断是否为回顾模式 ---
    private bool isReplayMode = false;
    void Awake()
    {

        // 强制开启日志
        Debug.unityLogger.logEnabled = true;
        // 确保过滤器不过滤任何类型
        Debug.unityLogger.filterLogType = LogType.Log;
    }

    void Start()
    {
        // 1. 初始化UI状态
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
        videoPanel.SetActive(false);
        tempAffection = 0;
        // 2. 获取目标章节 ID
        int savedProgress = SaveManager.Instance.CurrentGameData.currentChapter;

        // 获取用户点击选择的章节
        int selectedChapterId = PlayerPrefs.GetInt("SelectedChapterId", savedProgress);
        // 判断是否为回顾模式
        // 如果选的比当前存档进度小，肯定是回顾
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
        // 3. 配置加载
        currentConfig = allChapters.Find(c => c.chapterId == selectedChapterId);
        if (currentConfig == null || currentConfig.storyNodes == null || currentConfig.storyNodes.Count == 0)
        {
            Debug.LogError($"配置错误：找不到章节 {selectedChapterId}");
            SceneManager.LoadScene("01_MainUI");
            return;
        }
        // 如果不是回顾模式，且当前状态为 2 (小游戏阶段)，直接跳转小游戏
        if (!isReplayMode && SaveManager.Instance.CurrentGameData.chapterSubState == 2)
        {
            if (!string.IsNullOrEmpty(currentConfig.miniGameSceneName))
            {
                Debug.Log($"[Story] 检测到状态为 2，直接跳转至小游戏：{currentConfig.miniGameSceneName}");
                SceneManager.LoadScene(currentConfig.miniGameSceneName);
                return; // 终止后续 Start 逻辑
            }
            else
            {
                // 异常情况：状态是2但没有配置小游戏，重置回0并尝试正常结束
                Debug.LogWarning("状态为2但无小游戏配置，自动修正状态");
                EndChapterFlow();
                return;
            }
        }
        // 4. 开始播放剧情
        currentNodeIndex = 0;
        PlayCurrentNode();
    }

    void Update()
    {
        if (currentConfig == null || currentConfig.storyNodes == null || currentConfig.storyNodes.Count == 0) return;

        if (currentConfig.storyNodes[currentNodeIndex].nodeType == StoryNodeType.Dialog)
        {
            // 使用 Input System 检测点击或空格
            bool isSkipPressed = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                isSkipPressed = true;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                isSkipPressed = true;

            if (isSkipPressed)
            {
                if (isTyping)
                {
                    // 如果正在打字，瞬间显示全本
                    StopAllCoroutines();
                    dialogText.text = currentFullText;
                    isTyping = false;
                }
                else
                {
                    // 如果已经显示完了，进入下一句
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
                // 确保背景是启用的且颜色正常
                backgroundDisplay.gameObject.SetActive(true);
                backgroundDisplay.color = Color.white;
            }
        }
        if(node.bgmClip != null)
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

        // 显示选项时，需要判断好感度要求
        // 注意：这里读取的是当前存档的总好感度 + 本局临时好感度
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
        // 只有在非回顾模式下，或者为了临时逻辑，可以增加 tempAffection
        // 但在 EndStorySegment 时我们会拦截保存
        tempAffection += choice.addAffection;

        choicePanel.SetActive(false);

        if (choice.jumpToNodeIndex != -1)
            currentNodeIndex = choice.jumpToNodeIndex - 1;

        NextNode();
    }

    private void EndStorySegment()
    {
        // 1. 处理好感度和社交（仅在正常游戏模式下）
        if (!isReplayMode)
        {
            if (tempAffection != 0)
                SaveManager.Instance.CurrentGameData.affinity += tempAffection;

            if (currentConfig.socialUpdate != null)
                InjectSocialUpdate(currentConfig.socialUpdate);
            // 注意：这里先不要急着 Save，根据是否有小游戏决定状态
        }
        // 2. 判断是否有小游戏
        if (!string.IsNullOrEmpty(currentConfig.miniGameSceneName))
        {
            // --- 进入小游戏逻辑 ---
            if (isReplayMode)
            {
                PlayerPrefs.SetInt("IsReplayMode", 1);
            }
            else
            {
                // 不是回顾模式，进入小游戏前，将状态设为 2
                SaveManager.Instance.CurrentGameData.chapterSubState = 2;
                SaveManager.Instance.SaveCurrentGame();
            }
            // 跳转
            SceneManager.LoadScene(currentConfig.miniGameSceneName);
        }
        else
        {
            // --- 没有小游戏，直接结束章节 ---
            EndChapterFlow();
        }
    }
    private void InjectSocialUpdate(SocialUpdateBatch batch)
    {
        GameData data = SaveManager.Instance.CurrentGameData;
        // A. 处理朋友圈解锁
        foreach (var moment in batch.unlockMomentIds)
        {
            //MomentsRepository.Instance.GetMomentById(moment);
            if (!data.unlockedMomentIds.Contains(moment))
            {
                data.unlockedMomentIds.Add(moment);
            }
        }
        // B. 处理对话和联系人解锁
        foreach (var conv in batch.conversations)
        {
            // 解锁联系人
            if (!data.unlockedContactIds.Contains(conv.contactId))
            {
                data.unlockedContactIds.Add(conv.contactId);
            }
            // 获取或创建该联系人的历史记录
            ContactHistoryData history = data.GetOrCreateInfo(conv.contactId);
            // 注入新消息
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
            // 设置红点
            history.hasUnread = true;
            // 注入待处理的玩家选项
            if (conv.replyOptions != null && conv.replyOptions.Count > 0)
            {
                history.pendingOptions = new List<ChatReplyOption>(conv.replyOptions);
            }
        }
        Debug.Log("[StoryManager] 社交数据已成功注入存档。");
    }

    // 处理无小游戏的章节结束，或从小游戏返回（如果逻辑在这的话）
    private void EndChapterFlow()
    {
        if (!isReplayMode && currentConfig != null)
        {
            // 正常推进：进入下一章
            SaveManager.Instance.CurrentGameData.currentChapter = currentConfig.nextChapterId;

            // --- 核心修改：重置 subState 为 0 (等待下一章开启) ---
            SaveManager.Instance.CurrentGameData.chapterSubState = 0;

            SaveManager.Instance.SaveCurrentGame();
            Debug.Log($"[Story] 章节结束，进度更新为: {currentConfig.nextChapterId}, 状态重置为 0");
        }
        else
        {
            Debug.Log("[Story] 回顾模式结束，不更新进度");
        }
        SceneManager.LoadScene("01_MainUI");
    }
}