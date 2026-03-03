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

    [Header("--- UI 引用 ---")]
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    public Image leftCharacter;
    public Image rightCharacter;

    [Header("--- 选项 UI 引用 ---")]
    public GameObject choicePanel;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonContainer;

    [Header("--- 视频 UI 引用 ---")]
    public GameObject videoPanel;
    public VideoPlayer videoPlayer;

    private int currentNodeIndex = 0;
    private int tempAffection = 0;
    private bool isTyping = false;
    private string currentFullText = "";

    void Start()
    {
        // 1. 初始化UI隐藏
        dialogPanel.SetActive(false);
        choicePanel.SetActive(false);
        videoPanel.SetActive(false);
        tempAffection = 0;
        // 2. 读取存档，找到应该播哪一章
        int currentChapterId = SaveManager.Instance.CurrentGameData.currentChapter;
        currentConfig = allChapters.Find(c => c.chapterId == currentChapterId);
        // 【修改点 1】：加入安全判断
        if (currentConfig == null || currentConfig.storyNodes == null || currentConfig.storyNodes.Count == 0)
        {
            Debug.LogError($"找不到第 {currentChapterId} 章的配置，或者该章节 StoryNodes 为空！直接返回主页。");
            // 直接加载主页，不走正常的结算流程
            SceneManager.LoadScene("MainUIScene");
            return;
        }
        // 3. 开始播放
        currentNodeIndex = 0;
        PlayCurrentNode();
    }

    void Update()
    {
        if (currentConfig == null || currentConfig.storyNodes == null || currentConfig.storyNodes.Count == 0) return;

        if (currentConfig.storyNodes[currentNodeIndex].nodeType == StoryNodeType.Dialog)
        {
            // 使用新版 Input System 检测鼠标左键或空格键按下
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
            EndStorySegment(); // 剧情部分播完了
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

    // --- 具体播放逻辑 (与之前基本一致) ---
    private void PlayVideo(VideoClip clip)
    {
        // 【防呆设计】如果视频为空（开头结尾预留但暂无资源），直接跳过
        if (clip == null)
        {
            Debug.LogWarning("未配置视频资源，自动跳过该节点。");
            NextNode();
            return;
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
        NextNode();
    }

    private void PlayDialog(StoryNode node)
    {
        videoPanel.SetActive(false);
        choicePanel.SetActive(false);
        dialogPanel.SetActive(true);
        nameText.text = node.speakerName;
        currentFullText = node.dialogText;
        // --- 处理左侧立绘 ---
        if (node.leftSprite != null)
        {
            leftCharacter.sprite = node.leftSprite;
            leftCharacter.gameObject.SetActive(true); // 开启显示
            leftCharacter.color = Color.white;        // 恢复正常颜色，不再使用变灰效果
        }
        else
        {
            leftCharacter.gameObject.SetActive(false); // 如果没配图（None），直接隐藏不绘制
        }
        // --- 处理右侧立绘 ---
        if (node.rightSprite != null)
        {
            rightCharacter.sprite = node.rightSprite;
            rightCharacter.gameObject.SetActive(true); // 开启显示
            rightCharacter.color = Color.white;        // 恢复正常颜色，不再使用变灰效果
        }
        else
        {
            rightCharacter.gameObject.SetActive(false); // 如果没配图（None），直接隐藏不绘制
        }
        StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        dialogText.text = "";
        foreach (char c in currentFullText.ToCharArray())
        {
            dialogText.text += c;
            yield return new WaitForSeconds(0.03f);
        }
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

    // ================= 核心：场景流转逻辑 =================
    private void EndStorySegment()
    {
        // 1. 结算本次剧情获得的好感度
        SaveManager.Instance.CurrentGameData.affinity += tempAffection;
        SaveManager.Instance.SaveCurrentGame();

        // 2. 检查接力棒往哪传
        if (!string.IsNullOrEmpty(currentConfig.miniGameSceneName))
        {
            Debug.Log($"剧情结束，准备进入小游戏场景: {currentConfig.miniGameSceneName}");
            // 暂时跳过 Message，直接进入小游戏
            SceneManager.LoadScene(currentConfig.miniGameSceneName);
        }
        else
        {
            Debug.Log("本章没有小游戏，直接结算章节并返回主页。");
            EndChapterFlow();
        }
    }

    // 真正结算整个章节，推进进度
    private void EndChapterFlow()
    {
        // 推进章节号
        if (currentConfig != null)
        {
            SaveManager.Instance.CurrentGameData.currentChapter = currentConfig.nextChapterId;
            SaveManager.Instance.SaveCurrentGame();
        }
        // 返回主界面
        SceneManager.LoadScene("01_MainUI");
    }
}