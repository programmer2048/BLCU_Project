using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // --- 基础信息 ---
    public string saveId;        // 存档ID (文件名)
    public string lastSaveTime;  // 最后修改时间

    // --- 资源与进度 ---
    public int money = 0;
    public int affinity = 0;
    public int currentChapter = 1;
    public int chapterSubState = 0;

    // --- 收集系统 (ID索引) ---
    [SerializeField]
    private List<string> collectedInfoIds = new List<string>();

    // ==========================================
    // 关键修复：必须显式写出无参数构造函数
    // ==========================================
    public GameData()
    {
        // 反序列化时会被调用
        collectedInfoIds = new List<string>();
    }

    // 带参构造函数 (用于新建存档时手动调用)
    public GameData(string id)
    {
        this.saveId = id;
        this.lastSaveTime = System.DateTime.Now.ToString();
        this.collectedInfoIds = new List<string>();

        // 可以在这里初始化默认数值
        this.money = 1000;
        this.currentChapter = 1;
    }

    // --- 辅助方法 ---
    public bool HasInfo(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return collectedInfoIds.Contains(id);
    }

    public void AddInfo(string id)
    {
        if (!HasInfo(id)) collectedInfoIds.Add(id);
    }
}