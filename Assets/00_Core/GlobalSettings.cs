using System.Collections.Generic;

[System.Serializable]
public class GlobalSettings
{
    // --- 设置部分 ---
    public float musicVolume = 1.0f;
    public float sfxVolume = 1.0f;

    // --- 存档元数据 ---
    public int lastPlayedSaveIndex = -1; // -1 表示没有存档，>=0 表示存档槽位
    public List<int> existingSaveSlots;
    public GlobalSettings()
    {
        existingSaveSlots = new List<int>();
    }
}