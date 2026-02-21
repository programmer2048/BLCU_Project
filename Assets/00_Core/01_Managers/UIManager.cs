using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI管理器 —— 管理界面面板的显示/隐藏，不控制逻辑
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("通用UI引用")]
    public TextMeshProUGUI currencyText;

    private Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();

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

    void OnEnable()
    {
        EventBus.Subscribe<int>(GameEvent.OnCurrencyChanged, UpdateCurrencyDisplay);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<int>(GameEvent.OnCurrencyChanged, UpdateCurrencyDisplay);
    }

    /// <summary>注册面板</summary>
    public void RegisterPanel(string panelName, GameObject panel)
    {
        _panels[panelName] = panel;
    }

    /// <summary>切换到指定面板（隐藏其他）</summary>
    public void SwitchTo(string viewName)
    {
        foreach (var kvp in _panels)
        {
            if (kvp.Value != null)
                kvp.Value.SetActive(kvp.Key == viewName);
        }
        Debug.Log($"[UIManager] 切换到面板: {viewName}");
    }

    /// <summary>显示指定面板</summary>
    public void ShowPanel(string panelName)
    {
        if (_panels.TryGetValue(panelName, out var panel) && panel != null)
            panel.SetActive(true);
    }

    /// <summary>隐藏指定面板</summary>
    public void HidePanel(string panelName)
    {
        if (_panels.TryGetValue(panelName, out var panel) && panel != null)
            panel.SetActive(false);
    }

    private void UpdateCurrencyDisplay(int amount)
    {
        if (currencyText != null)
            currencyText.text = $"旅费: {amount}";
    }

    /// <summary>更新货币显示（手动调用）</summary>
    public void RefreshCurrency()
    {
        if (GameManager.Instance != null && currencyText != null)
            currencyText.text = $"旅费: {GameManager.Instance.TravelFee}";
    }
}
