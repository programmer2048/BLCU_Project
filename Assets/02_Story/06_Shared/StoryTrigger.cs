using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 剧情触发逻辑 —— 各章节的剧情场景控制
/// </summary>
public class StoryTrigger : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI storyText;
    public TextMeshProUGUI chapterTitle;
    public Button continueButton;
    public Button skipButton;

    private int _currentLine;
    private string[] _currentStory;
    private int _chapterIndex;

    // 各章节剧情文本
    private static readonly string[][] ChapterStories = new string[][]
    {
        // Ch0: 应县木塔
        new string[] {
            "陈默来到应县木塔脚下，抬头仰望这座千年木塔，心中感慨万千。",
            "\"爷爷日记里写到：'木塔之美，在于每一根木头的坚守。'\"",
            "林晓正在旁边架起相机拍照，注意到了驻足出神的陈默。",
            "\"你也感受到了吧？这座塔没用一颗钉子，全靠榫卯连接。\"",
            "陈默看着斗拱结构，仿佛看到了千年前匠人们的心血..."
        },
        // Ch1: 佛光寺
        new string[] {
            "五台山深处，佛光寺静静伫立在山坡之上。",
            "\"1937年，梁思成和林徽因骑着毛驴，翻山越岭才找到这里。\"林晓说道。",
            "陈默走进东大殿，斑驳的壁画和泥塑在暮光中愈发庄严。",
            "\"他们当时看到唐代的石碑铭文，证实了这是唐代建筑...\"",
            "阳光透过门缝照进大殿，陈默仿佛穿越到了千年之前。"
        },
        // Ch2: 南禅寺
        new string[] {
            "南禅寺是中国现存最古老的木结构建筑。",
            "林晓轻声说：\"这里的彩塑修复过很多次，但每一次都保留着最初的韵味。\"",
            "陈默仔细观察着檐角的曲线，\"简洁，却充满力量。\"",
            "\"就像我们修复它一样，不是要让它变新，而是要让它继续活着。\""
        },
        // Ch3: 晋祠
        new string[] {
            "晋祠的圣母殿内，44尊侍女像排列两侧，神态各异。",
            "\"每一尊侍女都有自己的故事。\"林晓拿出相机，细细拍摄。",
            "陈默在一尊微笑的侍女像前停住了脚步。",
            "\"爷爷最爱的就是这尊。他说她的笑容里，有千年的温柔。\"",
            "林晓转过头看着陈默，\"你爷爷一定是个很有情怀的人。\""
        }
    };

    void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(NextLine);
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipStory);
    }

    public void StartStory(int chapterIndex)
    {
        _chapterIndex = Mathf.Clamp(chapterIndex, 0, ChapterStories.Length - 1);
        _currentStory = ChapterStories[_chapterIndex];
        _currentLine = 0;

        if (chapterTitle != null)
            chapterTitle.text = $"第{_chapterIndex + 1}章: {GameManager.SpotNames[_chapterIndex]}";

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_currentLine < _currentStory.Length)
        {
            if (storyText != null) storyText.text = _currentStory[_currentLine];
        }
        else
        {
            EndStory();
        }
    }

    private void NextLine()
    {
        _currentLine++;
        ShowCurrentLine();
    }

    private void SkipStory()
    {
        EndStory();
    }

    private void EndStory()
    {
        Debug.Log($"[Story] 第{_chapterIndex + 1}章剧情结束");
        EventBus.Publish(GameEvent.OnDialogueEnd);
        // 进入对应小游戏
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMiniGame(_chapterIndex);
    }
}
