using UnityEngine;

/// <summary>
/// 好感度管理系统 —— 隐藏数值，通过对话选项影响
/// </summary>
public class EmotionSystem : MonoBehaviour
{
    public static EmotionSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>增加好感度</summary>
    public void AddEmotion(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddEmotion(amount);
    }

    /// <summary>获取当前好感度</summary>
    public int GetEmotion()
    {
        return GameManager.Instance != null ? GameManager.Instance.Emotion : 0;
    }

    /// <summary>获取好感度等级描述</summary>
    public string GetEmotionLevel()
    {
        int e = GetEmotion();
        if (e >= 80) return "挚友";
        if (e >= 50) return "好友";
        if (e >= 20) return "熟人";
        return "陌生人";
    }
}
