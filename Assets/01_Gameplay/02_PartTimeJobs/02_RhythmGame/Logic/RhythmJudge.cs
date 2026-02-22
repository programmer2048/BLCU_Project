using UnityEngine;

/// <summary>
/// 音游判定系统 —— 「妙」「佳」「绝」三档判定
/// </summary>
public class RhythmJudge : MonoBehaviour
{
    [Header("判定线Y坐标")]
    public float judgeLine = -300f;

    [Header("判定区间（距判定线距离）")]
    public float perfectRange = 30f;  // 绝
    public float goodRange = 60f;     // 佳
    public float okRange = 100f;      // 妙

    [Header("分值")]
    public int perfectScore = 100;
    public int goodScore = 50;
    public int okScore = 20;

    public enum JudgeResult { Miss, Ok, Good, Perfect }

    /// <summary>判定音符</summary>
    public JudgeResult Judge(RhythmNote note)
    {
        if (note == null) return JudgeResult.Miss;

        float distance = Mathf.Abs(note.GetY() - judgeLine);

        if (distance <= perfectRange)
        {
            Debug.Log("[Rhythm] 绝！");
            return JudgeResult.Perfect;
        }
        if (distance <= goodRange)
        {
            Debug.Log("[Rhythm] 佳！");
            return JudgeResult.Good;
        }
        if (distance <= okRange)
        {
            Debug.Log("[Rhythm] 妙！");
            return JudgeResult.Ok;
        }

        return JudgeResult.Miss;
    }

    /// <summary>获取对应分值</summary>
    public int GetScore(JudgeResult result)
    {
        switch (result)
        {
            case JudgeResult.Perfect: return perfectScore;
            case JudgeResult.Good: return goodScore;
            case JudgeResult.Ok: return okScore;
            default: return 0;
        }
    }

    /// <summary>获取判定文字</summary>
    public string GetJudgeText(JudgeResult result)
    {
        switch (result)
        {
            case JudgeResult.Perfect: return "绝";
            case JudgeResult.Good: return "佳";
            case JudgeResult.Ok: return "妙";
            default: return "失";
        }
    }
}
