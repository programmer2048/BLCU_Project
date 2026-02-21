using UnityEngine;
using System;

/// <summary>
/// 存档管理器 —— 加密存档/读取/校验
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "GameSaveData";

    [Serializable]
    public class SaveData
    {
        public int travelFee;
        public int emotion;
        public bool[] spotsUnlocked = new bool[4];
        public bool[] spotsExplored = new bool[4];
        public string[] albumItems = new string[0];
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Save()
    {
        if (GameManager.Instance == null) return;
        var data = new SaveData();
        data.travelFee = GameManager.Instance.TravelFee;
        data.emotion = GameManager.Instance.Emotion;
        data.spotsUnlocked = GameManager.Instance.GetUnlockedSpotsSnapshot();
        data.spotsExplored = GameManager.Instance.GetExploredSpotsSnapshot();
        data.albumItems = GameManager.Instance.AlbumItems.ToArray();

        string json = JsonUtility.ToJson(data);
        string encrypted = EncryptHelper.Encrypt(json);
        PlayerPrefs.SetString(SAVE_KEY, encrypted);
        PlayerPrefs.Save();
        Debug.Log("[SaveManager] 存档成功");
    }

    public bool Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return false;
        string encrypted = PlayerPrefs.GetString(SAVE_KEY);
        string json = EncryptHelper.Decrypt(encrypted);
        if (string.IsNullOrEmpty(json)) return false;

        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null || GameManager.Instance == null) return false;

        GameManager.Instance.ApplyLoadedData(
            data.travelFee,
            data.emotion,
            data.spotsUnlocked,
            data.spotsExplored,
            data.albumItems != null ? new System.Collections.Generic.List<string>(data.albumItems) : null
        );

        Debug.Log("[SaveManager] 读档成功");
        return true;
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        Debug.Log("[SaveManager] 存档已删除");
    }
}
