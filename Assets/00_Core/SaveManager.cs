using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    // 存档文件夹名称
    private const string SAVE_FOLDER = "Saves";
    private const string EXTENSION = ".json";

    // 获取存档根目录路径
    private static string GetSaveDirectory()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        // 如果文件夹不存在，自动创建
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    // 获取完整文件路径
    private static string GetFilePath(string fileName)
    {
        return Path.Combine(GetSaveDirectory(), fileName + EXTENSION);
    }

    /// <summary>
    /// 将对象序列化并写入硬盘
    /// </summary>
    public static void SaveToFile<T>(string fileName, T data)
    {
        try
        {
            // 1. 转为 JSON (prettyPrint=true 方便人眼阅读调试)
            string json = JsonUtility.ToJson(data, true);

            // 2. 写入文件
            string fullPath = GetFilePath(fileName);
            File.WriteAllText(fullPath, json, Encoding.UTF8);

            Debug.Log($"[SaveSystem] 成功保存文件: {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 保存失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从硬盘读取并反序列化
    /// </summary>
    public static T LoadFromFile<T>(string fileName) where T : new()
    {
        string fullPath = GetFilePath(fileName);
        if (!File.Exists(fullPath))
        {
            return new T(); // 如果文件不存在，返回一个全新的空对象，而不是 null，防止报错
        }

        try
        {
            string json = File.ReadAllText(fullPath, Encoding.UTF8);
            T data = JsonUtility.FromJson<T>(json);

            // 如果 JSON 格式不对，FromJson 可能返回 null
            if (data == null)
            {
                Debug.LogError($"[SaveSystem] 文件内容损坏: {fileName}");
                return new T();
            }
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 读取异常: {e.Message}");
            return new T(); // 发生异常时也返回一个空对象保底
        }
    }

    /// <summary>
    /// 删除指定文件
    /// </summary>
    public static void DeleteFile(string fileName)
    {
        string fullPath = GetFilePath(fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // 全局设置文件名
    private const string GLOBAL_SETTINGS_FILE = "global_settings";

    // 运行时数据缓存
    public GlobalSettings GlobalConfig { get; private set; }
    public GameData CurrentGameData { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGlobalSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================= 全局设置 =================

    private void LoadGlobalSettings()
    {
        // 尝试从硬盘加载
        GlobalConfig = SaveSystem.LoadFromFile<GlobalSettings>(GLOBAL_SETTINGS_FILE);

        // 如果硬盘里没有，就创建一个新的
        if (GlobalConfig == null)
        {
            GlobalConfig = new GlobalSettings();
            SaveGlobalSettings();
        }
    }

    public void SaveGlobalSettings()
    {
        SaveSystem.SaveToFile(GLOBAL_SETTINGS_FILE, GlobalConfig);
    }

    // ================= 游戏存档 =================
    public void ResetAllSaves()
    {
        Debug.Log("[SaveManager] 开始重置存档...");
        // 1. 删除所有已知的存档文件
        foreach (int slotId in GlobalConfig.existingSaveSlots)
        {
            string fileName = "save_" + slotId;
            SaveSystem.DeleteFile(fileName);
            Debug.Log($"已删除存档文件: {fileName}");
        }
        // 2. 清空全局配置中的存档记录
        GlobalConfig.existingSaveSlots.Clear();
        GlobalConfig.lastPlayedSaveIndex = -1;

        // 3. 重置当前内存中的数据
        CurrentGameData = null;
        // 4. 将重置后的全局配置写回硬盘 (保留音量设置，仅清除进度)
        SaveGlobalSettings();
        Debug.Log("[SaveManager] 存档清理完成，正在返回启动场景...");
        // 5. 返回 Boot 场景（确保 Boot 场景名为 "00_Boot"）
        TransitionManager.Instance.SwitchScene("00_Boot");
    }
    public bool HasAnySave()
    {
        return GlobalConfig.lastPlayedSaveIndex != -1 && GlobalConfig.existingSaveSlots.Count > 0;
    }

    public void CreateNewGame()
    {
        // 1. 找一个没用过的 ID (0, 1, 2...)
        int newSlotIndex = 0;
        while (GlobalConfig.existingSaveSlots.Contains(newSlotIndex))
        {
            newSlotIndex++;
        }

        // 2. 创建新数据
        string saveFileName = "save_" + newSlotIndex;
        CurrentGameData = new GameData(saveFileName);

        // 3. 更新全局记录
        GlobalConfig.existingSaveSlots.Add(newSlotIndex);
        GlobalConfig.lastPlayedSaveIndex = newSlotIndex;

        // 4. 双重保存
        SaveCurrentGame();
        SaveGlobalSettings();

        Debug.Log($"[SaveManager] 新建存档: {saveFileName}");
    }

    public void ContinueLastGame()
    {
        if (HasAnySave())
        {
            LoadGame(GlobalConfig.lastPlayedSaveIndex);
        }
    }

    public void LoadGame(int slotIndex)
    {
        string saveFileName = "save_" + slotIndex;
        CurrentGameData = SaveSystem.LoadFromFile<GameData>(saveFileName);

        if (CurrentGameData != null)
        {
            // 更新最后游玩时间
            GlobalConfig.lastPlayedSaveIndex = slotIndex;
            SaveGlobalSettings();
        }
        else
        {
            Debug.LogError($"[SaveManager] 存档文件 {saveFileName} 丢失！");
        }
    }

    public void SaveCurrentGame()
    {
        if (CurrentGameData != null)
        {
            CurrentGameData.lastSaveTime = System.DateTime.Now.ToString();
            // 使用 saveId 作为文件名
            SaveSystem.SaveToFile(CurrentGameData.saveId, CurrentGameData);
        }
    }
}