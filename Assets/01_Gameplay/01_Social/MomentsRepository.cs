using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MomentsRepository", menuName = "Social System/Moments Repository")]
public class MomentsRepository : ScriptableObject
{
    [Header("游戏内所有朋友圈数据")]
    public List<SocialMomentData> allMoments;
    public static MomentsRepository Instance;

    // 辅助函数：通过 ID 查找数据
    public SocialMomentData GetMomentById(string id)
    {
        return allMoments.Find(m => m.momentId == id);
    }
}