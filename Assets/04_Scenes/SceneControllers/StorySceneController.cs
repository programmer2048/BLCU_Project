using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 剧情场景控制器
/// </summary>
public class StorySceneController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI chapterTitleText;
    public TextMeshProUGUI storyContentText;
    public Button nextButton;
    public Button skipButton;

    [Header("对话面板")]
    public GameObject dialoguePanel;

    private StoryTrigger _storyTrigger;

    void Start()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentState == GameState.Ending)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            return;
        }

        _storyTrigger = GetComponentInChildren<StoryTrigger>();
        if (_storyTrigger == null)
        {
            var go = new GameObject("StoryTrigger");
            go.transform.SetParent(transform);
            _storyTrigger = go.AddComponent<StoryTrigger>();
        }

        // 绑定UI
        _storyTrigger.chapterTitle = chapterTitleText;
        _storyTrigger.storyText = storyContentText;
        _storyTrigger.continueButton = nextButton;
        _storyTrigger.skipButton = skipButton;

        int chapter = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentChapterIndex : 0;
        _storyTrigger.StartStory(chapter);
    }
}
