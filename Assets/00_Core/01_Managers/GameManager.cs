using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局数据管理器 —— 只负责数据存储与读写，不控制流程
/// 所有数据变更通过 EventBus 广播
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("货币")]
    [SerializeField] private int _travelFee = 0;
    public int TravelFee => _travelFee;

    [Header("好感度")]
    [SerializeField] private int _emotion = 0;
    public int Emotion => _emotion;

    [Header("景点解锁状态 (4个景点)")]
    [SerializeField] private bool[] _spotsUnlocked = new bool[4];
    [SerializeField] private bool[] _spotsExplored = new bool[4];

    [Header("图鉴收集")]
    [SerializeField] private List<string> _albumItems = new List<string>();
    public List<string> AlbumItems => _albumItems;

    // 景点名称
    public static readonly string[] SpotNames = { "应县木塔", "佛光寺", "南禅寺", "晋祠" };
    // 景点解锁费用
    public static readonly int[] SpotCosts = { 100, 200, 300, 400 };

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

    /// <summary>初始化数据（Boot阶段调用）</summary>
    public void InitializeData()
    {
        _travelFee = 0;
        _emotion = 0;
        _spotsUnlocked = new bool[4];
        _spotsExplored = new bool[4];
        _albumItems.Clear();
        Debug.Log("[GameManager] 数据初始化完成");
    }

    // ── 货币系统 ──
    public void AddCurrency(int amount)
    {
        _travelFee += amount;
        Debug.Log($"[GameManager] 获得旅费: +{amount}, 当前: {_travelFee}");
        EventBus.Publish<int>(GameEvent.OnCurrencyChanged, _travelFee);
        EventBus.Publish<int>(GameEvent.OnCurrencyEarned, amount);
        SaveManager.Instance?.Save();
    }

    public bool SpendCurrency(int amount)
    {
        if (_travelFee < amount)
        {
            Debug.Log($"[GameManager] 旅费不足! 需要: {amount}, 当前: {_travelFee}");
            return false;
        }
        _travelFee -= amount;
        Debug.Log($"[GameManager] 消耗旅费: -{amount}, 当前: {_travelFee}");
        EventBus.Publish<int>(GameEvent.OnCurrencyChanged, _travelFee);
        EventBus.Publish<int>(GameEvent.OnCurrencySpent, amount);
        SaveManager.Instance?.Save();
        return true;
    }

    // ── 好感度系统 ──
    public void AddEmotion(int amount)
    {
        _emotion += amount;
        _emotion = Mathf.Clamp(_emotion, 0, 100);
        Debug.Log($"[GameManager] 好感度变化: +{amount}, 当前: {_emotion}");
        EventBus.Publish<int>(GameEvent.OnEmotionChanged, _emotion);
        SaveManager.Instance?.Save();
    }

    // ── 景点系统 ──
    public bool IsSpotUnlocked(int index) => index >= 0 && index < 4 && _spotsUnlocked[index];
    public bool IsSpotExplored(int index) => index >= 0 && index < 4 && _spotsExplored[index];

    public bool UnlockSpot(int index)
    {
        if (index < 0 || index >= 4) return false;
        if (_spotsUnlocked[index]) return true;

        int cost = SpotCosts[index];
        if (!SpendCurrency(cost)) return false;

        _spotsUnlocked[index] = true;
        Debug.Log($"[GameManager] 景点解锁: {SpotNames[index]}");
        EventBus.Publish<int>(GameEvent.OnSpotUnlocked, index);

        // 检查是否全部解锁
        if (AllSpotsUnlocked())
            EventBus.Publish(GameEvent.OnAllSpotsUnlocked);

        SaveManager.Instance?.Save();

        return true;
    }

    public void SetSpotExplored(int index)
    {
        if (index >= 0 && index < 4)
        {
            _spotsExplored[index] = true;
            Debug.Log($"[GameManager] 景点已探索: {SpotNames[index]}");
            EventBus.Publish<int>(GameEvent.OnSpotExplored, index);
            SaveManager.Instance?.Save();
        }
    }

    public bool AllSpotsUnlocked()
    {
        for (int i = 0; i < 4; i++)
            if (!_spotsUnlocked[i]) return false;
        return true;
    }

    public bool AllSpotsExplored()
    {
        for (int i = 0; i < 4; i++)
            if (!_spotsExplored[i]) return false;
        return true;
    }

    // ── 图鉴系统 ──
    public void AddAlbumItem(string itemName)
    {
        if (!_albumItems.Contains(itemName))
        {
            _albumItems.Add(itemName);
            Debug.Log($"[GameManager] 图鉴解锁: {itemName}");
            EventBus.Publish<string>(GameEvent.OnAlbumItemUnlocked, itemName);
            SaveManager.Instance?.Save();
        }
    }

    public bool HasAlbumItem(string itemName) => _albumItems.Contains(itemName);

    public bool[] GetUnlockedSpotsSnapshot()
    {
        var copy = new bool[_spotsUnlocked.Length];
        _spotsUnlocked.CopyTo(copy, 0);
        return copy;
    }

    public bool[] GetExploredSpotsSnapshot()
    {
        var copy = new bool[_spotsExplored.Length];
        _spotsExplored.CopyTo(copy, 0);
        return copy;
    }

    public void ApplyLoadedData(int travelFee, int emotion, bool[] unlocked, bool[] explored, List<string> albumItems)
    {
        _travelFee = Mathf.Max(0, travelFee);
        _emotion = Mathf.Clamp(emotion, 0, 100);

        _spotsUnlocked = new bool[4];
        _spotsExplored = new bool[4];

        if (unlocked != null)
        {
            for (int i = 0; i < Mathf.Min(4, unlocked.Length); i++)
                _spotsUnlocked[i] = unlocked[i];
        }

        if (explored != null)
        {
            for (int i = 0; i < Mathf.Min(4, explored.Length); i++)
                _spotsExplored[i] = explored[i];
        }

        _albumItems = albumItems != null ? new List<string>(albumItems) : new List<string>();

        EventBus.Publish<int>(GameEvent.OnCurrencyChanged, _travelFee);
        EventBus.Publish<int>(GameEvent.OnEmotionChanged, _emotion);
        for (int i = 0; i < 4; i++)
        {
            if (_spotsUnlocked[i]) EventBus.Publish<int>(GameEvent.OnSpotUnlocked, i);
            if (_spotsExplored[i]) EventBus.Publish<int>(GameEvent.OnSpotExplored, i);
        }
        foreach (var item in _albumItems)
            EventBus.Publish<string>(GameEvent.OnAlbumItemUnlocked, item);
    }

    // ── 结局判定 ──
    public bool CanGetEndingA() => _emotion >= 50 && AllSpotsUnlocked();
}
