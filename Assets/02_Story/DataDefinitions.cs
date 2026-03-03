using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

// ================= 视觉小说剧情相关 =================
public enum StoryNodeType { Video, Dialog, Choice, EndStory }

[System.Serializable]
public class StoryChoice
{
    public string choiceText;
    public int addAffection;
    public int requireAffection;
    public int jumpToNodeIndex = -1;
}

[System.Serializable]
public class StoryNode
{
    public StoryNodeType nodeType;

    [Header("1. Video 配置")]
    public VideoClip videoClip;

    [Header("2. Dialog 配置")]
    public string speakerName;
    [TextArea(2, 5)] public string dialogText;
    public bool isLeftSpeaking;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("3. Choice 配置")]
    public List<StoryChoice> choices;
}

// ================= 手机消息(预留)相关 =================
[System.Serializable]
public class SocialPostData
{
    public string posterName;
    [TextArea] public string content;
    public Sprite image;
}

[System.Serializable]
public class ChatOption
{
    [TextArea] public string optionText;
    public int affectionBonus;
    [TextArea] public string npcReply;
}

[System.Serializable]
public class MessageData
{
    [Header("朋友圈内容")]
    public SocialPostData socialPost;

    [Header("聊天内容")]
    public string contactName;
    [TextArea(2, 4)] public List<string> npcPreMessages;

    [Header("玩家的回复选项")]
    public List<ChatOption> playerOptions;
}