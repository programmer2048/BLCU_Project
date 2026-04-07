[System.Serializable]
public class GlobalSettings
{
    // --- 音频设置 ---
    public float musicVolume = 1.0f; // 0.0 到 1.0
    public bool isMusicOn = true;    // 新增：BGM 开关状态
    public float sfxVolume = 1.0f;
    public bool isSfxOn = true;      // 新增：音效 开关状态
    // --- 存档元数据 ---
    public int lastPlayedSaveIndex = -1;
    public System.Collections.Generic.List<int> existingSaveSlots;
    public GlobalSettings()
    {
        existingSaveSlots = new System.Collections.Generic.List<int>();
    }
}