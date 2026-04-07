using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewChapter", menuName = "Story System/Chapter Config")]
public class ChapterConfig : ScriptableObject
{
    [Header("1. 基础信息")]
    public int chapterId;
    public string chapterTitle;
    public int unlockCost;

    [Header("2. 视觉小说内容")]
    public List<StoryNode> storyNodes;

    [Header("3. 小游戏配置 (可选)")]
    public string miniGameSceneName;
    public string unlockItemName;

    [Header("4. 社交网络更新 (章节结束触发)")]
    public SocialUpdateBatch socialUpdate; // 使用新的结构

    [Header("5. 流程流转")]
    public int nextChapterId;
}