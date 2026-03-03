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

    void Start()
    {
        // 1. 初始化UI状态
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
        videoPanel.SetActive(false);
        tempAffection = 0;

        // 2. 获取目标章节 ID
        // 读取存档中的实际进度
        int savedProgress = SaveManager.Instance.CurrentGameData.currentChapter;

        // 读取主菜单传递过来的目标章节 ID (如果没有传递，默认使用存档进度)
        int selectedChapterId = PlayerPrefs.GetInt("SelectedChapterId", savedProgress);

        // 关键逻辑：如果选择的章节 < 存档的进度，说明是在回顾旧剧情
        // 如果选择的章节 == 存档进度，说明是推进新剧情
        if (selectedChapterId < savedProgress)
        {
            isReplayMode = true;
            Debug.Log($"当前处于回顾模式 (Replay)，不会保存进度和好感度。ID: {selectedChapterId}");
        }
        else
        {
            isReplayMode = false;
        }

        // 用完之后清除 Key，防止逻辑污染
        PlayerPrefs.DeleteKey("SelectedChapterId");

        // 3. 查找配置
        currentConfig = allChapters.Find(c => c.chapterId == selectedChapterId);

        if (currentConfig == null || currentConfig.storyNodes == null || currentConfig.storyNodes.Count == 0)
        {
            Debug.LogError($"找不到章节 {selectedChapterId} 的配置，或内容为空！直接返回主菜单。");
            SceneManager.LoadScene("01_MainUI");
            return;
        }

        // 4. 开始播放
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

    // ... (PlayVideo, OnVideoEnd, PlayDialog, TypeText 保持不变) ...
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

    // ================= 核心修改：结算逻辑 =================
    private void EndStorySegment()
    {
        // 1. 处理好感度保存
        // 如果是回顾模式，不要保存好感度，防止刷分
        if (!isReplayMode)
        {
            if (tempAffection != 0)
            {
                SaveManager.Instance.CurrentGameData.affinity += tempAffection;
                SaveManager.Instance.SaveCurrentGame();
                Debug.Log($"[Story] 保存好感度变化: {tempAffection}");
            }
        }
        else
        {
            Debug.Log("[Story] 回顾模式：跳过好感度保存");
        }

        // 2. 判断是否有小游戏
        if (!string.IsNullOrEmpty(currentConfig.miniGameSceneName))
        {
            Debug.Log($"剧情结束，准备进入小游戏: {currentConfig.miniGameSceneName}");
            // 注意：进入小游戏后，小游戏的 Controller 也需要知道这是回顾模式
            // 如果小游戏也会增加章节进度，你需要再次设置 PlayerPrefs 或传递参数
            if (isReplayMode)
            {
                PlayerPrefs.SetInt("IsReplayMode", 1); // 告诉小游戏这是回顾
            }

            SceneManager.LoadScene(currentConfig.miniGameSceneName);
        }
        else
        {
            Debug.Log("本章没有小游戏，直接结算章节流程");
            EndChapterFlow();
        }
    }

    // 处理无小游戏的章节结束，或从小游戏返回（如果逻辑在这的话）
    private void EndChapterFlow()
    {
        // 核心逻辑：只有不是回顾模式，才推进章节 ID
        if (!isReplayMode && currentConfig != null)
        {
            SaveManager.Instance.CurrentGameData.currentChapter = currentConfig.nextChapterId;
            SaveManager.Instance.SaveCurrentGame();
            Debug.Log($"[Story] 章节进度更新为: {currentConfig.nextChapterId}");
        }
        else
        {
            Debug.Log("[Story] 回顾模式：跳过章节进度更新");
        }

        // 返回主菜单
        SceneManager.LoadScene("01_MainUI");
    }
}