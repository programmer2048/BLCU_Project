using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 时光修复师 —— 拼图占位小游戏
/// 三个微型步骤简化为：点击正确碎片完成修复
/// </summary>
public class TimeRestorer : MonoBehaviour
{
    [Header("UI")]
    public RectTransform playArea;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI stepText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backButton;

    private int _currentStep; // 0=拼图 1=上色 2=扶正
    private int _stepProgress;
    private bool _isPlaying;
    private List<GameObject> _elements = new List<GameObject>();

    private readonly string[] StepNames = { "步骤1: 拼合碎片", "步骤2: 匹配颜色", "步骤3: 扶正梁柱" };
    private readonly int[] StepTargets = { 3, 3, 2 };

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
    }

    public void StartGame()
    {
        Debug.Log("[TimeRestorer] 时光修复师开始！");
        _isPlaying = true;
        _currentStep = 0;
        _stepProgress = 0;
        if (resultPanel != null) resultPanel.SetActive(false);
        ClearElements();
        SetupStep();
    }

    private void SetupStep()
    {
        ClearElements();
        if (_currentStep >= 3)
        {
            EndGame(true);
            return;
        }

        if (stepText != null) stepText.text = StepNames[_currentStep];
        UpdateStatus();

        Color[] stepColors = {
            new Color(0.7f, 0.5f, 0.3f),
            new Color(0.3f, 0.6f, 0.8f),
            new Color(0.6f, 0.3f, 0.5f)
        };

        int count = StepTargets[_currentStep];
        for (int i = 0; i < count; i++)
        {
            GameObject elem = new GameObject($"Step{_currentStep}_Item{i}");
            RectTransform rt = elem.AddComponent<RectTransform>();
            rt.SetParent(playArea != null ? playArea : transform, false);
            float x = Random.Range(-200f, 200f);
            float y = Random.Range(-150f, 150f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(80, 80);

            Image img = elem.AddComponent<Image>();
            img.color = stepColors[_currentStep];

            Button btn = elem.AddComponent<Button>();
            int idx = i;
            btn.onClick.AddListener(() => OnElementClicked(idx, elem));

            // 标签
            GameObject label = new GameObject("Label");
            RectTransform lrt = label.AddComponent<RectTransform>();
            lrt.SetParent(rt, false);
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            string[] labels = _currentStep == 0 ? new[]{"碎片A","碎片B","碎片C"} :
                              _currentStep == 1 ? new[]{"红","蓝","绿"} :
                              new[]{"左柱","右柱","中柱"};
            tmp.text = i < labels.Length ? labels[i] : $"项{i}";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            _elements.Add(elem);
        }
    }

    private void OnElementClicked(int index, GameObject elem)
    {
        if (!_isPlaying) return;
        _stepProgress++;
        Debug.Log($"[TimeRestorer] {StepNames[_currentStep]} 进度: {_stepProgress}/{StepTargets[_currentStep]}");

        var img = elem.GetComponent<Image>();
        if (img != null) img.color = Color.green;
        elem.GetComponent<Button>().interactable = false;

        UpdateStatus();

        if (_stepProgress >= StepTargets[_currentStep])
        {
            _currentStep++;
            _stepProgress = 0;
            SetupStep();
        }
    }

    private void UpdateStatus()
    {
        if (statusText != null && _currentStep < 3)
            statusText.text = $"进度: {_stepProgress}/{StepTargets[_currentStep]}";
    }

    private void EndGame(bool success)
    {
        _isPlaying = false;
        if (resultPanel != null) resultPanel.SetActive(true);

        if (success)
        {
            Debug.Log("[TimeRestorer] 修复完成！解锁南禅寺禅意图鉴");
            if (resultText != null) resultText.text = "修复完成！\n解锁: 南禅寺禅意图鉴";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddAlbumItem("南禅寺禅意图鉴");
                GameManager.Instance.SetSpotExplored(2);
            }
        }
        EventBus.Publish(GameEvent.OnMiniGameEnd);
    }

    private void OnBack()
    {
        ClearElements();
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainUI();
    }

    private void ClearElements()
    {
        foreach (var e in _elements)
            if (e != null) Destroy(e.gameObject);
        _elements.Clear();
    }
}
