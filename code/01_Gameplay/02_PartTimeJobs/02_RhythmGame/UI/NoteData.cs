using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ChartJSON
{
    public ChartMetadata metadata;
    public List<ChartNote> notes;
}

[System.Serializable]
public class ChartMetadata
{
    public string title;
    public string artist;
    public float bpm;
    public float offset;
    public string musicFile;
}

[System.Serializable]
public class ChartNote
{
    public float beat;
    public int lane;
    public string type;
    public float duration;
}

public enum R_NoteType { Tap, Hold, Trap }
public enum Judgment { Myp, Jia, Jue, Kong, Miss } // 妙 佳 绝 空 Miss

[System.Serializable]
public class NoteData
{
    public float time;       // 判定时间（秒）
    public int lane;         // 轨道 0-4
    public R_NoteType type;    // 类型
    public float duration;   // 仅 Hold 有效

    // 运行时状态
    public bool isHit = false;
    public GameObject instance; // 对应的游戏物体
}