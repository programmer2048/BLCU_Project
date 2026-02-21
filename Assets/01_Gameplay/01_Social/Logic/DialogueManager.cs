using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 对话管理器 —— 加载对话/选项/分支
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    [SerializeField] private bool enableVerboseLog = false;

    // 当前对话索引
    private int _currentDialogueIndex = 0;
    private List<DialogueData> _dialogues;

    // 事件
    public event Action<DialogueData> OnDialogueShow;
    public event Action OnDialogueComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() { }

    /// <summary>初始化对话数据（硬编码Demo版）</summary>
    private void InitDialogues()
    {
        _dialogues = new List<DialogueData>
        {
            // 序章对话
            new DialogueData
            {
                id = 0,
                speaker = "系统",
                content = "凌晨3点，出租屋。你在爷爷的旧日记中发现了一张山西古建筑的照片...",
                choices = new DialogueChoice[]
                {
                    new DialogueChoice { text = "仔细端详照片", emotionChange = 5, nextId = 1 },
                    new DialogueChoice { text = "继续翻阅日记", emotionChange = 3, nextId = 1 }
                }
            },
            // 林晓初遇
            new DialogueData
            {
                id = 1,
                speaker = "林晓",
                content = "你也是来看木塔的吗？我是摄影师林晓，一直在拍山西的古建筑。",
                choices = new DialogueChoice[]
                {
                    new DialogueChoice { text = "真巧！我正好对古建筑感兴趣", emotionChange = 10, nextId = 2 },
                    new DialogueChoice { text = "嗯，路过看看", emotionChange = 2, nextId = 2 }
                }
            },
            // 旅途中对话
            new DialogueData
            {
                id = 2,
                speaker = "林晓",
                content = "你知道吗？应县木塔已经屹立近千年了，全靠的就是榫卯结构。",
                choices = new DialogueChoice[]
                {
                    new DialogueChoice { text = "太了不起了！我想了解更多", emotionChange = 8, nextId = 3 },
                    new DialogueChoice { text = "确实挺壮观的", emotionChange = 3, nextId = 3 }
                }
            },
            // 佛光寺
            new DialogueData
            {
                id = 3,
                speaker = "林晓",
                content = "佛光寺东大殿是梁思成和林徽因在1937年发现的，当时他们激动得落泪。",
                choices = new DialogueChoice[]
                {
                    new DialogueChoice { text = "他们的精神令人敬佩", emotionChange = 10, nextId = 4 },
                    new DialogueChoice { text = "原来有这样的故事", emotionChange = 5, nextId = 4 }
                }
            },
            // 南禅寺
            new DialogueData
            {
                id = 4,
                speaker = "林晓",
                content = "南禅寺的彩塑虽历经修复，但那份古朴之美依然动人。你觉得修复和保持原貌，哪个更重要？",
                choices = new DialogueChoice[]
                {
                    new DialogueChoice { text = "保护和传承同样重要", emotionChange = 10, nextId = 5 },
                    new DialogueChoice { text = "尽量保持原貌吧", emotionChange = 5, nextId = 5 }
                }
            },
            // 晋祠
            new DialogueData
            {
                id = 5,
                speaker = "林晓",
                content = "晋祠的侍女像，每一尊表情都不同。你觉得这趟旅行改变了你什么？",
                choices = new DialogueChoice[]
                {
                    new DialogueChoice { text = "让我重新找到了方向", emotionChange = 15, nextId = -1 },
                    new DialogueChoice { text = "至少看到了不一样的风景", emotionChange = 5, nextId = -1 }
                }
            }
        };
    }

    /// <summary>开始对话</summary>
    public void StartDialogue(int dialogueId = -1)
    {
        if (_dialogues == null || _dialogues.Count == 0)
            InitDialogues();

        if (dialogueId >= 0)
            _currentDialogueIndex = dialogueId;

        if (_currentDialogueIndex < _dialogues.Count)
        {
            var dialogue = _dialogues[_currentDialogueIndex];
            if (enableVerboseLog)
                Debug.Log($"[Dialogue] {dialogue.speaker}: {dialogue.content}");
            OnDialogueShow?.Invoke(dialogue);
        }
    }

    /// <summary>选择对话选项</summary>
    public void MakeChoice(int choiceIndex)
    {
        if (_dialogues == null || _dialogues.Count == 0)
            InitDialogues();

        if (_currentDialogueIndex >= _dialogues.Count) return;
        var dialogue = _dialogues[_currentDialogueIndex];
        if (choiceIndex < 0 || choiceIndex >= dialogue.choices.Length) return;

        var choice = dialogue.choices[choiceIndex];
        if (enableVerboseLog)
            Debug.Log($"[Dialogue] 选择: {choice.text} (好感度 +{choice.emotionChange})");

        // 增加好感度
        if (GameManager.Instance != null)
            GameManager.Instance.AddEmotion(choice.emotionChange);

        EventBus.Publish<int>(GameEvent.OnDialogueChoice, choiceIndex);

        // 下一段对话
        if (choice.nextId >= 0)
        {
            _currentDialogueIndex = choice.nextId;
            StartDialogue();
        }
        else
        {
            // 对话结束
            if (enableVerboseLog)
                Debug.Log("[Dialogue] 对话结束");
            OnDialogueComplete?.Invoke();
            EventBus.Publish(GameEvent.OnDialogueEnd);
        }
    }

    public DialogueData GetCurrentDialogue()
    {
        if (_dialogues == null || _dialogues.Count == 0)
            InitDialogues();

        if (_currentDialogueIndex < _dialogues.Count)
            return _dialogues[_currentDialogueIndex];
        return null;
    }
}

/// <summary>对话数据结构</summary>
[Serializable]
public class DialogueData
{
    public int id;
    public string speaker;
    public string content;
    public DialogueChoice[] choices;
}

/// <summary>对话选项</summary>
[Serializable]
public class DialogueChoice
{
    public string text;
    public int emotionChange;
    public int nextId; // -1 表示结束
}
