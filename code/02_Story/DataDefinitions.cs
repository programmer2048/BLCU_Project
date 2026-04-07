using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

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

    [Header("场景环境配置")]
    public Sprite backgroundSprite; // 如果非空，播放该节点时切换背景
    public AudioClip bgmClip;

    [Header("1. Video 参数")]
    public VideoClip videoClip;

    [Header("2. Dialog 参数")]
    public string speakerName;
    [TextArea(2, 5)] public string dialogText;
    public bool isLeftSpeaking;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("3. Choice 参数")]
    public List<StoryChoice> choices;
}
public enum MessageType { Text, Image, SystemAlert }
public enum SenderType { NPC, Player, System }
// 单条聊天记录（用于运行时存储和历史回放）
[System.Serializable]
public class ChatMessage
{
    public SenderType sender;
    public MessageType type;
    [TextArea] public string content; // 文字内容 或 图片名称/路径
    public string timeStamp; // 例如 "14:20"
}
// 聊天选项（玩家回复）
[System.Serializable]
public class ChatReplyOption
{
    [TextArea] public string optionText;
    public int affectionBonus;
    [TextArea] public string npcResponse; // 选完后的回应
}
// 朋友圈数据
[System.Serializable]
public class SocialMomentData
{
    public string momentId;     // 唯一ID，例如 "mom_ch1_01"
    public string contactId;    // 发帖人ID，关联 ContactConfig
    [TextArea] public string content; // 正文
    public Sprite image;        // 配图 (可为空)
    public int likeCount;       // 点赞数
    public List<string> comments; // 评论列表
}
[System.Serializable]
public class SocialUpdateBatch
{
    // 修改：这里只存 ID 列表，因为具体数据在 Config 里
    // 每次章节结束，只告诉系统解锁哪些 ID
    public List<string> unlockMomentIds;

    [Header("新增对话")]
    public List<NewConversation> conversations;
}
[System.Serializable]
public class NewConversation
{
    public string contactId; // 对应 ContactConfig 的 ID
    public bool forceTop; // 是否置顶（如新消息）

    [Header("NPC 发送的消息序列")]
    [TextArea] public List<string> messages; // 连续发几条

    [Header("玩家的回复选项 (可选)")]
    public List<ChatReplyOption> replyOptions; // 如果为空，说明只是NPC单方面发消息
}