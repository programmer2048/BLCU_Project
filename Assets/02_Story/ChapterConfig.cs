using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewChapter", menuName = "Story System/Chapter Config")]
public class ChapterConfig : ScriptableObject
{
    [Header("1. 基础信息")]
    public int chapterId;
    public string chapterTitle;
    public int unlockCost;

    [Header("2. 视觉小说剧情")]
    public List<StoryNode> storyNodes;

    [Header("3. 小游戏配置 (可选)")]
    public string miniGameSceneName;
    public string unlockItemName;

    [Header("4. 手机消息配置 (数据预留，当前暂不渲染)")]
    public MessageData messageData; // 解除注释，预留数据空间

    [Header("5. 本章结束后的跳转")]
    public int nextChapterId;
}